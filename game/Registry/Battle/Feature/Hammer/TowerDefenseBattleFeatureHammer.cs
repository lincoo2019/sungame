using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseBattleFeatureHammer : TowerDefenseBattleFeature
{
	private static PackedScene _towerDefenseHammerMode;

	public Node2D hammerModeNode;

	private static PackedScene TOWER_DEFENSE_HAMMER_MODE => _towerDefenseHammerMode ?? (_towerDefenseHammerMode = GD.Load<PackedScene>("uid://bg3jr7ygc5lkf"));

	public override void Init(Dictionary _data)
	{
		base.Init(_data);
		if (!GodotObject.IsInstanceValid(TOWER_DEFENSE_HAMMER_MODE) || !GodotObject.IsInstanceValid(control?.characterNode) || !GodotObject.IsInstanceValid(control.characterNode.GetParent()))
		{
			GD.PushError("[Hammer] Cannot create Hammer mode without its scene and character container.");
			return;
		}
		hammerModeNode = TOWER_DEFENSE_HAMMER_MODE.Instantiate<Node2D>(PackedScene.GenEditState.Disabled);
		control.characterNode.GetParent().AddChild(hammerModeNode, forceReadableName: false, Node.InternalMode.Disabled);
	}

	public override void GameFail()
	{
		Destroy();
	}

	public override void Destroy()
	{
		if (GodotObject.IsInstanceValid(hammerModeNode))
		{
			hammerModeNode.QueueFree();
		}
		hammerModeNode = null;
		base.Destroy();
	}

	public override Dictionary SaveFeature()
	{
		return new Dictionary { ["hammer"] = true };
	}

	public override void LoadFeature(Dictionary _data, TowerDefenseLevelSaveConfigCSharp _owner)
	{
	}
}
