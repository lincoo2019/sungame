using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TutorialConditionConfig : Resource
{
	public virtual void Init(Dictionary data)
	{
	}

	public virtual void Enter()
	{
	}

	public virtual bool Step()
	{
		return true;
	}
}
