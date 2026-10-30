using System.Collections.Generic;
using Godot;

public sealed class CharacterAabbAreaComponent : CharacterComponentRuntime, IAabbAreaQuery2D
{
	private readonly List<AabbShape2DResource> _shapeResources = new List<AabbShape2DResource>();

	private Dictionary<int, Vector2> _runtimeRectangleSizes;

	private Dictionary<int, Transform2D> _runtimeShapeLocalTransforms;

	private NodePath _anchorPath = new NodePath();

	private Node2D _anchor;

	private bool _configured;

	private bool _enabled = true;

	private CharacterAabbAreaComponentDefinition Definition => base.ComponentDefinition as CharacterAabbAreaComponentDefinition;

	public bool Enabled
	{
		get
		{
			if (_enabled && base.Alive)
			{
				return base.Lifecycle == ComponentRuntimeLifecycle.Active;
			}
			return false;
		}
	}

	public uint CollisionLayer { get; set; } = 1u;

	public uint CollisionMask { get; set; } = 1u;

	public bool Monitoring { get; set; } = true;

	public Node RegistryOwner => base.Owner;

	public Transform2D LocalTransform { get; set; } = Transform2D.Identity;

	public int ShapeCount => _shapeResources.Count;

	protected override void OnBound()
	{
		ConfigureOnce();
		_anchor = null;
	}

	protected override void OnActivated()
	{
		AabbAreaLayerRegistry.Register(this);
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		AabbAreaLayerRegistry.Unregister(this);
		_anchor = null;
	}

	protected override void OnReleased()
	{
		AabbAreaLayerRegistry.Unregister(this);
		_anchor = null;
		_shapeResources.Clear();
		_runtimeRectangleSizes?.Clear();
		_runtimeRectangleSizes = null;
		_runtimeShapeLocalTransforms?.Clear();
		_runtimeShapeLocalTransforms = null;
	}

	public void SetEnabled(bool enabled)
	{
		_enabled = enabled;
	}

	public AabbShape2DResource GetShapeResource(int shapeIndex)
	{
		if (!TryGetShapeResource(shapeIndex, out var resource))
		{
			return null;
		}
		return resource;
	}

	public bool TryGetWorldRect(out Rect2 rect)
	{
		rect = default(Rect2);
		if (!Enabled || !TryGetAnchor(out var anchor))
		{
			return false;
		}
		bool flag = false;
		ulong physicsFrameForCachedGameplayQuery = TowerDefenseProcessModeDispatch.GetPhysicsFrameForCachedGameplayQuery();
		Transform2D transform2D = ((anchor == base.Owner) ? base.Owner.GetGlobalTransformForPhysicsFrame(physicsFrameForCachedGameplayQuery) : base.Owner.GetLogicalGlobalTransform(anchor)) * LocalTransform;
		for (int i = 0; i < _shapeResources.Count; i++)
		{
			if (TryGetShapeResource(i, out var resource) && resource.Enabled && TryGetRuntimeShapeLocalTransform(i, out var localTransform))
			{
				Transform2D transform = transform2D * localTransform;
				Rect2 rect2;
				bool flag2;
				if (resource.Geometry is RectangleShape2D && TryGetRuntimeRectangleSize(i, out var size))
				{
					rect2 = AabbShapeUtil.ComputeRectangleWorldRect(transform, size);
					flag2 = true;
				}
				else
				{
					flag2 = AabbShapeUtil.TryComputeShapeWorldRect(resource.Geometry, transform, out rect2);
				}
				if (flag2)
				{
					rect = (flag ? AabbShapeUtil.Union(rect, rect2) : rect2);
					flag = true;
				}
			}
		}
		return flag;
	}

	public bool SetRuntimeRectangleSize(int shapeIndex, Vector2 size)
	{
		if (!float.IsFinite(size.X) || !float.IsFinite(size.Y) || size.X < 0f || size.Y < 0f || !TryGetRectangleResource(shapeIndex, out var _))
		{
			return false;
		}
		if (_runtimeRectangleSizes == null)
		{
			_runtimeRectangleSizes = new Dictionary<int, Vector2>();
		}
		_runtimeRectangleSizes[shapeIndex] = size;
		return true;
	}

	public bool SetRuntimeRectangleWidth(int shapeIndex, float width)
	{
		if (!float.IsFinite(width) || width < 0f || !TryGetEffectiveRectangleSize(shapeIndex, out var size))
		{
			return false;
		}
		size.X = width;
		return SetRuntimeRectangleSize(shapeIndex, size);
	}

	public bool TryGetRuntimeRectangleSize(int shapeIndex, out Vector2 size)
	{
		if (_runtimeRectangleSizes != null && _runtimeRectangleSizes.TryGetValue(shapeIndex, out size))
		{
			return true;
		}
		size = default(Vector2);
		return false;
	}

