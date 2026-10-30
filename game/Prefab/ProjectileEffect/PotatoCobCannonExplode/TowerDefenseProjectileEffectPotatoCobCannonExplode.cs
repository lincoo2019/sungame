using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseProjectileEffectPotatoCobCannonExplode : TowerDefenseProjectileEffectBase
{
	private static PackedScene mineExplosion;

	private static PackedScene MINE_EXPLOSION => mineExplosion ?? (mineExplosion = GD.Load<PackedScene>("uid://dqcu2ycvf4u72"));

	public override async void _Ready()
	{
		if (GodotObject.IsInstanceValid(target))
		{
			EffectCreate();
			TowerDefenseExplode.CreateExplode(base.GlobalPosition, new Vector2(1f, 0.2f), base.Eventlist, new Array<TowerDefenseCharacter>(), camp, -1);
			if (camp == TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE)
			{
				TowerDefenseManager.Instance.BrainSunCreate(base.GlobalPosition, 50L, TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, 0.0, new Vector2((float)GD.RandRange(-50.0, 50.0), -400f));
			}
			else
			{
				TowerDefenseManager.Instance.SunCreate(base.GlobalPosition, 50L, TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, 0.0, new Vector2((float)GD.RandRange(-50.0, 50.0), -400f));
			}
		}
		else
		{
			TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(gridPos);
			TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("PlantSunMine");
			if (GodotObject.IsInstanceValid(mapCell))
			{
				if (mapCell.CanPacketPlant(packetConfig))
				{
					TowerDefenseCharacter sunMine = packetConfig.Plant(gridPos);
					if (camp == TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE)
					{
						sunMine.Hypnoses();
					}
					await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
					if (sunMine is TowerDefensePlantSunMine towerDefensePlantSunMine)
					{
						towerDefensePlantSunMine.ReadyRise();
					}
					else if (sunMine is TowerDefensePlantMagnetMine towerDefensePlantMagnetMine)
					{
						towerDefensePlantMagnetMine.ReadyRise();
					}
				}
				else
				{
					if (camp == TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE)
					{
						TowerDefenseManager.Instance.BrainSunCreate(base.GlobalPosition, 50L, TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, 0.0, new Vector2((float)GD.RandRange(-50.0, 50.0), -400f));
					}
					else
					{
						TowerDefenseManager.Instance.SunCreate(base.GlobalPosition, 50L, TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, 0.0, new Vector2((float)GD.RandRange(-50.0, 50.0), -400f));
					}
					EffectCreate();
				}
			}
			else
			{
				if (camp == TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE)
				{
					TowerDefenseManager.Instance.BrainSunCreate(base.GlobalPosition, 50L, TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, 0.0, new Vector2((float)GD.RandRange(-50.0, 50.0), -400f));
				}
				else
				{
					TowerDefenseManager.Instance.SunCreate(base.GlobalPosition, 50L, TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, 0.0, new Vector2((float)GD.RandRange(-50.0, 50.0), -400f));
				}
				EffectCreate();
			}
		}
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		QueueFree();
	}

	public void EffectCreate()
	{
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(gridPos);
		ViewManager.Instance.CameraShake(new Vector2(GD.RandRange(-1, 1), GD.RandRange(-1, 1)), 3.0, 0.05, 4);
		TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(MINE_EXPLOSION, gridPos);
		Node2D characterNode = TowerDefenseManager.GetCharacterNode();
		towerDefenseEffectParticlesOnce.GlobalPosition = base.GlobalPosition;
		if (GodotObject.IsInstanceValid(mapCell))
		{
			towerDefenseEffectParticlesOnce.GlobalPosition = new Vector2(towerDefenseEffectParticlesOnce.GlobalPosition.X, towerDefenseEffectParticlesOnce.GlobalPosition.Y - (float)(mapCell.GetGroundHeight() - 30.0));
		}
		characterNode.AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, InternalMode.Disabled);
		AudioManager.Instance.AudioPlay("MineExplosion");
	}
}
