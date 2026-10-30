using System.Runtime.CompilerServices;
using Godot;

public sealed class ShadowComponent : CharacterComponentRuntime
{
	public const float SHADOW_SCALE_FACTOR = 900f;

	public static bool UseMultiMesh = true;

	public bool followHeight;

	public float heightScaleFactor = 900f;

	public float minimumHeightScale;

	public bool preferMultiMesh = true;

	public bool hideWhenInvisible = true;

	public TowerDefenseCharacter parent;

	public Vector2 saveShadowScale;

	public Vector2 saveShadowPosition;

	public Vector2 saveTransformPointScale = Vector2.One;

	public bool shadowDisabled;

	private bool _configured;

	private bool _initialized;

	private bool _baselineCaptured;

	private bool _shadowAvailabilityInitialized;

	private bool _pendingVisible = true;

	private bool _dirty = true;

	private bool _usingMultiMeshShadow;

	private bool _shadowVisibilityLayerSaved;

	private uint _shadowVisibilityLayer = 1u;

	private float _lastZ;

	private Vector2 _lastTransformScale = Vector2.One;

	private float _lastTransformGlobalScaleY = 1f;

	private float _lastGroundHeight;

	private Vector2 _lastSaveShadowPosition;

	private Vector2 _lastSaveShadowScale;

	private Vector2 _lastSaveTransformPointScale = Vector2.One;

	private bool _lastFollowHeight;

	private float _lastHeightScaleFactor;

	private float _lastMinimumHeightScale;

	private TowerDefenseShadowVisual _shadowSprite;

	private Node2D _transformPoint;

	private TowerDefenseCharacterTransformPoint _trackedTransformPoint;

	private ulong _trackedTransformPointScaleRevision;

	private ulong _trackedTransformPointLocalTransformRevision;

	private bool _transformPointTreeSignalsConnected;

	private bool _ownerActivationVisualCacheReady;

	private bool _shadowSpriteUsesOwnerTransform;

	private TowerDefenseShadowVisual _trackedShadowSprite;

	private ulong _trackedShadowLocalTransformRevision;

	private ulong _ownerGameplayActivationFastPathHits;

	private ulong _ownerGameplayActivationFallbacks;

	private bool _levelEditorPreview;

	private TowerDefenseShadowMultiMeshRenderer.SubmissionHandle _multiMeshSubmission;

	private Texture2D _multiMeshTexture;

	private Vector2 _multiMeshTextureSize;

	private Vector2 _multiMeshLocalOffset;

	private Color _multiMeshColor = Colors.White;

	private Vector2 _multiMeshShadowLocalPosition;

	private float _multiMeshShadowLocalRotation;

	private float _multiMeshShadowLocalSkew;

	private Vector2 _computedShadowScale = Vector2.One;

	private float _computedShadowGlobalY;

	private bool _shadowVisibleInitialized;

	private bool _lastShadowVisible;

	private bool _shadowVisualSignalsConnected;

	private bool _multiMeshVisualStateDirty = true;

	private bool _preparedRendererTransformValid;

	private Vector2 _preparedOwnerBasisX;

	private Vector2 _preparedOwnerBasisY;

	private Vector2 _preparedComputedShadowScale;

	private Transform2D _preparedRendererLocalFromGlobal;

	private Vector2 _preparedRendererBasisX;

	private Vector2 _preparedRendererBasisY;

	private float _preparedWorldOriginOffsetX;

	private float _preparedWorldOriginOffsetY;

	private ulong _rendererBatchRevision;

	internal override bool WantsPhysicsProcess
	{
		get
		{
			if (_usingMultiMeshShadow)
			{
				return _multiMeshSubmission == null;
			}
			return true;
		}
	}

	protected override bool AllowPhysicsOutsideComponentBattlefield => true;

	internal override bool HasOwnerGameplayActivationWork => true;

	private ShadowComponentDefinition Definition => base.ComponentDefinition as ShadowComponentDefinition;

	internal ulong OwnerGameplayActivationFastPathHits => _ownerGameplayActivationFastPathHits;

	internal ulong OwnerGameplayActivationFallbacks => _ownerGameplayActivationFallbacks;

	internal Vector2 ComputedShadowScale => _computedShadowScale;

	internal float ComputedShadowGlobalY => _computedShadowGlobalY;

	protected override void OnBound()
	{
		parent = base.Owner;
		_levelEditorPreview = IsLevelEditorPreview();
		if (!_configured)
		{
			ShadowComponentDefinition definition = Definition;
			followHeight = definition?.followHeight ?? false;
			heightScaleFactor = definition?.heightScaleFactor ?? 900f;
			minimumHeightScale = definition?.minimumHeightScale ?? 0f;
			preferMultiMesh = definition?.preferMultiMesh ?? true;
			hideWhenInvisible = definition?.hideWhenInvisible ?? true;
			_configured = true;
		}
		InitializeAfterOwnerPrepared();
	}

