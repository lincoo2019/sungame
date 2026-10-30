using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using PVZHE.ModEditor.FileSystem;

namespace PVZHE.ModEditor.GUI;

public partial class XWQuickOpenDialog : ConfirmationDialog
{
	[Signal]
	public delegate void FileOpenedEventHandler(string path);

	private const int PageSize = 120;

	private OptionButton _typeFilter;

	private LineEdit _searchLine;

	private ItemList _resultList;

	private Label _resultCount;

	private Button[] _typeChips;

	private readonly List<(string Name, string Path)> _matches = new List<(string, string)>();

	private Button _previousPageButton;

	private Button _nextPageButton;

	private Label _pageLabel;

	private int _selectedType;

	private int _currentPage;

	public override void _Ready()
	{
		_typeFilter = GetNode<OptionButton>("%TypeFilter");
		_searchLine = GetNode<LineEdit>("%SearchLine");
		_resultList = GetNode<ItemList>("%ResultList");
		_resultCount = GetNode<Label>("%ResultCount");
		_previousPageButton = GetNode<Button>("%PreviousPageButton");
		_nextPageButton = GetNode<Button>("%NextPageButton");
		_pageLabel = GetNode<Label>("%PageLabel");
		_typeChips = new Button[4]
		{
			GetNode<Button>("%AllTypeChip"),
			GetNode<Button>("%ScriptTypeChip"),
			GetNode<Button>("%SceneTypeChip"),
			GetNode<Button>("%ResourceTypeChip")
		};
		_searchLine.TextChanged += delegate
		{
			FilterItems();
		};
		_searchLine.GuiInput += OnSearchGuiInput;
		_resultList.ItemActivated += delegate
		{
			OnConfirmed();
		};
		_previousPageButton.Pressed += delegate
		{
			ChangePage(-1);
		};
		_nextPageButton.Pressed += delegate
		{
			ChangePage(1);
		};
		base.Confirmed += OnConfirmed;
		BuildTypeFilter();
		BindTypeChips();
	}

	private void BuildTypeFilter()
	{
		_typeFilter.Clear();
		_typeFilter.AddItem("全部", 0);
		_typeFilter.AddItem("脚本", 1);
		_typeFilter.AddItem("场景", 2);
		_typeFilter.AddItem("资源", 3);
	}

	private void BindTypeChips()
	{
		ButtonGroup buttonGroup = new ButtonGroup
		{
			AllowUnpress = false
		};
		for (int i = 0; i < _typeChips.Length; i++)
		{
			int type = i;
			Button obj = _typeChips[i];
			obj.ButtonGroup = buttonGroup;
			obj.Pressed += delegate
			{
				SelectType(type);
			};
		}
		_typeFilter.ItemSelected += delegate(long index)
		{
			SelectType((int)index);
		};
		SelectType(0);
	}

	private void SelectType(int type)
	{
		_selectedType = Mathf.Clamp(type, 0, _typeChips.Length - 1);
		_typeFilter.Select(_selectedType);
		for (int i = 0; i < _typeChips.Length; i++)
		{
			_typeChips[i].SetPressedNoSignal(i == _selectedType);
		}
		FilterItems();
	}

	public void Refresh()
	{
		FilterItems();
	}

	private void FilterItems()
	{
		_matches.Clear();
		string text = _searchLine.Text;
		XWFileSystemDirectory filesystem = XWFileSystem.GetSingleton().GetFilesystem();
		if (filesystem == null)
		{
			_resultCount.Text = "0 个结果";
			RenderResultPage();
		}
		else
		{
			CollectMatching(filesystem, text);
			_currentPage = 0;
			RenderResultPage();
		}
	}

	private void CollectMatching(XWFileSystemDirectory dir, string search)
	{
		foreach (XWFileInfo file in dir.Files)
		{
			if ((string.IsNullOrEmpty(search) || file.Name.Contains(search, StringComparison.OrdinalIgnoreCase)) && MatchesSelectedType(file.Path))
			{
				_matches.Add((file.Name, file.Path));
			}
		}
		foreach (XWFileSystemDirectory subdir in dir.Subdirs)
		{
			CollectMatching(subdir, search);
		}
	}

	private void RenderResultPage()
	{
		_resultList.Clear();
		int num = Math.Max(1, (_matches.Count + 120 - 1) / 120);
		_currentPage = Mathf.Clamp(_currentPage, 0, num - 1);
		int num2 = _currentPage * 120;
		int num3 = Math.Min(num2 + 120, _matches.Count);
		for (int i = num2; i < num3; i++)
		{
			(string, string) tuple = _matches[i];
			int idx = _resultList.AddItem(tuple.Item1, GetFileTypeIcon(tuple.Item2));
			_resultList.SetItemMetadata(idx, tuple.Item2);
		}
		_resultCount.Text = $"{_matches.Count} 个结果 · 当前 {num3 - num2} 个";
		_pageLabel.Text = $"第 {_currentPage + 1} / {num} 页";
		_previousPageButton.Disabled = _currentPage <= 0;
		_nextPageButton.Disabled = _currentPage >= num - 1;
	}

	private void ChangePage(int direction)
	{
		_currentPage += direction;
		RenderResultPage();
		if (_resultList.ItemCount > 0)
		{
			_resultList.Select(0);
			_resultList.GrabFocus();
		}
	}

	private void OnSearchGuiInput(InputEvent inputEvent)
	{
		if (inputEvent is InputEventKey { Pressed: not false, Echo: false } inputEventKey && inputEventKey.Keycode == Key.Down && _resultList.ItemCount > 0)
		{
			_resultList.Select(0);
			_resultList.GrabFocus();
			_searchLine.AcceptEvent();
		}
	}

	private bool MatchesSelectedType(string path)
	{
		if (_selectedType == 0)
		{
			return true;
		}
		string text = Path.GetExtension(path)?.ToLowerInvariant() ?? "";
		return _selectedType switch
		{
			1 => text == ".cs", 
			2 => (text == ".tscn" || text == ".scn") ? true : false, 
			3 => (text == ".tres" || text == ".res") ? true : false, 
			_ => true, 
		};
	}

	private static Texture2D GetFileTypeIcon(string path)
	{
		string path2;
		switch (Path.GetExtension(path)?.ToLowerInvariant() ?? "")
		{
		case ".cs":
			path2 = "res://addons/ModEditor/Icons/Script.svg";
			break;
		case ".tscn":
		case ".scn":
			path2 = "res://addons/ModEditor/Icons/PackedScene.svg";
			break;
		case ".tres":
		case ".res":
			path2 = "res://addons/ModEditor/Icons/FileThumbnail.svg";
			break;
		default:
			path2 = "res://addons/ModEditor/Icons/File.svg";
			break;
		}
		return ResourceLoader.Load<Texture2D>(path2, null, ResourceLoader.CacheMode.Reuse);
	}

	private void OnConfirmed()
	{
		int[] selectedItems = _resultList.GetSelectedItems();
		if (selectedItems.Length != 0)
		{
			string text = _resultList.GetItemMetadata(selectedItems[0]).AsString();
			EmitSignal(SignalName.FileOpened, text);
			Hide();
		}
	}
}
