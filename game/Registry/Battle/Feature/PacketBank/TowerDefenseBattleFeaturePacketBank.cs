using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.ModSystem;

[GlobalClass]
public partial class TowerDefenseBattleFeaturePacketBank : TowerDefenseBattleFeature
{
	public delegate void ChooseOverEventHandler();

	private static PackedScene _towerDefenseIngamePacketBank;

	public TowerDefenseInGamePacketBank packetBank;

	public TowerDefenseLevelPacketBankConfig config;

	public bool skipPacketChoose;

	public TowerDefensePacketBankData packetBankData;

	public Array<TowerDefenseInGamePacketShow> packetList = new Array<TowerDefenseInGamePacketShow>();

	public string currentCategory = "";

	public int currentIndex = -1;

	private TowerDefenseInGamePacketShow _currentPacket;

	private int _categoryGeneration;

	private readonly HashSet<string> _availablePacketNames = new HashSet<string>(StringComparer.Ordinal);

	private NinePatchButtonBase _guiTopShopButton;

	private NinePatchButtonBase _guiTopAlmanacButton;

	private bool _packetBankEntered;

	private bool _signalsConnected;

	private static PackedScene TOWER_DEFENSE_INGAME_PACKET_BANK => _towerDefenseIngamePacketBank ?? (_towerDefenseIngamePacketBank = GD.Load<PackedScene>("uid://iydc3rp06xgs"));

	public event ChooseOverEventHandler OnChooseOver;

	public void EmitChooseOver()
	{
		this.OnChooseOver?.Invoke();
	}

	public override void Init(Dictionary _data)
	{
		base.Init(_data);
		config = new TowerDefenseLevelPacketBankConfig();
		config.Init(data);
		packetBank = TOWER_DEFENSE_INGAME_PACKET_BANK.Instantiate<TowerDefenseInGamePacketBank>(PackedScene.GenEditState.Disabled);
		packetBank.packetBankFeature = this;
		control.AddUI(packetBank, 3);
	}

	public override void Destroy()
	{
		_categoryGeneration++;
		_DisconnectPacketBankSignals();
		this.OnChooseOver = null;
		if (GodotObject.IsInstanceValid(packetBank))
		{
			packetBank.DisposeOwnedPackets();
			packetBank.QueueFree();
		}
		packetBank = null;
		packetList.Clear();
		_currentPacket = null;
		_availablePacketNames.Clear();
		packetBankData = null;
		config = null;
		_guiTopShopButton = null;
		_guiTopAlmanacButton = null;
		base.Destroy();
	}

	public override Task GameInit()
	{
		TowerDefenseBattleFeatureSeedBank towerDefenseBattleFeatureSeedBank = GetFeature("SeedBank") as TowerDefenseBattleFeatureSeedBank;
		ResourceManager.Instance.RequireFullGameplayResourcesReady("TowerDefenseBattleFeaturePacketBank");
		packetBank.seedBank = towerDefenseBattleFeatureSeedBank.seedBank;
		_ConnectPacketBankSignals();
		PacketBankInit();
		if (TowerDefenseManager.Instance.IsIZMMode() || TowerDefenseManager.Instance.IsIZM2Mode())
		{
			CategoryChoose("Zombie");
		}
		else
		{
			CategoryChoose("White", reFresh: true);
		}
		return Task.CompletedTask;
	}

	public override Task GameInitFromProgress()
	{
		TowerDefenseBattleFeatureSeedBank towerDefenseBattleFeatureSeedBank = GetFeature("SeedBank") as TowerDefenseBattleFeatureSeedBank;
		ResourceManager.Instance.RequireFullGameplayResourcesReady("TowerDefenseBattleFeaturePacketBank");
		packetBank.seedBank = towerDefenseBattleFeatureSeedBank.seedBank;
		_ConnectPacketBankSignals();
		skipPacketChoose = true;
		if (towerDefenseBattleFeatureSeedBank.config.method == TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.CHOOSE)
		{
			TowerDefensePacketBankData towerDefensePacketBankData = ((!CommandManager.Instance.debugPacketOpenAll) ? TowerDefenseManager.GetPacketBankData(config.packetBankType) : TowerDefenseManager.GetPacketBankData("Total"));
			SetPacketBankData(towerDefensePacketBankData);
		}
		return Task.CompletedTask;
	}

