using System;
using Godot;

public partial class MowerHitVisualOwnershipRuntimeTest : Node
{
	private const string MowerHitScenePath = "res://Prefab/TowerDefense/Mower/MowerHit.tscn";

	private int _checks;

	private int _failures;

	public override void _Ready()
	{
		MowerHit mowerHit = null;
		MowerHitVisualTargetStub mowerHitVisualTargetStub = null;
		try
		{
			PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Prefab/TowerDefense/Mower/MowerHit.tscn", null, ResourceLoader.CacheMode.Ignore);
			Check(GodotObject.IsInstanceValid(packedScene), "The real mower-hit scene must load.");
			mowerHit = packedScene?.Instantiate<MowerHit>(PackedScene.GenEditState.Disabled);
			Check(GodotObject.IsInstanceValid(mowerHit), "The real mower-hit scene must instantiate.");
			if (GodotObject.IsInstanceValid(mowerHit))
			{
				AddChild(mowerHit, forceReadableName: false, InternalMode.Disabled);
				mowerHitVisualTargetStub = new MowerHitVisualTargetStub
				{
					Name = "MowerHitVisualTarget",
					Position = new Vector2(180f, 90f),
					Scale = new Vector2(1.25f, 0.8f)
				};
				AddChild(mowerHitVisualTargetStub, forceReadableName: false, InternalMode.Disabled);
				ulong instanceId = mowerHitVisualTargetStub.GetInstanceId();
				bool condition = mowerHit.Init(mowerHitVisualTargetStub);
				Check(condition, "The real mower-hit effect must accept a live target.");
				Check(mowerHitVisualTargetStub.GetInstanceId() == instanceId, "The squash effect must retain the original character instance.");
				Check(mowerHitVisualTargetStub.mowerDeathVisualOwned, "The target must publish mower visual ownership immediately.");
				Check(mowerHitVisualTargetStub.GetParent() is AdobeAnimateSlot, "The target must be mounted into the animation slot synchronously.");
				Check(mowerHitVisualTargetStub.Position.IsEqualApprox(Vector2.Zero), "The mounted target must be centered on the mower-hit slot.");
				AdobeAnimateSlot adobeAnimateSlot = mowerHitVisualTargetStub.GetParent() as AdobeAnimateSlot;
				Check(GodotObject.IsInstanceValid(adobeAnimateSlot) && adobeAnimateSlot.followSlotId == 1 && adobeAnimateSlot.useScale && adobeAnimateSlot.GetParent() is AdobeAnimateSprite, "The target must retain the authored scale-following squash slot.");
				Check(mowerHit.character == mowerHitVisualTargetStub, "The effect must retain exactly the original target reference.");
				Check(!mowerHitVisualTargetStub.IsQueuedForDeletion(), "The target must remain alive for the authored squash animation.");
			}
		}
		catch (Exception ex)
		{
			GD.PushError(ex.ToString());
			_failures++;
		}
		finally
		{
			GD.Print($"MOWER_HIT_VISUAL_OWNERSHIP_RESULT passed={_failures == 0} checks={_checks} failures={_failures}");
			GetTree().Quit((_failures != 0) ? 1 : 0);
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError(message);
		}
	}
}
