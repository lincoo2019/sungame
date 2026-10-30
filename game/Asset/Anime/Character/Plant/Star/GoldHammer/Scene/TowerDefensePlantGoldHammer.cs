using Godot;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantGoldHammer : TowerDefensePlant
{
	private ExplodeComponent _explodeComponent;

	public TowerDefenseCharacter crater;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !editorPreviewMode)
		{
			_explodeComponent = componentManager.GetRuntime<ExplodeComponent>();
			_explodeComponent.OnExplode += Explode;
			zombiePlaceDamage = 3000.0;
			CallDeferred("_DeferredReady");
		}
	}

	private async void _DeferredReady()
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
			return;
		}
		TowerDefenseCrater towerDefenseCrater = ResolveRepairableCrater();
		if (GodotObject.IsInstanceValid(towerDefenseCrater))
		{
			Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
			towerDefenseCrater.Destroy();
			if (!instance.hypnoses)
			{
				TowerDefenseCharacterEventGoldShardCreate towerDefenseCharacterEventGoldShardCreate = new TowerDefenseCharacterEventGoldShardCreate();
				towerDefenseCharacterEventGoldShardCreate.num = 1;
				towerDefenseCharacterEventGoldShardCreate.Execute(logicalGlobalPosition, this);
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