	public override Task GameStartFromProgress()
	{
		if (GetFeature("SeedBank") is TowerDefenseBattleFeatureSeedBank towerDefenseBattleFeatureSeedBank)
		{
			switch (towerDefenseBattleFeatureSeedBank.config.method)
			{
			case TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.NOONE:
				skipPacketChoose = true;
				break;
			case TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.CHOOSE:
				if (GodotObject.IsInstanceValid(packetBankData))
				{
					Array<string> unlockPacket = packetBankData.GetUnlockPacket();
					skipPacketChoose = unlockPacket.Count <= TowerDefenseManager.Instance.seedbankPacketMax;
				}
				else
				{
					skipPacketChoose = false;
				}
				break;
			case TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.PRESET:
				skipPacketChoose = true;
				break;
			}
		}
		return Task.CompletedTask;
	}

	public override Task GameEntry()
	{
		if (!skipPacketChoose && currentCategory != "")
		{
			CategoryChoose(currentCategory, reFresh: true);
		}
		return Task.CompletedTask;
	}

	public void PacketBankInit()
	{
		TowerDefenseBattleFeatureSeedBank towerDefenseBattleFeatureSeedBank = GetFeature("SeedBank") as TowerDefenseBattleFeatureSeedBank;
		TowerDefenseInGameSeedBank seedBank = towerDefenseBattleFeatureSeedBank.seedBank;
		packetBank.seedBank = seedBank;
		bool flag = false;
		switch (towerDefenseBattleFeatureSeedBank.config.method)
		{
		case TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.NOONE:
			skipPacketChoose = true;
			if (Global.IsMultiplayerMode)
			{
				flag = true;
			}
			break;
		case TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.CHOOSE:
		{
			TowerDefensePacketBankData towerDefensePacketBankData = ((!CommandManager.Instance.debugPacketOpenAll) ? TowerDefenseManager.GetPacketBankData(config.packetBankType) : TowerDefenseManager.GetPacketBankData("Total"));
			if (!GodotObject.IsInstanceValid(towerDefensePacketBankData))
			{
				control.RejectLevelConfiguration("选卡库不可用：" + config.packetBankType);
				return;
			}
			SetPacketBankData(towerDefensePacketBankData);
			Array<string> unlockPacket = towerDefensePacketBankData.GetUnlockPacket();
			skipPacketChoose = unlockPacket.Count <= TowerDefenseManager.Instance.seedbankPacketMax;
			foreach (TowerDefenseLevelPacketConfig packet3 in towerDefenseBattleFeatureSeedBank.config.packetList)
			{
				if (!GodotObject.IsInstanceValid(packet3.overrideVal))
				{
					PacketChooseFromName(packet3.packetName, @lock: true);
					continue;
				}
				TowerDefensePacketConfig packet2 = packet3.GetPacket();
				TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = seedBank.AddPacket(packet2);
				if (towerDefenseInGamePacketShow != null)
				{
					towerDefenseInGamePacketShow.@lock = true;
				}
			}
			if (!CommandManager.Instance.debugPacketSelect && skipPacketChoose)
			{
				foreach (string item in unlockPacket)
				{
					TowerDefensePacketConfig packetConfigReadOnly = TowerDefenseManager.GetPacketConfigReadOnly(item);
					seedBank.AddPacket(packetConfigReadOnly);
				}
				seedBank.ReadyPackets();
				if (Global.IsMultiplayerMode)
				{
					flag = true;
				}
			}
			if (CommandManager.Instance.debugPacketSelect)
			{
				skipPacketChoose = false;
			}
			break;
		}
		case TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.PRESET:
			skipPacketChoose = true;
			if (control.isInit && !control.hasProgress)
			{
				seedBank.DeleteAllPacket();
				foreach (TowerDefenseLevelPacketConfig packet4 in towerDefenseBattleFeatureSeedBank.config.packetList)
				{
					TowerDefensePacketConfig packet = packet4.GetPacket();
					seedBank.AddPacket(packet);
				}
				seedBank.ReadyPackets();
			}
			if (Global.IsMultiplayerMode)
			{
				flag = true;
			}
			if (CommandManager.Instance.debugPacketSelect)
			{
				skipPacketChoose = false;
			}
			break;
		}
		if (flag)
		{
			_EmitChooseOver();
		}
	}

