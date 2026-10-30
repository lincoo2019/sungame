using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefensePortal : TowerDefenseGroundItemBase
{
	private class PortalZone : IProjectileZone
	{
		private readonly TowerDefensePortal _portal;

		private readonly bool _isPortal1;

		private Rect2 _rect;

		public Rect2 WorldRect => _rect;

		public int GridY
		{
			get
			{
				if (!_isPortal1)
				{
					return _portal.gridPos2.Y;
				}
				return _portal.gridPos1.Y;
			}
		}

		public int RowSpan => 0;

		public PortalZone(TowerDefensePortal portal, bool isPortal1)
		{
			_portal = portal;
			_isPortal1 = isPortal1;
		}

		public void UpdateRect()
		{
			AabbArea2D aabbArea2D = (_isPortal1 ? _portal.hitBox1 : _portal.hitBox2);
			if (!GodotObject.IsInstanceValid(aabbArea2D) || !AabbShapeUtil.TryComputeAreaWorldRect(aabbArea2D, out _rect))
			{
				_rect = new Rect2(Vector2.Zero, Vector2.Zero);
			}
		}

		public void OnBulletIntersect(ref BulletData b, int index)
		{
			_portal.TeleportBulletData(ref b, index, _isPortal1);
		}

		public void OnProjectileIntersect(TowerDefenseProjectile projectile)
		{
			_portal.TeleportProjectile(projectile, _isPortal1);
		}
	}

	private static PackedScene _portalCircle;

	private static PackedScene _portalSquare;

	private static PackedScene _portalRhombus;

	public Node2D protalNode1;

	public Node2D protalNode2;

	public AabbArea2D hitBox1;

	public AabbArea2D hitBox2;

	public Vector4I posRange;

	public AdobeAnimateSprite protalSprite1;

	public AdobeAnimateSprite protalSprite2;

	public Vector2I gridPos1;

	public Vector2I gridPos2;

	public string shape = "";

	public double changeTime;

	public double changeTimer;

	public bool isChange;

	public Array<TowerDefenseGroundItemBase> exclude = new Array<TowerDefenseGroundItemBase>();

	private readonly HashSet<TowerDefenseGroundItemBase> _portal1Overlaps = new HashSet<TowerDefenseGroundItemBase>();

	private readonly HashSet<TowerDefenseGroundItemBase> _portal2Overlaps = new HashSet<TowerDefenseGroundItemBase>();

	private readonly HashSet<TowerDefenseGroundItemBase> _portal1Scratch = new HashSet<TowerDefenseGroundItemBase>();

	private readonly HashSet<TowerDefenseGroundItemBase> _portal2Scratch = new HashSet<TowerDefenseGroundItemBase>();

	private readonly HashSet<int> _bulletExclude = new HashSet<int>();

	public Vector2 gridSize;

	private PortalZone _zone1;

	private PortalZone _zone2;

	private bool _zonesRegistered;

	private readonly TaskCompletionSource<bool> _exitWaiter = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

	private static PackedScene PortalCircle => _portalCircle ?? (_portalCircle = GD.Load<PackedScene>("uid://duxojv3j24ulx"));

	private static PackedScene PortalSquare => _portalSquare ?? (_portalSquare = GD.Load<PackedScene>("uid://c0h2t4iq8wbd"));

	private static PackedScene PortalRhombus => _portalRhombus ?? (_portalRhombus = GD.Load<PackedScene>("uid://du34u3ii3ujh7"));

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			protalNode1 = GetNode<Node2D>("%ProtalNode1");
			protalNode2 = GetNode<Node2D>("%ProtalNode2");
			hitBox1 = GetNode<AabbArea2D>("%HitBox1");
			hitBox2 = GetNode<AabbArea2D>("%HitBox2");
			AddToGroup("Portal");
			base.gridPos = new Vector2I(base.gridPos.X, 0);
			gridSize = TowerDefenseManager.Instance.GetMapGridSize();
			_zone1 = new PortalZone(this, isPortal1: true);
			_zone2 = new PortalZone(this, isPortal1: false);
		}
	}

	public override void _ExitTree()
	{
		_exitWaiter.TrySetResult(result: true);
		isChange = false;
		if (_zonesRegistered && GodotObject.IsInstanceValid(BulletField.Instance))
		{
			BulletField.Instance.UnregisterZone(_zone1);
			BulletField.Instance.UnregisterZone(_zone2);
			_zonesRegistered = false;
		}
		_bulletExclude.Clear();
		_portal1Overlaps.Clear();
		_portal2Overlaps.Clear();
		_portal1Scratch.Clear();
		_portal2Scratch.Clear();
		base._ExitTree();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!_zonesRegistered && GodotObject.IsInstanceValid(BulletField.Instance))
		{
			BulletField.Instance.RegisterZone(_zone1);
			BulletField.Instance.RegisterZone(_zone2);
			_zonesRegistered = true;
		}
		RefreshProjectileExitGuards();
		ProcessPortalAreas();
		if (changeTime > 0.0 && (!Global.IsMultiplayerMode || MultiPlayerManager.IsHost))
		{
			if (changeTimer < changeTime)
			{
				changeTimer += delta;
				return;
			}
			changeTimer -= changeTime;
			ChangePos();
		}
	}

	private void RefreshProjectileExitGuards()
	{
		BulletField bulletField = (GodotObject.IsInstanceValid(BulletField.Instance) ? BulletField.Instance : null);
		if (_bulletExclude.Count > 0)
		{
			if (bulletField == null)
			{
				_bulletExclude.Clear();
			}
			else
			{
				_bulletExclude.RemoveWhere((int index) => !bulletField.IsBulletActive(index) || !IsBulletInsideEitherPortal(bulletField, index));
			}
		}
		for (int num = exclude.Count - 1; num >= 0; num--)
		{
			if (exclude[num] is TowerDefenseProjectile towerDefenseProjectile && (!GodotObject.IsInstanceValid(towerDefenseProjectile) || towerDefenseProjectile.over || towerDefenseProjectile.hitOver || !IsProjectileInsideEitherPortal(towerDefenseProjectile)))
			{
				exclude.RemoveAt(num);
			}
		}
	}

	private bool IsBulletInsideEitherPortal(BulletField bulletField, int index)
	{
		if (!TryGetPortalWorldRect(hitBox1, out var worldRect) || !bulletField.IsBulletIntersectingRect(index, worldRect))
		{
			if (TryGetPortalWorldRect(hitBox2, out var worldRect2))
			{
				return bulletField.IsBulletIntersectingRect(index, worldRect2);
			}
			return false;
		}
		return true;
	}

	private bool IsProjectileInsideEitherPortal(TowerDefenseProjectile projectile)
	{
		Rect2 worldHitRect = projectile.WorldHitRect;
		if (!TryGetPortalWorldRect(hitBox1, out var worldRect) || !AabbShapeUtil.Intersects(worldHitRect, worldRect))
		{
			if (TryGetPortalWorldRect(hitBox2, out var worldRect2))
			{
				return AabbShapeUtil.Intersects(worldHitRect, worldRect2);
			}
			return false;
		}
		return true;
	}

	private static bool TryGetPortalWorldRect(AabbArea2D hitBox, out Rect2 worldRect)
	{
		worldRect = default(Rect2);
		if (GodotObject.IsInstanceValid(hitBox))
		{
			return AabbShapeUtil.TryComputeAreaWorldRect(hitBox, out worldRect);
		}
		return false;
	}

	public void Init(string _shape, Vector4I _posRange, double _changeTime = 0.0)
	{
		posRange = _posRange;
		changeTime = Mathf.Max(0.0, _changeTime);
		shape = _shape;
		switch (shape)
		{
		case "Circle":
			protalSprite1 = PortalCircle.Instantiate<AdobeAnimateSprite>(PackedScene.GenEditState.Disabled);
			protalSprite2 = PortalCircle.Instantiate<AdobeAnimateSprite>(PackedScene.GenEditState.Disabled);
			break;
		case "Square":
			protalSprite1 = PortalSquare.Instantiate<AdobeAnimateSprite>(PackedScene.GenEditState.Disabled);
			protalSprite2 = PortalSquare.Instantiate<AdobeAnimateSprite>(PackedScene.GenEditState.Disabled);
			break;
		case "Rhombus":
			protalSprite1 = PortalRhombus.Instantiate<AdobeAnimateSprite>(PackedScene.GenEditState.Disabled);
			protalSprite2 = PortalRhombus.Instantiate<AdobeAnimateSprite>(PackedScene.GenEditState.Disabled);
			break;
		}
		protalSprite1.SetAnimation("Appear", loop: false, 0.2);
		protalSprite1.AddAnimation("Pulse", 0.0, loop: true, 0.2);
		protalNode1.AddChild(protalSprite1, forceReadableName: false, InternalMode.Disabled);
		protalSprite2.SetAnimation("Appear", loop: false, 0.2);
		protalSprite2.AddAnimation("Pulse", 0.0, loop: true, 0.2);
		protalNode2.AddChild(protalSprite2, forceReadableName: false, InternalMode.Disabled);
		ChangePos(isInit: true);
	}

	public void ChangePos(bool isInit = false)
	{
		if (!isChange && IsInsideTree() && (!Global.IsMultiplayerMode || MultiPlayerManager.IsHost))
		{
			ObserveChangePosAsync(isInit);
		}
	}

	private async Task ObserveChangePosAsync(bool isInit)
	{
		try
		{
			await ChangePosAsync(isInit);
		}
		catch (Exception value)
		{
			GD.PushError($"[Portal] ChangePos failed: {value}");
		}
		finally
		{
			isChange = false;
		}
	}

	private async Task ChangePosAsync(bool isInit)
	{
		if (isChange || !IsInsideTree())
		{
			return;
		}
		AudioManager.Instance.AudioPlay("Portal");
		isChange = true;
		TaskCompletionSource<bool> tcs;
		if (!isInit)
		{
			protalSprite1.SetAnimation("Dissappar");
			protalSprite2.SetAnimation("Dissappar");
			tcs = new TaskCompletionSource<bool>();
			protalSprite1.OnAnimeCompleted += Handler;
			Task task;
			try
			{
				task = await Task.WhenAny(tcs.Task, _exitWaiter.Task);
			}
			finally
			{
				if (GodotObject.IsInstanceValid(protalSprite1))
				{
					protalSprite1.OnAnimeCompleted -= Handler;
				}
			}
			if (task != tcs.Task || !IsInsideTree())
			{
				return;
			}
		}
		HashSet<Vector2I> hashSet = new HashSet<Vector2I>();
		foreach (Node item2 in GetTree().GetNodesInGroup("Portal"))
		{
			TowerDefensePortal towerDefensePortal = item2 as TowerDefensePortal;
			if (GodotObject.IsInstanceValid(towerDefensePortal) && towerDefensePortal != this)
			{
				hashSet.Add(towerDefensePortal.gridPos1);
				hashSet.Add(towerDefensePortal.gridPos2);
			}
		}
		List<Vector2I> list = new List<Vector2I>();
		int num = Mathf.Min(posRange.X, posRange.Z);
		int num2 = Mathf.Max(posRange.X, posRange.Z);
		int num3 = Mathf.Min(posRange.Y, posRange.W);
		int num4 = Mathf.Max(posRange.Y, posRange.W);
		for (int i = num; i <= num2; i++)
		{
			for (int j = num3; j <= num4; j++)
			{
				Vector2I item = new Vector2I(i, j);
				if (!hashSet.Contains(item))
				{
					list.Add(item);
				}
			}
		}
		if (list.Count < 2)
		{
			GD.PushWarning($"[Portal] Position range {posRange} has fewer than two free cells.");
			return;
		}
		int index = (int)(GD.Randi() % (uint)list.Count);
		gridPos1 = list[index];
		list.RemoveAt(index);
		gridPos2 = list[(int)(GD.Randi() % (uint)list.Count)];
		protalNode1.GlobalPosition = TowerDefenseManager.GetMapCellPlantPos(gridPos1) + new Vector2(gridSize.X / 2f, 0f);
		protalNode2.GlobalPosition = TowerDefenseManager.GetMapCellPlantPos(gridPos2) + new Vector2(gridSize.X / 2f, 0f);
		protalSprite1.SetAnimation("Appear", loop: false, 0.2);
		protalSprite1.AddAnimation("Pulse", 0.0, loop: true, 0.2);
		protalSprite2.SetAnimation("Appear", loop: false, 0.2);
		protalSprite2.AddAnimation("Pulse", 0.0, loop: true, 0.2);
		protalSprite1.ZIndex = gridPos1.Y * 15;
		protalSprite2.ZIndex = gridPos2.Y * 15;
		void Handler(string clip)
		{
			tcs.TrySetResult(result: true);
		}
	}

	private void ProcessPortalAreas()
	{
		bool flag = GodotObject.IsInstanceValid(hitBox1) && hitBox1.ProcessMode != ProcessModeEnum.Disabled;
		bool flag2 = GodotObject.IsInstanceValid(hitBox2) && hitBox2.ProcessMode != ProcessModeEnum.Disabled;
		if (!flag)
		{
			_portal1Overlaps.Clear();
		}
		if (!flag2)
		{
			_portal2Overlaps.Clear();
		}
		if (!flag && !flag2)
		{
			return;
		}
		Node2D node2D = TowerDefenseManager.GetCharacterNode();
		if (!GodotObject.IsInstanceValid(node2D))
		{
			return;
		}
		Rect2 a = (flag ? AabbShapeUtil.ComputeAreaWorldRect(hitBox1) : default(Rect2));
		Rect2 a2 = (flag2 ? AabbShapeUtil.ComputeAreaWorldRect(hitBox2) : default(Rect2));
		_portal1Scratch.Clear();
		_portal2Scratch.Clear();
		foreach (Node child in node2D.GetChildren())
		{
			if (!(child is TowerDefenseGroundItemBase towerDefenseGroundItemBase) || towerDefenseGroundItemBase == this || !GodotObject.IsInstanceValid(towerDefenseGroundItemBase) || towerDefenseGroundItemBase is TowerDefenseProjectile)
			{
				continue;
			}
			Rect2 b = ComputeGroundItemRect(towerDefenseGroundItemBase);
			if (flag && AabbShapeUtil.Intersects(a, b))
			{
				_portal1Scratch.Add(towerDefenseGroundItemBase);
				if (!_portal1Overlaps.Contains(towerDefenseGroundItemBase))
				{
					TryTeleport(towerDefenseGroundItemBase, fromPortal1: true);
				}
				b = ComputeGroundItemRect(towerDefenseGroundItemBase);
			}
			if (flag2 && AabbShapeUtil.Intersects(a2, b))
			{
				_portal2Scratch.Add(towerDefenseGroundItemBase);
				if (!_portal2Overlaps.Contains(towerDefenseGroundItemBase))
				{
					TryTeleport(towerDefenseGroundItemBase, fromPortal1: false);
				}
			}
		}
		UpdateOverlaps(_portal1Overlaps, _portal1Scratch, flag);
		UpdateOverlaps(_portal2Overlaps, _portal2Scratch, flag2);
	}

	private static void UpdateOverlaps(HashSet<TowerDefenseGroundItemBase> currentOverlaps, HashSet<TowerDefenseGroundItemBase> scratch, bool active)
	{
		if (!active)
		{
			currentOverlaps.Clear();
			return;
		}
		currentOverlaps.RemoveWhere((TowerDefenseGroundItemBase item) => !GodotObject.IsInstanceValid(item) || !scratch.Contains(item));
		currentOverlaps.UnionWith(scratch);
	}

	private Rect2 ComputeGroundItemRect(TowerDefenseGroundItemBase groundItem)
	{
		if (groundItem is TowerDefenseCharacter towerDefenseCharacter)
		{
			return towerDefenseCharacter.WorldHitRect;
		}
		if (groundItem is TowerDefenseProjectile towerDefenseProjectile && GodotObject.IsInstanceValid(towerDefenseProjectile.hitBox))
		{
			return AabbShapeUtil.ComputeAreaWorldRect(towerDefenseProjectile.hitBox);
		}
		return AabbShapeUtil.RectFromCenter(groundItem.GlobalPosition, gridSize);
	}

	private void TryTeleport(TowerDefenseGroundItemBase groundItem, bool fromPortal1)
	{
		Vector2I vector2I = (fromPortal1 ? gridPos1 : gridPos2);
		Vector2I vector2I2 = (fromPortal1 ? gridPos2 : gridPos1);
		Node2D node2D = (fromPortal1 ? protalNode2 : protalNode1);
		if (groundItem.gridPos.Y != vector2I.Y)
		{
			return;
		}
		if (exclude.Contains(groundItem))
		{
			exclude.Remove(groundItem);
		}
		else if ((!(groundItem is TowerDefensePlant) || groundItem is TowerDefensePlantBowlingBase) && (!(groundItem is TowerDefenseItem) || groundItem is TowerDefenseMower) && !(groundItem is TowerDefenseCrater) && !(groundItem is TowerDefenseGravestone))
		{
			if (groundItem is TowerDefenseCharacter towerDefenseCharacter)
			{
				Vector2 logicalGlobalPosition = towerDefenseCharacter.GetLogicalGlobalPosition();
				towerDefenseCharacter.shadowComponent.saveShadowPosition = new Vector2(towerDefenseCharacter.shadowComponent.saveShadowPosition.X, towerDefenseCharacter.shadowComponent.saveShadowPosition.Y + (node2D.GlobalPosition.Y - logicalGlobalPosition.Y));
				towerDefenseCharacter.SetLogicalGlobalPosition(node2D.GlobalPosition);
			}
			else if (groundItem is TowerDefenseProjectile towerDefenseProjectile)
			{
				towerDefenseProjectile.GlobalPosition = new Vector2(node2D.GlobalPosition.X, towerDefenseProjectile.GlobalPosition.Y + gridSize.Y * (float)(vector2I2.Y - towerDefenseProjectile.gridPos.Y));
			}
			else
			{
				groundItem.GlobalPosition = node2D.GlobalPosition;
			}
			groundItem.gridPos = vector2I2;
			exclude.Add(groundItem);
		}
	}

	public void TeleportBulletData(ref BulletData b, int index, bool fromPortal1)
	{
		Vector2I vector2I = (fromPortal1 ? gridPos1 : gridPos2);
		Vector2I vector2I2 = (fromPortal1 ? gridPos2 : gridPos1);
		Node2D node2D = (fromPortal1 ? protalNode2 : protalNode1);
		if (b.gridY == vector2I.Y && !_bulletExclude.Contains(index))
		{
			Vector2 newPos = new Vector2(node2D.GlobalPosition.X, b.pos.Y + gridSize.Y * (float)(vector2I2.Y - b.gridY));
			BulletField.Instance.TeleportBullet(index, newPos, vector2I2.Y);
			_bulletExclude.Add(index);
		}
	}

	public void TeleportProjectile(TowerDefenseProjectile projectile, bool fromPortal1)
	{
		if (GodotObject.IsInstanceValid(projectile) && !projectile.over && !projectile.hitOver)
		{
			Vector2I vector2I = (fromPortal1 ? gridPos1 : gridPos2);
			Vector2I vector2I2 = (fromPortal1 ? gridPos2 : gridPos1);
			Node2D node2D = (fromPortal1 ? protalNode2 : protalNode1);
			if (projectile.gridPos.Y == vector2I.Y && !exclude.Contains(projectile))
			{
				projectile.GlobalPosition = new Vector2(node2D.GlobalPosition.X, projectile.GlobalPosition.Y + gridSize.Y * (float)(vector2I2.Y - projectile.gridPos.Y));
				projectile.gridPos = vector2I2;
				exclude.Add(projectile);
			}
		}
	}
}
