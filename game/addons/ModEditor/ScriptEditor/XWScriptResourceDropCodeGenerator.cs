using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Godot;

namespace PVZHE.ModEditor.ScriptEditor;

public static class XWScriptResourceDropCodeGenerator
{
	private static readonly HashSet<string> CSharpKeywords = new HashSet<string>(StringComparer.Ordinal)
	{
		"abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
		"class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum",
		"event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto",
		"if", "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace",
		"new", "null", "object", "operator", "out", "override", "params", "private", "protected", "public",
		"readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string",
		"struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked",
		"unsafe", "ushort", "using", "virtual", "void", "volatile", "while"
	};

	public static string BuildSnippetForPaths(IEnumerable<string> paths, string scriptExtension)
	{
		return BuildSnippetForPaths(paths, scriptExtension, preferFieldDeclaration: false);
	}

	public static string BuildSnippetForPaths(IEnumerable<string> paths, string scriptExtension, bool preferFieldDeclaration)
	{
		if (paths == null)
		{
			return "";
		}
		List<string> list = new List<string>();
		foreach (string path in paths)
		{
			string text = BuildSnippetForPath(path, scriptExtension, preferFieldDeclaration);
			if (!string.IsNullOrWhiteSpace(text))
			{
				list.Add(text);
			}
		}
		return string.Join(System.Environment.NewLine, list);
	}

	public static string BuildSnippetForPath(string path, string scriptExtension)
	{
		return BuildSnippetForPath(path, scriptExtension, preferFieldDeclaration: false);
	}

	public static string BuildSnippetForPath(string path, string scriptExtension, bool preferFieldDeclaration)
	{
		string text = NormalizeResourcePath(path);
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		if ((scriptExtension ?? "").TrimStart('.').ToLowerInvariant() != "cs")
		{
			return "";
		}
		if (XWCSharpOnlyPolicy.IsUnsupportedScriptSourcePath(text))
		{
			return "";
		}
		return BuildCSharpLoadSnippet(text, preferFieldDeclaration);
	}

	public static string NormalizeResourcePath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return "";
		}
		return path.Replace('\\', '/').Trim();
	}

	public static string EscapeStringLiteral(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return "";
		}
		return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r")
			.Replace("\n", "\\n")
			.Replace("\t", "\\t");
	}

	public static string MakeVariableName(string path)
	{
		string value = Path.GetFileNameWithoutExtension(NormalizeResourcePath(path));
		if (string.IsNullOrWhiteSpace(value))
		{
			value = "resource";
		}
		List<string> list = SplitIdentifierWords(value);
		if (list.Count == 0)
		{
			list.Add("resource");
		}
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < list.Count; i++)
		{
			string text = list[i];
			if (i == 0)
			{
				StringBuilder stringBuilder2 = stringBuilder.Append(char.ToLowerInvariant(text[0]));
				object value2;
				if (text.Length <= 1)
				{
					value2 = "";
				}
				else
				{
					string text2 = text;
					value2 = text2.Substring(1, text2.Length - 1);
				}
				stringBuilder2.Append((string?)value2);
			}
			else
			{
				StringBuilder stringBuilder3 = stringBuilder.Append(char.ToUpperInvariant(text[0]));
				object value3;
				if (text.Length <= 1)
				{
					value3 = "";
				}
				else
				{
					string text2 = text;
					value3 = text2.Substring(1, text2.Length - 1);
				}
				stringBuilder3.Append((string?)value3);
			}
		}
		string text3 = stringBuilder.ToString();
		if (string.IsNullOrWhiteSpace(text3))
		{
			text3 = "resource";
		}
		if (char.IsDigit(text3[0]))
		{
			ReadOnlySpan<char> readOnlySpan = "resource";
			ReadOnlySpan<char> readOnlySpan2 = new ReadOnlySpan<char>(char.ToUpperInvariant(text3[0]));
			string text2 = text3;
			text3 = string.Concat(readOnlySpan, readOnlySpan2, text2.Substring(1, text2.Length - 1));
		}
		if (CSharpKeywords.Contains(text3))
		{
			text3 += "Resource";
		}
		return text3;
	}

	private static string BuildCSharpLoadSnippet(string resourcePath, bool preferFieldDeclaration)
	{
		string value = MakeVariableName(resourcePath);
		string cSharpResourceType = GetCSharpResourceType(resourcePath);
		if (!preferFieldDeclaration)
		{
			return $"var {value} = GD.Load<{cSharpResourceType}>(\"{EscapeStringLiteral(resourcePath)}\");";
		}
		return $"private {cSharpResourceType} {value} = GD.Load<{cSharpResourceType}>(\"{EscapeStringLiteral(resourcePath)}\");";
	}

	private static string GetCSharpResourceType(string path)
	{
		switch (Path.GetExtension(NormalizeResourcePath(path)).ToLowerInvariant())
		{
		case ".tscn":
		case ".scn":
			return "PackedScene";
		case ".jpeg":
		case ".webp":
		case ".svg":
		case ".png":
		case ".jpg":
		case ".bmp":
		case ".tga":
			return "Texture2D";
		case ".wav":
		case ".ogg":
		case ".mp3":
			return "AudioStream";
		case ".woff":
		case ".font":
		case ".ttf":
		case ".otf":
		case ".fnt":
		case ".woff2":
			return "Font";
		case ".cs":
			return "Script";
		case ".tres":
		case ".res":
			return TryResolveResourceClass(path);
		default:
			return "Resource";
		}
	}

	private static string TryResolveResourceClass(string path)
	{
		try
		{
			if (!ResourceLoader.Exists(path))
			{
				return "Resource";
			}
			Resource resource = ResourceLoader.Load<Resource>(path, null, ResourceLoader.CacheMode.Reuse);
			if (resource == null)
			{
				return "Resource";
			}
			string name = resource.GetType().Name;
			if (!string.IsNullOrWhiteSpace(name) && name != "Resource" && IsIdentifier(name))
			{
				return name;
			}
		}
		catch
		{
			return "Resource";
		}
		return "Resource";
	}

	private static List<string> SplitIdentifierWords(string value)
	{
		List<string> list = new List<string>();
		StringBuilder stringBuilder = new StringBuilder();
		foreach (char c in value)
		{
			if (char.IsLetterOrDigit(c))
			{
				stringBuilder.Append(c);
			}
			else
			{
				FlushWord(list, stringBuilder);
			}
		}
		FlushWord(list, stringBuilder);
		return (from word in list.Select(NormalizeWord)
			where word.Length > 0
			select word).ToList();
	}

	private static void FlushWord(List<string> words, StringBuilder builder)
	{
		if (builder.Length != 0)
		{
			words.Add(builder.ToString());
			builder.Clear();
		}
	}

	private static string NormalizeWord(string word)
	{
		if (string.IsNullOrWhiteSpace(word))
		{
			return "";
		}
		if (word.All(char.IsUpper))
		{
			word = word.ToLowerInvariant();
		}
		ReadOnlySpan<char> readOnlySpan = new ReadOnlySpan<char>(char.ToUpperInvariant(word[0]));
		object obj;
		if (word.Length <= 1)
		{
			obj = "";
		}
		else
		{
			string text = word;
			obj = text.Substring(1, text.Length - 1);
		}
		return string.Concat(readOnlySpan, (string?)obj);
	}

	private static bool IsIdentifier(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return false;
		}
		if (!char.IsLetter(value[0]) && value[0] != '_')
		{
			return false;
		}
		for (int i = 1; i < value.Length; i++)
		{
			if (!char.IsLetterOrDigit(value[i]) && value[i] != '_')
			{
				return false;
			}
		}
		return true;
	}
}
