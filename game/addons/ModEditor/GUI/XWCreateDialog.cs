using System;
using System.Collections.Generic;
using Godot;
using PVZHE.ModEditor.Registry.Class;

namespace PVZHE.ModEditor.GUI;

public partial class XWCreateDialog : ConfirmationDialog
{
	[Signal]
	public delegate void CreatedEventHandler(string typeHint);

	private const int TypePageSize = 200;

	private LineEdit _searchEdit;

	private OptionButton _typeFilter;

	private Tree _typeTree;

	private VBoxContainer _previewContainer;

	private TextureRect _typeIcon;

	private Label _selectedTypeLabel;

	private Label _inheritanceLabel;

	private Label _scriptPathLabel;

	private Button[] _typeChips;

	private Button _previousTypePageButton;

	private Button _nextTypePageButton;

	private Label _typePageLabel;

	private bool _resourceOnly;

	private int _selectedTypeMode;

	private int _currentTypePage;

	public override void _Ready()
	{
		_searchEdit = GetNode<LineEdit>("%SearchEdit");
		_typeFilter = GetNode<OptionButton>("%TypeFilter");
		_typeTree = GetNode<Tree>("%TypeTree");
		_previewContainer = GetNode<VBoxContainer>("%PreviewContainer");
		_typeIcon = GetNode<TextureRect>("%TypeIcon");
		_selectedTypeLabel = GetNode<Label>("%SelectedTypeLabel");
		_inheritanceLabel = GetNode<Label>("%InheritanceLabel");
		_scriptPathLabel = GetNode<Label>("%ScriptPathLabel");
		_previousTypePageButton = GetNode<Button>("%PreviousTypePageButton");
		_nextTypePageButton = GetNode<Button>("%NextTypePageButton");
		_typePageLabel = GetNode<Label>("%TypePageLabel");
		_typeChips = new Button[3]
		{
			GetNode<Button>("%AllTypeChip"),
			GetNode<Button>("%ResourceTypeChip"),
			GetNode<Button>("%NodeTypeChip")
		};
		_searchEdit.TextChanged += delegate
		{
			ResetTypePageAndRefresh();
		};
		_searchEdit.GuiInput += OnSearchGuiInput;
		_typeFilter.ItemSelected += delegate(long index)
		{
			SelectTypeMode((int)index);
		};
		_typeTree.ItemSelected += UpdateSelectionPreview;
		_typeTree.ItemActivated += delegate
		{
			OnConfirmed();
		};
		_previousTypePageButton.Pressed += delegate
		{
			ChangeTypePage(-1);
		};
		_nextTypePageButton.Pressed += delegate
		{
			ChangeTypePage(1);
		};
		base.Confirmed += OnConfirmed;
		BuildTypeFilter();
		BindTypeChips();
		RefreshTree();
	}

	public void ConfigureForResources()
	{
		_resourceOnly = true;
		base.Title = "新建其他资源";
		if (GodotObject.IsInstanceValid(_typeFilter))
		{
			SelectTypeMode(1);
			_typeFilter.Disabled = true;
			Button[] typeChips = _typeChips;
			for (int i = 0; i < typeChips.Length; i++)
			{
				typeChips[i].Disabled = true;
			}
		}
		if (IsNodeReady())
		{
			RefreshTree();
		}
	}

	private void BuildTypeFilter()
	{
		_typeFilter.Clear();
		_typeFilter.AddItem("全部", 0);
		_typeFilter.AddItem("资源", 1);
		_typeFilter.AddItem("节点", 2);
	}

	private void BindTypeChips()
	{
		for (int i = 0; i < _typeChips.Length; i++)
		{
			int mode = i;
			_typeChips[i].Pressed += delegate
			{
				SelectTypeMode(mode);
			};
		}
		SelectTypeMode(_resourceOnly ? 1 : 0);
	}

	private void SelectTypeMode(int mode)
	{
		int max = ((_typeChips == null) ? 2 : (_typeChips.Length - 1));
		_selectedTypeMode = (_resourceOnly ? 1 : Mathf.Clamp(mode, 0, max));
		_currentTypePage = 0;
		_typeFilter?.Select(_selectedTypeMode);
		if (_typeChips != null)
		{
			for (int i = 0; i < _typeChips.Length; i++)
			{
				_typeChips[i].SetPressedNoSignal(i == _selectedTypeMode);
			}
		}
		if (IsNodeReady())
		{
			RefreshTree();
		}
	}

