using Godot;

[GlobalClass]
public partial class UnlockConditionBaseConfig : Resource
{
	public virtual bool Check()
	{
		return false;
	}
}
