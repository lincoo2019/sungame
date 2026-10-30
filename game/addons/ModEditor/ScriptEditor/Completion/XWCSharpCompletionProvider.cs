using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Godot;

namespace PVZHE.ModEditor.ScriptEditor;

public partial class XWCSharpCompletionProvider : XWCodeCompletionProvider
{
	public sealed class CompletionAnalysis
	{
		internal string Prefix = "";

		internal string Owner = "";

		internal bool IsMemberCompletion;

		internal bool IsUsingDirective;

		internal bool IsCompletionBlocked;

		internal string UsingNamespacePrefix = "";

		internal int CaretOffset;

		internal string CurrentClassName = "";

		internal string CurrentBaseClassName = "Node";

		internal string CurrentNamespace = "";

		internal string DocumentPath = "";

		internal string Source = "";

		internal HashSet<string> UsingNamespaces = new HashSet<string>(StringComparer.Ordinal);

		internal List<string> Namespaces = new List<string>();

		internal List<string> KnownTypeNames = new List<string>();

		internal List<CompletionSymbol> Symbols = new List<CompletionSymbol>();

		internal List<CompletionOptionData> Options = new List<CompletionOptionData>();

		internal HashSet<string> AddedOptions = new HashSet<string>(StringComparer.Ordinal);

		internal CancellationToken CancellationToken;
	}

	internal enum CompletionColorKind
	{
		Keyword,
		Type,
		Method,
		Property,
		Constant,
		Local,
		Namespace
	}

	internal sealed class CompletionOptionData
	{
		public int Kind;

		public string Display = "";

		public string Insert = "";

		public CompletionColorKind ColorKind;
	}

	internal sealed class CompletionSymbol
	{
		public string Name = "";

		public string Type = "";
	}

	private static readonly Color KeywordColor = new Color(0.69f, 0.49f, 0.85f);

	private static readonly Color TypeColor = new Color(0.55f, 0.82f, 1f);

	private static readonly Color MethodColor = new Color(0.66f, 0.92f, 0.63f);

	private static readonly Color PropertyColor = new Color(0.72f, 0.86f, 0.98f);

	private static readonly Color ConstantColor = new Color(0.94f, 0.78f, 0.55f);

	private static readonly Color LocalColor = new Color(0.82f, 0.86f, 0.9f);

	private static readonly Color NamespaceColor = new Color(0.68f, 0.78f, 0.95f);

	private static readonly string[] Keywords = new string[81]
	{
		"abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
		"class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum",
		"event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "get",
		"goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock", "long",
		"namespace", "new", "null", "object", "operator", "out", "override", "params", "private", "protected",
		"public", "readonly", "ref", "return", "sbyte", "sealed", "set", "short", "sizeof", "stackalloc",
		"static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint",
		"ulong", "unchecked", "unsafe", "ushort", "using", "var", "virtual", "void", "volatile", "while",
		"yield"
	};

	private static readonly (string display, string insert)[] Snippets = new(string, string)[7]
	{
		("_Ready()", "public override void _Ready()\n{\n\t\n}"),
		("_Process(delta)", "public override void _Process(double delta)\n{\n\t\n}"),
		("_PhysicsProcess(delta)", "public override void _PhysicsProcess(double delta)\n{\n\t\n}"),
		("[Export]", "[Export] "),
		("[Signal]", "[Signal]\npublic delegate void SignalEventHandler();"),
		("GetNode<T>", "GetNode<>()"),
		("GetNodeOrNull<T>", "GetNodeOrNull<>()")
	};

	private static readonly (string display, string insert)[] GodotTypes = new(string, string)[48]
	{
		("Node", "Node"),
		("Node2D", "Node2D"),
		("Node3D", "Node3D"),
		("CanvasItem", "CanvasItem"),
		("Sprite2D", "Sprite2D"),
		("AnimatedSprite2D", "AnimatedSprite2D"),
		("Control", "Control"),
		("Button", "Button"),
		("Label", "Label"),
		("LineEdit", "LineEdit"),
		("TextEdit", "TextEdit"),
		("CodeEdit", "CodeEdit"),
		("Panel", "Panel"),
		("Color", "Color"),
		("Vector2", "Vector2"),
		("Vector2I", "Vector2I"),
		("Vector3", "Vector3"),
		("Vector3I", "Vector3I"),
		("Rect2", "Rect2"),
		("Rect2I", "Rect2I"),
		("Transform2D", "Transform2D"),
		("Resource", "Resource"),
		("PackedScene", "PackedScene"),
		("Texture2D", "Texture2D"),
		("Timer", "Timer"),
		("Area2D", "Area2D"),
		("CharacterBody2D", "CharacterBody2D"),
		("RigidBody2D", "RigidBody2D"),
		("CollisionShape2D", "CollisionShape2D"),
		("AnimationPlayer", "AnimationPlayer"),
		("Tween", "Tween"),
		("InputEvent", "InputEvent"),
		("StringName", "StringName"),
		("NodePath", "NodePath"),
		("Callable", "Callable"),
		("Signal", "Signal"),
		("Variant", "Variant"),
		("GodotObject", "GodotObject"),
		("RefCounted", "RefCounted"),
		("GD", "GD"),
		("Mathf", "Mathf"),
		("Input", "Input"),
		("DisplayServer", "DisplayServer"),
		("Engine", "Engine"),
		("ResourceLoader", "ResourceLoader"),
		("ResourceSaver", "ResourceSaver"),
		("OS", "OS"),
		("ProjectSettings", "ProjectSettings")
	};

	private static readonly (string display, string insert)[] GlobalMethods = new(string, string)[23]
	{
		("GD.Print", "GD.Print()"),
		("GD.PrintErr", "GD.PrintErr()"),
		("GD.PushWarning", "GD.PushWarning()"),
		("GD.PushError", "GD.PushError()"),
		("GD.Load", "GD.Load<>()"),
		("GetNode", "GetNode<>()"),
		("GetNodeOrNull", "GetNodeOrNull<>()"),
		("AddChild", "AddChild()"),
		("RemoveChild", "RemoveChild()"),
		("QueueFree", "QueueFree()"),
		("EmitSignal", "EmitSignal()"),
		("Connect", "Connect()"),
		("Disconnect", "Disconnect()"),
		("CreateTween", "CreateTween()"),
		("GetTree", "GetTree()"),
		("GetParent", "GetParent()"),
		("GetChildren", "GetChildren()"),
		("ResourceLoader.Load", "ResourceLoader.Load<>()"),
		("ResourceLoader.Exists", "ResourceLoader.Exists()"),
		("Input.IsActionPressed", "Input.IsActionPressed()"),
		("Input.IsActionJustPressed", "Input.IsActionJustPressed()"),
		("Mathf.Clamp", "Mathf.Clamp()"),
		("Mathf.Lerp", "Mathf.Lerp()")
	};

