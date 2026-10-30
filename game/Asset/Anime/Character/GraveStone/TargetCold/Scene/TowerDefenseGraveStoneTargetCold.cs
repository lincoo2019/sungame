using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseGraveStoneTargetCold : TowerDefenseGravestone
{
	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> eventList = new Array<TowerDefenseCharacterEventBase>();

	public bool boom;

	public override async void DestroySet()
	{
		if (!boom)
		{
			boom = true;
			ViewManager.Instance.FullScreenColorBlink(new Color(0.117647f, 0.564706f, 1f, 0.5f), 0.1);
			Vector2 explosionPosition = GetLogicalGlobalPosition();
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			TowerDefenseExplode.CreateExplode(explosionPosition, new Vector2(1.5f, 1.5f), eventList, new Array<TowerDefenseCharacter>(), TowerDefenseEnum.CHARACTER_CAMP.NOONE, -1);
		}
	}
}
