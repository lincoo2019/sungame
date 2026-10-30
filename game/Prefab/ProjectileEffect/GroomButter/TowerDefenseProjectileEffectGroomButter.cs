using System;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseProjectileEffectGroomButter : TowerDefenseProjectileEffectBase
{
	private static PackedScene gloomSplatsScene;

	public int num = 4;

	private Timer _timer;

	private RandomNumberGenerator _random;

	private TowerDefenseProjectileCreateData _kernelData;

	private TowerDefenseProjectileCreateData _butterData;

	private static PackedScene GLOOM_SPLATS_SCENE => gloomSplatsScene ?? (gloomSplatsScene = GD.Load<PackedScene>("uid://djchpnx8iwbpe"));

	public override string NetworkEffectId => "butter_gloom_burst";

	public override void _Ready()
	{
		if (PrepareNetworkEffect())
		{
			_random = new RandomNumberGenerator
			{
				Seed = base.NetworkRandomSeed
			};
			_kernelData = new TowerDefenseProjectileCreateData(new StringName("Kernal"))
			{
				baseDamage = 20.0
			};
			_butterData = new TowerDefenseProjectileCreateData(new StringName("Butter"))
			{
				baseDamage = 40.0,
				damageFlags = 2
			};
			_timer = GetNode<Timer>("Timer");
			_timer.Timeout += AttackCreate;
			AttackCreate();
		}
	}

	public override void _ExitTree()
	{
		if (GodotObject.IsInstanceValid(_timer))
		{
			_timer.Timeout -= AttackCreate;
		}
		_random?.Dispose();
		_kernelData?.Dispose();
		_butterData?.Dispose();
	}

	public void AttackCreate()
	{
		this.num--;
		for (int i = 0; i < 8; i++)
		{
			float num = Mathf.DegToRad(45f * (float)i);
			Vector2 vector = Vector2.FromAngle(num);
			TowerDefenseProjectileCreateData projectileData = ((_random.Randf() < 0.08f) ? _butterData : _kernelData);
			BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
			{
				checkAllOverride = true,
				initialRotationOverride = num + (float)Math.PI / 2f
			};
			FireComponent.CreateProjectilePositionByData(null, target, 0.0 - height, base.GlobalPosition + vector * 20f, vector * 300f, projectileData, collisionFlag, camp, default(Vector2), overrides);
		}
		AudioManager.Instance.AudioPlay("SplatNormal");
		Node2D characterNode = TowerDefenseManager.GetCharacterNode();
		TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = GLOOM_SPLATS_SCENE.Instantiate<TowerDefenseEffectParticlesOnce>(PackedScene.GenEditState.Disabled);
		towerDefenseEffectParticlesOnce.objectId = ObjectManagerConfig.OBJECT.NOONE;
		characterNode.AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, InternalMode.Disabled);
		towerDefenseEffectParticlesOnce.Refresh();
		towerDefenseEffectParticlesOnce.gridPos = gridPos;
		if (!GodotObject.IsInstanceValid(target))
		{
			towerDefenseEffectParticlesOnce.GlobalPosition = base.GlobalPosition - new Vector2(0f, (float)height);
			TowerDefenseExplode.CreateExplode(base.GlobalPosition - new Vector2(0f, (float)height), new Vector2(1.25f, 1.25f), base.Eventlist, new Array<TowerDefenseCharacter>(), camp, collisionFlag);
		}
		else
		{
			Vector2 pos = (towerDefenseEffectParticlesOnce.GlobalPosition = target.GetLogicalGlobalPosition(target.transformPoint) - new Vector2(0f, 50f));
			TowerDefenseExplode.CreateExplode(pos, new Vector2(1.25f, 1.25f), base.Eventlist, new Array<TowerDefenseCharacter>(), camp, collisionFlag);
		}
		if (this.num <= 0)
		{
			QueueFree();
		}
	}
}
