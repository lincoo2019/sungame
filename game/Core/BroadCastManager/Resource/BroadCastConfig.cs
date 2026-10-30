using Godot;

[GlobalClass]
public partial class BroadCastConfig : Resource
{
	[Export(PropertyHint.MultilineText, "")]
	public string broadCastString = "";

	[Export(PropertyHint.None, "")]
	public double broadCastTime = -1.0;
}
