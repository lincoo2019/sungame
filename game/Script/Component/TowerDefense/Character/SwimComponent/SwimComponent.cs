using Godot;
using Godot.Collections;

public sealed class SwimComponent : CharacterComponentRuntime
{
	public double animationBlend = 0.2;

	public double waterLoopBlend;

	public double outWaterWalkBlend;

	public float offscreenSpeedMultiplier = 2f;

	public float outWaterHorizontalOffset = 5f;

	public string underwaterEntryAudio = "ZombieEnteringWater";

	public string surfaceEntryAudio = "PlantWater";

	public bool createSplashOnEntryAnimation = true;

	public bool updateGroundHeightOnEntry = true;

	public TowerDefenseZombie parent;

	private static readonly StringName SyncAliveKey = new StringName("_alive");

	private bool _syncPayloadInitialized;

	private bool _syncPayloadInSwimPlay;

	private bool _syncPayloadInWater;

	private bool _syncPayloadOutFromWater;

	private bool _configured;

	private Dictionary _syncPayload => GetReusableSyncPayload();

	private SwimComponentDefinition Definition => base.ComponentDefinition as SwimComponentDefinition;

	protected override void OnBound()
	{
		parent = base.Owner as TowerDefenseZombie;
		ApplyDefinition();
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		ClearSyncPayload();
		parent = null;
	}

	protected override void OnReleased()
	{
		ClearSyncPayload();
		parent = null;
		_configured = false;
	}

	private void ApplyDefinition()
	{
		if (!_configured && Definition != null)
		{
			animationBlend = Definition.animationBlend;
			waterLoopBlend = Definition.waterLoopBlend;
			outWaterWalkBlend = Definition.outWaterWalkBlend;
			offscreenSpeedMultiplier = Definition.offscreenSpeedMultiplier;
			outWaterHorizontalOffset = Definition.outWaterHorizontalOffset;
			underwaterEntryAudio = Definition.underwaterEntryAudio;
			surfaceEntryAudio = Definition.surfaceEntryAudio;
			createSplashOnEntryAnimation = Definition.createSplashOnEntryAnimation;
			updateGroundHeightOnEntry = Definition.updateGroundHeightOnEntry;
			_configured = true;
		}
	}

	public void WalkEntered(double? animationBlendOverride = null)
	{
		if (TryGetParent(out var zombie))
		{
			if (zombie.inWater)
			{
				PlayWaterWalk(zombie, animationBlendOverride);
			}
			else
			{
				PlayLandWalk(zombie, animationBlendOverride);
			}
		}
	}

	private void PlayWaterWalk(TowerDefenseZombie zombie, double? animationBlendOverride)
	{
		string text = (string.IsNullOrEmpty(zombie.swimAnimeClip) ? zombie.walkAnimeClip : zombie.swimAnimeClip);
		if (!zombie.inSwimPlay && !string.IsNullOrEmpty(zombie.inSwimAnimeClip))
		{
			zombie.sprite.SetAnimation(zombie.inSwimAnimeClip, loop: false, animationBlendOverride ?? animationBlend);
			if (!string.IsNullOrEmpty(text))
			{
				zombie.sprite.AddAnimation(text, 0.0, loop: true, waterLoopBlend);
			}
			zombie.inSwimPlay = true;
		}
		else if (!string.IsNullOrEmpty(text))
		{
			zombie.sprite.SetAnimation(text, loop: true, waterLoopBlend);
		}
	}

	private void PlayLandWalk(TowerDefenseZombie zombie, double? animationBlendOverride)
	{
		if (!string.IsNullOrEmpty(zombie.walkAnimeClip))
		{
			double blendTime = animationBlendOverride ?? animationBlend;
			WaterInteractionComponent waterInteractionComponent = zombie.waterInteractionComponent;
			if (waterInteractionComponent != null && !waterInteractionComponent.IsReleased && zombie.waterInteractionComponent.outFromWater)
			{
				zombie.waterInteractionComponent.outFromWater = false;
				blendTime = animationBlendOverride ?? outWaterWalkBlend;
			}
			zombie.sprite.SetAnimation(zombie.walkAnimeClip, loop: true, blendTime);
		}
	}

