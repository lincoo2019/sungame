public partial class ProduceComponentHotPathProbeOwner : TowerDefenseCharacter
{
	public override void _Ready()
	{
		SetNotifyTransform(enable: true);
		SetNotifyLocalTransform(enable: false);
	}
}
