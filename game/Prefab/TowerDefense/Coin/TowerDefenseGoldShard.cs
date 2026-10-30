using Godot;

[GlobalClass]
public partial class TowerDefenseGoldShard : TowerDefenseCoinBase
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
