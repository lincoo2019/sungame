public partial class FireComponentHotPathProbeOwner : TowerDefenseCharacter
{
	public override void _Ready()
	{
		SetNotifyTransform(enable: true);
		SetNotifyLocalTransform(enable: false);
	}
}
