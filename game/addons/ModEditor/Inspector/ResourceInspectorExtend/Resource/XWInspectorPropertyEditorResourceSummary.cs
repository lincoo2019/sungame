using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Registry.Class;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorResourceSummary : XWInspectorPropertyEditorBase
{
	private const string ScenePath = "res://addons/ModEditor/Inspector/ResourceInspectorExtend/Resource/XWInspectorPropertyEditorResourceSummary.tscn";

	private XWResourcePicker _resourcePicker;

	private PanelContainer _previewPanel;

	private TextureRect _previewTexture;

	private Label _summaryLabel;

	private AudioStreamPlayer _audioPreviewPlayer;

	private Button _playButton;

	private Button _stopButton;

	private Button _openInspectorButton;

	private Button _refreshButton;

	private Tree _resourcePropertyTree;

	private string _baseType = "Resource";

	public static XWInspectorPropertyEditorResourceSummary Create()
	{
		return ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Inspector/ResourceInspectorExtend/Resource/XWInspectorPropertyEditorResourceSummary.tscn", null, ResourceLoader.CacheMode.Reuse).Instantiate<XWInspectorPropertyEditorResourceSummary>(PackedScene.GenEditState.Disabled);
	}

	public override void _Ready()
	{
		base._Ready();
		_resourcePicker = GetNode<XWResourcePicker>("%ResourcePicker");
		_previewPanel = GetNode<PanelContainer>("%PreviewPanel");
		_previewTexture = GetNode<TextureRect>("%PreviewTexture");
		_summaryLabel = GetNode<Label>("%SummaryLabel");
		_audioPreviewPlayer = GetNode<AudioStreamPlayer>("%AudioPreviewPlayer");
		_playButton = GetNode<Button>("%PlayButton");
		_stopButton = GetNode<Button>("%StopButton");
		_openInspectorButton = GetNode<Button>("%OpenInspectorButton");
		_refreshButton = GetNode<Button>("%RefreshButton");
		_resourcePropertyTree = GetNode<Tree>("%ResourcePropertyTree");
		_resourcePicker.ResourceChanged += OnResourceChanged;
		_resourcePicker.ResourceSelected += OnResourceSelected;
		_playButton.Pressed += PlayAudioPreview;
		_stopButton.Pressed += StopAudioPreview;
		_openInspectorButton.Pressed += delegate
		{
			OpenResourceInVisualEditor(ReadEditedResource());
		};
		_refreshButton.Pressed += UpdateValue;
		_resourcePropertyTree.ItemActivated += OpenSelectedPropertyResource;
		_resourcePropertyTree.SetColumnTitle(0, "属性");
		_resourcePropertyTree.SetColumnTitle(1, "值");
	}

	public override void SetEditProperty(XWInspectorProperty property, StringName field = null)
	{
		base.SetEditProperty(property, field);
		_baseType = ResolveBaseType(property);
		if (GodotObject.IsInstanceValid(_resourcePicker))
		{
			_resourcePicker.Setup(_baseType);
			UpdateValue();
		}
	}

	public override void UpdateValue()
	{
		Resource resource = ReadEditedResource();
		if (GodotObject.IsInstanceValid(_resourcePicker))
		{
			_resourcePicker.SetEditedResource(resource);
		}
		UpdatePreview(resource);
	}

	public override Variant GetValue()
	{
		return GetPropertyValue();
	}

	private void OnResourceChanged(Resource resource)
	{
		StopAudioPreview();
		ValueChange(resource);
		UpdatePreview(resource);
	}

	private void OnResourceSelected(Resource resource)
	{
		OpenResourceInVisualEditor(resource);
	}

	private void OpenResourceInVisualEditor(Resource resource)
	{
		if (GodotObject.IsInstanceValid(resource))
		{
			XWEditorInterface.Instance?.EditResource(resource);
		}
	}

	private void UpdatePreview(Resource resource)
	{
		if (IsNodeReady())
		{
			_summaryLabel.Text = BuildSummaryText(resource);
			Texture2D previewTexture = GetPreviewTexture(resource);
			_previewTexture.Texture = previewTexture;
			_previewTexture.Visible = GodotObject.IsInstanceValid(previewTexture);
			_previewPanel.Visible = GodotObject.IsInstanceValid(resource);
			bool flag = resource is AudioStream;
			_playButton.Visible = flag;
			_stopButton.Visible = flag;
			_openInspectorButton.Disabled = !GodotObject.IsInstanceValid(resource);
			_refreshButton.Disabled = !GodotObject.IsInstanceValid(resource);
			PopulateResourcePropertyTree(resource);
			if (!flag)
			{
				StopAudioPreview();
			}
		}
	}

	private string BuildSummaryText(Resource resource)
	{
		if (!GodotObject.IsInstanceValid(resource))
		{
			return "空资源";
		}
		string value = resource.GetClass();
		string value2 = (string.IsNullOrWhiteSpace(resource.ResourceName) ? "未命名" : resource.ResourceName);
		string value3 = (string.IsNullOrWhiteSpace(resource.ResourcePath) ? "内置资源" : resource.ResourcePath);
		string text = $"类型: {value}\n名称: {value2}\n路径: {value3}";
		if (resource is AudioStream audioStream)
		{
			double length = audioStream.GetLength();
			if (length > 0.0)
			{
				text += $"\n时长: {length:0.00}s";
			}
		}
		else if (resource is Texture2D texture2D && XWTextureSafety.CanPreview(texture2D))
		{
			text += $"\n尺寸: {texture2D.GetWidth()} x {texture2D.GetHeight()}";
		}
		else if (resource is Font font)
		{
			text += $"\n字体高度: {font.GetHeight():0.##}";
		}
		else if (resource is PackedScene packedScene)
		{
			SceneState state = packedScene.GetState();
			int value4 = (GodotObject.IsInstanceValid(state) ? state.GetNodeCount() : 0);
			text += $"\n节点数: {value4}";
		}
		return text;
	}

	private void PopulateResourcePropertyTree(Resource resource)
	{
		if (GodotObject.IsInstanceValid(_resourcePropertyTree))
		{
			_resourcePropertyTree.Clear();
			TreeItem root = _resourcePropertyTree.CreateItem();
			if (GodotObject.IsInstanceValid(resource))
			{
				AppendResourcePropertyRows(resource, root);
			}
		}
	}

	private int AppendResourcePropertyRows(Resource resource, TreeItem root)
	{
		int num = 0;
		foreach (Dictionary property in resource.GetPropertyList())
		{
			if (!TryReadPropertyInfo(property, out var name, out var type, out var hint, out var hintString, out var usage) || (usage & (PropertyUsageFlags.Default | PropertyUsageFlags.ScriptVariable)) == PropertyUsageFlags.None)
			{
				continue;
			}
			Variant value;
			try
			{
				value = resource.Get(new StringName(name));
			}
			catch
			{
				continue;
			}
			TreeItem treeItem = _resourcePropertyTree.CreateItem(root);
			treeItem.SetText(0, FormatPropertyName(name));
			treeItem.SetText(1, FormatPropertyValue(value));
			treeItem.SetTooltipText(0, $"{name}\n类型: {type}\nHint: {hint} {hintString}".StripEdges());
			treeItem.SetTooltipText(1, FormatPropertyValue(value));
			if (value.VariantType == Variant.Type.Object && value.As<GodotObject>() is Resource resource2)
			{
				treeItem.SetMetadata(0, resource2);
				Texture2D classIcon = XWClassRegistry.Instance.GetClassIcon(resource2.GetClass());
				if (GodotObject.IsInstanceValid(classIcon))
				{
					treeItem.SetIcon(0, classIcon);
				}
			}
			else
			{
				treeItem.SetMetadata(0, name);
			}
			num++;
		}
		if (num == 0)
		{
			TreeItem treeItem2 = _resourcePropertyTree.CreateItem(root);
			treeItem2.SetText(0, "没有可显示属性");
			treeItem2.SetText(1, "");
		}
		return num;
	}

	private static bool TryReadPropertyInfo(Dictionary property, out string name, out Variant.Type type, out PropertyHint hint, out string hintString, out PropertyUsageFlags usage)
	{
		name = "";
		type = Variant.Type.Nil;
		hint = PropertyHint.None;
		hintString = "";
		usage = PropertyUsageFlags.None;
		if (property == null || !property.TryGetValue("name", out var value))
		{
			return false;
		}
		name = value.AsString();
		if (string.IsNullOrWhiteSpace(name))
		{
			return false;
		}
		if (property.TryGetValue("type", out var value2))
		{
			type = (Variant.Type)value2.AsInt32();
		}
		if (property.TryGetValue("hint", out var value3))
		{
			hint = (PropertyHint)value3.AsInt32();
		}
		if (property.TryGetValue("hint_string", out var value4))
		{
			hintString = value4.AsString();
		}
		if (property.TryGetValue("usage", out var value5))
		{
			usage = (PropertyUsageFlags)value5.AsInt64();
		}
		return true;
	}

	private static string FormatPropertyName(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			return "";
		}
		return name.Replace('_', ' ');
	}

	private static string FormatPropertyValue(Variant value)
	{
		Variant.Type variantType = value.VariantType;
		if ((ulong)variantType <= 28uL)
		{
			switch ((int)variantType)
			{
			case 0:
				return "空";
			case 1:
				return value.AsBool() ? "启用" : "关闭";
			case 2:
				return value.AsInt64().ToString();
			case 3:
				return value.AsDouble().ToString("0.###");
			case 4:
			case 21:
			case 22:
				return string.IsNullOrEmpty(value.AsString()) ? "空" : value.AsString();
			case 5:
				return FormatVector(value.AsVector2());
			case 6:
				return FormatVector(value.AsVector2I());
			case 9:
				return FormatVector(value.AsVector3());
			case 10:
				return FormatVector(value.AsVector3I());
			case 12:
				return FormatVector(value.AsVector4());
			case 13:
				return FormatVector(value.AsVector4I());
			case 20:
				return value.AsColor().ToHtml();
			case 28:
				return $"数组({value.AsGodotArray().Count})";
			case 27:
				return $"字典({value.AsGodotDictionary().Count})";
			case 24:
				return FormatObject(value.AsGodotObject());
			}
		}
		return value.ToString();
	}

	private static string FormatVector(Vector2 value)
	{
		return $"{value.X:0.###}, {value.Y:0.###}";
	}

	private static string FormatVector(Vector2I value)
	{
		return $"{value.X}, {value.Y}";
	}

	private static string FormatVector(Vector3 value)
	{
		return $"{value.X:0.###}, {value.Y:0.###}, {value.Z:0.###}";
	}

	private static string FormatVector(Vector3I value)
	{
		return $"{value.X}, {value.Y}, {value.Z}";
	}

	private static string FormatVector(Vector4 value)
	{
		return $"{value.X:0.###}, {value.Y:0.###}, {value.Z:0.###}, {value.W:0.###}";
	}

	private static string FormatVector(Vector4I value)
	{
		return $"{value.X}, {value.Y}, {value.Z}, {value.W}";
	}

	private static string FormatObject(GodotObject obj)
	{
		if (!GodotObject.IsInstanceValid(obj))
		{
			return "空";
		}
		if (obj is Resource resource)
		{
			string text = (string.IsNullOrWhiteSpace(resource.ResourcePath) ? "内置" : resource.ResourcePath.GetFile());
			return resource.GetClass() + " (" + text + ")";
		}
		return obj.GetClass();
	}

	private void OpenSelectedPropertyResource()
	{
		TreeItem selected = _resourcePropertyTree.GetSelected();
		if (selected != null)
		{
			Variant metadata = selected.GetMetadata(0);
			if (metadata.VariantType == Variant.Type.Object && metadata.As<GodotObject>() is Resource resource)
			{
				OpenResourceInVisualEditor(resource);
			}
		}
	}

	private Texture2D GetPreviewTexture(Resource resource)
	{
		if (!GodotObject.IsInstanceValid(resource))
		{
			return null;
		}
		if (resource is Texture2D texture2D && XWTextureSafety.CanPreview(texture2D))
		{
			return texture2D;
		}
		if (resource is Gradient gradient)
		{
			return new GradientTexture1D
			{
				Gradient = gradient,
				Width = 128
			};
		}
		return null;
	}

	private Resource ReadEditedResource()
	{
		Variant propertyValue = GetPropertyValue();
		if (propertyValue.VariantType != Variant.Type.Object || !(propertyValue.As<GodotObject>() is Resource result))
		{
			return null;
		}
		return result;
	}

	private string ResolveBaseType(XWInspectorProperty property)
	{
		if (property == null)
		{
			return "Resource";
		}
		if (property.Hint == PropertyHint.ResourceType && !string.IsNullOrWhiteSpace(property.HintString))
		{
			return property.HintString;
		}
		if (!string.IsNullOrWhiteSpace(property.HintString))
		{
			return property.HintString;
		}
		Variant propertyValue = GetPropertyValue();
		if (propertyValue.VariantType == Variant.Type.Object && propertyValue.As<GodotObject>() is Resource resource)
		{
			return resource.GetClass();
		}
		return "Resource";
	}

	private void PlayAudioPreview()
	{
		if (ReadEditedResource() is AudioStream stream)
		{
			_audioPreviewPlayer.Stop();
			_audioPreviewPlayer.Stream = stream;
			_audioPreviewPlayer.Play();
		}
	}

	private void StopAudioPreview()
	{
		if (GodotObject.IsInstanceValid(_audioPreviewPlayer))
		{
			_audioPreviewPlayer.Stop();
		}
	}
}
