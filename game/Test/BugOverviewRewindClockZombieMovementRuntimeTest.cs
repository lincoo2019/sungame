using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewRewindClockZombieMovementRuntimeTest : Node
{
	private const string RewindClockScenePath = "res://Asset/Anime/Character/Plant/Star/AppleGreen/Scene/TowerDefensePlantAppleGreen.tscn";

	private const string AppleClockScenePath = "res://Asset/Anime/Character/Plant/Star/Apple/Scene/TowerDefensePlantApple.tscn";

	private const string NormalZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private const int ReverseSampleFrames = 420;

	private readonly List<string> _failures = new List<string>();

	private int _checks;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		bool previousBackZombie = manager?.backZombie ?? false;
		bool previousBackPacket = manager?.backPacket ?? false;
		bool previousPauseZombie = manager?.pauseZombie ?? false;
		bool previousPausePacket = manager?.pausePacket ?? false;
		bool previousUseBatch = TowerDefenseZombie.UseBatch;
		AdobeAnimateRenderBackend previousBackend = Global.Instance.adobeAnimateRenderBackend;
		bool previousRaster = AdobeAnimateRenderManager.RasterCompositeEnabled;
		TowerDefensePlantAppleGreen clock = null;
		TowerDefensePlantApple appleClock = null;
		TowerDefenseZombieNormal zombie = null;
		try
		{
			_ = 9;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager))
				{
					goto end_IL_0117;
				}
				TowerDefenseZombie.UseBatch = true;
				Global.Instance.adobeAnimateRenderBackend = AdobeAnimateRenderBackend.GpuCrowd;
				AdobeAnimateRenderManager.RasterCompositeEnabled = false;
				clock = Instantiate<TowerDefensePlantAppleGreen>("res://Asset/Anime/Character/Plant/Star/AppleGreen/Scene/TowerDefensePlantAppleGreen.tscn");
				zombie = Instantiate<TowerDefenseZombieNormal>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn");
				if (GodotObject.IsInstanceValid(clock))
				{
					clock.inGame = false;
				}
				Check(GodotObject.IsInstanceValid(clock) && clock.config?.name == "PlantAppleGreen", "The reported fixture must use the real Rewind Clock scene.");
				Check(GodotObject.IsInstanceValid(zombie) && zombie.config?.name == "ZombieNormal", "The movement fixture must be a real ordinary zombie, not a vehicle zombie.");
				if (!GodotObject.IsInstanceValid(clock) || !GodotObject.IsInstanceValid(zombie))
				{
					goto end_IL_0117;
				}
				zombie.inGame = true;
				zombie.editorPreviewMode = false;
				zombie.GlobalPosition = new Vector2(1000f, 100f);
				AddChild(zombie, forceReadableName: false, InternalMode.Disabled);
				AddChild(clock, forceReadableName: false, InternalMode.Disabled);
				await WaitPhysicsFrames(4);
				GroundMoveComponent groundMove = zombie.groundMoveComponent;
				Check(zombie.HasValidRuntimeConfiguration, "The real ordinary zombie must have a valid runtime configuration.");
				Check(groundMove != null && !groundMove.IsReleased && groundMove.HasMovementSource, "The real ordinary zombie must move through its authored GroundSlot runtime.");
				if (groundMove == null || groundMove.IsReleased)
				{
					goto end_IL_0117;
				}
				zombie.Walk();
				await WaitPhysicsFrames(24);
				Check(groundMove.Alive, "GroundMove must be active after the ordinary zombie enters Walk.");
				float forwardStartX = zombie.GlobalPosition.X;
				await WaitPhysicsFrames(90);
				float forwardDelta = zombie.GlobalPosition.X - forwardStartX;
				Check(forwardDelta < -0.1f, $"Before rewind, the ordinary zombie must advance toward the house; deltaX={forwardDelta:F3}.");
				CheckGpuPlayback(zombie, playing: true, reversed: false, "正常行走");
				clock.RunApple();
				await WaitPhysicsFrames(2);
				Check(manager.backZombie && manager.backPacket && clock.run, "The real Rewind Clock must activate zombie and packet rewind flags.");
				Check(zombie.sprite.playBack, "The live ordinary-zombie sprite must enter reverse playback from the clock effect.");
				CheckGpuPlayback(zombie, playing: true, reversed: true, "逆时效果");
				float rewindStartX = zombie.GlobalPosition.X;
				float previousX = rewindStartX;
				float mostForwardStep = 0f;
				float largestReverseStep = 0f;
				int forwardStepCount = 0;
				int reverseLoopCount = 0;
				zombie.sprite.OnAnimeCompleted += delegate
				{
					if (zombie.sprite.playBack)
					{
						reverseLoopCount++;
					}
				};
				for (int frame = 0; frame < 420; frame++)
				{
					await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
					float x = zombie.GlobalPosition.X;
					float num = x - previousX;
					if (num < mostForwardStep)
					{
						mostForwardStep = num;
					}
					if (num > largestReverseStep)
					{
						largestReverseStep = num;
					}
					if (num < -0.25f)
					{
						forwardStepCount++;
					}
					previousX = x;
				}
				float num2 = zombie.GlobalPosition.X - rewindStartX;
				Check(num2 > 0.1f, $"During rewind, the ordinary zombie must retreat away from the house; deltaX={num2:F3}.");
				Check(largestReverseStep > 0.01f, $"Reverse playback must produce real backward movement; largestStep={largestReverseStep:F3}.");
				Check(reverseLoopCount >= 3, $"The regression sample must cross several real reverse-loop boundaries; loops={reverseLoopCount}.");
				Check(forwardStepCount == 0, $"Reverse playback must never teleport the ordinary zombie forward; forwardSteps={forwardStepCount}, worstStep={mostForwardStep:F3}.");
				appleClock = Instantiate<TowerDefensePlantApple>("res://Asset/Anime/Character/Plant/Star/Apple/Scene/TowerDefensePlantApple.tscn");
				Check(GodotObject.IsInstanceValid(appleClock) && appleClock.config?.name == "PlantApple", "The overlap fixture must use the real Apple Clock scene.");
				if (!GodotObject.IsInstanceValid(appleClock))
				{
					goto end_IL_0117;
				}
				appleClock.inGame = false;
				AddChild(appleClock, forceReadableName: false, InternalMode.Disabled);
				appleClock.RunApple();
				await WaitPhysicsFrames(3);
				Check(manager.backZombie && manager.pauseZombie && zombie.sprite.playBack && zombie.sprite.pause, "Apple Clock must pause the live zombie while the earlier Rewind Clock still owns reverse playback.");
				CheckGpuPlayback(zombie, playing: false, reversed: true, "逆时与暂停重叠");
				float pausedStartX = zombie.GlobalPosition.X;
				await WaitPhysicsFrames(30);
				Check(Mathf.Abs(zombie.GlobalPosition.X - pausedStartX) < 0.01f, "The real zombie must remain stationary while Apple Clock pause overlaps Rewind Clock.");
				clock.isShovel = true;
				clock.Destroy();
				await WaitPhysicsFrames(4);
				Check(!manager.backZombie && manager.pauseZombie && !zombie.sprite.playBack && zombie.sprite.pause, "Rewind Clock expiry during Apple Clock pause must release only reverse playback.");
				CheckGpuPlayback(zombie, playing: false, reversed: false, "暂停期间逆时结束");
				appleClock.isShovel = true;
				appleClock.Destroy();
				await WaitPhysicsFrames(4);
				Check(!manager.backZombie && !manager.pauseZombie && !zombie.sprite.playBack && !zombie.sprite.pause, "Apple Clock expiry must restore ordinary forward playback after the overlapping effects end.");
				CheckGpuPlayback(zombie, playing: true, reversed: false, "两个效果均结束");
				float postClockStartX = zombie.GlobalPosition.X;
				float postClockPreviousX = postClockStartX;
				float largestPostClockStep = 0f;
				for (int frame = 0; frame < 120; frame++)
				{
					await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
					float x2 = zombie.GlobalPosition.X;
					largestPostClockStep = Mathf.Max(largestPostClockStep, Mathf.Abs(x2 - postClockPreviousX));
					postClockPreviousX = x2;
				}
				CheckGpuPlayback(zombie, playing: true, reversed: false, "效果结束后持续行走");
				float num3 = zombie.GlobalPosition.X - postClockStartX;
				float num4 = Mathf.Max(0.01f, Mathf.Abs(forwardDelta) / 90f);
				Check(num3 < -0.1f, $"The zombie must resume walking toward the house after both clocks end; deltaX={num3:F3}.");
				Check(largestPostClockStep <= Mathf.Max(1f, num4 * 8f), $"Clock direction and pause release must not translate the zombie in one frame; largestStep={largestPostClockStep:F3}, normalStep={num4:F3}.");
				goto end_IL_00e3;
				end_IL_0117:;
			}
			catch (Exception value)
			{
				_failures.Add($"Unexpected runtime exception: {value}");
				goto end_IL_00e3;
			}
			return;
			end_IL_00e3:;
		}
		finally
		{
			TowerDefenseZombie.UseBatch = previousUseBatch;
			Global.Instance.adobeAnimateRenderBackend = previousBackend;
			AdobeAnimateRenderManager.RasterCompositeEnabled = previousRaster;
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.backZombie = previousBackZombie;
				manager.backPacket = previousBackPacket;
				manager.pauseZombie = previousPauseZombie;
				manager.pausePacket = previousPausePacket;
			}
			if (GodotObject.IsInstanceValid(clock))
			{
				clock.QueueFree();
			}
			if (GodotObject.IsInstanceValid(appleClock))
			{
				appleClock.QueueFree();
			}
			if (GodotObject.IsInstanceValid(zombie))
			{
				zombie.QueueFree();
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		Finish();
	}

	private void CheckGpuPlayback(TowerDefenseZombie zombie, bool playing, bool reversed, string stage)
	{
		AdobeAnimateCrowdRenderState state;
		AdobeAnimateCrowdRenderStateResult adobeAnimateCrowdRenderStateResult = zombie.sprite.TryBuildCrowdRenderState(out state);
		AdobeAnimateGpuGraphOwnerState adobeAnimateGpuGraphOwnerState = state?.GpuGraphRootOwnerState;
		Check(adobeAnimateCrowdRenderStateResult == AdobeAnimateCrowdRenderStateResult.Submitted && state.Mode == AdobeAnimateCrowdRenderMode.GpuGraph && adobeAnimateGpuGraphOwnerState != null && adobeAnimateGpuGraphOwnerState.GpuClock.Enabled == playing && adobeAnimateGpuGraphOwnerState.GpuClock.FramesPerSecond < 0f == reversed, $"{stage}：GPU 时钟应 playing={playing}, reversed={reversed}，实际 result={adobeAnimateCrowdRenderStateResult}, mode={state?.Mode}, enabled={adobeAnimateGpuGraphOwnerState?.GpuClock.Enabled}, fps={adobeAnimateGpuGraphOwnerState?.GpuClock.FramesPerSecond}。");
	}

	private static T Instantiate<T>(string path) where T : Node
	{
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Ignore);
		if (packedScene == null)
		{
			return null;
		}
		return packedScene.Instantiate<T>(PackedScene.GenEditState.Disabled);
	}

	private async Task WaitPhysicsFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures.Add(message);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PushError("[BugOverviewRewindClockZombieMovementRuntimeTest] " + failure);
		}
		bool flag = _failures.Count == 0;
		GD.Print($"REWIND_CLOCK_ZOMBIE_MOVEMENT_RESULT passed={flag} checks={_checks} failures={_failures.Count}");
		GetTree().Quit((!flag) ? 2 : 0);
	}
}
