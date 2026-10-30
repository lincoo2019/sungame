using Godot;

internal readonly struct AdobeAnimateManagedSlotSprite
{
	public AdobeAnimateSlot Slot { get; }

	public Sprite2D Sprite { get; }

	public AdobeAnimatePart AtlasPart { get; }

	public CanvasItem Visual
	{
		get
		{
			if (!GodotObject.IsInstanceValid(Sprite))
			{
				return AtlasPart;
			}
			return Sprite;
		}
	}

	public AdobeAnimateManagedSlotSprite(AdobeAnimateSlot slot, Sprite2D sprite)
	{
		Slot = slot;
		Sprite = sprite;
		AtlasPart = null;
	}

	public AdobeAnimateManagedSlotSprite(AdobeAnimateSlot slot, AdobeAnimatePart atlasPart)
	{
		Slot = slot;
		Sprite = null;
		AtlasPart = atlasPart;
	}
}
