using Godot;

[GlobalClass]
public partial class TowerDefenseAwardTrophy : TowerDefenseAwardBase
{
	private static PackedScene _towerDefenseCoinGold;

	private static PackedScene _towerDefenseCoinSilver;

	private AwardRay awardRay;

	private Sprite2D awardPickupGlow;

	private Sprite2D downArrow;

	public int num = 250;

	private static PackedScene TOWER_DEFENSE_COIN_GOLD => _towerDefenseCoinGold ?? (_towerDefenseCoinGold = GD.Load<PackedScene>("uid://kbif4idtgolo"));

	private static PackedScene TOWER_DEFENSE_COIN_SILVER => _towerDefenseCoinSilver ?? (_towerDefenseCoinSilver = GD.Load<PackedScene>("uid://csynbfevdbiju"));

	public override void _Ready()
	{
		base._Ready();
		awardRay = GetNodeOrNull<AwardRay>("%AwardRay");
		awardPickupGlow = GetNodeOrNull<Sprite2D>("%AwardPickupGlow");
		downArrow = GetNodeOrNull<Sprite2D>("%DownArrow");
		GetNode<Button>("%Button").Pressed += Pressed;
	}

	public override void Init(string value)
	{
		num = int.Parse(value);
	}

	public override async void Pressed()
	{
		if (press)
		{
			return;
		}
		press = true;
		CreateCoin();
		AudioManager.Instance.AudioStopAll();
		AudioManager.Instance.AudioPlay("Win", AudioManagerEnum.TYPE.MUSIC);
		AudioManager.Instance.AudioPlay("AwardLightFill", AudioManagerEnum.TYPE.MUSIC);
		awardPickupGlow.Visible = false;
		downArrow.Visible = false;
		awardRay.Emit();
		Vector2 screenCenterPosition = GetViewport().GetCamera2D().GetScreenCenterPosition();
		Tween tween = CreateTween();
		tween.SetParallel();
		tween.SetEase(Tween.EaseType.Out);
		tween.SetTrans(Tween.TransitionType.Quart);
		tween.TweenProperty(this, "global_position", screenCenterPosition, 5.0);
		tween.TweenProperty(this, "scale", Vector2.One * 2f, 7.0);
		await ToSignal(tween, Tween.SignalName.Finished);
		Global.Instance.currentAwardMode = true;
		if (Global.Instance.isMultiplayerMode && !MultiPlayerManager.Instance.isHost)
		{
			MultiPlayerManager.Instance.SendClientReady();
			SceneManager.Instance.ChangeScene("MainMenu");
			return;
		}
		switch (Global.Instance.enterLevelMode)
		{
		case "ModLevel":
			SceneManager.Instance.ChangeScene("AwardSettlement");
			break;
		case "LevelChoose":
			if (Global.Instance.currentLevelChoose != "TryLevel")
			{
				TrySelectNextLevelFromLevelChoose();
				SceneManager.Instance.ChangeScene("LevelChoose");
			}
			else
			{
				SceneManager.Instance.ChangeScene("MainMenu");
			}
			break;
		case "DailyLevel":
			SceneManager.Instance.ChangeScene("MainMenu");
			break;
		case "DiyLevel":
			SceneManager.Instance.ChangeScene("LevelEditorStage");
			break;
		case "LoadLevel":
			SceneManager.Instance.ChangeScene("LevelEditorStage");
			break;
		case "OnlineLevel":
			SceneManager.Instance.ChangeScene("LevelEditorStage");
			break;
		}
	}

	public async void CreateCoin()
	{
		while (num >= 1000)
		{
			TowerDefenseCoinBase towerDefenseCoinBase = TowerDefenseManager.Instance.FallingObjectItemCreate(ObjectManagerConfig.OBJECT.COIN_DIAMOND, base.GlobalPosition - new Vector2(0f, 40f), 120.0, new Vector2((float)GD.RandRange(-100.0, 100.0), -400f), 980.0) as TowerDefenseCoinBase;
			towerDefenseCoinBase.gridPos = new Vector2I(towerDefenseCoinBase.gridPos.X, 200);
			towerDefenseCoinBase.canMagnet = false;
			GetTree().CreateTimer(1.0, processAlways: false).Timeout += towerDefenseCoinBase.moveComponent.MoveClear;
			GetTree().CreateTimer(1.5, processAlways: false).Timeout += towerDefenseCoinBase.Collection;
			num -= 1000;
			await ToSignal(GetTree().CreateTimer(0.1, processAlways: false), SceneTreeTimer.SignalName.Timeout);
		}
		while (num >= 50)
		{
			TowerDefenseCoinBase towerDefenseCoinBase2 = TowerDefenseManager.Instance.FallingObjectItemCreate(ObjectManagerConfig.OBJECT.COIN_GOLD, base.GlobalPosition - new Vector2(0f, 40f), 120.0, new Vector2((float)GD.RandRange(-100.0, 100.0), -400f), 980.0) as TowerDefenseCoinBase;
			towerDefenseCoinBase2.gridPos = new Vector2I(towerDefenseCoinBase2.gridPos.X, 200);
			towerDefenseCoinBase2.canMagnet = false;
			GetTree().CreateTimer(1.0, processAlways: false).Timeout += towerDefenseCoinBase2.moveComponent.MoveClear;
			GetTree().CreateTimer(1.5, processAlways: false).Timeout += towerDefenseCoinBase2.Collection;
			num -= 50;
			await ToSignal(GetTree().CreateTimer(0.1, processAlways: false), SceneTreeTimer.SignalName.Timeout);
		}
		while (num >= 10)
		{
			TowerDefenseCoinBase towerDefenseCoinBase3 = TowerDefenseManager.Instance.FallingObjectItemCreate(ObjectManagerConfig.OBJECT.COIN_SILVER, base.GlobalPosition - new Vector2(0f, 40f), 120.0, new Vector2((float)GD.RandRange(-100.0, 100.0), -400f), 980.0) as TowerDefenseCoinBase;
			towerDefenseCoinBase3.gridPos = new Vector2I(towerDefenseCoinBase3.gridPos.X, 200);
			towerDefenseCoinBase3.canMagnet = false;
			GetTree().CreateTimer(1.0, processAlways: false).Timeout += towerDefenseCoinBase3.moveComponent.MoveClear;
			GetTree().CreateTimer(1.5, processAlways: false).Timeout += towerDefenseCoinBase3.Collection;
			num -= 10;
			await ToSignal(GetTree().CreateTimer(0.1, processAlways: false), SceneTreeTimer.SignalName.Timeout);
		}
	}
}