	public void SetPacketBankData(TowerDefensePacketBankData _data)
	{
		if (!GodotObject.IsInstanceValid(_data))
		{
			control.RejectLevelConfiguration("选卡库不可用：" + config.packetBankType);
			return;
		}
		TowerDefenseBattleFeatureSeedBank towerDefenseBattleFeatureSeedBank = GetFeature("SeedBank") as TowerDefenseBattleFeatureSeedBank;
		packetBankData = ((config.packetBankType == "GeneralPlant" && towerDefenseBattleFeatureSeedBank != null && towerDefenseBattleFeatureSeedBank.config.method == TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.CHOOSE) ? XWModContentCatalog.WithPlants(_data) : _data);
		Control nodeOrNull = packetBank.cardSort.GetNodeOrNull<Control>("CardMod");
		if (nodeOrNull != null)
		{
			nodeOrNull.Visible = packetBankData.category.ContainsKey("ModPlants");
		}
		RebuildPacketAvailabilityIndex();
		PacketClear();
		if (TowerDefenseManager.Instance.IsIZMMode() || TowerDefenseManager.Instance.IsIZM2Mode())
		{
			CategoryChoose("Zombie");
		}
		else
		{
			CategoryChoose("White", reFresh: true);
		}
	}

	public void PacketChoose(TowerDefenseInGamePacketShow packet)
	{
		if (!GodotObject.IsInstanceValid(packet) || !GodotObject.IsInstanceValid(packet.config) || !GodotObject.IsInstanceValid(packetBank?.seedBank))
		{
			return;
		}
		string saveKey = packet.config.saveKey;
		TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = FindSelectedPacket(saveKey);
		if (GodotObject.IsInstanceValid(towerDefenseInGamePacketShow))
		{
			if (towerDefenseInGamePacketShow.@lock)
			{
				packet.select = false;
				return;
			}
			packetBank.CancelPacketAnimation(towerDefenseInGamePacketShow);
			packetBank.seedBank.DeletePacket(towerDefenseInGamePacketShow);
			if (!packet.alive)
			{
				PacketAlive(saveKey);
			}
			if (packet == _currentPacket)
			{
				_currentPacket = null;
				currentIndex = -1;
			}
			return;
		}
		if (!packet.alive || !packetBank.seedBank.CanAddPacket())
		{
			packet.Reset();
			return;
		}
		Vector2 cameraPos = packetBank.GetCameraPos();
		packetBank.CreateAnime(packet.config, cameraPos + packet.GlobalPosition);
		packet.alive = false;
		if (packet != _currentPacket)
		{
			if (GodotObject.IsInstanceValid(_currentPacket))
			{
				_currentPacket.Reset();
			}
			_currentPacket = packet;
			currentIndex = packetList.IndexOf(packet);
		}
	}

	private TowerDefenseInGamePacketShow FindSelectedPacket(string packetName)
	{
		foreach (TowerDefenseInGamePacketShow packet in packetBank.seedBank.packetList)
		{
			if (GodotObject.IsInstanceValid(packet) && GodotObject.IsInstanceValid(packet.config) && ((packet.originalSaveKey != "") ? packet.originalSaveKey : packet.config.saveKey) == packetName)
			{
				return packet;
			}
		}
		return null;
	}

