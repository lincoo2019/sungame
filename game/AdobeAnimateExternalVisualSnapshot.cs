using Godot;

internal readonly struct AdobeAnimateExternalVisualSnapshot(Sprite2D sprite, in AdobeAnimateExternalVisualDescriptor descriptor, bool visible, Texture2D texture, in Transform2D transform, in Transform2D registeredWorldTransform, in Color modulate, bool topologyDirty, bool stateDirty, long lastSuccessfulPublishedFrame)
{
	public Sprite2D Sprite { get; } = sprite;

	public AdobeAnimateExternalVisualDescriptor Descriptor { get; } = descriptor;

	public bool Visible { get; } = visible;

	public Texture2D Texture { get; } = texture;

	public Transform2D Transform { get; } = transform;

	public Transform2D RegisteredWorldTransform { get; } = registeredWorldTransform;

	public Color Modulate { get; } = modulate;

	public bool TopologyDirty { get; } = topologyDirty;

	public bool StateDirty { get; } = stateDirty;

	public long LastSuccessfulPublishedFrame { get; } = lastSuccessfulPublishedFrame;
}
