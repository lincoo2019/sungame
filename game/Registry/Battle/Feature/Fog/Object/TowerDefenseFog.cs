using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class TowerDefenseFog : Node2D
{
	private const uint LightCollisionLayerMask = 8u;

	public Sprite2D sprite;

	public AabbArea2D area;

	public CollisionShape2D shape;

	public bool canVisible = true;

	public int beginColumn = 6;

	public Vector2 gridPos = Vector2.Zero;

	public Vector2 savePos;

	public bool lightOverlapEnabled = true;

	private bool _hasLightOverlap;

	private bool _batchManaged;

	private TowerDefenseFogBatch _batch;

	internal bool IsBatchManaged => _batchManaged;

	public bool IsLit => _hasLightOverlap;

	public override void _Ready()
	{
		sprite = GetNode<Sprite2D>("%Sprite");
		area = GetNode<AabbArea2D>("%Area");
		shape = GetNode<CollisionShape2D>("%Shape");
		savePos = base.GlobalPosition;
		Vector2 mapGridSize = TowerDefenseManager.Instance.GetMapGridSize();
		shape.Shape = shape.Shape.Duplicate(deep: true) as Shape2D;
		if (shape.Shape is RectangleShape2D rectangleShape2D)
		{
			rectangleShape2D.Size = mapGridSize;
		}
		sprite.Frame = GD.RandRange(0, 7);
		if (!lightOverlapEnabled)
		{
			Color modulate = sprite.Modulate;
			float a = (canVisible ? 1f : 0f);
			sprite.Modulate = new Color(modulate.R, modulate.G, modulate.B, a);
			SetPhysicsProcess(enable: false);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!lightOverlapEnabled)
		{
			SetPhysicsProcess(enable: false);
			return;
		}
		if (_batchManaged)
		{
			SetPhysicsProcess(enable: false);
			return;
		}
		IReadOnlyList<AabbAreaLayerRegistry.WorldRectSnapshot> worldRectSnapshotsForCollisionLayer = AabbAreaLayerRegistry.GetWorldRectSnapshotsForCollisionLayer(TowerDefenseManager.GetCharacterNode(), 8u);
		UpdateFogState(delta, worldRectSnapshotsForCollisionLayer);
	}

	internal void SetBatchManaged(bool managed)
	{
		_batchManaged = managed;
		if (!managed)
		{
			_batch = null;
		}
		SetPhysicsProcess(!managed);
	}

	internal void BindBatch(TowerDefenseFogBatch batch)
	{
		_batch = batch;
	}

	internal bool BatchPhysicsUpdate(double delta, IReadOnlyList<AabbAreaLayerRegistry.WorldRectSnapshot> lightAreas)
	{
		if (!_batchManaged)
		{
			return true;
		}
		return UpdateFogState(delta, lightAreas);
	}

	private bool UpdateFogState(double delta, IReadOnlyList<AabbAreaLayerRegistry.WorldRectSnapshot> lightAreas)
	{
		_hasLightOverlap = lightOverlapEnabled && HasLightOverlap(lightAreas);
		float num = ((!canVisible || _hasLightOverlap) ? 0f : ((beginColumn == (int)gridPos.X) ? 0.75f : 1f));
		Color modulate = sprite.Modulate;
		float num2 = Mathf.Lerp(modulate.A, num, (float)delta * 2f);
		bool num3 = Mathf.Abs(num2 - num) <= 0.001f;
		if (num3)
		{
			num2 = num;
		}
		if (!Mathf.IsEqualApprox(modulate.A, num2))
		{
			sprite.Modulate = new Color(modulate.R, modulate.G, modulate.B, num2);
		}
		return num3;
	}

	public void SetCanVisible(bool visible)
	{
		if (canVisible != visible)
		{
			canVisible = visible;
			if (!lightOverlapEnabled && GodotObject.IsInstanceValid(sprite))
			{
				Color modulate = sprite.Modulate;
				float a = (visible ? 1f : 0f);
				sprite.Modulate = new Color(modulate.R, modulate.G, modulate.B, a);
				SetPhysicsProcess(enable: false);
			}
			else if (_batchManaged)
			{
				_batch?.RequestVisibilityUpdate();
			}
		}
	}

	public void SetMagicColor(Color rgb)
	{
		if (GodotObject.IsInstanceValid(sprite))
		{
			Color modulate = sprite.Modulate;
			sprite.Modulate = new Color(rgb.R, rgb.G, rgb.B, modulate.A);
		}
	}

	public Color GetMagicColor()
	{
		if (!GodotObject.IsInstanceValid(sprite))
		{
			return Colors.White;
		}
		Color modulate = sprite.Modulate;
		return new Color(modulate.R, modulate.G, modulate.B);
	}

	private bool HasLightOverlap(IReadOnlyList<AabbAreaLayerRegistry.WorldRectSnapshot> lightAreas)
	{
		if (!GodotObject.IsInstanceValid(area))
		{
			return false;
		}
		if (!area.TryGetWorldRect(out var rect))
		{
			return false;
		}
		for (int i = 0; i < lightAreas.Count; i++)
		{
			AabbAreaLayerRegistry.WorldRectSnapshot worldRectSnapshot = lightAreas[i];
			if (AabbShapeUtil.Intersects(rect, worldRectSnapshot.WorldRect))
			{
				return true;
			}
		}
		return false;
	}
}
