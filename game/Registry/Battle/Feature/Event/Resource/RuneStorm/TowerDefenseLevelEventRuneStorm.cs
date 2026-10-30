using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseLevelEventRuneStorm : TowerDefenseLevelEventBase
{
	private const int ScreenEffectLayer = 10;

	private const double FormingTipDuration = 2.0;

	private const double StormTipDuration = 2.0;

	private const string FormingTipKey = "TOWERDEFENSE_TIPS_RUNESTORM_FORMING";

	private const string TimerStateKey = "rune_storm_state";

	private static PackedScene _runeStormEffectScene;

	[Export(PropertyHint.None, "")]
	public string type = "Random";

	[Export(PropertyHint.None, "")]
	public double durationMin = 10.0;

	[Export(PropertyHint.None, "")]
	public double durationMax = 15.0;

	[Export(PropertyHint.None, "")]
	public double formingDelay = 2.0;

	private static PackedScene RuneStormEffectScene => _runeStormEffectScene ?? (_runeStormEffectScene = GD.Load<PackedScene>("res://Prefab/ScreenEffect/RuneStorm/RuneStorm.tscn"));

	public override string _GetName()
	{
		return "LEVLE_EVENT_RUNE_STORM";
	}

	public override void Init(Dictionary valueDictionary)
	{
		type = valueDictionary.GetValueOrDefault("Type", "Random").AsString();
		durationMin = Mathf.Max(0.1, valueDictionary.GetValueOrDefault("DurationMin", 10.0).AsDouble());
		durationMax = Mathf.Max(durationMin, valueDictionary.GetValueOrDefault("DurationMax", 15.0).AsDouble());
		formingDelay = Mathf.Max(0.0, valueDictionary.GetValueOrDefault("FormingDelay", 2.0).AsDouble());
	}

	public override Dictionary Export()
	{
		return new Dictionary
		{
			["EventName"] = "RuneStorm",
			["Value"] = new Dictionary
			{
				["Type"] = type,
				["DurationMax"] = durationMax,
				["DurationMin"] = durationMin,
				["FormingDelay"] = formingDelay
			}
		};
	}

	public override Dictionary GetProperty()
	{
		Dictionary property = base.GetProperty();
		property["符文风暴"] = new Dictionary
		{
			["风暴类型"] = new Dictionary
			{
				["Object"] = this,
				["Type"] = "Enum",
				["Property"] = "type",
				["Hint"] = new Dictionary
				{
					["随机"] = "Random",
					["紫色"] = "Purple",
					["蓝色"] = "Blue",
					["红色"] = "Red",
					["橙色"] = "Orange",
					["绿色"] = "Green"
				},
				["Rest"] = "Random"
			},
			["随机下限"] = new Dictionary
			{
				["Object"] = this,
				["Type"] = "Float",
				["Property"] = "durationMin",
				["Rest"] = 10.0
			},
			["随机上限"] = new Dictionary
			{
				["Object"] = this,
				["Type"] = "Float",
				["Property"] = "durationMax",
				["Rest"] = 15.0
			},
			["形成延时"] = new Dictionary
			{
				["Object"] = this,
				["Type"] = "Float",
				["Property"] = "formingDelay",
				["Rest"] = 2.0
			}
		};
		return property;
	}

	public override void Execute()
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (GodotObject.IsInstanceValid(instance))
		{
			TowerDefenseControlNew currentControl = instance.currentControl;
			if (GodotObject.IsInstanceValid(currentControl) && currentControl.IsInsideTree())
			{
				TowerDefenseRuneMagicColor color = ResolveColor();
				double safeDuration = ResolveDuration();
				double num = Mathf.Max(0.0, formingDelay);
				ScheduleStorm(currentControl, color, safeDuration, num, Mathf.Min(2.0, num));
			}
		}
	}

	private double ResolveDuration()
	{
		double num = Mathf.Max(0.1, Mathf.Min(durationMin, durationMax));
		double to = Mathf.Max(num, Mathf.Max(durationMin, durationMax));
		return GD.RandRange(num, to);
	}

	private static void ScheduleStorm(TowerDefenseControlNew control, TowerDefenseRuneMagicColor color, double safeDuration, double safeDelay, double noticeRemaining)
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		if (noticeRemaining > 0.0)
		{
			RuneWeatherNotice.Show(control, "TOWERDEFENSE_TIPS_RUNESTORM_FORMING", noticeRemaining);
		}
		if (safeDelay <= 0.0)
		{
			BeginStorm();
			return;
		}
		Timer timer = new Timer
		{
			OneShot = true,
			WaitTime = safeDelay
		};
		timer.SetMeta("rune_storm_state", new Dictionary
		{
			["color"] = TowerDefenseRuneMagicUtil.ToKey(color),
			["duration"] = safeDuration,
			["delay"] = safeDelay,
			["noticeRemaining"] = noticeRemaining
		});
		control.AddChild(timer, forceReadableName: false, Node.InternalMode.Disabled);
		timer.Timeout += delegate
		{
			timer.QueueFree();
			BeginStorm();
		};
		timer.Start();
		void BeginStorm()
		{
			if (GodotObject.IsInstanceValid(control) && control.IsInsideTree() && !control.IsQueuedForDeletion() && manager.currentControl == control)
			{
				TowerDefenseBattleProcess process = control.process;
				if (process == null || process.IsLifetimeActive)
				{
					manager.TipsPlay(StormTipKey(color));
					TowerDefenseRuneMagicUtil.ApplyStormInstant(color, safeDuration);
					SpawnStormEffect(control, color, safeDuration);
				}
			}
		}
	}

	public static Dictionary SaveStorms(TowerDefenseControlNew control)
	{
		Array array = new Array();
		Array array2 = new Array();
		foreach (Node child in control.GetChildren())
		{
			if (child is Timer timer && !timer.IsQueuedForDeletion() && timer.HasMeta("rune_storm_state"))
			{
				Dictionary dictionary = timer.GetMeta("rune_storm_state").AsGodotDictionary().Duplicate(deep: true);
				double num = dictionary["delay"].AsDouble() - timer.TimeLeft;
				dictionary["noticeRemaining"] = Mathf.Max(0.0, dictionary["noticeRemaining"].AsDouble() - num);
				dictionary["delay"] = timer.TimeLeft;
				array.Add(dictionary);
			}
		}
		if (control.uilayerDictionary.TryGetValue(10, out var value))
		{
			foreach (Node child2 in value.GetChildren())
			{
				if (child2 is RuneStormEffect runeStormEffect && !runeStormEffect.IsQueuedForDeletion())
				{
					array2.Add(runeStormEffect.SaveState());
				}
			}
		}
		return new Dictionary
		{
			["forming"] = array,
			["active"] = array2
		};
	}

	public static void RestoreStorms(TowerDefenseControlNew control, Dictionary state)
	{
		foreach (Variant item in state.GetValueOrDefault("forming", new Array()).AsGodotArray())
		{
			Dictionary dictionary = item.AsGodotDictionary();
			if (TowerDefenseRuneMagicUtil.TryParse(dictionary.GetValueOrDefault("color", "").AsString(), out var color))
			{
				ScheduleStorm(control, color, Mathf.Max(0.1, dictionary["duration"].AsDouble()), Mathf.Max(0.0, dictionary["delay"].AsDouble()), Mathf.Max(0.0, dictionary["noticeRemaining"].AsDouble()));
			}
		}
		foreach (Variant item2 in state.GetValueOrDefault("active", new Array()).AsGodotArray())
		{
			Dictionary dictionary2 = item2.AsGodotDictionary();
			if (TowerDefenseRuneMagicUtil.TryParse(dictionary2.GetValueOrDefault("color", "").AsString(), out var color2))
			{
				RuneStormEffect runeStormEffect = RuneStormEffectScene.Instantiate<RuneStormEffect>(PackedScene.GenEditState.Disabled);
				runeStormEffect.LoadState(dictionary2);
				control.AddUI(runeStormEffect, 10);
				double num = 2.0 - dictionary2.GetValueOrDefault("elapsed", 0.0).AsDouble();
				if (num > 0.0)
				{
					TowerDefenseManager.Instance.TipsPlay(StormTipKey(color2), num);
				}
			}
		}
	}

	private TowerDefenseRuneMagicColor ResolveColor()
	{
		if (TowerDefenseRuneMagicUtil.TryParse(type, out var color))
		{
			return color;
		}
		return TowerDefenseRuneMagicUtil.RandomStormColor();
	}

	private static string StormTipKey(TowerDefenseRuneMagicColor color)
	{
		return color switch
		{
			TowerDefenseRuneMagicColor.Purple => "TOWERDEFENSE_TIPS_RUNESTORM_PURPLE", 
			TowerDefenseRuneMagicColor.Blue => "TOWERDEFENSE_TIPS_RUNESTORM_BLUE", 
			TowerDefenseRuneMagicColor.Red => "TOWERDEFENSE_TIPS_RUNESTORM_RED", 
			TowerDefenseRuneMagicColor.Orange => "TOWERDEFENSE_TIPS_RUNESTORM_ORANGE", 
			TowerDefenseRuneMagicColor.Green => "TOWERDEFENSE_TIPS_RUNESTORM_GREEN", 
			_ => "TOWERDEFENSE_TIPS_RUNESTORM_FORMING", 
		};
	}

	private static void SpawnStormEffect(TowerDefenseControlNew control, TowerDefenseRuneMagicColor color, double time)
	{
		if (GodotObject.IsInstanceValid(control) && GodotObject.IsInstanceValid(RuneStormEffectScene))
		{
			RuneStormEffect runeStormEffect = RuneStormEffectScene.Instantiate<RuneStormEffect>(PackedScene.GenEditState.Disabled);
			if (GodotObject.IsInstanceValid(runeStormEffect))
			{
				runeStormEffect.Setup(color, time);
				control.AddUI(runeStormEffect, 10);
			}
		}
	}
}
