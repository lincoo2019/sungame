using Godot;

[Tool]
public partial class TowerDefensePlantTabooBean : TowerDefensePlant
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
		if (!Engine.IsEditorHint())
		{
			ExplodeComponent explodeComponent = _explodeComponent;
			if (explodeComponent != null && !explodeComponent.IsReleased)
			{
				_explodeComponent.OnExplode -= Explode;
			}
		}
	}

	public virtual void Explode()
	{
		if (!GodotObject.IsInstanceValid(cell))
		{
			return;
		}
		foreach (TowerDefenseCharacter character in cell.characterList)
		{
			if (character is TowerDefensePlant && character.camp == base.camp)
			{
				character.WakeUp();
				character.BuffAdd(new TowerDefenseCharacterBuffTabooBean());
			}
		}
	}
}
