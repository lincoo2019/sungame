using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.ModSystem;

[GlobalClass]
public partial class Almanac : DialogBoxBase
{
	private const float PlantDesktopColumnStride = 52f;

	private const float PlantMobileColumnStride = 98f;

	private const float PlantDesktopRowStride = 75f;

	private const float PlantMobileRowStride = 62f;

	private const float ZombieColumnStride = 75f;

	private const float ZombieRowStride = 75f;

	private static PackedScene _almanacZombieWidow;

	private static PackedScene _almanacPropWidow;

	private readonly List<TowerDefensePacketConfig> _plantLogicalConfigs = new List<TowerDefensePacketConfig>();

	private readonly List<TowerDefensePacketConfig> _zombieLogicalConfigs = new List<TowerDefensePacketConfig>();

	private readonly System.Collections.Generic.Dictionary<int, TowerDefenseInGamePacketShow> _visiblePlantPreviews = new System.Collections.Generic.Dictionary<int, TowerDefenseInGamePacketShow>();

	private readonly System.Collections.Generic.Dictionary<int, AlmanacZombieWidow> _visibleZombiePreviews = new System.Collections.Generic.Dictionary<int, AlmanacZombieWidow>();

	private readonly List<TowerDefenseInGamePacketShow> _plantPreviewPool = new List<TowerDefenseInGamePacketShow>();

	private readonly List<AlmanacZombieWidow> _zombiePreviewPool = new List<AlmanacZombieWidow>();

	private bool _plantInitialized;

	private bool _zombieInitialized;

	private bool _propInitialized;

	private bool _plantRefreshQueued;

	private bool _zombieRefreshQueued;

	private bool _mobilePreset;

	private int _plantBindingGeneration;

	private int _zombieBindingGeneration;

	private int _plantBindingOperationCount;

	private int _zombieBindingOperationCount;

	private bool _plantRefreshRequestedWhileBinding;

	private bool _zombieRefreshRequestedWhileBinding;

	private Viewport _layoutViewport;

	private Control _plantLayoutRoot;

	private Control _zombieLayoutRoot;

	private MarginContainer _zombiePacketMargin;

	public ScrollContainer plantPacketScroll;

	public MarginContainer packetMargin;

	public CanvasLayer plantLayer;

	public Control plantPacketContainer;

	public InformationPanel plantInformationPanel;

	public Label plantShowLabel;

	public CanvasLayer zombieLayer;

	public ScrollContainer zombiePacketScroll;

	public Control zombiePacketContainer;

	public InformationPanel zombieInformationPanel;

	public CanvasLayer propLayer;

	public GridContainer propShovelContainer;

	public GridContainer propMowerContainer;

	public InformationPanel propInformationPanel;

	public NinePatchButtonBase propUseButton;

	public TowerDefensePacketBankData plantPacketBank;

	public TowerDefensePacketBankData zombiePacketBank;

	public int plantCategoryId;

	public TowerDefenseInGamePacketShow plantCurrentSelect;

	public bool plantShowAll;

	public string propType = "Shovel";

	public string propKey = string.Empty;

	public AudioStreamPlayerMember audio;

	public bool isPlaying;

	private static PackedScene ALMANAC_ZOMBIE_WIDOW => _almanacZombieWidow ?? (_almanacZombieWidow = GD.Load<PackedScene>("uid://r4o1fp1gc7bg"));

	private static PackedScene ALMANAC_PROP_WIDOW => _almanacPropWidow ?? (_almanacPropWidow = GD.Load<PackedScene>("uid://uhke44e0fue6"));

	public int PlantLogicalEntryCount => _plantLogicalConfigs.Count;

	public int ZombieLogicalEntryCount => _zombieLogicalConfigs.Count;

	public int PlantPreviewNodeCount => _visiblePlantPreviews.Count + _plantPreviewPool.Count;

	public int ZombiePreviewNodeCount => _visibleZombiePreviews.Count + _zombiePreviewPool.Count;

	public bool ArePlantVirtualBindingsStable
	{
		get
		{
			if (_plantBindingOperationCount == 0 && !_plantRefreshQueued)
			{
				return !_plantRefreshRequestedWhileBinding;
			}
			return false;
		}
	}

	public bool AreZombieVirtualBindingsStable
	{
		get
		{
			if (_zombieBindingOperationCount == 0 && !_zombieRefreshQueued)
			{
				return !_zombieRefreshRequestedWhileBinding;
			}
			return false;
		}
	}

	public bool IsPropCategoryInitialized => _propInitialized;

	public int PlantPreviewNodeLimit => GetPlantColumnCount() * (GetVisibleRowCount(plantPacketScroll, GetPlantRowStride()) + 2);

	public int ZombiePreviewNodeLimit => GetZombieColumnCount() * (GetVisibleRowCount(zombiePacketScroll, 75f) + 2);

	public int PlantColumnCount => GetPlantColumnCount();

	public int ZombieColumnCount => GetZombieColumnCount();

