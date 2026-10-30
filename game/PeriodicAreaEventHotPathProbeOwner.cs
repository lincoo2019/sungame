public partial class PeriodicAreaEventHotPathProbeOwner : TowerDefenseCharacter
{
	public override void _Ready()
	{
		SetNotifyTransform(enable: true);
		SetNotifyLocalTransform(enable: false);
	}
}
