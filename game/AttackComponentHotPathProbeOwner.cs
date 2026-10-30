public partial class AttackComponentHotPathProbeOwner : TowerDefenseZombie
{
	public override void _Ready()
	{
		SetNotifyTransform(enable: true);
		SetNotifyLocalTransform(enable: false);
	}
}
