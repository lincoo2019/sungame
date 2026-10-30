using Godot;
using Godot.Collections;

[GlobalClass]
public partial class LevelEditorPacketBank : Control
{
	private ScrollContainer _packetBankScroll;

	private MarginContainer _packetBankMargin;

	private GridContainer _packetContainer;

	[Export(PropertyHint.None, "")]
	public TowerDefensePacketBankData data;

	public static LevelEditorPacketBank Instance;

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
		Instance = this;
		TryLoadDefaultPacketBank();
		base.VisibilityChanged += RefreshWhenVisible;
		CallDeferred("RefreshWhenVisible");
	}

	public override void _ExitTree()
	{
		base.VisibilityChanged -= RefreshWhenVisible;
		_categoryGeneration++;
		if (GodotObject.IsInstanceValid(_packetContainer))
		{
			PacketClear();
		}
		data = null;
		mapFeature = null;
		if (Instance == this)
		{
			Instance = null;
		}
		base._ExitTree();
	}

	public void RefreshWhenVisible()
	{
		if (!IsInsideTree() || !GodotObject.IsInstanceValid(_packetContainer))
		{
			return;
		}
		if (IsVisibleInTree())
		{
			TryLoadDefaultPacketBank();
			if (packetList.Count == 0 && GodotObject.IsInstanceValid(data))
			{
				CategoryChoose(string.IsNullOrEmpty(currentCategory) ? "White" : currentCategory, reFresh: true);
			}
		}
		else if (packetList.Count > 0)
		{
			PacketClear();
		}
	}

	private void TryLoadDefaultPacketBank()
	{
		if (!GodotObject.IsInstanceValid(data) && GodotObject.IsInstanceValid(ResourceManager.Instance) && ResourceManager.Instance.TOWERDEFENSE_PACKETBANKS.TryGetValue("Total", out var value))
		{
			data = value;
		}
	}

	public void Init(TowerDefensePacketBankData _data)
	{
		data = _data;
		PacketClear();
		CategoryChoose("White");
	}

	public void PacketChoose(TowerDefenseInGamePacketShow packet)
	{
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = mapFeature;
		if (!GodotObject.IsInstanceValid(towerDefenseBattleFeatureMap) && GodotObject.IsInstanceValid(LevelEditorMapEditor.instance))
		{
			towerDefenseBattleFeatureMap = (mapFeature = LevelEditorMapEditor.instance.mapFeature);
		}
		if (towerDefenseBattleFeatureMap == null || !GodotObject.IsInstanceValid(towerDefenseBattleFeatureMap.packetPickControl))
		{
			return;
		}
		towerDefenseBattleFeatureMap.packetPickControl.PickPacket(packet);
		int num = packetList.IndexOf(packet);
		if (num != currentIndex)
		{
			if (currentIndex != -1)
			{
				packetList[currentIndex].Reset();
			}
			currentIndex = num;
		}
	}

	public void PacketAlive(string packetName)
	{
		foreach (TowerDefenseInGamePacketShow packet in packetList)
		{
			if (packet.config.saveKey == packetName)
			{
				packet.alive = true;
				break;
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
		if (!data.category.ContainsKey(_category))
		{
			return;
		}
		_categoryGeneration++;
		int currentGeneration = _categoryGeneration;
		Array array = data.category[_category].AsGodotArray();
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
				AdobeAnimateRuntimeManager.RequestRenderRootRepublish();
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				if (_categoryGeneration != currentGeneration)
				{
					return;
				}
			}
		}
		if (batchCount > 0)
		{
			AdobeAnimateRuntimeManager.RequestRenderRootRepublish();
		}
		if (GodotObject.IsInstanceValid(LevelEditorMapEditor.instance))
		{
			LevelEditorMapEditor.instance.Release();
		}
	}

	public void LoveChange(TowerDefenseInGamePacketShow packet)
	{
		CategoryChoose(currentCategory, reFresh: true);
	}
}
