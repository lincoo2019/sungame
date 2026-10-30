using System.Collections.Generic;
using Godot;
using PVZHE.ModEditor.Registry;
using PVZHE.ModEditor.Registry.Class;

namespace PVZHE.ModEditor.GUI;

public partial class XWWindowTypeSelector : XWWindowSelector
{
	[Signal]
	public delegate void TypeSelectEventHandler(Variant.Type type, StringName className);

	private const string ScenePath = "res://addons/ModEditor/Window/Selector/Type/XWWindowTypeSelector.tscn";

	public Variant SelectData;

	public static XWWindowTypeSelector Create()
	{
		return ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Window/Selector/Type/XWWindowTypeSelector.tscn", null, ResourceLoader.CacheMode.Reuse).Instantiate<XWWindowTypeSelector>(PackedScene.GenEditState.Disabled);
	}

	public override void _Ready()
	{
		base._Ready();
	}

	protected override void OnCloseRequested()
	{
		QueueFree();
	}

	protected override void OnConfirmed()
	{
		Button okButton = GetOkButton();
		if (GodotObject.IsInstanceValid(okButton) && okButton.Disabled)
		{
			return;
		}
		if (SelectData.VariantType == Variant.Type.Object)
		{
			GodotObject godotObject = SelectData.As<GodotObject>();
			if (godotObject is XWClassData xWClassData)
			{
				EmitSignal(SignalName.TypeSelect, 24, xWClassData.ClassName);
			}
			else if (godotObject is XWTypeData xWTypeData)
			{
				EmitSignal(SignalName.TypeSelect, (int)xWTypeData.Type, new StringName(""));
			}
		}
		QueueFree();
	}

	protected override void BuildTree()
	{
		base.BuildTree();
		List<string> currentClassList = new List<string>(XWClassRegistry.Instance.GetAllClass());
		string text = "Object";
		TreeItem treeItem = _tree.CreateItem(Root);
		treeItem.Collapsed = true;
		XWClassData xWClassData = (XWClassRegistry.Instance.HasClass(text) ? XWClassRegistry.Instance.GetClassData(text) : null);
		treeItem.SetText(0, text);
		Texture2D classIcon = XWClassRegistry.Instance.GetClassIcon(text);
		if (GodotObject.IsInstanceValid(classIcon))
		{
			treeItem.SetIcon(0, classIcon);
		}
		if (GodotObject.IsInstanceValid(xWClassData))
		{
			treeItem.SetMetadata(0, xWClassData);
		}
		BuildClassTree(currentClassList, text, treeItem);
		foreach (XWTypeData item in XWTypeRegistry.Instance.GetAllType())
		{
			TreeItem treeItem2 = _tree.CreateItem(Root);
			treeItem2.SetText(0, item.Name);
			if (GodotObject.IsInstanceValid(item.Icon))
			{
				treeItem2.SetIcon(0, item.Icon);
			}
			treeItem2.SetMetadata(0, item);
		}
	}

	private void BuildClassTree(List<string> currentClassList, string currentClass, TreeItem classTreeItem)
	{
		List<string> list = new List<string>(XWClassRegistry.Instance.GetInheritersFromClass(currentClass));
		while (list.Count > 0)
		{
			string text = list[0];
			list.RemoveAt(0);
			if (currentClassList.Contains(text) && !(XWClassRegistry.Instance.GetParentClass(text) != currentClass))
			{
				XWClassData xWClassData = (XWClassRegistry.Instance.HasClass(text) ? XWClassRegistry.Instance.GetClassData(text) : null);
				currentClassList.Remove(text);
				TreeItem treeItem = _tree.CreateItem(classTreeItem);
				treeItem.Collapsed = true;
				treeItem.SetText(0, text);
				Texture2D classIcon = XWClassRegistry.Instance.GetClassIcon(text);
				if (GodotObject.IsInstanceValid(classIcon))
				{
					treeItem.SetIcon(0, classIcon);
				}
				if (GodotObject.IsInstanceValid(xWClassData))
				{
					treeItem.SetMetadata(0, xWClassData);
				}
				BuildClassTree(currentClassList, text, treeItem);
			}
		}
	}

	protected override void OnItemSelected()
	{
		TreeItem selected = _tree.GetSelected();
		if (!GodotObject.IsInstanceValid(selected))
		{
			return;
		}
		Variant metadata = selected.GetMetadata(0);
		if (metadata.VariantType == Variant.Type.Object)
		{
			GodotObject godotObject = metadata.As<GodotObject>();
			if (godotObject is XWClassData || godotObject is XWTypeData)
			{
				SelectData = metadata;
				ShowDescription(selected);
				GetOkButton().Disabled = false;
				return;
			}
		}
		GetOkButton().Disabled = true;
	}

	protected void ShowDescription(TreeItem item)
	{
		Variant metadata = item.GetMetadata(0);
		if (metadata.VariantType == Variant.Type.Object)
		{
			GodotObject godotObject = metadata.As<GodotObject>();
			if (godotObject is XWClassData xWClassData)
			{
				_nameLabel.Text = xWClassData.ClassName + "\n";
			}
			else if (godotObject is XWTypeData xWTypeData)
			{
				_nameLabel.Text = xWTypeData.Name + "\n";
			}
		}
		else
		{
			_nameLabel.Text = "";
			_describeLabel.Text = "";
		}
	}

	protected override bool CanFilterTreeItem(string lowerQuery, TreeItem treeItem)
	{
		if (treeItem.GetText(0).ToLower().Contains(lowerQuery))
		{
			return true;
		}
		Variant metadata = treeItem.GetMetadata(0);
		if (metadata.VariantType == Variant.Type.Object)
		{
			GodotObject godotObject = metadata.As<GodotObject>();
			if (godotObject is XWClassData xWClassData && xWClassData.ClassName.ToLower().Contains(lowerQuery))
			{
				return true;
			}
			if (godotObject is XWTypeData xWTypeData && xWTypeData.Name.ToLower().Contains(lowerQuery))
			{
				return true;
			}
		}
		return false;
	}
}
