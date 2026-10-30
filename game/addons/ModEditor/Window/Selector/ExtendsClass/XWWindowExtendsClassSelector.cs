using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Blueprint;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Registry.BP;
using PVZHE.ModEditor.Registry.Class;

namespace PVZHE.ModEditor.GUI;

public partial class XWWindowExtendsClassSelector : XWWindowSelector
{
	[Signal]
	public delegate void ClassSelectedEventHandler(StringName className);

	private const string ScenePath = "res://addons/ModEditor/Window/Selector/ExtendsClass/XWWindowExtendsClassSelector.tscn";

	private XWClassData _selectedData;

	private string _selectedProjectClassName = "";

	private string _catalogProjectRoot = "";

	private int _catalogProjectContextVersion;

	private XWBPScriptData _catalogScriptData;

	private bool _projectIndexRequested;

	public XWBPEditor ContextEditor { get; set; }

	public static XWWindowExtendsClassSelector Create()
	{
		return ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Window/Selector/ExtendsClass/XWWindowExtendsClassSelector.tscn", null, ResourceLoader.CacheMode.Reuse).Instantiate<XWWindowExtendsClassSelector>(PackedScene.GenEditState.Disabled);
	}

	public override void _Ready()
	{
		FavoritesFilePath = "user://xweditor_extends_class_favorites.cfg";
		RecentFilePath = "user://xweditor_extends_class_recent.cfg";
		_catalogProjectRoot = XWBPCSharpMemberRegistry.Instance.ActiveProjectRoot;
		_catalogProjectContextVersion = XWBPCSharpMemberRegistry.Instance.ProjectContextVersion;
		_catalogScriptData = ContextEditor?.BpScriptData;
		base._Ready();
		base.Title = "选择继承类";
		base.OkButtonText = "继承";
		GetOkButton().Disabled = true;
		if (GodotObject.IsInstanceValid(_searchLineEdit))
		{
			_searchLineEdit.PlaceholderText = "搜索类";
			_searchLineEdit.GrabFocus();
		}
		BeginProjectIndexWarmup();
	}

	protected override void OnCloseRequested()
	{
		QueueFree();
	}

	protected override void OnConfirmed()
	{
		XWBPCSharpMemberRegistry instance = XWBPCSharpMemberRegistry.Instance;
		if (!string.Equals(_catalogProjectRoot, instance.ActiveProjectRoot, StringComparison.OrdinalIgnoreCase) || _catalogProjectContextVersion != instance.ProjectContextVersion)
		{
			XWEditorInterface.Instance?.ShowToast("Mod 或 C# API 已变化，旧父类选择已取消，请重新打开类型目录。", 2);
			QueueFree();
			return;
		}
		if (GodotObject.IsInstanceValid(ContextEditor) && _catalogScriptData != ContextEditor.BpScriptData)
		{
			XWEditorInterface.Instance?.ShowToast("当前蓝图文档已变化，旧父类选择已取消，请在当前蓝图重新打开类型目录。", 2);
			QueueFree();
			return;
		}
		string text = (GodotObject.IsInstanceValid(_selectedData) ? _selectedData.ClassName : _selectedProjectClassName);
		if (!GetOkButton().Disabled && !string.IsNullOrWhiteSpace(text))
		{
			AddToRecent(text);
			EmitSignal(SignalName.ClassSelected, new StringName(text));
			QueueFree();
		}
	}

	protected override void BuildTree()
	{
		base.BuildTree();
		List<string> currentClassList = new List<string>(XWClassRegistry.Instance.GetAllClass());
		string text = (XWClassRegistry.Instance.HasClass("Object") ? "Object" : "GodotObject");
		if (XWClassRegistry.Instance.HasClass(text))
		{
			XWClassData classData = XWClassRegistry.Instance.GetClassData(text);
			TreeItem treeItem = _tree.CreateItem(Root);
			treeItem.Collapsed = false;
			treeItem.SetText(0, text);
			treeItem.SetIcon(0, XWClassRegistry.Instance.GetClassIcon(text));
			treeItem.SetMetadata(0, classData);
			BuildClassTree(currentClassList, text, treeItem);
			BuildProjectClassTree();
		}
	}