	public void WalkProcessing(float _delta)
	{
		if (TryGetParent(out var zombie))
		{
			ulong currentPhysicsFrame = TowerDefenseProcessModeDispatch.CurrentPhysicsFrame;
			UpdateWalkPlaybackTimeScale(zombie, zombie.sprite, currentPhysicsFrame);
		}
	}

	internal void WalkProcessingValidated(TowerDefenseZombie zombie, AdobeAnimateSprite playbackSprite, ulong physicsFrame)
	{
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && parent == zombie && playbackSprite != null)
		{
			UpdateWalkPlaybackTimeScale(zombie, playbackSprite, physicsFrame);
		}
	}

	private void UpdateWalkPlaybackTimeScale(TowerDefenseZombie zombie, AdobeAnimateSprite playbackSprite, ulong physicsFrame)
	{
		double num2;
		if (playbackSprite.clip != zombie.inSwimAnimeClip)
		{
			Vector2 ownerGlobalPosition = ((physicsFrame == 18446744073709551615uL) ? zombie.GetLogicalGlobalPosition() : zombie.GetGlobalPositionForPhysicsFrame(physicsFrame));
			float x = ownerGlobalPosition.X;
			GroundMoveComponent groundMoveComponent = zombie.groundMoveComponent;
			if (groundMoveComponent != null && groundMoveComponent.TryGetCurrentGroundGlobalPosition(ownerGlobalPosition, out var groundGlobalPosition))
			{
				x = groundGlobalPosition.X;
			}
			double num = (((double)x > zombie.groundRight) ? ((double)offscreenSpeedMultiplier) : 1.0);
			num2 = zombie.timeScale * zombie.walkSpeedScale * num;
		}
		else
		{
			num2 = zombie.timeScale * zombie.inSwimAnimeClipScale;
		}
		if (!Mathf.IsEqualApprox(playbackSprite.timeScale, num2))
		{
			playbackSprite.timeScale = num2;
		}
	}

	public void InWater()
	{
		if (TryGetParent(out var zombie))
		{
			if (updateGroundHeightOnEntry && !zombie.isRise)
			{
				zombie.groundHeight = 0.0 - zombie.waterHeight;
			}
			if ((!GodotObject.IsInstanceValid(Global.Instance) || !Global.Instance.isEditor || !(SceneManager.CurrentScene == "LevelEditorStage")) && GodotObject.IsInstanceValid(TowerDefenseManager.CurrentControl) && TowerDefenseManager.CurrentControl.isGameRunning && zombie.swimAnimeClip != zombie.walkAnimeClip && !zombie.die && !zombie.nearDie)
			{
				zombie.Walk();
			}
		}
	}

	public void OutWater()
	{
		if (!TryGetParent(out var zombie))
		{
			return;
		}
		if (!Mathf.IsZeroApprox(outWaterHorizontalOffset))
		{
			Vector2 logicalGlobalPosition = zombie.GetLogicalGlobalPosition();
			zombie.SetLogicalGlobalPosition(new Vector2(logicalGlobalPosition.X - (float)Mathf.Sign(zombie.Scale.X) * outWaterHorizontalOffset, logicalGlobalPosition.Y));
		}
		zombie.inSwimPlay = false;
		if (!string.IsNullOrEmpty(zombie.outSwimAnimeClip))
		{
			zombie.sprite.SetAnimation(zombie.outSwimAnimeClip, loop: false, animationBlend);
		}
		if (!(zombie.swimAnimeClip == zombie.walkAnimeClip) && !zombie.die && !zombie.nearDie)
		{
			WaterInteractionComponent waterInteractionComponent = zombie.waterInteractionComponent;
			if (waterInteractionComponent != null && !waterInteractionComponent.IsReleased)
			{
				zombie.waterInteractionComponent.outFromWater = true;
			}
			zombie.Walk();
		}
	}

	public void AnimeCompleted(string clip)
	{
		if (TryGetParent(out var zombie) && !string.IsNullOrEmpty(zombie.inSwimAnimeClip) && !(clip != zombie.inSwimAnimeClip))
		{
			if (createSplashOnEntryAnimation)
			{
				zombie.CreateSplash();
			}
			string text = (((zombie.instance.maskFlags & 0x20) != 0) ? underwaterEntryAudio : surfaceEntryAudio);
			if (!string.IsNullOrEmpty(text) && GodotObject.IsInstanceValid(AudioManager.Instance))
			{
				AudioManager.Instance.AudioPlay(text);
			}
		}
	}

	private bool TryGetParent(out TowerDefenseZombie zombie)
	{
		zombie = parent;
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(zombie) && GodotObject.IsInstanceValid(zombie.instance))
		{
			return GodotObject.IsInstanceValid(zombie.sprite);
		}
		return false;
	}

	public override Dictionary ExportComponentSave()
	{
		return new Dictionary { 
		{
			"inSwimPlay",
			GodotObject.IsInstanceValid(parent) && parent.inSwimPlay
		} };
	}

	public override void ImportComponentSave(Dictionary data, TowerDefenseLevelSaveConfigCSharp owner)
	{
		if (GodotObject.IsInstanceValid(parent))
		{
			parent.inSwimPlay = data.GetValueOrDefault("inSwimPlay", false).AsBool();
		}
	}

	public override Dictionary SyncSerialize()
	{
		bool flag = GodotObject.IsInstanceValid(parent) && parent.inSwimPlay;
		bool flag2 = GodotObject.IsInstanceValid(parent) && parent.inWater;
		TowerDefenseZombie towerDefenseZombie = parent;
		bool flag3 = towerDefenseZombie != null && towerDefenseZombie.waterInteractionComponent?.IsReleased == false && parent.waterInteractionComponent.outFromWater;
		if (_syncPayloadInitialized)
		{
			if (_syncPayload.Count == 4)
			{
				_syncPayload.Remove(SyncAliveKey);
			}
			if (_syncPayload.Count == 3 && _syncPayloadInSwimPlay == flag && _syncPayloadInWater == flag2 && _syncPayloadOutFromWater == flag3)
			{
				return _syncPayload;
			}
		}
		ClearReusableSyncPayload();
		_syncPayload["inSwimPlay"] = flag;
		_syncPayload["inWater"] = flag2;
		_syncPayload["outFromWater"] = flag3;
		_syncPayloadInSwimPlay = flag;
		_syncPayloadInWater = flag2;
		_syncPayloadOutFromWater = flag3;
		_syncPayloadInitialized = true;
		return _syncPayload;
	}

	private void ClearSyncPayload()
	{
		ClearReusableSyncPayload();
		_syncPayloadInitialized = false;
		_syncPayloadInSwimPlay = false;
		_syncPayloadInWater = false;
		_syncPayloadOutFromWater = false;
	}

	public override void SyncDeserialize(Dictionary data)
	{
		if (GodotObject.IsInstanceValid(parent))
		{
			parent.inSwimPlay = data.GetValueOrDefault("inSwimPlay", parent.inSwimPlay).AsBool();
			parent.inWater = data.GetValueOrDefault("inWater", parent.inWater).AsBool();
			WaterInteractionComponent waterInteractionComponent = parent.waterInteractionComponent;
			if (waterInteractionComponent != null && !waterInteractionComponent.IsReleased)
			{
				parent.waterInteractionComponent.outFromWater = data.GetValueOrDefault("outFromWater", parent.waterInteractionComponent.outFromWater).AsBool();
			}
		}
	}
}
