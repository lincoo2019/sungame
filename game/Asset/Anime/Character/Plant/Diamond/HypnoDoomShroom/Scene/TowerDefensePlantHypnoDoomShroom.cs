using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantHypnoDoomShroom : TowerDefensePlant
{
	private static PackedScene _HYPNO_DOOM_SHROOM_EXPLOSION_CUSTOM0;

	private ExplodeComponent _explodeComponent;

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> eventList = new Array<TowerDefenseCharacterEventBase>();

	private static PackedScene HYPNO_DOOM_SHROOM_EXPLOSION_CUSTOM0 => _HYPNO_DOOM_SHROOM_EXPLOSION_CUSTOM0 ?? (_HYPNO_DOOM_SHROOM_EXPLOSION_CUSTOM0 = GD.Load<PackedScene>("uid://h4noqc3yscoo"));

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_explodeComponent = componentManager.GetRuntime<ExplodeComponent>();
			if (base.currentCustom.Contains("Custom0"))
			{
				_explodeComponent.explodeEffect = HYPNO_DOOM_SHROOM_EXPLOSION_CUSTOM0;
			}
		}
	}

	public override void OnCustomSwitched(string customKey)
	{
		if (customKey == "Custom0")
		{
			_explodeComponent.explodeEffect = HYPNO_DOOM_SHROOM_EXPLOSION_CUSTOM0;
		}
	}

	public override void SleepEntered()
	{
		base.SleepEntered();
		instance.invincible = false;
	}
}
