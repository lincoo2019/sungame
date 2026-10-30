using Godot;
using Godot.Collections;

[Tool]
public partial class ZombieRakerSprite : AdobeAnimateSpriteBase
{
	private AdobeAnimateSlot _mustacheSlot;

	private AdobeAnimatePart _mustache;

	private AdobeAnimateSlot _rakeBackSlot;

	private AdobeAnimatePart _rakeBack;

	public override void _Ready()
	{
		base._Ready();
		_mustacheSlot = GetNode<AdobeAnimateSlot>("MustacheSlot");
		_mustache = _mustacheSlot.GetNode<AdobeAnimatePart>("Mustache");
		_rakeBackSlot = GetNode<AdobeAnimateSlot>("RakeHeadBackSlot");
		_rakeBack = _rakeBackSlot.GetNode<AdobeAnimatePart>("RakeHeadBack");
		RefreshAttachments();
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		if (AdobeAnimateSprite.IsEditorContext)
		{
			RefreshAttachments();
		}
	}

	public override void BatchPhysicsUpdate(double delta)
	{
		base.BatchPhysicsUpdate(delta);
		RefreshAttachments();
	}

	public void RefreshAttachments()
	{
		RefreshAttachment(this, _mustacheSlot, _mustache);
		RefreshAttachment(this, _rakeBackSlot, _rakeBack);
	}

	internal static void RefreshAttachment(AdobeAnimateSprite owner, AdobeAnimateSlot slot, AdobeAnimatePart visual)
	{
		if (GodotObject.IsInstanceValid(slot) && GodotObject.IsInstanceValid(visual))
		{
			Array<bool> layerVisibleForInternalRead = owner.GetLayerVisibleForInternalRead();
			int num = slot.followSlotId - 1;
			bool flag = num >= 0 && num < layerVisibleForInternalRead.Count && layerVisibleForInternalRead[num];
			if (AdobeAnimateManagedSprite2D.GetLogicalVisible(visual) != flag)
			{
				AdobeAnimateManagedSprite2D.SetLogicalVisible(visual, flag);
				owner.MarkManagedSlotVisualStateChanged();
			}
		}
	}

	internal override void OnPartSnapshotCreated(AdobeAnimateSlot slot, Array<StringName> layers, AdobeAnimatePart part)
	{
		RefreshAttachments();
		if (layers.Contains("anim_head1"))
		{
			AppendAttachment(this, slot, part, _mustacheSlot, _mustache);
		}
	}

	internal static void AppendAttachment(AdobeAnimateSprite owner, AdobeAnimateSlot anchor, AdobeAnimatePart part, AdobeAnimateSlot sourceSlot, AdobeAnimatePart source)
	{
		if (GodotObject.IsInstanceValid(source) && AdobeAnimateManagedSprite2D.GetLogicalVisible(source) && owner.TryGetManagedSlotTransformForRender(sourceSlot, out var transform))
		{
			AdobeAnimatePart adobeAnimatePart = AdobeAnimatePart.CreateAtlasTexturePart(source.externalAtlasTexturePath, source.externalAtlasCentered);
			adobeAnimatePart.Name = source.Name;
			adobeAnimatePart.Transform = transform.Translated(-anchor.Position) * source.Transform;
			adobeAnimatePart.Modulate = sourceSlot.Modulate * source.Modulate;
			adobeAnimatePart.SelfModulate = source.SelfModulate;
			part.AddChild(adobeAnimatePart, forceReadableName: false, InternalMode.Disabled);
		}
	}
}
