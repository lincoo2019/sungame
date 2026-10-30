using System;
using Godot;

public partial class ZombieBossDeathArmPlaybackRuntimeTest : Node
{
	private const string BossSpriteScenePath = "res://Asset/Anime/Character/Zombie/Boss/Boss/ZombieBoss.tscn";

	private const string BossDaveSpriteScenePath = "res://Asset/Anime/Character/Zombie/Boss/BossDave/ZombieBossDave.tscn";

	private const string ResultMarker = "ZOMBIE_BOSS_DEATH_ARM_PLAYBACK_RESULT";

	private int _checks;

	private int _failures;

	public override void _Ready()
	{
		VerifyDeathArmPlayback("res://Asset/Anime/Character/Zombie/Boss/Boss/ZombieBoss.tscn", "Boss");
		VerifyDeathArmPlayback("res://Asset/Anime/Character/Zombie/Boss/BossDave/ZombieBossDave.tscn", "DaveBoss");
		bool flag = _failures == 0 && _checks == 12;
		GD.Print($"{"ZOMBIE_BOSS_DEATH_ARM_PLAYBACK_RESULT"} passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private void VerifyDeathArmPlayback(string scenePath, string bossName)
	{
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(scenePath, null, ResourceLoader.CacheMode.Ignore);
		Check(GodotObject.IsInstanceValid(packedScene), bossName + " production sprite scene must load.");
		AdobeAnimateSpriteBase adobeAnimateSpriteBase = packedScene?.Instantiate<AdobeAnimateSpriteBase>(PackedScene.GenEditState.Disabled);
		Check(GodotObject.IsInstanceValid(adobeAnimateSpriteBase), bossName + " production sprite must instantiate.");
		if (GodotObject.IsInstanceValid(adobeAnimateSpriteBase))
		{
			AddChild(adobeAnimateSpriteBase, forceReadableName: false, InternalMode.Disabled);
			AdobeAnimateSpriteBase nodeOrNull = adobeAnimateSpriteBase.GetNodeOrNull<AdobeAnimateSpriteBase>("%Arm");
			Check(GodotObject.IsInstanceValid(nodeOrNull), bossName + " production sprite must expose its separated arm.");
			if (!GodotObject.IsInstanceValid(nodeOrNull))
			{
				adobeAnimateSpriteBase.QueueFree();
				return;
			}
			adobeAnimateSpriteBase.SetAnimation("Death", loop: false);
			Check(adobeAnimateSpriteBase.clip == "Death" && nodeOrNull.clip == "Death", bossName + " body and arm must both select the Death clip.");
			Check(!adobeAnimateSpriteBase.loop && !nodeOrNull.loop, bossName + " arm must inherit the body's non-looping Death playback.");
			nodeOrNull.frameIndex = Math.Max(nodeOrNull.clipRange.X, nodeOrNull.clipRange.Y - 1);
			nodeOrNull.elapsedTimer = 0.0;
			nodeOrNull.RunBatchedProcessUpdate(2.0 / Math.Max(1.0, nodeOrNull.frameRate));
			Check(nodeOrNull.clipOver && nodeOrNull.frameIndex == Math.Max(nodeOrNull.clipRange.X, nodeOrNull.clipRange.Y - 1), bossName + " arm must stop on the Death end pose instead of falling again.");
			adobeAnimateSpriteBase.QueueFree();
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[ZombieBossDeathArmPlaybackRuntimeTest] " + message);
		}
	}
}