	public void MobilePreset()
	{
		_mobilePreset = true;
		packetMargin.AddThemeConstantOverride("margin_left", 48);
		packetMargin.AddThemeConstantOverride("margin_top", 30);
		packetMargin.AddThemeConstantOverride("margin_right", 48);
		packetMargin.AddThemeConstantOverride("margin_down", 30);
		QueuePlantVirtualRefresh();
		QueueZombieVirtualRefresh();
	}

	public override void _EnterTree()
	{
		audio = AudioManager.Instance.MemberFind("ChooseYourSeeds", AudioManagerEnum.TYPE.MUSIC);
		audio.ProcessMode = ProcessModeEnum.Always;
		isPlaying = audio.Playing;
	}

	public override void _Ready()
	{
		base._Ready();
		ResourceManager.Instance.RequireFullGameplayResourcesReady("Almanac");
		_ = ALMANAC_ZOMBIE_WIDOW;
		_ = ALMANAC_PROP_WIDOW;
		plantPacketScroll = GetNode<ScrollContainer>("PlantLayer/Plant/PacketScroll");
		_plantLayoutRoot = GetNode<Control>("PlantLayer/Plant");
		packetMargin = GetNode<MarginContainer>("%PacketMargin");
		plantLayer = GetNode<CanvasLayer>("%PlantLayer");
		plantPacketContainer = GetNode<Control>("%PacketContainer");
		plantInformationPanel = GetNode<InformationPanel>("%PlantInformationPanel");
		plantShowLabel = GetNode<Label>("%PlantShowLabel");
		zombieLayer = GetNode<CanvasLayer>("%ZombieLayer");
		zombiePacketScroll = GetNode<ScrollContainer>("ZombieLayer/Zombie/ScrollContainer");
		_zombieLayoutRoot = GetNode<Control>("ZombieLayer/Zombie");
		_zombiePacketMargin = GetNode<MarginContainer>("ZombieLayer/Zombie/ScrollContainer/MarginContainer");
		zombiePacketContainer = GetNode<Control>("%ZombiePacketContainer");
		zombieInformationPanel = GetNode<InformationPanel>("%ZombieInformationPanel");
		propLayer = GetNode<CanvasLayer>("%PropLayer");
		propShovelContainer = GetNode<GridContainer>("%PropShovelContainer");
		propMowerContainer = GetNode<GridContainer>("%PropMowerContainer");
		propInformationPanel = GetNode<InformationPanel>("%PropInformationPanel");
		propUseButton = GetNode<NinePatchButtonBase>("%PropUseButton");
		ConnectButtons();
		plantPacketScroll.GetVScrollBar().ValueChanged += OnPlantScrollChanged;
		zombiePacketScroll.GetVScrollBar().ValueChanged += OnZombieScrollChanged;
		_layoutViewport = GetViewport();
		_layoutViewport.SizeChanged += OnViewportSizeChanged;
		base.Resized += OnViewportSizeChanged;
		_plantLayoutRoot.Resized += OnViewportSizeChanged;
		_zombieLayoutRoot.Resized += OnViewportSizeChanged;
		if (GameSaveManager.Instance.GetConfigValue("MobilePreset").AsBool())
		{
			MobilePreset();
		}
		plantPacketBank = XWModContentCatalog.WithPlants(TowerDefenseManager.GetPacketBankData("GeneralPlant"));
		zombiePacketBank = TowerDefenseManager.GetPacketBankData("GeneralZombie");
		if (!isPlaying)
		{
			audio.Play();
		}
		Node node = GetNode("ChooseLayer/Choose/PlantNode");
		Node node2 = GetNode("ChooseLayer/Choose/ZombieNode");
		SetLightMask(node);
		SetLightMask(node2);
		SetIndexPreviewRender(node);
		SetIndexPreviewRender(node2);
	}

	public override void _ExitTree()
	{
		_plantBindingGeneration++;
		_zombieBindingGeneration++;
		_plantRefreshQueued = false;
		_zombieRefreshQueued = false;
		_plantRefreshRequestedWhileBinding = false;
		_zombieRefreshRequestedWhileBinding = false;
		if (GodotObject.IsInstanceValid(plantPacketScroll))
		{
			plantPacketScroll.GetVScrollBar().ValueChanged -= OnPlantScrollChanged;
		}
		if (GodotObject.IsInstanceValid(zombiePacketScroll))
		{
			zombiePacketScroll.GetVScrollBar().ValueChanged -= OnZombieScrollChanged;
		}
		if (GodotObject.IsInstanceValid(_layoutViewport))
		{
			_layoutViewport.SizeChanged -= OnViewportSizeChanged;
		}
		base.Resized -= OnViewportSizeChanged;
		if (GodotObject.IsInstanceValid(_plantLayoutRoot))
		{
			_plantLayoutRoot.Resized -= OnViewportSizeChanged;
		}
		if (GodotObject.IsInstanceValid(_zombieLayoutRoot))
		{
			_zombieLayoutRoot.Resized -= OnViewportSizeChanged;
		}
		_layoutViewport = null;
		_plantLayoutRoot = null;
		_zombieLayoutRoot = null;
		_zombiePacketMargin = null;
		foreach (TowerDefenseInGamePacketShow value in _visiblePlantPreviews.Values)
		{
			value?.ClearEventHandlers();
		}
		foreach (AlmanacZombieWidow value2 in _visibleZombiePreviews.Values)
		{
			value2?.ClearEventHandlers();
		}
		base._ExitTree();
	}

