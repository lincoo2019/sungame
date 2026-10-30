using Godot;

[Tool]
public partial class TowerDefensePlantCoffeebean : TowerDefensePlant
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
		if (!GodotObject.IsInstanceValid(cell))
		{
			return;
		}
		foreach (TowerDefenseCharacter character in cell.characterList)
		{
			if (character.camp == base.camp)
			{
				character.WakeUp();
			}
		}
	}
}
