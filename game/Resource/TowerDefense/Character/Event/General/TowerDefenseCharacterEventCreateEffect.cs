using Godot;

[GlobalClass]
public partial class TowerDefenseCharacterEventCreateEffect : TowerDefenseCharacterEventBase
{
	[Export(PropertyHint.None, "")]
	public PackedScene effectScene;

	public override void Execute(Vector2 pos, TowerDefenseCharacter target)
	{
		Run(target, effectScene);
	}

	public override void ExecuteDps(Vector2 pos, TowerDefenseCharacter target, double delta)
	{
		Run(target, effectScene);
	}

	public override void ExecuteProject(TowerDefenseProjectile projectile, TowerDefenseCharacter target)
	{
		Run(target, effectScene);
	}

	public static void Run(TowerDefenseCharacter target, PackedScene effectScene)
	{
		Node2D characterNode = TowerDefenseManager.GetCharacterNode();
		Node2D node2D = effectScene.Instantiate<Node2D>(PackedScene.GenEditState.Disabled);
		characterNode.AddChild(node2D, forceReadableName: false, Node.InternalMode.Disabled);
		node2D.Set("gridPos", new Vector2I(node2D.Get("gridPos").AsVector2I().X, target.gridPos.Y));
		node2D.GlobalPosition = target.GetLogicalGlobalPosition() - new Vector2(0f, 20f);
	}
}
