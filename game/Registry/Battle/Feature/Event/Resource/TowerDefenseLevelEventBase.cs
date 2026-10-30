using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseLevelEventBase : Resource
{
	public virtual string _GetName()
	{
		return "";
	}

	public virtual void Execute()
	{
	}

	public virtual void Init(Dictionary valueDictionary)
	{
	}

	public virtual Dictionary Export()
	{
		return new Dictionary();
	}

	public virtual Dictionary GetProperty()
	{
		return new Dictionary();
	}
}
