using Godot;

[Tool]
public partial class TowerDefensePlantSunFlagbean : TowerDefensePlant
{
	private ExplodeComponent _explodeComponent;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_explodeComponent = componentManager.GetRuntime<ExplodeComponent>();
			_explodeComponent.OnExplode += Explode;
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
		if (GodotObject.IsInstanceValid(cell))
		{
			foreach (TowerDefenseCharacter character in cell.GetCharacterList())
			{
				if (GodotObject.IsInstanceValid(character))
				{
					character.WakeUp();
				}
			}
		}
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		if (instance.hypnoses)
		{
			BrainSunCreate(logicalGlobalPosition, 200L);
		}
		else
		{
			SunCreate(logicalGlobalPosition, 200L);
		}
		if (GodotObject.IsInstanceValid(TowerDefenseBattleFeatureWave.Instance) && GodotObject.IsInstanceValid(TowerDefenseBattleFeatureWave.Instance.config) && !TowerDefenseBattleFeatureWave.Instance.waveFinal)
		{
			TowerDefenseBattleFeatureWave.Instance.NextWave();
		}
	}
}
