using Godot;
using Godot.Collections;

[GlobalClass]
public partial class ShopItem : Control
{
	public delegate void PressedSignalEventHandler(ShopItem item);

	public delegate void TalkEventHandler(string text);

	public bool editorPreviewMode;

	private Label costLabel;

	private Label describeLabel;

	private Label soldOutLabel;

	private SpriteBrightButton itemButton;

	private Control packetNode;

	private TowerDefenseInGamePacketShow _packetPreview;

	public ShopItemConfig config;

	public int currentStageId = -1;

	private int _cost;

	public int cost
	{
		get
		{
			return _cost;
		}
		set
		{
			_cost = value;
			costLabel.Text = $"$ {_cost}";
		}
	}

	public event PressedSignalEventHandler OnPressedSignal;

	public event TalkEventHandler OnTalk;

	public override void _Ready()
	{
		costLabel = GetNode<Label>("%CostLabel");
		describeLabel = GetNode<Label>("%DescribeLabel");
		soldOutLabel = GetNode<Label>("%SoldOutLabel");
		itemButton = GetNode<SpriteBrightButton>("%ItemButton");
		packetNode = GetNode<Control>("%PacketNode");
		describeLabel.MouseFilter = MouseFilterEnum.Ignore;
		soldOutLabel.MouseFilter = MouseFilterEnum.Ignore;
		itemButton.OnMouseEntered += HandleTalk;
		itemButton.OnPressed += Pressed;
	}

	public void Init(ShopItemConfig _config)
	{
		config = _config;
		Refresh();
	}

	public void Refresh()
	{
		if (editorPreviewMode)
		{
			RefreshEditorPreview();
			return;
		}
		currentStageId = -1;
		soldOutLabel.Visible = false;
		describeLabel.Visible = true;
		itemButton.disabled = false;
		itemButton.Modulate = Colors.White;
		if (GodotObject.IsInstanceValid(_packetPreview))
		{
			_packetPreview.Visible = false;
		}
		if (!GodotObject.IsInstanceValid(config) || config.stageList == null || config.stageList.Count == 0)
		{
			ShowUnavailable("未配置商品阶段");
			return;
		}
		for (int i = 0; i < config.stageList.Count; i++)
		{
			ShopItemStageConfig shopItemStageConfig = config.stageList[i];
			if (!GodotObject.IsInstanceValid(shopItemStageConfig))
			{
				continue;
			}
			string saveType = shopItemStageConfig.saveType;
			if (!(saveType == "Feature"))
			{
				if (!(saveType == "TowerDefensePacket"))
				{
					continue;
				}
				itemButton.Modulate = new Color(1f, 1f, 1f, 0f);
				TowerDefensePacketConfig packetConfigReadOnly = TowerDefenseManager.GetPacketConfigReadOnly(shopItemStageConfig.saveKey);
				if (GodotObject.IsInstanceValid(packetConfigReadOnly))
				{
					TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = EnsurePacketPreview();
					towerDefenseInGamePacketShow.ResetForPool();
					towerDefenseInGamePacketShow.setPcLayout = true;
					towerDefenseInGamePacketShow.onlyDraw = true;
					towerDefenseInGamePacketShow.Visible = true;
					if (GodotObject.IsInstanceValid(towerDefenseInGamePacketShow.button))
					{
						towerDefenseInGamePacketShow.button.MouseFilter = MouseFilterEnum.Ignore;
					}
					towerDefenseInGamePacketShow.Init(packetConfigReadOnly);
					if (!packetConfigReadOnly.Unlock())
					{
						cost = shopItemStageConfig.cost;
						describeLabel.Text = shopItemStageConfig.describe;
						currentStageId = i;
						return;
					}
				}
			}
			else
			{
				int num = GlobalFeatureManager.Instance?.GetValue(shopItemStageConfig.saveKey) ?? 0;
				if (num >= shopItemStageConfig.openMinNum && num < shopItemStageConfig.openMaxNum)
				{
					cost = shopItemStageConfig.cost;
					itemButton.Texture = shopItemStageConfig.texture;
					describeLabel.Text = shopItemStageConfig.describe;
					currentStageId = i;
					return;
				}
			}
		}
		ShopItemStageConfig shopItemStageConfig2 = null;
		for (int num2 = config.stageList.Count - 1; num2 >= 0; num2--)
		{
			if (GodotObject.IsInstanceValid(config.stageList[num2]))
			{
				shopItemStageConfig2 = config.stageList[num2];
				break;
			}
		}
		if (!GodotObject.IsInstanceValid(shopItemStageConfig2))
		{
			ShowUnavailable("商品阶段为空");
			return;
		}
		itemButton.Texture = shopItemStageConfig2.texture;
		describeLabel.Text = shopItemStageConfig2.describe;
		cost = shopItemStageConfig2.cost;
		soldOutLabel.Visible = true;
		describeLabel.Visible = false;
		itemButton.disabled = true;
	}

