using Godot;
using Godot.Collections;

public sealed class GroundMoveComponent : CharacterComponentRuntime
{
	public Node2D groundNode;

	public StringName groundLayerName;

	public Vector2 groundLayerOffset = Vector2.Zero;

	public NodePath groundSlotPath = new NodePath("GroundSlot");

	public bool groundPosInit;

	public Vector2 groundPosSave = Vector2.Zero;

	public float delay = 0.2f;

	public bool moveYAxis;

	public bool syncNetworkPosition = true;

	public float networkSnapDistance = 32f;

	public float networkCorrectionWeight = 0.35f;

	public TowerDefenseCharacter parent;

	private AdobeAnimateSlot _groundSlot;

	private AdobeAnimateSprite _signalSprite;

	private int _groundLayerId = -1;

	private bool _usingGroundLayerSource;

	private int _groundSlotPositionVersion = -1;

	private Vector2 _moveScale = Vector2.One;

	private Vector2 _moveDelta = Vector2.Zero;

	private Vector2 _parentSpaceScale = Vector2.One;

	private Vector2 _groundSourceWorldOriginOffset = Vector2.Zero;

	private bool _moveTransformCached;

	private bool _signalsConnected;

	private bool _configured;

	private bool _managedPoseCached;

	private ulong _managedPoseRevision;

	private int _managedPoseFrameIndex;

	private double _managedPoseElapsedTimer;

	private bool _managedPosePlayBack;

	private bool _managedPoseLoop;

	private Vector2 _managedPoseSpriteOffset;

	private Vector2 _managedPoseSlotOffset;

	private int _managedPoseFollowSlotId;

	private int _managedPoseSlotMode;

	private Vector2 _managedPosePosition;

	protected override bool AllowPhysicsOutsideComponentBattlefield => true;

	private Dictionary _syncPayload => GetReusableSyncPayload();

	private GroundMoveComponentDefinition Definition => base.ComponentDefinition as GroundMoveComponentDefinition;

	public bool HasMovementSource
	{
		get
		{
			if (!_usingGroundLayerSource || _groundLayerId < 0)
			{
				return GodotObject.IsInstanceValid(groundNode);
			}
			return true;
		}
	}

	public bool UsesGroundLayerSource
	{
		get
		{
			if (_usingGroundLayerSource)
			{
				return _groundLayerId >= 0;
			}
			return false;
		}
	}

	public int GroundLayerId
	{
		get
		{
			if (!UsesGroundLayerSource)
			{
				return -1;
			}
			return _groundLayerId;
		}
	}

	internal override bool WantsPhysicsProcess => HasMovementSource;

	protected override void OnBound()
	{
		parent = base.Owner;
		if (!_configured)
		{
			GroundMoveComponentDefinition definition = Definition;
			groundLayerName = definition?.groundLayerName ?? null;
			groundLayerOffset = definition?.groundLayerOffset ?? Vector2.Zero;
			groundSlotPath = definition?.groundSlotPath ?? new NodePath("GroundSlot");
			delay = definition?.delay ?? 0.2f;
			moveYAxis = definition?.moveYAxis ?? false;
			syncNetworkPosition = definition?.syncNetworkPosition ?? true;
			networkSnapDistance = definition?.networkSnapDistance ?? 32f;
			networkCorrectionWeight = definition?.networkCorrectionWeight ?? 0.35f;
			_configured = true;
		}
		ResolveMovementSource();
	}

	protected override void OnActivated()
	{
		ResolveMovementSource();
		ConnectGroundNodeSignals();
		RefreshPhysicsProcessEligibility();
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		CleanupOwnerReferences();
	}

	protected override void OnReleased()
	{
		CleanupOwnerReferences();
	}

	private void CleanupOwnerReferences()
	{
		DisconnectGroundNodeSignals();
		ResetGroundTracking();
		_groundSlot = null;
		_signalSprite = null;
		_groundLayerId = -1;
		_usingGroundLayerSource = false;
		_moveTransformCached = false;
		groundNode = null;
		parent = null;
	}

	public override void SetAlive(bool value)
	{
		if (value)
		{
			ResolveMovementSource();
			if (IsParentMovementTerminated() || !HasMovementSource)
			{
				value = false;
			}
		}
		bool alive = base.Alive;
		base.SetAlive(value);
		if (value && !alive)
		{
			ResetGroundTracking(refreshScale: true);
		}
		else if (!value && alive)
		{
			ResetGroundTracking();
		}
	}

