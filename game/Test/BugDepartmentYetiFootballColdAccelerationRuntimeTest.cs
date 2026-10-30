using System;
using System.Threading.Tasks;
using Godot;

public partial class BugDepartmentYetiFootballColdAccelerationRuntimeTest : Node
{
	private const string YetiFootballScenePath = "res://Asset/Anime/Character/Zombie/Chapter5/YetiFootball/Scene/TowerDefenseZombieYetiFootball.tscn";

	private const string SnowPeaConfigPath = "res://Asset/Config/Projectile/Pea/SnowPea.tres";

	private const string IceSpearConfigPath = "res://Asset/Config/Projectile/IceSpear/IceSpearDefault.tres";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		YetiFootballColdAccelerationControlStub control = null;
		TowerDefenseZombieYetiFootball yeti = null;
		try
		{
			_ = 4;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager))
				{
					throw new InvalidOperationException("TowerDefenseManager is unavailable.");
				}
				control = new YetiFootballColdAccelerationControlStub
				{
					Name = "YetiFootballColdAccelerationControl",
					isGameRunning = true,
					isInit = false
				};
				AddChild(control, forceReadableName: false, InternalMode.Disabled);
				manager.currentControl = control;
				manager.gridBeginPos = Vector2.Zero;
				manager.gridSize = new Vector2(100f, 76f);
				manager.gridNum = new Vector2I(9, 5);
				PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter5/YetiFootball/Scene/TowerDefenseZombieYetiFootball.tscn", null, ResourceLoader.CacheMode.Ignore);
				Check(GodotObject.IsInstanceValid(packedScene), "The authored ZombieYetiFootball scene must load.");
				yeti = packedScene?.Instantiate<TowerDefenseZombieYetiFootball>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(yeti), "The fixture must instantiate the real ZombieYetiFootball object.");
				if (!GodotObject.IsInstanceValid(yeti))
				{
					throw new InvalidOperationException("ZombieYetiFootball did not instantiate.");
				}
				yeti.Position = new Vector2(600f, 228f);
				yeti.gridPos = new Vector2I(6, 2);
				yeti.inGame = true;
				AddChild(yeti, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(3);
				BugDepartmentYetiFootballColdAccelerationRuntimeTest bugDepartmentYetiFootballColdAccelerationRuntimeTest = this;
				BuffComponent buff = yeti.buff;
				bugDepartmentYetiFootballColdAccelerationRuntimeTest.Check(buff != null && !buff.IsReleased, "The real ZombieYetiFootball must expose its authored BuffComponent runtime.");
				Check(yeti.instance.unUseBuffFlags == 19, $"ZombieYetiFootball must retain its authored temperature-immunity flags; got {yeti.instance.unUseBuffFlags}.");
				Check(Math.Abs(yeti.timeScaleInit - 1.5) < 0.0001, $"The real ZombieYetiFootball must begin at authored 1.5 speed; got {yeti.timeScaleInit}.");
				yeti.WalkProcessing(0.016);
				double baselineWalkAnimationScale = yeti.sprite.timeScale;
				Check(baselineWalkAnimationScale > 0.0, $"The real ZombieYetiFootball must expose a positive baseline walk animation scale; got {baselineWalkAnimationScale}.");
				TowerDefenseProjectileConfig towerDefenseProjectileConfig = ResourceLoader.Load<TowerDefenseProjectileConfig>("res://Asset/Config/Projectile/Pea/SnowPea.tres", null, ResourceLoader.CacheMode.Ignore);
				Check(towerDefenseProjectileConfig != null && towerDefenseProjectileConfig.hitCharacterEventList.Count > 0 && towerDefenseProjectileConfig.hitCharacterEventList[0] is TowerDefenseCharacterEventAddBuff, "The authored SnowPea projectile must expose its add-Buff hit event.");
				towerDefenseProjectileConfig?.hitCharacterEventList[0].Execute(Vector2.Zero, yeti);
				await WaitFrames(2);
				Check(!yeti.buff.BuffHas("IceSpeedDown"), "ZombieYetiFootball must consume SnowPea's authored IceSpeedDown instead of retaining the generic half-speed modifier.");
				Check(Math.Abs(yeti.timeScaleInit - 3.0) < 0.0001, $"SnowPea's IceSpeedDown must switch ZombieYetiFootball to authored cold speed 3.0; got {yeti.timeScaleInit}.");
				Check(Math.Abs(yeti.timeScale - 3.0) < 0.0001, $"SnowPea's IceSpeedDown must produce an effective accelerated speed of 3.0; got {yeti.timeScale}.");
				yeti.WalkProcessing(0.016);
				Check(Math.Abs(yeti.sprite.timeScale - baselineWalkAnimationScale * 2.0) < 0.0001, $"SnowPea must double ZombieYetiFootball's real walk animation/root-motion scale; baseline={baselineWalkAnimationScale}, cold={yeti.sprite.timeScale}.");
				yeti.buff.BuffClear();
				yeti.timeScaleInit = 1.5;
				yeti.timeScale = 1.5;
				TowerDefenseProjectileConfig towerDefenseProjectileConfig2 = ResourceLoader.Load<TowerDefenseProjectileConfig>("res://Asset/Config/Projectile/IceSpear/IceSpearDefault.tres", null, ResourceLoader.CacheMode.Ignore);
				TowerDefenseCharacterEventConditionRandom towerDefenseCharacterEventConditionRandom = ((towerDefenseProjectileConfig2 != null && towerDefenseProjectileConfig2.hitCharacterEventList.Count > 1) ? (towerDefenseProjectileConfig2.hitCharacterEventList[1] as TowerDefenseCharacterEventConditionRandom) : null);
				Check(towerDefenseCharacterEventConditionRandom != null && towerDefenseCharacterEventConditionRandom.eventList.Count > 0 && towerDefenseCharacterEventConditionRandom.eventList[0] is TowerDefenseCharacterEventForzen, "The authored IceSpear projectile must expose its conditional Frozen hit event.");
				TowerDefenseCharacterEventConditionRandom.Run(Vector2.Zero, yeti, towerDefenseCharacterEventConditionRandom?.eventList, 1.0);
				await WaitFrames(2);
				Check(!yeti.buff.BuffHas("Frozen") && !yeti.buff.BuffHas("IceSpeedDown"), "ZombieYetiFootball must convert Frozen directly into acceleration without retaining either cold slowdown.");
				Check(Math.Abs(yeti.timeScaleInit - 3.0) < 0.0001, $"Frozen must switch ZombieYetiFootball to authored cold speed 3.0; got {yeti.timeScaleInit}.");
				Check(Math.Abs(yeti.timeScale - 3.0) < 0.0001, $"Frozen must not leave ZombieYetiFootball stopped; effective speed was {yeti.timeScale}.");
				yeti.WalkProcessing(0.016);
				Check(Math.Abs(yeti.sprite.timeScale - baselineWalkAnimationScale * 2.0) < 0.0001, $"Frozen must double ZombieYetiFootball's real walk animation/root-motion scale; baseline={baselineWalkAnimationScale}, cold={yeti.sprite.timeScale}.");
				yeti.buff.AddBuff(new TowerDefenseCharacterBuffRedHeat
				{
					time = 15.0
				});
				await WaitFrames(2);
				Check(!yeti.buff.BuffHas("RedHeat") && !yeti.buff.BuffHas("FireHit"), "ZombieYetiFootball must consume heat Buffs after applying its authored hot response.");
				Check(Math.Abs(yeti.timeScaleInit - 0.5) < 0.0001, $"RedHeat must retain the authored hot slowdown speed 0.5; got {yeti.timeScaleInit}.");
				yeti.buff.AddBuff(new TowerDefenseCharacterBuffIceSpeedDown
				{
					time = 15.0
				});
				await WaitFrames(2);
				Check(!yeti.buff.BuffHas("IceSpeedDown") && Math.Abs(yeti.timeScaleInit - 3.0) < 0.0001 && Math.Abs(yeti.timeScale - 3.0) < 0.0001, "A later cold hit must reliably replace the hot slowdown with the real accelerated state.");
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugDepartmentYetiFootballColdAccelerationRuntimeTest] Unexpected exception: {value}");
			}
		}
		finally
		{
			if (GodotObject.IsInstanceValid(yeti))
			{
				yeti.QueueFree();
			}
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
			}
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			await WaitFrames(3);
		}
		bool flag = _failures == 0 && _checks == 20;
		GD.Print($"YETI_FOOTBALL_COLD_ACCELERATION_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
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
			GD.PushError("[BugDepartmentYetiFootballColdAccelerationRuntimeTest] " + message);
		}
	}
}
