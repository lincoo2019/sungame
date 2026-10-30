using Godot;

[Tool]
public partial class TowerDefenseZombieBobsledTeam : TowerDefenseZombieBobsledVehicleBase
{
	private static readonly Vector2[] PushCrewPositions = new Vector2[4]
	{
		new Vector2(-27f, -49f),
		new Vector2(23f, -68f),
		new Vector2(73f, -52f),
		new Vector2(123f, -68f)
	};

	private static readonly Vector2[] JumpStartLocalPositions = new Vector2[4]
	{
		new Vector2(0f, 9f),
		new Vector2(0f, -8f),
		new Vector2(0f, 9f),
		new Vector2(0f, -8f)
	};

	private readonly AdobeAnimateSprite[] _pushCrew = new AdobeAnimateSprite[4];

	private readonly AdobeAnimateSprite[] _seatCrew = new AdobeAnimateSprite[4];

	private bool _entryCompletionSpritesBound;

	private bool _entryJumpCompletionPending;

	protected override bool UsesBreakAnimationCompletion => false;

	protected override string GetBreakClip()
	{
		return "Broken";
	}

	public override void _Ready()
	{
		base._Ready();
		for (int i = 0; i < 4; i++)
		{
			_pushCrew[i] = GetNodeOrNull<AdobeAnimateSprite>($"SpriteGroup/TransformPoint/ZombieBobsledVehicle/PushCrew{i}");
			_seatCrew[i] = GetNodeOrNull<AdobeAnimateSprite>($"SpriteGroup/TransformPoint/ZombieBobsledVehicle/Passenger{i}Slot/SeatCrew{i}");
		}
		BindEntryCompletionSprites();
		OnPhaseChanged(base.CurrentPhase);
		SyncCrewVisualPresentation();
	}

	public override void _ExitTree()
	{
		UnbindEntryCompletionSprites();
		base._ExitTree();
	}

	public override void ActivateLevelEntryPreview(string animationClip)
	{
		base.ActivateLevelEntryPreview(animationClip);
		ApplyLevelEntryPreviewPresentation();
	}

	public override void WalkProcessing(double delta)
	{
		base.WalkProcessing(delta);
		UpdateEntryJumpPresentation();
		SyncCrewVisualPresentation();
		if (!isShow)
		{
			ApplyRuntimeCrewVisibility(base.CurrentPhase);
		}
		if (_entryJumpCompletionPending && CanCompleteEntryJumpPresentation())
		{
			_entryJumpCompletionPending = false;
			CompleteAlignedEntryJump();
		}
	}

	public override void DamagePointReach(string damagePointName)
	{
		base.DamagePointReach(damagePointName);
		BobsledVehiclePhase currentPhase = base.CurrentPhase;
		bool flag = (uint)(currentPhase - 3) <= 1u;
		if (!flag && GodotObject.IsInstanceValid(base.sprite))
		{
			if (damagePointName == "DamagePoint2")
			{
				base.sprite.SetAnimation("Damaged1");
			}
			else if (damagePointName == "DamagePoint3")
			{
				base.sprite.SetAnimation("Damaged2");
			}
		}
	}

	protected override void OnPhaseChanged(BobsledVehiclePhase phase)
	{
		if (Engine.IsEditorHint())
		{
			return;
		}
		if (phase != BobsledVehiclePhase.EnteringJump)
		{
			_entryJumpCompletionPending = false;
		}
		if (isShow)
		{
			ApplyLevelEntryPreviewPresentation();
			return;
		}
		switch (phase)
		{
		case BobsledVehiclePhase.EnteringPush:
			ApplyCrewPositions(_pushCrew, PushCrewPositions);
			break;
		case BobsledVehiclePhase.EnteringJump:
			ApplyCrewPositions(_seatCrew, JumpStartLocalPositions);
			break;
		case BobsledVehiclePhase.Riding:
			ResetCrewPositions(_seatCrew);
			break;
		}
		for (int i = 0; i < 4; i++)
		{
			if (GodotObject.IsInstanceValid(_pushCrew[i]))
			{
				_pushCrew[i].SetFrozenPreview(frozen: false);
				if (phase == BobsledVehiclePhase.EnteringPush)
				{
					_pushCrew[i].SetAnimation("Push");
				}
				RefreshCrewSprite(_pushCrew[i]);
			}
			if (GodotObject.IsInstanceValid(_seatCrew[i]))
			{
				_seatCrew[i].SetFrozenPreview(frozen: false);
				switch (phase)
				{
				case BobsledVehiclePhase.EnteringJump:
					_seatCrew[i].SetAnimation("Jump", loop: false);
					break;
				case BobsledVehiclePhase.Riding:
					PrepareHeldPoseSprite(_seatCrew[i], "Jump", frozenPreview: false);
					break;
				default:
					RefreshCrewSprite(_seatCrew[i]);
					break;
				}
			}
		}
		ApplyRuntimeCrewVisibility(phase);
		RefreshCompositeVisuals();
	}

