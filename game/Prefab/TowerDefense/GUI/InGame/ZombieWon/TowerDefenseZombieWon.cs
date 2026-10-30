using Godot;

[GlobalClass]
public partial class TowerDefenseZombieWon : Node
{
	private ColorRect _colorRect;

	private AdobeAnimateSprite _sprite;

	private bool _failurePresentationStarted;

	private bool _failureDialogCreated;

	public override void _Ready()
	{
		_colorRect = GetNodeOrNull<ColorRect>("%ColorRect");
		_sprite = GetNodeOrNull<AdobeAnimateSprite>("%ZombiesWonSprite");
		if (_sprite != null)
		{
			_sprite.pause = true;
			_sprite.OnAnimeCompleted += AnimeCompleted;
		}
	}

	public async void LevelFail(bool playAnime = true)
	{
		if (_failurePresentationStarted)
		{
			return;
		}
		_failurePresentationStarted = true;
		AudioManager.Instance.AudioStopAll();
		AudioManager.Instance.AudioPlay("ZombieWon", AudioManagerEnum.TYPE.MUSIC);
		if (playAnime)
		{
			if (_sprite != null)
			{
				_sprite.Visible = true;
				_sprite.pause = false;
			}
			CreateTween().TweenProperty(_colorRect, "modulate:a", 0.5, 1.0);
			await ToSignal(GetTree().CreateTimer(2.0), SceneTreeTimer.SignalName.Timeout);
			AudioManager.Instance.AudioPlay("CrazyDaveScream", AudioManagerEnum.TYPE.MUSIC);
			ShakeSprite();
		}
		else
		{
			ShowBattleFailDialogOnce();
		}
	}

	public void AnimeCompleted(string clip)
	{
		if (!(clip != "Idle") && _failurePresentationStarted)
		{
			if (_sprite != null)
			{
				_sprite.Visible = false;
			}
			ShowBattleFailDialogOnce();
		}
	}

	private void ShowBattleFailDialogOnce()
	{
		if (!_failureDialogCreated && GodotObject.IsInstanceValid(DialogManager.Instance))
		{
			_failureDialogCreated = true;
			DialogManager.Instance.DialogCreate("BattleFail");
		}
	}

	private async void ShakeSprite()
	{
		for (int i = 0; i < 5; i++)
		{
			if (_sprite != null)
			{
				_sprite.Position = new Vector2(540f, 300f) + new Vector2(GD.RandRange(-10, 10), GD.RandRange(-10, 10));
			}
			await ToSignal(GetTree().CreateTimer(0.05), SceneTreeTimer.SignalName.Timeout);
		}
	}
}
