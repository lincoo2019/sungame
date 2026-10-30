using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseProjectileEffectPumpkinCannonExplode : TowerDefenseProjectileEffectBase
{
	private static PackedScene pumpkinCannonExplosion;

	private static PackedScene PUMPKIN_CANNON_EXPLOSION => pumpkinCannonExplosion ?? (pumpkinCannonExplosion = GD.Load<PackedScene>("uid://q3mewkbjcbor"));

	public override async void _Ready()
	{
		if (GodotObject.IsInstanceValid(target))
		{
			EffectCreate();
			TowerDefenseExplode.CreateExplode(base.GlobalPosition, new Vector2(1f, 0.2f), base.Eventlist, new Array<TowerDefenseCharacter>(), camp, -1);
		}
		else
		{
			TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(gridPos);
			TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("PlantPumpkin");
			if (GodotObject.IsInstanceValid(mapCell))
			{
				if (mapCell.CanPacketPlant(packetConfig))
				{
					TowerDefenseCharacter towerDefenseCharacter = packetConfig.Plant(gridPos);
					if (camp == TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE)
					{
						towerDefenseCharacter.Hypnoses();
					}
				}
				else if (mapCell.HasPlant())
				{
					for (int i = 0; i < 5; i++)
					{
						if (camp == TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE)
						{
							TowerDefenseManager.Instance.BrainSunCreate(base.GlobalPosition, 25L, TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, 0.0, new Vector2((float)GD.RandRange(-50.0, 50.0), -400f));
						}
						else
						{
							TowerDefenseManager.Instance.SunCreate(base.GlobalPosition, 25L, TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, 0.0, new Vector2((float)GD.RandRange(-50.0, 50.0), -400f));
						}
					}
				}
				else
				{
					EffectCreate();
				}
			}
			else
			{
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
		TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(PUMPKIN_CANNON_EXPLOSION, gridPos);
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
