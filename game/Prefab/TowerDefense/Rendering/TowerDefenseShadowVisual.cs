using Godot;

public sealed partial class TowerDefenseShadowVisual : Resource
{
	private const string BuiltInCharacterShadowScript = "res://Prefab/TowerDefense/Character/TowerDefenseCharacterShadowSprite.cs";

	private Node2D _owner;

	private Sprite2D _legacySprite;

	private bool _requiresLegacyRendering;

	private Texture2D _texture;

	private Vector2 _position;

	private Vector2 _scale = Vector2.One;

	private float _rotation;

	private float _skew;

	private Vector2 _offset;

	private bool _centered = true;

	private bool _flipH;

	private bool _flipV;

	private bool _visible = true;

	private Color _modulate = Colors.White;

	private Color _selfModulate = Colors.White;

	private int _zIndex;

	private bool _zAsRelative = true;

	private uint _visibilityLayer = 1u;

	private bool _topLevel;

	private ulong _revision = 1uL;

	internal Node2D OwnerNode => _owner;

	internal Sprite2D LegacySprite => _legacySprite;

	internal bool UsesLegacyNode => _legacySprite != null;

	internal bool RequiresLegacyRendering => _requiresLegacyRendering;

	internal ulong Revision
	{
		get
		{
			if (!(_legacySprite is TowerDefenseCharacterShadowSprite { LocalPoseReady: not false } towerDefenseCharacterShadowSprite))
			{
				return _revision;
			}
			return (_revision * 1099511628211L) ^ towerDefenseCharacterShadowSprite.LocalTransformRevision;
		}
	}

	public Texture2D Texture
	{
		get
		{
			return LegacySprite?.Texture ?? _texture;
		}
		set
		{
			Sprite2D legacySprite = LegacySprite;
			if (legacySprite != null)
			{
				legacySprite.Texture = value;
			}
			if (_texture != value)
			{
				_texture = value;
				MarkChanged();
			}
		}
	}

	public Vector2 Position
	{
		get
		{
			if (!(LegacySprite is TowerDefenseCharacterShadowSprite { LocalPoseReady: not false } towerDefenseCharacterShadowSprite))
			{
				return LegacySprite?.Position ?? _position;
			}
			return towerDefenseCharacterShadowSprite.CachedLocalPose.Position;
		}
		set
		{
			Sprite2D legacySprite = LegacySprite;
			if (legacySprite != null)
			{
				legacySprite.Position = value;
			}
			if (!_position.IsEqualApprox(value))
			{
				_position = value;
				MarkChanged();
			}
		}
	}

	public Vector2 GlobalPosition
	{
		get
		{
			return LegacySprite?.GlobalPosition ?? (GetOwnerGlobalTransform() * _position);
		}
		set
		{
			Sprite2D legacySprite = LegacySprite;
			if (legacySprite != null)
			{
				legacySprite.GlobalPosition = value;
				CaptureMutableState(legacySprite);
				MarkChanged();
			}
			else
			{
				Position = GetOwnerGlobalTransform().AffineInverse() * value;
			}
		}
	}

	public Vector2 Scale
	{
		get
		{
			if (!(LegacySprite is TowerDefenseCharacterShadowSprite { LocalPoseReady: not false } towerDefenseCharacterShadowSprite))
			{
				return LegacySprite?.Scale ?? _scale;
			}
			return towerDefenseCharacterShadowSprite.CachedLocalPose.Scale;
		}
		set
		{
			Sprite2D legacySprite = LegacySprite;
			if (legacySprite != null)
			{
				legacySprite.Scale = value;
			}
			if (!_scale.IsEqualApprox(value))
			{
				_scale = value;
				MarkChanged();
			}
		}
	}

	public float Rotation
	{
		get
		{
			if (!(LegacySprite is TowerDefenseCharacterShadowSprite { LocalPoseReady: not false } towerDefenseCharacterShadowSprite))
			{
				return LegacySprite?.Rotation ?? _rotation;
			}
			return towerDefenseCharacterShadowSprite.CachedLocalPose.Rotation;
		}
		set
		{
			Sprite2D legacySprite = LegacySprite;
			if (legacySprite != null)
			{
				legacySprite.Rotation = value;
			}
			if (!Mathf.IsEqualApprox(_rotation, value))
			{
				_rotation = value;
				MarkChanged();
			}
		}
	}

	public float Skew
	{
		get
		{
			if (!(LegacySprite is TowerDefenseCharacterShadowSprite { LocalPoseReady: not false } towerDefenseCharacterShadowSprite))
			{
				return LegacySprite?.Skew ?? _skew;
			}
			return towerDefenseCharacterShadowSprite.CachedLocalPose.Skew;
		}
		set
		{
			Sprite2D legacySprite = LegacySprite;
			if (legacySprite != null)
			{
				legacySprite.Skew = value;
			}
			if (!Mathf.IsEqualApprox(_skew, value))
			{
				_skew = value;
				MarkChanged();
			}
		}
	}

