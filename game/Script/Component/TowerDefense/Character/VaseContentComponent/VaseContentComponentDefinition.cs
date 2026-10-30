using Godot;

[GlobalClass]
[Tool]
public partial class VaseContentComponentDefinition : CharacterComponentDefinition
{
	[ExportGroup("Break Visual", "")]
	[Export(PropertyHint.None, "")]
	public bool playBreakAudio = true;

	[Export(PropertyHint.None, "")]
	public string breakAudio = "VaseBreaking";

	[Export(PropertyHint.None, "")]
	public Vector2 breakEffectOffset = new Vector2(0f, -30f);

	[ExportGroup("Packet Drop", "")]
	[Export(PropertyHint.None, "")]
	public Vector2 packetHorizontalSpeedRange = new Vector2(30f, 50f);

	[Export(PropertyHint.None, "")]
	public float packetVerticalSpeed = -300f;

	[Export(PropertyHint.None, "")]
	public float packetGravity = 980f;

	[Export(PropertyHint.Range, "0,120,0.1")]
	public float packetAliveTime = 15f;

	[Export(PropertyHint.Range, "1,30,1")]
	public int readyWaitFrameLimit = 2;

	public override CharacterComponentRuntime CreateRuntime()
	{
		return new VaseContentComponent();
	}
}
