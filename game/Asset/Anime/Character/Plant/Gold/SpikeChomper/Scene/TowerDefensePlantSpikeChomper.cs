using Godot;

[Tool]
public partial class TowerDefensePlantSpikeChomper : TowerDefensePlant
{
	private ChomperComponent _chomperComponent;

	private AttackComponent _attackComponent;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_chomperComponent = componentManager.GetRuntime<ChomperComponent>();
			_attackComponent = componentManager.GetRuntime<AttackComponent>("character.attack.0");
			_attackComponent.SetCheckAreaSegmentLengthX(0, TowerDefenseManager.Instance.GetMapGridSize().X * 1.75f);
		}
	}

	public override void AttackDeal(TowerDefenseCharacter character, string type, double num)
	{
		base.AttackDeal(character, type, num);
		if (GodotObject.IsInstanceValid(character))
		{
			character.Hurt(Mathf.Min(50.0, num / 2.0), playSplatAudio: false);
		}
	}
}