	public bool ResolveMovementSource()
	{
		if (TryResolveGroundLayerSource())
		{
			return true;
		}
		if (GodotObject.IsInstanceValid(groundNode))
		{
			_groundSlot = groundNode as AdobeAnimateSlot;
			return true;
		}
		if (!GodotObject.IsInstanceValid(parent) || !GodotObject.IsInstanceValid(parent.sprite) || groundSlotPath.IsEmpty)
		{
			return false;
		}
		Node2D nodeOrNull = parent.sprite.GetNodeOrNull<Node2D>(groundSlotPath);
		if (!GodotObject.IsInstanceValid(nodeOrNull))
		{
			return false;
		}
		groundNode = nodeOrNull;
		_groundSlot = nodeOrNull as AdobeAnimateSlot;
		ConnectGroundNodeSignals();
		RefreshPhysicsProcessEligibility();
		return true;
	}

	private bool TryResolveGroundLayerSource()
	{
		_usingGroundLayerSource = false;
		_groundLayerId = -1;
		if (groundLayerName == null || groundLayerName.IsEmpty || !GodotObject.IsInstanceValid(parent) || !GodotObject.IsInstanceValid(parent.sprite) || !parent.sprite.TryResolveLayerIdForRender(groundLayerName, out var layerId))
		{
			return false;
		}
		_groundLayerId = layerId;
		_usingGroundLayerSource = true;
		ConnectGroundNodeSignals();
		RefreshPhysicsProcessEligibility();
		return true;
	}

	public void ConnectGroundNodeSignals()
	{
		if (_signalsConnected)
		{
			return;
		}
		AdobeAnimateSprite adobeAnimateSprite;
		if (_usingGroundLayerSource)
		{
			adobeAnimateSprite = parent?.sprite;
		}
		else
		{
			if (!(groundNode is AdobeAnimateSlot adobeAnimateSlot))
			{
				return;
			}
			if (adobeAnimateSlot.updateAllFrame)
			{
				adobeAnimateSlot.updateAllFrame = false;
			}
			adobeAnimateSprite = adobeAnimateSlot.GetParent() as AdobeAnimateSprite;
		}
		if (GodotObject.IsInstanceValid(adobeAnimateSprite))
		{
			adobeAnimateSprite.OnAnimeCompleted += GroundNodeAnimationCompleted;
			adobeAnimateSprite.OnAnimeBlendCompleted += GroundNodeAnimationCompleted;
			_signalSprite = adobeAnimateSprite;
			_signalsConnected = true;
		}
	}

	public void DisconnectGroundNodeSignals()
	{
		if (_signalsConnected)
		{
			AdobeAnimateSprite signalSprite = _signalSprite;
			if (GodotObject.IsInstanceValid(signalSprite))
			{
				signalSprite.OnAnimeCompleted -= GroundNodeAnimationCompleted;
				signalSprite.OnAnimeBlendCompleted -= GroundNodeAnimationCompleted;
			}
			_signalSprite = null;
			_signalsConnected = false;
		}
	}

	internal override void PhysicsProcess(double delta, ulong physicsFrame)
	{
		BatchUpdateValidated(delta);
	}

