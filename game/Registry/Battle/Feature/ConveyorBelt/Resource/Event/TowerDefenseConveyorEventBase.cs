using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseConveyorEventBase : Resource
{
	public virtual void Init(Dictionary data)
	{
	}

	public virtual void Execute()
	{
	}

	public virtual Dictionary Export()
	{
		return new Dictionary();
	}
}
