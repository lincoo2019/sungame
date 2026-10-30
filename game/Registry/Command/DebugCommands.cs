using Godot;
using Godot.Collections;

[GlobalClass]
public partial class DebugCommands : RefCounted
{
	public static void Register()
	{
		CommandRegistry.RegisterCommand("sun", "设置阳光数量", "/sun <数量>", Callable.From(delegate(long value)
		{
			TowerDefenseManager.Instance.SetSun(value);
		}), new Array
		{
			new CommandArg("value", 2, _required: true, default(Variant), "阳光数量")
		});
		CommandRegistry.RegisterCommand("coin", "设置金币数量", "/coin <数量>", Callable.From(delegate(long value)
		{
			TowerDefenseManager.Instance.coinBank.num = value;
		}), new Array
		{
			new CommandArg("value", 2, _required: true, default(Variant), "金币数量")
		});
		CommandRegistry.RegisterCommand("crystal", "设置水晶数量", "/crystal <数量>", Callable.From(delegate(int value)
		{
			GameSaveManager.Instance.SetKeyValue("CrystalNum", value);
			GameSaveManager.Instance.Save();
		}), new Array
		{
			new CommandArg("value", 2, _required: true, default(Variant), "水晶数量")
		});
		CommandRegistry.RegisterCommand("killall", "秒杀所有僵尸", "/killall", Callable.From(delegate
		{
			_CmdKillAll();
		}));
		CommandRegistry.RegisterCommand("win", "直接胜利", "/win", Callable.From(delegate
		{
			_CmdInstantWin();
		}));
		CommandRegistry.RegisterCommand("skipwave", "跳过等待下一波", "/skipwave", Callable.From(delegate
		{
			_CmdSkipWaveWait();
		}));
		CommandRegistry.RegisterCommand("skipfinal", "跳到最终波", "/skipfinal", Callable.From(delegate
		{
			_CmdSkipToFinalWave();
		}));
		CommandRegistry.RegisterCommand("skipto", "跳到指定波次", "/skipto <波次>", Callable.From(delegate(int wave)
		{
			_CmdSkipToWave(wave);
		}), new Array
		{
			new CommandArg("wave", 2, _required: true, default(Variant), "目标波次")
		});
		CommandRegistry.RegisterCommand("restoremower", "恢复所有割草机", "/restoremower", Callable.From(delegate
		{
			_CmdRestoreAllMowers();
		}));
		CommandRegistry.RegisterCommand("removemower", "移除所有割草机", "/removemower", Callable.From(delegate
		{
			_CmdRemoveAllMowers();
		}));
		CommandRegistry.RegisterCommand("resetbrain", "重置所有脑子", "/resetbrain", Callable.From(delegate
		{
			_CmdResetAllBrains();
		}));
		CommandRegistry.RegisterCommand("debug", "切换调试选项", "/debug <选项名> [on/off]", Callable.From(delegate(string option, string value)
		{
			_CmdDebug(option, value);
		}), new Array
		{
			new CommandArg("option", 4, _required: true, default(Variant), "选项名"),
			new CommandArg("value", 4, _required: false, "toggle", "on/off")
		});
		CommandRegistry.RegisterCommand("debuglist", "列出所有调试选项", "/debuglist", Callable.From(delegate
		{
			_CmdDebugList();
		}));
		CommandRegistry.RegisterCommand("phonk", "切换Phonk果冻抖动效果", "/phonk <on/off> [intensity]", Callable.From(delegate(string state, double intensity)
		{
			_CmdPhonk(state, intensity);
		}), new Array
		{
			new CommandArg("state", 4, _required: true, default(Variant), "on/off"),
			new CommandArg("intensity", 3, _required: false, 1.0, "强度倍率")
		});
		CommandRegistry.RegisterCommand("dismember", "切换僵尸肢解效果", "/dismember <on/off>", Callable.From(delegate(string state)
		{
			_CmdDismember(state);
		}), new Array
		{
			new CommandArg("state", 4, _required: true, default(Variant), "on/off")
		});
		CommandRegistry.RegisterCommand("shovel", "更换铲子", "/shovel <铲子名称/list>", Callable.From(delegate(string name)
		{
			_CmdShovel(name);
		}), new Array
		{
			new CommandArg("name", 4, _required: true, default(Variant), "铲子名称/list", Callable.From(() => ShovelCommand.GetShovelNames()))
		});
	}

