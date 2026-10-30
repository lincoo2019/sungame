using System.Threading.Tasks;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseBattleFeatureTutorial : TowerDefenseBattleFeature
{
	public TutorialConfig config;

	private bool _tutorialActive;

	private TutorialConfig _activeTutorialConfig;

	public override void Init(Dictionary _data)
	{
		base.Init(_data);
		if (!data.GetValueOrDefault("isCustom", false).AsBool())
		{
			string text = data.GetValueOrDefault("TutorialName", "").AsString();
			if (!(text != ""))
			{
				return;
			}
			TutorialConfig tutorial = TowerDefenseManager.GetTutorial(text);
			if (!GodotObject.IsInstanceValid(tutorial))
			{
				GD.PushWarning("[Tutorial] Tutorial '" + text + "' is unavailable.");
				return;
			}
			config = tutorial.Duplicate(deep: true) as TutorialConfig;
			if (GodotObject.IsInstanceValid(config))
			{
				config.Init();
			}
		}
		else
		{
			config = new TutorialConfig();
			config.Load(data);
		}
	}

	public override Task GameStart()
	{
		if (base.IsLifetimeActive && GodotObject.IsInstanceValid(config))
		{
			if (config.saveKey != "" && GameSaveManager.Instance.GetTutorialValue(config.saveKey))
			{
				return Task.CompletedTask;
			}
			if (!GodotObject.IsInstanceValid(TutorialManager.Instance))
			{
				return Task.CompletedTask;
			}
			_tutorialActive = true;
			_activeTutorialConfig = config;
			TutorialManager.Instance.OnTutorialFinish -= OnTutorialFinished;
			TutorialManager.Instance.OnTutorialFinish += OnTutorialFinished;
			TutorialManager.Instance.StartTutorial(config);
		}
		return Task.CompletedTask;
	}

	public override Task GameStartFromProgress()
	{
		return Task.CompletedTask;
	}

	private void OnTutorialFinished()
	{
		if (GodotObject.IsInstanceValid(TutorialManager.Instance))
		{
			TutorialManager.Instance.OnTutorialFinish -= OnTutorialFinished;
		}
		_tutorialActive = false;
		_activeTutorialConfig = null;
	}

	public override void Destroy()
	{
		base.Destroy();
		if (GodotObject.IsInstanceValid(TutorialManager.Instance))
		{
			TutorialManager.Instance.OnTutorialFinish -= OnTutorialFinished;
			if (_tutorialActive && TutorialManager.Instance.currentTutoroal == _activeTutorialConfig)
			{
				TutorialManager.Instance.TutorialClear();
			}
		}
		_tutorialActive = false;
		_activeTutorialConfig = null;
		config = null;
	}
}
