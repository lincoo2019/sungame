using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantIceShroomGene : TowerDefensePlant
{
	public bool over;

	private ExplodeComponent _explodeComponent;

	private static PackedScene _GENETIC_EFFECT;

	private static PackedScene GENETIC_EFFECT => _GENETIC_EFFECT ?? (_GENETIC_EFFECT = GD.Load<PackedScene>("uid://k8ms8gteeqp6"));

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_explodeComponent = componentManager.GetRuntime<ExplodeComponent>();
			ExplodeComponent explodeComponent = _explodeComponent;
			if (explodeComponent != null && !explodeComponent.IsReleased)
			{
				_explodeComponent.OnExplode += OnExplode;
			}
		}
	}

	public override void _ExitTree()
	{
		ExplodeComponent explodeComponent = _explodeComponent;
		if (explodeComponent != null && !explodeComponent.IsReleased)
		{
			_explodeComponent.OnExplode -= OnExplode;
		}
		base._ExitTree();
	}

	public override void SleepEntered()
	{
		base.SleepEntered();
		instance.invincible = false;
	}

	private void OnExplode()
	{
		if (!over)
		{
			over = true;
			GeneMutateAndCold();
		}
	}

	private void GeneMutateAndCold()
	{
		base.gridPos = TowerDefenseManager.Instance.GetMapGridPos(GetLogicalGlobalPosition());
		SpawnGeneticEffect(base.gridPos);
		foreach (Variant item in TowerDefenseManager.Instance.GetCampTarget(base.camp))
		{
			if (item.As<TowerDefenseCharacter>() is TowerDefenseZombie towerDefenseZombie && CanGeneTransform(towerDefenseZombie))
			{
				towerDefenseZombie.TransformTo("ZombieNormal", ApplyColdToNormal);
			}
		}
		TowerDefenseCharacter.CreateColdEffect(base.camp, base.gridPos);
	}

	private static bool CanGeneTransform(TowerDefenseZombie zombie)
	{
		if (zombie == null || !GodotObject.IsInstanceValid(zombie))
		{
			return false;
		}
		if (zombie.instance.zombiePhysique == TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS)
		{
			return false;
		}
		if (zombie.config != null && zombie.config.name == "ZombieNormal" && zombie.GetArmor().Count == 0)
		{
			return false;
		}
		return true;
	}

	private static void ApplyColdToNormal(TowerDefenseCharacter normal)
	{
		if (!GodotObject.IsInstanceValid(normal))
		{
			return;
		}
		foreach (TowerDefenseCharacterEventBase item in TowerDefenseCharacter.CreateSnowEventList(new Array<TowerDefenseCharacterEventBase>()))
		{
			item.Execute(normal.GetLogicalGlobalPosition(), normal);
		}
	}

	private void SpawnGeneticEffect(Vector2I gridPos)
	{
		if (!GodotObject.IsInstanceValid(GENETIC_EFFECT))
		{
			return;
		}
		Node2D node2D = TowerDefenseManager.GetCharacterNode();
		if (GodotObject.IsInstanceValid(node2D))
		{
			TowerDefenseEffectSpriteOnce towerDefenseEffectSpriteOnce = TowerDefenseManager.CreateEffectSpriteOnce(GENETIC_EFFECT, gridPos, "Idle");
			if (GodotObject.IsInstanceValid(towerDefenseEffectSpriteOnce))
			{
				node2D.AddChild(towerDefenseEffectSpriteOnce, forceReadableName: false, InternalMode.Disabled);
				towerDefenseEffectSpriteOnce.GlobalPosition = TowerDefenseManager.GetMapCellPlantPos(gridPos);
			}
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { ["over"] = over };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		over = data.ContainsKey("over") && data["over"].AsBool();
	}
}