	private void ConnectButtons()
	{
		NinePatchButtonBase node = GetNode<NinePatchButtonBase>("ChooseLayer/Choose/PlantButton");
		node.OnPressed += PlantButtonPressed;
		node.OnPressed += ButtonPressed;
		NinePatchButtonBase node2 = GetNode<NinePatchButtonBase>("ChooseLayer/Choose/ZombieButton");
		node2.OnPressed += ZombieButtonPressed;
		node2.OnPressed += ButtonPressed;
		NinePatchButtonBase node3 = GetNode<NinePatchButtonBase>("ChooseLayer/Choose/PropButton");
		node3.OnPressed += PropButtonPressed;
		node3.OnPressed += ButtonPressed;
		TextureButton node4 = GetNode<TextureButton>("PlantLayer/Plant/IndexButton");
		node4.Pressed += IndexButtonPressed;
		node4.Pressed += ButtonPressed;
		TextureButton node5 = GetNode<TextureButton>("PlantLayer/Plant/PlantPreButton");
		node5.Pressed += PlantPreButtonPressed;
		node5.Pressed += ButtonPressed;
		TextureButton node6 = GetNode<TextureButton>("PlantLayer/Plant/PlantNextButton");
		node6.Pressed += PlantNextButtonPressed;
		node6.Pressed += ButtonPressed;
		GetNode<TextureButton>("PlantLayer/Plant/ShowButton").Pressed += ShowButtonPressed;
		TextureButton node7 = GetNode<TextureButton>("ZombieLayer/Zombie/IndexButton");
		node7.Pressed += IndexButtonPressed;
		node7.Pressed += ButtonPressed;
		TextureButton node8 = GetNode<TextureButton>("PropLayer/IndexButton");
		node8.Pressed += IndexButtonPressed;
		node8.Pressed += ButtonPressed;
		propUseButton.OnPressed += PropUseButtonPressed;
		propUseButton.OnPressed += ButtonPressed;
		TextureButton node9 = GetNode<TextureButton>("ButtonLayer/Button/CloseButton");
		node9.Pressed += CloseButtonPressed;
		node9.Pressed += ButtonPressed;
	}

	public async void InitPlant()
	{
		int generation = ++_plantBindingGeneration;
		ReturnAllPlantPreviewsToPool();
		_plantLogicalConfigs.Clear();
		plantCurrentSelect = null;
		if (!GodotObject.IsInstanceValid(plantPacketBank) || plantCategoryId >= plantPacketBank.category.Count)
		{
			UpdatePlantContentSize();
			TrimPlantPreviewPoolToLimit();
			return;
		}
		Godot.Collections.Array array = new Godot.Collections.Array();
		foreach (Variant key in plantPacketBank.category.Keys)
		{
			array.Add(key);
		}
		Godot.Collections.Array array2 = plantPacketBank.category[array[plantCategoryId]].AsGodotArray();
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		foreach (Variant item in array2)
		{
			string text = item.AsString();
			if (XWModPlayerProgressService.GetPacketState(text).GetValueOrDefault("Love", false).AsBool())
			{
				list.Add(text);
			}
			else
			{
				list2.Add(text);
			}
		}
		list.AddRange(list2);
		foreach (string item2 in list)
		{
			TowerDefensePacketConfig packetConfigReadOnly = TowerDefenseManager.GetPacketConfigReadOnly(item2);
			if (GodotObject.IsInstanceValid(packetConfigReadOnly) && (plantShowAll || packetConfigReadOnly.Unlock()))
			{
				_plantLogicalConfigs.Add(packetConfigReadOnly);
			}
		}
		_plantInitialized = true;
		plantPacketScroll.ScrollVertical = 0;
		UpdatePlantContentSize();
		await PopulateInitialPlantBindingsAsync(generation);
	}

	public void PlantLoveChange(TowerDefenseInGamePacketShow packet)
	{
		InitPlant();
	}

	public void PlantPacketChoose(TowerDefenseInGamePacketShow packet)
	{
		if (plantCurrentSelect != packet)
		{
			plantCurrentSelect?.Reset();
			PlantInformationSet(packet);
			plantCurrentSelect = packet;
		}
	}

	public void PlantInformationSet(TowerDefenseInGamePacketShow packet)
	{
		plantInformationPanel.InitPacket(packet.config);
	}

	public void PlantNextButtonPressed()
	{
		plantCategoryId = (plantCategoryId + 1) % plantPacketBank.category.Count;
		InitPlant();
	}

	public void PlantPreButtonPressed()
	{
		int count = plantPacketBank.category.Count;
		plantCategoryId = ((plantCategoryId - 1) % count + count) % count;
		InitPlant();
	}

