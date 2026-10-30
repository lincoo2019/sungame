using Godot;
using Godot.Collections;

[GlobalClass]
public partial class LevelEditorWaveEditorPacketChoose : Control
{
	private ScrollContainer _packetBankScroll;

	private MarginContainer _packetBankMargin;

	private GridContainer _packetContainer;

	private Control _animeNode;

	public static LevelEditorWaveEditorPacketChoose Instance;

	public Variant data;

	public TowerDefenseBattleFeatureMap mapFeature;

	public Array<TowerDefenseInGamePacketShow> packetList = new Array<TowerDefenseInGamePacketShow>();

	public string currentCategory = "";

	public int currentIndex = -1;

	public int _categoryGeneration;

	private const string DefaultCategory = "Zombie";

	public override void _Ready()
	{
		_packetBankScroll = GetNode<ScrollContainer>("%PacketBankScroll");
		_packetBankMargin = GetNode<MarginContainer>("%PacketBankMargin");
		_packetContainer = GetNode<GridContainer>("%PacketContainer");
		_animeNode = GetNode<Control>("%AnimeNode");
		foreach (Node child in GetNode<VBoxContainer>("VBoxContainer").GetChildren())
		{
			if (child is PacketCategoryButton packetCategoryButton)
			{
				packetCategoryButton.OnChoose += delegate(string category)
				{
					CategoryChoose(category);
				};
			}
		}
		mapFeature = TowerDefenseManager.GetMapFeature();
		Instance = this;
		data = TowerDefenseManager.GetPacketBankData("Total");
		base.VisibilityChanged += RefreshWhenVisible;
		CallDeferred("RefreshWhenVisible");
	}

	public void RefreshWhenVisible()
	{
		if (!IsInsideTree() || !GodotObject.IsInstanceValid(_packetContainer))
		{
			return;
		}
		if (IsVisibleInTree())
		{
			if (packetList.Count == 0 && data.VariantType != Variant.Type.Nil)
			{
				CategoryChoose(string.IsNullOrEmpty(currentCategory) ? "Zombie" : currentCategory, reFresh: true);
			}
		}
		else if (packetList.Count > 0)
		{
			PacketClear();
		}
	}

	public async void Init(Variant _data)
	{
		data = _data;
		PacketClear();
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		CategoryChoose("Zombie");
	}

	public void PacketChoose(TowerDefenseInGamePacketShow packet)
	{
		LevelEditorWaveEditor.Instance.levelConfig.canExport = false;
		int num = packetList.IndexOf(packet);
		if (packet.alive && currentIndex != -1 && LevelEditorWaveEditor.Instance.currentPacketContainer != null && LevelEditorWaveEditor.Instance.currentPacketContainer.CanAddPacket())
		{
			CreateAnime(packet.config, packet.GlobalPosition, LevelEditorWaveEditor.Instance.currentPacketContainer);
		}
		if (num != currentIndex)
		{
			if (currentIndex != -1)
			{
				packetList[currentIndex].Reset();
			}
			currentIndex = num;
		}
	}

	public void PacketNameChoose(string packetName)
	{
		AudioManager.Instance.AudioPlay("PacketPick");
		if (LevelEditorWaveEditor.Instance.currentPacketContainer == null || !LevelEditorWaveEditor.Instance.currentPacketContainer.CanAddPacket())
		{
			return;
		}
		bool flag = false;
		foreach (TowerDefenseInGamePacketShow packet in packetList)
		{
			if (packet.config.saveKey == packetName && packet.alive)
			{
				flag = true;
				PacketChoose(packet);
				break;
			}
		}
		if (!flag)
		{
			TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(packetName);
			Vector2 globalPosition = GetViewport().GetCamera2D().GlobalPosition;
			CreateAnime(packetConfig, globalPosition + new Vector2(300f, 260f), LevelEditorWaveEditor.Instance.currentPacketContainer);
		}
	}

	public void PacketListChoose(Array _packetList)
	{
		AudioManager.Instance.AudioPlay("PacketPick");
		foreach (Node child in _animeNode.GetChildren())
		{
			child.QueueFree();
		}
		foreach (Variant _packet in _packetList)
		{
			string packetName = (string)_packet;
			PacketNameChoose(packetName);
		}
	}