	private static readonly Dictionary<string, (string display, string insert, CodeEdit.CodeCompletionKind kind)[]> StaticMembers = new Dictionary<string, (string, string, CodeEdit.CodeCompletionKind)[]>(StringComparer.Ordinal)
	{
		["GD"] = new(string, string, CodeEdit.CodeCompletionKind)[8]
		{
			("Print()", "Print()", CodeEdit.CodeCompletionKind.Function),
			("PrintErr()", "PrintErr()", CodeEdit.CodeCompletionKind.Function),
			("PushWarning()", "PushWarning()", CodeEdit.CodeCompletionKind.Function),
			("PushError()", "PushError()", CodeEdit.CodeCompletionKind.Function),
			("Load<T>()", "Load<>()", CodeEdit.CodeCompletionKind.Function),
			("PrintRich()", "PrintRich()", CodeEdit.CodeCompletionKind.Function),
			("Randf()", "Randf()", CodeEdit.CodeCompletionKind.Function),
			("Randi()", "Randi()", CodeEdit.CodeCompletionKind.Function)
		},
		["Mathf"] = new(string, string, CodeEdit.CodeCompletionKind)[13]
		{
			("Abs()", "Abs()", CodeEdit.CodeCompletionKind.Function),
			("Clamp()", "Clamp()", CodeEdit.CodeCompletionKind.Function),
			("Lerp()", "Lerp()", CodeEdit.CodeCompletionKind.Function),
			("Min()", "Min()", CodeEdit.CodeCompletionKind.Function),
			("Max()", "Max()", CodeEdit.CodeCompletionKind.Function),
			("Pow()", "Pow()", CodeEdit.CodeCompletionKind.Function),
			("Sin()", "Sin()", CodeEdit.CodeCompletionKind.Function),
			("Cos()", "Cos()", CodeEdit.CodeCompletionKind.Function),
			("Sqrt()", "Sqrt()", CodeEdit.CodeCompletionKind.Function),
			("DegToRad()", "DegToRad()", CodeEdit.CodeCompletionKind.Function),
			("RadToDeg()", "RadToDeg()", CodeEdit.CodeCompletionKind.Function),
			("Pi", "Pi", CodeEdit.CodeCompletionKind.Constant),
			("Tau", "Tau", CodeEdit.CodeCompletionKind.Constant)
		},
		["Input"] = new(string, string, CodeEdit.CodeCompletionKind)[5]
		{
			("IsActionPressed()", "IsActionPressed()", CodeEdit.CodeCompletionKind.Function),
			("IsActionJustPressed()", "IsActionJustPressed()", CodeEdit.CodeCompletionKind.Function),
			("IsActionJustReleased()", "IsActionJustReleased()", CodeEdit.CodeCompletionKind.Function),
			("GetVector()", "GetVector()", CodeEdit.CodeCompletionKind.Function),
			("MouseMode", "MouseMode", CodeEdit.CodeCompletionKind.Constant)
		},
		["ResourceLoader"] = new(string, string, CodeEdit.CodeCompletionKind)[2]
		{
			("Load<T>()", "Load<>()", CodeEdit.CodeCompletionKind.Function),
			("Exists()", "Exists()", CodeEdit.CodeCompletionKind.Function)
		},
		["ResourceSaver"] = new(string, string, CodeEdit.CodeCompletionKind)[1] { ("Save()", "Save()", CodeEdit.CodeCompletionKind.Function) },
		["GodotObject"] = new(string, string, CodeEdit.CodeCompletionKind)[1] { ("IsInstanceValid()", "IsInstanceValid()", CodeEdit.CodeCompletionKind.Function) },
		["Colors"] = new(string, string, CodeEdit.CodeCompletionKind)[7]
		{
			("White", "White", CodeEdit.CodeCompletionKind.Constant),
			("Black", "Black", CodeEdit.CodeCompletionKind.Constant),
			("Transparent", "Transparent", CodeEdit.CodeCompletionKind.Constant),
			("Red", "Red", CodeEdit.CodeCompletionKind.Constant),
			("Green", "Green", CodeEdit.CodeCompletionKind.Constant),
			("Blue", "Blue", CodeEdit.CodeCompletionKind.Constant),
			("Yellow", "Yellow", CodeEdit.CodeCompletionKind.Constant)
		}
	};

	public XWCSharpCompletionProvider(CodeEdit codeEdit)
		: base(codeEdit)
	{
	}

	public override void RequestCompletion()
	{
		CodeEdit?.UpdateCodeCompletionOptions(force: true);
	}

	public Task<CompletionAnalysis> AnalyzeCompletionAsync(string source, string documentPath, int caretLine, int caretColumn, Action workerEntered, Action workerExited, CancellationToken cancellationToken)
	{
		return Task.Run(delegate
		{
			workerEntered?.Invoke();
			Thread currentThread = Thread.CurrentThread;
			ThreadPriority priority = currentThread.Priority;
			try
			{
				currentThread.Priority = ThreadPriority.BelowNormal;
				CompletionAnalysis completionAnalysis = BuildAnalysis(source ?? "", documentPath ?? "", caretLine, caretColumn, cancellationToken);
				BuildCompletionOptions(completionAnalysis);
				return completionAnalysis;
			}
			finally
			{
				currentThread.Priority = priority;
				workerExited?.Invoke();
			}
		}, cancellationToken);
	}

	public void ApplyCompletion(CompletionAnalysis context)
	{
		if (CodeEdit == null || context == null)
		{
			return;
		}
		if (!context.IsCompletionBlocked)
		{
			int num = Math.Min(context.Options.Count, 256);
			for (int i = 0; i < num; i++)
			{
				CompletionOptionData completionOptionData = context.Options[i];
				CodeEdit.AddCodeCompletionOption((CodeEdit.CodeCompletionKind)completionOptionData.Kind, completionOptionData.Display, completionOptionData.Insert, ResolveColor(completionOptionData.ColorKind));
			}
		}
		CodeEdit.UpdateCodeCompletionOptions(force: true);
	}