	private void UpdateEntryJumpPresentation()
	{
		if (base.CurrentPhase != BobsledVehiclePhase.EnteringJump)
		{
			return;
		}
		AdobeAnimateSprite entryJumpDriver = GetEntryJumpDriver();
		if (!GodotObject.IsInstanceValid(entryJumpDriver))
		{
			if (CanCompleteEntryJumpPresentation())
			{
				CompleteAlignedEntryJump();
			}
			else
			{
				_entryJumpCompletionPending = true;
			}
			return;
		}
		int x = entryJumpDriver.clipRange.X;
		int num = Mathf.Max(x, entryJumpDriver.clipRange.Y - 1);
		float weight = ((num <= x) ? 1f : Mathf.Clamp(((float)(entryJumpDriver.frameIndex - x) + Mathf.Clamp((float)entryJumpDriver.elapsedTimer, 0f, 1f)) / (float)(num - x), 0f, 1f));
		for (int i = 0; i < 4; i++)
		{
			if (GodotObject.IsInstanceValid(_seatCrew[i]))
			{
				_seatCrew[i].Position = JumpStartLocalPositions[i].Lerp(Vector2.Zero, weight);
			}
		}
	}

	private AdobeAnimateSprite GetEntryJumpDriver()
	{
		int releasedSlotMask = GetReleasedSlotMask();
		for (int i = 0; i < _seatCrew.Length; i++)
		{
			if ((releasedSlotMask & (1 << i)) == 0 && GodotObject.IsInstanceValid(_seatCrew[i]))
			{
				return _seatCrew[i];
			}
		}
		return null;
	}

	private void ApplyRuntimeCrewVisibility(BobsledVehiclePhase phase)
	{
		bool flag = GodotObject.IsInstanceValid(base.sprite) && base.sprite.Visible;
		bool flag2 = base.isPause || !GodotObject.IsInstanceValid(base.sprite) || base.sprite.pause;
		bool flag3 = phase == BobsledVehiclePhase.EnteringPush;
		bool flag4 = (uint)(phase - 1) <= 1u;
		bool flag5 = flag4;
		int releasedSlotMask = GetReleasedSlotMask();
		for (int i = 0; i < 4; i++)
		{
			bool flag6 = (releasedSlotMask & (1 << i)) != 0;
			if (GodotObject.IsInstanceValid(_pushCrew[i]))
			{
				_pushCrew[i].Visible = flag && flag3 && !flag6;
				_pushCrew[i].pause = !flag3 || flag2;
			}
			if (GodotObject.IsInstanceValid(_seatCrew[i]))
			{
				bool flag7 = flag5 && !flag6;
				_seatCrew[i].Visible = flag && flag7;
				_seatCrew[i].pause = !flag7 || flag2;
			}
		}
	}

