using System;
using System.Reflection;
using System.Threading.Tasks;
using Godot;

public partial class VaseEntryAnimationVerticalStabilityRuntimeTest : Node
{
	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		_ = 1;
		try
		{
			await VerifyEntryTweenOwnsVerticalMotion();
			await VerifyNestedBackCompletesScaleRefresh();
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[VaseEntryVerticalStability] Unexpected exception: {value}");
		}
		bool flag = _failures == 0;
		GD.Print($"VASE_ENTRY_VERTICAL_STABILITY_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task VerifyEntryTweenOwnsVerticalMotion()
	{
		VaseEntryAnimationVerticalStabilityProbeCharacter character = new VaseEntryAnimationVerticalStabilityProbeCharacter
		{
			Name = "VaseEntryProbe",
			groundHeight = 12.0,
			gravityUse = true,
			ySpeed = 0.0
		};
		character.AddChild(character.transformPoint = new Marker2D
		{
			Name = "TransformPoint"
		}, forceReadableName: false, InternalMode.Disabled);
		AddChild(character, forceReadableName: false, InternalMode.Disabled);
		ComponentManager manager = new ComponentManager();
		EntryAnimationComponentDefinition definition = new EntryAnimationComponentDefinition
		{
			defaultFallHeight = 120f,
			defaultFallDelay = 0.2f,
			defaultFallDuration = 0.1f,
			impactDuration = 0.05f,
			settleDuration = 0.05f
		};
		EntryAnimationComponent runtime = new EntryAnimationComponent();
		runtime.Bind(manager, character, definition);
		runtime.Activate();
		runtime.PlayConfiguredFallBounce();
		Check(!character.gravityUse, "The entry tween must suppress base gravity while it owns z.");
		Check(!character.isGround, "The vase must remain marked airborne during the entry animation.");
		Check(Mathf.IsZeroApprox((float)character.ySpeed), "Entry must clear inherited vertical speed before the delayed tween.");
		double z = character.z;
		character.PhysiceUpdate(0.1f);
		Check(Mathf.IsEqualApprox((float)character.z, (float)z), "Base physics changed z during the random entry delay.");
		Check(Mathf.IsZeroApprox((float)character.ySpeed), "Base physics accumulated vertical speed during the random entry delay.");
		await ToSignal(GetTree().CreateTimer(0.6), SceneTreeTimer.SignalName.Timeout);
		Check(character.gravityUse, "The original gravity setting must be restored after entry completes.");
		Check(character.isGround, "Entry completion must restore the grounded state.");
		Check(Mathf.IsEqualApprox((float)character.z, (float)character.groundHeight), "Entry completion must settle exactly at the current ground height.");
		runtime.Release();
		character.QueueFree();
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
	}

	private async Task VerifyNestedBackCompletesScaleRefresh()
	{
		Node2D transformPoint = new Node2D
		{
			Name = "VaseTransformPoint"
		};
		PackedScene packedScene = GD.Load<PackedScene>("res://Asset/Anime/Character/Vase/Normal/VaseNormal.tscn");
		AdobeAnimateSprite front = packedScene.Instantiate<AdobeAnimateSprite>(PackedScene.GenEditState.Disabled);
		transformPoint.AddChild(front, forceReadableName: false, InternalMode.Disabled);
		AddChild(transformPoint, forceReadableName: false, InternalMode.Disabled);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		AdobeAnimateSprite back = front.GetNode<AdobeAnimateSprite>("Node2D/Back");
		ReadCachedRenderTransform(front);
		ReadCachedRenderTransform(back);
		transformPoint.Scale = new Vector2(1.5f, 0.5f);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		Check(ReadCachedRenderTransform(front).IsEqualApprox(front.GlobalTransform), "The front vase layer must refresh at the impact scale.");
		Check(ReadCachedRenderTransform(back).IsEqualApprox(back.GlobalTransform), "The nested Back layer must refresh at the impact scale.");
		transformPoint.Scale = Vector2.One;
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		Check(ReadCachedRenderTransform(front).IsEqualApprox(front.GlobalTransform), "The front vase layer must refresh at the settled scale.");
		Check(ReadCachedRenderTransform(back).IsEqualApprox(back.GlobalTransform), "The nested Back layer must finish refreshing at the settled scale.");
		transformPoint.QueueFree();
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
	}

	private static Transform2D ReadCachedRenderTransform(AdobeAnimateSprite sprite)
	{
		MethodInfo? method = typeof(AdobeAnimateSprite).GetMethod("GetCachedGlobalTransformForRender", BindingFlags.Instance | BindingFlags.NonPublic);
		if (method == null)
		{
			throw new MissingMethodException(typeof(AdobeAnimateSprite).FullName, "GetCachedGlobalTransformForRender");
		}
		return (Transform2D)method.Invoke(sprite, null);
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[VaseEntryVerticalStability] " + message);
		}
	}
}
