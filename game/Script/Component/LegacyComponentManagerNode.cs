using Godot;
using Godot.Collections;

[GlobalClass]
public partial class LegacyComponentManagerNode : Node2D
{
	[Export(PropertyHint.None, "")]
	public CharacterComponentSet ComponentSet { get; set; }

	public void MigrateComponentsTo(TowerDefenseCharacter owner)
	{
		if (!GodotObject.IsInstanceValid(owner))
		{
			return;
		}
		Array<Node> children = GetChildren();
		for (int i = 0; i < children.Count; i++)
		{
			if (children[i] is ComponentBase componentBase)
			{
				componentBase.Reparent(owner);
			}
		}
	}
}