	public void PlantButtonPressed()
	{
		plantLayer.Visible = true;
		if (!_plantInitialized)
		{
			InitPlant();
		}
		else
		{
			QueuePlantVirtualRefresh();
		}
	}

	public void ShowButtonPressed()
	{
		plantShowAll = !plantShowAll;
		plantShowLabel.Text = (plantShowAll ? "关闭显示全部" : "显示全部");
		InitPlant();
	}

	public async void InitZombie()
	{
		int generation = ++_zombieBindingGeneration;
		ReturnAllZombiePreviewsToPool();
		_zombieLogicalConfigs.Clear();
		if (GodotObject.IsInstanceValid(zombiePacketBank))
		{
			foreach (Variant item in zombiePacketBank.GetCategory("Zombie"))
			{
				TowerDefensePacketConfig packetConfigReadOnly = TowerDefenseManager.GetPacketConfigReadOnly(item.AsString());
				if (GodotObject.IsInstanceValid(packetConfigReadOnly))
				{
					_zombieLogicalConfigs.Add(packetConfigReadOnly);
				}
			}
		}
		foreach (XWModContentCatalog.Packet packet in XWModContentCatalog.GetPackets(plants: false))
		{
			_zombieLogicalConfigs.Add(packet.Config);
		}
		_zombieInitialized = true;
		zombiePacketScroll.ScrollVertical = 0;
		UpdateZombieContentSize();
		await PopulateInitialZombieBindingsAsync(generation);
	}

	public void ZombiePacketChoose(TowerDefensePacketConfig config)
	{
		ZombieInformationSet(config);
	}

	public void ZombieInformationSet(TowerDefensePacketConfig config)
	{
		zombieInformationPanel.InitPacket(config);
	}

	public void ZombieButtonPressed()
	{
		zombieLayer.Visible = true;
		if (!_zombieInitialized)
		{
			InitZombie();
		}
		else
		{
			QueueZombieVirtualRefresh();
		}
	}

	private void OnPlantScrollChanged(double value)
	{
		QueuePlantVirtualRefresh();
	}

	private void OnZombieScrollChanged(double value)
	{
		QueueZombieVirtualRefresh();
	}

	private void OnViewportSizeChanged()
	{
		QueuePlantVirtualRefresh();
		QueueZombieVirtualRefresh();
	}

	public void QueuePlantVirtualRefresh()
	{
		if (!_plantRefreshQueued && IsInsideTree() && _plantInitialized)
		{
			_plantRefreshQueued = true;
			CallDeferred("RefreshPlantVirtualBindings");
		}
	}

	public void QueueZombieVirtualRefresh()
	{
		if (!_zombieRefreshQueued && IsInsideTree() && _zombieInitialized)
		{
			_zombieRefreshQueued = true;
			CallDeferred("RefreshZombieVirtualBindings");
		}
	}

	public async void RefreshPlantVirtualBindings()
	{
		_plantRefreshQueued = false;
		if (!_plantInitialized || !GodotObject.IsInstanceValid(plantPacketScroll))
		{
			return;
		}
		if (_plantBindingOperationCount > 0)
		{
			_plantRefreshRequestedWhileBinding = true;
			return;
		}
		int generation = ++_plantBindingGeneration;
		_plantBindingOperationCount++;
		try
		{
			UpdatePlantContentSize();
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			if (generation != _plantBindingGeneration || !IsInsideTree())
			{
				return;
			}
			UpdatePlantContentSize();
			int columns = GetPlantColumnCount();
			float columnStride = GetPlantColumnStride();
			float rowStride = GetPlantRowStride();
			var (firstIndex, lastIndex) = GetVisibleIndexRange(_plantLogicalConfigs.Count, columns, rowStride, plantPacketScroll);
			RecyclePlantPreviewsOutside(firstIndex, lastIndex);
			TrimPlantPreviewPoolToLimit();
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			if (generation != _plantBindingGeneration || !IsInsideTree())
			{
				return;
			}
			int batchCount = 0;
			for (int index = firstIndex; index <= lastIndex; index++)
			{
				if (_visiblePlantPreviews.TryGetValue(index, out var value) && GodotObject.IsInstanceValid(value))
				{
					PositionVirtualPreview(value, index, columns, columnStride, rowStride);
					continue;
				}
				TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = TakePlantPreview();
				BindPlantPreview(towerDefenseInGamePacketShow, _plantLogicalConfigs[index]);
				PositionVirtualPreview(towerDefenseInGamePacketShow, index, columns, columnStride, rowStride);
				_visiblePlantPreviews[index] = towerDefenseInGamePacketShow;
				batchCount++;
				if (batchCount >= 12)
				{
					batchCount = 0;
					await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
					if (generation != _plantBindingGeneration || !IsInsideTree())
					{
						return;
					}
				}
			}
			if (plantCurrentSelect == null && _visiblePlantPreviews.TryGetValue(0, out var value2))
			{
				PlantPacketChoose(value2);
			}
		}
		finally
		{
			_plantBindingOperationCount--;
			if (_plantRefreshRequestedWhileBinding && _plantBindingOperationCount == 0 && CanQueueVirtualRefresh())
			{
				_plantRefreshRequestedWhileBinding = false;
				QueuePlantVirtualRefresh();
			}
		}
	}