	private static SceneTree _GetTree()
	{
		return (SceneTree)Engine.GetMainLoop();
	}

	private static void _CmdKillAll()
	{
		foreach (Node item in _GetTree().GetNodesInGroup("Zombie"))
		{
			if (GodotObject.IsInstanceValid(item) && item is TowerDefenseZombie towerDefenseZombie && !towerDefenseZombie.instance.die)
			{
				towerDefenseZombie.instance.SkipInvincibleDealHurt(towerDefenseZombie.instance.hitpoints, playSplatAudio: false, default(Vector2), createDamagePart: false);
			}
		}
		CommandConsole.Instance.PrintSuccess("已秒杀所有僵尸");
	}

	private static void _CmdInstantWin()
	{
		_CmdKillAll();
		TowerDefenseBattleFeatureWave instance = TowerDefenseBattleFeatureWave.Instance;
		if (instance == null)
		{
			CommandConsole.Instance.PrintError("当前不在战斗中");
			return;
		}
		instance.waveStart = true;
		instance.waveFinal = true;
		instance.EmitFinal();
		instance.awaitSpawn = false;
		CommandConsole.Instance.PrintSuccess("已直接胜利");
	}

	private static void _CmdSkipWaveWait()
	{
		TowerDefenseBattleFeatureWave instance = TowerDefenseBattleFeatureWave.Instance;
		if (instance == null)
		{
			CommandConsole.Instance.PrintError("当前不在战斗中");
			return;
		}
		instance.timer = instance.nextWaveTime;
		instance.awaitSpawn = false;
		CommandConsole.Instance.PrintSuccess("已跳过等待");
	}

	private static async void _CmdSkipToFinalWave()
	{
		TowerDefenseBattleFeatureWave wave = TowerDefenseBattleFeatureWave.Instance;
		if (wave == null)
		{
			CommandConsole.Instance.PrintError("当前不在战斗中");
			return;
		}
		if (!wave.waveStart)
		{
			wave.waveStart = true;
		}
		while (!wave.waveFinal)
		{
			wave.timer = wave.nextWaveTime;
			wave.awaitSpawn = false;
			SceneTreeTimer sceneTreeTimer = _GetTree().CreateTimer(0.2, processAlways: false);
			await sceneTreeTimer.ToSignal(sceneTreeTimer, SceneTreeTimer.SignalName.Timeout);
		}
		CommandConsole.Instance.PrintSuccess("已跳到最终波");
	}

	private static async void _CmdSkipToWave(int targetWave)
	{
		TowerDefenseBattleFeatureWave wave = TowerDefenseBattleFeatureWave.Instance;
		if (wave == null)
		{
			CommandConsole.Instance.PrintError("当前不在战斗中");
			return;
		}
		while (wave.currentWave < targetWave && !wave.waveFinal)
		{
			wave.timer = wave.nextWaveTime;
			wave.awaitSpawn = false;
			SceneTreeTimer sceneTreeTimer = _GetTree().CreateTimer(0.2, processAlways: false);
			await sceneTreeTimer.ToSignal(sceneTreeTimer, SceneTreeTimer.SignalName.Timeout);
		}
		CommandConsole.Instance.PrintSuccess($"已跳到第 {targetWave} 波");
	}

	private static void _CmdRestoreAllMowers()
	{
		if (!(TowerDefenseManager.CurrentControl.GetFeature("Mower") is TowerDefenseBattleFeatureMower towerDefenseBattleFeatureMower))
		{
			CommandConsole.Instance.PrintError("当前场景没有割草机");
			return;
		}
		TowerDefenseBattleFeatureMap mapFeature = TowerDefenseManager.GetMapFeature();
		if (mapFeature == null)
		{
			return;
		}
		for (int i = 1; i <= mapFeature.config.gridNum.Y; i++)
		{
			if (mapFeature.lineUse[i] && !GodotObject.IsInstanceValid(towerDefenseBattleFeatureMower.mowerLine[i]))
			{
				towerDefenseBattleFeatureMower.CreateMower(i);
			}
		}
		CommandConsole.Instance.PrintSuccess("已恢复所有割草机");
	}

