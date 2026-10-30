using Godot;

public partial class BugOverviewBossDeathHeadOverlayRuntimeTest : Node
{
	private const string BossScenePath = "res://Asset/Anime/Character/Zombie/Boss/Boss/ZombieBoss.tscn";

	private const string BossDaveScenePath = "res://Asset/Anime/Character/Zombie/Boss/BossDave/ZombieBossDave.tscn";

	private int _checks;

	private int _failures;

	public override void _Ready()
	{
		VerifyBossDeathOverlay<ZombieBoss>("res://Asset/Anime/Character/Zombie/Boss/Boss/ZombieBoss.tscn", "Boss", "Zombie_boss_head_damage2.png");
		VerifyBossDeathOverlay<ZombieBossDave>("res://Asset/Anime/Character/Zombie/Boss/BossDave/ZombieBossDave.tscn", "DaveBoss", "Zombie_bossDave_head_damage2.png");
		bool flag = _failures == 0 && _checks == 18;
		GD.Print($"BOSS_DEATH_HEAD_OVERLAY_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private void VerifyBossDeathOverlay<TSprite>(string scenePath, string label, string damagedHeadFile) where TSprite : AdobeAnimateSprite
	{
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(scenePath, null, ResourceLoader.CacheMode.Ignore);
		Check(GodotObject.IsInstanceValid(packedScene), label + " production sprite scene must load.");
		TSprite val = ((packedScene != null) ? packedScene.Instantiate<TSprite>(PackedScene.GenEditState.Disabled) : null);
		Check(GodotObject.IsInstanceValid(val), label + " production sprite must instantiate with its exact runtime type.");
		if (!GodotObject.IsInstanceValid(val))
		{
			return;
		}
		AddChild(val, forceReadableName: false, InternalMode.Disabled);
		AdobeAnimateSlot nodeOrNull = val.GetNodeOrNull<AdobeAnimateSlot>("HeadSlot");
		AdobeAnimatePart nodeOrNull2 = val.GetNodeOrNull<AdobeAnimatePart>("%HeadSprite");
		Check(GodotObject.IsInstanceValid(nodeOrNull) && nodeOrNull.useFollowVisible, label + " must expose its production follow-visible head slot.");
		Check(GodotObject.IsInstanceValid(nodeOrNull2), label + " must expose its production external-atlas head overlay.");
		if (!GodotObject.IsInstanceValid(nodeOrNull) || !GodotObject.IsInstanceValid(nodeOrNull2))
		{
			val.QueueFree();
			return;
		}
		ApplyDamagePoint(val, "Stage2");
		Check(nodeOrNull2.externalAtlasTexturePath.GetFile() == damagedHeadFile, label + " Stage2 must activate the authored damaged head overlay; path=" + nodeOrNull2.externalAtlasTexturePath + ".");
		Check(AdobeAnimateManagedSprite2D.IsManagedVisible(nodeOrNull2), label + " damaged head overlay must be submitted before death.");
		val.SetAnimation("Death", loop: false);
		Check(val.clip == "Death" && !val.loop, label + " must enter the real non-looping Death clip.");
		bool flag = false;
		int layerId = val.flashAnimeData.layerDictionary.GetValueOrDefault("Boss_head", -1).AsInt32();
		for (int i = val.clipRange.X; i < val.clipRange.Y; i++)
		{
			val.frameIndex = i;
			val.elapsedTimer = 0.0;
			flag |= val.TryGetInterpolatedLayerPoseForRender(layerId, out var mediaId, out var _) && mediaId != 65535;
		}
		Check(flag, label + " Death timeline must retain its own authored main-head frames.");
		bool flag2 = AdobeAnimateManagedSprite2D.IsManagedVisible(nodeOrNull2) && AdobeAnimateManagedSprite2D.ShouldRender(nodeOrNull2);
		Transform2D transform2;
		bool value = val.TryGetManagedSlotTransformForRender(nodeOrNull, out transform2);
		Check(!flag2, $"{label} Death clip must not submit the detached damaged-head overlay; managedVisible={flag2}, slotPose={value}, slotOrigin={transform2.Origin}, path={nodeOrNull2.externalAtlasTexturePath}.");
		val.QueueFree();
	}

	private static void ApplyDamagePoint(AdobeAnimateSprite sprite, string damagePoint)
	{
		if (!(sprite is ZombieBoss zombieBoss))
		{
			if (sprite is ZombieBossDave zombieBossDave)
			{
				zombieBossDave.DamagePointSet(damagePoint);
			}
		}
		else
		{
			zombieBoss.DamagePointSet(damagePoint);
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BossDeathHeadOverlay] " + message);
		}
	}
}
