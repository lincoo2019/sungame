using System;
using System.Collections.Generic;
using Godot;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWObjectPoolVisualPicker : VBoxContainer
{
	private const int PageSize = 18;

	private readonly List<XWObjectPoolVisualChoice> _choices = new List<XWObjectPoolVisualChoice>();

	private readonly List<XWObjectPoolVisualChoice> _filteredChoices = new List<XWObjectPoolVisualChoice>();

	private LineEdit _searchEdit;

	private ItemList _choiceList;

	private Label _selectedLabel;

	private Label _pageLabel;

	private Button _previousButton;

	private Button _nextButton;

	private int _selectedId;

	private int _page;

	private bool _updating;

	public int SelectedChoiceId => _selectedId;

	public int TotalChoiceCount => _choices.Count;

	public int VisibleChoiceCount
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_choiceList))
			{
				return 0;
			}
			return _choiceList.ItemCount;
		}
	}

	public int PageCount => Math.Max(1, Mathf.CeilToInt((float)_filteredChoices.Count / 18f));

	public bool HasSearchAndPaging
	{
		get
		{
			if (GodotObject.IsInstanceValid(_searchEdit) && GodotObject.IsInstanceValid(_previousButton))
			{
				return GodotObject.IsInstanceValid(_nextButton);
			}
			return false;
		}
	}

	public event Action<int> ChoiceSelected;

	public override void _Ready()
	{
		_searchEdit = GetNode<LineEdit>("%SearchEdit");
		_choiceList = GetNode<ItemList>("%ChoiceList");
		_selectedLabel = GetNode<Label>("%SelectedLabel");
		_pageLabel = GetNode<Label>("%PageLabel");
		_previousButton = GetNode<Button>("%PreviousButton");
		_nextButton = GetNode<Button>("%NextButton");
		_searchEdit.TextChanged += OnSearchChanged;
		_choiceList.ItemSelected += OnItemSelected;
		_previousButton.Pressed += delegate
		{
			ChangePage(-1);
		};
		_nextButton.Pressed += delegate
		{
			ChangePage(1);
		};
		RebuildFilteredChoices();
	}

	public void Configure(IEnumerable<XWObjectPoolVisualChoice> choices, int selectedId)
	{
		_choices.Clear();
		if (choices != null)
		{
			_choices.AddRange(choices);
		}
		_selectedId = selectedId;
		_page = 0;
		RebuildFilteredChoices();
	}

	public void SetSelectedId(int selectedId)
	{
		_selectedId = selectedId;
		RefreshSelectedLabel();
		SelectVisibleChoice();
	}

	public void SetSearchText(string text)
	{
		if (GodotObject.IsInstanceValid(_searchEdit))
		{
			_searchEdit.Text = text ?? string.Empty;
			OnSearchChanged(_searchEdit.Text);
		}
	}

	private void OnSearchChanged(string _)
	{
		_page = 0;
		RebuildFilteredChoices();
	}

	private void RebuildFilteredChoices()
	{
		_filteredChoices.Clear();
		string text = (GodotObject.IsInstanceValid(_searchEdit) ? _searchEdit.Text.Trim() : string.Empty);
		foreach (XWObjectPoolVisualChoice choice in _choices)
		{
			if (text.Length == 0 || choice.Title.Contains(text, StringComparison.OrdinalIgnoreCase) || choice.Detail.Contains(text, StringComparison.OrdinalIgnoreCase) || choice.Id.ToString().Contains(text, StringComparison.OrdinalIgnoreCase))
			{
				_filteredChoices.Add(choice);
			}
		}
		_page = Math.Clamp(_page, 0, PageCount - 1);
		RebuildPage();
	}

	private void RebuildPage()
	{
		if (!GodotObject.IsInstanceValid(_choiceList))
		{
			return;
		}
		_updating = true;
		try
		{
			_choiceList.Clear();
			int num = _page * 18;
			int num2 = Math.Min(_filteredChoices.Count, num + 18);
			for (int i = num; i < num2; i++)
			{
				XWObjectPoolVisualChoice xWObjectPoolVisualChoice = _filteredChoices[i];
				int idx = _choiceList.AddItem(xWObjectPoolVisualChoice.Title, xWObjectPoolVisualChoice.Icon);
				_choiceList.SetItemMetadata(idx, xWObjectPoolVisualChoice.Id);
				_choiceList.SetItemTooltip(idx, string.IsNullOrWhiteSpace(xWObjectPoolVisualChoice.Detail) ? $"{xWObjectPoolVisualChoice.Title} · ID {xWObjectPoolVisualChoice.Id}" : $"{xWObjectPoolVisualChoice.Title} · {xWObjectPoolVisualChoice.Detail} · ID {xWObjectPoolVisualChoice.Id}");
			}
			SelectVisibleChoice();
		}
		finally
		{
			_updating = false;
		}
		if (GodotObject.IsInstanceValid(_pageLabel))
		{
			_pageLabel.Text = $"{_page + 1} / {PageCount} · {_filteredChoices.Count} 项";
		}
		if (GodotObject.IsInstanceValid(_previousButton))
		{
			_previousButton.Disabled = _page <= 0;
		}
		if (GodotObject.IsInstanceValid(_nextButton))
		{
			_nextButton.Disabled = _page >= PageCount - 1;
		}
		RefreshSelectedLabel();
	}

	private void SelectVisibleChoice()
	{
		if (!GodotObject.IsInstanceValid(_choiceList))
		{
			return;
		}
		for (int i = 0; i < _choiceList.ItemCount; i++)
		{
			if (_choiceList.GetItemMetadata(i).AsInt32() == _selectedId)
			{
				_choiceList.Select(i);
				return;
			}
		}
		_choiceList.DeselectAll();
	}

	private void OnItemSelected(long index)
	{
		if (!_updating && GodotObject.IsInstanceValid(_choiceList) && index >= 0 && index < _choiceList.ItemCount)
		{
			_selectedId = _choiceList.GetItemMetadata((int)index).AsInt32();
			RefreshSelectedLabel();
			this.ChoiceSelected?.Invoke(_selectedId);
		}
	}

	private void ChangePage(int direction)
	{
		_page = Math.Clamp(_page + direction, 0, PageCount - 1);
		RebuildPage();
	}

	private void RefreshSelectedLabel()
	{
		if (GodotObject.IsInstanceValid(_selectedLabel))
		{
			XWObjectPoolVisualChoice xWObjectPoolVisualChoice = _choices.Find((XWObjectPoolVisualChoice choice) => choice.Id == _selectedId);
			_selectedLabel.Text = ((xWObjectPoolVisualChoice == null) ? $"当前 ID {_selectedId}" : ("当前：" + xWObjectPoolVisualChoice.Title + " · " + xWObjectPoolVisualChoice.Detail));
		}
	}
}
