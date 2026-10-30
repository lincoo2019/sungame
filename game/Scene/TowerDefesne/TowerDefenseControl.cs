using Godot;

[GlobalClass]
public partial class TowerDefenseControl : Node
{
	[Export(PropertyHint.None, "")]
	public MainButton buttonPause;

	[Export(PropertyHint.None, "")]
	public CheckBox checkBox2X;

	[Export(PropertyHint.None, "")]
	public SpriteBrightButton optionButton;

	public TowerDefenseLevelBaseConfig levelConfig;

	public bool hasProgress;

	public virtual void Init(TowerDefenseLevelBaseConfig _levelConfig)
	{
		levelConfig = _levelConfig;
		levelConfig.Init();
		TowerDefenseManager.Instance.currentLevelConfig = levelConfig;
		if (this is TowerDefenseControlNew currentControl)
		{
			TowerDefenseManager.Instance.currentControl = currentControl;
		}
	}

	public override void _Ready()
	{
		if (buttonPause != null)
		{
			buttonPause.Visible = false;
			buttonPause.Toggled += ButtonPauseToggled;
		}
		if (checkBox2X != null)
		{
			checkBox2X.Visible = false;
			checkBox2X.Toggled += CheckBox2XToggled;
		}
		if (optionButton != null)
		{
			optionButton.Visible = false;
			optionButton.OnPressed += OptionButtonPressed;
		}
	}

	public void ButtonPauseToggled(bool toggled)
	{
		if (!toggled)
		{
			return;
		}
		AudioManager.Instance.AudioPlay("Pause", AudioManagerEnum.TYPE.SFX, 0.0, once: true, pauseAlive: true);
		DialogManager.Instance.DialogCreate("BattlePause").OnClose += delegate
		{
			if (GodotObject.IsInstanceValid(buttonPause))
			{
				buttonPause.ButtonPressed = false;
			}
			if (Global.IsMultiplayerMode && MultiPlayerManager.Instance.IsConnect())
			{
				if (this is TowerDefenseControlNew towerDefenseControlNew)
				{
					towerDefenseControlNew.ApplyNetworkPauseFromSession(paused: false);
				}
				MultiPlayerManager.Instance.SendResume();
			}
		};
	}

	public void CheckBox2XToggled(bool toggled)
	{
		if (toggled)
		{
			AudioManager.Instance.AudioPlay("2XSpeedOn");
			if (levelConfig is TowerDefenseLevelConfig { finishMethod: not TowerDefenseEnum.LEVEL_FINISH_METHOD.QUIZ } towerDefenseLevelConfig)
			{
				Global.TimeScale = towerDefenseLevelConfig.baseTimeScale * 1.5;
			}
			else if (levelConfig is TowerDefenseLevelConfig towerDefenseLevelConfig2)
			{
				Global.TimeScale = towerDefenseLevelConfig2.baseTimeScale * 3.0;
			}
		}
		else
		{
			AudioManager.Instance.AudioPlay("2XSpeedDown");
			Global.TimeScale = ((levelConfig is TowerDefenseLevelConfig towerDefenseLevelConfig3) ? towerDefenseLevelConfig3.baseTimeScale : 1.0);
		}
	}

	public void OptionButtonPressed()
	{
		DialogManager.Instance.DialogCreate("BattleOption");
	}
}
