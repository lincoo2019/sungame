using Godot;
using Godot.Collections;

[GlobalClass]
public partial class LevelEditorSeedBankChoose : Control
{
	private ScrollContainer _packetBankScroll;

	private MarginContainer _packetBankMargin;

	private GridContainer _packetContainer;

	private Control _animeNode;

	public static LevelEditorSeedBankChoose Instance;

	public Variant data;

	public TowerDefenseBattleFeatureMap mapFeature;

	public Array<TowerDefenseInGamePacketShow> packetList = new Array<TowerDefenseInGamePacketShow>();

	public string currentCategory = "";

	public int currentIndex = -1;

	public int _categoryGeneration;

	private const string DefaultCategory = "White";

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
				CategoryChoose(string.IsNullOrEmpty(currentCategory) ? "White" : currentCategory, reFresh: true);
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
		CategoryChoose("White");
	}

	public void ClearSelectionAnimations()
	{
		if (!GodotObject.IsInstanceValid(_animeNode))
		{
			return;
		}
		foreach (Node child in _animeNode.GetChildren())
		{
			child.QueueFree();
		}
	}

	public void PacketChoose(TowerDefenseInGamePacketShow packet)
	{
		LevelEditorSeedbankEditor.Instance.levelConfig.canExport = false;
		int num = packetList.IndexOf(packet);
		if (packet.alive && currentIndex != -1 && LevelEditorSeedbank.Instance != null && LevelEditorSeedbank.Instance.CanAddPacket())
		{
			CreateAnime(packet.config, packet.GlobalPosition);
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
		if (LevelEditorSeedbank.Instance != null && LevelEditorSeedbank.Instance.CanAddPacket())
		{
			LevelEditorSeedbank.Instance.AddPacket(TowerDefenseManager.GetPacketConfig(packetName));
		}
	}

	public void PacketListChoose(Array _packetList)
	{
		ClearSelectionAnimations();
		foreach (Variant _packet in _packetList)
		{
			if (_packet.VariantType == Variant.Type.Object && _packet.AsGodotObject() is TowerDefenseLevelPacketConfig towerDefenseLevelPacketConfig)
			{
				PacketNameChoose(towerDefenseLevelPacketConfig.packetName);
			}
			else if (_packet.VariantType == Variant.Type.String)
			{
				PacketNameChoose(_packet.AsString());
			}
		}
	}

	public async void CreateAnime(TowerDefensePacketConfig config, Vector2 pos)
	{
		TowerDefenseInGamePacketShow animePacket = TowerDefenseManager.CreatePacketShow();
		animePacket.setPcLayout = true;
		_animeNode.AddChild(animePacket, forceReadableName: false, InternalMode.Disabled);
		animePacket.Init(config);
		animePacket.onlyDraw = true;
		animePacket.GlobalPosition = pos;
		Tween tween = animePacket.CreateTween();
		tween.SetEase(Tween.EaseType.Out);
		tween.SetTrans(Tween.TransitionType.Quart);
		tween.TweenProperty(animePacket, "global_position", LevelEditorSeedbank.Instance.GetPacketPos(LevelEditorSeedbank.Instance.packetNum), 0.5);
		TowerDefenseInGamePacketShow packet = LevelEditorSeedbank.Instance.AddPacket(config);
		packet.Visible = false;
		await ToSignal(tween, Tween.SignalName.Finished);
		if (GodotObject.IsInstanceValid(packet))
		{
			packet.Visible = true;
		}
		if (GodotObject.IsInstanceValid(animePacket))
		{
			animePacket.QueueFree();
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
			towerDefenseInGamePacketShow.OnPressed += delegate(TowerDefenseInGamePacketShow p)
			{
				PacketChoose(p);
			};
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
