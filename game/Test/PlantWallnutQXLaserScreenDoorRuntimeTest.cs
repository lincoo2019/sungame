using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class PlantWallnutQXLaserScreenDoorRuntimeTest : Node
{
	private const string WallnutScenePath = "res://Asset/Anime/Character/Plant/Chapter9/WallnutQX/Scene/TowerDefensePlantWallnutQX.tscn";

	private const string NormalZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private const string ScreenDoorPacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Plant/ZombieNormalScreendoor.tres";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		PlantWallnutQXLaserScreenDoorControlStub control = null;
		TowerDefensePlantWallnutQX wallnut = null;
		TowerDefenseZombieNormal screenDoorZombie = null;
		try
		{
			_ = 1;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "测试必须能访问战斗管理器自动加载。");
				if (!GodotObject.IsInstanceValid(manager))
				{
					throw new InvalidOperationException("运行回归所需的战斗管理器不可用。");
				}
				control = new PlantWallnutQXLaserScreenDoorControlStub
				{
					Name = "PlantWallnutQXLaserScreenDoorControl",
					isGameRunning = false,
					isInit = true
				};
				AddChild(control, forceReadableName: false, InternalMode.Disabled);
				control.characterNode = new Node2D
				{
					Name = "CharacterNode"
				};
				control.AddChild(control.characterNode, forceReadableName: false, InternalMode.Disabled);
				manager.currentControl = control;
				wallnut = LoadCharacter<TowerDefensePlantWallnutQX>("res://Asset/Anime/Character/Plant/Chapter9/WallnutQX/Scene/TowerDefensePlantWallnutQX.tscn");
				screenDoorZombie = LoadCharacter<TowerDefenseZombieNormal>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn");
				PrepareCharacter(wallnut);
				PrepareCharacter(screenDoorZombie);
				control.characterNode.AddChild(wallnut, forceReadableName: false, InternalMode.Disabled);
				control.characterNode.AddChild(screenDoorZombie, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(6);
				Check(GodotObject.IsInstanceValid(wallnut) && wallnut.config?.name == "PlantWallnutQX", "测试必须加载真实的全息激光坚果角色场景。");
				Check(GodotObject.IsInstanceValid(screenDoorZombie) && screenDoorZombie.config?.name == "ZombieNormal", "测试必须加载真实的普通僵尸角色场景。");
				Check(GodotObject.IsInstanceValid(wallnut?.instance) && GodotObject.IsInstanceValid(screenDoorZombie?.instance), "两个真实角色必须完成运行时实例初始化。");
				if (!GodotObject.IsInstanceValid(wallnut?.instance) || !GodotObject.IsInstanceValid(screenDoorZombie?.instance))
				{
					throw new InvalidOperationException("真实角色运行时实例未完成初始化。");
				}
				TowerDefensePacketConfig towerDefensePacketConfig = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Plant/ZombieNormalScreendoor.tres", null, ResourceLoader.CacheMode.Ignore);
				Check(towerDefensePacketConfig != null && towerDefensePacketConfig.initArmor?.Count == 1 && towerDefensePacketConfig.initArmor[0] == "Screendoor", "真实铁栅门卡片必须声明唯一的 Screendoor 初始护具。");
				if (towerDefensePacketConfig == null || towerDefensePacketConfig.initArmor?.Count != 1)
				{
					throw new InvalidOperationException("铁栅门卡片初始护具配置无效。");
				}
				TowerDefenseArmorRegistry.Init();
				screenDoorZombie.instance.ArmorAdd(towerDefensePacketConfig.initArmor[0]);
				await WaitFrames(2);
				AttackComponent runtime = wallnut.componentManager.GetRuntime<AttackComponent>("character.attack.0");
				PlantWallnutQXLaserScreenDoorRuntimeTest plantWallnutQXLaserScreenDoorRuntimeTest = this;
				int condition;
				if (runtime != null && !runtime.IsReleased)
				{
					Array<TowerDefenseCharacterEventBase> eventList = runtime.eventList;
					condition = ((eventList != null && eventList.Count == 1) ? 1 : 0);
				}
				else
				{
					condition = 0;
				}
				plantWallnutQXLaserScreenDoorRuntimeTest.Check((byte)condition != 0, "全息激光坚果必须绑定唯一的真实攻击伤害事件。");
				if (runtime == null || runtime.IsReleased)
				{
					throw new InvalidOperationException("全息激光坚果攻击组件未完成绑定。");
				}
				TowerDefenseCharacterEventHurtWithConfig towerDefenseCharacterEventHurtWithConfig = runtime.eventList[0] as TowerDefenseCharacterEventHurtWithConfig;
				int num = 2;
				Check(towerDefenseCharacterEventHurtWithConfig?.AttackConfig != null && towerDefenseCharacterEventHurtWithConfig.AttackConfig.damageFlags == num, "非实体激光必须只命中本体层，不能被铁栅门的 SHIELD 层吸收。");
				TowerDefenseArmorInstance towerDefenseArmorInstance = FindArmor(screenDoorZombie, "Screendoor");
				Check(towerDefenseArmorInstance != null && HasFlag(towerDefenseArmorInstance.armorMethodFlags, TowerDefenseEnum.ARMOR_METHOD_FLAGS.SHIELD), "真实铁栅门必须进入 SHIELD 二类护具层。");
				if (towerDefenseArmorInstance == null || towerDefenseCharacterEventHurtWithConfig?.AttackConfig == null)
				{
					throw new InvalidOperationException("真实铁栅门护具或激光伤害配置未生成。");
				}
				double hitpoints = screenDoorZombie.instance.hitpoints;
				double hitPoints = towerDefenseArmorInstance.hitPoints;
				towerDefenseCharacterEventHurtWithConfig.Execute(wallnut.GlobalPosition, screenDoorZombie);
				Check(Math.Abs(screenDoorZombie.instance.hitpoints - (hitpoints - 40.0)) < 0.001, "激光必须绕过铁栅门并对僵尸本体造成 40 点伤害。");
				Check(Math.Abs(towerDefenseArmorInstance.hitPoints - hitPoints) < 0.001, "非实体激光穿透时不得消耗铁栅门耐久。");
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[PlantWallnutQXLaserScreenDoorRuntimeTest] 未预期异常：{value}");
			}
		}
		finally
		{
			if (GodotObject.IsInstanceValid(wallnut) && !wallnut.IsQueuedForDeletion())
			{
				wallnut.QueueFree();
			}
			if (GodotObject.IsInstanceValid(screenDoorZombie) && !screenDoorZombie.IsQueuedForDeletion())
			{
				screenDoorZombie.QueueFree();
			}
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
			}
			if (GodotObject.IsInstanceValid(control) && !control.IsQueuedForDeletion())
			{
				control.QueueFree();
			}
			await WaitFrames(8);
			if (GodotObject.IsInstanceValid(ObjectManager.Instance))
			{
				ObjectManager.Instance.Clear();
			}
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			AdobeAnimateDefinitionCache.Clear();
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
		bool flag = _failures == 0 && _checks == 10;
		GD.Print($"PLANT_WALLNUT_QX_LASER_SCREEN_DOOR_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static T LoadCharacter<T>(string path) where T : TowerDefenseCharacter
	{
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Ignore);
		if (packedScene == null)
		{
			return null;
		}
		return packedScene.Instantiate<T>(PackedScene.GenEditState.Disabled);
	}

	private static void PrepareCharacter(TowerDefenseCharacter character)
	{
		if (!GodotObject.IsInstanceValid(character))
		{
			throw new InvalidOperationException("角色场景实例化失败。");
		}
		character.editorPreviewMode = true;
		character.inGame = false;
	}

	private static TowerDefenseArmorInstance FindArmor(TowerDefenseCharacter character, string armorName)
	{
		foreach (TowerDefenseArmorInstance armor in character.instance.armorList)
		{
			if (armor?.slotConfig?.armorName == armorName && !armor.isRemove)
			{
				return armor;
			}
		}
		return null;
	}

	private static bool HasFlag(int flags, TowerDefenseEnum.ARMOR_METHOD_FLAGS flag)
	{
		return ((uint)flags & (uint)flag) != 0;
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[PlantWallnutQXLaserScreenDoorRuntimeTest] " + message);
		}
	}
}