	private static void BuildCompletionOptions(CompletionAnalysis context)
	{
		if (!context.IsCompletionBlocked)
		{
			if (context.IsUsingDirective)
			{
				AddUsingNamespaceCompletions(context);
			}
			else if (context.IsMemberCompletion)
			{
				AddMemberCompletions(context);
			}
			else
			{
				AddGlobalCompletions(context);
			}
			context.Options = context.Options.OrderBy((CompletionOptionData option) => GetCompletionPriority(option.ColorKind)).ThenBy<CompletionOptionData, string>((CompletionOptionData option) => option.Display, StringComparer.OrdinalIgnoreCase).Take(256)
				.ToList();
		}
	}

	private static CompletionAnalysis BuildAnalysis(string source, string documentPath, int line, int column, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		string line2 = GetLine(source, line);
		column = Math.Clamp(column, 0, line2.Length);
		string text = line2.Substring(0, column);
		int num = text.Length;
		while (num > 0 && IsIdentifierChar(text[num - 1]))
		{
			num--;
		}
		CompletionAnalysis completionAnalysis = new CompletionAnalysis
		{
			Prefix = text.Substring(num),
			CaretOffset = GetCaretOffset(source, line, column),
			CancellationToken = cancellationToken,
			DocumentPath = documentPath,
			Source = source
		};
		completionAnalysis.IsCompletionBlocked = IsInCommentOrString(source, completionAnalysis.CaretOffset, cancellationToken);
		cancellationToken.ThrowIfCancellationRequested();
		completionAnalysis.UsingNamespaces = XWCSharpCodeModel.ExtractUsingNamespaces(source);
		completionAnalysis.CurrentNamespace = XWCSharpCodeModel.ExtractCurrentNamespace(source);
		Match match = Regex.Match(text.TrimStart(), "^using\\s+(?!\\()(?:(?:static)\\s+)?(?:(?:[A-Za-z_][A-Za-z0-9_]*)\\s*=\\s*)?(?<ns>[A-Za-z_][A-Za-z0-9_.]*)?$");
		if (match.Success)
		{
			string value = match.Groups["ns"].Value;
			int num2 = value.LastIndexOf('.');
			completionAnalysis.IsUsingDirective = true;
			completionAnalysis.UsingNamespacePrefix = value;
			completionAnalysis.Prefix = ((num2 >= 0) ? value.Substring(num2 + 1) : value);
		}
		string text2 = text.Substring(0, num);
		if (!completionAnalysis.IsUsingDirective && text2.EndsWith(".", StringComparison.Ordinal))
		{
			int num3 = text2.Length - 1;
			int num4 = num3;
			while (num4 > 0 && IsIdentifierChar(text2[num4 - 1]))
			{
				num4--;
			}
			completionAnalysis.Owner = text2.Substring(num4, num3 - num4);
			completionAnalysis.IsMemberCompletion = !string.IsNullOrEmpty(completionAnalysis.Owner);
		}
		cancellationToken.ThrowIfCancellationRequested();
		(string, string) tuple = FindCurrentClass(source, completionAnalysis.CaretOffset, cancellationToken);
		completionAnalysis.CurrentClassName = tuple.Item1;
		completionAnalysis.CurrentBaseClassName = (string.IsNullOrEmpty(tuple.Item2) ? "Node" : tuple.Item2);
		string nameFilter = (completionAnalysis.IsMemberCompletion ? completionAnalysis.Owner : completionAnalysis.Prefix);
		completionAnalysis.Symbols = ExtractSymbols(source, completionAnalysis.CaretOffset, nameFilter, cancellationToken);
		cancellationToken.ThrowIfCancellationRequested();
		completionAnalysis.Namespaces = XWCSharpCodeModel.GetNamespaces(source);
		completionAnalysis.KnownTypeNames = XWCSharpCodeModel.GetKnownTypeNames(source);
		return completionAnalysis;
	}

	private static void AddGlobalCompletions(CompletionAnalysis context)
	{
		string prefix = context.Prefix;
		bool flag = prefix.Length > 0;
		string[] keywords = Keywords;
		foreach (string text in keywords)
		{
			AddIfMatches(CodeEdit.CodeCompletionKind.Constant, text, GetKeywordInsert(text), CompletionColorKind.Keyword, prefix, context);
		}
		(string, string)[] snippets = Snippets;
		for (int i = 0; i < snippets.Length; i++)
		{
			(string, string) tuple = snippets[i];
			AddIfMatches(CodeEdit.CodeCompletionKind.PlainText, tuple.Item1, tuple.Item2, CompletionColorKind.Method, prefix, context);
		}
		snippets = GodotTypes;
		for (int i = 0; i < snippets.Length; i++)
		{
			(string, string) tuple2 = snippets[i];
			AddIfMatches(CodeEdit.CodeCompletionKind.Class, tuple2.Item1, tuple2.Item2, CompletionColorKind.Type, prefix, context);
		}
		if (flag)
		{
			AddKnownTypeCompletions(prefix, context);
			snippets = GlobalMethods;
			for (int i = 0; i < snippets.Length; i++)
			{
				(string, string) tuple3 = snippets[i];
				AddIfMatches(CodeEdit.CodeCompletionKind.Function, tuple3.Item1, tuple3.Item2, CompletionColorKind.Method, prefix, context);
			}
		}
		foreach (CompletionSymbol symbol in context.Symbols)
		{
			AddIfMatches(CodeEdit.CodeCompletionKind.Constant, FormatSymbol(symbol), symbol.Name, CompletionColorKind.Local, prefix, context);
		}
		string className = ((!string.IsNullOrEmpty(context.CurrentClassName)) ? context.CurrentClassName : context.CurrentBaseClassName);
		if (flag)
		{
			AddClassMemberCompletions(className, prefix, staticOnly: false, allowPrivate: true, allowProtected: true, context);
		}
	}

