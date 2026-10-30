using System.Collections.Generic;
using Godot;

[Tool]
public partial class TowerDefensePlantRichwood : TowerDefensePlant, IProjectileZone
{
	private static readonly StringName ChestProjectileName = new StringName("Chest");

	private FireComponent _fireComponent;

	private PointLight2D _light;

	private Rect2 _zoneRect;

	private readonly List<BulletFieldStoredProjectile> _projectileList = new List<BulletFieldStoredProjectile>(6);

	public Rect2 WorldRect => _zoneRect;

	public int GridY => base.gridPos.Y;

	public int RowSpan => 0;

	public override void BatchUpdate(double delta)
	{
		if (!Engine.IsEditorHint())
		{
			base.BatchUpdate(delta);
			_light.Visible = TowerDefenseManager.GetMapIsNight() && GameSaveManager.Instance.GetConfigValue("MapEffect").AsBool();
		}
	}

	private bool TryStoreProjectileConfig(TowerDefenseProjectileConfig projectileConfig, int sourceFireMethodFlags)
	{
		if (projectileConfig == null || IsRichwoodOutputConfig(projectileConfig))
		{
			return false;
		}
		_projectileList.Add(new BulletFieldStoredProjectile(projectileConfig, sourceFireMethodFlags));
		if (_projectileList.Count < 6)
		{
			return true;
		}
		BulletFieldStoredProjectile[] storedProjectiles = _projectileList.ToArray();
		_projectileList.Clear();
		Attack(storedProjectiles);
		return true;
	}

	private static bool IsRichwoodOutputConfig(TowerDefenseProjectileConfig projectileConfig)
	{
		if (projectileConfig != null)
		{
			return projectileConfig.NameSN == ChestProjectileName;
		}
		return false;
	}

	private static int GetStoredProjectileFireMethodFlags(int fireMethodFlags, bool trackOpen)
	{
		if (trackOpen)
		{
			fireMethodFlags |= 0x20;
		}
		return fireMethodFlags;
	}

	public void Attack(BulletFieldStoredProjectile[] storedProjectiles)
	{
		if (_fireComponent != null)
		{
			TowerDefenseProjectileCreateData projectileData = new TowerDefenseProjectileCreateData(ChestProjectileName)
			{
				baseDamage = 100.0
			};
			BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
			{
				gridYOverride = base.gridPos.Y,
				flipXOverride = (base.Scale.X < 0f),
				eventProjectiles = storedProjectiles
			};
			_fireComponent.CreateProjectileByData(0, new Vector2(600f, 0f), projectileData, -1, base.camp, Vector2.Zero, overrides);
		}
	}

	public override async void DestroySet()
	{
		if (!instance.hypnoses)
		{
			TowerDefenseCharacterEventGoldShardCreate towerDefenseCharacterEventGoldShardCreate = new TowerDefenseCharacterEventGoldShardCreate();
			towerDefenseCharacterEventGoldShardCreate.num = 1;
			towerDefenseCharacterEventGoldShardCreate.Execute(GetLogicalGlobalPosition(), this);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			_light = GetNode<PointLight2D>("%Light");
			if (BulletField.Instance != null)
			{
				BulletField.Instance.RegisterZone(this);
			}
		}
	}

	public override void _ExitTree()
	{
		if (BulletField.Instance != null)
		{
			BulletField.Instance.UnregisterZone(this);
		}
		base._ExitTree();
	}

	public void UpdateRect()
	{
		if (base.IsHitBoxEnabled)
		{
			_zoneRect = base.WorldHitRect;
		}
		else
		{
			_zoneRect = new Rect2(Vector2.Zero, Vector2.Zero);
		}
	}

	public void OnBulletIntersect(ref BulletData b, int index)
	{
		if (!die && !nearDie && base.componentAlive && b.active && b.config != null && !IsRichwoodOutputConfig(b.config) && b.camp == base.camp && (b.checkAll || b.gridPos.Y == base.gridPos.Y))
		{
			int storedProjectileFireMethodFlags = GetStoredProjectileFireMethodFlags(b.fireMethodFlags, b.trackOpen);
			if ((storedProjectileFireMethodFlags & 2) == 0 && TryStoreProjectileConfig(b.config, storedProjectileFireMethodFlags))
			{
				BulletField.Instance.Despawn(index);
			}
		}
	}

	public void OnProjectileIntersect(TowerDefenseProjectile projectile)
	{
	}
}
