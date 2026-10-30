using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace PVZHE.ModEditor.ScriptEditor;

internal static class XWCSharpIdeService
{
	internal sealed class LocationResult
	{
		public string FilePath = "";

		public int Line;

		public int Column;

		public int Length;

		public string Preview = "";
	}

	internal sealed class SymbolInfoResult
	{
		public string Name = "";

		public string Kind = "";

		public string Display = "";

		public string Documentation = "";

		public string Signature = "";

		public LocationResult Definition;

		public List<LocationResult> References = new List<LocationResult>();
	}

	internal sealed class RenamePreview
	{
		public string ProjectRoot = "";

		public string OldName = "";

		public string NewName = "";

		public string SymbolDisplay = "";

		public List<LocationResult> Occurrences = new List<LocationResult>();

		public Dictionary<string, string> OriginalDocuments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		public Dictionary<string, string> UpdatedDocuments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		public List<string> ReadOnlyGeneratedDocuments = new List<string>();

		public bool IsValid
		{
			get
			{
				if (Occurrences.Count > 0 && UpdatedDocuments.Count > 0)
				{
					return ReadOnlyGeneratedDocuments.Count == 0;
				}
				return false;
			}
		}
	}

	internal sealed class RenameTransaction
	{
		public string ProjectRoot = "";

		public string OldName = "";

		public string NewName = "";

		public Dictionary<string, string> Before = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		public Dictionary<string, string> After = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	}

	private sealed class SemanticProject
	{
		public string Root = "";

		public CSharpCompilation Compilation;

		public Dictionary<string, SyntaxTree> Trees = new Dictionary<string, SyntaxTree>(StringComparer.OrdinalIgnoreCase);

		public Dictionary<string, string> Sources = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		public ConcurrentDictionary<string, SemanticModel> Models = new ConcurrentDictionary<string, SemanticModel>(StringComparer.OrdinalIgnoreCase);

		public SemanticModel GetModel(string path, SyntaxTree tree)
		{
			return Models.GetOrAdd(path, (string _) => Compilation.GetSemanticModel(tree, ignoreAccessibility: true));
		}
	}

	internal readonly struct IdeMetrics(int semanticBuildRuns, int semanticCacheHits, int metadataBuildRuns)
	{
		public readonly int SemanticBuildRuns = semanticBuildRuns;

		public readonly int SemanticCacheHits = semanticCacheHits;

		public readonly int MetadataBuildRuns = metadataBuildRuns;
	}

	internal readonly struct RenameHistoryAvailability(bool canUndo, bool canRedo)
	{
		public bool CanUndo { get; } = canUndo;

		public bool CanRedo { get; } = canRedo;
	}

	private static readonly object TransactionLock = new object();

	private static readonly object SemanticCacheLock = new object();

	private static readonly Dictionary<string, SemanticProject> SemanticProjects = new Dictionary<string, SemanticProject>(StringComparer.Ordinal);

	private static readonly Lazy<MetadataReference[]> StableMetadataReferences = new Lazy<MetadataReference[]>(BuildMetadataReferences, LazyThreadSafetyMode.ExecutionAndPublication);

	private static RenameTransaction _lastTransaction;

	private static bool _lastTransactionApplied;

	private static string _lastRenameError = "";

	private static int _semanticBuildRuns;

	private static int _semanticCacheHits;

	private static int _metadataBuildRuns;

	public static string LastRenameError
	{
		get
		{
			lock (TransactionLock)
			{
				return _lastRenameError;
			}
		}
	}

	internal static IdeMetrics GetMetrics()
	{
		return new IdeMetrics(Volatile.Read(in _semanticBuildRuns), Volatile.Read(in _semanticCacheHits), Volatile.Read(in _metadataBuildRuns));
	}

	internal static RenameHistoryAvailability GetRenameHistoryAvailability(string currentProjectRoot)
	{
		string text = NormalizeRoot(currentProjectRoot);
		lock (TransactionLock)
		{
			bool flag = !string.IsNullOrWhiteSpace(text) && _lastTransaction != null && string.Equals(NormalizeRoot(_lastTransaction.ProjectRoot), text, StringComparison.OrdinalIgnoreCase);
			return new RenameHistoryAvailability(flag && _lastTransactionApplied, flag && !_lastTransactionApplied);
		}
	}