	public void BindVirtualizedPacket(TowerDefenseInGamePacketShow packet, TowerDefensePacketConfig packetConfig)
	{
		if (GodotObject.IsInstanceValid(packet) && GodotObject.IsInstanceValid(packetConfig))
		{
			packet.enforceRuntimeAvailabilityOnPress = false;
			packet.allowPressWhenUnavailable = true;
			packet.Init(packetConfig);
			packet.showLove = true;
			packet.OnLoveChange += LoveChange;
			packet.OnPressed += PacketChoose;
			packet.alive = !GodotObject.IsInstanceValid(packetBank?.seedBank) || !packetBank.seedBank.HasPacket(packetConfig.saveKey);
			packetList.Add(packet);
		}
	}

	public void UnbindVirtualizedPacket(TowerDefenseInGamePacketShow packet)
	{
		if (GodotObject.IsInstanceValid(packet))
		{
			packet.OnPressed -= PacketChoose;
			packet.OnLoveChange -= LoveChange;
			packetList.Remove(packet);
			if (packet == _currentPacket)
			{
				_currentPacket = null;
				currentIndex = -1;
			}
		}
	}

	private bool IsPacketAvailableInCurrentPacketBank(string packetName)
	{
		if (!string.IsNullOrEmpty(packetName))
		{
			return _availablePacketNames.Contains(packetName);
		}
		return false;
	}

	private void RebuildPacketAvailabilityIndex()
	{
		_availablePacketNames.Clear();
		if (!GodotObject.IsInstanceValid(packetBankData))
		{
			return;
		}
		foreach (Variant value in packetBankData.category.Values)
		{
			foreach (Variant item in (Godot.Collections.Array)value)
			{
				string text = item.AsString();
				if (text != "")
				{
					_availablePacketNames.Add(text);
				}
			}
		}
	}

	public void PacketListChoose(Godot.Collections.Array _packetList)
	{
		AudioManager.Instance.AudioPlay("PacketPick");
		packetBank.ClearAnimeNode();
		foreach (Variant _packet in _packetList)
		{
			string text = _packet.AsString();
			if (!IsPacketAvailableInCurrentPacketBank(text))
			{
				continue;
			}
			TowerDefensePacketConfig packetConfigReadOnly = TowerDefenseManager.GetPacketConfigReadOnly(text);
			if (!GodotObject.IsInstanceValid(packetConfigReadOnly) || !packetConfigReadOnly.Unlock() || packetBank.seedBank.HasPacket(text) || packetBank.seedBank == null || !packetBank.seedBank.CanAddPacket())
			{
				continue;
			}
			bool flag = false;
			foreach (TowerDefenseInGamePacketShow packet in packetList)
			{
				if (packet.config.saveKey == text && packet.alive)
				{
					flag = true;
					PacketChoose(packet);
					break;
				}
			}
			if (!flag)
			{
				Vector2 cameraPos = packetBank.GetCameraPos();
				packetBank.CreateAnime(packetConfigReadOnly, cameraPos + new Vector2(300f, 260f));
			}
		}
	}

	public void PacketChooseFromName(string packetName, bool @lock = false)
	{
		TowerDefensePacketConfig packetConfigReadOnly = TowerDefenseManager.GetPacketConfigReadOnly(packetName);
		if (!GodotObject.IsInstanceValid(packetConfigReadOnly) || packetBank.seedBank.HasPacket(packetName))
		{
			return;
		}
		if (packetBank.seedBank.CanAddPacket())
		{
			TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = packetBank.seedBank.AddPacket(packetConfigReadOnly);
			if (towerDefenseInGamePacketShow != null)
			{
				towerDefenseInGamePacketShow.@lock = @lock;
			}
		}
		foreach (TowerDefenseInGamePacketShow packet in packetList)
		{
			if (packet.config.saveKey == packetName && packet.alive)
			{
				packet.alive = false;
			}
		}
	}