	private int GetReleasedSlotMask()
	{
		if (base.TeamComponent == null)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < 4; i++)
		{
			if (base.TeamComponent.IsSlotReleased(i))
			{
				num |= 1 << i;
			}
		}
		return num;
	}

	private void ApplyLevelEntryPreviewPresentation()
	{
		ResetCrewPositions(_seatCrew);
		for (int i = 0; i < 4; i++)
		{
			if (GodotObject.IsInstanceValid(_pushCrew[i]))
			{
				_pushCrew[i].Visible = false;
				_pushCrew[i].SetFrozenPreview(frozen: true);
				RefreshCrewSprite(_pushCrew[i]);
			}
			if (GodotObject.IsInstanceValid(_seatCrew[i]))
			{
				AdobeAnimateSprite obj = _seatCrew[i];
				obj.Visible = true;
				PrepareHeldPoseSprite(obj, "Jump", frozenPreview: true);
			}
		}
		RefreshCompositeVisuals();
	}

	private static void PrepareHeldPoseSprite(AdobeAnimateSprite visual, string clip, bool frozenPreview)
	{
		if (frozenPreview)
		{
			visual.ProcessMode = ProcessModeEnum.Always;
		}
		visual.SetFrozenPreview(frozen: false);
		visual.SetAnimation(clip, loop: false);
		visual.frameIndex = Mathf.Max(visual.clipRange.X, visual.clipRange.Y - 1);
		visual.elapsedTimer = 0.0;
		visual.clipOver = true;
		visual.UpdateMediaReplaceData();
		visual.UpdateChild();
		visual.RefreshManagedSlotSpriteCacheForRender();
		visual.SetFrozenPreview(frozenPreview);
		RefreshCrewSprite(visual);
	}

	private static void ApplyCrewPositions(AdobeAnimateSprite[] crew, Vector2[] positions)
	{
		for (int i = 0; i < crew.Length && i < positions.Length; i++)
		{
			if (GodotObject.IsInstanceValid(crew[i]))
			{
				crew[i].Position = positions[i];
			}
		}
	}

	private static void ResetCrewPositions(AdobeAnimateSprite[] crew)
	{
		for (int i = 0; i < crew.Length; i++)
		{
			if (GodotObject.IsInstanceValid(crew[i]))
			{
				crew[i].Position = Vector2.Zero;
			}
		}
	}

	private static void RefreshCrewSprite(AdobeAnimateSprite crew)
	{
		if (GodotObject.IsInstanceValid(crew))
		{
			crew.RefreshProcessScheduling();
			crew.QueueRedraw();
		}
	}

	private void RefreshCompositeVisuals()
	{
		RefreshCrewSprite(base.sprite);
		SyncCrewVisualPresentation();
	}

	private void SyncCrewVisualPresentation()
	{
		if (GodotObject.IsInstanceValid(base.sprite))
		{
			for (int i = 0; i < 4; i++)
			{
				SyncVisualPresentation(_pushCrew[i]);
				SyncVisualPresentation(_seatCrew[i]);
			}
		}
	}

	private void SyncVisualPresentation(AdobeAnimateSprite visual)
	{
		if (GodotObject.IsInstanceValid(visual) && visual != base.sprite)
		{
			if (visual.meshColor != base.sprite.meshColor)
			{
				visual.meshColor = base.sprite.meshColor;
			}
			if (visual.LightMask != base.sprite.LightMask)
			{
				visual.LightMask = base.sprite.LightMask;
			}
			if (!Mathf.IsEqualApprox((float)visual.timeScale, (float)base.sprite.timeScale))
			{
				visual.timeScale = base.sprite.timeScale;
			}
			if (visual.playBack != base.sprite.playBack)
			{
				visual.playBack = base.sprite.playBack;
			}
		}
	}

	private void BindEntryCompletionSprites()
	{
		if (Engine.IsEditorHint() || _entryCompletionSpritesBound)
		{
			return;
		}
		for (int i = 0; i < _seatCrew.Length; i++)
		{
			if (GodotObject.IsInstanceValid(_seatCrew[i]))
			{
				_seatCrew[i].OnAnimeCompleted += OnEntryCrewCompleted;
			}
		}
		_entryCompletionSpritesBound = true;
	}

	private void UnbindEntryCompletionSprites()
	{
		if (!_entryCompletionSpritesBound)
		{
			return;
		}
		for (int i = 0; i < _seatCrew.Length; i++)
		{
			if (GodotObject.IsInstanceValid(_seatCrew[i]))
			{
				_seatCrew[i].OnAnimeCompleted -= OnEntryCrewCompleted;
			}
		}
		_entryCompletionSpritesBound = false;
	}

	private void OnEntryCrewCompleted(string clip)
	{
		if (!(clip != "Jump") && base.CurrentPhase == BobsledVehiclePhase.EnteringJump)
		{
			if (!CanCompleteEntryJumpPresentation())
			{
				_entryJumpCompletionPending = true;
			}
			else
			{
				CompleteAlignedEntryJump();
			}
		}
	}

	private void CompleteAlignedEntryJump()
	{
		ResetCrewPositions(_seatCrew);
		if (TowerDefenseManager.HasGameplayAuthority)
		{
			CompleteEntryJump();
		}
	}

	private bool CanCompleteEntryJumpPresentation()
	{
		if (!base.isPause && GodotObject.IsInstanceValid(base.sprite) && !base.sprite.pause)
		{
			return !Mathf.IsZeroApprox((float)base.sprite.timeScale);
		}
		return false;
	}
}
