using System;
using System.Collections.Generic;
using Godot;

namespace PVZHE.ModEditor.GUI;

public partial class XWResourceWorkspacePalette : Window
{
	private sealed class PaletteEntry
	{
		public string Key { get; init; } = "";

		public string Label { get; init; } = "";

		public string Group { get; init; } = "系统";

		public Texture2D Icon { get; init; }

		public Button Card { get; set; }

		public XWUiMotion Motion { get; set; }
	}

	private static readonly string[] GroupOrder = new string[6] { "全部", "角色战斗", "关卡玩法", "卡牌经济", "视听表现", "系统工具" };

	private readonly List<PaletteEntry> _entries = new List<PaletteEntry>();

	private readonly Dictionary<string, Button> _groupButtons = new Dictionary<string, Button>(StringComparer.Ordinal);

	private LineEdit _searchEdit;

	private HFlowContainer _groupFlow;

	private GridContainer _cardGrid;

	private Label _resultLabel;

	private Label _emptyLabel;

	private XWUiMotion _surfaceMotion;

	private string _activeGroup = "全部";

	public int EntryCount => _entries.Count;

	public int VisibleEntryCount { get; private set; }

	public int MotionBindingCount { get; private set; }

	public XWUiMotion SurfaceMotion => _surfaceMotion;

	public event Action<string> WorkspaceSelected;

	public event Action PaletteDismissed;

	public override void _Ready()
	{
		_searchEdit = GetNode<LineEdit>("%SearchEdit");
		_groupFlow = GetNode<HFlowContainer>("%GroupFlow");
		_cardGrid = GetNode<GridContainer>("%CardGrid");
		_resultLabel = GetNode<Label>("%ResultLabel");
		_emptyLabel = GetNode<Label>("%EmptyLabel");
		_surfaceMotion = XWUiMotion.BindPanel(GetNodeOrNull<PanelContainer>("%Surface"));
		base.CloseRequested += HidePalette;
		base.VisibilityChanged += delegate
		{
			if (!base.Visible)
			{
				this.PaletteDismissed?.Invoke();
			}
		};
		_searchEdit.TextChanged += delegate
		{
			RefreshCards();
		};
		BuildGroupButtons();
		RefreshCards();
	}

	public void AddWorkspace(string key, string label, Texture2D icon)
	{
		if (!string.IsNullOrWhiteSpace(key) && !_entries.Exists((PaletteEntry entry) => entry.Key == key))
		{
			PaletteEntry paletteEntry = new PaletteEntry
			{
				Key = key.Trim(),
				Label = (string.IsNullOrWhiteSpace(label) ? "资源" : label.Trim()),
				Group = ResolveGroup(label),
				Icon = icon
			};
			_entries.Add(paletteEntry);
			if (GodotObject.IsInstanceValid(_cardGrid))
			{
				CreateCard(paletteEntry);
				RefreshCards();
			}
		}
	}

	public void OpenPalette()
	{
		if (GodotObject.IsInstanceValid(_searchEdit))
		{
			if (base.Visible)
			{
				GrabFocus();
				_searchEdit.GrabFocus();
				return;
			}
			_searchEdit.Text = "";
			SelectGroup("全部");
			PopupCenteredClamped(new Vector2I(900, 640), 0.9f);
			GrabFocus();
			_searchEdit.GrabFocus();
			CallDeferred("PlayPaletteReveal");
		}
	}

	public bool TrySelectWorkspace(string key)
	{
		PaletteEntry paletteEntry = _entries.Find((PaletteEntry candidate) => candidate.Key == key);
		if (paletteEntry == null)
		{
			return false;
		}
		this.WorkspaceSelected?.Invoke(paletteEntry.Key);
		Hide();
		return true;
	}

	private void BuildGroupButtons()
	{
		string[] groupOrder = GroupOrder;
		foreach (string group in groupOrder)
		{
			Button button = new Button
			{
				Name = "Group_" + group,
				Text = group,
				ToggleMode = true,
				ButtonPressed = (group == _activeGroup),
				CustomMinimumSize = new Vector2(92f, 32f),
				ThemeTypeVariation = "GameNavCard",
				TooltipText = "只显示" + group + "资源工作台"
			};
			button.Pressed += delegate
			{
				SelectGroup(group);
			};
			_groupButtons[group] = button;
			_groupFlow.AddChild(button, forceReadableName: false, InternalMode.Disabled);
			if (XWUiMotion.BindButton(button) != null)
			{
				MotionBindingCount++;
			}
		}
	}

