using System;
using Godot;

[Tool]
[GlobalClass]
public partial class TowerDefenseGroundItemBase : Node2D
{
	public delegate void LandEventHandler();

	private static readonly bool DebugGridRenderOrderTrace;

	private const int DebugGridRenderOrderTraceLimit = 800;

	private static int _debugGridRenderOrderTraceCount;

	private double _groundHeight;

	private double _z;

	private Vector2I _gridPos = new Vector2I(-1, -1);

	private TowerDefenseEnum.LAYER_GROUNDITEM _itemLayer;

	[Export(PropertyHint.None, "")]
	public TowerDefenseCellInstance cell;

	[Export(PropertyHint.None, "")]
	public double cellPercentage = 0.5;

	[Export(PropertyHint.None, "")]
	public bool gravityUse = true;

	[Export(PropertyHint.None, "")]
	public double gravity = 245.0;

	[Export(PropertyHint.None, "")]
	public double gravityScale = 1.5;

	[Export(PropertyHint.None, "")]
	public double ySpeed;

	private bool _topLayer;

	public static Node2D characterNode;

	public bool isGround = true;

	private bool _insidePhysicsUpdate;

	[Export(PropertyHint.None, "")]
	public double groundHeight
	{
		get
		{
			return _groundHeight;
		}
		set
		{
			if (_groundHeight != value)
			{
				double oldGroundHeight = _groundHeight;
				_groundHeight = value;
				if (isGround && z != groundHeight)
				{
					z = groundHeight;
				}
				OnGroundHeightChanged(oldGroundHeight, value);
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public double z
	{
		get
		{
			return _z;
		}
		set
		{
			if (_z != value)
			{
				_z = value;
				SetZ();
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public Vector2I gridPos
	{
		get
		{
			return _gridPos;
		}
		set
		{
			if (_gridPos != value)
			{
				Vector2I oldGridPos = _gridPos;
				int zIndex = base.ZIndex;
				_gridPos = value;
				cell = TowerDefenseManager.GetMapCell(gridPos);
				FreshZIndex("gridPos", oldGridPos, zIndex);
				OnGridPositionChanged(oldGridPos, value);
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public TowerDefenseEnum.LAYER_GROUNDITEM itemLayer
	{
		get
		{
			return _itemLayer;
		}
		set
		{
			TowerDefenseEnum.LAYER_GROUNDITEM value2 = _itemLayer;
			int zIndex = base.ZIndex;
			_itemLayer = value;
			FreshZIndex($"itemLayer:{value2}->{value}", gridPos, zIndex);
		}
	}

	[Export(PropertyHint.None, "")]
	public bool topLayer
	{
		get
		{
			return _topLayer;
		}
		set
		{
			bool value2 = _topLayer;
			int zIndex = base.ZIndex;
			_topLayer = value;
			FreshZIndex($"topLayer:{value2}->{value}", gridPos, zIndex);
		}
	}

	protected bool ZChangedInsidePhysicsUpdate => _insidePhysicsUpdate;

	public event LandEventHandler OnLand;

	internal event Action<int> RenderZIndexChanged;

	protected virtual void OnGroundHeightChanged(double oldGroundHeight, double newGroundHeight)
	{
	}

	protected virtual void OnGridPositionChanged(Vector2I oldGridPos, Vector2I newGridPos)
	{
	}

	internal static void ClearStaticBattleReferences()
	{
		characterNode = null;
	}

	public override void _EnterTree()
	{
		if (!Engine.IsEditorHint())
		{
			characterNode = TowerDefenseManager.GetCharacterNode();
		}
	}

	public override void _Ready()
	{
		FreshZIndex();
	}

	public override void _PhysicsProcess(double delta)
	{
		PhysiceUpdate((float)delta);
	}

	public void PhysiceUpdate(float delta)
	{
		if (isGround && ySpeed >= 0.0 && z == groundHeight)
		{
			return;
		}
		_insidePhysicsUpdate = true;
		try
		{
			if (isGround && ySpeed >= 0.0)
			{
				if (z != groundHeight)
				{
					z = groundHeight;
				}
			}
			else if (z > groundHeight || ySpeed < 0.0)
			{
				isGround = false;
				if (gravityUse)
				{
					ySpeed += gravity * gravityScale * (double)delta;
				}
				z -= ySpeed * (double)delta;
				if (z <= groundHeight && ySpeed >= 0.0)
				{
					CompleteLanding();
				}
			}
			else
			{
				CompleteLanding();
			}
		}
		finally
		{
			_insidePhysicsUpdate = false;
		}
	}

	private void CompleteLanding()
	{
		this.OnLand?.Invoke();
		z = groundHeight;
		if (ySpeed >= 0.0)
		{
			ySpeed = 0.0;
			isGround = true;
		}
	}

	public virtual void SetZ()
	{
	}

	public void FreshZIndex()
	{
		FreshZIndex("", gridPos, base.ZIndex);
	}

	protected void FreshZIndexForGlobalPosition(Vector2 globalPosition, string reason)
	{
		int zIndex = base.ZIndex;
		int renderGridY = ResolveRenderGridY(globalPosition);
		ApplyRenderZIndex(renderGridY, reason, gridPos, zIndex);
	}

	protected void SetTransientRenderZIndex(int requestedZIndex, string reason)
	{
		int num = Math.Clamp(requestedZIndex, -4096, 4096);
		if (base.ZIndex != num)
		{
			base.ZIndex = num;
			this.RenderZIndexChanged?.Invoke(num);
			InvalidateDescendantAdobeAnimateRenderOrder(string.IsNullOrWhiteSpace(reason) ? "transient-z-index" : reason);
		}
	}

	private void FreshZIndex(string debugReason, Vector2I oldGridPos, int oldZIndex)
	{
		int renderGridY = ResolveRenderGridY();
		ApplyRenderZIndex(renderGridY, debugReason, oldGridPos, oldZIndex);
	}

	private void ApplyRenderZIndex(int renderGridY, string debugReason, Vector2I oldGridPos, int oldZIndex)
	{
		long value = (topLayer ? (1500L + (long)itemLayer) : ((long)renderGridY * 15L + (long)itemLayer));
		int num = (int)Math.Clamp(value, -4096L, 4096L);
		if (base.ZIndex != num)
		{
			base.ZIndex = num;
			this.RenderZIndexChanged?.Invoke(num);
			if (!string.IsNullOrEmpty(debugReason))
			{
				DebugPrintGridRenderOrderChange(debugReason, oldGridPos, oldZIndex, num, renderGridY, zChanged: true);
			}
			InvalidateDescendantAdobeAnimateRenderOrder(BuildGridRenderOrderReason(debugReason, oldGridPos, oldZIndex, num, renderGridY, zChanged: true));
		}
	}

	private int ResolveRenderGridY()
	{
		if (gridPos.Y >= 0)
		{
			return gridPos.Y;
		}
		if (GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			Vector2I mapGridPos = TowerDefenseManager.Instance.GetMapGridPos(base.GlobalPosition);
			if (mapGridPos.X >= 0 && mapGridPos.Y >= 0)
			{
				return mapGridPos.Y;
			}
		}
		return gridPos.Y;
	}

	private int ResolveRenderGridY(Vector2 globalPosition)
	{
		if (GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			Vector2I mapGridPos = TowerDefenseManager.Instance.GetMapGridPos(globalPosition);
			if (TowerDefenseManager.Instance.CheckMapGridPosIn(mapGridPos))
			{
				return mapGridPos.Y;
			}
		}
		return ResolveRenderGridY();
	}

	private void InvalidateDescendantAdobeAnimateRenderOrder(string reason)
	{
		foreach (Node child in GetChildren())
		{
			InvalidateDescendantAdobeAnimateRenderOrder(child, reason);
		}
	}

	private static void InvalidateDescendantAdobeAnimateRenderOrder(Node node, string reason)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return;
		}
		if (node is AdobeAnimateSprite adobeAnimateSprite)
		{
			adobeAnimateSprite.InvalidateInheritedRenderOrderForRender(reason);
		}
		foreach (Node child in node.GetChildren())
		{
			InvalidateDescendantAdobeAnimateRenderOrder(child, reason);
		}
	}

	private void MarkDescendantAdobeAnimateRenderOrderDebug(string reason)
	{
		foreach (Node child in GetChildren())
		{
			MarkDescendantAdobeAnimateRenderOrderDebug(child, reason);
		}
	}

	private static void MarkDescendantAdobeAnimateRenderOrderDebug(Node node, string reason)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return;
		}
		if (node is AdobeAnimateSprite adobeAnimateSprite)
		{
			adobeAnimateSprite.MarkRenderOrderDebugForRender(reason);
		}
		foreach (Node child in node.GetChildren())
		{
			MarkDescendantAdobeAnimateRenderOrderDebug(child, reason);
		}
	}

	private string BuildGridRenderOrderReason(string debugReason, Vector2I oldGridPos, int oldZIndex, int nextZIndex, int renderGridY, bool zChanged)
	{
		return $"{debugReason} ground={GetPathOrName(this)} grid={oldGridPos}->{gridPos} renderGridY={renderGridY} z={oldZIndex}->{nextZIndex} zChanged={zChanged}";
	}

	private void DebugPrintGridRenderOrderChange(string debugReason, Vector2I oldGridPos, int oldZIndex, int nextZIndex, int renderGridY, bool zChanged)
	{
		if (!DebugGridRenderOrderTrace || _debugGridRenderOrderTraceCount >= 800)
		{
			return;
		}
		int num = CountDescendantAdobeAnimateSprites(this);
		if (num > 0 && zChanged)
		{
			_debugGridRenderOrderTraceCount++;
			string value = "<no-manager>";
			if (GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
			{
				value = TowerDefenseManager.Instance.GetMapGridPos(base.GlobalPosition).ToString();
			}
			GD.Print($"[GridRenderOrderDebug] #{_debugGridRenderOrderTraceCount} reason={debugReason} path={GetPathOrName(this)} type={GetType().Name} grid={oldGridPos}->{gridPos} mapGridNow={value} renderGridY={renderGridY} itemLayer={itemLayer} topLayer={topLayer} z={oldZIndex}->{nextZIndex} currentZ={base.ZIndex} zChanged={zChanged} zRel={base.ZAsRelative} behind={base.ShowBehindParent} global=({base.GlobalPosition.X:F1},{base.GlobalPosition.Y:F1}) children={GetChildCount()} adobeChildren={num}");
		}
	}

	private static int CountDescendantAdobeAnimateSprites(Node node)
	{
		int num = 0;
		foreach (Node child in node.GetChildren())
		{
			if (child is AdobeAnimateSprite)
			{
				num++;
			}
			num += CountDescendantAdobeAnimateSprites(child);
		}
		return num;
	}

	private static string GetPathOrName(Node node)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return "<invalid>";
		}
		if (!node.IsInsideTree())
		{
			return node.Name.ToString();
		}
		return node.GetPath().ToString();
	}

	public double GetFallTime()
	{
		double num = Math.Abs(ySpeed) / (gravity * gravityScale);
		double num2 = gravity * gravityScale * num * num / 2.0 + (z - groundHeight);
		double num3 = Math.Sqrt(2.0 * num2 / (gravity * gravityScale));
		return num + num3;
	}

	public virtual void Collection()
	{
	}
}
