public partial class DamagePartHotPathProbeOwner : TowerDefenseCharacter
{
	public override void _Ready()
	{
		SetNotifyTransform(enable: true);
		SetNotifyLocalTransform(enable: false);
	}
}
