using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using Godot;
using Godot.Collections;

public sealed class ShowHealthComponent : CharacterComponentRuntime
{
	public enum SecondaryArmorPriority
	{
		ShieldFirst,
		HeadCoverFirst
	}

	private struct HealthLabelState
	{
		public bool Initialized;

		public bool Visible;

		public bool RoundHitpoints;

		public int DecimalPlaces;

		public double Current;

		public double Maximum;

		public string Template;

		public readonly bool Matches(bool visible, double current, double maximum, bool roundHitpoints, int decimalPlaces, string template)
		{
			if (Initialized && Visible == visible)
			{
				if (visible)
				{
					if (Current.Equals(current) && Maximum.Equals(maximum) && RoundHitpoints == roundHitpoints && DecimalPlaces == decimalPlaces)
					{
						return string.Equals(Template, template, StringComparison.Ordinal);
					}
					return false;
				}
				return true;
			}
			return false;
		}
	}

	private const string DefaultViewScenePath = "res://Script/Component/TowerDefense/Character/ShowHealthComponent/ShowHealthComponentView.tscn";

	private const string DefaultTextTemplate = "HP:{0}/{1}";

	private const string FallbackTextTemplate = "{0}/{1}";

	internal const ulong RefreshFrameStride = 3uL;

	public PackedScene viewScene;

	public string textTemplate = "HP:{0}/{1}";

	public int decimalPlaces;

	public bool roundHitpoints = true;

	public bool showBody = true;

	public bool showSecondaryArmor = true;

	public bool showHelmet = true;

	public SecondaryArmorPriority secondaryArmorPriority;

	public bool keepScreenAligned = true;

	public int displayZIndex = 10;

	public Color shieldColor = new Color(0f, 0.992157f, 1f);

	public Color helmetColor = Colors.Yellow;

	public Color bodyColor = Colors.Red;

	public Label shieldHitpointLabel;

	public Label helmetHitpointLabel;

	public Label bodyHitpointLabel;

	public CenterContainer centerContainer;

	public TowerDefenseCharacter parent;

	private Node2D _viewHost;

	private Node2D _viewAnchor;

	private RemoteTransform2D _viewFollower;

	private bool _dirty = true;

	private bool _refreshPending;

	private bool _forceRefreshPending;

	private ulong _refreshFrameOffset;

	private bool _viewFromPool;

	private bool _visibilityConnected;

	private bool _configured;

	private bool _visible = true;

	private bool _usingSharedDrawBatch;

	private bool _viewHostTransformConnected;

	private TowerDefenseCharacterSpriteGroup _trackedViewHost;

	private string _shieldDisplayText;

	private string _helmetDisplayText;

	private string _bodyDisplayText;

	private TowerDefenseArmorInstance _activeShieldArmor;

	private TowerDefenseArmorInstance _activeHeadCoverArmor;

	private TowerDefenseArmorInstance _activeHelmetArmor;

	private HealthLabelState _shieldLabelState;

	private HealthLabelState _helmetLabelState;

	private HealthLabelState _bodyLabelState;

	public bool AllowRefreshOutsideComponentBattlefield { get; set; }

	protected override bool AllowPhysicsOutsideComponentBattlefield
	{
		get
		{
			if (!AllowRefreshOutsideComponentBattlefield)
			{
				return IsOwnerInsideLawnBounds();
			}
			return true;
		}
	}

	internal override bool WantsPhysicsProcess => _refreshPending;

	private ShowHealthComponentDefinition Definition => base.ComponentDefinition as ShowHealthComponentDefinition;

	public bool Visible
	{
		get
		{
			return _visible;
		}
		set
		{
			SetVisible(value);
		}
	}

	internal TowerDefenseCharacter BatchOwner => parent;

	internal bool UsingSharedDrawBatch => _usingSharedDrawBatch;

	internal string ShieldDisplayText => _shieldDisplayText;

	internal string HelmetDisplayText => _helmetDisplayText;

	internal string BodyDisplayText => _bodyDisplayText;

