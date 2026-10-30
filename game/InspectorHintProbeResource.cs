using Godot;

[Tool]
public partial class InspectorHintProbeResource : Resource
{
	[Export(PropertyHint.EnumSuggestion, "pea,sun,wall")]
	public string SuggestedCard { get; set; } = "pea";

	[Export(PropertyHint.LocaleId, "")]
	public string Locale { get; set; } = "zh_CN";

	[Export(PropertyHint.ObjectId, "")]
	public long TargetObjectId { get; set; }
}