	public void PacketAlive(string packetName)
	{
		foreach (TowerDefenseInGamePacketShow packet in packetList)
		{
			if (packet.config.saveKey == packetName)
			{
				packet.alive = true;
				packet.Reset();
				break;
			}
		}
	}

	public void RockButtonPressed()
	{
		if (GodotObject.IsInstanceValid(packetBank.seedBank) && (packetBank.seedBank.packetNum > 0 || !(Global.Instance.enterLevelMode != "LevelTest")))
		{
			_EmitChooseOver();
		}
	}

	public void _EmitChooseOver()
	{
		RunLifetimeTask(EmitChooseOverAsync, "_EmitChooseOver");
	}

	private async Task EmitChooseOverAsync()
	{
		EmitChooseOver();
		Godot.Collections.Array array = new Godot.Collections.Array();
		foreach (TowerDefenseInGamePacketShow packet in packetBank.seedBank.packetList)
		{
			string text = ((packet.originalSaveKey != "") ? packet.originalSaveKey : packet.config.saveKey);
			array.Add(text);
		}
		if (TowerDefenseManager.Instance.IsIZMMode() || TowerDefenseManager.Instance.IsIZM2Mode())
		{
			GameSaveManager.Instance.SetKeyValue("ZombiePacketReSlect", array);
		}
		else
		{
			GameSaveManager.Instance.SetKeyValue("PacketReSlect", array);
		}
		GameSaveManager.Instance.Save();
		await ToSignal(control.GetTree().CreateTimer(1.0, processAlways: false), SceneTreeTimer.SignalName.Timeout);
		if (base.IsLifetimeActive && GodotObject.IsInstanceValid(packetBank))
		{
			PacketClear();
		}
	}

	public void PacketClear()
	{
		packetBank.ClearPackets();
		packetList.Clear();
		_currentPacket = null;
		currentIndex = -1;
	}

	public void CategoryChoose(string _category, bool reFresh = false)
	{
		RunLifetimeTask(() => CategoryChooseAsync(_category, reFresh), "CategoryChoose");
	}

	private Task CategoryChooseAsync(string _category, bool reFresh)
	{
		if (currentCategory == _category && !reFresh)
		{
			return Task.CompletedTask;
		}
		if (!GodotObject.IsInstanceValid(packetBankData))
		{
			return Task.CompletedTask;
		}
		_categoryGeneration++;
		int categoryGeneration = _categoryGeneration;
		currentCategory = _category;
		if (!packetBankData.category.ContainsKey(_category))
		{
			PacketClear();
			return Task.CompletedTask;
		}
		Godot.Collections.Array array = (Godot.Collections.Array)packetBankData.category[_category];
		Godot.Collections.Array array2 = new Godot.Collections.Array();
		Godot.Collections.Array array3 = new Godot.Collections.Array();
		foreach (Variant item in array)
		{
			string text = item.AsString();
			if (XWModPlayerProgressService.GetPacketState(text).GetValueOrDefault("Love", false).AsBool())
			{
				array2.Add(text);
			}
			else
			{
				array3.Add(text);
			}
		}
		foreach (Variant item2 in array3)
		{
			array2.Add(item2);
		}
		ResourceManager.Instance.RequireFullGameplayResourcesReady("CategoryChooseAsync");
		if (!base.IsLifetimeActive || _categoryGeneration != categoryGeneration || !GodotObject.IsInstanceValid(packetBank))
		{
			return Task.CompletedTask;
		}
		PacketClear();
		List<TowerDefensePacketConfig> list = new List<TowerDefensePacketConfig>(array2.Count);
		foreach (Variant item3 in array2)
		{
			if (_categoryGeneration != categoryGeneration)
			{
				return Task.CompletedTask;
			}
			TowerDefensePacketConfig packetConfigReadOnly = TowerDefenseManager.GetPacketConfigReadOnly(item3.AsString());
			if (GodotObject.IsInstanceValid(packetConfigReadOnly) && packetConfigReadOnly.Unlock())
			{
				list.Add(packetConfigReadOnly);
			}
		}
		if (!base.IsLifetimeActive || _categoryGeneration != categoryGeneration || !GodotObject.IsInstanceValid(packetBank))
		{
			return Task.CompletedTask;
		}
		packetBank.SetVirtualizedPackets(list);
		return Task.CompletedTask;
	}