	private static void AddUsingNamespaceCompletions(CompletionAnalysis context)
	{
		string text = context.UsingNamespacePrefix ?? "";
		int num = text.LastIndexOf('.');
		string text2 = ((num >= 0) ? text.Substring(0, num) : "");
		string text3 = ((num >= 0) ? text.Substring(num + 1) : text);
		foreach (string @namespace in context.Namespaces)
		{
			if (!string.IsNullOrEmpty(@namespace) && (text2.Length <= 0 || @namespace.StartsWith(text2 + ".", StringComparison.Ordinal)) && (text.Length <= 0 || @namespace.StartsWith(text, StringComparison.OrdinalIgnoreCase) || GetNamespaceTail(@namespace).StartsWith(text3, StringComparison.OrdinalIgnoreCase)))
			{
				string insert = @namespace;
				if (text2.Length > 0 && @namespace.StartsWith(text2 + ".", StringComparison.Ordinal))
				{
					insert = @namespace.Substring(text2.Length + 1);
				}
				AddIfMatches(CodeEdit.CodeCompletionKind.Class, @namespace, insert, CompletionColorKind.Namespace, text3, context);
			}
		}
	}

	private static void AddKnownTypeCompletions(string prefix, CompletionAnalysis context)
	{
		if (prefix.Length < 2)
		{
			return;
		}
		foreach (string knownTypeName in context.KnownTypeNames)
		{
			AddIfMatches(CodeEdit.CodeCompletionKind.Class, knownTypeName, knownTypeName, CompletionColorKind.Type, prefix, context);
		}
	}

	private static void AddMemberCompletions(CompletionAnalysis context)
	{
		string owner = context.Owner;
		if (StaticMembers.TryGetValue(owner, out (string, string, CodeEdit.CodeCompletionKind)[] value))
		{
			(string, string, CodeEdit.CodeCompletionKind)[] array = value;
			for (int i = 0; i < array.Length; i++)
			{
				(string, string, CodeEdit.CodeCompletionKind) tuple = array[i];
				AddIfMatches(tuple.Item3, tuple.Item1, tuple.Item2, GetColorKindForCompletion(tuple.Item3), context.Prefix, context);
			}
		}
		bool isStaticOwner;
		string text = ResolveOwnerType(owner, context, context.Symbols, out isStaticOwner);
		if (!string.IsNullOrEmpty(text))
		{
			bool flag = owner == "this" || (owner != "base" && string.Equals(text, context.CurrentClassName, StringComparison.Ordinal));
			bool allowProtected = flag || owner == "base";
			AddClassMemberCompletions(text, context.Prefix, isStaticOwner, flag, allowProtected, context);
		}
	}

	private static string ResolveOwnerType(string owner, CompletionAnalysis context, List<CompletionSymbol> symbols, out bool isStaticOwner)
	{
		isStaticOwner = false;
		if (owner == "this")
		{
			if (string.IsNullOrEmpty(context.CurrentClassName))
			{
				return context.CurrentBaseClassName;
			}
			return context.CurrentClassName;
		}
		if (owner == "base")
		{
			return context.CurrentBaseClassName;
		}
		foreach (CompletionSymbol symbol in symbols)
		{
			if (symbol.Name == owner)
			{
				return symbol.Type;
			}
		}
		if (XWCSharpCodeModel.FindTypes(owner, context.UsingNamespaces, context.CurrentNamespace).Count > 0)
		{
			isStaticOwner = true;
			return owner;
		}
		if (context.KnownTypeNames.Contains(owner))
		{
			isStaticOwner = true;
			return owner;
		}
		return "";
	}

	private static void AddClassMemberCompletions(string className, string prefix, bool staticOnly, bool allowPrivate, bool allowProtected, CompletionAnalysis context)
	{
		if (!string.IsNullOrEmpty(className))
		{
			AddSourceMemberCompletions(className, prefix, staticOnly, allowPrivate, allowProtected, context);
			AddReflectedMemberCompletions(className, prefix, staticOnly, context);
		}
	}

