using Godot;
using Godot.Collections;

[GlobalClass]
public partial class NpcAnimationConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public string enterAnimation = "";

	[Export(PropertyHint.None, "")]
	public string idleAnimation = "";

	[Export(PropertyHint.None, "")]
	public string leaveAnimation = "";

	[Export(PropertyHint.None, "")]
	public Dictionary<string, string> aliases = new Dictionary<string, string>();

	[Export(PropertyHint.None, "")]
	public Dictionary<string, string> nextAnimations = new Dictionary<string, string>();

	[Export(PropertyHint.None, "")]
	public Dictionary<string, bool> loops = new Dictionary<string, bool>();

	[Export(PropertyHint.None, "")]
	public Dictionary<string, double> blendTimes = new Dictionary<string, double>();

	public string ResolveAnimation(string animation)
	{
		if (string.IsNullOrEmpty(animation))
		{
			return "";
		}
		if (!aliases.TryGetValue(animation, out var value))
		{
			return animation;
		}
		return value;
	}

	public bool IsLeaveAnimation(string animation)
	{
		string text = ResolveAnimation(leaveAnimation);
		if (!string.IsNullOrEmpty(text))
		{
			return ResolveAnimation(animation) == text;
		}
		return false;
	}

	public bool GetLoop(string clip)
	{
		bool value;
		return !loops.TryGetValue(clip, out value) || value;
	}

	public double GetBlendTime(string clip)
	{
		if (!blendTimes.TryGetValue(clip, out var value))
		{
			return 0.0;
		}
		return value;
	}
}