	public async void RefreshZombieVirtualBindings()
	{
		_zombieRefreshQueued = false;
		if (!_zombieInitialized || !GodotObject.IsInstanceValid(zombiePacketScroll))
		{
			return;
		}
		if (_zombieBindingOperationCount > 0)
		{
			_zombieRefreshRequestedWhileBinding = true;
			return;
		}
		int generation = ++_zombieBindingGeneration;
		_zombieBindingOperationCount++;
		try
		{
			UpdateZombieContentSize();
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			if (generation != _zombieBindingGeneration || !IsInsideTree())
			{
				return;
			}
			UpdateZombieContentSize();
			int columns = GetZombieColumnCount();
			var (firstIndex, lastIndex) = GetVisibleIndexRange(_zombieLogicalConfigs.Count, columns, 75f, zombiePacketScroll);
			RecycleZombiePreviewsOutside(firstIndex, lastIndex);
			TrimZombiePreviewPoolToLimit();
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			if (generation != _zombieBindingGeneration || !IsInsideTree())
			{
				return;
			}
			int batchCount = 0;
			for (int index = firstIndex; index <= lastIndex; index++)
			{
				if (_visibleZombiePreviews.TryGetValue(index, out var value) && GodotObject.IsInstanceValid(value))
				{
					PositionVirtualPreview(value, index, columns, 75f, 75f);
					continue;
				}
				AlmanacZombieWidow almanacZombieWidow = TakeZombiePreview();
				BindZombiePreview(almanacZombieWidow, _zombieLogicalConfigs[index]);
				PositionVirtualPreview(almanacZombieWidow, index, columns, 75f, 75f);
				_visibleZombiePreviews[index] = almanacZombieWidow;
				batchCount++;
				if (batchCount >= 12)
				{
					batchCount = 0;
					await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
					if (generation != _zombieBindingGeneration || !IsInsideTree())
					{
						return;
					}
				}
			}
			if (_visibleZombiePreviews.TryGetValue(0, out var value2))
			{
				ZombiePacketChoose(value2.config);
			}
		}
		finally
		{
			_zombieBindingOperationCount--;
			if (_zombieRefreshRequestedWhileBinding && _zombieBindingOperationCount == 0 && CanQueueVirtualRefresh())
			{
				_zombieRefreshRequestedWhileBinding = false;
				QueueZombieVirtualRefresh();
			}
		}
	}

