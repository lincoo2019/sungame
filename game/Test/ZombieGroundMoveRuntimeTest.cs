using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;

public partial class ZombieGroundMoveRuntimeTest : Node
{
	private const string ZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private readonly List<string> _failures = new List<string>();

	private bool _previousUseBatch;

	public override async void _Ready()
	{
		_previousUseBatch = TowerDefenseZombie.UseBatch;
		TowerDefenseZombie.UseBatch = true;
		TowerDefenseZombie zombie = null;
		TowerDefenseZombie rearZombie = null;
		try
		{
			PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn", null, ResourceLoader.CacheMode.Ignore);
			if (packedScene == null)
			{
				_failures.Add("normal zombie scene failed to load");
				Finish();
				return;
			}
			zombie = packedScene.Instantiate<TowerDefenseZombie>(PackedScene.GenEditState.Disabled);
			zombie.inGame = true;
			zombie.editorPreviewMode = false;
			zombie.GlobalPosition = new Vector2(1000f, 100f);
			AddChild(zombie, forceReadableName: false, InternalMode.Disabled);
			rearZombie = packedScene.Instantiate<TowerDefenseZombie>(PackedScene.GenEditState.Disabled);
			rearZombie.inGame = true;
			rearZombie.editorPreviewMode = false;
			rearZombie.GlobalPosition = new Vector2(1060f, 100f);
			AddChild(rearZombie, forceReadableName: false, InternalMode.Disabled);
			await WaitPhysicsFrames(3);
			GroundMoveComponent groundMove = zombie.groundMoveComponent;
			Check(zombie.HasValidRuntimeConfiguration, "normal zombie runtime configuration is invalid");
			Check(zombie.StateMachine?.IsInitialized ?? false, "normal zombie main state machine is not initialized");
			Check(groundMove != null, "normal zombie has no GroundMove runtime");
			if (groundMove == null)
			{
				Finish();
				return;
			}
			Check(groundMove.Lifecycle == ComponentRuntimeLifecycle.Active, $"GroundMove lifecycle is {groundMove.Lifecycle}, expected Active");
			Check(groundMove.HasMovementSource, "GroundMove did not resolve an animation movement source");
			Check(groundMove.UsesGroundLayerSource, "Built-in GroundMove did not select the direct animation-layer source");
			Check(groundMove.GroundLayerId == 0, $"Built-in GroundMove resolved layer {groundMove.GroundLayerId}, expected 0");
			Check(!GodotObject.IsInstanceValid(groundMove.groundNode), "Built-in GroundMove unexpectedly resolved the legacy GroundSlot Node");
			Check(!groundMove.moveYAxis, "Normal zombie GroundMove must filter authored Y movement by default");
			Check(zombie.IsOwnerBatchRegistered, "normal zombie was not registered with the zombie physics batch");
			zombie.walkAnimeClip = "Walk1";
			rearZombie.walkAnimeClip = "Walk2";
			zombie.Walk();
			rearZombie.Walk();
			await WaitPhysicsFrames(18);
			Check(string.Equals(zombie.CurrentStateHandle?.StableId, "zombie.walk", StringComparison.Ordinal), "zombie did not enter walk state: " + (zombie.CurrentStateHandle?.StableId ?? "<null>"));
			Check(groundMove.Alive, "GroundMove stayed inactive after entering walk state");
			ZombieGroundMoveRuntimeTest zombieGroundMoveRuntimeTest = this;
			GroundMoveComponent groundMoveComponent = rearZombie.groundMoveComponent;
			zombieGroundMoveRuntimeTest.Check(groundMoveComponent != null && groundMoveComponent.Alive && groundMoveComponent.UsesGroundLayerSource, "untargeted rear normal zombie did not activate its own GroundMove source");
			Transform2D cachedTransformBeforeMove = ReadCachedRenderTransform(zombie.sprite);
			Vector2 startPosition = zombie.GlobalPosition;
			await WaitPhysicsFrames(30);
			float num = Mathf.Abs(zombie.GlobalPosition.X - startPosition.X);
			float num2 = Mathf.Abs(zombie.GlobalPosition.Y - startPosition.Y);
			Transform2D transform2D = ReadCachedRenderTransform(zombie.sprite);
			Check(num > 0.01f, $"zombie did not move while walking; deltaX={num:F4}, source={groundMove.HasMovementSource}, alive={groundMove.Alive}");
			Check(num2 <= 0.01f, $"default GroundMove changed the zombie Y position; deltaY={num2:F4}");
			Check(transform2D.IsEqualApprox(zombie.sprite.GlobalTransform), "GroundMove's cached Adobe render transform diverged from the native scene transform");
			Check(Mathf.Abs(transform2D.Origin.X - cachedTransformBeforeMove.Origin.X - (zombie.GlobalPosition.X - startPosition.X)) <= 0.01f, "GroundMove's cached Adobe render translation did not match character movement");
			VerifyGroundEntrySpeedBoundary(zombie, "front Walk1 zombie");
			VerifyGroundEntrySpeedBoundary(rearZombie, "untargeted rear Walk2 zombie");
			groundMove.groundLayerName = null;
			Check(groundMove.ResolveMovementSource(), "Legacy Mod GroundMove failed to resolve its GroundSlot NodePath");
			Check(!groundMove.UsesGroundLayerSource, "Legacy Mod GroundMove unexpectedly stayed on the direct layer source");
			Check(groundMove.groundNode is AdobeAnimateSlot, "Legacy Mod GroundMove did not preserve the AdobeAnimateSlot source");
			groundMove.RefreshDirectionCache();
			Vector2 legacyStartPosition = zombie.GlobalPosition;
			await WaitPhysicsFrames(18);
			Check(Mathf.Abs(zombie.GlobalPosition.X - legacyStartPosition.X) > 0.01f, "Legacy Mod GroundSlot fallback stopped authored walking movement");
			groundMove.groundLayerName = "__missing_ground_layer__";
			Check(groundMove.ResolveMovementSource(), "Missing direct layer did not fall back to the legacy GroundSlot");
			Check(!groundMove.UsesGroundLayerSource && groundMove.groundNode is AdobeAnimateSlot, "Missing direct layer changed the legacy Mod movement source");
		}
		catch (Exception value)
		{
			_failures.Add($"runtime exception: {value}");
		}
		finally
		{
			TowerDefenseZombie.UseBatch = _previousUseBatch;
			if (GodotObject.IsInstanceValid(zombie))
			{
				zombie.QueueFree();
			}
			if (GodotObject.IsInstanceValid(rearZombie))
			{
				rearZombie.QueueFree();
			}
		}
		Finish();
	}