	public bool ClearRuntimeRectangleSize(int shapeIndex)
	{
		if (_runtimeRectangleSizes == null || !_runtimeRectangleSizes.Remove(shapeIndex))
		{
			return false;
		}
		if (_runtimeRectangleSizes.Count == 0)
		{
			_runtimeRectangleSizes = null;
		}
		return true;
	}

	public bool SetRuntimeShapeLocalTransform(int shapeIndex, Transform2D localTransform)
	{
		if (!TryGetShapeResource(shapeIndex, out var _))
		{
			return false;
		}
		if (_runtimeShapeLocalTransforms == null)
		{
			_runtimeShapeLocalTransforms = new Dictionary<int, Transform2D>();
		}
		_runtimeShapeLocalTransforms[shapeIndex] = localTransform;
		return true;
	}

	public bool SetRuntimeShapeLocalOrigin(int shapeIndex, Vector2 localOrigin)
	{
		if (!TryGetRuntimeShapeLocalTransform(shapeIndex, out var localTransform))
		{
			return false;
		}
		localTransform.Origin = localOrigin;
		return SetRuntimeShapeLocalTransform(shapeIndex, localTransform);
	}

	public bool SetRuntimeShapeLocalOriginY(int shapeIndex, float localY)
	{
		if (!float.IsFinite(localY) || !TryGetRuntimeShapeLocalTransform(shapeIndex, out var localTransform))
		{
			return false;
		}
		localTransform.Origin = new Vector2(localTransform.Origin.X, localY);
		return SetRuntimeShapeLocalTransform(shapeIndex, localTransform);
	}

	public bool TryGetRuntimeShapeLocalTransform(int shapeIndex, out Transform2D localTransform)
	{
		if (!TryGetShapeResource(shapeIndex, out var resource))
		{
			localTransform = Transform2D.Identity;
			return false;
		}
		if (_runtimeShapeLocalTransforms != null && _runtimeShapeLocalTransforms.TryGetValue(shapeIndex, out localTransform))
		{
			return true;
		}
		localTransform = resource.LocalTransform;
		return true;
	}

	public bool ClearRuntimeShapeLocalTransform(int shapeIndex)
	{
		if (_runtimeShapeLocalTransforms == null || !_runtimeShapeLocalTransforms.Remove(shapeIndex))
		{
			return false;
		}
		if (_runtimeShapeLocalTransforms.Count == 0)
		{
			_runtimeShapeLocalTransforms = null;
		}
		return true;
	}

	private void ConfigureOnce()
	{
		if (_configured)
		{
			return;
		}
		CharacterAabbAreaComponentDefinition definition = Definition;
		if (definition == null)
		{
			return;
		}
		_enabled = definition.enabled;
		CollisionLayer = definition.collisionLayer;
		CollisionMask = definition.collisionMask;
		Monitoring = definition.monitoring;
		_anchorPath = definition.anchorPath ?? new NodePath();
		LocalTransform = definition.localTransform;
		_shapeResources.Clear();
		if (definition.shapeResources != null)
		{
			for (int i = 0; i < definition.shapeResources.Count; i++)
			{
				_shapeResources.Add(definition.shapeResources[i]);
			}
		}
		_configured = true;
	}

	private bool TryGetAnchor(out Node2D anchor)
	{
		anchor = null;
		if (!GodotObject.IsInstanceValid(base.Owner))
		{
			return false;
		}
		if (_anchorPath == null || _anchorPath.IsEmpty)
		{
			anchor = base.Owner;
			return true;
		}
		if (!GodotObject.IsInstanceValid(_anchor))
		{
			_anchor = base.Owner.GetNodeOrNull<Node2D>(_anchorPath);
		}
		anchor = _anchor;
		return GodotObject.IsInstanceValid(anchor);
	}

	private bool TryGetShapeResource(int shapeIndex, out AabbShape2DResource resource)
	{
		resource = null;
		if (shapeIndex < 0 || shapeIndex >= _shapeResources.Count)
		{
			return false;
		}
		resource = _shapeResources[shapeIndex];
		if (GodotObject.IsInstanceValid(resource))
		{
			return GodotObject.IsInstanceValid(resource.Geometry);
		}
		return false;
	}

	private bool TryGetRectangleResource(int shapeIndex, out RectangleShape2D rectangle)
	{
		rectangle = null;
		if (!TryGetShapeResource(shapeIndex, out var resource) || !(resource.Geometry is RectangleShape2D rectangleShape2D))
		{
			return false;
		}
		rectangle = rectangleShape2D;
		return true;
	}

	private bool TryGetEffectiveRectangleSize(int shapeIndex, out Vector2 size)
	{
		if (TryGetRuntimeRectangleSize(shapeIndex, out size))
		{
			return true;
		}
		if (TryGetRectangleResource(shapeIndex, out var rectangle))
		{
			size = rectangle.Size;
			return true;
		}
		size = default(Vector2);
		return false;
	}
}