	public void LoveChange(TowerDefenseInGamePacketShow packet)
	{
		CategoryChoose(currentCategory, reFresh: true);
	}

	public void ReSelectButtonPressed()
	{
		Godot.Collections.Array array = ((!TowerDefenseManager.Instance.IsIZMMode() && !TowerDefenseManager.Instance.IsIZM2Mode()) ? GameSaveManager.Instance.GetKeyValue("PacketReSlect").AsGodotArray() : GameSaveManager.Instance.GetKeyValue("ZombiePacketReSlect").AsGodotArray());
		packetBank.seedBank.DeleteAllPacket();
		PacketListChoose(array);
	}

	public void ViewButtonPressed()
	{
		control.ViewMap();
	}

	public void LoadPacketGroup(int id)
	{
		Godot.Collections.Array array = ((!TowerDefenseManager.Instance.IsIZMMode() && !TowerDefenseManager.Instance.IsIZM2Mode()) ? GameSaveManager.Instance.GetKeyValue($"PacketGroup{id}").AsGodotArray() : GameSaveManager.Instance.GetKeyValue($"ZombiePacketGroup{id}").AsGodotArray());
		packetBank.seedBank.DeleteAllPacket();
		PacketListChoose(array);
	}

	public void SavePacketGroup(int id)
	{
		Godot.Collections.Array array = new Godot.Collections.Array();
		foreach (TowerDefenseInGamePacketShow packet in packetBank.seedBank.packetList)
		{
			string text = ((packet.originalSaveKey != "") ? packet.originalSaveKey : packet.config.saveKey);
			array.Add(text);
		}
		if (TowerDefenseManager.Instance.IsIZMMode() || TowerDefenseManager.Instance.IsIZM2Mode())
		{
			GameSaveManager.Instance.SetKeyValue($"ZombiePacketGroup{id}", array);
		}
		else
		{
			GameSaveManager.Instance.SetKeyValue($"PacketGroup{id}", array);
		}
		GameSaveManager.Instance.Save();
	}

	public void ShopButtonPressed()
	{
		GlobalFeatureManager instance = GlobalFeatureManager.Instance;
		if (instance != null && instance.IsUnlocked("Shop"))
		{
			DialogManager.Instance.DialogCreate("Shop");
		}
	}

	public void AlmanacButtonPressed()
	{
		DialogManager.Instance.DialogCreate("Almanac");
	}

