using System;
using System.Text;
using Godot;

namespace PVZHE.ModEditor.Blueprint;

public static class XWBlueprintCreationService
{
	public sealed class Result
	{
		public bool Success { get; init; }

		public string CreatedPath { get; init; } = "";

		public string Error { get; init; } = "";

		public XWBPScript Blueprint { get; init; }
	}

	public static Result Create(string path, string parentClass = "Node", string resourceName = "", bool overwrite = false)
	{
		path = NormalizePath(path);
		if (string.IsNullOrWhiteSpace(path))
		{
			return Failure(path, "蓝图路径为空。");
		}
		if (!path.EndsWith(".tres", StringComparison.OrdinalIgnoreCase))
		{
			path += ".tres";
		}
		if (FileAccess.FileExists(path) && !overwrite)
		{
			return Failure(path, "目标蓝图已经存在。");
		}
		string baseDir = path.GetBaseDir();
		if (!string.IsNullOrWhiteSpace(baseDir) && !DirAccess.DirExistsAbsolute(baseDir))
		{
			Error error = DirAccess.MakeDirRecursiveAbsolute(baseDir);
			if (error != Error.Ok)
			{
				return Failure(path, $"无法创建蓝图目录：{error}");
			}
		}
		XWBPScript xWBPScript = XWBPScript.Create();
		xWBPScript.ExtendsClass = new StringName(string.IsNullOrWhiteSpace(parentClass) ? "Node" : parentClass.Trim());
		xWBPScript.ResourceName = (string.IsNullOrWhiteSpace(resourceName) ? path.GetFile().GetBaseName() : resourceName.Trim());
		Error error2 = ResourceSaver.Save(xWBPScript, path, ResourceSaver.SaverFlags.None);
		if (error2 != Error.Ok)
		{
			error2 = SaveBlueprintTextFallback(xWBPScript, path);
		}
		if (error2 != Error.Ok)
		{
			return Failure(path, $"无法保存蓝图：{error2}");
		}
		return new Result
		{
			Success = true,
			CreatedPath = path,
			Blueprint = xWBPScript
		};
	}

	private static Result Failure(string path, string error)
	{
		return new Result
		{
			Success = false,
			CreatedPath = (path ?? ""),
			Error = (error ?? "蓝图创建失败。")
		};
	}

	private static Error SaveBlueprintTextFallback(XWBPScript script, string path)
	{
		using FileAccess fileAccess = FileAccess.Open(path, FileAccess.ModeFlags.Write);
		if (!GodotObject.IsInstanceValid(fileAccess))
		{
			return FileAccess.GetOpenError();
		}
		fileAccess.StoreString(BuildBlueprintText(script));
		fileAccess.Flush();
		return fileAccess.GetError();
	}

	private static string BuildBlueprintText(XWBPScript script)
	{
		string value = EscapeGodotString(script.ExtendsClass.ToString());
		string value2 = EscapeGodotString(script.ResourceName);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("[gd_resource type=\"Resource\" script_class=\"XWBPScript\" load_steps=3 format=3]");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("[ext_resource type=\"Script\" path=\"res://addons/ModEditor/Blueprint/Resource/XWBPScript.cs\" id=\"1_xwbp\"]");
		stringBuilder.AppendLine("[ext_resource type=\"Script\" path=\"res://addons/ModEditor/Blueprint/Resource/XWBPGraphSerializeData.cs\" id=\"2_graph\"]");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("[sub_resource type=\"Resource\" id=\"Resource_graph\"]");
		stringBuilder.AppendLine("script = ExtResource(\"2_graph\")");
		stringBuilder.AppendLine("Id = 1");
		stringBuilder.AppendLine("Name = \"图表\"");
		stringBuilder.AppendLine("NextNodeId = 1");
		stringBuilder.AppendLine("Lock = true");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("[resource]");
		stringBuilder.AppendLine("script = ExtResource(\"1_xwbp\")");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(18, 1, stringBuilder2);
		handler.AppendLiteral("resource_name = \"");
		handler.AppendFormatted(value2);
		handler.AppendLiteral("\"");
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(18, 1, stringBuilder2);
		handler.AppendLiteral("ExtendsClass = &\"");
		handler.AppendFormatted(value);
		handler.AppendLiteral("\"");
		stringBuilder4.AppendLine(ref handler);
		stringBuilder.AppendLine("Graphs = Array[ExtResource(\"2_graph\")]([SubResource(\"Resource_graph\")])");
		stringBuilder.AppendLine("NextGraphId = 2");
		stringBuilder.AppendLine("NextFunctionId = 1");
		stringBuilder.AppendLine("NextVariableId = 1");
		stringBuilder.AppendLine("NextSignalId = 1");
		return stringBuilder.ToString();
	}

	private static string EscapeGodotString(string value)
	{
		return (value ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
	}

	private static string NormalizePath(string path)
	{
		return (path ?? "").Trim().Replace('\\', '/');
	}
}