	public static async Task<SymbolInfoResult> FindDefinitionAsync(string projectRoot, string filePath, string source, int offset, IReadOnlyDictionary<string, string> openDocuments = null)
	{
		SemanticProject project = await BuildSemanticProjectAsync(projectRoot, filePath, source, openDocuments);
		return await Task.Run(() => QuerySymbol(project, filePath, offset, includeReferences: false));
	}

	public static async Task<SymbolInfoResult> FindReferencesAsync(string projectRoot, string filePath, string source, int offset, IReadOnlyDictionary<string, string> openDocuments = null)
	{
		SemanticProject project = await BuildSemanticProjectAsync(projectRoot, filePath, source, openDocuments);
		return await Task.Run(() => QuerySymbol(project, filePath, offset, includeReferences: true));
	}

	public static async Task<SymbolInfoResult> GetSymbolInfoAsync(string projectRoot, string filePath, string source, int offset, IReadOnlyDictionary<string, string> openDocuments = null)
	{
		SemanticProject project = await BuildSemanticProjectAsync(projectRoot, filePath, source, openDocuments);
		return await Task.Run(() => QuerySymbol(project, filePath, offset, includeReferences: false));
	}

	public static async Task<RenamePreview> PreviewRenameAsync(string projectRoot, string filePath, string source, int offset, string newName, IReadOnlyDictionary<string, string> openDocuments = null)
	{
		if (!IsValidIdentifier(newName))
		{
			return new RenamePreview();
		}
		SemanticProject project = await BuildSemanticProjectAsync(projectRoot, filePath, source, openDocuments);
		return await Task.Run(() => BuildRenamePreview(project, filePath, offset, newName));
	}

	public static Task<bool> ApplyRenameAsync(RenamePreview preview)
	{
		return Task.Run(delegate
		{
			if (preview == null || !preview.IsValid || !ValidateDocumentsInRoot(preview.UpdatedDocuments, preview.ProjectRoot))
			{
				SetRenameError("重命名预览无效，或包含当前 Mod 之外的路径。");
				return false;
			}
			string text = DescribeDiskMismatch(preview.OriginalDocuments, preview.ProjectRoot);
			if (!string.IsNullOrEmpty(text))
			{
				SetRenameError("文件在预览后已被外部修改；已中止，未覆盖任何内容。" + text);
				return false;
			}
			RenameTransaction renameTransaction = new RenameTransaction
			{
				ProjectRoot = NormalizeRoot(preview.ProjectRoot),
				OldName = preview.OldName,
				NewName = preview.NewName,
				Before = new Dictionary<string, string>(preview.OriginalDocuments, StringComparer.OrdinalIgnoreCase),
				After = new Dictionary<string, string>(preview.UpdatedDocuments, StringComparer.OrdinalIgnoreCase)
			};
			if (!WriteDocuments(renameTransaction.After, renameTransaction.ProjectRoot, renameTransaction.Before))
			{
				return false;
			}
			lock (TransactionLock)
			{
				_lastTransaction = renameTransaction;
				_lastTransactionApplied = true;
			}
			PublishDocuments(renameTransaction.After);
			SetRenameError("");
			return true;
		});
	}

	public static Task<bool> UndoLastRenameAsync(string currentProjectRoot)
	{
		string root = NormalizeRoot(currentProjectRoot);
		return Task.Run(delegate
		{
			RenameTransaction lastTransaction;
			lock (TransactionLock)
			{
				if (string.IsNullOrWhiteSpace(root) || _lastTransaction == null || !_lastTransactionApplied || !string.Equals(NormalizeRoot(_lastTransaction.ProjectRoot), root, StringComparison.OrdinalIgnoreCase))
				{
					_lastRenameError = "当前 Mod 没有可撤销的项目重命名记录。";
					return false;
				}
				lastTransaction = _lastTransaction;
			}
			if (!WriteDocuments(lastTransaction.Before, lastTransaction.ProjectRoot, lastTransaction.After))
			{
				return false;
			}
			lock (TransactionLock)
			{
				_lastTransactionApplied = false;
			}
			PublishDocuments(lastTransaction.Before);
			SetRenameError("");
			return true;
		});
	}