	private void RefreshTree()
	{
		_typeTree.Clear();
		TreeItem treeItem = _typeTree.CreateItem();
		string value = _searchEdit.Text?.StripEdges() ?? "";
		int num = (_resourceOnly ? 1 : _selectedTypeMode);
		List<string> list = new List<string>();
		foreach (XWClassData allClassDatum in XWClassRegistry.Instance.GetAllClassData())
		{
			bool flag = XWClassRegistry.Instance.IsParentClass(allClassDatum.ClassName, "Resource");
			bool flag2 = XWClassRegistry.Instance.IsParentClass(allClassDatum.ClassName, "Node");
			if ((num != 1 || flag) && (num != 2 || flag2) && (num != 1 || allClassDatum.IsGlobalClass) && XWClassRegistry.Instance.CanInstantiate(allClassDatum.ClassName) && (string.IsNullOrEmpty(value) || allClassDatum.ClassName.Contains(value, StringComparison.OrdinalIgnoreCase)))
			{
				list.Add(allClassDatum.ClassName);
			}
		}
		list.Sort(StringComparer.OrdinalIgnoreCase);
		int num2 = Math.Max(1, (list.Count + 200 - 1) / 200);
		_currentTypePage = Mathf.Clamp(_currentTypePage, 0, num2 - 1);
		int num3 = _currentTypePage * 200;
		int num4 = Math.Min(num3 + 200, list.Count);
		for (int i = num3; i < num4; i++)
		{
			string text = list[i];
			TreeItem treeItem2 = _typeTree.CreateItem(treeItem);
			treeItem2.SetText(0, text);
			treeItem2.SetMetadata(0, text);
			treeItem2.SetIcon(0, XWClassRegistry.Instance.GetClassIcon(text));
		}
		_typePageLabel.Text = $"第 {_currentTypePage + 1} / {num2} 页 · {list.Count} 项";
		_previousTypePageButton.Disabled = _currentTypePage <= 0;
		_nextTypePageButton.Disabled = _currentTypePage >= num2 - 1;
		if (treeItem.GetFirstChild() != null)
		{
			treeItem.GetFirstChild().Select(0);
			UpdateSelectionPreview();
		}
		else
		{
			ClearSelectionPreview("没有匹配的可创建类型");
		}
	}

	private void ResetTypePageAndRefresh()
	{
		_currentTypePage = 0;
		RefreshTree();
	}

	private void ChangeTypePage(int direction)
	{
		_currentTypePage += direction;
		RefreshTree();
		_typeTree.GrabFocus();
	}

	private void OnSearchGuiInput(InputEvent inputEvent)
	{
		if (inputEvent is InputEventKey { Pressed: not false, Echo: false } inputEventKey && inputEventKey.Keycode == Key.Down && _typeTree.GetSelected() != null)
		{
			_typeTree.GrabFocus();
			_searchEdit.AcceptEvent();
		}
	}

	private void UpdateSelectionPreview()
	{
		TreeItem selected = _typeTree.GetSelected();
		if (selected == null)
		{
			ClearSelectionPreview("请选择资源类型");
			return;
		}
		string text = selected.GetMetadata(0).AsString();
		XWClassData classData = XWClassRegistry.Instance.GetClassData(text);
		_typeIcon.Texture = XWClassRegistry.Instance.GetClassIcon(text);
		_selectedTypeLabel.Text = text;
		_inheritanceLabel.Text = "继承：" + BuildInheritancePath(text);
		_scriptPathLabel.Text = classData?.ScriptFile?.ResourcePath ?? "Godot 内置类型";
	}

	private static string BuildInheritancePath(string className)
	{
		List<string> list = new List<string>();
		string text = className;
		for (int i = 0; i < 12; i++)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				break;
			}
			list.Add(text);
			if (text == "Resource" || text == "Node")
			{
				break;
			}
			text = XWClassRegistry.Instance.GetParentClass(text);
		}
		return string.Join("  →  ", list);
	}

	private void ClearSelectionPreview(string message)
	{
		_typeIcon.Texture = null;
		_selectedTypeLabel.Text = message;
		_inheritanceLabel.Text = "";
		_scriptPathLabel.Text = "";
	}

	private void OnConfirmed()
	{
		TreeItem selected = _typeTree.GetSelected();
		if (selected != null)
		{
			string text = selected.GetMetadata(0).AsString();
			EmitSignal(SignalName.Created, text);
		}
	}
}
