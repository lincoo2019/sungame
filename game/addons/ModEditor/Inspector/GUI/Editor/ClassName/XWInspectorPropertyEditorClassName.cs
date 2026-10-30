using System.Collections.Generic;
using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorClassName : XWInspectorPropertyEditorBase
{
	private const string ScenePath = "res://addons/ModEditor/Inspector/GUI/Editor/ClassName/XWInspectorPropertyEditorClassName.tscn";

	private LineEdit _lineEdit;

	private Button _browseButton;

	private PopupPanel _classPopup;

	private ItemList _classList;

	private LineEdit _classSearch;

	public static XWInspectorPropertyEditorClassName Create()
	{
		return ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Inspector/GUI/Editor/ClassName/XWInspectorPropertyEditorClassName.tscn", null, ResourceLoader.CacheMode.Reuse).Instantiate<XWInspectorPropertyEditorClassName>(PackedScene.GenEditState.Disabled);
	}

	public override void _Ready()
	{
		base._Ready();
		_lineEdit = GetNode<LineEdit>("%LineEdit");
		_browseButton = GetNode<Button>("%BrowseButton");
		_classPopup = GetNode<PopupPanel>("%ClassPopup");
		_classList = GetNode<ItemList>("%ClassList");
		_classSearch = GetNode<LineEdit>("%ClassSearch");
		_lineEdit.TextChanged += OnTextChanged;
		_browseButton.Pressed += OnBrowsePressed;
		_classSearch.TextChanged += OnSearchChanged;
		_classList.ItemSelected += OnClassSelected;
		PopulateClassList("");
	}

	public override void UpdateValue()
	{
		Variant propertyValue = GetPropertyValue();
		if (propertyValue.VariantType != Variant.Type.Nil)
		{
			XWInspectorPropertyEditorBase.SetLineEditTextPreservingCaret(_lineEdit, propertyValue.AsString());
		}
	}

	public override Variant GetValue()
	{
		return _lineEdit.Text;
	}

	private void PopulateClassList(string filter)
	{
		_classList.Clear();
		string[] classList = ClassDB.GetClassList();
		List<string> list = new List<string>();
		string[] array = classList;
		foreach (string text in array)
		{
			list.Add(text.ToString());
		}
		list.Sort();
		string value = filter.ToLower();
		foreach (string item in list)
		{
			if (string.IsNullOrEmpty(filter) || item.ToLower().Contains(value))
			{
				_classList.AddItem(item);
			}
		}
	}

	private void OnTextChanged(string newText)
	{
		ValueChange(newText);
	}

	private void OnBrowsePressed()
	{
		_classSearch.Text = "";
		PopulateClassList("");
		_classPopup.Position = new Vector2I((int)GetGlobalMousePosition().X, (int)GetGlobalMousePosition().Y);
		_classPopup.Popup();
	}

	private void OnSearchChanged(string filter)
	{
		PopulateClassList(filter);
	}

	private void OnClassSelected(long index)
	{
		int num = (int)index;
		if (num >= 0 && num < _classList.ItemCount)
		{
			string itemText = _classList.GetItemText(num);
			_lineEdit.Text = itemText;
			ValueChange(itemText);
			_classPopup.Hide();
		}
	}
}