	private void BuildProjectClassTree()
	{
		XWBPCSharpMemberRegistry instance = XWBPCSharpMemberRegistry.Instance;
		if (string.IsNullOrWhiteSpace(instance.ActiveProjectRoot))
		{
			return;
		}
		if (!instance.IsProjectScanReady || !instance.IsHostScanReady)
		{
			TreeItem treeItem = _tree.CreateItem(Root);
			treeItem.Collapsed = false;
			treeItem.SetText(0, "当前 Mod C# API");
			treeItem.SetSelectable(0, selectable: false);
			treeItem.SetIcon(0, XWClassRegistry.Instance.GetUIIcon("Script"));
			TreeItem treeItem2 = _tree.CreateItem(treeItem);
			treeItem2.SetText(0, "正在后台索引可继承的 C# 类…");
			treeItem2.SetSelectable(0, selectable: false);
			BeginProjectIndexWarmup();
			return;
		}
		IReadOnlyList<XWBPCSharpMemberRegistry.CSharpClassDescriptor> projectClassDescriptors = instance.GetProjectClassDescriptors();
		if (projectClassDescriptors.Count == 0)
		{
			return;
		}
		TreeItem treeItem3 = _tree.CreateItem(Root);
		treeItem3.Collapsed = false;
		treeItem3.SetText(0, "当前 Mod C# API");
		treeItem3.SetSelectable(0, selectable: false);
		treeItem3.SetIcon(0, XWClassRegistry.Instance.GetUIIcon("Script"));
		foreach (XWBPCSharpMemberRegistry.CSharpClassDescriptor item in projectClassDescriptors)
		{
			Dictionary dictionary = new Dictionary
			{
				["xw_mod_csharp_class"] = true,
				["class_name"] = (string.IsNullOrWhiteSpace(item.QualifiedName) ? item.Name : item.QualifiedName),
				["display_name"] = item.Name,
				["qualified_name"] = item.QualifiedName,
				["base_class"] = item.BaseClass,
				["script_path"] = item.ScriptPath
			};
			TreeItem treeItem4 = _tree.CreateItem(treeItem3);
			treeItem4.Collapsed = true;
			treeItem4.SetText(0, item.CanInherit ? item.Name : (item.Name + " · 仅 API"));
			treeItem4.SetTooltipText(0, item.CanInherit ? (item.QualifiedName + "\n" + item.ScriptPath) : $"{item.QualifiedName}\n{item.ScriptPath}\n{item.InheritanceBlockReason}");
			treeItem4.SetIcon(0, XWClassRegistry.Instance.GetClassIcon(item.BaseClass) ?? XWClassRegistry.Instance.GetUIIcon("Script"));
			treeItem4.SetSelectable(0, item.CanInherit);
			if (item.CanInherit)
			{
				treeItem4.SetMetadata(0, dictionary);
			}
		}
	}

	private async void BeginProjectIndexWarmup()
	{
		XWBPCSharpMemberRegistry registry = XWBPCSharpMemberRegistry.Instance;
		if (_projectIndexRequested || (registry.IsProjectScanReady && registry.IsHostScanReady) || string.IsNullOrWhiteSpace(registry.ActiveProjectRoot))
		{
			return;
		}
		string rootSnapshot = registry.ActiveProjectRoot;
		int versionSnapshot = registry.ProjectContextVersion;
		XWBPScriptData scriptSnapshot = ContextEditor?.BpScriptData;
		_projectIndexRequested = true;
		bool ready = false;
		try
		{
			ready = await registry.EnsureProjectScannedAsync();
		}
		catch (Exception ex)
		{
			GD.PushWarning("当前 Mod C# 继承类型后台索引失败：" + ex.Message);
		}
		finally
		{
			_projectIndexRequested = false;
		}
		if (ready && GodotObject.IsInstanceValid(this) && IsInsideTree() && string.Equals(rootSnapshot, registry.ActiveProjectRoot, StringComparison.OrdinalIgnoreCase) && versionSnapshot == registry.ProjectContextVersion && scriptSnapshot == ContextEditor?.BpScriptData)
		{
			string query = (GodotObject.IsInstanceValid(_searchLineEdit) ? _searchLineEdit.Text : "");
			_selectedData = null;
			_selectedProjectClassName = "";
			BuildTree();
			FilterTree(query);
			GetOkButton().Disabled = true;
			_nameLabel.Text = "";
			_describeLabel.Text = "";
		}
	}

	private void BuildClassTree(List<string> currentClassList, string currentClass, TreeItem parentItem)
	{
		List<string> list = new List<string>(XWClassRegistry.Instance.GetInheritersFromClass(currentClass));
		list.Sort(StringComparer.OrdinalIgnoreCase);
		foreach (string item in list)
		{
			if (currentClassList.Contains(item) && !(XWClassRegistry.Instance.GetParentClass(item) != currentClass))
			{
				XWClassData classData = XWClassRegistry.Instance.GetClassData(item);
				if (GodotObject.IsInstanceValid(classData))
				{
					currentClassList.Remove(item);
					TreeItem treeItem = _tree.CreateItem(parentItem);
					treeItem.Collapsed = true;
					treeItem.SetText(0, item);
					treeItem.SetIcon(0, XWClassRegistry.Instance.GetClassIcon(item));
					treeItem.SetMetadata(0, classData);
					BuildClassTree(currentClassList, item, treeItem);
				}
			}
		}
	}