	private static void _CmdRemoveAllMowers()
	{
		if (!(TowerDefenseManager.CurrentControl.GetFeature("Mower") is TowerDefenseBattleFeatureMower towerDefenseBattleFeatureMower))
		{
			CommandConsole.Instance.PrintError("当前场景没有割草机");
			return;
		}
		for (int i = 0; i < towerDefenseBattleFeatureMower.mowerLine.Count; i++)
		{
			Node node = towerDefenseBattleFeatureMower.mowerLine[i];
			if (GodotObject.IsInstanceValid(node))
			{
				node.QueueFree();
				towerDefenseBattleFeatureMower.mowerLine[i] = null;
			}
		}
		CommandConsole.Instance.PrintSuccess("已移除所有割草机");
	}

	private static void _CmdResetAllBrains()
	{
		if (!(TowerDefenseManager.CurrentControl.GetFeature("Brain") is TowerDefenseBattleFeatureBrain towerDefenseBattleFeatureBrain))
		{
			CommandConsole.Instance.PrintError("当前场景没有脑子");
			return;
		}
		TowerDefenseBattleFeatureMap mapFeature = TowerDefenseManager.GetMapFeature();
		if (mapFeature == null)
		{
			return;
		}
		for (int i = 0; i < towerDefenseBattleFeatureBrain.brainLine.Count; i++)
		{
			Node node = towerDefenseBattleFeatureBrain.brainLine[i];
			if (GodotObject.IsInstanceValid(node))
			{
				node.QueueFree();
				towerDefenseBattleFeatureBrain.brainLine[i] = null;
			}
		}
		for (int j = 1; j <= mapFeature.config.gridNum.Y; j++)
		{
			if (mapFeature.lineUse[j])
			{
				towerDefenseBattleFeatureBrain.CreateBrain(j);
			}
		}
		CommandConsole.Instance.PrintSuccess("已重置所有脑子");
	}

	private static void _CmdDebug(string option, string value = "toggle")
	{
		Dictionary dictionary = new Dictionary
		{
			["openalllevel"] = "debugOpenAllLevel",
			["coinmax"] = "debugCoinMax",
			["sunmax"] = "debugSunMax",
			["packetselect"] = "debugPacketSelect",
			["packetopenall"] = "debugPacketOpenAll",
			["packetcolddown"] = "debugPacketColdDown",
			["openallcustom"] = "debugOpenAllCustom",
			["openglove"] = "debugOpenGlove",
			["unlimitedfire"] = "debugUnlimitedFire",
			["plantinvincible"] = "debugPlantInvincible",
			["nolose"] = "debugNoLose",
			["wavepaused"] = "debugWavePaused",
			["nozombiespawn"] = "debugNoZombieSpawn",
			["braininvincible"] = "debugBrainInvincible"
		};
		string text = option.ToLower();
		if (!dictionary.ContainsKey(text))
		{
			CommandConsole.Instance.PrintError("未知调试选项: " + option + "  输入 /debuglist 查看所有选项");
			return;
		}
		string text2 = dictionary[text].AsString();
		switch (value.ToLower())
		{
		case "on":
		case "true":
		case "1":
			CommandManager.Instance.Set(text2, true);
			break;
		case "off":
		case "false":
		case "0":
			CommandManager.Instance.Set(text2, false);
			break;
		default:
		{
			bool flag = CommandManager.Instance.Get(text2).AsBool();
			CommandManager.Instance.Set(text2, !flag);
			break;
		}
		}
		bool flag2 = CommandManager.Instance.Get(text2).AsBool();
		CommandConsole.Instance.PrintSuccess(text + " -> " + (flag2 ? "开启" : "关闭"));
	}