	public static Task<bool> RedoLastRenameAsync(string currentProjectRoot)
	{
		string root = NormalizeRoot(currentProjectRoot);
		return Task.Run(delegate
		{
			RenameTransaction lastTransaction;
			lock (TransactionLock)
			{
				if (string.IsNullOrWhiteSpace(root) || _lastTransaction == null || _lastTransactionApplied || !string.Equals(NormalizeRoot(_lastTransaction.ProjectRoot), root, StringComparison.OrdinalIgnoreCase))
				{
					_lastRenameError = "当前 Mod 没有可重做的项目重命名记录。";
					return false;
				}
				lastTransaction = _lastTransaction;
			}
			if (!WriteDocuments(lastTransaction.After, lastTransaction.ProjectRoot, lastTransaction.Before))
			{
				return false;
			}
			lock (TransactionLock)
			{
				_lastTransactionApplied = true;
			}
			PublishDocuments(lastTransaction.After);
			SetRenameError("");
			return true;
		});
	}

	public static string FormatCSharp(string source)
	{
		return CSharpSyntaxTree.ParseText(source ?? "", new CSharpParseOptions(LanguageVersion.Latest)).GetRoot().NormalizeWhitespace("    ", "\n")
			.ToFullString();
	}

	public static string TryBuildQuickFix(string source, XWCodeErrorChecker.ErrorData diagnostic, out string title)
	{
		title = "";
		if (diagnostic == null)
		{
			return null;
		}
		string text = source ?? "";
		if (diagnostic.Code == "MISSING_SEMICOLON")
		{
			string[] array = text.Replace("\r\n", "\n").Split('\n');
			if (diagnostic.Line < 0 || diagnostic.Line >= array.Length)
			{
				return null;
			}
			if (array[diagnostic.Line].TrimEnd().EndsWith(';'))
			{
				return null;
			}
			array[diagnostic.Line] = array[diagnostic.Line].TrimEnd() + ";";
			title = "补上分号";
			return string.Join("\n", array);
		}
		if (diagnostic.Code == "MISSING_USING")
		{
			Match match = Regex.Match(diagnostic.Message ?? "", "来自\\s+(?<namespaces>[A-Za-z0-9_. /]+)$");
			string text2 = (match.Success ? match.Groups["namespaces"].Value.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() : "");
			if (string.IsNullOrWhiteSpace(text2) || Regex.IsMatch(text, "(?m)^\\s*using\\s+" + Regex.Escape(text2) + "\\s*;"))
			{
				return null;
			}
			title = "添加 using " + text2;
			return "using " + text2 + ";\n" + text;
		}
		return null;
	}

	public static int GetTextOffset(string source, int line, int column)
	{
		string text = source ?? "";
		int num = 0;
		int num2 = 0;
		while (num < text.Length && num2 < Math.Max(0, line))
		{
			if (text[num++] == '\n')
			{
				num2++;
			}
		}
		return Math.Clamp(num + Math.Max(0, column), 0, text.Length);
	}

	private static async Task<SemanticProject> BuildSemanticProjectAsync(string projectRoot, string filePath, string source, IReadOnlyDictionary<string, string> openDocuments)
	{
		string root = NormalizeRoot(projectRoot);
		if (string.IsNullOrWhiteSpace(root))
		{
			throw new InvalidOperationException("No active Mod project is open.");
		}
		string normalizedFile = NormalizePath(filePath);
		if (!IsInsideRoot(normalizedFile, root))
		{
			throw new InvalidOperationException("The active document is outside the current Mod project.");
		}
		await XWCSharpProjectIndex.RequestFullIndexAsync(root);
		XWCSharpProjectIndex.Snapshot snapshot = XWCSharpProjectIndex.GetSnapshot();
		return await Task.Run(delegate
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>(snapshot.Documents, StringComparer.OrdinalIgnoreCase);
			if (openDocuments != null)
			{
				foreach (KeyValuePair<string, string> openDocument in openDocuments)
				{
					string text = NormalizePath(openDocument.Key);
					if (IsSafeProjectFile(text, root) && text.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
					{
						dictionary[text] = openDocument.Value ?? "";
					}
				}
			}
			if (!IsSafeProjectFile(normalizedFile, root))
			{
				throw new InvalidOperationException("The active document uses a junction or symbolic link and cannot be refactored safely.");
			}
			dictionary[normalizedFile] = source ?? "";
			string key = root + "|" + ComputeDocumentsFingerprint(dictionary);
			lock (SemanticCacheLock)
			{
				if (SemanticProjects.TryGetValue(key, out var value))
				{
					Interlocked.Increment(ref _semanticCacheHits);
					return value;
				}
			}
			SemanticProject semanticProject = BuildSemanticProject(root, dictionary);
			lock (SemanticCacheLock)
			{
				if (SemanticProjects.Count >= 4)
				{
					SemanticProjects.Clear();
				}
				SemanticProjects[key] = semanticProject;
			}
			Interlocked.Increment(ref _semanticBuildRuns);
			return semanticProject;
		});
	}