	private void _ConnectPacketBankSignals()
	{
		if (_signalsConnected || !GodotObject.IsInstanceValid(packetBank))
		{
			return;
		}
		foreach (Node child in packetBank.cardSort.GetNode("VBoxContainer").GetChildren())
		{
			if (child is PacketCategoryButton packetCategoryButton)
			{
				packetCategoryButton.OnChoose += _OnCategoryChoose;
			}
		}
		(packetBank.cardItem as PacketCategoryButton).OnChoose += _OnCategoryChoose;
		(packetBank.cardGraveStone as PacketCategoryButton).OnChoose += _OnCategoryChoose;
		(packetBank.cardZombie as PacketCategoryButton).OnChoose += _OnCategoryChoose;
		packetBank.cardSort.GetNode<PacketCategoryButton>("CardMod").OnChoose += _OnCategoryChoose;
		(packetBank.translate.GetNode("RockButton") as NinePatchButtonBase).OnPressed += RockButtonPressed;
		(packetBank.translate.GetNode("ReSelectButton") as NinePatchButtonBase).OnPressed += ReSelectButtonPressed;
		(packetBank.translate.GetNode("ViewButton") as NinePatchButtonBase).OnPressed += ViewButtonPressed;
		Node node = packetBank.translate.GetNode("PacketGroup");
		for (int i = 1; i < 7; i++)
		{
			string text = ((i == 1) ? "PacketGroupButton" : $"PacketGroupButton{i}");
			PacketGroupButton node2 = node.GetNode<PacketGroupButton>(text);
			node2.OnLoadGroup += LoadPacketGroup;
			node2.OnSaveGroup += SavePacketGroup;
		}
		(packetBank.GetNode("ShopButton") as NinePatchButtonBase).OnPressed += ShopButtonPressed;
		(packetBank.GetNode("AlmanacButton") as NinePatchButtonBase).OnPressed += AlmanacButtonPressed;
		_signalsConnected = true;
		if (GlobalFeatureManager.Instance != null)
		{
			GlobalFeatureManager.Instance.FeatureChanged += _OnGlobalFeatureChanged;
		}
		_InitGuiTopButtons();
	}

	private void _OnCategoryChoose(string category)
	{
		CategoryChoose(category);
	}

	private void _InitGuiTopButtons()
	{
		packetBank.packetBankAnimationPlayer.AnimationFinished += _OnPacketBankAnimationFinished;
		if (GodotObject.IsInstanceValid(BattleEventBus.Instance))
		{
			BattleEventBus.Instance.OnUiSwitched += _OnUISwitched;
		}
		Node nodeOrNull = control.GetNodeOrNull("GUITop");
		if (GodotObject.IsInstanceValid(nodeOrNull))
		{
			_guiTopShopButton = nodeOrNull.GetNodeOrNull<NinePatchButtonBase>("ShopButton");
			_guiTopAlmanacButton = nodeOrNull.GetNodeOrNull<NinePatchButtonBase>("AlmanacButton");
			if (GodotObject.IsInstanceValid(_guiTopShopButton))
			{
				_guiTopShopButton.Visible = false;
				_guiTopShopButton.OnPressed += ShopButtonPressed;
			}
			if (GodotObject.IsInstanceValid(_guiTopAlmanacButton))
			{
				_guiTopAlmanacButton.Visible = false;
				_guiTopAlmanacButton.OnPressed += AlmanacButtonPressed;
			}
		}
	}

	private void _DisconnectPacketBankSignals()
	{
		if (!_signalsConnected)
		{
			return;
		}
		if (GodotObject.IsInstanceValid(packetBank))
		{
			foreach (Node child in packetBank.cardSort.GetNode("VBoxContainer").GetChildren())
			{
				if (child is PacketCategoryButton packetCategoryButton)
				{
					packetCategoryButton.OnChoose -= _OnCategoryChoose;
				}
			}
			(packetBank.cardItem as PacketCategoryButton).OnChoose -= _OnCategoryChoose;
			(packetBank.cardGraveStone as PacketCategoryButton).OnChoose -= _OnCategoryChoose;
			(packetBank.cardZombie as PacketCategoryButton).OnChoose -= _OnCategoryChoose;
			packetBank.cardSort.GetNode<PacketCategoryButton>("CardMod").OnChoose -= _OnCategoryChoose;
			(packetBank.translate.GetNode("RockButton") as NinePatchButtonBase).OnPressed -= RockButtonPressed;
			(packetBank.translate.GetNode("ReSelectButton") as NinePatchButtonBase).OnPressed -= ReSelectButtonPressed;
			(packetBank.translate.GetNode("ViewButton") as NinePatchButtonBase).OnPressed -= ViewButtonPressed;
			Node node = packetBank.translate.GetNode("PacketGroup");
			for (int i = 1; i < 7; i++)
			{
				string text = ((i == 1) ? "PacketGroupButton" : $"PacketGroupButton{i}");
				PacketGroupButton node2 = node.GetNode<PacketGroupButton>(text);
				node2.OnLoadGroup -= LoadPacketGroup;
				node2.OnSaveGroup -= SavePacketGroup;
			}
			(packetBank.GetNode("ShopButton") as NinePatchButtonBase).OnPressed -= ShopButtonPressed;
			(packetBank.GetNode("AlmanacButton") as NinePatchButtonBase).OnPressed -= AlmanacButtonPressed;
			if (GodotObject.IsInstanceValid(packetBank.packetBankAnimationPlayer))
			{
				packetBank.packetBankAnimationPlayer.AnimationFinished -= _OnPacketBankAnimationFinished;
			}
		}
		if (GodotObject.IsInstanceValid(_guiTopShopButton))
		{
			_guiTopShopButton.OnPressed -= ShopButtonPressed;
			_guiTopShopButton.Visible = false;
		}
		if (GodotObject.IsInstanceValid(_guiTopAlmanacButton))
		{
			_guiTopAlmanacButton.OnPressed -= AlmanacButtonPressed;
			_guiTopAlmanacButton.Visible = false;
		}
		if (GodotObject.IsInstanceValid(BattleEventBus.Instance))
		{
			BattleEventBus.Instance.OnUiSwitched -= _OnUISwitched;
		}
		if (GlobalFeatureManager.Instance != null)
		{
			GlobalFeatureManager.Instance.FeatureChanged -= _OnGlobalFeatureChanged;
		}
		_signalsConnected = false;
	}

