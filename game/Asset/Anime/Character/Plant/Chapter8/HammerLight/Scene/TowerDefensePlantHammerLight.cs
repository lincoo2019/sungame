using Godot;

[Tool]
public partial class TowerDefensePlantHammerLight : TowerDefensePlant
{
	private ExplodeComponent _explodeComponent;

	public TowerDefenseCharacter crater;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_explodeComponent = componentManager.GetRuntime<ExplodeComponent>();
			_explodeComponent.OnExplode += Explode;
			zombiePlaceDamage = 500.0;
			CallDeferred("DeferredReady");
		}
	}

	private async void DeferredReady()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		if (GodotObject.IsInstanceValid(targetZombie))
		{
			_explodeComponent.explodeAnimeTimeScale = 2f;
			_explodeComponent.explodeAnimeClips = "WhackZombie";
		}
		else if (GodotObject.IsInstanceValid(cell))
		{
			crater = ResolveRepairableCrater();
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		ExplodeComponent explodeComponent = _explodeComponent;
		if (explodeComponent != null && !explodeComponent.IsReleased)
		{
			_explodeComponent.OnExplode -= Explode;
		}
	}

	public void Explode()
	{
		if (GodotObject.IsInstanceValid(targetZombie))
		{
			AudioManager.Instance.AudioPlay("Bonk");
			_explodeComponent.CreateParticlesEffect();
			targetZombie.Hurt(zombiePlaceDamage, playSplatAudio: true, Vector2.Zero, createDamagePart: false);
			targetZombie.AttackDeal(null, "Explode", zombiePlaceDamage);
		}
		else
		{
			TowerDefenseCrater towerDefenseCrater = ResolveRepairableCrater();
			if (GodotObject.IsInstanceValid(towerDefenseCrater))
			{
				towerDefenseCrater.Destroy();
			}
		}
	}

	private TowerDefenseCrater ResolveRepairableCrater()
	{
		if (crater is TowerDefenseCrater towerDefenseCrater && GodotObject.IsInstanceValid(towerDefenseCrater) && !towerDefenseCrater.isDestroy)
		{
			return towerDefenseCrater;
		}
		crater = null;
		if (!GodotObject.IsInstanceValid(cell))
		{
			return null;
		}
		TowerDefenseCrater repairableCrater = cell.GetRepairableCrater();
		if (GodotObject.IsInstanceValid(repairableCrater))
		{
			crater = repairableCrater;
		}
		return repairableCrater;
	}
}