	public Transform2D Transform
	{
		get
		{
			if (!(LegacySprite is TowerDefenseCharacterShadowSprite { LocalPoseReady: not false } towerDefenseCharacterShadowSprite))
			{
				return LegacySprite?.Transform ?? new Transform2D(_rotation, _scale, _skew, _position);
			}
			return towerDefenseCharacterShadowSprite.CachedLocalTransform;
		}
		set
		{
			Sprite2D legacySprite = LegacySprite;
			if (legacySprite != null)
			{
				legacySprite.Transform = value;
			}
			_position = value.Origin;
			_scale = value.Scale;
			_rotation = value.Rotation;
			_skew = value.Skew;
			MarkChanged();
		}
	}

	public Transform2D GlobalTransform
	{
		get
		{
			return LegacySprite?.GlobalTransform ?? (GetOwnerGlobalTransform() * Transform);
		}
		set
		{
			Transform = GetOwnerGlobalTransform().AffineInverse() * value;
		}
	}

	public Vector2 Offset
	{
		get
		{
			return LegacySprite?.Offset ?? _offset;
		}
		set
		{
			Sprite2D legacySprite = LegacySprite;
			if (legacySprite != null)
			{
				legacySprite.Offset = value;
			}
			if (!_offset.IsEqualApprox(value))
			{
				_offset = value;
				MarkChanged();
			}
		}
	}

	public bool Centered
	{
		get
		{
			return LegacySprite?.Centered ?? _centered;
		}
		set
		{
			Sprite2D legacySprite = LegacySprite;
			if (legacySprite != null)
			{
				legacySprite.Centered = value;
			}
			if (_centered != value)
			{
				_centered = value;
				MarkChanged();
			}
		}
	}

	public bool FlipH
	{
		get
		{
			return LegacySprite?.FlipH ?? _flipH;
		}
		set
		{
			Sprite2D legacySprite = LegacySprite;
			if (legacySprite != null)
			{
				legacySprite.FlipH = value;
			}
			if (_flipH != value)
			{
				_flipH = value;
				MarkChanged();
			}
		}
	}

	public bool FlipV
	{
		get
		{
			return LegacySprite?.FlipV ?? _flipV;
		}
		set
		{
			Sprite2D legacySprite = LegacySprite;
			if (legacySprite != null)
			{
				legacySprite.FlipV = value;
			}
			if (_flipV != value)
			{
				_flipV = value;
				MarkChanged();
			}
		}
	}

	public bool Visible
	{
		get
		{
			return LegacySprite?.Visible ?? _visible;
		}
		set
		{
			Sprite2D legacySprite = LegacySprite;
			if (legacySprite != null)
			{
				legacySprite.Visible = value;
			}
			if (_visible != value)
			{
				_visible = value;
				MarkChanged();
			}
		}
	}

	public Color Modulate
	{
		get
		{
			return LegacySprite?.Modulate ?? _modulate;
		}
		set
		{
			Sprite2D legacySprite = LegacySprite;
			if (legacySprite != null)
			{
				legacySprite.Modulate = value;
			}
			if (!_modulate.IsEqualApprox(value))
			{
				_modulate = value;
				MarkChanged();
			}
		}
	}

	public Color SelfModulate
	{
		get
		{
			return LegacySprite?.SelfModulate ?? _selfModulate;
		}
		set
		{
			Sprite2D legacySprite = LegacySprite;
			if (legacySprite != null)
			{
				legacySprite.SelfModulate = value;
			}
			if (!_selfModulate.IsEqualApprox(value))
			{
				_selfModulate = value;
				MarkChanged();
			}
		}
	}

	public int ZIndex
	{
		get
		{
			return LegacySprite?.ZIndex ?? _zIndex;
		}
		set
		{
			Sprite2D legacySprite = LegacySprite;
			if (legacySprite != null)
			{
				legacySprite.ZIndex = value;
			}
			_zIndex = value;
		}
	}

	public bool ZAsRelative
	{
		get
		{
			return LegacySprite?.ZAsRelative ?? _zAsRelative;
		}
		set
		{
			Sprite2D legacySprite = LegacySprite;
			if (legacySprite != null)
			{
				legacySprite.ZAsRelative = value;
			}
			_zAsRelative = value;
		}
	}

	public uint VisibilityLayer
	{
		get
		{
			return LegacySprite?.VisibilityLayer ?? _visibilityLayer;
		}
		set
		{
			Sprite2D legacySprite = LegacySprite;
			if (legacySprite != null)
			{
				legacySprite.VisibilityLayer = value;
			}
			_visibilityLayer = value;
		}
	}