	private bool IsOwnerInsideLawnBounds()
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(parent) || !GodotObject.IsInstanceValid(instance))
		{
			return false;
		}
		float x = parent.GetGlobalPositionForPhysicsFrame(TowerDefenseProcessModeDispatch.CurrentPhysicsFrame).X;
		if ((double)x >= instance.GetMapGroundLeft())
		{
			return (double)x <= instance.GetMapGroundRight();
		}
		return false;
	}

	protected override void OnBound()
	{
		parent = base.Owner;
		_refreshFrameOffset = (ulong)(Math.Abs((long)parent.randFreshIndex) % 3);
		ApplyDefinitionOnce();
		_viewHost = (GodotObject.IsInstanceValid(parent?.spriteGroup) ? parent.spriteGroup : parent?.GetNodeOrNull<Node2D>("SpriteGroup"));
		parent.OnArmorHurt += OnArmorHurt;
		parent.RenderZIndexChanged += OnParentRenderZIndexChanged;
		ConnectVisibilitySignal();
	}

	protected override void OnActivated()
	{
		RefreshViewOwnership();
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		_refreshPending = false;
		_forceRefreshPending = false;
		DetachSharedDrawBatch();
		DisconnectArmorHurtSignal();
		DisconnectRenderZIndexSignal();
		DisconnectVisibilitySignal();
		HideAllLabels();
		ReleaseView();
		_viewHost = null;
		ClearArmorCache();
		parent = null;
	}

	private void OnArmorHurt(int _)
	{
		MarkDirty();
	}

	private void DisconnectArmorHurtSignal()
	{
		if (GodotObject.IsInstanceValid(parent))
		{
			parent.OnArmorHurt -= OnArmorHurt;
		}
	}

	private void OnParentRenderZIndexChanged(int _)
	{
		if (_usingSharedDrawBatch)
		{
			TowerDefenseHealthDisplayBatch.NotifyZIndexChanged(this);
		}
		else
		{
			UpdateViewAnchorZIndex();
		}
	}

	private void DisconnectRenderZIndexSignal()
	{
		if (GodotObject.IsInstanceValid(parent))
		{
			parent.RenderZIndexChanged -= OnParentRenderZIndexChanged;
		}
	}

	protected override void OnReleased()
	{
		_refreshPending = false;
		_forceRefreshPending = false;
		DetachSharedDrawBatch();
		DisconnectRenderZIndexSignal();
		DisconnectVisibilitySignal();
		HideAllLabels();
		ReleaseView();
		viewScene = null;
		_viewHost = null;
		ClearArmorCache();
		parent = null;
	}

	protected override void OnAliveChanged(bool alive)
	{
		if (!alive)
		{
			_refreshPending = false;
			_forceRefreshPending = false;
			DetachSharedDrawBatch();
			HideAllLabels();
			ReleaseView();
		}
		else
		{
			_dirty = true;
			RefreshViewOwnership();
		}
	}

	private void ApplyDefinitionOnce()
	{
		if (!_configured && Definition != null)
		{
			ShowHealthComponentDefinition definition = Definition;
			viewScene = definition.viewScene;
			textTemplate = definition.textTemplate;
			decimalPlaces = definition.decimalPlaces;
			roundHitpoints = definition.roundHitpoints;
			showBody = definition.showBody;
			showSecondaryArmor = definition.showSecondaryArmor;
			showHelmet = definition.showHelmet;
			secondaryArmorPriority = definition.secondaryArmorPriority;
			keepScreenAligned = definition.keepScreenAligned;
			displayZIndex = definition.displayZIndex;
			shieldColor = definition.shieldColor;
			helmetColor = definition.helmetColor;
			bodyColor = definition.bodyColor;
			_configured = true;
		}
	}

	public void SetVisible(bool visible)
	{
		if (_visible != visible)
		{
			_visible = visible;
			RefreshViewOwnership();
		}
	}

	public void MarkDirty()
	{
		if (!base.IsReleased)
		{
			_dirty = true;
			QueueRefresh();
		}
	}

	internal override void PhysicsProcess(double delta, ulong physicsFrame)
	{
		if (_forceRefreshPending || (physicsFrame + _refreshFrameOffset) % 3 == 0L)
		{
			_forceRefreshPending = false;
			_refreshPending = false;
			BatchUpdate();
			RefreshPhysicsProcessEligibility();
		}
	}

	public void BatchUpdate()
	{
		TowerDefenseCharacterInstance instance;
		if (!IsDisplayVisible())
		{
			DetachSharedDrawBatch();
			ReleaseView();
		}
		else if (TryGetRuntime(out instance) && (_usingSharedDrawBatch || EnsureView()) && _dirty)
		{
			_dirty = false;
			TowerDefenseArmorInstance armor = null;
			if (showSecondaryArmor)
			{
				armor = ((secondaryArmorPriority != SecondaryArmorPriority.HeadCoverFirst) ? (FindActiveArmor(instance.armorShield, ref _activeShieldArmor) ?? FindActiveArmor(instance.armorHeadCover, ref _activeHeadCoverArmor)) : (FindActiveArmor(instance.armorHeadCover, ref _activeHeadCoverArmor) ?? FindActiveArmor(instance.armorShield, ref _activeShieldArmor)));
			}
			TowerDefenseArmorInstance armor2 = (showHelmet ? FindActiveArmor(instance.armorHelm, ref _activeHelmetArmor) : null);
			UpdateArmorLabel(shieldHitpointLabel, armor, ref _shieldLabelState, ref _shieldDisplayText);
			UpdateArmorLabel(helmetHitpointLabel, armor2, ref _helmetLabelState, ref _helmetDisplayText);
			UpdateBodyLabel(instance, ref _bodyLabelState, ref _bodyDisplayText);
			if (_usingSharedDrawBatch)
			{
				TowerDefenseHealthDisplayBatch.Update(this);
			}
		}
	}

	private static TowerDefenseArmorInstance FindActiveArmor(Array<TowerDefenseArmorInstance> armorList, ref TowerDefenseArmorInstance cachedArmor)
	{
		if (GodotObject.IsInstanceValid(cachedArmor) && !cachedArmor.isRemove)
		{
			return cachedArmor;
		}
		cachedArmor = null;
		if (armorList == null)
		{
			return null;
		}
		for (int i = 0; i < armorList.Count; i++)
		{
			TowerDefenseArmorInstance towerDefenseArmorInstance = armorList[i];
			if (GodotObject.IsInstanceValid(towerDefenseArmorInstance) && !towerDefenseArmorInstance.isRemove)
			{
				cachedArmor = towerDefenseArmorInstance;
				return cachedArmor;
			}
		}
		return null;
	}

	private void ClearArmorCache()
	{
		_activeShieldArmor = null;
		_activeHeadCoverArmor = null;
		_activeHelmetArmor = null;
	}

	private void UpdateArmorLabel(Label label, TowerDefenseArmorInstance armor, ref HealthLabelState state, ref string displayText)
	{
		bool flag = GodotObject.IsInstanceValid(armor);
		UpdateHealthLabel(label, flag, flag ? armor.hitPoints : 0.0, flag ? armor.hitpointsSave : 0.0, ref state, ref displayText);
	}

	private void UpdateBodyLabel(TowerDefenseCharacterInstance instance, ref HealthLabelState state, ref string displayText)
	{
		UpdateHealthLabel(bodyHitpointLabel, showBody, instance.hitpoints, instance.hitpointsSave, ref state, ref displayText);
	}

	private void UpdateHealthLabel(Label label, bool visible, double current, double maximum, ref HealthLabelState state, ref string displayText)
	{
		int digits = Mathf.Clamp(decimalPlaces, 0, 3);
		string template = (string.IsNullOrEmpty(textTemplate) ? "{0}/{1}" : textTemplate);
		double current2 = (roundHitpoints ? ((double)Mathf.RoundToInt(current)) : current);
		double maximum2 = (roundHitpoints ? ((double)Mathf.RoundToInt(maximum)) : maximum);
		if (!state.Matches(visible, current2, maximum2, roundHitpoints, digits, template))
		{
			bool num = GodotObject.IsInstanceValid(label);
			if (num && (!state.Initialized || state.Visible != visible))
			{
				label.Visible = visible;
			}
			displayText = (visible ? FormatHealthForDisplay(current, maximum, template, digits, roundHitpoints) : null);
			if (num && visible)
			{
				label.Text = displayText;
			}
			state = new HealthLabelState
			{
				Initialized = true,
				Visible = visible,
				RoundHitpoints = roundHitpoints,
				DecimalPlaces = digits,
				Current = current2,
				Maximum = maximum2,
				Template = template
			};
		}
	}

	public static string FormatHealthForDisplay(double current, double maximum, string template, int digits, bool roundHitpoints)
	{
		digits = Mathf.Clamp(digits, 0, 3);
		template = (string.IsNullOrEmpty(template) ? "{0}/{1}" : template);
		DefaultInterpolatedStringHandler handler;
		if (roundHitpoints)
		{
			int value = Mathf.RoundToInt(current);
			int value2 = Mathf.RoundToInt(maximum);
			if (string.Equals(template, "HP:{0}/{1}", StringComparison.Ordinal))
			{
				IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider = invariantCulture;
				handler = new DefaultInterpolatedStringHandler(4, 2, invariantCulture);
				handler.AppendLiteral("HP:");
				handler.AppendFormatted(value);
				handler.AppendLiteral("/");
				handler.AppendFormatted(value2);
				return string.Create(provider, ref handler);
			}
			if (string.Equals(template, "{0}/{1}", StringComparison.Ordinal))
			{
				IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider2 = invariantCulture;
				handler = new DefaultInterpolatedStringHandler(1, 2, invariantCulture);
				handler.AppendFormatted(value);
				handler.AppendLiteral("/");
				handler.AppendFormatted(value2);
				return string.Create(provider2, ref handler);
			}
		}
		else if (string.Equals(template, "HP:{0}/{1}", StringComparison.Ordinal))
		{
			switch (digits)
			{
			case 0:
			{
				IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider6 = invariantCulture;
				handler = new DefaultInterpolatedStringHandler(4, 2, invariantCulture);
				handler.AppendLiteral("HP:");
				handler.AppendFormatted(current, "F0");
				handler.AppendLiteral("/");
				handler.AppendFormatted(maximum, "F0");
				return string.Create(provider6, ref handler);
			}
			case 1:
			{
				IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider5 = invariantCulture;
				handler = new DefaultInterpolatedStringHandler(4, 2, invariantCulture);
				handler.AppendLiteral("HP:");
				handler.AppendFormatted(current, "F1");
				handler.AppendLiteral("/");
				handler.AppendFormatted(maximum, "F1");
				return string.Create(provider5, ref handler);
			}
			case 2:
			{
				IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider4 = invariantCulture;
				handler = new DefaultInterpolatedStringHandler(4, 2, invariantCulture);
				handler.AppendLiteral("HP:");
				handler.AppendFormatted(current, "F2");
				handler.AppendLiteral("/");
				handler.AppendFormatted(maximum, "F2");
				return string.Create(provider4, ref handler);
			}
			default:
			{
				IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider3 = invariantCulture;
				handler = new DefaultInterpolatedStringHandler(4, 2, invariantCulture);
				handler.AppendLiteral("HP:");
				handler.AppendFormatted(current, "F3");
				handler.AppendLiteral("/");
				handler.AppendFormatted(maximum, "F3");
				return string.Create(provider3, ref handler);
			}
			}
		}
		string newValue = FormatValue(current, digits, roundHitpoints);
		string newValue2 = FormatValue(maximum, digits, roundHitpoints);
		return template.Replace("{0}", newValue).Replace("{1}", newValue2);
	}

	private static string FormatValue(double value, int digits, bool roundHitpoints)
	{
		if (roundHitpoints)
		{
			return Mathf.RoundToInt(value).ToString(CultureInfo.InvariantCulture);
		}
		return value.ToString($"F{digits}", CultureInfo.InvariantCulture);
	}

	private void ApplyConfiguredColors()
	{
		ApplyLabelColor(shieldHitpointLabel, shieldColor);
		ApplyLabelColor(helmetHitpointLabel, helmetColor);
		ApplyLabelColor(bodyHitpointLabel, bodyColor);
	}

	private static void ApplyLabelColor(Label label, Color color)
	{
		if (GodotObject.IsInstanceValid(label))
		{
			label.AddThemeColorOverride("font_color", color);
		}
	}

	private void ConnectVisibilitySignal()
	{
		if (!_visibilityConnected && GodotObject.IsInstanceValid(_viewHost))
		{
			_viewHost.VisibilityChanged += OnVisibilityChanged;
			_visibilityConnected = true;
		}
	}

	private void DisconnectVisibilitySignal()
	{
		if (_visibilityConnected && GodotObject.IsInstanceValid(_viewHost))
		{
			_viewHost.VisibilityChanged -= OnVisibilityChanged;
		}
		_visibilityConnected = false;
	}

	private void OnVisibilityChanged()
	{
		RefreshViewOwnership();
	}

	private void RefreshViewOwnership()
	{
		if (!IsDisplayVisible())
		{
			_refreshPending = false;
			_forceRefreshPending = false;
			DetachSharedDrawBatch();
			HideAllLabels();
			ReleaseView();
			RefreshPhysicsProcessEligibility();
		}
		else
		{
			if (!TryGetRuntime(out var _))
			{
				return;
			}
			if (CanUseSharedDrawBatch() && TowerDefenseHealthDisplayBatch.Attach(this))
			{
				_usingSharedDrawBatch = true;
				ConnectSharedDrawPositionSignals();
				ReleaseView();
				QueueRefresh(immediate: true);
			}
			else
			{
				DetachSharedDrawBatch();
				if (EnsureView())
				{
					QueueRefresh(immediate: true);
				}
			}
		}
	}

	private void DetachSharedDrawBatch()
	{
		if (_usingSharedDrawBatch)
		{
			_usingSharedDrawBatch = false;
			TowerDefenseHealthDisplayBatch.Detach(this);
		}
		DisconnectSharedDrawPositionSignals();
	}

	private void ConnectSharedDrawPositionSignals()
	{
		if (!_viewHostTransformConnected && _viewHost is TowerDefenseCharacterSpriteGroup trackedViewHost)
		{
			_trackedViewHost = trackedViewHost;
			_trackedViewHost.TransformChanged += OnSharedDrawPositionChanged;
			_viewHostTransformConnected = true;
		}
	}

	private void DisconnectSharedDrawPositionSignals()
	{
		if (_viewHostTransformConnected && GodotObject.IsInstanceValid(_trackedViewHost))
		{
			_trackedViewHost.TransformChanged -= OnSharedDrawPositionChanged;
		}
		_viewHostTransformConnected = false;
		_trackedViewHost = null;
	}

	private void OnSharedDrawPositionChanged()
	{
		if (_usingSharedDrawBatch)
		{
			TowerDefenseHealthDisplayBatch.NotifyPositionChanged(this);
		}
	}

	internal void NotifyLogicalPositionChanged()
	{
		if (IsDisplayVisible())
		{
			if (_usingSharedDrawBatch)
			{
				TowerDefenseHealthDisplayBatch.NotifyPositionChanged(this);
			}
			else
			{
				SyncViewAnchorPosition(parent?.showHealthOffset ?? Vector2.Zero);
			}
		}
	}

	private void QueueRefresh(bool immediate = false)
	{
		if (_refreshPending)
		{
			if (immediate)
			{
				_forceRefreshPending = true;
			}
		}
		else if (IsDisplayVisible())
		{
			if (immediate)
			{
				_forceRefreshPending = true;
			}
			_refreshPending = true;
			RefreshPhysicsProcessEligibility();
		}
	}

	private bool IsDisplayVisible()
	{
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && _visible && GodotObject.IsInstanceValid(_viewHost))
		{
			return _viewHost.IsVisibleInTree();
		}
		return false;
	}

	internal bool CanUseSharedDrawBatch()
	{
		if (keepScreenAligned && UsesDefaultViewScene())
		{
			return _viewHost is TowerDefenseCharacterSpriteGroup;
		}
		return false;
	}

	private bool UsesDefaultViewScene()
	{
		if (viewScene != null)
		{
			return string.Equals(viewScene.ResourcePath, "res://Script/Component/TowerDefense/Character/ShowHealthComponent/ShowHealthComponentView.tscn", StringComparison.Ordinal);
		}
		return false;
	}

	internal int ResolveSharedDrawZIndex()
	{
		return ResolveViewAnchorZIndex();
	}

	internal bool TryGetSharedDrawCenter(out Vector2 center)
	{
		center = default(Vector2);
		if (!_usingSharedDrawBatch || !IsDisplayVisible() || !GodotObject.IsInstanceValid(_viewHost))
		{
			return false;
		}
		Vector2 vector = parent?.showHealthOffset ?? Vector2.Zero;
		center = parent.GetLogicalGlobalTransform(_viewHost) * vector;
		return true;
	}

	internal bool TryGetSharedDrawData(out HealthDisplayDrawData data)
	{
		data = default(HealthDisplayDrawData);
		if (!TryGetSharedDrawCenter(out var center))
		{
			return false;
		}
		data = new HealthDisplayDrawData(center, _shieldDisplayText, _helmetDisplayText, _bodyDisplayText, shieldColor, helmetColor, bodyColor);
		return true;
	}

	private bool EnsureViewAnchor()
	{
		Vector2 vector = parent?.showHealthOffset ?? Vector2.Zero;
		bool flag = !keepScreenAligned || GodotObject.IsInstanceValid(_viewFollower);
		if (GodotObject.IsInstanceValid(_viewAnchor) && flag)
		{
			UpdateViewAnchorZIndex();
			if (keepScreenAligned)
			{
				_viewFollower.Position = vector;
				SyncViewAnchorPosition(vector);
			}
			else
			{
				_viewAnchor.Position = vector;
			}
			return true;
		}
		if (!GodotObject.IsInstanceValid(_viewHost))
		{
			return false;
		}
		ReleaseViewAnchor();
		_viewAnchor = new Node2D
		{
			Name = "ShowHealthViewAnchor",
			Position = (keepScreenAligned ? Vector2.Zero : vector),
			ZIndex = ResolveViewAnchorZIndex(),
			ZAsRelative = !keepScreenAligned,
			LightMask = 0,
			TopLevel = keepScreenAligned
		};
		_viewHost.AddChild(_viewAnchor, forceReadableName: false, Node.InternalMode.Disabled);
		if (keepScreenAligned)
		{
			_viewFollower = new RemoteTransform2D
			{
				Name = "ShowHealthViewFollower",
				Position = vector,
				UpdatePosition = true,
				UpdateRotation = false,
				UpdateScale = false,
				UseGlobalCoordinates = true
			};
			_viewHost.AddChild(_viewFollower, forceReadableName: false, Node.InternalMode.Disabled);
			_viewFollower.RemotePath = _viewFollower.GetPathTo(_viewAnchor);
			SyncViewAnchorPosition(vector);
		}
		return true;
	}

	private void UpdateViewAnchorZIndex()
	{
		if (GodotObject.IsInstanceValid(_viewAnchor))
		{
			_viewAnchor.ZAsRelative = !keepScreenAligned;
			_viewAnchor.ZIndex = ResolveViewAnchorZIndex();
		}
	}

	private int ResolveViewAnchorZIndex()
	{
		if (!keepScreenAligned)
		{
			return displayZIndex;
		}
		long num = displayZIndex;
		CanvasItem canvasItem = _viewHost;
		while (GodotObject.IsInstanceValid(canvasItem))
		{
			num += canvasItem.ZIndex;
			if (!canvasItem.ZAsRelative || canvasItem.TopLevel)
			{
				break;
			}
			canvasItem = canvasItem.GetParent() as CanvasItem;
		}
		return (int)Math.Clamp(num, -4096L, 4096L);
	}

	private void SyncViewAnchorPosition(Vector2 offset)
	{
		if (GodotObject.IsInstanceValid(_viewHost) && GodotObject.IsInstanceValid(_viewAnchor))
		{
			Vector2 vector = parent.GetLogicalGlobalTransform(_viewHost) * offset;
			if (GodotObject.IsInstanceValid(_viewFollower))
			{
				_viewFollower.GlobalPosition = vector;
			}
			_viewAnchor.GlobalTransform = new Transform2D(0f, Vector2.One, 0f, vector);
		}
	}

	private bool EnsureView()
	{
		if (GodotObject.IsInstanceValid(this.centerContainer))
		{
			return true;
		}
		ClearViewReferences();
		if (viewScene == null || !EnsureViewAnchor())
		{
			return false;
		}
		CenterContainer centerContainer = TryRentPooledView();
		if (!GodotObject.IsInstanceValid(centerContainer))
		{
			centerContainer = viewScene.Instantiate<CenterContainer>(PackedScene.GenEditState.Disabled);
			_viewFromPool = false;
		}
		if (!GodotObject.IsInstanceValid(centerContainer))
		{
			ReleaseViewAnchor();
			return false;
		}
		if (centerContainer.GetParent() != _viewAnchor)
		{
			_viewAnchor.AddChild(centerContainer, forceReadableName: false, Node.InternalMode.Disabled);
		}
		centerContainer.Visible = true;
		this.centerContainer = centerContainer;
		shieldHitpointLabel = centerContainer.GetNodeOrNull<Label>("VBoxContainer/ShieldHitpointLabel");
		helmetHitpointLabel = centerContainer.GetNodeOrNull<Label>("VBoxContainer/HelmetHitpointLabel");
		bodyHitpointLabel = centerContainer.GetNodeOrNull<Label>("VBoxContainer/BodyHitpointLabel");
		if (!GodotObject.IsInstanceValid(shieldHitpointLabel) || !GodotObject.IsInstanceValid(helmetHitpointLabel) || !GodotObject.IsInstanceValid(bodyHitpointLabel))
		{
			ReleaseView();
			return false;
		}
		ApplyConfiguredColors();
		_dirty = true;
		return true;
	}

	private void ReleaseView()
	{
		CenterContainer centerContainer = this.centerContainer;
		bool viewFromPool = _viewFromPool;
		_viewFromPool = false;
		ClearViewReferences();
		if (GodotObject.IsInstanceValid(centerContainer))
		{
			centerContainer.Visible = false;
			if (viewFromPool && CanUseDefaultViewPool())
			{
				if (GodotObject.IsInstanceValid(_viewHost) && centerContainer.GetParent() != _viewHost)
				{
					centerContainer.Reparent(_viewHost, keepGlobalTransform: false);
				}
				ObjectManager.PoolPush(ObjectManagerConfig.OBJECT.SHOW_HEALTH_VIEW, centerContainer);
			}
			else
			{
				centerContainer.Free();
			}
		}
		ReleaseViewAnchor();
		_dirty = true;
	}

	private void ReleaseViewAnchor()
	{
		if (GodotObject.IsInstanceValid(_viewFollower))
		{
			_viewFollower.Free();
		}
		_viewFollower = null;
		if (GodotObject.IsInstanceValid(_viewAnchor))
		{
			_viewAnchor.Free();
		}
		_viewAnchor = null;
	}

	private CenterContainer TryRentPooledView()
	{
		_viewFromPool = false;
		if (!CanUseDefaultViewPool() || !GodotObject.IsInstanceValid(_viewAnchor))
		{
			return null;
		}
		CenterContainer centerContainer = ObjectManager.PoolPop(ObjectManagerConfig.OBJECT.SHOW_HEALTH_VIEW, _viewAnchor) as CenterContainer;
		_viewFromPool = GodotObject.IsInstanceValid(centerContainer);
		return centerContainer;
	}

	private bool CanUseDefaultViewPool()
	{
		if (!GodotObject.IsInstanceValid(ObjectManager.Instance) || !UsesDefaultViewScene())
		{
			return false;
		}
		int num = 301;
		if (ObjectManager.Instance.poolList != null && num >= 0 && num < ObjectManager.Instance.poolList.Count)
		{
			return GodotObject.IsInstanceValid(ObjectManager.Instance.poolList[num]);
		}
		return false;
	}

	private void ClearViewReferences()
	{
		shieldHitpointLabel = null;
		helmetHitpointLabel = null;
		bodyHitpointLabel = null;
		centerContainer = null;
		_shieldLabelState = default(HealthLabelState);
		_helmetLabelState = default(HealthLabelState);
		_bodyLabelState = default(HealthLabelState);
		_shieldDisplayText = null;
		_helmetDisplayText = null;
		_bodyDisplayText = null;
	}

	private void HideAllLabels()
	{
		SetLabelVisible(shieldHitpointLabel, visible: false);
		SetLabelVisible(helmetHitpointLabel, visible: false);
		SetLabelVisible(bodyHitpointLabel, visible: false);
	}

	private static void SetLabelVisible(Label label, bool visible)
	{
		if (GodotObject.IsInstanceValid(label))
		{
			label.Visible = visible;
		}
	}

	private bool TryGetRuntime(out TowerDefenseCharacterInstance instance)
	{
		instance = (GodotObject.IsInstanceValid(parent) ? parent.instance : null);
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active)
		{
			return GodotObject.IsInstanceValid(instance);
		}
		return false;
	}
}