	private void ShowUnavailable(string message)
	{
		currentStageId = -1;
		cost = 0;
		describeLabel.Text = message;
		describeLabel.Visible = true;
		soldOutLabel.Visible = false;
		itemButton.disabled = true;
		itemButton.Modulate = Colors.White;
		if (GodotObject.IsInstanceValid(_packetPreview))
		{
			_packetPreview.Visible = false;
		}
	}

	private void RefreshEditorPreview()
	{
		currentStageId = -1;
		soldOutLabel.Visible = false;
		describeLabel.Visible = true;
		itemButton.disabled = false;
		itemButton.Modulate = Colors.White;
		if (GodotObject.IsInstanceValid(_packetPreview))
		{
			_packetPreview.Visible = false;
		}
		if (!GodotObject.IsInstanceValid(config) || config.stageList == null || config.stageList.Count == 0)
		{
			ShowUnavailable("未配置商品阶段");
			return;
		}
		ShopItemStageConfig shopItemStageConfig = null;
		for (int i = 0; i < config.stageList.Count; i++)
		{
			if (GodotObject.IsInstanceValid(config.stageList[i]))
			{
				shopItemStageConfig = config.stageList[i];
				currentStageId = i;
				break;
			}
		}
		if (!GodotObject.IsInstanceValid(shopItemStageConfig))
		{
			ShowUnavailable("商品阶段为空");
			return;
		}
		cost = shopItemStageConfig.cost;
		itemButton.Texture = shopItemStageConfig.texture;
		describeLabel.Text = shopItemStageConfig.describe;
		if (shopItemStageConfig.saveType != "TowerDefensePacket")
		{
			return;
		}
		TowerDefensePacketConfig packetConfigReadOnly = TowerDefenseManager.GetPacketConfigReadOnly(shopItemStageConfig.saveKey);
		if (GodotObject.IsInstanceValid(packetConfigReadOnly))
		{
			itemButton.Modulate = new Color(1f, 1f, 1f, 0f);
			TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = EnsurePacketPreview();
			towerDefenseInGamePacketShow.ResetForPool();
			towerDefenseInGamePacketShow.setPcLayout = true;
			towerDefenseInGamePacketShow.onlyDraw = true;
			towerDefenseInGamePacketShow.Visible = true;
			if (GodotObject.IsInstanceValid(towerDefenseInGamePacketShow.button))
			{
				towerDefenseInGamePacketShow.button.MouseFilter = MouseFilterEnum.Ignore;
			}
			towerDefenseInGamePacketShow.Init(packetConfigReadOnly);
		}
	}

	private TowerDefenseInGamePacketShow EnsurePacketPreview()
	{
		if (GodotObject.IsInstanceValid(_packetPreview))
		{
			return _packetPreview;
		}
		foreach (Node child in packetNode.GetChildren())
		{
			child.QueueFree();
		}
		_packetPreview = TowerDefenseManager.CreatePacketShow();
		_packetPreview.setPcLayout = true;
		_packetPreview.onlyDraw = true;
		_packetPreview.MouseFilter = MouseFilterEnum.Ignore;
		packetNode.AddChild(_packetPreview, forceReadableName: false, InternalMode.Disabled);
		if (GodotObject.IsInstanceValid(_packetPreview.button))
		{
			_packetPreview.button.MouseFilter = MouseFilterEnum.Ignore;
		}
		return _packetPreview;
	}

	public void Sale()
	{
		if (!TryGetCurrentStage(out var stage))
		{
			return;
		}
		string saveType = stage.saveType;
		if (!(saveType == "Feature"))
		{
			if (saveType == "TowerDefensePacket")
			{
				Dictionary towerDefensePacketValue = GameSaveManager.Instance.GetTowerDefensePacketValue(stage.saveKey);
				towerDefensePacketValue["Unlock"] = true;
				GameSaveManager.Instance.SetTowerDefensePacketValue(stage.saveKey, towerDefensePacketValue);
			}
		}
		else
		{
			GlobalFeatureManager.Instance?.AddValue(stage.saveKey, stage.addNum, saveImmediately: false);
		}
		GameSaveManager.Instance.Save();
		Refresh();
	}

	public void Pressed()
	{
		this.OnPressedSignal?.Invoke(this);
	}

	public void HandleTalk()
	{
		if (TryGetTalkStage(out var stage))
		{
			this.OnTalk?.Invoke(stage.npcTalk);
		}
	}

	private bool TryGetCurrentStage(out ShopItemStageConfig stage)
	{
		stage = null;
		if (config == null || currentStageId < 0 || currentStageId >= config.stageList.Count)
		{
			return false;
		}
		stage = config.stageList[currentStageId];
		return true;
	}

	private bool TryGetTalkStage(out ShopItemStageConfig stage)
	{
		if (TryGetCurrentStage(out stage))
		{
			return true;
		}
		stage = null;
		if (config == null || config.stageList.Count == 0)
		{
			return false;
		}
		stage = config.stageList[config.stageList.Count - 1];
		return true;
	}
}