	public bool TopLevel
	{
		get
		{
			if (!(LegacySprite is TowerDefenseCharacterShadowSprite { LocalPoseReady: not false } towerDefenseCharacterShadowSprite))
			{
				return LegacySprite?.TopLevel ?? _topLevel;
			}
			return towerDefenseCharacterShadowSprite.CachedLocalPose.TopLevel;
		}
		set
		{
			Sprite2D legacySprite = LegacySprite;
			if (legacySprite != null)
			{
				legacySprite.TopLevel = value;
			}
			_topLevel = value;
			MarkChanged();
		}
	}

	public bool IsVisibleInTree()
	{
		Sprite2D legacySprite = LegacySprite;
		if (legacySprite == null)
		{
			if (_visible && GodotObject.IsInstanceValid(_owner))
			{
				return _owner.IsVisibleInTree();
			}
			return false;
		}
		return legacySprite.IsVisibleInTree();
	}

	public bool IsInsideTree()
	{
		return LegacySprite?.IsInsideTree() ?? false;
	}

	public Node GetParent()
	{
		return LegacySprite?.GetParent() ?? _owner;
	}

	public new NodePath GetPath()
	{
		return LegacySprite?.GetPath() ?? new NodePath("@ShadowData");
	}

	internal Color GetEffectiveColor()
	{
		Color color = Multiply(Modulate, SelfModulate);
		if (GodotObject.IsInstanceValid(_owner))
		{
			color = Multiply(color, Multiply(_owner.Modulate, _owner.SelfModulate));
		}
		return color;
	}

	internal static TowerDefenseShadowVisual Capture(Node2D owner, Sprite2D source, bool detachCompatibleNode = true)
	{
		if (!GodotObject.IsInstanceValid(source))
		{
			return null;
		}
		TowerDefenseShadowVisual towerDefenseShadowVisual = new TowerDefenseShadowVisual
		{
			_owner = owner
		};
		towerDefenseShadowVisual.CaptureAllState(source);
		bool flag = CanDetach(source);
		towerDefenseShadowVisual._requiresLegacyRendering = !flag;
		if (detachCompatibleNode && flag)
		{
			Node parent = source.GetParent();
			if (GodotObject.IsInstanceValid(parent))
			{
				parent.RemoveChild(source);
			}
			source.Free();
		}
		else
		{
			towerDefenseShadowVisual._legacySprite = source;
		}
		return towerDefenseShadowVisual;
	}

	internal static TowerDefenseShadowVisual Create(Node2D owner)
	{
		return new TowerDefenseShadowVisual
		{
			_owner = owner
		};
	}

	public static implicit operator TowerDefenseShadowVisual(Sprite2D sprite)
	{
		return Capture(sprite?.GetParent() as Node2D, sprite, detachCompatibleNode: false);
	}

	public static implicit operator Sprite2D(TowerDefenseShadowVisual visual)
	{
		return visual?.LegacySprite;
	}

	private static bool CanDetach(Sprite2D source)
	{
		if (source.GetChildCount() != 0)
		{
			return false;
		}
		if (source.TopLevel)
		{
			return false;
		}
		Variant script = source.GetScript();
		if (script.VariantType == Variant.Type.Nil)
		{
			return true;
		}
		if (!(script.AsGodotObject() is Script script2))
		{
			return false;
		}
		return script2.ResourcePath == "res://Prefab/TowerDefense/Character/TowerDefenseCharacterShadowSprite.cs";
	}

	private void CaptureAllState(Sprite2D source)
	{
		CaptureMutableState(source);
		_texture = source.Texture;
		_offset = source.Offset;
		_centered = source.Centered;
		_flipH = source.FlipH;
		_flipV = source.FlipV;
		_visible = source.Visible;
		_modulate = source.Modulate;
		_selfModulate = source.SelfModulate;
		_zIndex = source.ZIndex;
		_zAsRelative = source.ZAsRelative;
		_visibilityLayer = source.VisibilityLayer;
		_topLevel = source.TopLevel;
	}

	private void CaptureMutableState(Sprite2D source)
	{
		Transform2D transform = source.Transform;
		_position = transform.Origin;
		_scale = transform.Scale;
		_rotation = transform.Rotation;
		_skew = transform.Skew;
	}

	private Transform2D GetOwnerGlobalTransform()
	{
		if (!GodotObject.IsInstanceValid(_owner))
		{
			return Transform2D.Identity;
		}
		if (_owner is TowerDefenseCharacter towerDefenseCharacter)
		{
			ulong num = TowerDefenseProcessModeDispatch.CurrentPhysicsFrame;
			if (num == 18446744073709551615uL)
			{
				num = Engine.GetPhysicsFrames();
			}
			return towerDefenseCharacter.GetGlobalTransformForShadow(num);
		}
		return _owner.GlobalTransform;
	}

	private void MarkChanged()
	{
		_revision++;
		if (_revision == 0L)
		{
			_revision = 1uL;
		}
		EmitChanged();
	}

	private static Color Multiply(Color a, Color b)
	{
		return new Color(a.R * b.R, a.G * b.G, a.B * b.B, a.A * b.A);
	}
}
