using Godot;

[GlobalClass]
public partial class TowerDefenseAwardCollectable : TowerDefenseAwardBase
{
	private Sprite2D sprite;

	private AwardRay awardRay;

	private Sprite2D awardPickupGlow;

	private Sprite2D downArrow;

	public override void _Ready()
	{
		base._Ready();
		sprite = GetNodeOrNull<Sprite2D>("%Sprite");
		awardRay = GetNodeOrNull<AwardRay>("%AwardRay");
		awardPickupGlow = GetNodeOrNull<Sprite2D>("%AwardPickupGlow");
		downArrow = GetNodeOrNull<Sprite2D>("%DownArrow");
		GetNode<Button>("%Button").Pressed += Pressed;
	}

	public override void Init(string collectableName)
	{
		CollectableConfig collectable = TowerDefenseManager.GetCollectable(collectableName);
		if (collectable.config is ShovelConfig)
		{
			ShovelConfig shovelConfig = (ShovelConfig)collectable.config;
			sprite.Texture = shovelConfig.texture;
			sprite.Scale = Vector2.One * 80f / sprite.Texture.GetWidth();
			if (GameSaveManager.Instance.GetKeyValue("CurrentShovel").AsString() == "ShovelDefault")
			{
				GameSaveManager.Instance.SetKeyValue("CurrentShovel", collectableName);
			}
		}
		if (collectable.config is AwardSettlementConfig)
		{
			AwardSettlementConfig awardSettlementConfig = (AwardSettlementConfig)collectable.config;
			sprite.Texture = awardSettlementConfig.texture;
		}
	}

	public override async void Pressed()
	{
		if (!press)
		{
			press = true;
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
			tween.SetParallel(parallel: false);
			tween.TweenProperty(sprite, "modulate:a", 0.0, 2.0);
			await ToSignal(tween, Tween.SignalName.Finished);
			if (Global.Instance.isMultiplayerMode && !MultiPlayerManager.Instance.isHost)
			{
				MultiPlayerManager.Instance.SendClientReady();
				SceneManager.Instance.ChangeScene("MainMenu");
			}
			else
			{
				SceneManager.Instance.ChangeScene("AwardSettlement");
			}
		}
	}
}