	[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "The in-game script editor intentionally reflects public members of loaded editor/game types for best-effort C# completion.")]
	private static void AddReflectedMemberCompletions(string className, string prefix, bool staticOnly, CompletionAnalysis context)
	{
		List<Type> list = XWCSharpCodeModel.FindTypes(className, context?.UsingNamespaces, context?.CurrentNamespace ?? "");
		if (list.Count == 0)
		{
			return;
		}
		BindingFlags bindingAttr = (BindingFlags)(0x50 | (staticOnly ? 8 : 4));
		foreach (Type item in list)
		{
			MethodInfo[] methods = item.GetMethods(bindingAttr);
			foreach (MethodInfo methodInfo in methods)
			{
				context.CancellationToken.ThrowIfCancellationRequested();
				if (!methodInfo.IsSpecialName)
				{
					string name = methodInfo.Name;
					string insert = (methodInfo.IsGenericMethodDefinition ? (name + "<>()") : (name + "()"));
					AddIfMatches(CodeEdit.CodeCompletionKind.Function, FormatReflectedMethod(methodInfo), insert, CompletionColorKind.Method, prefix, context);
				}
			}
			PropertyInfo[] properties = item.GetProperties(bindingAttr);
			foreach (PropertyInfo propertyInfo in properties)
			{
				AddIfMatches(CodeEdit.CodeCompletionKind.Constant, FormatReflectedProperty(propertyInfo), propertyInfo.Name, CompletionColorKind.Property, prefix, context);
			}
			FieldInfo[] fields = item.GetFields(bindingAttr);
			foreach (FieldInfo fieldInfo in fields)
			{
				AddIfMatches(CodeEdit.CodeCompletionKind.Constant, FormatReflectedField(fieldInfo), fieldInfo.Name, CompletionColorKind.Constant, prefix, context);
			}
			EventInfo[] events = item.GetEvents(bindingAttr);
			foreach (EventInfo eventInfo in events)
			{
				AddIfMatches(CodeEdit.CodeCompletionKind.Constant, eventInfo.Name, eventInfo.Name, CompletionColorKind.Constant, prefix, context);
			}
		}
	}

	private static void AddSourceMemberCompletions(string className, string prefix, bool staticOnly, bool allowPrivate, bool allowProtected, CompletionAnalysis context)
	{
		AddSourceMemberCompletionsRecursive(className, prefix, staticOnly, inherited: false, allowPrivate, allowProtected, XWCSharpProjectIndex.GetSnapshot(), context, new HashSet<string>(StringComparer.Ordinal));
	}

	private static void AddSourceMemberCompletionsRecursive(string className, string prefix, bool staticOnly, bool inherited, bool allowPrivate, bool allowProtected, XWCSharpProjectIndex.Snapshot snapshot, CompletionAnalysis context, HashSet<string> visited)
	{
		if (string.IsNullOrEmpty(className) || !visited.Add(className))
		{
			return;
		}
		Regex regex = new Regex("(?m)^\\s*(?:(?:\\[[^\\]\\r\\n]+\\]\\s*)+)?(?<mods>(?:(?:public|private|protected|internal|static|virtual|override|abstract|sealed|async|new|partial|extern|unsafe)\\s+)*)(?<type>[A-Za-z_][A-Za-z0-9_<>.,\\[\\]? ]*)\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\\s*(?<generic><[^>{}()]*>)?\\s*\\((?<args>[^)]*)\\)");
		Regex regex2 = new Regex("(?m)^\\s*(?:(?:\\[[^\\]\\r\\n]+\\]\\s*)+)?(?<mods>(?:(?:public|private|protected|internal|static|readonly|const|virtual|override|abstract|sealed|new|required|volatile)\\s+)*)(?<type>[A-Za-z_][A-Za-z0-9_<>.,\\[\\]? ]*)\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\\s*(?=\\{|=|;)");
		Regex regex3 = new Regex("(?m)^\\s*\\[Signal(?:Attribute)?\\]\\s*public\\s+delegate\\s+void\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\\s*\\((?<args>[^)]*)\\)");
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		List<string> list = new List<string> { context.Source ?? "" };
		if (snapshot.TypeDocuments.TryGetValue(className, out var value))
		{
			string[] array = value;
			foreach (string text in array)
			{
				if (!PathsEqual(text, context.DocumentPath) && snapshot.Documents.TryGetValue(text, out var value2) && (!string.IsNullOrEmpty(context.DocumentPath) || !string.Equals(value2, context.Source, StringComparison.Ordinal)))
				{
					list.Add(value2 ?? "");
				}
			}
		}
		foreach (string item in list)
		{
			context.CancellationToken.ThrowIfCancellationRequested();
			if (string.IsNullOrEmpty(item))
			{
				continue;
			}
			string text2 = FindDeclaredBaseClass(item, className, context.CancellationToken);
			if (!string.IsNullOrEmpty(text2) && text2 != className)
			{
				hashSet.Add(text2);
			}
			foreach (string item2 in ExtractTypeBodyMasks(item, className, context.CancellationToken))
			{
				string input = FilterTopLevelLinesContaining(item2, prefix, context.CancellationToken);
				foreach (Match item3 in regex.Matches(input))
				{
					context.CancellationToken.ThrowIfCancellationRequested();
					string value3 = item3.Groups["mods"].Value;
					bool flag = value3.Contains("static", StringComparison.Ordinal);
					if ((!staticOnly || flag) && (!(!allowPrivate || inherited) || !value3.Contains("private", StringComparison.Ordinal)) && (allowProtected || !value3.Contains("protected", StringComparison.Ordinal)))
					{
						string value4 = item3.Groups["name"].Value;
						string value5 = Regex.Replace(item3.Groups["args"].Value, "\\s+", " ").Trim();
						string text3 = item3.Groups["type"].Value.Trim();
						if (!text3.StartsWith("delegate ", StringComparison.Ordinal))
						{
							string display = $"{value4}({value5}) : {text3}";
							string insert = (item3.Groups["generic"].Success ? (value4 + "<>()") : (value4 + "()"));
							AddIfMatches(CodeEdit.CodeCompletionKind.Function, display, insert, CompletionColorKind.Method, prefix, context);
						}
					}
				}
				foreach (Match item4 in regex2.Matches(input))
				{
					context.CancellationToken.ThrowIfCancellationRequested();
					string value6 = item4.Groups["mods"].Value;
					bool flag2 = value6.Contains("static", StringComparison.Ordinal) || value6.Contains("const", StringComparison.Ordinal);
					if ((!staticOnly || flag2) && (!(!allowPrivate || inherited) || !value6.Contains("private", StringComparison.Ordinal)) && (allowProtected || !value6.Contains("protected", StringComparison.Ordinal)))
					{
						string value7 = item4.Groups["name"].Value;
						string text4 = item4.Groups["type"].Value.Trim();
						AddIfMatches(CodeEdit.CodeCompletionKind.Constant, value7 + " : " + text4, value7, CompletionColorKind.Property, prefix, context);
					}
				}
				foreach (Match item5 in regex3.Matches(input))
				{
					context.CancellationToken.ThrowIfCancellationRequested();
					if (!staticOnly)
					{
						string value8 = item5.Groups["name"].Value;
						string text5 = (value8.EndsWith("EventHandler", StringComparison.Ordinal) ? value8.Substring(0, value8.Length - "EventHandler".Length) : value8);
						string text6 = Regex.Replace(item5.Groups["args"].Value, "\\s+", " ").Trim();
						AddIfMatches(CodeEdit.CodeCompletionKind.Constant, text5 + "(" + text6 + ") : signal", text5, CompletionColorKind.Property, prefix, context);
					}
				}
			}
		}
		foreach (string item6 in hashSet)
		{
			context.CancellationToken.ThrowIfCancellationRequested();
			AddSourceMemberCompletionsRecursive(item6, prefix, staticOnly, inherited: true, allowPrivate: false, allowProtected, snapshot, context, visited);
		}
	}

	private static string FindDeclaredBaseClass(string source, string className, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(className))
		{
			return "";
		}
		string input = MaskCommentsAndStrings(source, cancellationToken);
		string pattern = "\\b(?:class|struct|interface|record(?:\\s+(?:class|struct))?)\\s+" + Regex.Escape(className) + "(?:\\s*<[^>{}]*>)?\\s*:\\s*(?<base>[A-Za-z_][A-Za-z0-9_.]*(?:\\s*<[^>{}]*>)?)";
		Match match = Regex.Match(input, pattern);
		if (!match.Success)
		{
			return "";
		}
		return StripQualifiedTypeName(CleanTypeName(match.Groups["base"].Value));
	}

	private static List<CompletionSymbol> ExtractSymbols(string source, int caretOffset, string nameFilter, CancellationToken cancellationToken)
	{
		if (source == null)
		{
			source = "";
		}
		caretOffset = Math.Clamp(caretOffset, 0, source.Length);
		string source2 = FilterLinesContaining(source.Substring(0, caretOffset), nameFilter, cancellationToken);
		List<CompletionSymbol> list = new List<CompletionSymbol>();
		HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
		AddTypedDeclarations(source2, list, seen, cancellationToken);
		AddVarInferredDeclarations(source2, list, seen, cancellationToken);
		return list;
	}

	private static string FilterLinesContaining(string source, string value, CancellationToken cancellationToken)
	{
		if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(value))
		{
			return source ?? "";
		}
		StringBuilder stringBuilder = new StringBuilder(Math.Min(source.Length, 4096));
		int num = 0;
		int num2 = 0;
		while (num < source.Length)
		{
			if ((num2++ & 0xFF) == 0)
			{
				cancellationToken.ThrowIfCancellationRequested();
			}
			int num3 = source.IndexOf('\n', num);
			int num4 = ((num3 >= 0) ? num3 : source.Length) - num;
			if (num4 >= value.Length && source.IndexOf(value, num, num4, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				stringBuilder.Append(source, num, num4).Append('\n');
			}
			if (num3 < 0)
			{
				break;
			}
			num = num3 + 1;
		}
		return stringBuilder.ToString();
	}

	private static List<string> ExtractTypeBodyMasks(string source, string className, CancellationToken cancellationToken)
	{
		List<string> list = new List<string>();
		if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(className))
		{
			return list;
		}
		string text = MaskCommentsAndStrings(source, cancellationToken);
		string pattern = "\\b(?:class|struct|interface|record(?:\\s+(?:class|struct))?)\\s+" + Regex.Escape(className) + "\\b[^;{}]*\\{";
		foreach (Match item in Regex.Matches(text, pattern))
		{
			cancellationToken.ThrowIfCancellationRequested();
			int num = item.Index + item.Value.LastIndexOf('{');
			int num2 = FindMatchingBrace(text, num, cancellationToken);
			if (num2 > num)
			{
				list.Add(text.Substring(num + 1, num2 - num - 1));
			}
		}
		return list;
	}

	private static string FilterTopLevelLinesContaining(string bodyMask, string value, CancellationToken cancellationToken)
	{
		StringBuilder stringBuilder = new StringBuilder(Math.Min(bodyMask?.Length ?? 0, 4096));
		if (string.IsNullOrEmpty(bodyMask))
		{
			return "";
		}
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		while (num2 < bodyMask.Length)
		{
			if ((num3++ & 0xFF) == 0)
			{
				cancellationToken.ThrowIfCancellationRequested();
			}
			int num4 = bodyMask.IndexOf('\n', num2);
			int num5 = ((num4 >= 0) ? num4 : bodyMask.Length);
			int num6 = num;
			for (int i = num2; i < num5; i++)
			{
				if (bodyMask[i] == '{')
				{
					num++;
				}
				else if (bodyMask[i] == '}')
				{
					num = Math.Max(0, num - 1);
				}
			}
			int num7 = num5 - num2;
			bool flag = string.IsNullOrEmpty(value) || (num7 >= value.Length && bodyMask.IndexOf(value, num2, num7, StringComparison.OrdinalIgnoreCase) >= 0);
			if (num6 == 0 && flag)
			{
				stringBuilder.Append(bodyMask, num2, num7).Append('\n');
			}
			if (num4 < 0)
			{
				break;
			}
			num2 = num4 + 1;
		}
		return stringBuilder.ToString();
	}

	private static string MaskCommentsAndStrings(string source, CancellationToken cancellationToken)
	{
		char[] array = (source ?? "").ToCharArray();
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		bool flag4 = false;
		bool flag5 = false;
		for (int i = 0; i < array.Length; i++)
		{
			if ((i & 0x7FF) == 0)
			{
				cancellationToken.ThrowIfCancellationRequested();
			}
			char c = array[i];
			char c2 = ((i + 1 < array.Length) ? array[i + 1] : '\0');
			if (flag)
			{
				if (c == '\n')
				{
					flag = false;
				}
				else
				{
					array[i] = ' ';
				}
			}
			else if (flag2)
			{
				array[i] = ((c == '\n') ? '\n' : ' ');
				if (c == '*' && c2 == '/')
				{
					if (i + 1 < array.Length)
					{
						array[++i] = ' ';
					}
					flag2 = false;
				}
			}
			else if (flag3 || flag5 || flag4)
			{
				array[i] = ((c == '\n') ? '\n' : ' ');
				if (flag4 && c == '"' && c2 == '"')
				{
					if (i + 1 < array.Length)
					{
						array[++i] = ' ';
					}
				}
				else if (!flag4 && c == '\\')
				{
					if (i + 1 < array.Length)
					{
						array[++i] = ' ';
					}
				}
				else if (flag3 && c == '"')
				{
					flag3 = false;
				}
				else if (flag4 && c == '"')
				{
					flag4 = false;
				}
				else if (flag5 && c == '\'')
				{
					flag5 = false;
				}
			}
			else if (c == '/' && c2 == '/')
			{
				array[i] = (array[++i] = ' ');
				flag = true;
			}
			else if (c == '/' && c2 == '*')
			{
				array[i] = (array[++i] = ' ');
				flag2 = true;
			}
			else if (c == '@' && c2 == '"')
			{
				array[i] = (array[++i] = ' ');
				flag4 = true;
			}
			else
			{
				switch (c)
				{
				case '"':
					array[i] = ' ';
					flag3 = true;
					break;
				case '\'':
					array[i] = ' ';
					flag5 = true;
					break;
				}
			}
		}
		return new string(array);
	}

	private static int FindMatchingBrace(string source, int openBrace, CancellationToken cancellationToken)
	{
		int num = 0;
		for (int i = openBrace; i < source.Length; i++)
		{
			if ((i & 0x7FF) == 0)
			{
				cancellationToken.ThrowIfCancellationRequested();
			}
			if (source[i] == '{')
			{
				num++;
			}
			else if (source[i] == '}' && --num == 0)
			{
				return i;
			}
		}
		return -1;
	}

	private static bool PathsEqual(string left, string right)
	{
		if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
		{
			return false;
		}
		return string.Equals(left.Replace('\\', '/').TrimEnd('/'), right.Replace('\\', '/').TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
	}

	private static void AddTypedDeclarations(string source, List<CompletionSymbol> symbols, HashSet<string> seen, CancellationToken cancellationToken)
	{
		foreach (Match item in new Regex("(?m)(?:^|[;{(,])\\s*(?:(?:public|private|protected|internal|static|readonly|const|new|override|virtual|required|volatile)\\s+)*(?<type>[A-Za-z_][A-Za-z0-9_<>.,\\[\\]?]*)\\s+(?<name>@?[A-Za-z_][A-Za-z0-9_]*)\\s*(?=[=;,\\)])").Matches(source))
		{
			cancellationToken.ThrowIfCancellationRequested();
			string type = CleanTypeName(item.Groups["type"].Value);
			string name = item.Groups["name"].Value.TrimStart('@');
			if (!ShouldSkipType(type) && !ShouldSkipSymbolName(name))
			{
				AddSymbol(symbols, seen, name, type);
			}
		}
	}

	private static void AddVarInferredDeclarations(string source, List<CompletionSymbol> symbols, HashSet<string> seen, CancellationToken cancellationToken)
	{
		foreach (Match item in new Regex("\\bvar\\s+(?<name>@?[A-Za-z_][A-Za-z0-9_]*)\\s*=\\s*new\\s+(?<type>[A-Za-z_][A-Za-z0-9_.]*)").Matches(source))
		{
			cancellationToken.ThrowIfCancellationRequested();
			AddSymbol(symbols, seen, item.Groups["name"].Value.TrimStart('@'), StripQualifiedTypeName(item.Groups["type"].Value));
		}
		foreach (Match item2 in new Regex("\\bvar\\s+(?<name>@?[A-Za-z_][A-Za-z0-9_]*)\\s*=\\s*GetNode(?:OrNull)?<(?<type>[A-Za-z_][A-Za-z0-9_.]*)>").Matches(source))
		{
			cancellationToken.ThrowIfCancellationRequested();
			AddSymbol(symbols, seen, item2.Groups["name"].Value.TrimStart('@'), StripQualifiedTypeName(item2.Groups["type"].Value));
		}
	}

	private static void AddSymbol(List<CompletionSymbol> symbols, HashSet<string> seen, string name, string type)
	{
		if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(type) && seen.Add(name))
		{
			symbols.Add(new CompletionSymbol
			{
				Name = name,
				Type = type
			});
		}
	}

	private static (string className, string baseClass) FindCurrentClass(string source, int caretOffset, CancellationToken cancellationToken)
	{
		if (source == null)
		{
			source = "";
		}
		caretOffset = Math.Clamp(caretOffset, 0, source.Length);
		Regex regex = new Regex("(?m)^\\s*(?:(?:public|internal|private|protected|static|abstract|sealed|partial|unsafe|new)\\s+)*class\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)(?:\\s*:\\s*(?<base>[^{\\r\\n]+))?");
		string item = "";
		string text = "Node";
		foreach (Match item2 in regex.Matches(source))
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (item2.Index > caretOffset)
			{
				break;
			}
			item = item2.Groups["name"].Value;
			text = CleanBaseClass(item2.Groups["base"].Value);
			if (string.IsNullOrEmpty(text))
			{
				text = "Node";
			}
		}
		return (className: item, baseClass: text);
	}

	private static void AddIfMatches(CodeEdit.CodeCompletionKind kind, string display, string insert, CompletionColorKind colorKind, string prefix, CompletionAnalysis context)
	{
		context.CancellationToken.ThrowIfCancellationRequested();
		if (!string.IsNullOrEmpty(display) && !string.IsNullOrEmpty(insert) && (MatchesPrefix(display, prefix) || MatchesPrefix(insert, prefix)))
		{
			string item = kind.ToString() + ":" + insert;
			if (context.AddedOptions.Add(item))
			{
				context.Options.Add(new CompletionOptionData
				{
					Kind = (int)kind,
					Display = display,
					Insert = insert,
					ColorKind = colorKind
				});
			}
		}
	}

	private static int GetCompletionPriority(CompletionColorKind colorKind)
	{
		return colorKind switch
		{
			CompletionColorKind.Local => 0, 
			CompletionColorKind.Method => 1, 
			CompletionColorKind.Property => 2, 
			CompletionColorKind.Keyword => 3, 
			CompletionColorKind.Type => 4, 
			CompletionColorKind.Constant => 5, 
			CompletionColorKind.Namespace => 6, 
			_ => 10, 
		};
	}

	private static bool MatchesPrefix(string text, string prefix)
	{
		if (!string.IsNullOrEmpty(prefix))
		{
			if (!string.IsNullOrEmpty(text))
			{
				return text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
			}
			return false;
		}
		return true;
	}

	private static string GetKeywordInsert(string keyword)
	{
		switch (keyword)
		{
		case "break":
			return "break;";
		case "continue":
			return "continue;";
		case "class":
		case "const":
		case "event":
		case "float":
		case "sbyte":
		case "short":
		case "throw":
		case "ulong":
		case "using":
		case "while":
		case "yield":
		case "override":
		case "readonly":
		case "volatile":
		case "delegate":
		case "internal":
		case "bool":
		case "byte":
		case "char":
		case "enum":
		case "long":
		case "uint":
		case "void":
		case "private":
		case "virtual":
		case "decimal":
		case "foreach":
		case "double":
		case "object":
		case "public":
		case "return":
		case "sealed":
		case "static":
		case "string":
		case "struct":
		case "switch":
		case "unsafe":
		case "ushort":
		case "for":
		case "int":
		case "ref":
		case "try":
		case "var":
		case "interface":
		case "namespace":
		case "protected":
		case "if":
			return keyword + " ";
		default:
			return keyword;
		}
	}

	private static string FormatSymbol(CompletionSymbol symbol)
	{
		if (!string.IsNullOrEmpty(symbol.Type))
		{
			return symbol.Name + " : " + symbol.Type;
		}
		return symbol.Name;
	}

	private static string FormatReflectedMethod(MethodInfo method)
	{
		ParameterInfo[] parameters = method.GetParameters();
		List<string> list = new List<string>();
		ParameterInfo[] array = parameters;
		foreach (ParameterInfo parameterInfo in array)
		{
			list.Add(GetFriendlyTypeName(parameterInfo.ParameterType) + " " + parameterInfo.Name);
		}
		return $"{method.Name}({string.Join(", ", list)}) : {GetFriendlyTypeName(method.ReturnType)}";
	}

	private static string FormatReflectedProperty(PropertyInfo property)
	{
		return property.Name + " : " + GetFriendlyTypeName(property.PropertyType);
	}

	private static string FormatReflectedField(FieldInfo field)
	{
		return field.Name + " : " + GetFriendlyTypeName(field.FieldType);
	}

	private static string GetFriendlyTypeName(Type type)
	{
		if (type == null)
		{
			return "";
		}
		if (type == typeof(void))
		{
			return "void";
		}
		if (!type.IsGenericType)
		{
			return XWCSharpCodeModel.NormalizeSimpleTypeName(type.Name);
		}
		string text = XWCSharpCodeModel.NormalizeSimpleTypeName(type.Name);
		Type[] genericArguments = type.GetGenericArguments();
		List<string> list = new List<string>();
		Type[] array = genericArguments;
		foreach (Type type2 in array)
		{
			list.Add(GetFriendlyTypeName(type2));
		}
		return text + "<" + string.Join(", ", list) + ">";
	}

	private static string CleanBaseClass(string basePart)
	{
		if (string.IsNullOrWhiteSpace(basePart))
		{
			return "";
		}
		return CleanTypeName(basePart.Split(',')[0].Trim());
	}

	private static string CleanTypeName(string typeName)
	{
		string text = (typeName ?? "").Trim();
		text = text.Replace("global::", "");
		text = Regex.Replace(text, "\\b(public|private|protected|internal|static|readonly|const|required|volatile|partial|sealed|abstract|new|override|virtual)\\b\\s*", "");
		if (text.EndsWith("?", StringComparison.Ordinal))
		{
			text = text.Substring(0, text.Length - 1);
		}
		return StripQualifiedTypeName(text);
	}

	private static string StripQualifiedTypeName(string type)
	{
		type = (type ?? "").Trim();
		int num = type.IndexOf('<');
		if (num >= 0)
		{
			type = type.Substring(0, num);
		}
		type = type.TrimEnd('[', ']');
		int num2 = type.LastIndexOf('.');
		if (num2 < 0)
		{
			return type;
		}
		return type.Substring(num2 + 1);
	}

	private static bool ShouldSkipType(string type)
	{
		bool flag = string.IsNullOrEmpty(type) || type == "var";
		if (!flag)
		{
			bool flag2;
			switch (type)
			{
			case "for":
			case "new":
			case "catch":
			case "using":
			case "while":
			case "return":
			case "switch":
			case "if":
			case "foreach":
				flag2 = true;
				break;
			default:
				flag2 = false;
				break;
			}
			flag = flag2;
		}
		return flag;
	}

	private static bool ShouldSkipSymbolName(string name)
	{
		bool flag = string.IsNullOrEmpty(name);
		if (!flag)
		{
			bool flag2;
			switch (name)
			{
			case "get":
			case "set":
			case "add":
			case "remove":
				flag2 = true;
				break;
			default:
				flag2 = false;
				break;
			}
			flag = flag2;
		}
		return flag;
	}

	private static string GetNamespaceTail(string namespaceName)
	{
		if (string.IsNullOrEmpty(namespaceName))
		{
			return "";
		}
		int num = namespaceName.LastIndexOf('.');
		if (num < 0)
		{
			return namespaceName;
		}
		return namespaceName.Substring(num + 1);
	}

	private static bool IsInCommentOrString(string source, int caretOffset, CancellationToken cancellationToken)
	{
		if (source == null)
		{
			source = "";
		}
		caretOffset = Math.Clamp(caretOffset, 0, source.Length);
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		bool flag4 = false;
		bool flag5 = false;
		for (int i = 0; i < caretOffset; i++)
		{
			if ((i & 0x7FF) == 0)
			{
				cancellationToken.ThrowIfCancellationRequested();
			}
			char c = source[i];
			char c2 = ((i + 1 < caretOffset) ? source[i + 1] : '\0');
			if (flag)
			{
				if (c == '\n')
				{
					flag = false;
				}
			}
			else if (flag2)
			{
				if (c == '*' && c2 == '/')
				{
					flag2 = false;
					i++;
				}
			}
			else if (flag3)
			{
				switch (c)
				{
				case '\\':
					i++;
					break;
				case '"':
					flag3 = false;
					break;
				}
			}
			else if (flag5)
			{
				if (c == '"' && c2 == '"')
				{
					i++;
				}
				else if (c == '"')
				{
					flag5 = false;
				}
			}
			else if (flag4)
			{
				switch (c)
				{
				case '\\':
					i++;
					break;
				case '\'':
					flag4 = false;
					break;
				}
			}
			else if (c == '/' && c2 == '/')
			{
				flag = true;
				i++;
			}
			else if (c == '/' && c2 == '*')
			{
				flag2 = true;
				i++;
			}
			else if (c == '@' && c2 == '"')
			{
				flag5 = true;
				i++;
			}
			else
			{
				switch (c)
				{
				case '"':
					flag3 = true;
					break;
				case '\'':
					flag4 = true;
					break;
				}
			}
		}
		return flag || flag2 || flag3 || flag5 || flag4;
	}

	private static bool IsIdentifierChar(char c)
	{
		if (!char.IsLetterOrDigit(c) && c != '_')
		{
			return c == '@';
		}
		return true;
	}

	private static int GetCaretOffset(string source, int line, int column)
	{
		int num = 0;
		for (int i = 0; i < line; i++)
		{
			if (num >= source.Length)
			{
				break;
			}
			int num2 = source.IndexOf('\n', num);
			if (num2 < 0)
			{
				num = source.Length;
				break;
			}
			num = num2 + 1;
		}
		return Math.Clamp(num + column, 0, source.Length);
	}

	private static string GetLine(string source, int line)
	{
		if (line < 0 || string.IsNullOrEmpty(source))
		{
			return "";
		}
		int caretOffset = GetCaretOffset(source, line, 0);
		if (caretOffset >= source.Length)
		{
			return "";
		}
		int num = source.IndexOf('\n', caretOffset);
		if (num < 0)
		{
			num = source.Length;
		}
		if (num > caretOffset && source[num - 1] == '\r')
		{
			num--;
		}
		return source.Substring(caretOffset, num - caretOffset);
	}

	private static CompletionColorKind GetColorKindForCompletion(CodeEdit.CodeCompletionKind kind)
	{
		if (kind != CodeEdit.CodeCompletionKind.Function)
		{
			return CompletionColorKind.Constant;
		}
		return CompletionColorKind.Method;
	}

	private static Color ResolveColor(CompletionColorKind colorKind)
	{
		return colorKind switch
		{
			CompletionColorKind.Keyword => KeywordColor, 
			CompletionColorKind.Type => TypeColor, 
			CompletionColorKind.Method => MethodColor, 
			CompletionColorKind.Property => PropertyColor, 
			CompletionColorKind.Constant => ConstantColor, 
			CompletionColorKind.Local => LocalColor, 
			CompletionColorKind.Namespace => NamespaceColor, 
			_ => ConstantColor, 
		};
	}
}
