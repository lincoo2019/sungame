using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public sealed class MagnetComponent : CharacterComponentRuntime
{
	public delegate void DrawTargetEventHandler(TowerDefenseCharacter target);

	public delegate void BreakDownEventHandler(TowerDefenseArmorInstance armor);

	public Marker2D posMarker;

	public float breakDownTime = 15f;

	public AabbShape2DResource checkShape;

	public bool scaleCheckShapeToMapGrid = true;

	public Vector2 checkRange = new Vector2(2.5f, 2.5f);

	public bool checkAll;

	public bool hostAuthoritative = true;

	public float magnetMoveSpeed = 10f;

	public float arriveDistance = 0.01f;

	public AdobeAnimateSprite sprite;

	public string drawEventName = "action";

	public string shootBeginAnimeClips = "Begin";

	public float shootBeginAnimeTimeScale = 1f;

	public float shootBeginAnimeStartPosition = 0.2f;

	public string shootAnimeClips = "Shooting";

	public float shootAnimeTimeScale = 1f;

	public float shootAnimeStartPosition;

	public string shootEndAnimeClips = "End";

	public float shootEndAnimeTimeScale = 1f;

	public float shootEndAnimeStartPosition = 0.2f;

	public string noActiveAnimeClips = "NonActiveIdle2";

	public float noActiveAnimeTimeScale = 1f;

	public float noActiveAnimeStartPosition = 0.2f;

	public string drawAudioName = "Magnet";

	public StringName beginStateEvent = "ToBegin";

	public StringName shootStateEvent = "ToShoot";

	public StringName noActiveStateEvent = "ToNoActive";

	public StringName endStateEvent = "ToEnd";

	public StringName idleStateEvent = "ToIdle";

	public TowerDefenseCharacter parent;

	private TowerDefenseArmorInstance _breakDownArmor;

	public float breakDownTimer;

	public TowerDefenseMagnet magnet;

	public TowerDefenseCharacter drawArmorCharacter;

	public TowerDefenseArmorInstance drawArmor;

	public bool isArrive;

	private StateHandle _idleState;

	private StateHandle _beginState;

	private StateHandle _shootState;

	private StateHandle _endState;

	private StateHandle _noActiveState;

	private bool _signalsConnected;

	private bool _stateSignalsConnected;

	private bool _waitingForParentReady;

	private bool _runtimeInitialized;

	private bool _animationConfigurationValidated;

	private bool _animationConfigurationValid;

	private string _validatedBeginClips = "";

	private string _validatedShootClips = "";

	private string _validatedEndClips = "";

	private string _validatedNoActiveClips = "";

	private string _pendingStateName = "";

	private bool _authoritativeHasBreakDownArmor;

	private readonly List<TowerDefenseArmorInstance> _armorCheckBuffer = new List<TowerDefenseArmorInstance>();

	private NodePath _posMarkerPath = new NodePath();

	private NodePath _spritePath = new NodePath();

	private bool _configured;

	protected override bool IsStateMachineDispatchEligible => ValidateAnimationConfiguration();

	internal override bool WantsPhysicsProcess => GodotObject.IsInstanceValid(breakDownArmor);

	public TowerDefenseArmorInstance breakDownArmor
	{
		get
		{
			return _breakDownArmor;
		}
		set
		{
			if (_breakDownArmor != value)
			{
				_breakDownArmor = value;
				RefreshPhysicsProcessEligibility();
			}
		}
	}

	private bool IsRemoteClient
	{
		get
		{
			if (hostAuthoritative && Global.IsMultiplayerMode)
			{
				return !MultiPlayerManager.IsHost;
			}
			return false;
		}
	}

	private MagnetComponentDefinition Definition => base.ComponentDefinition as MagnetComponentDefinition;

	public event DrawTargetEventHandler OnDrawTarget;

	public event BreakDownEventHandler OnBreakDown;

	protected override void OnBound()
	{
		parent = base.Owner;
		ConfigureOnce();
		ResolveOwnerReferences();
		ConnectSignals();
		if (GodotObject.IsInstanceValid(parent))
		{
			if (parent.IsNodeReady())
			{
				InitializeRuntime();
			}
			else
			{
				ConnectParentReady();
			}
		}
	}

	protected override void OnActivated()
	{
		ConnectSignals();
		if (GodotObject.IsInstanceValid(parent) && parent.IsNodeReady())
		{
			InitializeRuntime();
		}
		RestoreMagnetAttachmentVisuals();
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		DisconnectSignals();
		posMarker = null;
		sprite = null;
		parent = null;
	}

	protected override void OnReleased()
	{
		DisconnectSignals();
		DisconnectStateSignals();
		Destroy();
		this.OnDrawTarget = null;
		this.OnBreakDown = null;
		breakDownArmor = null;
		drawArmor = null;
		drawArmorCharacter = null;
		_armorCheckBuffer.Clear();
		posMarker = null;
		sprite = null;
		parent = null;
		checkShape = null;
	}

	private void ConfigureOnce()
	{
		if (!_configured && Definition != null)
		{
			MagnetComponentDefinition definition = Definition;
			checkShape = definition.checkShape;
			scaleCheckShapeToMapGrid = definition.scaleCheckShapeToMapGrid;
			checkRange = definition.checkRange;
			checkAll = definition.checkAll;
			hostAuthoritative = definition.hostAuthoritative;
			_posMarkerPath = definition.posMarkerPath;
			breakDownTime = definition.breakDownTime;
			magnetMoveSpeed = definition.magnetMoveSpeed;
			arriveDistance = definition.arriveDistance;
			_spritePath = definition.spritePath;
			drawEventName = definition.drawEventName;
			shootBeginAnimeClips = definition.shootBeginAnimeClips;
			shootBeginAnimeTimeScale = definition.shootBeginAnimeTimeScale;
			shootBeginAnimeStartPosition = definition.shootBeginAnimeStartPosition;
			shootAnimeClips = definition.shootAnimeClips;
			shootAnimeTimeScale = definition.shootAnimeTimeScale;
			shootAnimeStartPosition = definition.shootAnimeStartPosition;
			shootEndAnimeClips = definition.shootEndAnimeClips;
			shootEndAnimeTimeScale = definition.shootEndAnimeTimeScale;
			shootEndAnimeStartPosition = definition.shootEndAnimeStartPosition;
			noActiveAnimeClips = definition.noActiveAnimeClips;
			noActiveAnimeTimeScale = definition.noActiveAnimeTimeScale;
			noActiveAnimeStartPosition = definition.noActiveAnimeStartPosition;
			drawAudioName = definition.drawAudioName;
			beginStateEvent = definition.beginStateEvent;
			shootStateEvent = definition.shootStateEvent;
			noActiveStateEvent = definition.noActiveStateEvent;
			endStateEvent = definition.endStateEvent;
			idleStateEvent = definition.idleStateEvent;
			_configured = true;
		}
	}

	private void ResolveOwnerReferences()
	{
		if (GodotObject.IsInstanceValid(parent))
		{
			posMarker = (_posMarkerPath.IsEmpty ? null : parent.GetNodeOrNull<Marker2D>(_posMarkerPath));
			sprite = (_spritePath.IsEmpty ? null : parent.GetNodeOrNull<AdobeAnimateSprite>(_spritePath));
		}
	}

	public override void SetAlive(bool value)
	{
		base.SetAlive(value);
		ValidateAnimationConfiguration();
		RefreshStateMachineDispatchEligibility();
	}

	protected override void OnStateRuntimeAttached()
	{
		_idleState = base.StateMachine?.GetStateById("magnet.idle");
		_beginState = base.StateMachine?.GetStateById("magnet.begin");
		_shootState = base.StateMachine?.GetStateById("magnet.shoot");
		_endState = base.StateMachine?.GetStateById("magnet.end");
		_noActiveState = base.StateMachine?.GetStateById("magnet.no_active");
		ConnectStateSignals();
		if (_runtimeInitialized)
		{
			ApplyStateName(_pendingStateName);
		}
	}

	protected override void OnStateRuntimeRegistered()
	{
		if (_runtimeInitialized)
		{
			ApplyStateName(_pendingStateName);
		}
	}

	protected override void OnStateRuntimeDetaching()
	{
		DisconnectStateSignals();
		Destroy();
		_idleState = null;
		_beginState = null;
		_shootState = null;
		_endState = null;
		_noActiveState = null;
	}

	protected override void OnAuthoritativeStateRestorePreparing(bool remote)
	{
		_pendingStateName = "";
	}

	protected override void OnAuthoritativeStateRestored(StateMachineSnapshot snapshot, bool remote)
	{
		ReconcileAuthoritativeState(remote);
	}

	private void ReconcileAuthoritativeState(bool remote)
	{
		StateHandle stateHandle = base.StateMachine?.CurrentStateHandle;
		if (stateHandle == null || !stateHandle.IsValid)
		{
			return;
		}
		string stableId = stateHandle.StableId;
		bool flag = GodotObject.IsInstanceValid(breakDownArmor);
		bool flag2 = GodotObject.IsInstanceValid(magnet);
		if (remote)
		{
			if (_authoritativeHasBreakDownArmor && (!flag || !flag2))
			{
				DowngradeUnsafeAuthoritativeState();
				return;
			}
			if (!_authoritativeHasBreakDownArmor && (flag || flag2))
			{
				breakDownArmor = null;
				drawArmor = null;
				drawArmorCharacter = null;
				Destroy();
			}
		}
		switch (stableId)
		{
		case "magnet.begin":
			SetAuthoritativeAnimation(shootBeginAnimeClips, loop: false, shootBeginAnimeStartPosition, shootBeginAnimeTimeScale);
			break;
		case "magnet.shoot":
			SetAuthoritativeAnimation(shootAnimeClips, loop: false, shootAnimeStartPosition, shootAnimeTimeScale);
			break;
		case "magnet.end":
			SetAuthoritativeAnimation(shootEndAnimeClips, loop: false, shootEndAnimeStartPosition, shootEndAnimeTimeScale);
			break;
		case "magnet.no_active":
			if (!flag || !flag2)
			{
				DowngradeUnsafeAuthoritativeState();
				break;
			}
			RestoreMagnetAttachmentVisuals();
			SetAuthoritativeAnimation(noActiveAnimeClips, loop: true, noActiveAnimeStartPosition, noActiveAnimeTimeScale);
			break;
		}
	}

	private void DowngradeUnsafeAuthoritativeState()
	{
		breakDownArmor = null;
		drawArmor = null;
		drawArmorCharacter = null;
		breakDownTimer = 0f;
		Destroy();
		RestoreStateSilently("magnet.idle");
	}

	private void RestoreMagnetAttachmentVisuals()
	{
		if (GodotObject.IsInstanceValid(magnet) && GodotObject.IsInstanceValid(parent))
		{
			magnet.adsorbedObject = parent;
			magnet.gridPos = parent.gridPos;
			if (isArrive && GodotObject.IsInstanceValid(posMarker))
			{
				magnet.GlobalPosition = parent.GetLogicalGlobalPosition(posMarker);
			}
			float num = Mathf.Max(0.0001f, breakDownTime);
			magnet.Scale = Vector2.One * Mathf.Clamp(breakDownTimer / num, 0f, 1f);
		}
	}

	private void SetAuthoritativeAnimation(string clip, bool loop, float startPosition, float timeScale)
	{
		if (CanPlayAnimation(clip))
		{
			sprite.SetAnimation(clip, loop, Math.Max(0f, startPosition));
			ApplyAnimationTimeScale(timeScale);
		}
	}

	private void RestoreStateSilently(string stableId)
	{
		StateHandle stateHandle = base.StateMachine?.GetStateById(stableId);
		StateMachineSnapshot stateMachineSnapshot = base.StateMachine?.CaptureSnapshot();
		if (stateHandle != null && stateHandle.IsValid && stateMachineSnapshot != null)
		{
			stateMachineSnapshot.ActiveStateIds.Clear();
			stateMachineSnapshot.ActiveStateIds.Add(stateHandle.StableId);
			stateMachineSnapshot.PendingTransitionIds.Clear();
			stateMachineSnapshot.PendingDelayRemaining.Clear();
			base.StateMachine.RestoreSnapshot(stateMachineSnapshot, suppressEntryEffects: true);
		}
	}

	private void ConnectParentReady()
	{
		if (!_waitingForParentReady && GodotObject.IsInstanceValid(parent))
		{
			parent.Ready += OnParentReady;
			_waitingForParentReady = true;
		}
	}

	private void DisconnectParentReady()
	{
		if (_waitingForParentReady && GodotObject.IsInstanceValid(parent))
		{
			parent.Ready -= OnParentReady;
		}
		_waitingForParentReady = false;
	}

	private void OnParentReady()
	{
		DisconnectParentReady();
		InitializeRuntime();
	}

	private void InitializeRuntime()
	{
		if (!_runtimeInitialized && GodotObject.IsInstanceValid(parent))
		{
			_runtimeInitialized = true;
			RefreshConfiguration();
			ApplyStateName(_pendingStateName);
		}
	}

	private void ConnectSignals()
	{
		if (!_signalsConnected)
		{
			if (GodotObject.IsInstanceValid(sprite))
			{
				sprite.OnAnimeCompleted += AnimeCompleted;
				sprite.OnAnimeEvent += AnimeEvent;
			}
			_signalsConnected = true;
		}
	}

	private void DisconnectSignals()
	{
		DisconnectParentReady();
		if (GodotObject.IsInstanceValid(sprite))
		{
			sprite.OnAnimeCompleted -= AnimeCompleted;
			sprite.OnAnimeEvent -= AnimeEvent;
		}
		_signalsConnected = false;
	}

	private void ConnectStateSignals()
	{
		if (!_stateSignalsConnected)
		{
			ConnectState(_idleState, IdleEntered, IdleExited, IdleProcessing);
			ConnectState(_beginState, BeginEntered, BeginExited, BeginProcessing);
			ConnectState(_shootState, ShootEntered, ShootExited, ShootProcessing);
			ConnectState(_endState, EndEntered, EndExited, EndProcessing);
			ConnectState(_noActiveState, NoActiveEntered, NoActiveExited, NoActiveProcessing);
			_stateSignalsConnected = true;
		}
	}

	private void DisconnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			DisconnectState(_idleState, IdleEntered, IdleExited, IdleProcessing);
			DisconnectState(_beginState, BeginEntered, BeginExited, BeginProcessing);
			DisconnectState(_shootState, ShootEntered, ShootExited, ShootProcessing);
			DisconnectState(_endState, EndEntered, EndExited, EndProcessing);
			DisconnectState(_noActiveState, NoActiveEntered, NoActiveExited, NoActiveProcessing);
			_stateSignalsConnected = false;
		}
	}

	private static void ConnectState(StateHandle handle, Action entered, Action exited, Action<double> processing)
	{
		if (handle != null && handle.IsValid)
		{
			handle.Entered += entered;
			handle.Exited += exited;
			handle.PhysicsProcessing += processing;
		}
	}

	private static void DisconnectState(StateHandle handle, Action entered, Action exited, Action<double> processing)
	{
		if (handle != null)
		{
			handle.Entered -= entered;
			handle.Exited -= exited;
			handle.PhysicsProcessing -= processing;
		}
	}

	public void RefreshConfiguration()
	{
		_animationConfigurationValidated = false;
		ValidateAnimationConfiguration();
	}

	private bool ValidateAnimationConfiguration()
	{
		if (_animationConfigurationValidated && _validatedBeginClips == shootBeginAnimeClips && _validatedShootClips == shootAnimeClips && _validatedEndClips == shootEndAnimeClips && _validatedNoActiveClips == noActiveAnimeClips)
		{
			return _animationConfigurationValid;
		}
		_validatedBeginClips = shootBeginAnimeClips;
		_validatedShootClips = shootAnimeClips;
		_validatedEndClips = shootEndAnimeClips;
		_validatedNoActiveClips = noActiveAnimeClips;
		_animationConfigurationValidated = true;
		_animationConfigurationValid = (string.IsNullOrEmpty(shootBeginAnimeClips) || CanPlayAnimation(shootBeginAnimeClips)) && CanPlayAnimation(shootAnimeClips) && (string.IsNullOrEmpty(shootEndAnimeClips) || CanPlayAnimation(shootEndAnimeClips)) && CanPlayAnimation(noActiveAnimeClips);
		return _animationConfigurationValid;
	}

	private bool CanPlayAnimation(string clipName)
	{
		if (!GodotObject.IsInstanceValid(sprite) || !GodotObject.IsInstanceValid(sprite.flashAnimeData) || string.IsNullOrEmpty(clipName))
		{
			return false;
		}
		string[] array = clipName.Split('&', StringSplitOptions.RemoveEmptyEntries);
		if (array.Length == 0)
		{
			return false;
		}
		for (int i = 0; i < array.Length; i++)
		{
			if (!sprite.HasClip(array[i]))
			{
				return false;
			}
		}
		return true;
	}

	private void ReturnToIdle()
	{
		if (GodotObject.IsInstanceValid(parent) && parent.componentRunning)
		{
			parent.Idle();
		}
		SendStateEvent(idleStateEvent);
	}

	internal override void PhysicsProcess(double delta, ulong physicsFrame)
	{
		PhysicsProcessValidated((float)delta);
	}

	private void PhysicsProcessValidated(float delta)
	{
		if (!base.Alive || IsRemoteClient || !GodotObject.IsInstanceValid(parent) || !GodotObject.IsInstanceValid(breakDownArmor))
		{
			return;
		}
		if (GodotObject.IsInstanceValid(magnet))
		{
			magnet.gridPos = parent.gridPos;
			if (GodotObject.IsInstanceValid(posMarker))
			{
				Vector2 logicalGlobalPosition = parent.GetLogicalGlobalPosition(posMarker);
				float num = Math.Max(0f, arriveDistance);
				if (!isArrive && magnet.GlobalPosition.DistanceSquaredTo(logicalGlobalPosition) > num * num)
				{
					float weight = Mathf.Clamp(magnetMoveSpeed * delta, 0f, 1f);
					magnet.GlobalPosition = magnet.GlobalPosition.Lerp(logicalGlobalPosition, weight);
				}
				else
				{
					isArrive = true;
					magnet.GlobalPosition = logicalGlobalPosition;
				}
			}
		}
		ArmorBreakDown(delta);
	}

	private List<TowerDefenseCharacter> GetCharactersInCheckRange()
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance) || instance.characterRegistry == null || !TryGetCheckRect(out var checkRect))
		{
			return null;
		}
		return instance.characterRegistry.GetCharactersIntersectingRectListExcludingCamp(checkRect, parent.camp);
	}

	private bool TryGetCheckRect(out Rect2 checkRect)
	{
		checkRect = default(Rect2);
		if (!GodotObject.IsInstanceValid(parent) || !GodotObject.IsInstanceValid(checkShape) || !checkShape.Enabled)
		{
			return false;
		}
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		Transform2D globalTransformForPhysicsFrame = parent.GetGlobalTransformForPhysicsFrame(TowerDefenseProcessModeDispatch.CurrentPhysicsFrame);
		if (!scaleCheckShapeToMapGrid || !GodotObject.IsInstanceValid(instance))
		{
			return checkShape.TryGetWorldRect(globalTransformForPhysicsFrame, out checkRect);
		}
		Vector2 vector = new Vector2(Mathf.Abs(checkRange.X), Mathf.Abs(checkRange.Y));
		Vector2 size = instance.GetMapGridSize() * 2f * vector;
		if (size.X <= 0f || size.Y <= 0f)
		{
			return false;
		}
		Vector2 mapCellPosCenter = instance.GetMapCellPosCenter(instance.GetMapGridPos(globalTransformForPhysicsFrame.Origin));
		Vector2 origin = checkShape.LocalTransform.Origin;
		mapCellPosCenter += globalTransformForPhysicsFrame.X * origin.X + globalTransformForPhysicsFrame.Y * origin.Y;
		checkRect = AabbShapeUtil.RectFromCenter(mapCellPosCenter, size);
		return true;
	}

	private bool IsEligibleCharacter(TowerDefenseCharacter character, bool enforceRangeAndCamp)
	{
		if (!GodotObject.IsInstanceValid(character) || character == parent || !GodotObject.IsInstanceValid(character.instance) || !character.instance.canBeCollection)
		{
			return false;
		}
		if (!enforceRangeAndCamp)
		{
			return true;
		}
		if (character.camp == parent.camp)
		{
			return false;
		}
		Vector2I vector2I = (character.gridPos - parent.gridPos).Abs();
		if ((float)vector2I.X <= Math.Abs(checkRange.X))
		{
			return (float)vector2I.Y <= Math.Abs(checkRange.Y);
		}
		return false;
	}

	private bool TryGetFirstMetalArmor(TowerDefenseCharacter character, out TowerDefenseArmorInstance armor)
	{
		armor = null;
		_armorCheckBuffer.Clear();
		_armorCheckBuffer.AddRange(character.GetArmorHeadCover());
		_armorCheckBuffer.AddRange(character.GetArmorShield());
		_armorCheckBuffer.AddRange(character.GetArmorHelment());
		_armorCheckBuffer.AddRange(character.GetArmor());
		for (int i = 0; i < _armorCheckBuffer.Count; i++)
		{
			TowerDefenseArmorInstance towerDefenseArmorInstance = _armorCheckBuffer[i];
			if (GodotObject.IsInstanceValid(towerDefenseArmorInstance) && !towerDefenseArmorInstance.isRemove && towerDefenseArmorInstance.IsMetallic())
			{
				armor = towerDefenseArmorInstance;
				return true;
			}
		}
		return false;
	}

	private void ClearSelectedArmor()
	{
		drawArmorCharacter = null;
		drawArmor = null;
	}

	private bool TrySelectCharacterArmor(TowerDefenseCharacter character, bool enforceRangeAndCamp)
	{
		if (!IsEligibleCharacter(character, enforceRangeAndCamp) || !TryGetFirstMetalArmor(character, out var armor))
		{
			return false;
		}
		drawArmorCharacter = character;
		drawArmor = armor;
		return true;
	}

	private bool CanArmorDrawImmediate()
	{
		ClearSelectedArmor();
		if (!GodotObject.IsInstanceValid(parent))
		{
			return false;
		}
		if (GodotObject.IsInstanceValid(breakDownArmor))
		{
			return false;
		}
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			return false;
		}
		if (checkAll)
		{
			foreach (Variant item in instance.GetCampTarget(parent.camp))
			{
				if (TrySelectCharacterArmor(item.As<TowerDefenseCharacter>(), enforceRangeAndCamp: false))
				{
					return true;
				}
			}
			return false;
		}
		List<TowerDefenseCharacter> charactersInCheckRange = GetCharactersInCheckRange();
		if (charactersInCheckRange == null)
		{
			return false;
		}
		for (int i = 0; i < charactersInCheckRange.Count; i++)
		{
			if (TrySelectCharacterArmor(charactersInCheckRange[i], enforceRangeAndCamp: true))
			{
				return true;
			}
		}
		return false;
	}

	public Task<bool> CanArmorDraw()
	{
		return Task.FromResult(CanArmorDrawImmediate());
	}

	public void FillCanArmorDrawCharacterList(List<TowerDefenseCharacter> output)
	{
		if (output == null)
		{
			return;
		}
		output.Clear();
		if (!GodotObject.IsInstanceValid(parent))
		{
			return;
		}
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			return;
		}
		TowerDefenseArmorInstance armor;
		if (checkAll)
		{
			foreach (Variant item in instance.GetCampTarget(parent.camp))
			{
				TowerDefenseCharacter towerDefenseCharacter = item.As<TowerDefenseCharacter>();
				if (IsEligibleCharacter(towerDefenseCharacter, enforceRangeAndCamp: false) && TryGetFirstMetalArmor(towerDefenseCharacter, out armor))
				{
					output.Add(towerDefenseCharacter);
				}
			}
			return;
		}
		List<TowerDefenseCharacter> charactersInCheckRange = GetCharactersInCheckRange();
		if (charactersInCheckRange == null)
		{
			return;
		}
		for (int i = 0; i < charactersInCheckRange.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter2 = charactersInCheckRange[i];
			if (IsEligibleCharacter(towerDefenseCharacter2, enforceRangeAndCamp: true) && TryGetFirstMetalArmor(towerDefenseCharacter2, out armor))
			{
				output.Add(towerDefenseCharacter2);
			}
		}
	}

	public Task<List<TowerDefenseCharacter>> GetCanArmorDrawCharacterList()
	{
		List<TowerDefenseCharacter> list = new List<TowerDefenseCharacter>();
		FillCanArmorDrawCharacterList(list);
		return Task.FromResult(list);
	}

	private bool TryFindNearestArmor()
	{
		ClearSelectedArmor();
		if (!GodotObject.IsInstanceValid(parent))
		{
			return false;
		}
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			return false;
		}
		float nearestDistanceSquared = 3.4028235E+38f;
		if (checkAll)
		{
			foreach (Variant item in instance.GetCampTarget(parent.camp))
			{
				TowerDefenseCharacter character = item.As<TowerDefenseCharacter>();
				TryUpdateNearestArmor(character, enforceRangeAndCamp: false, ref nearestDistanceSquared);
			}
		}
		else
		{
			List<TowerDefenseCharacter> charactersInCheckRange = GetCharactersInCheckRange();
			if (charactersInCheckRange == null)
			{
				return false;
			}
			for (int i = 0; i < charactersInCheckRange.Count; i++)
			{
				TryUpdateNearestArmor(charactersInCheckRange[i], enforceRangeAndCamp: true, ref nearestDistanceSquared);
			}
		}
		if (GodotObject.IsInstanceValid(drawArmorCharacter))
		{
			return GodotObject.IsInstanceValid(drawArmor);
		}
		return false;
	}

	private void TryUpdateNearestArmor(TowerDefenseCharacter character, bool enforceRangeAndCamp, ref float nearestDistanceSquared)
	{
		if (IsEligibleCharacter(character, enforceRangeAndCamp))
		{
			float num = parent.GetLogicalGlobalPosition().DistanceSquaredTo(character.GetLogicalGlobalPosition());
			if (!(num >= nearestDistanceSquared) && TryGetFirstMetalArmor(character, out var armor))
			{
				nearestDistanceSquared = num;
				drawArmorCharacter = character;
				drawArmor = armor;
			}
		}
	}

	public TowerDefenseArmorInstance ArmorDraw()
	{
		if (!AttachSelectedArmor(notifyTarget: false))
		{
			return null;
		}
		return breakDownArmor;
	}

	public void ArmorDrawNear()
	{
		if (TryFindNearestArmor())
		{
			AttachSelectedArmor(notifyTarget: true);
		}
	}

	private bool AttachSelectedArmor(bool notifyTarget)
	{
		if (!GodotObject.IsInstanceValid(parent) || !GodotObject.IsInstanceValid(drawArmorCharacter) || !GodotObject.IsInstanceValid(drawArmor))
		{
			return false;
		}
		TowerDefenseCharacter towerDefenseCharacter = drawArmorCharacter;
		TowerDefenseArmorInstance armor = drawArmor;
		TowerDefenseMagnet instance = towerDefenseCharacter.ArmorDraw(armor);
		if (!GodotObject.IsInstanceValid(instance))
		{
			breakDownArmor = null;
			return false;
		}
		breakDownArmor = armor;
		magnet = instance;
		isArrive = false;
		breakDownTimer = Math.Max(0f, breakDownTime);
		magnet.adsorbedObject = parent;
		magnet.gridPos = parent.gridPos;
		if (GodotObject.IsInstanceValid(towerDefenseCharacter.sprite))
		{
			towerDefenseCharacter.sprite.QueueRedraw();
		}
		PlayAudio(drawAudioName);
		if (notifyTarget)
		{
			this.OnDrawTarget?.Invoke(towerDefenseCharacter);
		}
		return true;
	}

	public void ArmorBreakDown(float delta)
	{
		if (!GodotObject.IsInstanceValid(breakDownArmor))
		{
			return;
		}
		if (breakDownTimer <= 0f || breakDownTime <= 0f)
		{
			BreakDownOver();
			return;
		}
		float num = (GodotObject.IsInstanceValid(parent) ? ((float)Math.Max(0.0, parent.timeScale)) : 1f);
		breakDownTimer = Math.Max(0f, breakDownTimer - Math.Max(0f, delta) * num);
		if (GodotObject.IsInstanceValid(magnet))
		{
			float num2 = Mathf.Max(0.0001f, breakDownTime);
			magnet.Scale = Vector2.One * Mathf.Clamp(breakDownTimer / num2, 0f, 1f);
		}
		if (breakDownTimer <= 0f)
		{
			BreakDownOver();
		}
	}

	public void BreakDownOver()
	{
		TowerDefenseArmorInstance towerDefenseArmorInstance = breakDownArmor;
		if (GodotObject.IsInstanceValid(towerDefenseArmorInstance))
		{
			BreakDownInternal(towerDefenseArmorInstance);
			this.OnBreakDown?.Invoke(towerDefenseArmorInstance);
		}
		breakDownArmor = null;
		drawArmor = null;
		drawArmorCharacter = null;
		breakDownTimer = 0f;
		isArrive = false;
		Destroy();
	}

	public void Destroy()
	{
		if (GodotObject.IsInstanceValid(magnet))
		{
			magnet.QueueFree();
		}
		magnet = null;
		isArrive = false;
	}

	public void IdleEntered()
	{
		if (GodotObject.IsInstanceValid(parent) && parent.componentRunning)
		{
			parent.Idle();
		}
	}

	public void IdleProcessing(double delta)
	{
		if (!base.Alive || IsRemoteClient || !ValidateAnimationConfiguration() || !GodotObject.IsInstanceValid(parent))
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine != null && stateMachine.IsInitialized)
		{
			TowerDefenseManager instance = TowerDefenseManager.Instance;
			if (GodotObject.IsInstanceValid(instance) && instance.IsGameRunning() && parent.inGame && parent.componentAlive && !parent.componentRunning && CanArmorDrawImmediate())
			{
				parent.Component();
				SendStateEvent(beginStateEvent);
			}
		}
	}

	public void IdleExited()
	{
	}

	public void BeginEntered()
	{
		if (string.IsNullOrEmpty(shootBeginAnimeClips))
		{
			SendStateEvent(shootStateEvent);
		}
		else if (!CanPlayAnimation(shootBeginAnimeClips))
		{
			ReturnToIdle();
		}
		else
		{
			sprite.SetAnimation(shootBeginAnimeClips, loop: false, Math.Max(0f, shootBeginAnimeStartPosition));
		}
	}

	public void BeginProcessing(double delta)
	{
		if (CanProcessStateCallbacks())
		{
			ApplyAnimationTimeScale(shootBeginAnimeTimeScale);
		}
	}

	public void BeginExited()
	{
	}

	public void ShootEntered()
	{
		if (!CanPlayAnimation(shootAnimeClips))
		{
			ReturnToIdle();
		}
		else
		{
			sprite.SetAnimation(shootAnimeClips, loop: false, Math.Max(0f, shootAnimeStartPosition));
		}
	}

	public void ShootProcessing(double delta)
	{
		if (CanProcessStateCallbacks())
		{
			ApplyAnimationTimeScale(shootAnimeTimeScale);
		}
	}

	public void ShootExited()
	{
	}

	public void EndEntered()
	{
		if (!CanPlayAnimation(shootEndAnimeClips))
		{
			ReturnToIdle();
		}
		else
		{
			sprite.SetAnimation(shootEndAnimeClips, loop: false, Math.Max(0f, shootEndAnimeStartPosition));
		}
	}

	public void EndProcessing(double delta)
	{
		if (CanProcessStateCallbacks())
		{
			ApplyAnimationTimeScale(shootEndAnimeTimeScale);
		}
	}

	public void EndExited()
	{
	}

	public void NoActiveEntered()
	{
		if (!CanPlayAnimation(noActiveAnimeClips))
		{
			ReturnToIdle();
		}
		else
		{
			sprite.SetAnimation(noActiveAnimeClips, loop: true, Math.Max(0f, noActiveAnimeStartPosition));
		}
	}

	public void NoActiveProcessing(double delta)
	{
		if (CanProcessStateCallbacks())
		{
			ApplyAnimationTimeScale(noActiveAnimeTimeScale);
		}
	}

	public void NoActiveExited()
	{
	}

	private void ApplyAnimationTimeScale(float multiplier)
	{
		if (!GodotObject.IsInstanceValid(sprite))
		{
			ReturnToIdle();
			return;
		}
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		bool flag = GodotObject.IsInstanceValid(instance) && instance.IsIZMMode();
		sprite.timeScale = ((flag || !GodotObject.IsInstanceValid(parent)) ? ((double)multiplier) : (parent.timeScale * (double)multiplier));
	}

	private bool CanProcessStateCallbacks()
	{
		if (base.Alive)
		{
			return ValidateAnimationConfiguration();
		}
		return false;
	}

	public void AnimeEvent(string command, Variant argument)
	{
		if (base.Alive && !IsRemoteClient && !(command != drawEventName))
		{
			ArmorDrawNear();
		}
	}

	public void AnimeCompleted(string clip)
	{
		if (!base.Alive)
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine != null && stateMachine.IsInitialized)
		{
			if (clip == shootBeginAnimeClips)
			{
				SendStateEvent(shootStateEvent);
			}
			else if (clip == shootAnimeClips)
			{
				SendStateEvent(GodotObject.IsInstanceValid(breakDownArmor) ? noActiveStateEvent : endStateEvent);
			}
			else if (clip == shootEndAnimeClips)
			{
				SendStateEvent(idleStateEvent);
			}
		}
	}

	public void BreakDownInternal(TowerDefenseArmorInstance armor)
	{
		SendStateEvent(endStateEvent);
	}

	public override Dictionary ExportComponentSave()
	{
		Dictionary dictionary = new Dictionary
		{
			{ "breakDownTimer", breakDownTimer },
			{ "isArrive", isArrive },
			{ "alive", base.Alive }
		};
		if (GodotObject.IsInstanceValid(breakDownArmor) && GodotObject.IsInstanceValid(drawArmorCharacter))
		{
			dictionary["breakDownArmorSave"] = breakDownArmor.ExportSave();
			dictionary["breakDownTargetNodeName"] = drawArmorCharacter.Name.ToString().ValidateNodeName();
		}
		string activeStateName = GetActiveStateName();
		if (!string.IsNullOrEmpty(activeStateName))
		{
			dictionary["state"] = activeStateName;
		}
		return dictionary;
	}

	public override void ImportComponentSave(Dictionary data, TowerDefenseLevelSaveConfigCSharp owner)
	{
		SetAlive(data.GetValueOrDefault("alive", base.Alive).AsBool());
		if (TryRestoreBreakDown(data, owner))
		{
			_pendingStateName = "";
			return;
		}
		ResetTransientStateForRestore();
		ForceResetToIdleAfterRestore();
	}

	private bool TryRestoreBreakDown(Dictionary data, TowerDefenseLevelSaveConfigCSharp owner)
	{
		if (owner == null || !GodotObject.IsInstanceValid(parent) || !data.ContainsKey("breakDownArmorSave") || !data.ContainsKey("breakDownTargetNodeName"))
		{
			return false;
		}
		string text = data["breakDownArmorSave"].AsGodotDictionary().GetValueOrDefault("armorName", "").AsString();
		string text2 = data["breakDownTargetNodeName"].AsString();
		if (text == "" || text2 == "" || !owner.charcterDicionary.ContainsKey(text2))
		{
			return false;
		}
		TowerDefenseCharacter towerDefenseCharacter = owner.charcterDicionary[text2];
		if (!GodotObject.IsInstanceValid(towerDefenseCharacter) || !GodotObject.IsInstanceValid(towerDefenseCharacter.instance))
		{
			return false;
		}
		TowerDefenseArmorInstance armorFromName = towerDefenseCharacter.GetArmorFromName(text);
		if (!GodotObject.IsInstanceValid(armorFromName) || armorFromName.isRemove)
		{
			towerDefenseCharacter.instance.ArmorAdd(text);
			armorFromName = towerDefenseCharacter.GetArmorFromName(text);
		}
		if (!GodotObject.IsInstanceValid(armorFromName) || armorFromName.isRemove)
		{
			return false;
		}
		if (data.ContainsKey("breakDownArmorSave"))
		{
			armorFromName.ImportSave(data["breakDownArmorSave"].AsGodotDictionary());
		}
		drawArmorCharacter = towerDefenseCharacter;
		drawArmor = armorFromName;
		TowerDefenseMagnet instance = towerDefenseCharacter.ArmorDraw(armorFromName);
		if (!GodotObject.IsInstanceValid(instance))
		{
			drawArmorCharacter = null;
			drawArmor = null;
			return false;
		}
		breakDownArmor = armorFromName;
		magnet = instance;
		isArrive = data.GetValueOrDefault("isArrive", true).AsBool();
		breakDownTimer = Math.Max(0f, data.GetValueOrDefault("breakDownTimer", breakDownTime).AsSingle());
		magnet.adsorbedObject = parent;
		magnet.gridPos = parent.gridPos;
		magnet.GlobalPosition = (GodotObject.IsInstanceValid(posMarker) ? parent.GetLogicalGlobalPosition(posMarker) : parent.GetLogicalGlobalPosition());
		return true;
	}

	private void ResetTransientStateForRestore()
	{
		breakDownArmor = null;
		drawArmor = null;
		drawArmorCharacter = null;
		breakDownTimer = 0f;
		isArrive = false;
		Destroy();
	}

	private void ForceResetToIdleAfterRestore()
	{
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine != null && stateMachine.IsInitialized)
		{
			SyncForceState(base.StateMachine, "magnet.idle");
		}
		else
		{
			_pendingStateName = "magnet.idle";
		}
		if (base.Alive)
		{
			Callable.From(CompleteRestoreResetToIdle).CallDeferred();
		}
	}

	private void CompleteRestoreResetToIdle()
	{
		if (!base.Alive || base.IsReleased || !GodotObject.IsInstanceValid(parent))
		{
			return;
		}
		ResetTransientStateForRestore();
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine != null && stateMachine.IsInitialized)
		{
			SyncForceState(base.StateMachine, "magnet.idle");
			if (parent.componentRunning)
			{
				parent.Idle();
			}
		}
		else
		{
			_pendingStateName = "magnet.idle";
		}
	}

	public override Dictionary SyncSerialize()
	{
		Dictionary dictionary = ExportComponentSave();
		dictionary["hasBreakDownArmor"] = GodotObject.IsInstanceValid(breakDownArmor);
		return dictionary;
	}

	public override void SyncDeserialize(Dictionary data)
	{
		breakDownTimer = Math.Max(0f, data.GetValueOrDefault("breakDownTimer", breakDownTimer).AsSingle());
		isArrive = data.GetValueOrDefault("isArrive", isArrive).AsBool();
		SetAlive(data.GetValueOrDefault("alive", base.Alive).AsBool());
		if (data.ContainsKey("hasBreakDownArmor"))
		{
			_authoritativeHasBreakDownArmor = data["hasBreakDownArmor"].AsBool();
			if (!_authoritativeHasBreakDownArmor)
			{
				breakDownArmor = null;
				drawArmor = null;
				drawArmorCharacter = null;
				Destroy();
			}
		}
		if (base.ShouldApplyLegacyStateField)
		{
			ApplyStateName(data.GetValueOrDefault("state", "").AsString());
		}
	}

	private void ApplyStateName(string stateName)
	{
		if (string.IsNullOrEmpty(stateName))
		{
			return;
		}
		if (base.IsStateMachineRegistered)
		{
			IStateMachineController stateMachine = base.StateMachine;
			if (stateMachine != null && stateMachine.CurrentStateHandle?.IsValid == true)
			{
				if (SyncForceState(base.StateMachine, stateName))
				{
					_pendingStateName = "";
				}
				else
				{
					_pendingStateName = stateName;
				}
				return;
			}
		}
		_pendingStateName = stateName;
	}

	private string GetActiveStateName()
	{
		StateHandle stateHandle = base.StateMachine?.CurrentStateHandle;
		if (stateHandle == null || !stateHandle.IsValid)
		{
			return "";
		}
		return stateHandle.StableId.ToString();
	}

	public void ForceResetToIdle()
	{
		if (base.Alive)
		{
			IStateMachineController stateMachine = base.StateMachine;
			if (stateMachine != null && stateMachine.IsInitialized)
			{
				SyncForceState(base.StateMachine, "magnet.idle");
			}
		}
	}

	private static bool SyncForceState(IStateMachineController state, string targetState)
	{
		if (state == null || !state.IsInitialized || string.IsNullOrEmpty(targetState))
		{
			return false;
		}
		StateHandle currentStateHandle = state.CurrentStateHandle;
		StateHandle stateHandle = state.ResolveState(targetState, allowDisplayNameFallback: true);
		if (currentStateHandle == null || !currentStateHandle.IsValid || stateHandle == null || !stateHandle.IsValid)
		{
			return false;
		}
		if (currentStateHandle.StableId == stateHandle.StableId)
		{
			return true;
		}
		StateMachineSnapshot stateMachineSnapshot = state.CaptureSnapshot();
		if (stateMachineSnapshot == null)
		{
			return false;
		}
		stateMachineSnapshot.ActiveStateIds.Clear();
		stateMachineSnapshot.ActiveStateIds.Add(stateHandle.StableId);
		stateMachineSnapshot.PendingTransitionIds.Clear();
		stateMachineSnapshot.PendingDelayRemaining.Clear();
		return state.RestoreSnapshot(stateMachineSnapshot);
	}

	private static void PlayAudio(string audioName)
	{
		if (!string.IsNullOrEmpty(audioName) && GodotObject.IsInstanceValid(AudioManager.Instance))
		{
			AudioManager.Instance.AudioPlay(audioName);
		}
	}
}
