using Godot;
using Godot.Collections;

[GlobalClass]
public partial class CommandConfig : Resource
{
	public string Name;

	public string Description;

	public string Usage;

	public Callable Callback;

	public Array<CommandArg> ArgsInfo = new Array<CommandArg>();
}
