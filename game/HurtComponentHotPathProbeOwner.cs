public partial class HurtComponentHotPathProbeOwner : TowerDefenseCharacter
{
	public override void _Ready()
	{
		SetNotifyTransform(enable: true);
		SetNotifyLocalTransform(enable: false);
	}
}
