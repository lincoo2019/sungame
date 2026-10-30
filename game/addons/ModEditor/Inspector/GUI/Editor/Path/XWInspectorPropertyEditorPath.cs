using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorPath : XWInspectorPropertyEditorBase
{
	private const string ScenePath = "res://addons/ModEditor/Inspector/GUI/Editor/Path/XWInspectorPropertyEditorPath.tscn";

	private static readonly Texture2D IconFileBrowse = XWTextureSafety.SafeIcon(ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/ClassIcon/FileBrowse.svg", null, ResourceLoader.CacheMode.Reuse));

	private static readonly Texture2D IconFolderBrowse = XWTextureSafety.SafeIcon(ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/ClassIcon/FolderBrowse.svg", null, ResourceLoader.CacheMode.Reuse));

	private LineEdit _lineEdit;

	private Button _browseButton;

	private FileDialog _fileDialog;

	private PropertyHint _hint = PropertyHint.File;

	public static XWInspectorPropertyEditorPath Create()
	{
		return ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Inspector/GUI/Editor/Path/XWInspectorPropertyEditorPath.tscn", null, ResourceLoader.CacheMode.Reuse).Instantiate<XWInspectorPropertyEditorPath>(PackedScene.GenEditState.Disabled);
	}

	public override void _Ready()
	{
		base._Ready();
		_lineEdit = GetNode<LineEdit>("%LineEdit");
		_browseButton = GetNode<Button>("%BrowseButton");
		_fileDialog = GetNode<FileDialog>("%FileDialog");
		_lineEdit.TextChanged += OnTextChanged;
		_lineEdit.TextSubmitted += OnTextSubmitted;
		_lineEdit.FocusExited += OnFocusExited;
		_lineEdit.SetDragForwarding(Callable.From<Vector2, Variant>(GetDragData), Callable.From<Vector2, Variant, bool>(CanDropDataForwarded), Callable.From<Vector2, Variant>(DropDataForwarded));
		_browseButton.Pressed += OnBrowsePressed;
		_fileDialog.FileSelected += OnFileSelected;
		_fileDialog.DirSelected += OnDirSelected;
		UpdateBrowseButtonIcon();
	}

	public override void SetEditProperty(XWInspectorProperty property, StringName field = null)
	{
		_hint = property.Hint;
		SetupFileDialog();
		base.SetEditProperty(property, field);
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

	private void SetupFileDialog()
	{
		PropertyHint hint = _hint;
		if (hint <= PropertyHint.SaveFile)
		{
			PropertyHint num = hint - 13;
			if ((ulong)num <= 3uL)
			{
				switch ((int)num)
				{
				case 1:
					goto IL_0052;
				case 3:
					goto IL_0071;
				case 0:
					goto IL_0090;
				case 2:
					goto IL_00ac;
				}
			}
			if (hint != PropertyHint.SaveFile)
			{
				goto IL_0100;
			}
			_fileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
			_fileDialog.Access = FileDialog.AccessEnum.Resources;
		}
		else
		{
			if (hint != PropertyHint.GlobalSaveFile)
			{
				if (hint == PropertyHint.FilePath)
				{
					goto IL_0090;
				}
				goto IL_0100;
			}
			_fileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
			_fileDialog.Access = FileDialog.AccessEnum.Filesystem;
		}
		goto IL_011a;
		IL_00ac:
		_fileDialog.FileMode = FileDialog.FileModeEnum.OpenFile;
		_fileDialog.Access = FileDialog.AccessEnum.Filesystem;
		goto IL_011a;
		IL_011a:
		_fileDialog.Filters = System.Array.Empty<string>();
		if (GodotObject.IsInstanceValid(Property) && Property.HintString != "")
		{
			string[] array = ParseFilters(Property.HintString);
			if (array.Length != 0)
			{
				_fileDialog.Filters = array;
			}
		}
		UpdateBrowseButtonIcon();
		return;
		IL_0100:
		_fileDialog.FileMode = FileDialog.FileModeEnum.OpenFile;
		_fileDialog.Access = FileDialog.AccessEnum.Resources;
		goto IL_011a;
		IL_0052:
		_fileDialog.FileMode = FileDialog.FileModeEnum.OpenDir;
		_fileDialog.Access = FileDialog.AccessEnum.Resources;
		goto IL_011a;
		IL_0071:
		_fileDialog.FileMode = FileDialog.FileModeEnum.OpenDir;
		_fileDialog.Access = FileDialog.AccessEnum.Filesystem;
		goto IL_011a;
		IL_0090:
		_fileDialog.FileMode = FileDialog.FileModeEnum.OpenFile;
		_fileDialog.Access = FileDialog.AccessEnum.Resources;
		goto IL_011a;
	}

	private void OnTextChanged(string newText)
	{
		ValueChange(newText);
	}

	private void OnTextSubmitted(string newText)
	{
		CommitPath(newText);
	}

	private void OnFocusExited()
	{
		CommitPath(_lineEdit.Text);
	}

	private void OnBrowsePressed()
	{
		SetupFileDialog();
		if (_lineEdit.Text != "")
		{
			if (_hint == PropertyHint.Dir || _hint == PropertyHint.GlobalDir)
			{
				_fileDialog.CurrentDir = _lineEdit.Text;
			}
			else
			{
				_fileDialog.CurrentPath = _lineEdit.Text;
			}
		}
		_fileDialog.PopupCenteredClamped(new Vector2I(800, 600), 0.9f);
	}

	private void OnFileSelected(string path)
	{
		CommitPath(path);
	}

	private void OnDirSelected(string path)
	{
		CommitPath(path);
	}

	private void CommitPath(string path)
	{
		_lineEdit.Text = path;
		ValueChange(path);
	}

	private void UpdateBrowseButtonIcon()
	{
		if (GodotObject.IsInstanceValid(_browseButton))
		{
			bool flag = IsFolderHint(_hint);
			_browseButton.Icon = (flag ? IconFolderBrowse : IconFileBrowse);
			_browseButton.TooltipText = (flag ? "选择文件夹" : "选择文件");
			_browseButton.CustomMinimumSize = new Vector2(30f, 28f);
			_browseButton.Flat = true;
		}
	}

	private static bool IsFolderHint(PropertyHint hint)
	{
		if (hint != PropertyHint.Dir)
		{
			return hint == PropertyHint.GlobalDir;
		}
		return true;
	}

	private static string[] ParseFilters(string hintString)
	{
		if (string.IsNullOrWhiteSpace(hintString))
		{
			return System.Array.Empty<string>();
		}
		string[] array = hintString.Split(",");
		List<string> list = new List<string>();
		string[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			string text = array2[i].Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				list.Add(text);
			}
		}
		return list.ToArray();
	}

	private Variant GetDragData(Vector2 atPosition)
	{
		return default(Variant);
	}

	private bool CanDropDataForwarded(Vector2 atPosition, Variant data)
	{
		string text = ExtractDropPath(data);
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (IsFolderHint(_hint))
		{
			if (!DirAccess.DirExistsAbsolute(text))
			{
				return text.EndsWith("/");
			}
			return true;
		}
		if (!FileAccess.FileExists(text) && !ResourceLoader.Exists(text))
		{
			return false;
		}
		if (!GodotObject.IsInstanceValid(Property) || string.IsNullOrWhiteSpace(Property.HintString))
		{
			return true;
		}
		string extension = text.GetExtension();
		string[] array = ParseFilters(Property.HintString);
		for (int i = 0; i < array.Length; i++)
		{
			string text2 = array[i].Split(';')[0].Trim();
			if (text2 == "*.*" || text2 == "*")
			{
				return true;
			}
			if (text2.StartsWith("*.") && extension.Equals(text2.Substring(2), StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	private void DropDataForwarded(Vector2 atPosition, Variant data)
	{
		string text = ExtractDropPath(data);
		if (!string.IsNullOrWhiteSpace(text))
		{
			CommitPath(text);
		}
	}

	private static string ExtractDropPath(Variant data)
	{
		if (data.VariantType != Variant.Type.Dictionary)
		{
			return "";
		}
		Dictionary dictionary = data.As<Dictionary>();
		if (dictionary.ContainsKey("files"))
		{
			return ExtractFirstPath(dictionary["files"]);
		}
		if (dictionary.ContainsKey("paths"))
		{
			return ExtractFirstPath(dictionary["paths"]);
		}
		if (dictionary.ContainsKey("path"))
		{
			return dictionary["path"].AsString();
		}
		return "";
	}

	private static string ExtractFirstPath(Variant value)
	{
		if (value.VariantType == Variant.Type.PackedStringArray)
		{
			string[] array = value.AsStringArray();
			if (array.Length == 0)
			{
				return "";
			}
			return array[0];
		}
		if (value.VariantType == Variant.Type.Array)
		{
			Godot.Collections.Array array2 = value.As<Godot.Collections.Array>();
			if (array2.Count <= 0)
			{
				return "";
			}
			return array2[0].AsString();
		}
		return value.AsString();
	}
}