	public async void CreateAnime(TowerDefensePacketConfig config, Vector2 pos, LevelEditorWaveEditorPacketContainer _packetContainer)
	{
		if (!GodotObject.IsInstanceValid(config) || !GodotObject.IsInstanceValid(_packetContainer) || !_packetContainer.CanAddPacket())
		{
			return;
		}
		TowerDefenseInGamePacketShow packet = _packetContainer.AddPacket(config);
		if (!GodotObject.IsInstanceValid(packet))
		{
			return;
		}
		packet.Visible = false;
		TowerDefenseInGamePacketShow animePacket = TowerDefenseManager.CreatePacketShow();
		animePacket.setPcLayout = true;
		_animeNode.AddChild(animePacket, forceReadableName: false, InternalMode.Disabled);
		animePacket.Init(config);
		animePacket.onlyDraw = true;
		animePacket.GlobalPosition = pos;
		try
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			if (GodotObject.IsInstanceValid(animePacket) && GodotObject.IsInstanceValid(_packetContainer) && !packet.IsQueuedForDeletion())
			{
				int num = _packetContainer.packetList.IndexOf(packet);
				if (!_packetContainer.TryGetPacketPos(num, out var position))
				{
					GD.PushWarning($"[WaveEditor] Packet animation target slot is unavailable: {num}.");
				}
				else
				{
					Tween tween = animePacket.CreateTween();
					tween.SetEase(Tween.EaseType.Out);
					tween.SetTrans(Tween.TransitionType.Quart);
					tween.TweenProperty(animePacket, "global_position", position, 0.5);
					await ToSignal(tween, Tween.SignalName.Finished);
				}
			}
		}
		finally
		{
			if (GodotObject.IsInstanceValid(packet) && !packet.IsQueuedForDeletion())
			{
				packet.Visible = true;
			}
			if (GodotObject.IsInstanceValid(animePacket) && !animePacket.IsQueuedForDeletion())
			{
				animePacket.QueueFree();
			}
		}
	}

	public void PacketClear()
	{
		foreach (Node child in _packetContainer.GetChildren())
		{
			child.QueueFree();
		}
		packetList = new Array<TowerDefenseInGamePacketShow>();
		currentIndex = 0;
	}

	public async void CategoryChoose(string _category, bool reFresh = false)
	{
		if (currentCategory == _category && !reFresh)
		{
			return;
		}
		currentCategory = _category;
		PacketClear();
		if (!(data.AsGodotObject() is TowerDefensePacketBankData towerDefensePacketBankData) || !towerDefensePacketBankData.category.ContainsKey(_category))
		{
			return;
		}
		_categoryGeneration++;
		int currentGeneration = _categoryGeneration;
		Array array = towerDefensePacketBankData.category[_category].AsGodotArray();
		Array array2 = new Array();
		Array array3 = new Array();
		foreach (Variant item in array)
		{
			string text = (string)item;
			if (GameSaveManager.Instance.GetTowerDefensePacketValue(text).GetValueOrDefault("Love", false).AsBool())
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
		int batchCount = 0;
		foreach (Variant item3 in array2)
		{
			string packetName = (string)item3;
			if (_categoryGeneration != currentGeneration)
			{
				return;
			}
			TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(packetName);
			TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = TowerDefenseManager.CreatePacketShow();
			towerDefenseInGamePacketShow.setPcLayout = true;
			towerDefenseInGamePacketShow.enforceRuntimeAvailabilityOnPress = false;
			_packetContainer.AddChild(towerDefenseInGamePacketShow, forceReadableName: false, InternalMode.Disabled);
			towerDefenseInGamePacketShow.Init(packetConfig);
			towerDefenseInGamePacketShow.showLove = true;
			towerDefenseInGamePacketShow.OnLoveChange += LoveChange;
			towerDefenseInGamePacketShow.OnPressed += PacketChoose;
			packetList.Add(towerDefenseInGamePacketShow);
			batchCount++;
			if (batchCount >= 8)
			{
				batchCount = 0;
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				if (_categoryGeneration != currentGeneration)
				{
					return;
				}
			}
		}
	}

	public void LoveChange(TowerDefenseInGamePacketShow packet)
	{
		CategoryChoose(currentCategory, reFresh: true);
	}
}
