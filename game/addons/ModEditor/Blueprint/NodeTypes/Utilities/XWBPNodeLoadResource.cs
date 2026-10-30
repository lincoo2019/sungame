using System;
using Godot;
using PVZHE.ModEditor.ScriptEditor;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeLoadResource : XWBPNodeType
{
	private const string OutputPortName = "资源";

	private const string MetaResourcePath = "ResourcePath";

	private const string MetaResourceClass = "ResourceClass";

	public string ResourcePath { get; set; } = "";

	public string ResourceClass { get; set; } = "Resource";

	public XWBPNodeLoadResource()
	{
		base.TypeId = "__XWBPGraphNode_LoadResource";
		base.Category = "实用工具";
		base.DisplayName = "加载资源";
		base.Description = "输出拖入蓝图图表的资源。";
		base.Color = new Color(0.36f, 0.46f, 0.62f);
		AddOutput("资源", XWBPNodePortData.PortType.Object, "Resource");
	}

	public static XWBPNodeLoadResource CreateForPath(string resourcePath, Resource resource = null)
	{
		string text = NormalizeResourcePath(resourcePath);
		if (XWCSharpOnlyPolicy.IsUnsupportedScriptSourcePath(text) || !XWCSharpOnlyPolicy.IsSupportedModScriptResource(resource))
		{
			return null;
		}
		return new XWBPNodeLoadResource
		{
			ResourcePath = text,
			ResourceClass = ResolveResourceClass(text, resource)
		};
	}

	public override XWBPNodeData CreateNodeData()
	{
		XWBPNodeData xWBPNodeData = base.CreateNodeData();
		ApplyResourceMetadata(xWBPNodeData, ResourcePath, ResourceClass);
		RebuildResourceOutput(xWBPNodeData, ResourceClass);
		return xWBPNodeData;
	}

	public override void LoadNodeData(XWBPNodeData nodeData)
	{
		if (nodeData != null)
		{
			ResourcePath = GetResourcePath(nodeData);
			ResourceClass = GetResourceClass(nodeData);
			RebuildResourceOutput(nodeData, ResourceClass);
		}
	}

	public override void GraphNodeBuildRefresh(GodotObject graphNode)
	{
		if (graphNode is XWBPGraphNode { NodeData: not null } xWBPGraphNode)
		{
			string resourcePath = GetResourcePath(xWBPGraphNode.NodeData);
			string resourceClass = GetResourceClass(xWBPGraphNode.NodeData);
			RebuildResourceOutput(xWBPGraphNode.NodeData, resourceClass);
			ApplyResourceMetadata(xWBPGraphNode.NodeData, resourcePath, resourceClass);
			if (!string.IsNullOrWhiteSpace(resourcePath))
			{
				xWBPGraphNode.Title = "加载 " + resourcePath.GetFile();
			}
		}
	}

	public override void GenerateCode(XWBPCodeGenerator generator, XWBPNodeData node, XWBPFunctionData function)
	{
	}

	public override void GenerateCodeFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, XWBPGraphData graph)
	{
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (!(portName == "资源"))
		{
			return "null";
		}
		return BuildLoadExpression(generator, node);
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (!(portName == "资源"))
		{
			return "null";
		}
		return BuildLoadExpression(generator, node);
	}

	public static string NormalizeResourcePath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return "";
		}
		string text = path.Replace('\\', '/').Trim();
		if (text.StartsWith("res://", StringComparison.OrdinalIgnoreCase) || text.StartsWith("uid://", StringComparison.OrdinalIgnoreCase))
		{
			return text;
		}
		string text2 = ProjectSettings.GlobalizePath("res://").Replace('\\', '/').TrimEnd('/');
		if (!string.IsNullOrWhiteSpace(text2) && text.StartsWith(text2, StringComparison.OrdinalIgnoreCase))
		{
			string text3 = text;
			int length = text2.Length;
			string text4 = text3.Substring(length, text3.Length - length).TrimStart('/');
			return "res://" + text4;
		}
		return text;
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

	private static void ApplyResourceMetadata(XWBPNodeData nodeData, string resourcePath, string resourceClass)
	{
		if (nodeData != null)
		{
			nodeData.SetMetaData("ResourcePath", NormalizeResourcePath(resourcePath));
			nodeData.SetMetaData("ResourceClass", NormalizeResourceClass(resourceClass));
		}
	}

	private static void RebuildResourceOutput(XWBPNodeData nodeData, string resourceClass)
	{
		if (nodeData != null)
		{
			nodeData.InputPorts.Clear();
			nodeData.OutputPorts.Clear();
			nodeData.OutputPorts.Add(new XWBPNodePortData("资源", XWBPNodePortData.Direction.Output, XWBPNodePortData.PortType.Object, NormalizeResourceClass(resourceClass), default(Variant)));
			nodeData.RebuildPortMap();
		}
	}

	private static string BuildLoadExpression(XWBPCodeGenerator generator, XWBPNodeData node)
	{
		string resourcePath = GetResourcePath(node);
		if (string.IsNullOrWhiteSpace(resourcePath))
		{
			return "null";
		}
		if (XWCSharpOnlyPolicy.IsUnsupportedScriptSourcePath(resourcePath))
		{
			return "null";
		}
		string resourceClass = GetResourceClass(node);
		string value = ((generator != null) ? generator.ToCsClassName(resourceClass) : resourceClass);
		if (!IsSafeTypeName(value))
		{
			value = "Resource";
		}
		return $"GD.Load<{value}>(\"{EscapeStringLiteral(resourcePath)}\")";
	}

	private static string GetResourcePath(XWBPNodeData nodeData)
	{
		if (nodeData == null)
		{
			return "";
		}
		return NormalizeResourcePath(nodeData.GetMetaData("ResourcePath").AsString());
	}

	private static string GetResourceClass(XWBPNodeData nodeData)
	{
		if (nodeData == null)
		{
			return "Resource";
		}
		return NormalizeResourceClass(nodeData.GetMetaData("ResourceClass").AsString());
	}

	private static string ResolveResourceClass(string resourcePath, Resource resource)
	{
		string resourceClass;
		switch ((resourcePath ?? "").GetExtension().ToLowerInvariant())
		{
		case "tscn":
		case "scn":
			resourceClass = "PackedScene";
			break;
		case "jpeg":
		case "webp":
		case "svg":
		case "png":
		case "jpg":
		case "bmp":
		case "tga":
			resourceClass = "Texture2D";
			break;
		case "wav":
		case "ogg":
		case "mp3":
			resourceClass = "AudioStream";
			break;
		case "woff":
		case "font":
		case "ttf":
		case "otf":
		case "fnt":
		case "woff2":
			resourceClass = "Font";
			break;
		case "cs":
			resourceClass = "Script";
			break;
		default:
			resourceClass = resource?.GetClass() ?? "Resource";
			break;
		}
		return NormalizeResourceClass(resourceClass);
	}

	private static string NormalizeResourceClass(string resourceClass)
	{
		if (string.IsNullOrWhiteSpace(resourceClass))
		{
			return "Resource";
		}
		if (!IsSafeTypeName(resourceClass))
		{
			return "Resource";
		}
		return resourceClass;
	}

	private static bool IsSafeTypeName(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return false;
		}
		string[] array = value.Split('.');
		foreach (string text in array)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return false;
			}
			if (!char.IsLetter(text[0]) && text[0] != '_')
			{
				return false;
			}
			for (int j = 1; j < text.Length; j++)
			{
				char c = text[j];
				if (!char.IsLetterOrDigit(c) && c != '_')
				{
					return false;
				}
			}
		}
		return true;
	}
}