	public void BatchUpdate(double delta)
	{
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && HasMovementSource)
		{
			BatchUpdateValidated(delta);
		}
	}

	private void BatchUpdateValidated(double delta)
	{
		if (IsParentMovementTerminated())
		{
			SetAlive(false);
			return;
		}
		AdobeAnimateSprite sprite = parent.sprite;
		if (!GodotObject.IsInstanceValid(sprite))
		{
			return;
		}
		if (sprite.pause || sprite.blend)
		{
			ResetGroundTracking();
			return;
		}
		if (delay > 0f)
		{
			delay -= (float)delta;
			ResetGroundTracking();
			return;
		}
		if (sprite.playBack && sprite.loop && sprite.frameIndex <= sprite.clipRange.X)
		{
			ResetGroundTracking(refreshScale: true);
			return;
		}
		bool groundChanged;
		Vector2 vector = TryGetGroundPosition(sprite, out groundChanged);
		if (!groundPosInit)
		{
			groundPosSave = vector;
			groundPosInit = true;
			RefreshMoveScale(sprite);
			return;
		}
		if (!groundChanged)
		{
			TowerDefensePerfProfiler.Sample("groundMove.slotUnchanged");
			TowerDefensePerfProfiler.Sample("groundMove.zeroDelta");
			return;
		}
		Vector2 vector2 = groundPosSave - vector;
		groundPosSave = vector;
		if (!moveYAxis)
		{
			vector2.Y = 0f;
		}
		if (vector2 == Vector2.Zero)
		{
			TowerDefensePerfProfiler.Sample("groundMove.zeroDelta");
			return;
		}
		_moveDelta = new Vector2(vector2.X * _moveScale.X, vector2.Y * _moveScale.Y);
		if (_moveDelta == Vector2.Zero)
		{
			TowerDefensePerfProfiler.Sample("groundMove.zeroDelta");
			return;
		}
		TranslateParent(_moveDelta);
		TowerDefensePerfProfiler.Sample("groundMove.moved");
	}

	private void TranslateParent(Vector2 worldDelta)
	{
		Vector2 localDelta = new Vector2(ScaleAxisDelta(worldDelta.X, _parentSpaceScale.X), ScaleAxisDelta(worldDelta.Y, _parentSpaceScale.Y));
		ulong num = TowerDefenseProcessModeDispatch.CurrentPhysicsFrame;
		if (num == 18446744073709551615uL)
		{
			num = Engine.GetPhysicsFrames();
		}
		parent.TranslateForPhysicsFrame(localDelta, worldDelta, num);
	}

	private static float ScaleAxisDelta(float worldDelta, float parentScale)
	{
		if (!(Mathf.Abs(parentScale) > 0.0001f))
		{
			return 0f;
		}
		return worldDelta / parentScale;
	}

	private void ResetGroundTracking(bool refreshScale = false)
	{
		groundPosSave = Vector2.Zero;
		groundPosInit = false;
		_groundSlotPositionVersion = -1;
		_managedPoseCached = false;
		if (refreshScale && GodotObject.IsInstanceValid(parent))
		{
			RefreshMoveScale(parent.sprite);
		}
	}

	private bool IsParentMovementTerminated()
	{
		if (GodotObject.IsInstanceValid(parent) && !parent.die)
		{
			return parent.isDestroy;
		}
		return true;
	}

	public void RefreshDirectionCache()
	{
		ResetGroundTracking(refreshScale: true);
	}

	public bool TryGetCurrentGroundGlobalPosition(Vector2 ownerGlobalPosition, out Vector2 groundGlobalPosition)
	{
		groundGlobalPosition = Vector2.Zero;
		if (!HasMovementSource || !GodotObject.IsInstanceValid(parent) || !GodotObject.IsInstanceValid(parent.sprite))
		{
			return false;
		}
		AdobeAnimateSprite sprite = parent.sprite;
		if (!_moveTransformCached)
		{
			RefreshMoveScale(sprite);
		}
		Vector2 position;
		if (_usingGroundLayerSource)
		{
			if (!TryGetGroundLayerDirectPosition(sprite, out position))
			{
				if (!groundPosInit)
				{
					return false;
				}
				position = groundPosSave;
			}
		}
		else
		{
			if (_groundSlot == null)
			{
				if (GodotObject.IsInstanceValid(groundNode))
				{
					groundGlobalPosition = ownerGlobalPosition + groundNode.GlobalPosition - parent.GetLogicalGlobalPosition();
					return true;
				}
				return false;
			}
			if (!TryGetGroundSlotDirectPosition(sprite, out position))
			{
				position = _groundSlot.CachedPosition;
			}
		}
		Vector2 vector = _groundSourceWorldOriginOffset + new Vector2(position.X * _moveScale.X * _parentSpaceScale.X, position.Y * _moveScale.Y * _parentSpaceScale.Y);
		groundGlobalPosition = ownerGlobalPosition + vector;
		return true;
	}

	private Vector2 TryGetGroundPosition(AdobeAnimateSprite sprite, out bool groundChanged)
	{
		if (_usingGroundLayerSource)
		{
			if (TryGetGroundLayerDirectPosition(sprite, out var position))
			{
				groundChanged = !groundPosInit || position != groundPosSave;
				TowerDefensePerfProfiler.Sample("groundMove.directLayerPose");
				return position;
			}
			groundChanged = false;
			if (!groundPosInit)
			{
				return Vector2.Zero;
			}
			return groundPosSave;
		}
		if (_groundSlot != null)
		{
			if (TryGetGroundSlotDirectPosition(sprite, out var position2))
			{
				groundChanged = !groundPosInit || position2 != groundPosSave;
				TowerDefensePerfProfiler.Sample("groundMove.directPose");
				return position2;
			}
			int runtimePositionVersion = _groundSlot.RuntimePositionVersion;
			groundChanged = runtimePositionVersion != _groundSlotPositionVersion;
			_groundSlotPositionVersion = runtimePositionVersion;
			return _groundSlot.CachedPosition;
		}
		groundChanged = true;
		TowerDefensePerfProfiler.Sample("groundMove.nativeFallback");
		return groundNode.Position;
	}

	private bool TryGetGroundLayerDirectPosition(AdobeAnimateSprite sprite, out Vector2 position)
	{
		position = Vector2.Zero;
		if (!_usingGroundLayerSource || _groundLayerId < 0 || sprite == null)
		{
			return false;
		}
		if (_managedPoseCached && _managedPoseRevision == sprite.ManagedPoseRevision && _managedPoseFrameIndex == sprite.frameIndex && _managedPoseElapsedTimer == sprite.elapsedTimer && _managedPosePlayBack == sprite.playBack && _managedPoseLoop == sprite.loop && _managedPoseSpriteOffset == sprite.offset && _managedPoseSlotOffset == groundLayerOffset && _managedPoseFollowSlotId == _groundLayerId && _managedPoseSlotMode == 0)
		{
			position = _managedPosePosition;
			TowerDefensePerfProfiler.Sample("groundMove.directLayerPoseCached");
			return true;
		}
		if (!sprite.TryGetManagedLayerPositionForRender(_groundLayerId, groundLayerOffset, out position))
		{
			_managedPoseCached = false;
			return false;
		}
		_managedPoseRevision = sprite.ManagedPoseRevision;
		_managedPoseFrameIndex = sprite.frameIndex;
		_managedPoseElapsedTimer = sprite.elapsedTimer;
		_managedPosePlayBack = sprite.playBack;
		_managedPoseLoop = sprite.loop;
		_managedPoseSpriteOffset = sprite.offset;
		_managedPoseSlotOffset = groundLayerOffset;
		_managedPoseFollowSlotId = _groundLayerId;
		_managedPoseSlotMode = 0;
		_managedPosePosition = position;
		_managedPoseCached = true;
		return true;
	}

	private bool TryGetGroundSlotDirectPosition(AdobeAnimateSprite sprite, out Vector2 position)
	{
		position = Vector2.Zero;
		if (_groundSlot == null || sprite == null)
		{
			return false;
		}
		if (_managedPoseCached && _managedPoseRevision == sprite.ManagedPoseRevision && _managedPoseFrameIndex == sprite.frameIndex && _managedPoseElapsedTimer == sprite.elapsedTimer && _managedPosePlayBack == sprite.playBack && _managedPoseLoop == sprite.loop && _managedPoseSpriteOffset == sprite.offset && _managedPoseSlotOffset == _groundSlot.offset && _managedPoseFollowSlotId == _groundSlot.followSlotId && _managedPoseSlotMode == _groundSlot.mode)
		{
			position = _managedPosePosition;
			TowerDefensePerfProfiler.Sample("groundMove.directPoseCached");
			return true;
		}
		if (!sprite.TryGetManagedSlotPositionForRender(_groundSlot, out position))
		{
			_managedPoseCached = false;
			return false;
		}
		_managedPoseRevision = sprite.ManagedPoseRevision;
		_managedPoseFrameIndex = sprite.frameIndex;
		_managedPoseElapsedTimer = sprite.elapsedTimer;
		_managedPosePlayBack = sprite.playBack;
		_managedPoseLoop = sprite.loop;
		_managedPoseSpriteOffset = sprite.offset;
		_managedPoseSlotOffset = _groundSlot.offset;
		_managedPoseFollowSlotId = _groundSlot.followSlotId;
		_managedPoseSlotMode = _groundSlot.mode;
		_managedPosePosition = position;
		_managedPoseCached = true;
		return true;
	}

	private void RefreshMoveScale(AdobeAnimateSprite sprite)
	{
		if (GodotObject.IsInstanceValid(parent) && GodotObject.IsInstanceValid(sprite))
		{
			Vector2 vector = (GodotObject.IsInstanceValid(parent.transformPoint) ? parent.transformPoint.Scale : Vector2.One);
			_moveScale = new Vector2(parent.spriteGroup.Scale.X * vector.X * parent.Scale.X * sprite.Scale.X, parent.spriteGroup.Scale.Y * vector.Y * parent.Scale.Y * sprite.Scale.Y);
			RefreshTranslationSpaceCache();
			_groundSourceWorldOriginOffset = sprite.GlobalPosition - parent.GetLogicalGlobalPosition();
			_moveTransformCached = true;
			TowerDefensePerfProfiler.Sample("groundMove.scaleRefresh");
		}
	}

	private void RefreshTranslationSpaceCache()
	{
		if (!GodotObject.IsInstanceValid(parent))
		{
			_parentSpaceScale = Vector2.One;
		}
		else if (parent.TopLevel || !(parent.GetParent() is Node2D node2D))
		{
			_parentSpaceScale = Vector2.One;
		}
		else
		{
			_parentSpaceScale = node2D.GlobalScale;
		}
	}

	public Vector2 GetMoveLength()
	{
		if (!HasMovementSource || !GodotObject.IsInstanceValid(parent) || !GodotObject.IsInstanceValid(parent.sprite))
		{
			return Vector2.Zero;
		}
		Vector2 position;
		if (_usingGroundLayerSource)
		{
			if (!parent.sprite.TryGetManagedLayerPositionForRender(_groundLayerId, groundLayerOffset, out position))
			{
				return Vector2.Zero;
			}
		}
		else
		{
			position = groundNode.Position;
		}
		Vector2 vector = groundPosSave - position;
		if (!moveYAxis)
		{
			vector.Y = 0f;
		}
		Vector2 vector2 = (GodotObject.IsInstanceValid(parent.transformPoint) ? parent.transformPoint.Scale : Vector2.One);
		return new Vector2(vector.X * parent.spriteGroup.Scale.X * vector2.X * parent.Scale.X * parent.sprite.Scale.X, vector.Y * parent.spriteGroup.Scale.Y * vector2.Y * parent.Scale.Y * parent.sprite.Scale.Y);
	}

	public void GroundNodeAnimationCompleted(string clip)
	{
		if (base.Lifecycle == ComponentRuntimeLifecycle.Active)
		{
			ResetGroundTracking(refreshScale: true);
		}
	}

	public override Dictionary SyncSerialize()
	{
		if (!syncNetworkPosition || !GodotObject.IsInstanceValid(parent) || parent.syncId < 0)
		{
			_syncPayload.Clear();
			return _syncPayload;
		}
		Vector2 logicalGlobalPosition = parent.GetLogicalGlobalPosition();
		_syncPayload["x"] = Mathf.Snapped(logicalGlobalPosition.X, 0.1f);
		_syncPayload["y"] = Mathf.Snapped(logicalGlobalPosition.Y, 0.1f);
		_syncPayload["delay"] = Mathf.Max(0f, delay);
		return _syncPayload;
	}

	public override void SyncDeserialize(Dictionary data)
	{
		if (syncNetworkPosition && data != null && GodotObject.IsInstanceValid(parent))
		{
			Vector2 logicalGlobalPosition = parent.GetLogicalGlobalPosition();
			Vector2 vector = new Vector2((float)data.GetValueOrDefault("x", logicalGlobalPosition.X).AsDouble(), (float)data.GetValueOrDefault("y", logicalGlobalPosition.Y).AsDouble());
			float num = Mathf.Max(0f, networkSnapDistance);
			parent.SetLogicalGlobalPosition((logicalGlobalPosition.DistanceTo(vector) > num) ? vector : logicalGlobalPosition.Lerp(vector, Mathf.Clamp(networkCorrectionWeight, 0f, 1f)));
			if (data.ContainsKey("delay"))
			{
				delay = Mathf.Max(0f, (float)data["delay"].AsDouble());
			}
		}
	}
}
