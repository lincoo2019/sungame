using Godot;

[GlobalClass]
[Tool]
public partial class GarlicComponentDefinition : CharacterComponentDefinition
{
	private const string DefaultGrossoutTexturePath = "uid://bbidqxovk4j7y";

	private static Texture2D _defaultGrossoutTextureCache;

	private static Texture2D DefaultGrossoutTexture => _defaultGrossoutTextureCache ?? (_defaultGrossoutTextureCache = GD.Load<Texture2D>("uid://bbidqxovk4j7y"));

	[Export(PropertyHint.None, "")]
	public Texture2D grossoutTexture { get; set; } = DefaultGrossoutTexture;

	[Export(PropertyHint.Range, "0,5,0.01")]
	public float reactionDelay { get; set; } = 0.5f;

	[Export(PropertyHint.Range, "0,5,0.01")]
	public float grossoutDuration { get; set; } = 0.5f;

	[Export(PropertyHint.Range, "0.01,10,0.01")]
	public float changeLineDuration { get; set; } = 1f;

	[Export(PropertyHint.Range, "0,1,0.01")]
	public float moveDownChance { get; set; } = 0.5f;

	[Export(PropertyHint.None, "")]
	public string reactionAudio { get; set; } = "Yuck";

	[Export(PropertyHint.None, "")]
	public StringName garlicStateEvent { get; set; } = "ToGarlic";

	[Export(PropertyHint.None, "")]
	public Tween.EaseType changeLineEase { get; set; } = Tween.EaseType.InOut;

	[Export(PropertyHint.None, "")]
	public Tween.TransitionType changeLineTransition { get; set; }

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new GarlicComponent();
	}
}
