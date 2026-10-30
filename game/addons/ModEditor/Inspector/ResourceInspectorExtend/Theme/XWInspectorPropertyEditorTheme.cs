using System;
using Godot;
using PVZHE.ModEditor.Core;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorTheme : XWInspectorPropertyEditorBase
{
	private Tree _themeItemTree;

	private Button _previewButton;

	private PanelContainer _previewPanel;

	private Theme _theme;

	public override void _Ready()
	{
		base._Ready();
		_themeItemTree = GetNode<Tree>("%ThemeItemTree");
		_previewButton = GetNode<Button>("%PreviewButton");
		_previewPanel = GetNode<PanelContainer>("%PreviewPanel");
		_themeItemTree.SetColumnTitle(0, "项目");
		_themeItemTree.SetColumnTitle(1, "类型");
		_themeItemTree.SetColumnTitle(2, "值");
		_themeItemTree.ItemActivated += OpenSelectedResource;
		_previewButton.Pressed += CommitThemeChange;
	}

	public override void SetEditProperty(XWInspectorProperty property, StringName field = null)
	{
		base.SetEditProperty(property, field);
		RefreshTheme();
	}

	public override void UpdateValue()
	{
		RefreshTheme();
	}

	public override Variant GetValue()
	{
		return _theme;
	}

	private void RefreshTheme()
	{
		_theme = ReadTheme();
		_previewPanel.Theme = _theme;
		_previewButton.Theme = _theme;
		PopulateThemeTree();
	}

	private Theme ReadTheme()
	{
		Variant propertyValue = GetPropertyValue();
		if (propertyValue.VariantType != Variant.Type.Object || !(propertyValue.As<GodotObject>() is Theme result))
		{
			return null;
		}
		return result;
	}

	private void PopulateThemeTree()
	{
		_themeItemTree.Clear();
		TreeItem parent = _themeItemTree.CreateItem();
		if (!GodotObject.IsInstanceValid(_theme))
		{
			return;
		}
		string[] typeList = _theme.GetTypeList();
		foreach (string typeName in typeList)
		{
			TreeItem treeItem = _themeItemTree.CreateItem(parent);
			treeItem.SetText(0, typeName);
			treeItem.SetText(1, "Type");
			treeItem.Collapsed = true;
			AppendThemeItemRows(treeItem, typeName, "颜色", _theme.GetColorList(typeName), (StringName name) => FormatColor(_theme.GetColor(name, typeName)), null);
			AppendThemeItemRows(treeItem, typeName, "常量", _theme.GetConstantList(typeName), (StringName name) => _theme.GetConstant(name, typeName).ToString(), null);
			AppendThemeItemRows(treeItem, typeName, "字体大小", _theme.GetFontSizeList(typeName), (StringName name) => _theme.GetFontSize(name, typeName).ToString(), null);
			AppendThemeItemRows(treeItem, typeName, "字体", _theme.GetFontList(typeName), (StringName name) => FormatResource(_theme.GetFont(name, typeName)), (StringName name) => _theme.GetFont(name, typeName));
			AppendThemeItemRows(treeItem, typeName, "图标", _theme.GetIconList(typeName), (StringName name) => FormatResource(_theme.GetIcon(name, typeName)), (StringName name) => _theme.GetIcon(name, typeName));
			AppendThemeItemRows(treeItem, typeName, "StyleBox", _theme.GetStyleboxList(typeName), (StringName name) => FormatResource(_theme.GetStylebox(name, typeName)), (StringName name) => _theme.GetStylebox(name, typeName));
		}
	}

	private void AppendThemeItemRows(TreeItem parent, string typeName, string groupName, string[] names, Func<StringName, string> valueFormatter, Func<StringName, Resource> resourceGetter)
	{
		if (names == null || names.Length == 0)
		{
			return;
		}
		TreeItem treeItem = _themeItemTree.CreateItem(parent);
		treeItem.SetText(0, groupName);
		treeItem.SetText(1, typeName);
		treeItem.Collapsed = true;
		foreach (string text in names)
		{
			StringName arg = new StringName(text);
			TreeItem treeItem2 = _themeItemTree.CreateItem(treeItem);
			treeItem2.SetText(0, text);
			treeItem2.SetText(1, groupName);
			treeItem2.SetText(2, valueFormatter(arg));
			Resource resource = resourceGetter?.Invoke(arg);
			if (GodotObject.IsInstanceValid(resource))
			{
				treeItem2.SetMetadata(0, resource);
			}
		}
	}

	private void OpenSelectedResource()
	{
		TreeItem selected = _themeItemTree.GetSelected();
		if (selected != null)
		{
			Variant metadata = selected.GetMetadata(0);
			if (metadata.VariantType == Variant.Type.Object && metadata.As<GodotObject>() is Resource res)
			{
				XWEditorInterface.Instance?.EditResource(res);
			}
		}
	}

	private void CommitThemeChange()
	{
		if (GodotObject.IsInstanceValid(_theme))
		{
			_theme.EmitChanged();
			if (GodotObject.IsInstanceValid(Property) && Property.PropName != (StringName)"")
			{
				ValueChange(_theme);
			}
			else
			{
				Property?.SetCall();
			}
		}
	}

	private static string FormatColor(Color color)
	{
		return "#" + color.ToHtml();
	}

	private static string FormatResource(Resource resource)
	{
		if (!GodotObject.IsInstanceValid(resource))
		{
			return "空";
		}
		string text = (string.IsNullOrWhiteSpace(resource.ResourcePath) ? "内置" : resource.ResourcePath.GetFile());
		return resource.GetClass() + " (" + text + ")";
	}
}
