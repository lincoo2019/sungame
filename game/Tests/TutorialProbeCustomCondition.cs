using Godot;

public partial class TutorialProbeCustomCondition : TutorialConditionConfig
{
	[Export(PropertyHint.None, "")]
	public int customValue = 7;

	[Export(PropertyHint.None, "")]
	public string playerText = "自定义条件提示";

	[Export(PropertyHint.None, "")]
	public Color accentColor = new Color(0.2f, 0.8f, 0.35f);
}
