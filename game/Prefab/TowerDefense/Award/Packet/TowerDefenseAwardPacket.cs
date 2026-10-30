using Godot;

[GlobalClass]
public partial class TowerDefenseAwardPacket : TowerDefenseAwardBase
{
	private TowerDefenseInGamePacketShow packet;

	private AwardRay awardRay;

	private Sprite2D awardPickupGlow;

	private Sprite2D downArrow;

	public override void _Ready()
	{
		base._Ready();
		packet = GetNodeOrNull<TowerDefenseInGamePacketShow>("%Packet");
		awardRay = GetNodeOrNull<AwardRay>("%AwardRay");
		awardPickupGlow = GetNodeOrNull<Sprite2D>("%AwardPickupGlow");
		downArrow = GetNodeOrNull<Sprite2D>("%DownArrow");
		GetNode<Button>("%Button").Pressed += Pressed;
	}

	public override void Init(string packetName)
	{
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(packetName);
		packet.Init(packetConfig, skipGlobalChangeCost: true);
		packet.Reset();
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
			await ToSignal(tween, Tween.SignalName.Finished);
			tween = CreateTween();
			tween.TweenProperty(packet, "modulate:a", 0.0, 2.0);
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
