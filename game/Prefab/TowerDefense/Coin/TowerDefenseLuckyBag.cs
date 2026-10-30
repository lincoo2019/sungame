using Godot;

[GlobalClass]
public partial class TowerDefenseLuckyBag : TowerDefenseCoinBase
{
	public override void _Ready()
	{
		if (!Engine.IsEditorHint())
		{
			base._Ready();
			RemoveFromGroup("Coin");
		}
	}

	public override void Collection()
	{
		base.Collection();
	}
}