	private void SelectGroup(string group)
	{
		_activeGroup = (_groupButtons.ContainsKey(group) ? group : "全部");
		foreach (var (text2, button2) in _groupButtons)
		{
			button2.SetPressedNoSignal(text2 == _activeGroup);
			XWUiMotion.FindBinding(button2)?.SyncButtonState();
		}
		RefreshCards();
	}

	private void CreateCard(PaletteEntry entry)
	{
		Button button = new Button
		{
			Name = "ResourceWorkspace_" + Sanitize(entry.Key),
			Text = entry.Label,
			Icon = entry.Icon,
			ExpandIcon = false,
			Alignment = HorizontalAlignment.Left,
			CustomMinimumSize = new Vector2(190f, 70f),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
			TooltipText = $"{entry.Label}\n{entry.Group} · {entry.Key}\n点击进入可视资源工作台",
			ThemeTypeVariation = "GameNavCard"
		};
		button.Pressed += delegate
		{
			this.WorkspaceSelected?.Invoke(entry.Key);
			Hide();
		};
		entry.Card = button;
		_cardGrid.AddChild(button, forceReadableName: false, InternalMode.Disabled);
		entry.Motion = XWUiMotion.BindButton(button, XWUiMotion.MotionRole.Card);
		if (entry.Motion != null)
		{
			MotionBindingCount++;
		}
	}

	private void PlayPaletteReveal()
	{
		_surfaceMotion?.PlayPanelReveal(12f);
	}

	private void RefreshCards()
	{
		if (!GodotObject.IsInstanceValid(_cardGrid))
		{
			return;
		}
		string value = _searchEdit?.Text?.Trim() ?? "";
		int num = 0;
		foreach (PaletteEntry entry in _entries)
		{
			bool num2 = _activeGroup == "全部" || entry.Group == _activeGroup;
			bool flag = string.IsNullOrWhiteSpace(value) || entry.Label.Contains(value, StringComparison.OrdinalIgnoreCase) || entry.Key.Contains(value, StringComparison.OrdinalIgnoreCase) || entry.Group.Contains(value, StringComparison.OrdinalIgnoreCase);
			bool flag2 = num2 && flag;
			if (GodotObject.IsInstanceValid(entry.Card))
			{
				entry.Card.Visible = flag2;
			}
			if (flag2)
			{
				num++;
			}
		}
		VisibleEntryCount = num;
		if (GodotObject.IsInstanceValid(_resultLabel))
		{
			_resultLabel.Text = $"{num} / {_entries.Count} 个可视工作台";
		}
		if (GodotObject.IsInstanceValid(_emptyLabel))
		{
			_emptyLabel.Visible = num == 0;
		}
	}

	private void HidePalette()
	{
		Hide();
	}

	private static string ResolveGroup(string label)
	{
		string value = label ?? "";
		if (ContainsAny(value, "角色", "战斗", "投射", "BUFF", "掉落", "收集", "小推车", "铲子", "组件"))
		{
			return "角色战斗";
		}
		if (ContainsAny(value, "关卡", "地图", "生存", "事件", "波次", "解锁", "传送", "玩法", "格子"))
		{
			return "关卡玩法";
		}
		if (ContainsAny(value, "卡片", "卡牌", "费用", "商店", "卡库", "牌库"))
		{
			return "卡牌经济";
		}
		if (ContainsAny(value, "动画", "音频", "BGM", "对话", "界面", "外观", "拼图"))
		{
			return "视听表现";
		}
		return "系统工具";
	}

	private static bool ContainsAny(string value, params string[] tokens)
	{
		foreach (string value2 in tokens)
		{
			if (value.Contains(value2, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	private static string Sanitize(string value)
	{
		return (value ?? "").Replace('/', '_').Replace(':', '_').Replace('.', '_')
			.Replace('@', '_');
	}
}