	private void VerifyGroundEntrySpeedBoundary(TowerDefenseZombie zombie, string label)
	{
		GroundMoveComponent groundMoveComponent = zombie?.groundMoveComponent;
		Check(groundMoveComponent != null && groundMoveComponent.Alive && groundMoveComponent.UsesGroundLayerSource, label + " has no active direct GroundMove source");
		Check(zombie?.attackComponent?.target == null, label + " unexpectedly acquired a target in the no-target entry fixture");
		if (groundMoveComponent != null && groundMoveComponent.Alive && groundMoveComponent.UsesGroundLayerSource)
		{
			Vector2 logicalGlobalPosition = zombie.GetLogicalGlobalPosition();
			Check(groundMoveComponent.TryGetCurrentGroundGlobalPosition(logicalGlobalPosition, out var groundGlobalPosition), label + " could not expose the current animation Ground world position");
			Check(zombie.sprite.TryGetManagedLayerPositionForRender(groundMoveComponent.GroundLayerId, Vector2.Zero, out var position) && groundGlobalPosition.DistanceTo(zombie.sprite.ToGlobal(position)) <= 0.01f, label + " entry position must match the current authored Ground layer world position");
			Check(groundGlobalPosition.X < logicalGlobalPosition.X - 0.01f, $"{label} Ground must lead its root while entering; ground={groundGlobalPosition.X:F4}, root={logicalGlobalPosition.X:F4}");
			float num = groundGlobalPosition.X - 1f;
			zombie.groundRight = num;
			zombie.timeScale = 1.0;
			zombie.walkSpeedScale = 1.0;
			ulong playbackRevisionForBareTest = zombie.sprite.PlaybackRevisionForBareTest;
			zombie.WalkProcessing(1.0 / 60.0);
			Check(Mathf.IsEqualApprox((float)zombie.sprite.timeScale, zombie.swimComponent.offscreenSpeedMultiplier) && zombie.sprite.PlaybackRevisionForBareTest == playbackRevisionForBareTest + 1, $"{label} must retain entry acceleration and invalidate its submitted playback while Ground is outside; timeScale={zombie.sprite.timeScale:F4}, revision={zombie.sprite.PlaybackRevisionForBareTest}, previousRevision={playbackRevisionForBareTest}");
			float num2 = (groundGlobalPosition.X + logicalGlobalPosition.X) * 0.5f;
			zombie.groundRight = num2;
			ulong playbackRevisionForBareTest2 = zombie.sprite.PlaybackRevisionForBareTest;
			zombie.WalkProcessing(1.0 / 60.0);
			Check(logicalGlobalPosition.X > num2 && groundGlobalPosition.X <= num2 && Mathf.IsEqualApprox((float)zombie.sprite.timeScale, 1f) && zombie.sprite.PlaybackRevisionForBareTest == playbackRevisionForBareTest2 + 1, $"{label} must return to 1x and invalidate its submitted playback as soon as its current Ground enters; ground={groundGlobalPosition.X:F4}, root={logicalGlobalPosition.X:F4}, boundary={num2:F4}, timeScale={zombie.sprite.timeScale:F4}, revision={zombie.sprite.PlaybackRevisionForBareTest}, previousRevision={playbackRevisionForBareTest2}");
		}
	}

	private static Transform2D ReadCachedRenderTransform(AdobeAnimateSprite sprite)
	{
		MethodInfo? method = typeof(AdobeAnimateSprite).GetMethod("GetCachedGlobalTransformForRender", BindingFlags.Instance | BindingFlags.NonPublic);
		if (method == null)
		{
			throw new MissingMethodException("AdobeAnimateSprite", "GetCachedGlobalTransformForRender");
		}
		return (Transform2D)method.Invoke(sprite, null);
	}

	private async Task WaitPhysicsFrames(int count)
	{
		for (int index = 0; index < count; index++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private void Check(bool condition, string failure)
	{
		if (!condition)
		{
			_failures.Add(failure);
		}
	}

	private void Finish()
	{
		for (int i = 0; i < _failures.Count; i++)
		{
			GD.PrintErr("ZOMBIE_GROUND_MOVE_FAILURE " + _failures[i]);
		}
		bool flag = _failures.Count == 0;
		GD.Print($"ZOMBIE_GROUND_MOVE_RESULT passed={flag} failures={_failures.Count}");
		GetTree().Quit((!flag) ? 2 : 0);
	}
}
