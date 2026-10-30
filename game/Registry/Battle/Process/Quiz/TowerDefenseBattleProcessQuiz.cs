using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseBattleProcessQuiz : TowerDefenseBattleProcess
{
	private sealed class LineButtonBinding
	{
		public MainButton Button { get; }

		public Action Handler { get; }

		public LineButtonBinding(MainButton button, Action handler)
		{
			Button = button;
			Handler = handler;
		}
	}

	private static PackedScene _quizControlScene;

	private static PackedScene _changePresentBoxButtonScene;

	private static PackedScene _betPanelScene;

	public int stripPos = 5;

	public bool running;

	public List<TowerDefenseCharacter> vaseList = new List<TowerDefenseCharacter>();

	public int multiple = 1;

	public bool over;

	public int useCoin;

	public TowerDefenseBattleProcessQuizConfig config;

	public bool quizStarted;

	public QuizControl quizUI;

	public TowerDefenseBattleFeatureMap mapFeature;

	public TowerDefenseBattleFeatureProgress progressFeature;

	public TowerDefenseBattleFeatureBrain brainFeature;

	private readonly List<LineButtonBinding> _lineButtonBindings = new List<LineButtonBinding>();

	private DialogBoxChoose _pendingRunDialog;

	private DialogBoxChoose.ChooseTrueEventHandler _pendingRunHandler;

	private int _pendingRunCost;

	private static PackedScene QuizControlScene => _quizControlScene ?? (_quizControlScene = GD.Load<PackedScene>("uid://dlywvjp6hpbgu"));

	private static PackedScene ChangePresentBoxButtonScene => _changePresentBoxButtonScene ?? (_changePresentBoxButtonScene = GD.Load<PackedScene>("uid://bjtvgkckq2ft5"));

	private static PackedScene BetPanelScene => _betPanelScene ?? (_betPanelScene = GD.Load<PackedScene>("uid://knp6uvl6ja3d"));

	public override bool CanLoadProgress()
	{
		return false;
	}

	public override void OnReady()
	{
	}

	public void SetupUI()
	{
		RunLifetimeTask(SetupUIAsync, "SetupUI");
	}

	private async Task SetupUIAsync()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		if (base.IsLifetimeActive && GodotObject.IsInstanceValid(progressFeature) && GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			string levelName = Tr(TowerDefenseManager.Instance.currentLevelConfig.levelName).Replace("{LevelNumber}", TowerDefenseManager.Instance.currentLevelConfig.levelNumber.ToString());
			progressFeature.SetLevelName(levelName);
			progressFeature.SetDifficultVisible(visible: false);
		}
	}

	public override Task GameInit()
	{
		CreateUI();
		SetupUI();
		return Task.CompletedTask;
	}

	public override Task GameInitFromProgress()
	{
		CreateUI();
		SetupUI();
		return Task.CompletedTask;
	}

	public void CreateUI()
	{
		DestroyQuizUI();
		quizUI = QuizControlScene.Instantiate<QuizControl>(PackedScene.GenEditState.Disabled);
		control.AddUI(quizUI, 4);
		quizUI.OnRunButtonPressed += RunButtonPressed;
		TowerDefenseManager.Instance.coinBank.ShowCoinBank(new Vector2(250f, 557f), still: true);
		quizUI.Visible = true;
		quizUI.startGUINode.Visible = true;
		Vector2I gridNum = mapFeature.config.gridNum;
		for (int i = 1; i <= gridNum.Y; i++)
		{
			MainButton button = ChangePresentBoxButtonScene.Instantiate<MainButton>(PackedScene.GenEditState.Disabled);
			button.GlobalPosition = new Vector2((float)TowerDefenseManager.Instance.GetMapGroundRight(), TowerDefenseManager.Instance.GetMapCellPos(new Vector2I(0, i)).Y + 20f);
			quizUI.changePresentBoxButtonNode.AddChild(button, forceReadableName: false, Node.InternalMode.Disabled);
			int lineY = i;
			Action action = delegate
			{
				ChangePresentBoxButtonPressed(lineY, button);
			};
			button.Pressed += action;
			_lineButtonBindings.Add(new LineButtonBinding(button, action));
			BetPanel betPanel = BetPanelScene.Instantiate<BetPanel>(PackedScene.GenEditState.Disabled);
			betPanel.GlobalPosition = new Vector2(0f, TowerDefenseManager.Instance.GetMapCellPos(new Vector2I(0, i)).Y + 30f);
			quizUI.betPanelNode.AddChild(betPanel, forceReadableName: false, Node.InternalMode.Disabled);
			betPanel.Init(i);
		}
	}

	public override void Init(Dictionary _data)
	{
		base.Init(_data);
		config = new TowerDefenseBattleProcessQuizConfig();
		config.Init(data);
		mapFeature = GetFeature("Map") as TowerDefenseBattleFeatureMap;
		progressFeature = GetFeature("Progress") as TowerDefenseBattleFeatureProgress;
		brainFeature = GetFeature("Brain") as TowerDefenseBattleFeatureBrain;
	}

	public void StartQuiz()
	{
		if (quizStarted)
		{
			return;
		}
		quizStarted = true;
		Vector2I gridNum = mapFeature.config.gridNum;
		stripPos = config.ResolveStripColumn(gridNum.X);
		mapFeature.currentMap.UseStripe(stripPos);
		if (brainFeature != null)
		{
			brainFeature.BrainInit();
		}
		for (int i = 1; i <= gridNum.X; i++)
		{
			for (int j = 1; j <= gridNum.Y; j++)
			{
				Vector2I gridPos = new Vector2I(i, j);
				if (i <= stripPos)
				{
					CreatePresentBox(gridPos);
				}
				else
				{
					CreateZombieVase(gridPos);
				}
			}
		}
	}

	public void Refresh()
	{
		RunLifetimeTask(RefreshAsync, "Refresh");
	}

	private async Task RefreshAsync()
	{
		if (quizUI == null)
		{
			return;
		}
		TowerDefenseManager.Instance.currentControl.isGameRunning = false;
		useCoin = 0;
		running = false;
		over = false;
		multiple = 1;
		quizStarted = false;
		quizUI.coinLabel.Text = $"金币倍数：{multiple}倍";
		vaseList.Clear();
		Vector2I gridNum = mapFeature.config.gridNum;
		stripPos = config.ResolveStripColumn(gridNum.X);
		for (int i = 1; i <= gridNum.X; i++)
		{
			for (int j = 1; j <= gridNum.Y; j++)
			{
				Vector2I gridPos = new Vector2I(i, j);
				ClearCell(gridPos);
			}
		}
		if (GodotObject.IsInstanceValid(ProjectileUpdateManager.Instance))
		{
			ProjectileUpdateManager.Instance.Clear();
		}
		foreach (Node item in GetTree().GetNodesInGroup("Projectile"))
		{
			item.QueueFree();
		}
		foreach (Variant item2 in TowerDefenseManager.Instance.GetCharacter())
		{
			TowerDefenseCharacter towerDefenseCharacter = (TowerDefenseCharacter)(GodotObject)item2;
			towerDefenseCharacter.skipDestroySet = true;
			TowerDefenseManager.Instance.CharacterUnregister(towerDefenseCharacter);
			towerDefenseCharacter.RemoveFromGroup("Character");
			towerDefenseCharacter.QueueFree();
		}
		foreach (Variant iceCap in mapFeature.iceCapList)
		{
			Node node = (Node)(GodotObject)iceCap;
			if (GodotObject.IsInstanceValid(node))
			{
				node.QueueFree();
			}
		}
		foreach (Node item3 in GetTree().GetNodesInGroup("Brain"))
		{
			item3.QueueFree();
		}
		DestroyQuizUI();
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		if (base.IsLifetimeActive)
		{
			CreateUI();
			StartQuiz();
		}
	}

	public void ClearCell(Vector2I gridPos)
	{
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(gridPos);
		List<TowerDefenseCharacter> list = new List<TowerDefenseCharacter>();
		foreach (TowerDefenseCharacter character in mapCell.characterList)
		{
			list.Add(character);
		}
		foreach (TowerDefenseCharacter item in list)
		{
			item.skipDestroySet = true;
		}
		mapCell.Clear();
	}

	public void CreatePresentBox(Vector2I gridPos, bool open = true)
	{
		RunLifetimeTask(() => CreatePresentBoxAsync(gridPos, open), "CreatePresentBox");
	}

	private async Task CreatePresentBoxAsync(Vector2I gridPos, bool open)
	{
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(gridPos);
		if (GodotObject.IsInstanceValid(mapCell))
		{
			if (mapCell.gridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.BRICK) || mapCell.gridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.SOIL))
			{
				TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(config.potPacketName);
				if (GodotObject.IsInstanceValid(packetConfig))
				{
					packetConfig.Plant(gridPos);
				}
			}
			if (mapCell.gridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.WATER))
			{
				TowerDefensePacketConfig packetConfig2 = TowerDefenseManager.GetPacketConfig(config.lilyPadPacketName);
				if (GodotObject.IsInstanceValid(packetConfig2))
				{
					packetConfig2.Plant(gridPos);
				}
			}
		}
		TowerDefensePacketConfig packetConfig3 = TowerDefenseManager.GetPacketConfig(config.presentBoxPacketName);
		if (!GodotObject.IsInstanceValid(packetConfig3))
		{
			return;
		}
		TowerDefenseCharacter presentBox = packetConfig3.Plant(gridPos);
		if (!GodotObject.IsInstanceValid(presentBox))
		{
			return;
		}
		presentBox.AddToGroup("PresentBox");
		presentBox.ProcessMode = Node.ProcessModeEnum.Disabled;
		presentBox.Set("packetBank", config.presentBoxPacketBank);
		if (open)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			if (base.IsLifetimeActive && GodotObject.IsInstanceValid(presentBox))
			{
				presentBox.ProcessMode = Node.ProcessModeEnum.Inherit;
				presentBox.instance.invincible = true;
				presentBox.sprite.SetAnimation("Open", loop: false);
			}
		}
	}

	public void CreateZombieVase(Vector2I gridPos)
	{
		RunLifetimeTask(() => CreateZombieVaseAsync(gridPos), "CreateZombieVase");
	}

	private async Task CreateZombieVaseAsync(Vector2I gridPos)
	{
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(config.zombieVasePacketName);
		if (!GodotObject.IsInstanceValid(packetConfig))
		{
			return;
		}
		TowerDefenseCharacter zombieVase = packetConfig.Plant(gridPos);
		if (zombieVase == null)
		{
			return;
		}
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		if (base.IsLifetimeActive && GodotObject.IsInstanceValid(zombieVase))
		{
			IStateMachineController stateMachine = zombieVase.StateMachine;
			if (stateMachine != null && stateMachine.IsInitialized)
			{
				zombieVase.SetMainStateMachineDispatchEnabled(enabled: true);
				zombieVase.Set("packetBank", config.zombieVasePacketBank);
				vaseList.Add(zombieVase);
			}
		}
	}

	public void ChangePresentBox(int line)
	{
		RunLifetimeTask(() => ChangePresentBoxAsync(line), "ChangePresentBox");
	}

	private async Task ChangePresentBoxAsync(int line)
	{
		for (int x = 1; x <= stripPos; x++)
		{
			Vector2I gridPos = new Vector2I(x, line);
			TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(gridPos);
			List<TowerDefenseCharacter> list = new List<TowerDefenseCharacter>();
			foreach (TowerDefenseCharacter character in mapCell.characterList)
			{
				list.Add(character);
			}
			foreach (TowerDefenseCharacter item in list)
			{
				item.skipDestroySet = true;
			}
			mapCell.Clear();
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			if (!base.IsLifetimeActive)
			{
				break;
			}
			CreatePresentBox(gridPos, open: false);
		}
	}

	public void ChangePresentBoxButtonPressed(int line, MainButton button)
	{
		if (quizUI == null)
		{
			return;
		}
		int num = 0;
		foreach (Node child in quizUI.betPanelNode.GetChildren())
		{
			if (child is BetPanel betPanel)
			{
				num += (int)betPanel.betCoinSpinBox.Value;
			}
		}
		if (TowerDefenseManager.Instance.GetCoin() < num * (multiple + 2))
		{
			DialogManager.Instance.DialogCreate("DialogBoxTips").Set("text", $"[center][font_size=24]您的金币不足[/font_size][/center]\n[center][font_size=24]至少需要{num * (multiple + 2)}金币[/font_size][/center]");
			return;
		}
		ChangePresentBox(line);
		RemoveLineButtonBinding(button);
		button.QueueFree();
		multiple++;
		quizUI.coinLabel.Text = $"金币倍数：{multiple}倍";
	}

	public void RunButtonPressed()
	{
		if (quizUI == null)
		{
			return;
		}
		int num = 0;
		foreach (Node child in quizUI.betPanelNode.GetChildren())
		{
			if (child is BetPanel betPanel)
			{
				num += (int)betPanel.betCoinSpinBox.Value;
			}
		}
		if (TowerDefenseManager.Instance.GetCoin() < num * (multiple + 1))
		{
			DialogManager.Instance.DialogCreate("DialogBoxTips").Set("text", $"[center][font_size=24]您的金币不足[/font_size][/center]\n[center][font_size=24]至少需要{num * (multiple + 1)}金币[/font_size][/center]");
			return;
		}
		DisconnectPendingRunDialog();
		DialogBoxBase dialogBoxBase = DialogManager.Instance.DialogCreate("DialogBoxChoose");
		dialogBoxBase.Set("text", $"[center][font_size=24]是否使用{num * multiple}金币入场[/font_size][/center]");
		_pendingRunDialog = dialogBoxBase as DialogBoxChoose;
		if (GodotObject.IsInstanceValid(_pendingRunDialog))
		{
			_pendingRunCost = num * multiple;
			_pendingRunHandler = ConfirmPendingRun;
			_pendingRunDialog.OnChooseTrue += _pendingRunHandler;
		}
	}

	private void ConfirmPendingRun()
	{
		int pendingRunCost = _pendingRunCost;
		DisconnectPendingRunDialog();
		Run(pendingRunCost);
	}

	private void DisconnectPendingRunDialog()
	{
		if (GodotObject.IsInstanceValid(_pendingRunDialog) && _pendingRunHandler != null)
		{
			_pendingRunDialog.OnChooseTrue -= _pendingRunHandler;
		}
		_pendingRunDialog = null;
		_pendingRunHandler = null;
		_pendingRunCost = 0;
	}

	private void RemoveLineButtonBinding(MainButton button)
	{
		for (int num = _lineButtonBindings.Count - 1; num >= 0; num--)
		{
			LineButtonBinding lineButtonBinding = _lineButtonBindings[num];
			if (lineButtonBinding.Button == button)
			{
				if (GodotObject.IsInstanceValid(lineButtonBinding.Button))
				{
					lineButtonBinding.Button.Pressed -= lineButtonBinding.Handler;
				}
				_lineButtonBindings.RemoveAt(num);
			}
		}
	}

	private void DestroyQuizUI()
	{
		DisconnectPendingRunDialog();
		foreach (LineButtonBinding lineButtonBinding in _lineButtonBindings)
		{
			if (GodotObject.IsInstanceValid(lineButtonBinding.Button))
			{
				lineButtonBinding.Button.Pressed -= lineButtonBinding.Handler;
			}
		}
		_lineButtonBindings.Clear();
		if (GodotObject.IsInstanceValid(quizUI))
		{
			quizUI.OnRunButtonPressed -= RunButtonPressed;
			quizUI.QueueFree();
		}
		quizUI = null;
	}

	public void Run(int needCoin)
	{
		RunLifetimeTask(() => RunAsync(needCoin), "Run");
	}

	private async Task RunAsync(int needCoin)
	{
		if (quizUI == null)
		{
			return;
		}
		useCoin = needCoin;
		TowerDefenseManager.Instance.UseCoin(useCoin);
		running = true;
		quizUI.startGUINode.Visible = false;
		TowerDefenseManager.Instance.currentControl.isGameRunning = true;
		foreach (TowerDefenseCharacter vase in vaseList)
		{
			if (GodotObject.IsInstanceValid(vase))
			{
				vase.Destroy();
			}
		}
		foreach (Node child in quizUI.betPanelNode.GetChildren())
		{
			if (child is BetPanel betPanel)
			{
				betPanel.Finish();
			}
		}
		foreach (Variant item in TowerDefenseManager.Instance.GetCharacter())
		{
			TowerDefenseCharacter towerDefenseCharacter = (TowerDefenseCharacter)(GodotObject)item;
			if (!(towerDefenseCharacter is TowerDefensePlantPresentBox))
			{
				towerDefenseCharacter.SetMainStateMachineDispatchEnabled(enabled: true);
				towerDefenseCharacter.Idle();
			}
		}
		foreach (Node item2 in GetTree().GetNodesInGroup("PresentBox"))
		{
			item2.ProcessMode = Node.ProcessModeEnum.Inherit;
		}
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		if (!base.IsLifetimeActive || !GodotObject.IsInstanceValid(quizUI))
		{
			return;
		}
		foreach (Node item3 in GetTree().GetNodesInGroup("PresentBox"))
		{
			if (item3 is TowerDefensePlantPresentBox towerDefensePlantPresentBox && GodotObject.IsInstanceValid(towerDefensePlantPresentBox.instance))
			{
				towerDefensePlantPresentBox.instance.invincible = true;
				towerDefensePlantPresentBox.sprite.SetAnimation("Open", loop: false);
			}
		}
		GameSaveManager.Instance.Save();
	}

	public override bool CheckFinal()
	{
		if (TowerDefenseManager.Instance.GetCampTarget(TowerDefenseEnum.CHARACTER_CAMP.PLANT).Count <= 0)
		{
			if (GetTree().GetNodeCountInGroup("Vase") > 0)
			{
				foreach (Node item in GetTree().GetNodesInGroup("Vase"))
				{
					if (item is TowerDefenseCharacter towerDefenseCharacter)
					{
						towerDefenseCharacter.Destroy();
					}
				}
				return false;
			}
			return true;
		}
		if (CheckFail())
		{
			return true;
		}
		return false;
	}

	public override bool CheckFail()
	{
		if (GetTree().GetNodeCountInGroup("Vase") > 0)
		{
			return false;
		}
		foreach (Variant item in TowerDefenseManager.Instance.GetCampTarget(TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE))
		{
			TowerDefenseCharacter towerDefenseCharacter = (TowerDefenseCharacter)(GodotObject)item;
			if (!towerDefenseCharacter.instance.die && !towerDefenseCharacter.instance.nearDie)
			{
				return false;
			}
		}
		return true;
	}

	public void Final()
	{
		RunLifetimeTask(FinalAsync, "Final");
	}

	private async Task FinalAsync()
	{
		if (over)
		{
			return;
		}
		over = true;
		if (quizUI == null)
		{
			return;
		}
		TowerDefenseManager.Instance.currentControl.checkBox2X.ButtonPressed = false;
		running = false;
		int coinNum = 0;
		Vector2I gridNum = mapFeature.config.gridNum;
		bool[] zombieWinLine = new bool[gridNum.Y + 1];
		for (int i = 0; i < zombieWinLine.Length; i++)
		{
			zombieWinLine[i] = true;
		}
		foreach (Node item in GetTree().GetNodesInGroup("Brain"))
		{
			if (item is TowerDefenseCharacter towerDefenseCharacter)
			{
				zombieWinLine[towerDefenseCharacter.gridPos.Y] = false;
			}
		}
		string numText = "";
		for (int y = 1; y <= gridNum.Y; y++)
		{
			bool flag = false;
			bool flag2 = false;
			BetPanel obj = quizUI.betPanelNode.GetChild(y - 1) as BetPanel;
			int num = (int)obj.betCoinSpinBox.Value;
			switch (obj.chooseLabel.Text)
			{
			case "赢":
				if (zombieWinLine[y])
				{
					flag = true;
				}
				break;
			case "输":
				if (!zombieWinLine[y])
				{
					flag = true;
				}
				break;
			case "跳":
				flag2 = true;
				break;
			}
			BroadCastManager.Instance.BraodCastClear();
			BroadCastConfig broadCastConfig = new BroadCastConfig();
			if (flag2)
			{
				broadCastConfig.broadCastString = $"第{y}行:不结算";
			}
			else if (flag)
			{
				broadCastConfig.broadCastString = $"第{y}行:判断成功 结算:{num}金币";
				numText = ((y != 1) ? (numText + $"+{num}") : (numText + $"{num}"));
				coinNum += num * (multiple + 1);
			}
			else
			{
				broadCastConfig.broadCastString = $"第{y}行:判断失败 结算:-{num}金币";
				numText = ((y != 1) ? (numText + $"-{num}") : (numText + $"-{num}"));
				coinNum -= num * (multiple + 1);
			}
			BroadCastManager.Instance.BroadCastAdd(broadCastConfig);
			await ToSignal(GetTree().CreateTimer(config.settlementLineDelay, processAlways: false), SceneTreeTimer.SignalName.Timeout);
			if (!base.IsLifetimeActive)
			{
				return;
			}
		}
		BroadCastManager.Instance.BraodCastClear();
		BroadCastConfig broadCastConfig2 = new BroadCastConfig();
		if (coinNum > 0)
		{
			broadCastConfig2.broadCastString = $"总结算:({numText})x{multiple + 1}={coinNum}\n获得{coinNum}金币\n返还{useCoin}金币";
		}
		else if (coinNum < 0)
		{
			if (coinNum + useCoin > 0)
			{
				broadCastConfig2.broadCastString = $"总结算:({numText})x{multiple + 1}={coinNum}\n返还{coinNum + useCoin}金币";
			}
			else
			{
				broadCastConfig2.broadCastString = $"总结算:({numText})x{multiple + 1}={coinNum}\n扣除{Math.Abs(coinNum + useCoin)}金币";
			}
		}
		BroadCastManager.Instance.BroadCastAdd(broadCastConfig2);
		if (coinNum > 0)
		{
			await CreateCoinAsync(coinNum + useCoin);
		}
		else if (coinNum < 0)
		{
			if (coinNum + useCoin > 0)
			{
				await CreateCoinAsync(coinNum + useCoin);
			}
			else
			{
				TowerDefenseManager.Instance.UseCoin(Math.Abs(coinNum + useCoin));
			}
		}
		if (base.IsLifetimeActive)
		{
			await ToSignal(GetTree().CreateTimer(config.settlementSummaryDelay, processAlways: false), SceneTreeTimer.SignalName.Timeout);
			if (base.IsLifetimeActive)
			{
				BroadCastManager.Instance.BraodCastClear();
				ViewManager.Instance.FullScreenColorBlink(Colors.White, 0.2, rise: false);
				AudioManager.Instance.AudioPlay("WaveHuge");
				Refresh();
				GameSaveManager.Instance.Save();
			}
		}
	}

	public void CreateCoin(int num)
	{
		RunLifetimeTask(() => CreateCoinAsync(num), "CreateCoin");
	}

	private async Task CreateCoinAsync(int num)
	{
		if (quizUI == null)
		{
			return;
		}
		Vector2I gridNum = mapFeature.config.gridNum;
		while (num >= 1000)
		{
			TowerDefenseGroundItemBase towerDefenseGroundItemBase = TowerDefenseManager.Instance.FallingObjectItemCreate(ObjectManagerConfig.OBJECT.COIN_DIAMOND, TowerDefenseManager.Instance.GetMapCellPosCenter(new Vector2I(GD.RandRange(1, gridNum.X), GD.RandRange(1, gridNum.Y))), 30.0, new Vector2((float)GD.RandRange(-100.0, 100.0), -400f), 980.0);
			TowerDefenseCoinBase coinItem = towerDefenseGroundItemBase as TowerDefenseCoinBase;
			if (!GodotObject.IsInstanceValid(coinItem))
			{
				return;
			}
			coinItem.canMagnet = false;
			coinItem.gridPos = new Vector2I(coinItem.gridPos.X, 200);
			coinItem.Reparent(quizUI, keepGlobalTransform: false);
			RunLifetimeTask(() => SettleQuizCoinAsync(coinItem), "SettleQuizCoinAsync");
			num -= 1000;
			await ToSignal(GetTree().CreateTimer(config.coinSpawnInterval, processAlways: false), SceneTreeTimer.SignalName.Timeout);
			if (!base.IsLifetimeActive)
			{
				return;
			}
		}
		while (num >= 50)
		{
			TowerDefenseGroundItemBase towerDefenseGroundItemBase2 = TowerDefenseManager.Instance.FallingObjectItemCreate(ObjectManagerConfig.OBJECT.COIN_GOLD, TowerDefenseManager.Instance.GetMapCellPosCenter(new Vector2I(GD.RandRange(1, gridNum.X), GD.RandRange(1, gridNum.Y))), 30.0, new Vector2((float)GD.RandRange(-100.0, 100.0), -400f), 980.0);
			TowerDefenseCoinBase coinItem2 = towerDefenseGroundItemBase2 as TowerDefenseCoinBase;
			if (!GodotObject.IsInstanceValid(coinItem2))
			{
				return;
			}
			coinItem2.canMagnet = false;
			coinItem2.gridPos = new Vector2I(coinItem2.gridPos.X, 200);
			coinItem2.Reparent(quizUI, keepGlobalTransform: false);
			RunLifetimeTask(() => SettleQuizCoinAsync(coinItem2), "SettleQuizCoinAsync");
			num -= 50;
			await ToSignal(GetTree().CreateTimer(config.coinSpawnInterval, processAlways: false), SceneTreeTimer.SignalName.Timeout);
			if (!base.IsLifetimeActive)
			{
				return;
			}
		}
		while (num >= 10)
		{
			TowerDefenseGroundItemBase towerDefenseGroundItemBase3 = TowerDefenseManager.Instance.FallingObjectItemCreate(ObjectManagerConfig.OBJECT.COIN_SILVER, TowerDefenseManager.Instance.GetMapCellPosCenter(new Vector2I(GD.RandRange(1, gridNum.X), GD.RandRange(1, gridNum.Y))), 30.0, new Vector2((float)GD.RandRange(-100.0, 100.0), -400f), 980.0);
			TowerDefenseCoinBase coinItem3 = towerDefenseGroundItemBase3 as TowerDefenseCoinBase;
			if (!GodotObject.IsInstanceValid(coinItem3))
			{
				break;
			}
			coinItem3.canMagnet = false;
			coinItem3.gridPos = new Vector2I(coinItem3.gridPos.X, 200);
			coinItem3.Reparent(quizUI, keepGlobalTransform: false);
			RunLifetimeTask(() => SettleQuizCoinAsync(coinItem3), "SettleQuizCoinAsync");
			num -= 10;
			await ToSignal(GetTree().CreateTimer(config.coinSpawnInterval, processAlways: false), SceneTreeTimer.SignalName.Timeout);
			if (!base.IsLifetimeActive)
			{
				break;
			}
		}
	}

	private async Task SettleQuizCoinAsync(TowerDefenseCoinBase coinItem)
	{
		await ToSignal(GetTree().CreateTimer(config.coinFlightDelay, processAlways: false), SceneTreeTimer.SignalName.Timeout);
		if (base.IsLifetimeActive && GodotObject.IsInstanceValid(coinItem))
		{
			if (GodotObject.IsInstanceValid(coinItem.moveComponent))
			{
				coinItem.moveComponent.MoveClear();
			}
			await ToSignal(GetTree().CreateTimer(config.coinCollectDelay, processAlways: false), SceneTreeTimer.SignalName.Timeout);
			if (base.IsLifetimeActive && GodotObject.IsInstanceValid(coinItem))
			{
				coinItem.Collection();
			}
		}
	}

	public override async Task GameEntry()
	{
		await GameReady();
	}

	public override Task GameReady()
	{
		if (progressFeature != null)
		{
			progressFeature.SetProgressMeterMaxValue(GetTree().GetNodeCountInGroup("Brain"));
			progressFeature.SetProgressMeterWaveNum(GetTree().GetNodeCountInGroup("Brain"));
			progressFeature.SetProgressMeterPreviewWave(0);
			progressFeature.SetProgressMeterValue(0.0);
			progressFeature.SetLevelNameVisible(visible: true);
			if (Global.Instance.enterLevelMode == "DailyLevel" || Global.Instance.enterLevelMode == "OnlineLevel" || Global.Instance.enterLevelMode == "LevelTest" || Global.Instance.enterLevelMode == "DiyLevel")
			{
				progressFeature.SetDifficultVisible(visible: false);
			}
		}
		return Task.CompletedTask;
	}

	public override Task GameStart()
	{
		StartQuiz();
		return Task.CompletedTask;
	}

	public override Task GameStartFromProgress()
	{
		StartQuiz();
		return Task.CompletedTask;
	}

	public override void GameFail(TowerDefenseCharacter enterCharacter)
	{
	}

	public override void ZombieEnterHouse(TowerDefenseCharacter character)
	{
		character.Destroy();
	}

	public override void Finish()
	{
	}

	public override void Destroy()
	{
		base.Destroy();
		running = false;
		over = true;
		DestroyQuizUI();
		if (GodotObject.IsInstanceValid(BroadCastManager.Instance))
		{
			BroadCastManager.Instance.BraodCastClear();
		}
		vaseList.Clear();
		mapFeature = null;
		progressFeature = null;
		brainFeature = null;
		config = null;
	}

	public override Dictionary SaveProcess()
	{
		GD.Print("[Save] 保存Process[Quiz]...");
		Dictionary result = new Dictionary
		{
			{ "stripPos", stripPos },
			{ "running", running },
			{ "multiple", multiple },
			{ "over", over },
			{ "useCoin", useCoin }
		};
		GD.Print($"[Save] Process[Quiz]保存完成: stripPos={stripPos}, running={running}, multiple={multiple}, over={over}, useCoin={useCoin}");
		return result;
	}

	public override void LoadProcess(Dictionary _data, TowerDefenseLevelSaveConfigCSharp _owner)
	{
		GD.Print($"[Load] 加载Process[Quiz]... (数据项: {_data.Count})");
		stripPos = _data.GetValueOrDefault("stripPos", 5).AsInt32();
		running = _data.GetValueOrDefault("running", false).AsBool();
		multiple = _data.GetValueOrDefault("multiple", 1).AsInt32();
		over = _data.GetValueOrDefault("over", false).AsBool();
		useCoin = _data.GetValueOrDefault("useCoin", 0).AsInt32();
		GD.Print($"[Load] Process[Quiz]加载完成: stripPos={stripPos}, running={running}, multiple={multiple}, over={over}, useCoin={useCoin}");
	}

	public override void PhysicsProcess(double delta)
	{
		if (running && CheckFinal())
		{
			Final();
		}
	}
}
