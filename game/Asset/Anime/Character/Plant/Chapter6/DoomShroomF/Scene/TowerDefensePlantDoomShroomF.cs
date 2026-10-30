using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantDoomShroomF : TowerDefensePlant
{
	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> eventList = new Array<TowerDefenseCharacterEventBase>();

	public bool over;

	public override void SleepEntered()
	{
		base.SleepEntered();
		instance.invincible = false;
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { { "over", over } };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		over = data.GetValueOrDefault("over", false).AsBool();
	}
}
