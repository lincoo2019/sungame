using Godot;

[GlobalClass]
public partial class CommandArg : Resource
{
	public string Name;

	public int Type = 4;

	public bool Required = true;

	public Variant DefaultValue;

	public string Description = "";

	public Callable Suggestions;

	public CommandArg()
	{
	}

	public CommandArg(string _name, int _type = 4, bool _required = true, Variant _defaultValue = default(Variant), string _description = "", Callable _suggestions = default(Callable))
	{
		Name = _name;
		Type = _type;
		Required = _required;
		DefaultValue = _defaultValue;
		Description = _description;
		Suggestions = _suggestions;
	}
}
