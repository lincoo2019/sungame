using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Godot;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace PVZHE.ModEditor.ScriptEditor;

public partial class XWCSharpErrorChecker : XWCodeErrorChecker
{
	private enum State
	{
		Normal,
		InString,
		InVerbatimString,
		InInterpolatedString,
		InChar,
		InLineComment,
		InBlockComment
	}

	private readonly struct TypeReferenceInfo(string typeName, int index)
	{
		public readonly string TypeName = typeName;

		public readonly int Index = index;
	}

	private readonly struct TypeBodyInfo(int bodyStart, int bodyEnd)
	{
		public readonly int BodyStart = bodyStart;

		public readonly int BodyEnd = bodyEnd;
	}

	protected override void DoCheck(string code)
	{
		ThrowIfCancellationRequested();
		if (RunRoslynSyntaxCheck(code))
		{
			ThrowIfCancellationRequested();
			if (_errors.Count < 256)
			{
				RunDeclarationChecks(code);
				ThrowIfCancellationRequested();
				if (_errors.Count < 256)
				{
					RunMissingUsingChecks(code);
				}
			}
		}
		else
		{
			ThrowIfCancellationRequested();
			RunFallbackSyntaxCheck(code);
		}
	}

	private bool RunRoslynSyntaxCheck(string code)
	{
		try
		{
			CSharpParseOptions options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest).WithDocumentationMode(DocumentationMode.Parse).WithKind(SourceCodeKind.Regular);
			foreach (Diagnostic diagnostic in CSharpSyntaxTree.ParseText(code ?? "", options, "", null, base.CancellationToken).GetDiagnostics(base.CancellationToken))
			{
				ThrowIfCancellationRequested();
				if (_errors.Count < 256)
				{
					if (diagnostic.Severity == DiagnosticSeverity.Error || diagnostic.Severity == DiagnosticSeverity.Warning)
					{
						FileLinePositionSpan lineSpan = diagnostic.Location.GetLineSpan();
						int line = lineSpan.StartLinePosition.Line;
						int character = lineSpan.StartLinePosition.Character;
						AddError(line, character, diagnostic.Id + ": " + diagnostic.GetMessage(), (diagnostic.Severity == DiagnosticSeverity.Warning) ? Severity.Warning : Severity.Error, diagnostic.Id);
					}
					continue;
				}
				break;
			}
			return true;
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex2)
		{
			AddError(0, 0, "Roslyn 语法检测失败，已使用基础检测: " + ex2.Message, Severity.Warning, "ROSLYN_PARSE_FAILED");
			return false;
		}
	}

	private void RunFallbackSyntaxCheck(string code)
	{
		Stack<(int, int)> stack = new Stack<(int, int)>();
		Stack<(int, int)> stack2 = new Stack<(int, int)>();
		Stack<(int, int)> stack3 = new Stack<(int, int)>();
		State state = State.Normal;
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < code.Length; i++)
		{
			if ((i & 0x3FF) == 0)
			{
				ThrowIfCancellationRequested();
			}
			char c = code[i];
			char c2 = ((i + 1 < code.Length) ? code[i + 1] : '\0');
			switch (state)
			{
			case State.Normal:
				if (c == '/' && c2 == '/')
				{
					state = State.InLineComment;
					i++;
					num2++;
					continue;
				}
				if (c == '/' && c2 == '*')
				{
					state = State.InBlockComment;
					i++;
					num2++;
					continue;
				}
				if (c == '@' && c2 == '"')
				{
					state = State.InVerbatimString;
					i++;
					num2++;
					continue;
				}
				if (c == '$' && c2 == '"')
				{
					state = State.InInterpolatedString;
					i++;
					num2++;
					continue;
				}
				switch (c)
				{
				case '"':
					state = State.InString;
					continue;
				case '\'':
					state = State.InChar;
					continue;
				case '{':
					stack.Push((num, num2));
					break;
				case '}':
					if (stack.Count > 0)
					{
						stack.Pop();
					}
					else
					{
						AddError(num, num2, "多余的闭合花括号 '}'", Severity.Error, "EXTRA_BRACE");
					}
					break;
				case '[':
					stack2.Push((num, num2));
					break;
				case ']':
					if (stack2.Count > 0)
					{
						stack2.Pop();
					}
					else
					{
						AddError(num, num2, "多余的闭合方括号 ']'", Severity.Error, "EXTRA_BRACKET");
					}
					break;
				case '(':
					stack3.Push((num, num2));
					break;
				case ')':
					if (stack3.Count > 0)
					{
						stack3.Pop();
					}
					else
					{
						AddError(num, num2, "多余的闭合圆括号 ')'", Severity.Error, "EXTRA_PAREN");
					}
					break;
				}
				break;
			case State.InString:
				switch (c)
				{
				case '\\':
					i++;
					num2++;
					break;
				case '"':
					state = State.Normal;
					break;
				}
				break;
			case State.InVerbatimString:
				if (c == '"' && c2 == '"')
				{
					i++;
					num2++;
				}
				else if (c == '"')
				{
					state = State.Normal;
				}
				break;
			case State.InInterpolatedString:
				switch (c)
				{
				case '\\':
					i++;
					num2++;
					break;
				case '"':
					state = State.Normal;
					break;
				}
				break;
			case State.InChar:
				switch (c)
				{
				case '\\':
					i++;
					num2++;
					break;
				case '\'':
					state = State.Normal;
					break;
				}
				break;
			case State.InLineComment:
				if (c == '\n')
				{
					state = State.Normal;
				}
				break;
			case State.InBlockComment:
				if (c == '*' && c2 == '/')
				{
					state = State.Normal;
					i++;
					num2++;
					continue;
				}
				break;
			}
			if (c == '\n')
			{
				num++;
				num2 = 0;
			}
			else
			{
				num2++;
			}
		}
		switch (state)
		{
		case State.InString:
		case State.InInterpolatedString:
			AddError(num, num2, "未闭合的字符串", Severity.Error, "UNCLOSED_STRING");
			break;
		case State.InVerbatimString:
			AddError(num, num2, "未闭合的逐字字符串", Severity.Error, "UNCLOSED_STRING");
			break;
		case State.InChar:
			AddError(num, num2, "未闭合的字符字面量", Severity.Error, "UNCLOSED_CHAR");
			break;
		case State.InBlockComment:
			AddError(num, num2, "未闭合的块注释", Severity.Error, "UNCLOSED_COMMENT");
			break;
		}
		foreach (var (line, column) in stack)
		{
			AddError(line, column, "未闭合的花括号 '{'", Severity.Error, "UNCLOSED_BRACE");
		}
		foreach (var (line2, column2) in stack2)
		{
			AddError(line2, column2, "未闭合的方括号 '['", Severity.Error, "UNCLOSED_BRACKET");
		}
		foreach (var (line3, column3) in stack3)
		{
			AddError(line3, column3, "未闭合的圆括号 '('", Severity.Error, "UNCLOSED_PAREN");
		}
		RunLineChecks(code);
		RunDeclarationChecks(code);
		RunMissingUsingChecks(code);
	}

	private void RunLineChecks(string code)
	{
		string[] array = code.Replace("\r\n", "\n").Split('\n');
		bool flag = false;
		for (int i = 0; i < array.Length; i++)
		{
			if ((i & 0xFF) == 0)
			{
				ThrowIfCancellationRequested();
			}
			string text = array[i];
			string text2 = StripLineComment(text).Trim();
			if (text2.Length != 0)
			{
				if (Regex.IsMatch(text2, "\\b(class|struct|interface|enum)\\s+[A-Za-z_][A-Za-z0-9_]*"))
				{
					flag = true;
				}
				if (text2.StartsWith("using ") && flag)
				{
					AddError(i, text.IndexOf("using", StringComparison.Ordinal), "using 指令必须放在命名空间或类型声明之前", Severity.Error, "USING_AFTER_TYPE");
				}
				if (text2.StartsWith("#") && !IsKnownPreprocessorDirective(text2))
				{
					AddError(i, text.IndexOf('#'), "未知或非法的 C# 预处理指令", Severity.Error, "INVALID_PREPROCESSOR");
				}
				if (text2.StartsWith("using ") && !text2.EndsWith(";"))
				{
					AddError(i, text.Length, "using 指令缺少分号", Severity.Error, "MISSING_SEMICOLON");
				}
				if (LooksLikeStatementMissingSemicolon(text2))
				{
					AddError(i, text.Length, "语句可能缺少分号", Severity.Error, "MISSING_SEMICOLON");
				}
			}
		}
	}

	private void RunDeclarationChecks(string code)
	{
		string text = StripCommentsAndStrings(code);
		List<int> lineStarts = BuildLineStarts(text);
		HashSet<string> hashSet = new HashSet<string>();
		foreach (Match item2 in Regex.Matches(text, "\\b(class|struct|interface|enum)\\s+([A-Za-z_][A-Za-z0-9_]*)"))
		{
			ThrowIfCancellationRequested();
			if (_errors.Count >= 256)
			{
				break;
			}
			string value = item2.Groups[2].Value;
			if (!hashSet.Add(value))
			{
				(int, int) lineColumn = GetLineColumn(lineStarts, item2.Index);
				AddError(lineColumn.Item1, lineColumn.Item2, "重复的类型声明: " + value, Severity.Error, "DUPLICATE_TYPE");
			}
		}
		if (_errors.Count >= 256)
		{
			return;
		}
		foreach (TypeBodyInfo item3 in FindTypeBodies(text))
		{
			ThrowIfCancellationRequested();
			HashSet<string> hashSet2 = new HashSet<string>();
			string text2 = text.Substring(item3.BodyStart + 1, item3.BodyEnd - item3.BodyStart - 1);
			int i = 0;
			int num = 0;
			foreach (Match item4 in Regex.Matches(text2, "(?m)^\\s*(?:public|private|protected|internal)(?:\\s+(?:protected|internal))?\\s+(?:(?:static|override|virtual|async|sealed|new|partial|extern|unsafe)\\s+)*[A-Za-z_][A-Za-z0-9_<>.,\\[\\]? ]*\\s+([A-Za-z_][A-Za-z0-9_]*)\\s*(?:<[^>{}()]*>)?\\s*\\(([^)]*)\\)"))
			{
				ThrowIfCancellationRequested();
				for (; i < item4.Index; i++)
				{
					if (text2[i] == '{')
					{
						num++;
					}
					else if (text2[i] == '}')
					{
						num = Math.Max(0, num - 1);
					}
				}
				if (num == 0)
				{
					string value2 = item4.Groups[1].Value;
					string item = BuildMethodSignatureKey(value2, item4.Groups[2].Value);
					if (!hashSet2.Add(item))
					{
						(int, int) lineColumn2 = GetLineColumn(lineStarts, item3.BodyStart + 1 + item4.Index);
						AddError(lineColumn2.Item1, lineColumn2.Item2, "重复的方法声明: " + value2, Severity.Error, "DUPLICATE_METHOD");
					}
				}
			}
		}
	}

	private void RunMissingUsingChecks(string code)
	{
		if (_errors.Count >= 256)
		{
			return;
		}
		string text = StripCommentsAndStrings(code ?? "");
		List<int> lineStarts = BuildLineStarts(text);
		HashSet<string> usingNamespaces = XWCSharpCodeModel.ExtractUsingNamespaces(text);
		string currentNamespace = XWCSharpCodeModel.ExtractCurrentNamespace(text);
		HashSet<string> declaredTypes = CollectDeclaredTypes(text);
		HashSet<string> typeParameters = CollectTypeParameters(text);
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		foreach (TypeReferenceInfo item2 in CollectPotentialTypeReferences(text))
		{
			ThrowIfCancellationRequested();
			if (_errors.Count >= 256)
			{
				break;
			}
			string text2 = XWCSharpCodeModel.NormalizeSimpleTypeName(item2.TypeName);
			if (ShouldSkipTypeReference(text2, declaredTypes, typeParameters))
			{
				continue;
			}
			string item = text2 + "@" + item2.Index;
			if (!hashSet.Add(item))
			{
				continue;
			}
			List<string> list = XWCSharpCodeModel.FindNamespacesByType(text2);
			if (list.Count == 0)
			{
				if (text2.Length >= 3)
				{
					(int, int) lineColumn = GetLineColumn(lineStarts, item2.Index);
					AddError(lineColumn.Item1, lineColumn.Item2, "无法识别类型 " + text2, Severity.Warning, "UNKNOWN_TYPE");
				}
				continue;
			}
			bool flag = false;
			foreach (string item3 in list)
			{
				if (XWCSharpCodeModel.IsNamespaceAlreadyVisible(item3, usingNamespaces, currentNamespace))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				(int, int) lineColumn2 = GetLineColumn(lineStarts, item2.Index);
				AddError(lineColumn2.Item1, lineColumn2.Item2, "缺少 using：" + text2 + " 来自 " + string.Join(" / ", list), Severity.Warning, "MISSING_USING");
			}
		}
	}

	private List<TypeReferenceInfo> CollectPotentialTypeReferences(string stripped)
	{
		List<TypeReferenceInfo> list = new List<TypeReferenceInfo>();
		AddTypeMatches(list, stripped, "\\bnew\\s+(?<type>[A-Z][A-Za-z0-9_.]*)");
		AddTypeMatches(list, stripped, "(?m)^\\s*(?:(?:public|private|protected|internal|static|readonly|const|new|override|virtual|required|volatile|partial|sealed|abstract|async)\\s+)*(?<type>[A-Z][A-Za-z0-9_.]*(?:\\s*<[^;\\r\\n=(){}]+>)?)\\s+@?[A-Za-z_][A-Za-z0-9_]*\\s*(?=[=;,{])");
		AddTypeMatches(list, stripped, "(?m)^\\s*(?:(?:public|private|protected|internal|static|new|override|virtual|partial|sealed|abstract|async)\\s+)*(?<type>[A-Z][A-Za-z0-9_.]*)\\s+[A-Za-z_][A-Za-z0-9_]*\\s*(?:<[^>{}()]*>)?\\s*\\(");
		AddTypeMatches(list, stripped, ":\\s*(?<type>[A-Z][A-Za-z0-9_.]*)");
		foreach (Match item in Regex.Matches(stripped, "\\b(?<container>[A-Z][A-Za-z0-9_.]*)\\s*<(?<args>[^>;=\\r\\n{}]+)>"))
		{
			ThrowIfCancellationRequested();
			if (IsMemberGenericMethodInvocation(stripped, item))
			{
				AddGenericArgumentTypeReferences(list, item);
				continue;
			}
			list.Add(new TypeReferenceInfo(item.Groups["container"].Value, item.Groups["container"].Index));
			AddGenericArgumentTypeReferences(list, item);
		}
		return list;
	}

	private bool IsMemberGenericMethodInvocation(string stripped, Match match)
	{
		int num = match.Groups["container"].Index - 1;
		while (num >= 0 && char.IsWhiteSpace(stripped[num]))
		{
			if ((num & 0x3FF) == 0)
			{
				ThrowIfCancellationRequested();
			}
			num--;
		}
		if (num < 0 || stripped[num] != '.')
		{
			return false;
		}
		int i;
		for (i = match.Index + match.Length; i < stripped.Length && char.IsWhiteSpace(stripped[i]); i++)
		{
			if ((i & 0x3FF) == 0)
			{
				ThrowIfCancellationRequested();
			}
		}
		if (i < stripped.Length)
		{
			return stripped[i] == '(';
		}
		return false;
	}

	private static void AddGenericArgumentTypeReferences(List<TypeReferenceInfo> result, Match match)
	{
		foreach (string item in SplitTopLevelParameters(match.Groups["args"].Value))
		{
			string text = XWCSharpCodeModel.NormalizeSimpleTypeName(item);
			if (!string.IsNullOrEmpty(text) && char.IsUpper(text[0]))
			{
				result.Add(new TypeReferenceInfo(text, match.Groups["args"].Index));
			}
		}
	}

	private void AddTypeMatches(List<TypeReferenceInfo> result, string stripped, string pattern)
	{
		foreach (Match item in Regex.Matches(stripped, pattern))
		{
			ThrowIfCancellationRequested();
			result.Add(new TypeReferenceInfo(item.Groups["type"].Value, item.Groups["type"].Index));
		}
	}

	private HashSet<string> CollectDeclaredTypes(string stripped)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		foreach (Match item in Regex.Matches(stripped, "\\b(class|struct|interface|enum|record)\\s+([A-Za-z_][A-Za-z0-9_]*)"))
		{
			ThrowIfCancellationRequested();
			hashSet.Add(item.Groups[2].Value);
		}
		return hashSet;
	}

	private HashSet<string> CollectTypeParameters(string stripped)
	{
		HashSet<string> result = new HashSet<string>(StringComparer.Ordinal);
		foreach (Match item in Regex.Matches(stripped, "\\b(class|struct|interface|record)\\s+[A-Za-z_][A-Za-z0-9_]*\\s*<(?<args>[^>{}()]*)>"))
		{
			ThrowIfCancellationRequested();
			AddTypeParameterNames(result, item.Groups["args"].Value);
		}
		foreach (Match item2 in Regex.Matches(stripped, "\\b[A-Za-z_][A-Za-z0-9_<>.,\\[\\]? ]+\\s+[A-Za-z_][A-Za-z0-9_]*\\s*<(?<args>[^>{}()]*)>\\s*\\("))
		{
			ThrowIfCancellationRequested();
			AddTypeParameterNames(result, item2.Groups["args"].Value);
		}
		return result;
	}

	private static void AddTypeParameterNames(HashSet<string> result, string args)
	{
		string[] array = args.Split(',');
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i].Trim();
			if (Regex.IsMatch(text, "^[A-Za-z_][A-Za-z0-9_]*$"))
			{
				result.Add(text);
			}
		}
	}

	private static bool ShouldSkipTypeReference(string simpleName, HashSet<string> declaredTypes, HashSet<string> typeParameters)
	{
		bool flag = string.IsNullOrEmpty(simpleName) || simpleName.Length <= 1 || !char.IsUpper(simpleName[0]) || declaredTypes.Contains(simpleName) || typeParameters.Contains(simpleName) || XWCSharpCodeModel.IsBuiltInTypeAlias(simpleName);
		if (!flag)
		{
			bool flag2;
			switch (simpleName)
			{
			case "Var":
			case "Null":
			case "True":
			case "False":
			case "Get":
			case "Set":
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

	private List<TypeBodyInfo> FindTypeBodies(string stripped)
	{
		List<TypeBodyInfo> list = new List<TypeBodyInfo>();
		Dictionary<int, int> dictionary = BuildBracePairs(stripped);
		foreach (Match item in Regex.Matches(stripped, "\\b(class|struct|interface)\\s+[A-Za-z_][A-Za-z0-9_]*[^{};]*\\{"))
		{
			ThrowIfCancellationRequested();
			int num = item.Index + item.Value.LastIndexOf('{');
			if (dictionary.TryGetValue(num, out var value) && value > num)
			{
				list.Add(new TypeBodyInfo(num, value));
			}
		}
		return list;
	}

	private Dictionary<int, int> BuildBracePairs(string text)
	{
		Dictionary<int, int> dictionary = new Dictionary<int, int>();
		Stack<int> stack = new Stack<int>();
		for (int i = 0; i < text.Length; i++)
		{
			if ((i & 0x3FF) == 0)
			{
				ThrowIfCancellationRequested();
			}
			if (text[i] == '{')
			{
				stack.Push(i);
			}
			else if (text[i] == '}' && stack.Count > 0)
			{
				dictionary[stack.Pop()] = i;
			}
		}
		return dictionary;
	}

	private static string BuildMethodSignatureKey(string name, string parameters)
	{
		List<string> list = new List<string>();
		foreach (string item in SplitTopLevelParameters(parameters))
		{
			string text = ExtractParameterType(item);
			if (text.Length > 0)
			{
				list.Add(text);
			}
		}
		return name + "(" + string.Join(",", list) + ")";
	}

	private static List<string> SplitTopLevelParameters(string parameters)
	{
		List<string> list = new List<string>();
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		for (int i = 0; i < parameters.Length; i++)
		{
			char c = parameters[i];
			switch (c)
			{
			case '<':
				num2++;
				continue;
			case '>':
				if (num2 > 0)
				{
					num2--;
					continue;
				}
				break;
			}
			switch (c)
			{
			case '(':
				num3++;
				continue;
			case ')':
				if (num3 > 0)
				{
					num3--;
					continue;
				}
				break;
			}
			switch (c)
			{
			case '[':
				num4++;
				continue;
			case ']':
				if (num4 > 0)
				{
					num4--;
					continue;
				}
				break;
			}
			if (c == ',' && num2 == 0 && num3 == 0 && num4 == 0)
			{
				list.Add(parameters.Substring(num, i - num));
				num = i + 1;
			}
		}
		if (num <= parameters.Length)
		{
			list.Add(parameters.Substring(num));
		}
		return list;
	}

	private static string ExtractParameterType(string parameter)
	{
		int num = parameter.IndexOf('=');
		if (num >= 0)
		{
			parameter = parameter.Substring(0, num);
		}
		string text = Regex.Replace(parameter.Trim(), "\\b(ref|out|in|params|this)\\b\\s*", "");
		if (text.Length == 0)
		{
			return "";
		}
		int num2 = text.LastIndexOf(' ');
		if (num2 <= 0)
		{
			return text;
		}
		return text.Substring(0, num2).Trim();
	}

	private static bool LooksLikeStatementMissingSemicolon(string line)
	{
		if (line.EndsWith(";") || line.EndsWith("{") || line.EndsWith("}") || line.EndsWith(",") || line.EndsWith(":"))
		{
			return false;
		}
		if (Regex.IsMatch(line, "^(if|else|for|foreach|while|switch|catch|using|lock|namespace|class|struct|interface|enum|try|finally)\\b"))
		{
			return false;
		}
		if (Regex.IsMatch(line, "^(public|private|protected|internal|static|override|virtual|partial|sealed|abstract)\\b"))
		{
			return false;
		}
		if (Regex.IsMatch(line, "^(return|throw|break|continue)\\b"))
		{
			return true;
		}
		if (Regex.IsMatch(line, "^[A-Za-z_][A-Za-z0-9_<>\\[\\].]*\\s+[A-Za-z_][A-Za-z0-9_]*\\s*="))
		{
			return true;
		}
		if (Regex.IsMatch(line, "^[A-Za-z_][A-Za-z0-9_.]*(\\+\\+|--|\\s*[\\+\\-\\*/%]?=|\\s*\\()"))
		{
			return true;
		}
		return false;
	}

	private static bool IsKnownPreprocessorDirective(string line)
	{
		return Regex.IsMatch(line, "^#\\s*(if|else|elif|endif|define|undef|warning|error|line|region|endregion|pragma|nullable)\\b");
	}

	private static string StripLineComment(string line)
	{
		bool flag = false;
		bool flag2 = false;
		for (int i = 0; i < line.Length - 1; i++)
		{
			char c = line[i];
			char c2 = line[i + 1];
			if (flag)
			{
				switch (c)
				{
				case '\\':
					i++;
					break;
				case '"':
					flag = false;
					break;
				}
				continue;
			}
			if (flag2)
			{
				switch (c)
				{
				case '\\':
					i++;
					break;
				case '\'':
					flag2 = false;
					break;
				}
				continue;
			}
			switch (c)
			{
			case '"':
				flag = true;
				break;
			case '\'':
				flag2 = true;
				break;
			case '/':
				if (c2 == '/')
				{
					return line.Substring(0, i);
				}
				break;
			}
		}
		return line;
	}

	private string StripCommentsAndStrings(string code)
	{
		char[] array = code.ToCharArray();
		State state = State.Normal;
		for (int i = 0; i < array.Length; i++)
		{
			if ((i & 0x3FF) == 0)
			{
				ThrowIfCancellationRequested();
			}
			char c = array[i];
			char c2 = ((i + 1 < array.Length) ? array[i + 1] : '\0');
			switch (state)
			{
			case State.Normal:
				if (c == '/' && c2 == '/')
				{
					state = State.InLineComment;
					array[i] = ' ';
					array[++i] = ' ';
					break;
				}
				if (c == '/' && c2 == '*')
				{
					state = State.InBlockComment;
					array[i] = ' ';
					array[++i] = ' ';
					break;
				}
				switch (c)
				{
				case '"':
					state = State.InString;
					array[i] = ' ';
					break;
				case '\'':
					state = State.InChar;
					array[i] = ' ';
					break;
				}
				break;
			case State.InString:
				switch (c)
				{
				case '\\':
					array[i] = ' ';
					if (i + 1 < array.Length)
					{
						array[++i] = ' ';
					}
					goto end_IL_0036;
				case '"':
					state = State.Normal;
					break;
				}
				array[i] = ((c == '\n') ? '\n' : ' ');
				break;
			case State.InChar:
				switch (c)
				{
				case '\\':
					array[i] = ' ';
					if (i + 1 < array.Length)
					{
						array[++i] = ' ';
					}
					goto end_IL_0036;
				case '\'':
					state = State.Normal;
					break;
				}
				array[i] = ((c == '\n') ? '\n' : ' ');
				break;
			case State.InLineComment:
				if (c == '\n')
				{
					state = State.Normal;
				}
				else
				{
					array[i] = ' ';
				}
				break;
			case State.InBlockComment:
				{
					if (c == '*' && c2 == '/')
					{
						array[i] = ' ';
						array[++i] = ' ';
						state = State.Normal;
					}
					else
					{
						array[i] = ((c == '\n') ? '\n' : ' ');
					}
					break;
				}
				end_IL_0036:
				break;
			}
		}
		return new string(array);
	}

	private List<int> BuildLineStarts(string text)
	{
		List<int> list = new List<int> { 0 };
		for (int i = 0; i < text.Length; i++)
		{
			if ((i & 0x3FF) == 0)
			{
				ThrowIfCancellationRequested();
			}
			if (text[i] == '\n')
			{
				list.Add(i + 1);
			}
		}
		return list;
	}

	private static (int Line, int Column) GetLineColumn(List<int> lineStarts, int index)
	{
		index = Math.Max(0, index);
		int num = lineStarts.BinarySearch(index);
		int val = ((num >= 0) ? num : (~num - 1));
		val = Math.Max(0, val);
		return (Line: val, Column: index - lineStarts[val]);
	}
}
