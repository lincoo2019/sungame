using Godot;

[GlobalClass]
public partial class CoinDropItemHandler : DropItemHandler
{
	public override void OnCollect(Vector2 pos, long value)
	{
		TowerDefenseManager.Instance.AddCoin(value);
	}
}
