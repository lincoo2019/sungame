using Godot;

public sealed class AdobeAnimateRuntimeState
{
	public AdobeAnimateSprite Sprite;

	public AdobeAnimateRuntimeDefinition Definition;

	public string Clip = "";

	public Vector2I ClipRange;

	public int FrameIndex;

	public double ElapsedTimer;

	public double TimeScale = 1.0;

	public bool Pause;

	public bool PlayBack;

	public bool Loop = true;

	public bool Blend;

	public double BlendTime;

	public double BlendTimer;

	public PoseCache BlendFromPose = new PoseCache();

	public ulong LayerMask = 18446744073709551615uL;

	public ReplaceState Replace = new ReplaceState();

	public VerticalClipState VerticalClip = VerticalClipState.Disabled;

	public AdobeAnimateQuality Quality;
}