	protected override void OnActivated()
	{
		if (!_initialized || !_baselineCaptured)
		{
			InitializeAfterOwnerPrepared();
		}
		ApplyShadowVisibility();
		MarkDirty();
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		ResetVisualReferences(reason != ComponentDetachReason.TemporaryTreeExit);
	}

	protected override void OnReleased()
	{
		ResetVisualReferences(resetBaseline: true);
	}

	protected override void OnAliveChanged(bool alive)
	{
		MarkDirty();
		ApplyShadowVisibility();
	}

	protected override void OnOwnerGameplayActivated()
	{
		if (TryRecaptureOwnerGameplayActivationFastPath())
		{
			_ownerGameplayActivationFastPathHits++;
			bool flag = _pendingVisible && !shadowDisabled && (!hideWhenInvisible || !parent.invisible);
			if (!_shadowVisibleInitialized || _lastShadowVisible != flag)
			{
				_shadowSprite.Visible = flag;
				_lastShadowVisible = flag;
				_shadowVisibleInitialized = true;
			}
		}
		else
		{
			_ownerGameplayActivationFallbacks++;
			Init();
			ApplyShadowVisibility();
		}
	}

	private bool TryRecaptureOwnerGameplayActivationFastPath()
	{
		if (!_ownerActivationVisualCacheReady)
		{
			return false;
		}
		if (_shadowSprite != parent.shadowSprite || _transformPoint != parent.transformPoint || _trackedShadowSprite.TopLevel)
		{
			_ownerActivationVisualCacheReady = false;
			return false;
		}
		bool flag = IsLevelEditorPreview();
		bool flag2 = preferMultiMesh && UseMultiMesh && !flag && !_shadowSprite.RequiresLegacyRendering;
		if (flag != _levelEditorPreview || flag2 != _usingMultiMeshShadow)
		{
			return false;
		}
		ulong scaleRevision = _trackedTransformPoint.ScaleRevision;
		if (scaleRevision != _trackedTransformPointScaleRevision)
		{
			_trackedTransformPointScaleRevision = scaleRevision;
			saveTransformPointScale = _trackedTransformPoint.CachedScale;
		}
		ulong revision = _trackedShadowSprite.Revision;
		if (revision != _trackedShadowLocalTransformRevision)
		{
			_trackedShadowLocalTransformRevision = revision;
			Transform2D transform = _trackedShadowSprite.Transform;
			saveShadowScale = transform.Scale;
			_multiMeshShadowLocalPosition = transform.Origin;
			_multiMeshShadowLocalRotation = transform.Rotation;
			_multiMeshShadowLocalSkew = transform.Skew;
			_preparedRendererTransformValid = false;
		}
		Transform2D globalTransformForShadow = parent.GetGlobalTransformForShadow(TowerDefenseProcessModeDispatch.CurrentPhysicsFrame);
		saveShadowPosition = globalTransformForShadow * _trackedShadowSprite.Position;
		MarkDirty();
		return true;
	}

	private void InitializeAfterOwnerPrepared()
	{
		if (!TryGetOwner(out var owner) || !GodotObject.IsInstanceValid(owner.shadowSprite) || !GodotObject.IsInstanceValid(owner.transformPoint))
		{
			shadowDisabled = true;
			_initialized = true;
			return;
		}
		BindShadowVisualSignals(owner.shadowSprite);
		CaptureTransformPointScale(owner.transformPoint);
		if (!_baselineCaptured)
		{
			Init();
		}
		if (!_shadowAvailabilityInitialized)
		{
			shadowDisabled = !GodotObject.IsInstanceValid(owner.shadowSprite.Texture);
			_shadowAvailabilityInitialized = true;
		}
		_initialized = true;
		RefreshMultiMeshMode();
		MarkDirty();
		RefreshOwnerActivationVisualCache();
	}

	private void ResetVisualReferences(bool resetBaseline)
	{
		_ownerActivationVisualCacheReady = false;
		_shadowSpriteUsesOwnerTransform = false;
		RestoreShadowSpriteVisibilityLayer();
		DisconnectShadowVisualSignals();
		DisconnectTransformPointTreeSignals();
		parent = null;
		_shadowSprite = null;
		_transformPoint = null;
		_trackedTransformPoint = null;
		_trackedTransformPointScaleRevision = 0uL;
		_trackedTransformPointLocalTransformRevision = 0uL;
		_trackedShadowSprite = null;
		_trackedShadowLocalTransformRevision = 0uL;
		_multiMeshSubmission = null;
		_multiMeshTexture = null;
		_shadowVisibleInitialized = false;
		_multiMeshVisualStateDirty = true;
		_preparedRendererTransformValid = false;
		_initialized = false;
		if (resetBaseline)
		{
			_baselineCaptured = false;
			_shadowAvailabilityInitialized = false;
		}
		_dirty = true;
	}

