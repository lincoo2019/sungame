using System;
using System.Collections.Generic;
using Godot;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public sealed class XWVisualOptionGallery : IDisposable
{
	public const int PageSize = 36;

	private readonly OptionButton _source;

	private readonly HFlowContainer _host;

	private readonly Func<int, Texture2D> _iconProvider;

	private readonly Button _openButton;

	private readonly PopupPanel _popup;

	private readonly LineEdit _search;

	private readonly HFlowContainer _cards;

	private readonly List<Button> _optionCards = new List<Button>();

	private readonly List<int> _filteredIndices = new List<int>();

	private readonly Button _previousPage;

	private readonly Button _nextPage;

	private readonly Label _pageStatus;

	private int _pageIndex;

	private bool _disposed;

	public int CardCount => _optionCards.Count;

	public int FilteredOptionCount => _filteredIndices.Count;

	public bool IsVisualSurfaceReady
	{
		get
		{
			if (!_disposed && GodotObject.IsInstanceValid(_source) && GodotObject.IsInstanceValid(_openButton))
			{
				return GodotObject.IsInstanceValid(_popup);
			}
			return false;
		}
	}

	public XWVisualOptionGallery(OptionButton source, HFlowContainer host, string buttonName, Func<int, Texture2D> iconProvider = null)
	{
		_source = source;
		_host = host;
		_iconProvider = iconProvider;
		if (GodotObject.IsInstanceValid(_source) && GodotObject.IsInstanceValid(_host))
		{
			_source.Visible = false;
			_openButton = new Button
			{
				Name = buttonName,
				CustomMinimumSize = new Vector2(190f, 40f),
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
				ClipText = true,
				ExpandIcon = false
			};
			_openButton.AddThemeConstantOverride("icon_max_width", 24);
			_host.AddChild(_openButton, forceReadableName: false, Node.InternalMode.Disabled);
			_popup = new PopupPanel
			{
				Name = buttonName + "Gallery",
				ProcessMode = Node.ProcessModeEnum.Disabled
			};
			_host.AddChild(_popup, forceReadableName: false, Node.InternalMode.Disabled);
			MarginContainer marginContainer = new MarginContainer();
			marginContainer.AddThemeConstantOverride("margin_left", 12);
			marginContainer.AddThemeConstantOverride("margin_top", 12);
			marginContainer.AddThemeConstantOverride("margin_right", 12);
			marginContainer.AddThemeConstantOverride("margin_bottom", 12);
			_popup.AddChild(marginContainer, forceReadableName: false, Node.InternalMode.Disabled);
			VBoxContainer vBoxContainer = new VBoxContainer();
			vBoxContainer.AddThemeConstantOverride("separation", 8);
			marginContainer.AddChild(vBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
			_search = new LineEdit
			{
				Name = buttonName + "Search",
				PlaceholderText = "搜索可视选项……",
				ClearButtonEnabled = true
			};
			vBoxContainer.AddChild(_search, forceReadableName: false, Node.InternalMode.Disabled);
			HBoxContainer hBoxContainer = new HBoxContainer();
			hBoxContainer.AddThemeConstantOverride("separation", 8);
			vBoxContainer.AddChild(hBoxContainer, forceReadableName: false, Node.InternalMode.Disabled);
			_previousPage = new Button
			{
				Name = buttonName + "PreviousPage",
				Text = "上一页",
				TooltipText = "查看上一页"
			};
			hBoxContainer.AddChild(_previousPage, forceReadableName: false, Node.InternalMode.Disabled);
			_pageStatus = new Label
			{
				Name = buttonName + "PageStatus",
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};
			hBoxContainer.AddChild(_pageStatus, forceReadableName: false, Node.InternalMode.Disabled);
			_nextPage = new Button
			{
				Name = buttonName + "NextPage",
				Text = "下一页",
				TooltipText = "查看下一页"
			};
			hBoxContainer.AddChild(_nextPage, forceReadableName: false, Node.InternalMode.Disabled);
			ScrollContainer scrollContainer = new ScrollContainer
			{
				CustomMinimumSize = new Vector2(560f, 320f),
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
				SizeFlagsVertical = Control.SizeFlags.ExpandFill
			};
			vBoxContainer.AddChild(scrollContainer, forceReadableName: false, Node.InternalMode.Disabled);
			_cards = new HFlowContainer
			{
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
			};
			_cards.AddThemeConstantOverride("h_separation", 7);
			_cards.AddThemeConstantOverride("v_separation", 7);
			scrollContainer.AddChild(_cards, forceReadableName: false, Node.InternalMode.Disabled);
			_openButton.Pressed += OpenGallery;
			_search.TextChanged += ApplyFilter;
			_previousPage.Pressed += ShowPreviousPage;
			_nextPage.Pressed += ShowNextPage;
			_popup.PopupHide += OnPopupHidden;
			_source.ItemSelected += OnSourceSelected;
		}
	}

	public void Rebuild()
	{
		if (!_disposed && GodotObject.IsInstanceValid(_source) && GodotObject.IsInstanceValid(_cards))
		{
			ClearRenderedCards();
			_filteredIndices.Clear();
			_pageIndex = 0;
			RefreshSelection();
			if (_popup.Visible)
			{
				ApplyFilter(_search?.Text ?? "");
			}
		}
	}

	public void RefreshSelection()
	{
		if (_disposed || !GodotObject.IsInstanceValid(_source) || !GodotObject.IsInstanceValid(_openButton))
		{
			return;
		}
		int selected = _source.Selected;
		foreach (Button optionCard in _optionCards)
		{
			if (GodotObject.IsInstanceValid(optionCard))
			{
				int num = (optionCard.HasMeta("source_index") ? optionCard.GetMeta("source_index").AsInt32() : (-1));
				optionCard.SetPressedNoSignal(num == selected);
			}
		}
		if (selected >= 0 && selected < _source.ItemCount)
		{
			_openButton.Text = _source.GetItemText(selected);
			_openButton.TooltipText = "打开可视选项：" + _openButton.Text;
			_openButton.Icon = _iconProvider?.Invoke(selected) ?? _source.GetItemIcon(selected) ?? DefaultIcon();
		}
		else
		{
			_openButton.Text = "从图集中选择……";
			_openButton.Icon = DefaultIcon();
		}
	}

	public void SetDisabled(bool disabled, string reason = "")
	{
		if (GodotObject.IsInstanceValid(_openButton))
		{
			_openButton.Disabled = disabled;
			if (disabled && !string.IsNullOrWhiteSpace(reason))
			{
				_openButton.TooltipText = reason;
			}
			else
			{
				RefreshSelection();
			}
		}
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;
		if (GodotObject.IsInstanceValid(_source))
		{
			_source.ItemSelected -= OnSourceSelected;
		}
		if (GodotObject.IsInstanceValid(_openButton))
		{
			_openButton.Pressed -= OpenGallery;
		}
		if (GodotObject.IsInstanceValid(_search))
		{
			_search.TextChanged -= ApplyFilter;
		}
		if (GodotObject.IsInstanceValid(_previousPage))
		{
			_previousPage.Pressed -= ShowPreviousPage;
		}
		if (GodotObject.IsInstanceValid(_nextPage))
		{
			_nextPage.Pressed -= ShowNextPage;
		}
		if (GodotObject.IsInstanceValid(_popup))
		{
			_popup.PopupHide -= OnPopupHidden;
			_popup.Hide();
			_popup.ProcessMode = Node.ProcessModeEnum.Disabled;
		}
		Node[] array = new Node[2] { _openButton, _popup };
		foreach (Node node in array)
		{
			if (GodotObject.IsInstanceValid(node))
			{
				if (GodotObject.IsInstanceValid(_host) && node.GetParent() == _host)
				{
					_host.RemoveChild(node);
				}
				node.QueueFree();
			}
		}
		_optionCards.Clear();
		_filteredIndices.Clear();
	}

	private void OpenGallery()
	{
		if (!_disposed && GodotObject.IsInstanceValid(_popup))
		{
			_popup.ProcessMode = Node.ProcessModeEnum.Inherit;
			if (_search.Text != "")
			{
				_search.Text = "";
			}
			else
			{
				ApplyFilter("");
			}
			_popup.PopupCenteredClamped(new Vector2I(640, 420), 0.86f);
			_search.GrabFocus();
		}
	}

	private void Select(int index)
	{
		if (!_disposed && index >= 0 && index < _source.ItemCount)
		{
			_source.Select(index);
			RefreshSelection();
			_source.EmitSignal(OptionButton.SignalName.ItemSelected, index);
			_popup.Hide();
		}
	}

	private void OnSourceSelected(long _)
	{
		RefreshSelection();
	}

	private void ApplyFilter(string query)
	{
		string value = query?.StripEdges() ?? "";
		_filteredIndices.Clear();
		for (int i = 0; i < _source.ItemCount; i++)
		{
			string itemText = _source.GetItemText(i);
			if (string.IsNullOrWhiteSpace(value) || itemText.Contains(value, StringComparison.OrdinalIgnoreCase))
			{
				_filteredIndices.Add(i);
			}
		}
		_pageIndex = 0;
		RenderPage();
	}

	private void ShowPreviousPage()
	{
		if (_pageIndex > 0)
		{
			_pageIndex--;
			RenderPage();
		}
	}

	private void ShowNextPage()
	{
		int num = Math.Max(1, (_filteredIndices.Count + 36 - 1) / 36);
		if (_pageIndex < num - 1)
		{
			_pageIndex++;
			RenderPage();
		}
	}

	private void RenderPage()
	{
		ClearRenderedCards();
		int num = Math.Max(1, (_filteredIndices.Count + 36 - 1) / 36);
		_pageIndex = Mathf.Clamp(_pageIndex, 0, num - 1);
		int num2 = _pageIndex * 36;
		int num3 = Math.Min(num2 + 36, _filteredIndices.Count);
		ButtonGroup buttonGroup = new ButtonGroup
		{
			AllowUnpress = false
		};
		for (int i = num2; i < num3; i++)
		{
			int sourceIndex = _filteredIndices[i];
			string itemText = _source.GetItemText(sourceIndex);
			Button button = new Button
			{
				Name = $"GalleryOption{sourceIndex}",
				Text = itemText,
				TooltipText = itemText,
				ToggleMode = true,
				ButtonGroup = buttonGroup,
				CustomMinimumSize = new Vector2(164f, 54f),
				Icon = (_iconProvider?.Invoke(sourceIndex) ?? _source.GetItemIcon(sourceIndex) ?? DefaultIcon()),
				ExpandIcon = false,
				ClipText = true
			};
			button.SetMeta("source_index", sourceIndex);
			button.AddThemeConstantOverride("icon_max_width", 28);
			button.Pressed += delegate
			{
				Select(sourceIndex);
			};
			_cards.AddChild(button, forceReadableName: false, Node.InternalMode.Disabled);
			_optionCards.Add(button);
		}
		_previousPage.Disabled = _pageIndex == 0;
		_nextPage.Disabled = _pageIndex >= num - 1;
		_pageStatus.Text = ((_filteredIndices.Count == 0) ? "没有匹配的选项" : $"第 {_pageIndex + 1} / {num} 页  ·  共 {_filteredIndices.Count} 项");
		RefreshSelection();
	}

	private void ClearRenderedCards()
	{
		foreach (Button optionCard in _optionCards)
		{
			if (GodotObject.IsInstanceValid(optionCard))
			{
				if (GodotObject.IsInstanceValid(_cards) && optionCard.GetParent() == _cards)
				{
					_cards.RemoveChild(optionCard);
				}
				optionCard.QueueFree();
			}
		}
		_optionCards.Clear();
	}

	private void OnPopupHidden()
	{
		if (GodotObject.IsInstanceValid(_popup))
		{
			_popup.ProcessMode = Node.ProcessModeEnum.Disabled;
		}
	}

	private static Texture2D DefaultIcon()
	{
		return ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/ResourceCard.svg", null, ResourceLoader.CacheMode.Reuse);
	}
}