	private static SemanticProject BuildSemanticProject(string root, Dictionary<string, string> documents)
	{
		SemanticProject semanticProject = new SemanticProject
		{
			Root = root
		};
		List<SyntaxTree> list = new List<SyntaxTree>();
		CSharpParseOptions options = new CSharpParseOptions(LanguageVersion.Latest);
		foreach (KeyValuePair<string, string> item in documents.OrderBy<KeyValuePair<string, string>, string>((KeyValuePair<string, string> pair) => pair.Key, StringComparer.OrdinalIgnoreCase))
		{
			string text = NormalizePath(item.Key);
			if (IsSafeProjectFile(text, root) && text.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
			{
				SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(item.Value ?? "", options, text);
				semanticProject.Trees[text] = syntaxTree;
				semanticProject.Sources[text] = item.Value ?? "";
				list.Add(syntaxTree);
			}
		}
		semanticProject.Compilation = CSharpCompilation.Create("ModEditorIde", list, StableMetadataReferences.Value, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		return semanticProject;
	}

	private static MetadataReference[] BuildMetadataReferences()
	{
		Interlocked.Increment(ref _metadataBuildRuns);
		HashSet<string> paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		AddAssembly(typeof(object).Assembly);
		AddAssembly(typeof(Enumerable).Assembly);
		AddAssembly(typeof(Task).Assembly);
		AddAssembly(typeof(Node).Assembly);
		AddAssembly(typeof(XWCSharpIdeService).Assembly);
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		for (int i = 0; i < assemblies.Length; i++)
		{
			AddAssembly(assemblies[i]);
		}
		foreach (string item in XWInGameDotNetBuildService.ResolveReferenceAssemblies())
		{
			if (File.Exists(item))
			{
				paths.Add(item);
			}
		}
		List<MetadataReference> list = new List<MetadataReference>();
		foreach (string item2 in paths)
		{
			try
			{
				list.Add(MetadataReference.CreateFromFile(item2));
			}
			catch
			{
			}
		}
		return list.ToArray();
		void AddAssembly(Assembly assembly)
		{
			if (!(assembly == null) && !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
			{
				paths.Add(assembly.Location);
			}
		}
	}

	private static string ComputeDocumentsFingerprint(Dictionary<string, string> documents)
	{
		using IncrementalHash incrementalHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
		foreach (KeyValuePair<string, string> item in documents.OrderBy<KeyValuePair<string, string>, string>((KeyValuePair<string, string> pair) => pair.Key, StringComparer.OrdinalIgnoreCase))
		{
			byte[] bytes = Encoding.UTF8.GetBytes(NormalizePath(item.Key));
			byte[] bytes2 = Encoding.UTF8.GetBytes(item.Value ?? "");
			incrementalHash.AppendData(bytes);
			incrementalHash.AppendData(new byte[1]);
			incrementalHash.AppendData(bytes2);
			incrementalHash.AppendData(new byte[1] { 255 });
		}
		return Convert.ToHexString(incrementalHash.GetHashAndReset());
	}

	private static SymbolInfoResult QuerySymbol(SemanticProject project, string filePath, int offset, bool includeReferences)
	{
		string text = NormalizePath(filePath);
		if (!project.Trees.TryGetValue(text, out var value))
		{
			return null;
		}
		SemanticModel model = project.GetModel(text, value);
		SyntaxToken token = FindIdentifierToken(value, offset);
		ISymbol symbol = ResolveSymbol(model, token);
		if (symbol == null)
		{
			return null;
		}
		symbol = symbol.OriginalDefinition;
		SymbolInfoResult symbolInfoResult = new SymbolInfoResult
		{
			Name = symbol.Name,
			Kind = symbol.Kind.ToString(),
			Display = symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
			Signature = BuildSignature(symbol),
			Documentation = CleanDocumentation(symbol.GetDocumentationCommentXml())
		};
		Location location = symbol.Locations.FirstOrDefault((Location location2) => location2.IsInSource);
		if (location != null)
		{
			symbolInfoResult.Definition = ToLocation(project, location);
		}
		if (includeReferences)
		{
			symbolInfoResult.References = FindSymbolLocations(project, symbol);
		}
		return symbolInfoResult;
	}

	private static RenamePreview BuildRenamePreview(SemanticProject project, string filePath, int offset, string newName)
	{
		string text = NormalizePath(filePath);
		RenamePreview renamePreview = new RenamePreview
		{
			ProjectRoot = project.Root,
			NewName = newName
		};
		if (!project.Trees.TryGetValue(text, out var value))
		{
			return renamePreview;
		}
		ISymbol symbol = ResolveSymbol(project.GetModel(text, value), FindIdentifierToken(value, offset))?.OriginalDefinition;
		if (symbol == null || !CanRename(symbol, project.Root))
		{
			return renamePreview;
		}
		renamePreview.OldName = symbol.Name;
		renamePreview.SymbolDisplay = symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
		List<LocationResult> list = FindSymbolLocations(project, symbol);
		renamePreview.Occurrences.AddRange(list);
		foreach (IGrouping<string, LocationResult> item in list.GroupBy<LocationResult, string>((LocationResult item) => item.FilePath, StringComparer.OrdinalIgnoreCase))
		{
			if (!project.Sources.TryGetValue(item.Key, out var original))
			{
				continue;
			}
			if (XWBlueprintGeneratedCSharpPolicy.IsGeneratedSource(original))
			{
				renamePreview.ReadOnlyGeneratedDocuments.Add(item.Key);
				continue;
			}
			string text2 = original;
			foreach (LocationResult item2 in item.OrderByDescending((LocationResult item) => GetTextOffset(original, item.Line, item.Column)))
			{
				int textOffset = GetTextOffset(original, item2.Line, item2.Column);
				if (textOffset >= 0 && textOffset + item2.Length <= text2.Length)
				{
					text2 = text2.Remove(textOffset, item2.Length).Insert(textOffset, newName);
				}
			}
			if (!(text2 == original))
			{
				renamePreview.OriginalDocuments[item.Key] = original;
				renamePreview.UpdatedDocuments[item.Key] = text2;
			}
		}
		return renamePreview;
	}

	private static List<LocationResult> FindSymbolLocations(SemanticProject project, ISymbol target)
	{
		List<LocationResult> list = new List<LocationResult>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (KeyValuePair<string, SyntaxTree> tree in project.Trees)
		{
			SyntaxTree value = tree.Value;
			SemanticModel model = project.GetModel(tree.Key, value);
			foreach (SyntaxToken item2 in value.GetRoot().DescendantTokens())
			{
				if (!item2.IsKind(SyntaxKind.IdentifierToken) || item2.ValueText != target.Name)
				{
					continue;
				}
				ISymbol x = ResolveSymbol(model, item2)?.OriginalDefinition;
				if (SymbolEqualityComparer.Default.Equals(x, target))
				{
					LocationResult locationResult = ToLocation(project, item2.GetLocation());
					string item = $"{locationResult.FilePath}:{locationResult.Line}:{locationResult.Column}";
					if (hashSet.Add(item))
					{
						list.Add(locationResult);
					}
				}
			}
		}
		list.Sort(delegate(LocationResult a, LocationResult b)
		{
			int num = string.Compare(a.FilePath, b.FilePath, StringComparison.OrdinalIgnoreCase);
			if (num != 0)
			{
				return num;
			}
			int num2 = a.Line.CompareTo(b.Line);
			return (num2 == 0) ? a.Column.CompareTo(b.Column) : num2;
		});
		return list;
	}

	private static ISymbol ResolveSymbol(SemanticModel model, SyntaxToken token)
	{
		SyntaxNode parent = token.Parent;
		for (SyntaxNode syntaxNode = parent; syntaxNode != null; syntaxNode = syntaxNode.Parent)
		{
			ISymbol symbol = ((syntaxNode is BaseTypeDeclarationSyntax declarationSyntax) ? model.GetDeclaredSymbol(declarationSyntax) : ((syntaxNode is DelegateDeclarationSyntax declarationSyntax2) ? model.GetDeclaredSymbol(declarationSyntax2) : ((syntaxNode is MethodDeclarationSyntax declarationSyntax3) ? model.GetDeclaredSymbol(declarationSyntax3) : ((syntaxNode is ConstructorDeclarationSyntax declarationSyntax4) ? model.GetDeclaredSymbol(declarationSyntax4) : ((syntaxNode is PropertyDeclarationSyntax declarationSyntax5) ? model.GetDeclaredSymbol(declarationSyntax5) : ((syntaxNode is EventDeclarationSyntax declarationSyntax6) ? model.GetDeclaredSymbol(declarationSyntax6) : ((syntaxNode is VariableDeclaratorSyntax declarationSyntax7) ? model.GetDeclaredSymbol(declarationSyntax7) : ((syntaxNode is ParameterSyntax declarationSyntax8) ? ((ISymbol)model.GetDeclaredSymbol(declarationSyntax8)) : ((ISymbol)((!(syntaxNode is TypeParameterSyntax typeParameter)) ? null : model.GetDeclaredSymbol(typeParameter)))))))))));
			ISymbol symbol2 = symbol;
			if (symbol2 != null && GetDeclarationIdentifier(syntaxNode) == token)
			{
				return symbol2;
			}
			bool flag = syntaxNode != parent;
			if (flag)
			{
				bool flag2 = ((syntaxNode is StatementSyntax || syntaxNode is MemberDeclarationSyntax) ? true : false);
				flag = flag2;
			}
			if (flag)
			{
				break;
			}
		}
		for (SyntaxNode syntaxNode2 = parent; syntaxNode2 != null; syntaxNode2 = syntaxNode2.Parent)
		{
			SymbolInfo symbolInfo = model.GetSymbolInfo(syntaxNode2);
			ISymbol symbol3 = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();
			if (symbol3 != null)
			{
				return symbol3;
			}
			if ((syntaxNode2 is StatementSyntax || syntaxNode2 is MemberDeclarationSyntax) ? true : false)
			{
				break;
			}
		}
		return null;
	}

	private static SyntaxToken GetDeclarationIdentifier(SyntaxNode declaration)
	{
		if (!(declaration is BaseTypeDeclarationSyntax { Identifier: var identifier }))
		{
			if (!(declaration is DelegateDeclarationSyntax { Identifier: var identifier2 }))
			{
				if (!(declaration is MethodDeclarationSyntax { Identifier: var identifier3 }))
				{
					if (!(declaration is ConstructorDeclarationSyntax { Identifier: var identifier4 }))
					{
						if (!(declaration is PropertyDeclarationSyntax { Identifier: var identifier5 }))
						{
							if (!(declaration is EventDeclarationSyntax { Identifier: var identifier6 }))
							{
								if (!(declaration is VariableDeclaratorSyntax { Identifier: var identifier7 }))
								{
									if (!(declaration is ParameterSyntax { Identifier: var identifier8 }))
									{
										if (!(declaration is TypeParameterSyntax { Identifier: var identifier9 }))
										{
											return default(SyntaxToken);
										}
										return identifier9;
									}
									return identifier8;
								}
								return identifier7;
							}
							return identifier6;
						}
						return identifier5;
					}
					return identifier4;
				}
				return identifier3;
			}
			return identifier2;
		}
		return identifier;
	}

	private static SyntaxToken FindIdentifierToken(SyntaxTree tree, int offset)
	{
		SourceText text = tree.GetText();
		int position = Math.Clamp(offset, 0, Math.Max(0, text.Length - 1));
		SyntaxToken syntaxToken = tree.GetRoot().FindToken(position, findInsideTrivia: true);
		if (syntaxToken.IsKind(SyntaxKind.IdentifierToken))
		{
			return syntaxToken;
		}
		SyntaxToken previousToken = syntaxToken.GetPreviousToken(includeZeroWidth: true);
		if (!previousToken.IsKind(SyntaxKind.IdentifierToken))
		{
			return syntaxToken;
		}
		return previousToken;
	}

	private static LocationResult ToLocation(SemanticProject project, Location location)
	{
		FileLinePositionSpan lineSpan = location.GetLineSpan();
		string text = NormalizePath(lineSpan.Path);
		string value;
		string[] array = (project.Sources.TryGetValue(text, out value) ? value : "").Replace("\r\n", "\n").Split('\n');
		int line = lineSpan.StartLinePosition.Line;
		return new LocationResult
		{
			FilePath = text,
			Line = line,
			Column = lineSpan.StartLinePosition.Character,
			Length = location.SourceSpan.Length,
			Preview = ((line >= 0 && line < array.Length) ? array[line].Trim() : "")
		};
	}

	private static string BuildSignature(ISymbol symbol)
	{
		if ((symbol is IMethodSymbol || symbol is IPropertySymbol || symbol is IEventSymbol) ? true : false)
		{
			return symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
		}
		return "";
	}

	private static string CleanDocumentation(string xml)
	{
		if (string.IsNullOrWhiteSpace(xml))
		{
			return "";
		}
		return Regex.Replace(WebUtility.HtmlDecode(Regex.Replace(xml, "<[^>]+>", " ")), "\\s+", " ").Trim();
	}

	private static bool CanRename(ISymbol symbol, string root)
	{
		if (symbol == null || symbol.IsImplicitlyDeclared || string.IsNullOrWhiteSpace(symbol.Name))
		{
			return false;
		}
		return symbol.Locations.Any((Location location) => location.IsInSource && IsSafeProjectFile(location.SourceTree?.FilePath, root));
	}

	private static bool IsValidIdentifier(string value)
	{
		if (string.IsNullOrWhiteSpace(value) || !SyntaxFacts.IsValidIdentifier(value))
		{
			return false;
		}
		if (SyntaxFacts.GetKeywordKind(value) == SyntaxKind.None)
		{
			return SyntaxFacts.GetContextualKeywordKind(value) == SyntaxKind.None;
		}
		return false;
	}

	private static bool ValidateDocumentsInRoot(Dictionary<string, string> documents, string root)
	{
		string normalizedRoot = NormalizeRoot(root);
		if (!string.IsNullOrWhiteSpace(normalizedRoot) && documents.Count > 0)
		{
			return documents.All((KeyValuePair<string, string> pair) => IsSafeProjectFile(pair.Key, normalizedRoot) && pair.Key.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) && !XWBlueprintGeneratedCSharpPolicy.IsGeneratedSource(pair.Value));
		}
		return false;
	}

	private static bool WriteDocuments(Dictionary<string, string> documents, string root, Dictionary<string, string> expectedCurrent)
	{
		if (!ValidateDocumentsInRoot(documents, root) || !ValidateDocumentsInRoot(expectedCurrent, root) || !DiskMatchesPreview(expectedCurrent, root))
		{
			SetRenameError("文件内容或路径在操作前已变化；已中止，未覆盖任何内容。");
			return false;
		}
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, string> dictionary2 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		List<string> list = new List<string>();
		bool flag = false;
		try
		{
			foreach (KeyValuePair<string, string> document in documents)
			{
				string text = NormalizePath(document.Key);
				if (!File.Exists(text))
				{
					throw new FileNotFoundException("Rename target disappeared.", text);
				}
				string text2 = text + ".modeditor-rename-" + Guid.NewGuid().ToString("N") + ".tmp";
				File.WriteAllText(text2, document.Value ?? "");
				dictionary[text] = text2;
			}
			foreach (KeyValuePair<string, string> item in dictionary)
			{
				if (!expectedCurrent.TryGetValue(item.Key, out var value) || !File.Exists(item.Key) || !string.Equals(File.ReadAllText(item.Key), value, StringComparison.Ordinal))
				{
					throw new IOException("Rename target changed while applying the transaction.");
				}
				string text3 = item.Key + ".modeditor-rename-" + Guid.NewGuid().ToString("N") + ".bak";
				File.Replace(item.Value, item.Key, text3, ignoreMetadataErrors: true);
				dictionary2[item.Key] = text3;
				list.Add(item.Key);
			}
			flag = true;
			return true;
		}
		catch
		{
			for (int num = list.Count - 1; num >= 0; num--)
			{
				string text4 = list[num];
				if (dictionary2.TryGetValue(text4, out var value2) && File.Exists(value2))
				{
					try
					{
						File.Replace(value2, text4, null, ignoreMetadataErrors: true);
					}
					catch
					{
					}
				}
			}
			SetRenameError("重命名事务检测到并发修改；已回滚，未保留部分写入。");
			return false;
		}
		finally
		{
			foreach (string value3 in dictionary.Values)
			{
				try
				{
					if (File.Exists(value3))
					{
						File.Delete(value3);
					}
				}
				catch
				{
				}
			}
			if (flag)
			{
				foreach (string value4 in dictionary2.Values)
				{
					try
					{
						if (File.Exists(value4))
						{
							File.Delete(value4);
						}
					}
					catch
					{
					}
				}
			}
		}
	}

	private static void PublishDocuments(Dictionary<string, string> documents)
	{
		foreach (KeyValuePair<string, string> document in documents)
		{
			XWCSharpProjectIndex.RequestSourceUpdate(document.Key, document.Value);
		}
	}

	private static string NormalizeRoot(string root)
	{
		string text = NormalizePath(root).TrimEnd('/');
		if (!Directory.Exists(text) || IsReparsePoint(text))
		{
			return "";
		}
		return text;
	}

	private static string NormalizePath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return "";
		}
		if (path.StartsWith("res://", StringComparison.OrdinalIgnoreCase) || path.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
		{
			path = ProjectSettings.GlobalizePath(path);
		}
		try
		{
			return Path.GetFullPath(path).Replace('\\', '/');
		}
		catch
		{
			return path.Replace('\\', '/');
		}
	}

	private static bool IsInsideRoot(string path, string root)
	{
		string text = NormalizePath(path);
		string value = NormalizePath(root).TrimEnd('/') + "/";
		return text.StartsWith(value, StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsSafeProjectFile(string path, string root)
	{
		string path2 = NormalizePath(path);
		string text = NormalizeRoot(root);
		if (string.IsNullOrWhiteSpace(text) || !IsInsideRoot(path2, text) || !File.Exists(path2))
		{
			return false;
		}
		if (IsReparsePoint(path2))
		{
			return false;
		}
		string relativePath = Path.GetRelativePath(text, path2);
		string text2 = text;
		string[] array = relativePath.Split(new char[2]
		{
			Path.DirectorySeparatorChar,
			Path.AltDirectorySeparatorChar
		}, StringSplitOptions.RemoveEmptyEntries);
		foreach (string path3 in array)
		{
			text2 = Path.Combine(text2, path3);
			if ((Directory.Exists(text2) || File.Exists(text2)) && IsReparsePoint(text2))
			{
				return false;
			}
		}
		return true;
	}

	private static bool IsReparsePoint(string path)
	{
		try
		{
			return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
		}
		catch
		{
			return true;
		}
	}

	private static bool DiskMatchesPreview(Dictionary<string, string> expected, string root)
	{
		return string.IsNullOrEmpty(DescribeDiskMismatch(expected, root));
	}

	private static string DescribeDiskMismatch(Dictionary<string, string> expected, string root)
	{
		if (!ValidateDocumentsInRoot(expected, root))
		{
			return "路径或文件集无效。";
		}
		foreach (KeyValuePair<string, string> item in expected)
		{
			try
			{
				string text = File.ReadAllText(item.Key);
				string text2 = item.Value ?? "";
				if (!string.Equals(text, text2, StringComparison.Ordinal))
				{
					return $"不匹配文件: {Path.GetFileName(item.Key)} (disk={text.Length}, preview={text2.Length})";
				}
			}
			catch (Exception ex)
			{
				return $"无法读取: {Path.GetFileName(item.Key)} ({ex.GetType().Name})";
			}
		}
		return "";
	}

	private static void SetRenameError(string message)
	{
		lock (TransactionLock)
		{
			_lastRenameError = message ?? "";
		}
	}
}
