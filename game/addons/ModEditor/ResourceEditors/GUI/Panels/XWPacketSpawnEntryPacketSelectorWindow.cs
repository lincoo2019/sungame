using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWPacketSpawnEntryPacketSelectorWindow : Window
{
	[Signal]
	public delegate void PacketSelectedEventHandler(string packetKey);

	private sealed class PacketSnapshot
	{
		public string Key { get; init; } = "";

		public string DisplayName { get; init; } = "";

		public string CostText { get; init; } = "";
	}

	private const string SelectorCardScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWCompactPacketSelectorRow.tscn";

	private const string PacketKeyMetadata = "packet_key";

	private const string RetryCountMetadata = "packet_retry_count";

	private static PackedScene _selectorCardScene;

	private readonly List<PacketSnapshot> _packetSnapshots = new List<PacketSnapshot>();

	private LineEdit _filter;

	private ScrollContainer _packetScroll;

	private GridContainer _packetGrid;

	private Label _emptyState;

	private Label _description;

	private Button _cancelButton;

	private Button _selectButton;

	private ScrollBar _verticalScrollBar;

	private string _selectedPacketKey = "";

	private int _requestVersion;

	private bool _suppressFilterSignal;

	public override void _Ready()
	{
		_filter = GetNode<LineEdit>("%Filter");
		_packetScroll = GetNode<ScrollContainer>("%PacketScroll");
		_packetGrid = GetNode<GridContainer>("%PacketGrid");
		_emptyState = GetNode<Label>("%EmptyState");
		_description = GetNode<Label>("%Description");
		_cancelButton = GetNode<Button>("%CancelButton");
		_selectButton = GetNode<Button>("%SelectButton");
		_verticalScrollBar = _packetScroll.GetVScrollBar();
		_filter.TextChanged += OnFilterChanged;
		_cancelButton.Pressed += CloseSelector;
		_selectButton.Pressed += ConfirmSelection;
		base.CloseRequested += CloseSelector;
		base.VisibilityChanged += OnVisibilityChanged;
		base.SizeChanged += OnWindowSizeChanged;
		if (GodotObject.IsInstanceValid(_verticalScrollBar))
		{
			_verticalScrollBar.ValueChanged += OnScrollValueChanged;
		}
	}

	public override void _ExitTree()
	{
		CancelPendingRequests();
		if (GodotObject.IsInstanceValid(_filter))
		{
			_filter.TextChanged -= OnFilterChanged;
		}
		if (GodotObject.IsInstanceValid(_cancelButton))
		{
			_cancelButton.Pressed -= CloseSelector;
		}
		if (GodotObject.IsInstanceValid(_selectButton))
		{
			_selectButton.Pressed -= ConfirmSelection;
		}
		base.CloseRequested -= CloseSelector;
		base.VisibilityChanged -= OnVisibilityChanged;
		base.SizeChanged -= OnWindowSizeChanged;
		if (GodotObject.IsInstanceValid(_verticalScrollBar))
		{
			_verticalScrollBar.ValueChanged -= OnScrollValueChanged;
		}
		base._ExitTree();
	}

	public void ShowFor(string selectedPacketKey)
	{
		_requestVersion++;
		_selectedPacketKey = selectedPacketKey?.Trim() ?? "";
		_packetSnapshots.Clear();
		try
		{
			ResourceManager instance = ResourceManager.Instance;
			if (instance != null && instance.AreFullGameplayResourcesReady)
			{
				foreach (string item in (from key in ResourceManager.Instance.GetPacketNames().Concat(ResourceManager.Instance.TOWERDEFENSE_PACKETS.Keys)
					where !string.IsNullOrWhiteSpace(key)
					select key).Distinct<string>(StringComparer.OrdinalIgnoreCase).OrderBy<string, string>((string key) => key, StringComparer.OrdinalIgnoreCase))
				{
					TowerDefensePacketConfig packetConfigReadOnly = TowerDefenseManager.GetPacketConfigReadOnly(item);
					string displayName = ((GodotObject.IsInstanceValid(packetConfigReadOnly) && !string.IsNullOrWhiteSpace(packetConfigReadOnly.name)) ? packetConfigReadOnly.name : item);
					string costText = (GodotObject.IsInstanceValid(packetConfigReadOnly) ? packetConfigReadOnly.GetCost().ToString() : "?");
					_packetSnapshots.Add(new PacketSnapshot
					{
						Key = item,
						DisplayName = displayName,
						CostText = costText
					});
				}
			}
		}
		catch (Exception ex)
		{
			_description.Text = "卡牌注册表暂不可用：" + ex.Message;
		}
		_suppressFilterSignal = true;
		_filter.Text = "";
		_suppressFilterSignal = false;
		RebuildFilteredItems("");
		PopupCenteredClamped(new Vector2I(960, 680), 0.9f);
		CallDeferred("RefreshVisibleCardPreviews");
	}

	public void CancelPendingRequests()
	{
		_requestVersion++;
		if (!GodotObject.IsInstanceValid(_packetGrid))
		{
			return;
		}
		foreach (Node child in _packetGrid.GetChildren())
		{
			if (child is Control card)
			{
				ReleaseCardPreview(card);
			}
		}
	}

	private void OnFilterChanged(string query)
	{
		if (!_suppressFilterSignal)
		{
			_requestVersion++;
			RebuildFilteredItems(query);
		}
	}

	private void RebuildFilteredItems(string query)
	{
		if (!GodotObject.IsInstanceValid(_packetGrid))
		{
			return;
		}
		foreach (Node child in _packetGrid.GetChildren())
		{
			_packetGrid.RemoveChild(child);
			child.QueueFree();
		}
		if (_selectorCardScene == null)
		{
			_selectorCardScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWCompactPacketSelectorRow.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		string normalized = query?.Trim() ?? "";
		int num = 0;
		foreach (PacketSnapshot packet in _packetSnapshots)
		{
			if (!MatchesQuery(packet, normalized))
			{
				continue;
			}
			Control card = _selectorCardScene?.Instantiate<Control>(PackedScene.GenEditState.Disabled);
			if (GodotObject.IsInstanceValid(card))
			{
				card.SetMeta("packet_key", packet.Key);
				card.SetMeta("packet_retry_count", 0);
				card.GetNode<Label>("%PacketName").Text = packet.DisplayName;
				card.GetNode<Label>("%PacketCost").Text = "阳光 " + packet.CostText;
				Button node = card.GetNode<Button>("%SelectSurface");
				node.ButtonPressed = string.Equals(packet.Key, _selectedPacketKey, StringComparison.OrdinalIgnoreCase);
				node.Pressed += delegate
				{
					SelectCard(card, packet);
				};
				node.GuiInput += delegate(InputEvent input)
				{
					OnCardGuiInput(input, card, packet);
				};
				_packetGrid.AddChild(card, forceReadableName: false, InternalMode.Disabled);
				num++;
			}
		}
		_emptyState.Visible = num == 0;
		_selectButton.Disabled = num == 0 || string.IsNullOrWhiteSpace(_selectedPacketKey) || !_packetSnapshots.Any((PacketSnapshot packetSnapshot) => string.Equals(packetSnapshot.Key, _selectedPacketKey, StringComparison.OrdinalIgnoreCase) && MatchesQuery(packetSnapshot, normalized));
		if (num == 0)
		{
			_description.Text = "没有符合条件的卡牌；请按名称、saveKey 或费用过滤。";
		}
		else
		{
			UpdateSelectionDescription();
		}
		CallDeferred("RefreshVisibleCardPreviews");
	}

	private void RefreshVisibleCardPreviews()
	{
		if (!base.Visible || !GodotObject.IsInstanceValid(_packetScroll) || !GodotObject.IsInstanceValid(_packetGrid))
		{
			return;
		}
		Rect2 b = _packetScroll.GetGlobalRect().Grow(12f);
		int requestVersion = _requestVersion;
		foreach (Node child in _packetGrid.GetChildren())
		{
			if (child is Control control)
			{
				if (!control.GetGlobalRect().Intersects(b))
				{
					ReleaseCardPreview(control);
				}
				else if (control.GetNode<Control>("%PreviewHost").GetChildCount() == 0)
				{
					control.GetNode<Label>("%LoadingBadge").Visible = true;
					HydrateCardPreview(control, control.GetMeta("packet_key").AsString(), requestVersion);
				}
			}
		}
	}

	private void HydrateCardPreview(Control card, string packetKey, int requestVersion)
	{
		if (requestVersion != _requestVersion || !GodotObject.IsInstanceValid(card) || !card.IsInsideTree())
		{
			return;
		}
		TowerDefensePacketConfig towerDefensePacketConfig = null;
		try
		{
			towerDefensePacketConfig = TowerDefenseManager.GetPacketConfigReadOnly(packetKey)?.Duplicate(deep: true) as TowerDefensePacketConfig;
		}
		catch (Exception)
		{
		}
		if (!GodotObject.IsInstanceValid(towerDefensePacketConfig))
		{
			ScheduleHydrationRetry(card, packetKey, requestVersion);
			return;
		}
		TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = null;
		try
		{
			towerDefenseInGamePacketShow = TowerDefenseManager.CreatePacketShowWithConfig();
			if (!GodotObject.IsInstanceValid(towerDefenseInGamePacketShow))
			{
				ScheduleHydrationRetry(card, packetKey, requestVersion);
				return;
			}
			if (requestVersion != _requestVersion || !GodotObject.IsInstanceValid(card) || !card.IsInsideTree())
			{
				towerDefenseInGamePacketShow.QueueFree();
				return;
			}
			Control node = card.GetNode<Control>("%PreviewHost");
			towerDefenseInGamePacketShow.Name = "SelectorPacketPreview";
			towerDefenseInGamePacketShow.onlyDraw = true;
			towerDefenseInGamePacketShow.setPcLayout = true;
			towerDefenseInGamePacketShow.showLove = false;
			towerDefenseInGamePacketShow.select = false;
			towerDefenseInGamePacketShow.alive = true;
			towerDefenseInGamePacketShow.@lock = false;
			towerDefenseInGamePacketShow.MouseFilter = Control.MouseFilterEnum.Ignore;
			towerDefenseInGamePacketShow.Position = new Vector2(16f, 18f);
			towerDefenseInGamePacketShow.Scale = new Vector2(0.23f, 0.23f);
			node.AddChild(towerDefenseInGamePacketShow, forceReadableName: false, InternalMode.Disabled);
			towerDefenseInGamePacketShow.Init(towerDefensePacketConfig);
			towerDefenseInGamePacketShow.onlyDraw = true;
			towerDefenseInGamePacketShow.RefreshPreview();
			card.GetNode<Label>("%LoadingBadge").Visible = false;
		}
		catch (Exception)
		{
			if (GodotObject.IsInstanceValid(towerDefenseInGamePacketShow))
			{
				towerDefenseInGamePacketShow.GetParent()?.RemoveChild(towerDefenseInGamePacketShow);
				towerDefenseInGamePacketShow.QueueFree();
			}
			ScheduleHydrationRetry(card, packetKey, requestVersion);
		}
	}

	private void ScheduleHydrationRetry(Control card, string packetKey, int requestVersion)
	{
		if (requestVersion == _requestVersion && GodotObject.IsInstanceValid(card))
		{
			int num = card.GetMeta("packet_retry_count", 0).AsInt32();
			if (num >= 3)
			{
				card.GetNode<Label>("%LoadingBadge").Text = "预览暂不可用";
				return;
			}
			card.SetMeta("packet_retry_count", num + 1);
			CallDeferred("RetryHydrateCardPreview", card.GetInstanceId(), packetKey, requestVersion);
		}
	}

	private void RetryHydrateCardPreview(ulong cardInstanceId, string packetKey, int requestVersion)
	{
		Control control = GodotObject.InstanceFromId(cardInstanceId) as Control;
		if (requestVersion == _requestVersion && GodotObject.IsInstanceValid(control) && control.IsInsideTree())
		{
			HydrateCardPreview(control, packetKey, requestVersion);
		}
	}

	private void ReleaseCardPreview(Control card)
	{
		if (!GodotObject.IsInstanceValid(card))
		{
			return;
		}
		Control nodeOrNull = card.GetNodeOrNull<Control>("%PreviewHost");
		if (GodotObject.IsInstanceValid(nodeOrNull))
		{
			foreach (Node child in nodeOrNull.GetChildren())
			{
				if (child is TowerDefenseInGamePacketShow towerDefenseInGamePacketShow)
				{
					towerDefenseInGamePacketShow.ClearEventHandlers();
				}
				nodeOrNull.RemoveChild(child);
				child.QueueFree();
			}
		}
		Label nodeOrNull2 = card.GetNodeOrNull<Label>("%LoadingBadge");
		if (GodotObject.IsInstanceValid(nodeOrNull2))
		{
			nodeOrNull2.Text = "加载卡面…";
			nodeOrNull2.Visible = false;
		}
		card.SetMeta("packet_retry_count", 0);
	}

	private void SelectCard(Control card, PacketSnapshot packet)
	{
		_selectedPacketKey = packet.Key;
		foreach (Node child in _packetGrid.GetChildren())
		{
			if (child is Control control)
			{
				control.GetNode<Button>("%SelectSurface").ButtonPressed = control == card;
			}
		}
		_selectButton.Disabled = false;
		UpdateSelectionDescription();
	}

	private void OnCardGuiInput(InputEvent input, Control card, PacketSnapshot packet)
	{
		if (input is InputEventMouseButton inputEventMouseButton && inputEventMouseButton.ButtonIndex == MouseButton.Left && inputEventMouseButton.DoubleClick)
		{
			SelectCard(card, packet);
			ConfirmSelection();
		}
	}

	private void ConfirmSelection()
	{
		if (!string.IsNullOrWhiteSpace(_selectedPacketKey) && !_selectButton.Disabled)
		{
			string selectedPacketKey = _selectedPacketKey;
			CancelPendingRequests();
			Hide();
			EmitSignal(SignalName.PacketSelected, selectedPacketKey);
		}
	}

	private void CloseSelector()
	{
		CancelPendingRequests();
		Hide();
	}

	private void OnVisibilityChanged()
	{
		if (base.Visible)
		{
			CallDeferred("RefreshVisibleCardPreviews");
		}
		else
		{
			CancelPendingRequests();
		}
	}

	private void OnWindowSizeChanged()
	{
		CallDeferred("RefreshVisibleCardPreviews");
	}

	private void OnScrollValueChanged(double value)
	{
		CallDeferred("RefreshVisibleCardPreviews");
	}

	private void UpdateSelectionDescription()
	{
		PacketSnapshot packetSnapshot = _packetSnapshots.FirstOrDefault((PacketSnapshot packet) => string.Equals(packet.Key, _selectedPacketKey, StringComparison.OrdinalIgnoreCase));
		_description.Text = ((packetSnapshot == null) ? "单击卡面选择；双击可直接确认。" : $"{packetSnapshot.DisplayName} · saveKey: {packetSnapshot.Key} · 阳光: {packetSnapshot.CostText}");
	}

	private static bool MatchesQuery(PacketSnapshot packet, string query)
	{
		if (string.IsNullOrWhiteSpace(query))
		{
			return true;
		}
		if (!packet.Key.Contains(query, StringComparison.OrdinalIgnoreCase) && !packet.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase))
		{
			return packet.CostText.Contains(query, StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}
}