	private void _OnGlobalFeatureChanged(string featureId, int _oldValue, int _newValue)
	{
		if (!(featureId != "Shop"))
		{
			bool mobileMode = GameSaveManager.Instance.GetConfigValue("MobilePreset").AsBool();
			_UpdateGuiTopButtonVisibility(mobileMode);
			_HidePacketBankLegacyButtons();
		}
	}

	private void _OnPacketBankAnimationFinished(StringName animName)
	{
		switch (animName.ToString())
		{
		case "Enter":
		case "MobileEnter":
			_packetBankEntered = true;
			break;
		case "Exit":
		case "MobileExit":
			_packetBankEntered = false;
			break;
		}
		_UpdateGuiTopButtonVisibility(GameSaveManager.Instance.GetConfigValue("MobilePreset").AsBool());
		_HidePacketBankLegacyButtons();
	}

	private void _OnUISwitched(bool mobileMode)
	{
		if (GodotObject.IsInstanceValid(packetBank) && GodotObject.IsInstanceValid(packetBank.translate))
		{
			_packetBankEntered = packetBank.translate.Position.Y < 500f;
			_UpdateGuiTopButtonVisibility(mobileMode);
			_HidePacketBankLegacyButtons();
		}
	}

	private void _UpdateGuiTopButtonVisibility(bool mobileMode)
	{
		if (GodotObject.IsInstanceValid(_guiTopShopButton) && GodotObject.IsInstanceValid(_guiTopAlmanacButton))
		{
			if (mobileMode || !_packetBankEntered)
			{
				_guiTopShopButton.Visible = false;
				_guiTopAlmanacButton.Visible = false;
			}
			else
			{
				_guiTopShopButton.Visible = GlobalFeatureManager.Instance?.IsUnlocked("Shop") ?? false;
				_guiTopAlmanacButton.Visible = true;
			}
		}
	}

	private void _HidePacketBankLegacyButtons()
	{
		CanvasItem nodeOrNull = packetBank.GetNodeOrNull<CanvasItem>("ShopButton");
		CanvasItem nodeOrNull2 = packetBank.GetNodeOrNull<CanvasItem>("AlmanacButton");
		if (GodotObject.IsInstanceValid(nodeOrNull))
		{
			nodeOrNull.Visible = false;
		}
		if (GodotObject.IsInstanceValid(nodeOrNull2))
		{
			nodeOrNull2.Visible = false;
		}
	}
}