	public void Init()
	{
		_ownerActivationVisualCacheReady = false;
		_levelEditorPreview = IsLevelEditorPreview();
		if (TryGetOwner(out var owner) && GodotObject.IsInstanceValid(owner.shadowSprite) && GodotObject.IsInstanceValid(owner.transformPoint))
		{
			BindShadowVisualSignals(owner.shadowSprite);
			Vector2 vector = CaptureTransformPointScale(owner.transformPoint);
			Transform2D transform2D = CaptureShadowLocalTransform(owner.shadowSprite);
			saveShadowScale = transform2D.Scale;
			saveShadowPosition = owner.GetLogicalGlobalPosition(owner.shadowSprite);
			saveTransformPointScale = vector;
			_multiMeshShadowLocalPosition = transform2D.Origin;
			_multiMeshShadowLocalRotation = transform2D.Rotation;
			_multiMeshShadowLocalSkew = transform2D.Skew;
			_preparedRendererTransformValid = false;
			_baselineCaptured = true;
			RefreshMultiMeshMode();
			MarkDirty();
			RefreshOwnerActivationVisualCache();
		}
	}

	public void MarkDirty()
	{
		_dirty = true;
		_rendererBatchRevision++;
	}

	public void SetSaveShadowPosition(Vector2 value)
	{
		if (!base.IsReleased)
		{
			saveShadowPosition = value;
			MarkDirty();
		}
	}

	public void TweenSaveShadowPosition(Tween tween, Vector2 target, double duration)
	{
		if (!base.IsReleased && GodotObject.IsInstanceValid(tween))
		{
			tween.TweenMethod(Callable.From<Vector2>(SetSaveShadowPosition), saveShadowPosition, target, duration);
		}
	}

	public void TweenSaveShadowPositionY(Tween tween, float targetY, double duration)
	{
		TweenSaveShadowPosition(tween, new Vector2(saveShadowPosition.X, targetY), duration);
	}

