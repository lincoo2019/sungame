using Godot;
using Godot.Collections;

[GlobalClass]
public partial class WildMineCannonLandingEvent : TowerDefenseCharacterEventBase
{
	[Export(PropertyHint.None, "")]
	public string packetName = "PlantPotatoMine";

	[Export(PropertyHint.None, "")]
	public PackedScene explosionEffect;

	[Export(PropertyHint.None, "")]
	public string explosionAudio = "MineExplosion";

	[Export(PropertyHint.None, "")]
	public Vector2 explosionSize = new Vector2(1f, 0.25f);

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> explosionEvents = new Array<TowerDefenseCharacterEventBase>();

	public override void Execute(Vector2 pos, TowerDefenseCharacter target)
	{
		Run(target.gridPos, target.camp, packetName, explosionEffect, explosionAudio, explosionSize, explosionEvents);
	}

	public override void ExecuteDps(Vector2 pos, TowerDefenseCharacter target, double delta)
	{
		Run(target.gridPos, target.camp, packetName, explosionEffect, explosionAudio, explosionSize, explosionEvents);
	}

	public override void ExecuteProject(TowerDefenseProjectile projectile, TowerDefenseCharacter target)
	{
		Run(target.gridPos, projectile.camp, packetName, explosionEffect, explosionAudio, explosionSize, explosionEvents);
	}

	public override void ExecuteGroundProject(TowerDefenseProjectile projectile, Vector2 position, Vector2I gridPos)
	{
		Run(gridPos, projectile.camp, packetName, explosionEffect, explosionAudio, explosionSize, explosionEvents);
	}

	public static void Run(Vector2I gridPos, TowerDefenseEnum.CHARACTER_CAMP camp, string _packetName, PackedScene _explosionEffect, string _explosionAudio, Vector2 _explosionSize, Array<TowerDefenseCharacterEventBase> _explosionEvents)
	{
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return;
		}
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(gridPos);
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(_packetName);
		if (CanPlantOriginalMine(mapCell, camp, packetConfig))
		{
			TowerDefenseCharacter spawned = packetConfig.Plant(gridPos);
			if (GodotObject.IsInstanceValid(spawned))
			{
				Callable.From(delegate
				{
					if (GodotObject.IsInstanceValid(spawned) && !spawned.isDestroy)
					{
						if (spawned.camp != camp)
						{
							spawned.Hypnoses();
						}
						spawned.componentManager?.GetRuntime<PotatoComponent>()?.ReadyCharge();
					}
				}).CallDeferred();
				return;
			}
		}
		Explode(gridPos, camp, _explosionEffect, _explosionAudio, _explosionSize, _explosionEvents);
	}

	public static bool CanPlantOriginalMine(TowerDefenseCellInstance cell, TowerDefenseEnum.CHARACTER_CAMP camp, TowerDefensePacketConfig packetConfig)
	{
		if (!IsInsidePlantRegion(cell))
		{
			return false;
		}
		if (CellHasEnemyCharacter(cell, camp))
		{
			return false;
		}
		if (!GodotObject.IsInstanceValid(packetConfig))
		{
			return false;
		}
		return cell.CanPacketPlant(packetConfig);
	}

	public static bool IsInsidePlantRegion(TowerDefenseCellInstance cell)
	{
		if (!GodotObject.IsInstanceValid(cell))
		{
			return false;
		}
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			return false;
		}
		if (instance.CheckMapGridPosIn(cell.gridPos))
		{
			return GodotObject.IsInstanceValid(TowerDefenseManager.GetMapCell(cell.gridPos));
		}
		return false;
	}

	public static bool CellHasEnemyZombie(TowerDefenseCellInstance cell, TowerDefenseEnum.CHARACTER_CAMP camp)
	{
		TowerDefenseBattleCharacterRegistry towerDefenseBattleCharacterRegistry = TowerDefenseManager.Instance?.characterRegistry;
		if (towerDefenseBattleCharacterRegistry == null)
		{
			return false;
		}
		foreach (TowerDefenseCharacter charactersForGridWindow in towerDefenseBattleCharacterRegistry.GetCharactersForGridWindowList(cell.gridPos.X, cell.gridPos.Y, 0, 0))
		{
			if (charactersForGridWindow is TowerDefenseZombie towerDefenseZombie && towerDefenseZombie.camp != camp && !towerDefenseZombie.die && !towerDefenseZombie.isDestroy)
			{
				return true;
			}
		}
		return false;
	}

	public static bool CellHasEnemyCharacter(TowerDefenseCellInstance cell, TowerDefenseEnum.CHARACTER_CAMP camp)
	{
		TowerDefenseBattleCharacterRegistry towerDefenseBattleCharacterRegistry = TowerDefenseManager.Instance?.characterRegistry;
		if (towerDefenseBattleCharacterRegistry == null)
		{
			return false;
		}
		foreach (TowerDefenseCharacter charactersForGridWindow in towerDefenseBattleCharacterRegistry.GetCharactersForGridWindowList(cell.gridPos.X, cell.gridPos.Y, 0, 0))
		{
			if (IsEnemyCharacter(charactersForGridWindow, camp))
			{
				return true;
			}
		}
		return false;
	}

	public static bool IsEnemyCharacter(TowerDefenseCharacter character, TowerDefenseEnum.CHARACTER_CAMP camp)
	{
		if (!GodotObject.IsInstanceValid(character) || character.die || character.isDestroy)
		{
			return false;
		}
		return IsEnemyCamp(character.camp, camp);
	}

	private static bool IsEnemyCamp(TowerDefenseEnum.CHARACTER_CAMP characterCamp, TowerDefenseEnum.CHARACTER_CAMP camp)
	{
		if (characterCamp != TowerDefenseEnum.CHARACTER_CAMP.PLANT && characterCamp != TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE)
		{
			return false;
		}
		return characterCamp != camp;
	}

	private static void Explode(Vector2I gridPos, TowerDefenseEnum.CHARACTER_CAMP camp, PackedScene explosionEffect, string explosionAudio, Vector2 explosionSize, Array<TowerDefenseCharacterEventBase> explosionEvents)
	{
		Vector2 mapCellPlantPos = TowerDefenseManager.GetMapCellPlantPos(gridPos);
		if (explosionEvents != null && explosionEvents.Count > 0)
		{
			TowerDefenseExplode.CreateExplode(mapCellPlantPos, explosionSize, explosionEvents, new Array<TowerDefenseCharacter>(), camp, -1);
		}
		if (GodotObject.IsInstanceValid(explosionEffect))
		{
			Node2D characterNode = TowerDefenseManager.GetCharacterNode();
			TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(explosionEffect, gridPos);
			if (GodotObject.IsInstanceValid(towerDefenseEffectParticlesOnce) && GodotObject.IsInstanceValid(characterNode))
			{
				characterNode.AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, Node.InternalMode.Disabled);
				towerDefenseEffectParticlesOnce.GlobalPosition = mapCellPlantPos;
			}
		}
		if (!string.IsNullOrEmpty(explosionAudio))
		{
			AudioManager.Instance?.AudioPlay(explosionAudio);
		}
	}
}