	private static void _CmdDebugList()
	{
		Array obj = new Array
		{
			new Array { "openalllevel", "开启所有关卡" },
			new Array { "coinmax", "满金币" },
			new Array { "sunmax", "满阳光" },
			new Array { "packetselect", "任何模式启用选卡" },
			new Array { "packetopenall", "解锁所有卡牌" },
			new Array { "packetcolddown", "卡牌无冷却" },
			new Array { "openallcustom", "开启所有装扮" },
			new Array { "openglove", "启用手套" },
			new Array { "unlimitedfire", "无限火力" },
			new Array { "plantinvincible", "植物无敌" },
			new Array { "nolose", "禁用失败" },
			new Array { "wavepaused", "暂停波次" },
			new Array { "nozombiespawn", "禁止僵尸生成" },
			new Array { "braininvincible", "脑子无敌" }
		};
		Dictionary dictionary = new Dictionary
		{
			["openalllevel"] = "debugOpenAllLevel",
			["coinmax"] = "debugCoinMax",
			["sunmax"] = "debugSunMax",
			["packetselect"] = "debugPacketSelect",
			["packetopenall"] = "debugPacketOpenAll",
			["packetcolddown"] = "debugPacketColdDown",
			["openallcustom"] = "debugOpenAllCustom",
			["openglove"] = "debugOpenGlove",
			["unlimitedfire"] = "debugUnlimitedFire",
			["plantinvincible"] = "debugPlantInvincible",
			["nolose"] = "debugNoLose",
			["wavepaused"] = "debugWavePaused",
			["nozombiespawn"] = "debugNoZombieSpawn",
			["braininvincible"] = "debugBrainInvincible"
		};
		CommandConsole.Instance.PrintLine("[color=cyan]═══════ 调试选项列表 ═══════[/color]");
		foreach (Variant item in obj)
		{
			Array array = item.AsGodotArray();
			string text = array[0].AsString();
			string text2 = dictionary[text].AsString();
			Variant variant = CommandManager.Instance.Get(text2);
			string value = ((variant.VariantType != Variant.Type.Nil && variant.AsBool()) ? "[color=green]ON[/color]" : "[color=red]OFF[/color]");
			CommandConsole.Instance.PrintLine($"[color=green]/debug {text}[/color] {value} - {array[1].AsString()}");
		}
	}

	private static void _CmdPhonk(string state, double intensity = 1.0)
	{
		switch (state.ToLower())
		{
		case "on":
		case "true":
		case "1":
			PhonkComponent.phonkEnabled = true;
			PhonkComponent.InjectAll();
			break;
		case "off":
		case "false":
		case "0":
			PhonkComponent.phonkEnabled = false;
			PhonkComponent.RemoveAll();
			break;
		default:
			PhonkComponent.phonkEnabled = !PhonkComponent.phonkEnabled;
			if (PhonkComponent.phonkEnabled)
			{
				PhonkComponent.InjectAll();
			}
			else
			{
				PhonkComponent.RemoveAll();
			}
			break;
		}
		PhonkComponent.phonkIntensity = (float)intensity;
		string value = (PhonkComponent.phonkEnabled ? "[color=green]ON[/color]" : "[color=red]OFF[/color]");
		CommandConsole.Instance.PrintSuccess($"Phonk果冻抖动 {value}  强度: {PhonkComponent.phonkIntensity:F1}");
	}

	private static void _CmdDismember(string state)
	{
		switch (state.ToLower())
		{
		case "on":
		case "true":
		case "1":
			DismemberComponent.dismemberEnabled = true;
			DismemberComponent.InjectAll();
			break;
		case "off":
		case "false":
		case "0":
			DismemberComponent.dismemberEnabled = false;
			DismemberComponent.RemoveAll();
			break;
		default:
			DismemberComponent.dismemberEnabled = !DismemberComponent.dismemberEnabled;
			if (DismemberComponent.dismemberEnabled)
			{
				DismemberComponent.InjectAll();
			}
			else
			{
				DismemberComponent.RemoveAll();
			}
			break;
		}
		string text = (DismemberComponent.dismemberEnabled ? "[color=green]ON[/color]" : "[color=red]OFF[/color]");
		CommandConsole.Instance.PrintSuccess("僵尸肢解 " + text);
	}

	private static void _CmdShovel(string name)
	{
		if (name.ToLower() == "list")
		{
			ShovelCommand.ListShovels();
		}
		else
		{
			ShovelCommand.ChangeShovel(name);
		}
	}
}
