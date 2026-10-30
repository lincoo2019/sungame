using Godot;
using Godot.Collections;

[GlobalClass]
public partial class ChangeProjectileConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public Array<ChangeProjectileSingleConfig> changeList;
}
