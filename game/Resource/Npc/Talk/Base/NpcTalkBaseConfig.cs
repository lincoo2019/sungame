using Godot;
using Godot.Collections;

[GlobalClass]
public partial class NpcTalkBaseConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public string npc = "";

	[Export(PropertyHint.None, "")]
	public string text = "";

	[Export(PropertyHint.None, "")]
	public string anime = "";

	[Export(PropertyHint.None, "")]
	public string audio = "";

	public virtual void Init(Dictionary data)
	{
		npc = data["Npc"].AsString();
		text = data["Text"].AsString();
		anime = data["Anime"].AsString();
		audio = data["Audio"].AsString();
	}
}
