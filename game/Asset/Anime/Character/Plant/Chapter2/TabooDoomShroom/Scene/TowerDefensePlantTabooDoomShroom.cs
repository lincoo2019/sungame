using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantTabooDoomShroom : TowerDefensePlant
{
	private static readonly string TArOO_DOOM_SHROOM_CUSTOM_0_EXPLOSION = "uid://cs13qb2gi1km7";

	private ExplodeComponent explodeComponent;

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> eventList = new Array<TowerDefenseCharacterEventBase>();

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			explodeComponent = componentManager.GetRuntime<ExplodeComponent>();
			if (base.currentCustom.Contains("Custom0"))
			{
				explodeComponent.explodeEffect = GD.Load<PackedScene>(TArOO_DOOM_SHROOM_CUSTOM_0_EXPLOSION);
			}
		}
	}

	public override void OnCustomSwitched(string customKey)
	{
		if (customKey == "Custom0")
		{
			explodeComponent.explodeEffect = GD.Load<PackedScene>(TArOO_DOOM_SHROOM_CUSTOM_0_EXPLOSION);
		}
	}

	public override void SleepEntered()
	{
		base.SleepEntered();
		instance.invincible = false;
	}
}