	protected override void OnItemSelected()
	{
		TreeItem selected = _tree.GetSelected();
		if (GodotObject.IsInstanceValid(selected))
		{
			Variant metadata = selected.GetMetadata(0);
			string className;
			string qualifiedName;
			string baseClass;
			string scriptPath;
			if (metadata.VariantType == Variant.Type.Object && metadata.As<GodotObject>() is XWClassData selectedData)
			{
				_selectedData = selectedData;
				_selectedProjectClassName = "";
				ShowDescription(selected);
				GetOkButton().Disabled = false;
			}
			else if (TryReadProjectClass(metadata, out className, out qualifiedName, out baseClass, out scriptPath))
			{
				_selectedData = null;
				_selectedProjectClassName = className;
				string text = (className.Contains('.') ? className.Substring(className.LastIndexOf('.') + 1) : className);
				_nameLabel.Text = text + "\n";
				_describeLabel.Text = $"[color=gray]类型:[/color] 当前 Mod C# 类\n[color=gray]完整名:[/color] {qualifiedName}\n[color=gray]基类:[/color] {baseClass}\n[color=gray]脚本:[/color] {scriptPath}";
				GetOkButton().Disabled = false;
			}
			else
			{
				_selectedData = null;
				_selectedProjectClassName = "";
				GetOkButton().Disabled = true;
			}
		}
	}

	private void ShowDescription(TreeItem item)
	{
		Variant metadata = item.GetMetadata(0);
		if (metadata.VariantType != Variant.Type.Object || !(metadata.As<GodotObject>() is XWClassData xWClassData))
		{
			_nameLabel.Text = "";
			_describeLabel.Text = "";
			return;
		}
		string text = (xWClassData.IsGodotClass ? "Godot 内置类" : (xWClassData.IsGlobalClass ? "全局脚本类" : "脚本类"));
		string parentClass = XWClassRegistry.Instance.GetParentClass(xWClassData.ClassName);
		_nameLabel.Text = xWClassData.ClassName + "\n";
		_describeLabel.Text = "[color=gray]类型:[/color] " + text + "\n[color=gray]基类:[/color] " + parentClass;
		if (!string.IsNullOrEmpty(xWClassData.ScriptPath))
		{
			RichTextLabel describeLabel = _describeLabel;
			describeLabel.Text = describeLabel.Text + "\n[color=gray]脚本:[/color] " + xWClassData.ScriptPath;
		}
	}

	protected override bool CanFilterTreeItem(string lowerQuery, TreeItem treeItem)
	{
		string text = treeItem.GetText(0);
		if (!string.IsNullOrEmpty(text) && text.ToLower().Contains(lowerQuery))
		{
			return true;
		}
		Variant metadata = treeItem.GetMetadata(0);
		if (metadata.VariantType == Variant.Type.Object && metadata.As<GodotObject>() is XWClassData xWClassData)
		{
			if (!xWClassData.ClassName.ToLower().Contains(lowerQuery) && !xWClassData.BaseClass.ToLower().Contains(lowerQuery))
			{
				return xWClassData.ScriptPath.ToLower().Contains(lowerQuery);
			}
			return true;
		}
		if (TryReadProjectClass(metadata, out var className, out var qualifiedName, out var baseClass, out var scriptPath))
		{
			if (!className.ToLowerInvariant().Contains(lowerQuery) && !qualifiedName.ToLowerInvariant().Contains(lowerQuery) && !baseClass.ToLowerInvariant().Contains(lowerQuery))
			{
				return scriptPath.ToLowerInvariant().Contains(lowerQuery);
			}
			return true;
		}
		return false;
	}

	private static bool TryReadProjectClass(Variant metadata, out string className, out string qualifiedName, out string baseClass, out string scriptPath)
	{
		className = "";
		qualifiedName = "";
		baseClass = "";
		scriptPath = "";
		if (metadata.VariantType != Variant.Type.Dictionary)
		{
			return false;
		}
		Dictionary dictionary = metadata.As<Dictionary>();
		if (!dictionary.TryGetValue("xw_mod_csharp_class", out var value) || !value.AsBool())
		{
			return false;
		}
		className = (dictionary.TryGetValue("class_name", out var value2) ? value2.AsString() : "");
		qualifiedName = (dictionary.TryGetValue("qualified_name", out var value3) ? value3.AsString() : className);
		baseClass = (dictionary.TryGetValue("base_class", out var value4) ? value4.AsString() : "");
		scriptPath = (dictionary.TryGetValue("script_path", out var value5) ? value5.AsString() : "");
		return !string.IsNullOrWhiteSpace(className);
	}
}