	private async Task PopulateInitialPlantBindingsAsync(int generation)
	{
		_plantBindingOperationCount++;
		try
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			if (generation != _plantBindingGeneration || !IsInsideTree())
			{
				return;
			}
			UpdatePlantContentSize();
			TrimPlantPreviewPoolToLimit();
			int columns = GetPlantColumnCount();
			float columnStride = GetPlantColumnStride();
			float rowStride = GetPlantRowStride();
			(int, int) visibleIndexRange = GetVisibleIndexRange(_plantLogicalConfigs.Count, columns, rowStride, plantPacketScroll);
			int item = visibleIndexRange.Item1;
			int lastIndex = visibleIndexRange.Item2;
			int batchCount = 0;
			for (int index = item; index <= lastIndex; index++)
			{
				if (generation != _plantBindingGeneration || !IsInsideTree())
				{
					return;
				}
				TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = TakePlantPreview();
				BindPlantPreview(towerDefenseInGamePacketShow, _plantLogicalConfigs[index]);
				PositionVirtualPreview(towerDefenseInGamePacketShow, index, columns, columnStride, rowStride);
				_visiblePlantPreviews[index] = towerDefenseInGamePacketShow;
				batchCount++;
				if (batchCount >= 12)
				{
					batchCount = 0;
					await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				}
			}
			if (generation == _plantBindingGeneration && plantCurrentSelect == null && _visiblePlantPreviews.TryGetValue(0, out var value))
			{
				PlantPacketChoose(value);
			}
		}
		finally
		{
			_plantBindingOperationCount--;
			if (_plantRefreshRequestedWhileBinding && _plantBindingOperationCount == 0 && CanQueueVirtualRefresh())
			{
				_plantRefreshRequestedWhileBinding = false;
				QueuePlantVirtualRefresh();
			}
		}
	}

	private async Task PopulateInitialZombieBindingsAsync(int generation)
	{
		_zombieBindingOperationCount++;
		try
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			if (generation != _zombieBindingGeneration || !IsInsideTree())
			{
				return;
			}
			UpdateZombieContentSize();
			TrimZombiePreviewPoolToLimit();
			int columns = GetZombieColumnCount();
			(int, int) visibleIndexRange = GetVisibleIndexRange(_zombieLogicalConfigs.Count, columns, 75f, zombiePacketScroll);
			int item = visibleIndexRange.Item1;
			int lastIndex = visibleIndexRange.Item2;
			int batchCount = 0;
			for (int index = item; index <= lastIndex; index++)
			{
				if (generation != _zombieBindingGeneration || !IsInsideTree())
				{
					return;
				}
				AlmanacZombieWidow almanacZombieWidow = TakeZombiePreview();
				BindZombiePreview(almanacZombieWidow, _zombieLogicalConfigs[index]);
				PositionVirtualPreview(almanacZombieWidow, index, columns, 75f, 75f);
				_visibleZombiePreviews[index] = almanacZombieWidow;
				batchCount++;
				if (batchCount >= 12)
				{
					batchCount = 0;
					await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				}
			}
			if (generation == _zombieBindingGeneration && _visibleZombiePreviews.TryGetValue(0, out var value))
			{
				ZombiePacketChoose(value.config);
			}
		}
		finally
		{
			_zombieBindingOperationCount--;
			if (_zombieRefreshRequestedWhileBinding && _zombieBindingOperationCount == 0 && CanQueueVirtualRefresh())
			{
				_zombieRefreshRequestedWhileBinding = false;
				QueueZombieVirtualRefresh();
			}
		}
	}

	private bool CanQueueVirtualRefresh()
	{
		if (GodotObject.IsInstanceValid(this))
		{
			return IsInsideTree();
		}
		return false;
	}

	private static (int firstIndex, int lastIndex) GetVisibleIndexRange(int itemCount, int columns, float rowStride, ScrollContainer scroll)
	{
		if (itemCount <= 0)
		{
			return (firstIndex: 0, lastIndex: -1);
		}
		int num = Mathf.CeilToInt((float)itemCount / (float)columns);
		int num2 = Mathf.FloorToInt(Math.Max(0.0, scroll.ScrollVertical) / (double)rowStride);
		int visibleRowCount = GetVisibleRowCount(scroll, rowStride);
		int num3 = Math.Max(0, num2 - 1);
		int num4 = Math.Min(num - 1, num2 + visibleRowCount);
		return (firstIndex: num3 * columns, lastIndex: Math.Min(itemCount - 1, (num4 + 1) * columns - 1));
	}

	private static int GetVisibleRowCount(ScrollContainer scroll, float rowStride)
	{
		if (!GodotObject.IsInstanceValid(scroll))
		{
			return 1;
		}
		return Math.Max(1, Mathf.CeilToInt(scroll.Size.Y / rowStride));
	}

	private void UpdatePlantContentSize()
	{
		int columnCount = GetColumnCount(GetVirtualContentWidth(plantPacketScroll, packetMargin), GetPlantColumnStride());
		int num = ((_plantLogicalConfigs.Count != 0) ? Mathf.CeilToInt((float)_plantLogicalConfigs.Count / (float)columnCount) : 0);
		plantPacketContainer.CustomMinimumSize = new Vector2(0f, (float)num * GetPlantRowStride());
	}

	private void UpdateZombieContentSize()
	{
		int columnCount = GetColumnCount(GetVirtualContentWidth(zombiePacketScroll, _zombiePacketMargin), 75f);
		int num = ((_zombieLogicalConfigs.Count != 0) ? Mathf.CeilToInt((float)_zombieLogicalConfigs.Count / (float)columnCount) : 0);
		zombiePacketContainer.CustomMinimumSize = new Vector2(0f, (float)num * 75f);
	}

	private float GetPlantColumnStride()
	{
		if (!_mobilePreset)
		{
			return 52f;
		}
		return 98f;
	}

	private float GetPlantRowStride()
	{
		if (!_mobilePreset)
		{
			return 75f;
		}
		return 62f;
	}

	private int GetPlantColumnCount()
	{
		return GetColumnCount(GetVirtualContentWidth(plantPacketScroll, packetMargin), GetPlantColumnStride());
	}

	private int GetZombieColumnCount()
	{
		return GetColumnCount(GetVirtualContentWidth(zombiePacketScroll, _zombiePacketMargin), 75f);
	}

	private static float GetVirtualContentWidth(ScrollContainer scroll, MarginContainer margin)
	{
		if (!GodotObject.IsInstanceValid(scroll) || scroll.Size.X <= 0f)
		{
			return 0f;
		}
		float num = scroll.Size.X;
		ScrollBar vScrollBar = scroll.GetVScrollBar();
		if (GodotObject.IsInstanceValid(vScrollBar) && vScrollBar.Visible)
		{
			num -= vScrollBar.Size.X;
		}
		if (GodotObject.IsInstanceValid(margin))
		{
			num -= (float)(margin.GetThemeConstant("margin_left") + margin.GetThemeConstant("margin_right"));
		}
		return Math.Max(0f, num);
	}

	private static int GetColumnCount(float width, float columnStride)
	{
		if (width <= 0f)
		{
			return 1;
		}
		return Math.Max(1, Mathf.FloorToInt(width / columnStride) + 1);
	}

	private static void PositionVirtualPreview(Control preview, int itemIndex, int columns, float columnStride, float rowStride)
	{
		preview.Position = new Vector2((float)(itemIndex % columns) * columnStride, (float)(itemIndex / columns) * rowStride);
	}

	private TowerDefenseInGamePacketShow TakePlantPreview()
	{
		TowerDefenseInGamePacketShow towerDefenseInGamePacketShow;
		if (_plantPreviewPool.Count > 0)
		{
			int index = _plantPreviewPool.Count - 1;
			towerDefenseInGamePacketShow = _plantPreviewPool[index];
			_plantPreviewPool.RemoveAt(index);
		}
		else
		{
			towerDefenseInGamePacketShow = TowerDefenseManager.CreatePacketShow();
			plantPacketContainer.AddChild(towerDefenseInGamePacketShow, forceReadableName: false, InternalMode.Disabled);
			SetLightMask(towerDefenseInGamePacketShow);
		}
		towerDefenseInGamePacketShow.Visible = true;
		return towerDefenseInGamePacketShow;
	}

	private void BindPlantPreview(TowerDefenseInGamePacketShow preview, TowerDefensePacketConfig config)
	{
		preview.ResetForPool();
		preview.setMobileLayout = _mobilePreset;
		preview.showLove = true;
		preview.enforceRuntimeAvailabilityOnPress = false;
		preview.Init(config);
		preview.openShadow = plantShowAll && !config.Unlock();
		preview.OnLoveChange += PlantLoveChange;
		preview.OnPressed += PlantPacketChoose;
	}

	private AlmanacZombieWidow TakeZombiePreview()
	{
		AlmanacZombieWidow almanacZombieWidow;
		if (_zombiePreviewPool.Count > 0)
		{
			int index = _zombiePreviewPool.Count - 1;
			almanacZombieWidow = _zombiePreviewPool[index];
			_zombiePreviewPool.RemoveAt(index);
		}
		else
		{
			almanacZombieWidow = ALMANAC_ZOMBIE_WIDOW.Instantiate<AlmanacZombieWidow>(PackedScene.GenEditState.Disabled);
			zombiePacketContainer.AddChild(almanacZombieWidow, forceReadableName: false, InternalMode.Disabled);
			SetLightMask(almanacZombieWidow);
		}
		almanacZombieWidow.Visible = true;
		return almanacZombieWidow;
	}

	private void BindZombiePreview(AlmanacZombieWidow preview, TowerDefensePacketConfig config)
	{
		preview.ClearEventHandlers();
		preview.Reset();
		preview.Init(config);
		preview.OnPressed += ZombiePacketChoose;
	}

	private void RecyclePlantPreviewsOutside(int firstIndex, int lastIndex)
	{
		List<int> list = new List<int>();
		foreach (var (num2, preview) in _visiblePlantPreviews)
		{
			if (num2 < firstIndex || num2 > lastIndex)
			{
				list.Add(num2);
				ReturnPlantPreviewToPool(preview);
			}
		}
		foreach (int item in list)
		{
			_visiblePlantPreviews.Remove(item);
		}
	}

	private void RecycleZombiePreviewsOutside(int firstIndex, int lastIndex)
	{
		List<int> list = new List<int>();
		foreach (var (num2, preview) in _visibleZombiePreviews)
		{
			if (num2 < firstIndex || num2 > lastIndex)
			{
				list.Add(num2);
				ReturnZombiePreviewToPool(preview);
			}
		}
		foreach (int item in list)
		{
			_visibleZombiePreviews.Remove(item);
		}
	}

	private void TrimPlantPreviewPoolToLimit()
	{
		int num = Math.Max(0, PlantPreviewNodeLimit - _visiblePlantPreviews.Count);
		while (_plantPreviewPool.Count > num)
		{
			int index = _plantPreviewPool.Count - 1;
			TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = _plantPreviewPool[index];
			_plantPreviewPool.RemoveAt(index);
			if (GodotObject.IsInstanceValid(towerDefenseInGamePacketShow))
			{
				towerDefenseInGamePacketShow.ResetForPool();
				towerDefenseInGamePacketShow.QueueFree();
			}
		}
	}

	private void TrimZombiePreviewPoolToLimit()
	{
		int num = Math.Max(0, ZombiePreviewNodeLimit - _visibleZombiePreviews.Count);
		while (_zombiePreviewPool.Count > num)
		{
			int index = _zombiePreviewPool.Count - 1;
			AlmanacZombieWidow almanacZombieWidow = _zombiePreviewPool[index];
			_zombiePreviewPool.RemoveAt(index);
			if (GodotObject.IsInstanceValid(almanacZombieWidow))
			{
				almanacZombieWidow.ReleaseForPoolDisposal();
				almanacZombieWidow.QueueFree();
			}
		}
	}

	private void ReturnPlantPreviewToPool(TowerDefenseInGamePacketShow preview)
	{
		if (GodotObject.IsInstanceValid(preview))
		{
			if (plantCurrentSelect == preview)
			{
				plantCurrentSelect = null;
			}
			preview.ResetForPool();
			preview.Visible = false;
			_plantPreviewPool.Add(preview);
		}
	}

	private void ReturnZombiePreviewToPool(AlmanacZombieWidow preview)
	{
		if (GodotObject.IsInstanceValid(preview))
		{
			preview.ClearEventHandlers();
			preview.Reset();
			preview.Visible = false;
			_zombiePreviewPool.Add(preview);
		}
	}

	private void ReturnAllPlantPreviewsToPool()
	{
		foreach (TowerDefenseInGamePacketShow value in _visiblePlantPreviews.Values)
		{
			ReturnPlantPreviewToPool(value);
		}
		_visiblePlantPreviews.Clear();
	}

	private void ReturnAllZombiePreviewsToPool()
	{
		foreach (AlmanacZombieWidow value in _visibleZombiePreviews.Values)
		{
			ReturnZombiePreviewToPool(value);
		}
		_visibleZombiePreviews.Clear();
	}

	public void InitProp()
	{
		if (_propInitialized)
		{
			return;
		}
		_propInitialized = true;
		foreach (Variant shovel2 in TowerDefenseManager.GetShovelList())
		{
			ShovelConfig shovel = TowerDefenseManager.GetShovel((string)shovel2);
			if (shovel != null && shovel.Unlock())
			{
				AlmanacPropWidow almanacPropWidow = ALMANAC_PROP_WIDOW.Instantiate<AlmanacPropWidow>(PackedScene.GenEditState.Disabled);
				propShovelContainer.AddChild(almanacPropWidow, forceReadableName: false, InternalMode.Disabled);
				almanacPropWidow.InitShovel(shovel);
				SetLightMask(almanacPropWidow);
				almanacPropWidow.OnPressedShovel += PropShovelChoose;
			}
		}
		foreach (Variant mower in TowerDefenseManager.GetMowerList())
		{
			MowerConfig mowerConfig = TowerDefenseManager.GetMowerConfig((string)mower);
			if (mowerConfig != null && mowerConfig.Unlock())
			{
				AlmanacPropWidow almanacPropWidow2 = ALMANAC_PROP_WIDOW.Instantiate<AlmanacPropWidow>(PackedScene.GenEditState.Disabled);
				propMowerContainer.AddChild(almanacPropWidow2, forceReadableName: false, InternalMode.Disabled);
				almanacPropWidow2.InitMower(mowerConfig);
				SetLightMask(almanacPropWidow2);
				almanacPropWidow2.OnPressedMower += PropMowerChoose;
			}
		}
		if (propShovelContainer.GetChildCount() > 0)
		{
			PropShovelChoose(((AlmanacPropWidow)propShovelContainer.GetChild(0)).shovelConfig);
		}
	}

	public void PropShovelChoose(ShovelConfig config)
	{
		PropShovelInformationSet(config);
		propType = "Shovel";
		propKey = config.saveKey;
		SetPropUseButtonState(GameSaveManager.Instance.GetKeyValue("CurrentShovel").AsString() == config.saveKey);
	}

	public void PropMowerChoose(MowerConfig config)
	{
		PropMowerInformationSet(config);
		propType = "Mower";
		propKey = config.saveKey;
		SetPropUseButtonState(GameSaveManager.Instance.GetKeyValue("CurrentMower").AsString() == config.saveKey);
	}

	private void SetPropUseButtonState(bool equipped)
	{
		propUseButton.text = (equipped ? "已装备" : "装备道具");
		propUseButton.disable = equipped;
	}

	public void PropShovelInformationSet(ShovelConfig config)
	{
		propInformationPanel.InitShovel(config);
	}

	public void PropMowerInformationSet(MowerConfig config)
	{
		propInformationPanel.InitMower(config);
	}

	public void PropUseButtonPressed()
	{
		string key = ((propType == "Mower") ? "CurrentMower" : "CurrentShovel");
		GameSaveManager.Instance.SetKeyValue(key, propKey);
		SetPropUseButtonState(equipped: true);
		GameSaveManager.Instance.Save();
	}

	public void PropButtonPressed()
	{
		propLayer.Visible = true;
		InitProp();
	}

	public void ButtonPressed()
	{
		AudioManager.Instance.AudioPlay("ButtonPress");
	}

	public void IndexButtonPressed()
	{
		plantLayer.Visible = false;
		zombieLayer.Visible = false;
		propLayer.Visible = false;
	}

	public void CloseButtonPressed()
	{
		audio.ProcessMode = ProcessModeEnum.Pausable;
		if (!isPlaying && GodotObject.IsInstanceValid(audio))
		{
			audio.Stop();
		}
		CloseDialog();
	}

	private void SetLightMask(Node node)
	{
		if (node is CanvasItem canvasItem)
		{
			canvasItem.LightMask = 0;
		}
		if (node is Light2D light2D)
		{
			light2D.Visible = false;
		}
		foreach (Node child in node.GetChildren(includeInternal: true))
		{
			SetLightMask(child);
		}
	}

	private void SetIndexPreviewRender(Node node)
	{
		if (node is AdobeAnimateSprite adobeAnimateSprite)
		{
			adobeAnimateSprite.forceLocalRender = true;
			adobeAnimateSprite.ProcessMode = ProcessModeEnum.Always;
		}
		foreach (Node child in node.GetChildren(includeInternal: true))
		{
			SetIndexPreviewRender(child);
		}
	}
}