	public void UpdateShadow()
	{
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && TryGetVisuals(out var owner, out var shadow, out var transformPoint))
		{
			bool changed;
			Vector2 currentTransformPointScale = GetCurrentTransformPointScale(transformPoint, out changed);
			float transformGlobalScaleY = (Mathf.IsZeroApprox((float)owner.groundHeight) ? 1f : transformPoint.GlobalScale.Y);
			UpdateShadowValidated(owner, shadow, transformPoint, currentTransformPointScale, transformGlobalScaleY);
		}
	}

	private void UpdateShadowValidated(TowerDefenseCharacter owner, TowerDefenseShadowVisual shadow, Node2D transformPoint, Vector2 transformScale, float transformGlobalScaleY, bool applyToSprite = true, float followHeightBaseY = 0f / 0f)
	{
		Vector2 vector = new Vector2(Mathf.Abs(transformScale.X) / Mathf.Max(Mathf.Abs(saveTransformPointScale.X), 0.001f), Mathf.Abs(transformScale.Y) / Mathf.Max(Mathf.Abs(saveTransformPointScale.Y), 0.001f));
		float b = ((heightScaleFactor <= 0f) ? 1f : (1f - (float)owner.z / heightScaleFactor));
		b = Mathf.Max(minimumHeightScale, b);
		_computedShadowScale = saveShadowScale * b * vector;
		float num = ((!followHeight) ? saveShadowPosition.Y : (float.IsNaN(followHeightBaseY) ? owner.GetLogicalGlobalPosition(owner.transformPoint).Y : followHeightBaseY));
		float num2 = (float)owner.groundHeight * Mathf.Abs(transformGlobalScaleY);
		_computedShadowGlobalY = num - num2;
		if (applyToSprite)
		{
			shadow.Scale = _computedShadowScale;
			shadow.GlobalPosition = new Vector2(shadow.GlobalPosition.X, _computedShadowGlobalY);
		}
	}

	internal override void PhysicsProcess(double delta, ulong physicsFrame)
	{
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && parent != null && _initialized && _shadowSprite != null && _transformPoint != null)
		{
			BatchUpdateValidated(physicsFrame);
		}
	}

	public void BatchUpdate()
	{
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && _initialized && TryGetVisuals(out var _, out var _, out var _))
		{
			BatchUpdateValidated(Engine.GetPhysicsFrames());
		}
	}

	internal void BatchUpdateValidated(ulong physicsFrame)
	{
		TowerDefenseCharacter towerDefenseCharacter = parent;
		TowerDefenseShadowVisual shadowSprite = _shadowSprite;
		Node2D transformPoint = _transformPoint;
		if (UpdateShadowStateValidated(towerDefenseCharacter, shadowSprite, transformPoint))
		{
			SubmitMultiMeshShadow(shadowSprite, towerDefenseCharacter.gridPos.Y, physicsFrame);
		}
	}

	private bool UpdateShadowStateValidated(TowerDefenseCharacter owner, TowerDefenseShadowVisual shadow, Node2D transformPoint)
	{
		if (!ApplyActiveShadowVisibilityValidated())
		{
			return false;
		}
		bool changed;
		Vector2 currentTransformPointScale = GetCurrentTransformPointScale(transformPoint, out changed);
		float a = _lastTransformGlobalScaleY;
		if (!_dirty && !Mathf.IsZeroApprox(_lastGroundHeight))
		{
			a = transformPoint.GlobalScale.Y;
		}
		if (!_dirty && (changed || !Mathf.IsEqualApprox(a, _lastTransformGlobalScaleY) || !saveShadowPosition.IsEqualApprox(_lastSaveShadowPosition) || !saveShadowScale.IsEqualApprox(_lastSaveShadowScale) || followHeight != _lastFollowHeight))
		{
			_dirty = true;
		}
		if (_dirty)
		{
			float lastZ = (float)owner.z;
			float num = (float)owner.groundHeight;
			a = (Mathf.IsZeroApprox(num) ? 1f : transformPoint.GlobalScale.Y);
			_lastZ = lastZ;
			_lastTransformScale = currentTransformPointScale;
			_lastTransformGlobalScaleY = a;
			_lastGroundHeight = num;
			_lastSaveShadowPosition = saveShadowPosition;
			_lastSaveShadowScale = saveShadowScale;
			_lastSaveTransformPointScale = saveTransformPointScale;
			_lastFollowHeight = followHeight;
			_lastHeightScaleFactor = heightScaleFactor;
			_lastMinimumHeightScale = minimumHeightScale;
			_dirty = false;
			UpdateShadowValidated(owner, shadow, transformPoint, currentTransformPointScale, a, !_usingMultiMeshShadow);
		}
		return true;
	}

	public void SetShadowVisible(bool visible)
	{
		_pendingVisible = visible;
		MarkDirty();
		ApplyShadowVisibility();
	}

	public Vector2 GetShadowPosition()
	{
		return saveShadowPosition;
	}

	public Vector2 GetShadowScale()
	{
		return saveShadowScale;
	}

	private bool ApplyShadowVisibility()
	{
		if (!TryGetVisuals(out var _, out var _, out var _))
		{
			return false;
		}
		return ApplyShadowVisibilityValidated();
	}

	private bool ApplyShadowVisibilityValidated()
	{
		bool visible = base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && _initialized && _pendingVisible && !shadowDisabled && (!hideWhenInvisible || !parent.invisible);
		return ApplyShadowVisibilityState(visible);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool ApplyActiveShadowVisibilityValidated()
	{
		bool visible = _pendingVisible && !shadowDisabled && (!hideWhenInvisible || !parent.invisible);
		return ApplyShadowVisibilityState(visible);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool ApplyShadowVisibilityState(bool visible)
	{
		if (_shadowVisibleInitialized && _lastShadowVisible == visible)
		{
			return visible;
		}
		_shadowSprite.Visible = visible;
		_lastShadowVisible = visible;
		_shadowVisibleInitialized = true;
		return visible;
	}

	private void RefreshMultiMeshMode()
	{
		bool flag = preferMultiMesh && UseMultiMesh && !_levelEditorPreview && _shadowSprite != null && !_shadowSprite.RequiresLegacyRendering;
		if (flag != _usingMultiMeshShadow)
		{
			if (flag)
			{
				EnableMultiMeshShadow();
			}
			else
			{
				RestoreShadowSpriteVisibilityLayer();
			}
		}
	}

	private static bool IsLevelEditorPreview()
	{
		if (Global.Instance != null && Global.Instance.isEditor)
		{
			return SceneManager.CurrentScene == "LevelEditorStage";
		}
		return false;
	}

	private void EnableMultiMeshShadow()
	{
		if (_shadowSprite != null)
		{
			_usingMultiMeshShadow = true;
		}
	}

	private void HideShadowSpriteForMultiMesh()
	{
		Sprite2D sprite2D = _shadowSprite?.LegacySprite;
		if (sprite2D != null)
		{
			if (!_shadowVisibilityLayerSaved)
			{
				_shadowVisibilityLayer = sprite2D.VisibilityLayer;
				_shadowVisibilityLayerSaved = true;
			}
			sprite2D.VisibilityLayer = 0u;
		}
	}

	private void RestoreShadowSpriteVisibilityLayer()
	{
		if (_multiMeshSubmission != null)
		{
			_multiMeshSubmission.UnregisterSource(this);
		}
		_multiMeshSubmission = null;
		_multiMeshTexture = null;
		_preparedRendererTransformValid = false;
		Sprite2D sprite2D = _shadowSprite?.LegacySprite;
		if (_shadowVisibilityLayerSaved && sprite2D != null)
		{
			sprite2D.VisibilityLayer = _shadowVisibilityLayer;
		}
		_usingMultiMeshShadow = false;
		RefreshPhysicsProcessEligibility();
	}

	private void SubmitMultiMeshShadow(TowerDefenseShadowVisual shadow, int line, ulong physicsFrame)
	{
		RefreshMultiMeshMode();
		if (_usingMultiMeshShadow && _multiMeshSubmission == null)
		{
			Texture2D texture = shadow.Texture;
			if (TowerDefenseShadowMultiMeshRenderer.SubmitVisual(shadow, line))
			{
				CaptureReusableMultiMeshSubmission(shadow, texture, physicsFrame);
				HideShadowSpriteForMultiMesh();
			}
			else
			{
				RestoreShadowSpriteVisibilityLayer();
			}
		}
	}

	private void CaptureReusableMultiMeshSubmission(TowerDefenseShadowVisual shadow, Texture2D texture, ulong physicsFrame)
	{
		if (texture == null)
		{
			return;
		}
		Vector2 size = texture.GetSize();
		if (!(size.X <= 0f) && !(size.Y <= 0f))
		{
			Vector2 offset = shadow.Offset;
			if (!shadow.Centered)
			{
				offset += size * 0.5f;
			}
			if (shadow.FlipH)
			{
				size.X = 0f - size.X;
			}
			if (shadow.FlipV)
			{
				size.Y = 0f - size.Y;
			}
			Color modulate = shadow.Modulate;
			Color selfModulate = shadow.SelfModulate;
			Transform2D transform = shadow.Transform;
			_multiMeshTexture = texture;
			_multiMeshTextureSize = size;
			_multiMeshLocalOffset = offset;
			_multiMeshColor = new Color(modulate.R * selfModulate.R, modulate.G * selfModulate.G, modulate.B * selfModulate.B, modulate.A * selfModulate.A);
			_multiMeshShadowLocalPosition = transform.Origin;
			_multiMeshShadowLocalRotation = transform.Rotation;
			_multiMeshShadowLocalSkew = transform.Skew;
			_multiMeshSubmission = TowerDefenseShadowMultiMeshRenderer.PrepareReusableSubmission(shadow.OwnerNode, texture, (long)physicsFrame);
			if (_multiMeshSubmission != null && !_multiMeshSubmission.RegisterSource(this))
			{
				_multiMeshSubmission = null;
				_multiMeshTexture = null;
			}
			_multiMeshVisualStateDirty = false;
			_preparedRendererTransformValid = false;
			RefreshPhysicsProcessEligibility();
		}
	}

	internal void SubmitRegisteredMultiMeshShadow(ulong physicsFrame, Transform2D rendererLocalFromGlobal)
	{
		if (TrySubmitRegisteredDataFastPath(physicsFrame, rendererLocalFromGlobal))
		{
			return;
		}
		RefreshMultiMeshMode();
		if (!base.Alive || base.Lifecycle != ComponentRuntimeLifecycle.Active || !_initialized || !_usingMultiMeshShadow || _multiMeshSubmission == null)
		{
			return;
		}
		TowerDefenseShadowVisual shadowSprite = _shadowSprite;
		if (UpdateShadowStateValidated(parent, shadowSprite, _transformPoint))
		{
			Transform2D rendererLocalTransform;
			if (_multiMeshVisualStateDirty)
			{
				RestoreShadowSpriteVisibilityLayer();
				RefreshMultiMeshMode();
				SubmitMultiMeshShadow(shadowSprite, parent.gridPos.Y, physicsFrame);
			}
			else if (!TryGetPreparedRendererTransform(physicsFrame, rendererLocalFromGlobal, out rendererLocalTransform))
			{
				RestoreShadowSpriteVisibilityLayer();
			}
			else if (!_multiMeshSubmission.SubmitRendererLocalPrepared(rendererLocalTransform, _multiMeshColor))
			{
				RestoreShadowSpriteVisibilityLayer();
			}
		}
	}

	internal bool TrySubmitRegisteredDataFastPath(ulong physicsFrame, Transform2D rendererLocalFromGlobal)
	{
		if (!preferMultiMesh || !UseMultiMesh || !TowerDefenseShadowMultiMeshRenderer.Enabled || !base.Alive || base.Lifecycle != ComponentRuntimeLifecycle.Active || !_initialized || !_usingMultiMeshShadow || _multiMeshSubmission == null || _multiMeshVisualStateDirty || _trackedTransformPoint == null)
		{
			return false;
		}
		ulong localTransformRevision = _trackedTransformPoint.LocalTransformRevision;
		bool flag = localTransformRevision != _trackedTransformPointLocalTransformRevision;
		float num = (float)parent.z;
		float num2 = (float)parent.groundHeight;
		bool flag2 = _dirty || flag || num != _lastZ || num2 != _lastGroundHeight || saveShadowPosition != _lastSaveShadowPosition || saveShadowScale != _lastSaveShadowScale || saveTransformPointScale != _lastSaveTransformPointScale || followHeight != _lastFollowHeight || heightScaleFactor != _lastHeightScaleFactor || minimumHeightScale != _lastMinimumHeightScale;
		Transform2D globalTransformForShadow = parent.GetGlobalTransformForShadow(physicsFrame);
		if (!flag2 && !Mathf.IsZeroApprox(num2))
		{
			Transform2D cachedLocalTransform = _trackedTransformPoint.CachedLocalTransform;
			flag2 = (globalTransformForShadow * cachedLocalTransform).Scale.Y != _lastTransformGlobalScaleY;
		}
		if (followHeight)
		{
			flag2 = true;
		}
		if (flag2)
		{
			RefreshRegisteredDataState(globalTransformForShadow, localTransformRevision, num, num2);
		}
		if (!ApplyActiveShadowVisibilityValidated())
		{
			return true;
		}
		if (!TryGetPreparedRendererTransform(globalTransformForShadow, rendererLocalFromGlobal, out var rendererLocalTransform))
		{
			return false;
		}
		return _multiMeshSubmission.SubmitRendererLocalPrepared(rendererLocalTransform, _multiMeshColor);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void RefreshRegisteredDataState(Transform2D ownerTransform, ulong localTransformRevision, float currentZ, float currentGroundHeight)
	{
		_trackedTransformPointLocalTransformRevision = localTransformRevision;
		_trackedTransformPointScaleRevision = _trackedTransformPoint.ScaleRevision;
		Transform2D cachedLocalTransform = _trackedTransformPoint.CachedLocalTransform;
		Vector2 scale = cachedLocalTransform.Scale;
		float num = (Mathf.IsZeroApprox(currentGroundHeight) ? 1f : (ownerTransform * cachedLocalTransform).Scale.Y);
		_lastZ = currentZ;
		_lastTransformScale = scale;
		_lastTransformGlobalScaleY = num;
		_lastGroundHeight = currentGroundHeight;
		_lastSaveShadowPosition = saveShadowPosition;
		_lastSaveShadowScale = saveShadowScale;
		_lastSaveTransformPointScale = saveTransformPointScale;
		_lastFollowHeight = followHeight;
		_lastHeightScaleFactor = heightScaleFactor;
		_lastMinimumHeightScale = minimumHeightScale;
		_dirty = false;
		float y = (ownerTransform * cachedLocalTransform.Origin).Y;
		UpdateShadowValidated(parent, _shadowSprite, _transformPoint, scale, num, applyToSprite: false, y);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool TryGetPreparedRendererTransform(ulong physicsFrame, Transform2D rendererLocalFromGlobal, out Transform2D rendererLocalTransform)
	{
		Transform2D globalTransformForShadow = parent.GetGlobalTransformForShadow(physicsFrame);
		return TryGetPreparedRendererTransform(globalTransformForShadow, rendererLocalFromGlobal, out rendererLocalTransform);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool TryGetPreparedRendererTransform(Transform2D ownerTransform, Transform2D rendererLocalFromGlobal, out Transform2D rendererLocalTransform)
	{
		Vector2 x = ownerTransform.X;
		Vector2 y = ownerTransform.Y;
		if (!_preparedRendererTransformValid || x != _preparedOwnerBasisX || y != _preparedOwnerBasisY || _computedShadowScale != _preparedComputedShadowScale || rendererLocalFromGlobal != _preparedRendererLocalFromGlobal)
		{
			RebuildPreparedRendererTransform(ownerTransform, rendererLocalFromGlobal);
		}
		Vector2 value = new Vector2(ownerTransform.Origin.X + _preparedWorldOriginOffsetX, _computedShadowGlobalY + _preparedWorldOriginOffsetY);
		Vector2 originPos = rendererLocalFromGlobal.Origin + TransformBasis(in rendererLocalFromGlobal, value);
		rendererLocalTransform = new Transform2D(_preparedRendererBasisX, _preparedRendererBasisY, originPos);
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void RebuildPreparedRendererTransform(Transform2D ownerTransform, Transform2D rendererLocalFromGlobal)
	{
		Transform2D transform2D = new Transform2D(_multiMeshShadowLocalRotation, _computedShadowScale, _multiMeshShadowLocalSkew, _multiMeshShadowLocalPosition);
		Vector2 vector = TransformBasis(in ownerTransform, transform2D.X);
		Vector2 vector2 = TransformBasis(in ownerTransform, transform2D.Y);
		Vector2 vector3 = TransformBasis(in ownerTransform, transform2D.Origin);
		Vector2 value = vector * _multiMeshTextureSize.X;
		Vector2 value2 = vector2 * _multiMeshTextureSize.Y;
		_preparedOwnerBasisX = ownerTransform.X;
		_preparedOwnerBasisY = ownerTransform.Y;
		_preparedComputedShadowScale = _computedShadowScale;
		_preparedRendererLocalFromGlobal = rendererLocalFromGlobal;
		_preparedRendererBasisX = TransformBasis(in rendererLocalFromGlobal, value);
		_preparedRendererBasisY = TransformBasis(in rendererLocalFromGlobal, value2);
		_preparedWorldOriginOffsetX = vector3.X + vector.X * _multiMeshLocalOffset.X + vector2.X * _multiMeshLocalOffset.Y;
		_preparedWorldOriginOffsetY = vector.Y * _multiMeshLocalOffset.X + vector2.Y * _multiMeshLocalOffset.Y;
		_preparedRendererTransformValid = true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector2 TransformBasis(in Transform2D transform, Vector2 value)
	{
		return new Vector2(transform.X.X * value.X + transform.Y.X * value.Y, transform.X.Y * value.X + transform.Y.Y * value.Y);
	}

	private bool TryGetOwner(out TowerDefenseCharacter owner)
	{
		owner = parent;
		return GodotObject.IsInstanceValid(owner);
	}

	private bool TryGetVisuals(out TowerDefenseCharacter owner, out TowerDefenseShadowVisual shadow, out Node2D transformPoint)
	{
		shadow = null;
		transformPoint = null;
		if (!TryGetOwner(out owner))
		{
			return false;
		}
		shadow = owner.shadowSprite;
		transformPoint = owner.transformPoint;
		if (!GodotObject.IsInstanceValid(shadow) || !GodotObject.IsInstanceValid(transformPoint))
		{
			return false;
		}
		BindShadowVisualSignals(shadow);
		if (_transformPoint != transformPoint)
		{
			CaptureTransformPointScale(transformPoint);
		}
		if (!_ownerActivationVisualCacheReady)
		{
			RefreshOwnerActivationVisualCache();
		}
		return true;
	}

	private Vector2 CaptureTransformPointScale(Node2D transformPoint)
	{
		if (_transformPoint != transformPoint)
		{
			_ownerActivationVisualCacheReady = false;
			DisconnectTransformPointTreeSignals();
			_transformPoint = transformPoint;
			_trackedTransformPoint = transformPoint as TowerDefenseCharacterTransformPoint;
			BindTransformPointTreeSignals(transformPoint);
		}
		else if (!_transformPointTreeSignalsConnected)
		{
			BindTransformPointTreeSignals(transformPoint);
		}
		if (_trackedTransformPoint != null)
		{
			_trackedTransformPointScaleRevision = _trackedTransformPoint.ScaleRevision;
			_trackedTransformPointLocalTransformRevision = _trackedTransformPoint.LocalTransformRevision;
			return _trackedTransformPoint.CachedScale;
		}
		_trackedTransformPointScaleRevision = 0uL;
		_trackedTransformPointLocalTransformRevision = 0uL;
		return transformPoint?.Scale ?? Vector2.One;
	}

	private Transform2D CaptureShadowLocalTransform(TowerDefenseShadowVisual shadow)
	{
		_trackedShadowSprite = shadow;
		if (_trackedShadowSprite != null)
		{
			_trackedShadowLocalTransformRevision = _trackedShadowSprite.Revision;
			return _trackedShadowSprite.Transform;
		}
		_trackedShadowLocalTransformRevision = 0uL;
		return shadow?.Transform ?? Transform2D.Identity;
	}

	private Vector2 GetCurrentTransformPointScale(Node2D transformPoint, out bool changed)
	{
		if (_transformPoint != transformPoint)
		{
			Vector2 result = CaptureTransformPointScale(transformPoint);
			changed = true;
			return result;
		}
		if (_trackedTransformPoint != null)
		{
			ulong scaleRevision = _trackedTransformPoint.ScaleRevision;
			changed = scaleRevision != _trackedTransformPointScaleRevision;
			_trackedTransformPointScaleRevision = scaleRevision;
			return _trackedTransformPoint.CachedScale;
		}
		Vector2 scale = transformPoint.Scale;
		changed = !scale.IsEqualApprox(_lastTransformScale);
		return scale;
	}

	private void BindShadowVisualSignals(TowerDefenseShadowVisual shadow)
	{
		if (_shadowSprite == shadow && _shadowVisualSignalsConnected)
		{
			return;
		}
		_ownerActivationVisualCacheReady = false;
		_trackedShadowSprite = null;
		_trackedShadowLocalTransformRevision = 0uL;
		DisconnectShadowVisualSignals();
		_shadowSprite = shadow;
		if (GodotObject.IsInstanceValid(shadow))
		{
			shadow.Changed += OnShadowVisualStateChanged;
			Sprite2D legacySprite = shadow.LegacySprite;
			if (GodotObject.IsInstanceValid(legacySprite))
			{
				legacySprite.ItemRectChanged += OnShadowVisualStateChanged;
				legacySprite.TreeExiting += OnShadowVisualTreeStateChanged;
				legacySprite.TreeEntered += OnShadowVisualTreeStateChanged;
			}
			_shadowVisualSignalsConnected = true;
			_multiMeshVisualStateDirty = true;
		}
	}

	private void DisconnectShadowVisualSignals()
	{
		if (_shadowVisualSignalsConnected && GodotObject.IsInstanceValid(_shadowSprite))
		{
			_shadowSprite.Changed -= OnShadowVisualStateChanged;
			Sprite2D legacySprite = _shadowSprite.LegacySprite;
			if (GodotObject.IsInstanceValid(legacySprite))
			{
				legacySprite.ItemRectChanged -= OnShadowVisualStateChanged;
				legacySprite.TreeExiting -= OnShadowVisualTreeStateChanged;
				legacySprite.TreeEntered -= OnShadowVisualTreeStateChanged;
			}
		}
		_shadowVisualSignalsConnected = false;
		_ownerActivationVisualCacheReady = false;
	}

	private void BindTransformPointTreeSignals(Node2D transformPoint)
	{
		if (!_transformPointTreeSignalsConnected && GodotObject.IsInstanceValid(transformPoint))
		{
			transformPoint.TreeExiting += OnTransformPointTreeStateChanged;
			transformPoint.TreeEntered += OnTransformPointTreeStateChanged;
			_transformPointTreeSignalsConnected = true;
		}
	}

	private void DisconnectTransformPointTreeSignals()
	{
		if (_transformPointTreeSignalsConnected && GodotObject.IsInstanceValid(_transformPoint))
		{
			_transformPoint.TreeExiting -= OnTransformPointTreeStateChanged;
			_transformPoint.TreeEntered -= OnTransformPointTreeStateChanged;
		}
		_transformPointTreeSignalsConnected = false;
		_ownerActivationVisualCacheReady = false;
	}

	private void RefreshOwnerActivationVisualCache()
	{
		bool flag = _initialized && _baselineCaptured && _shadowAvailabilityInitialized && _shadowVisualSignalsConnected && _transformPointTreeSignalsConnected && _trackedShadowSprite != null && _trackedTransformPoint != null && GodotObject.IsInstanceValid(parent) && GodotObject.IsInstanceValid(_shadowSprite) && GodotObject.IsInstanceValid(_transformPoint) && parent == base.Owner && _shadowSprite == parent.shadowSprite && _transformPoint == parent.transformPoint && _trackedShadowSprite == _shadowSprite && _trackedTransformPoint == _transformPoint && _transformPoint.IsInsideTree() && !_trackedShadowSprite.TopLevel;
		_shadowSpriteUsesOwnerTransform = flag && _shadowSprite.OwnerNode == parent;
		_ownerActivationVisualCacheReady = flag && _shadowSpriteUsesOwnerTransform;
		if (_ownerActivationVisualCacheReady)
		{
			parent.GetGlobalTransformForShadow(TowerDefenseProcessModeDispatch.CurrentPhysicsFrame);
		}
	}

	private void OnShadowVisualTreeStateChanged()
	{
		_ownerActivationVisualCacheReady = false;
		MarkDirty();
	}

	private void OnTransformPointTreeStateChanged()
	{
		_ownerActivationVisualCacheReady = false;
		MarkDirty();
	}

	private void OnShadowVisualStateChanged()
	{
		_preparedRendererTransformValid = false;
		_multiMeshVisualStateDirty = true;
		MarkDirty();
	}
}
