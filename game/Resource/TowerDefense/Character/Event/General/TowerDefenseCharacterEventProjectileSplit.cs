using System;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseCharacterEventProjectileSplit : TowerDefenseCharacterEventBase
{
	[Export(PropertyHint.None, "")]
	public TowerDefenseProjectileCreateData projectileData;

	[Export(PropertyHint.None, "")]
	public Array<int> lineOffsets = new Array<int> { -1, 0, 1 };

	[Export(PropertyHint.None, "")]
	public double speed = 300.0;

	[Export(PropertyHint.None, "")]
	public double forwardOffset = 30.0;

	[Export(PropertyHint.None, "")]
	public bool useTargetGridY = true;

	[Export(PropertyHint.None, "")]
	public bool enemyOnly = true;

	[Export(PropertyHint.None, "")]
	public bool edgeCompensate = true;

	[Export(PropertyHint.None, "")]
	public PackedScene effectScene;

	public string projectileName
	{
		get
		{
			if (projectileData == null)
			{
				return "";
			}
			return projectileData.projectileName.ToString();
		}
	}

	public override void ExecuteProject(TowerDefenseProjectile projectile, TowerDefenseCharacter target)
	{
		Run(projectile, target, projectileData, lineOffsets, speed, forwardOffset, useTargetGridY, enemyOnly, edgeCompensate, effectScene);
	}

	public static void Run(TowerDefenseProjectile projectile, TowerDefenseCharacter target, TowerDefenseProjectileCreateData _projectileData, Array<int> _lineOffsets, double _speed, double _forwardOffset, bool _useTargetGridY, bool _enemyOnly = true, bool _edgeCompensate = true, PackedScene _effectScene = null)
	{
		if (_projectileData == null || _lineOffsets == null || _lineOffsets.Count == 0)
		{
			return;
		}
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance) || !GodotObject.IsInstanceValid(target) || (_enemyOnly && !IsEnemyTarget(projectile, target)))
		{
			return;
		}
		float num = ResolveDirSign(projectile, target);
		Vector2 vector;
		int x;
		int y;
		double height;
		int collisionFlags;
		TowerDefenseEnum.CHARACTER_CAMP camp;
		if (GodotObject.IsInstanceValid(projectile))
		{
			vector = projectile.GlobalPosition;
			x = projectile.gridPos.X;
			y = projectile.gridPos.Y;
			height = projectile.height;
			collisionFlags = projectile.collisionFlags;
			camp = projectile.camp;
		}
		else
		{
			vector = target.GetLogicalGlobalPosition();
			x = target.gridPos.X;
			y = target.gridPos.Y;
			height = 0.0;
			collisionFlags = ((target.instance == null) ? 1 : target.instance.collisionFlags);
			camp = ((target.camp == TowerDefenseEnum.CHARACTER_CAMP.PLANT) ? TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE : TowerDefenseEnum.CHARACTER_CAMP.PLANT);
		}
		if (_useTargetGridY)
		{
			x = target.gridPos.X;
			y = target.gridPos.Y;
		}
		float x2 = vector.X + (float)(_forwardOffset * (double)num);
		Vector2 velocity = new Vector2((float)(_speed * (double)num), 0f);
		float value = ((num > 0f) ? 0f : ((float)Math.PI));
		if (_effectScene != null)
		{
			Vector2 position = (GodotObject.IsInstanceValid(target) ? target.GetLogicalGlobalPosition() : vector);
			SpawnSplitEffect(_effectScene, position, new Vector2I(x, y), height, collisionFlags, camp);
		}
		int max = (_edgeCompensate ? instance.GetMapGridNum().Y : 0);
		for (int i = 0; i < _lineOffsets.Count; i++)
		{
			int num2 = y + _lineOffsets[i];
			if (_edgeCompensate)
			{
				num2 = Mathf.Clamp(num2, 1, max);
			}
			Vector2 mapCellPosCenter = instance.GetMapCellPosCenter(new Vector2I(x, num2));
			BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
			{
				gridYOverride = num2,
				checkAllOverride = false,
				initialRotationOverride = value
			};
			FireComponent.CreateProjectilePosition(null, null, height, new Vector2(x2, mapCellPosCenter.Y), velocity, _projectileData, collisionFlags, camp, default(Vector2), overrides);
		}
	}

	private static void SpawnSplitEffect(PackedScene scene, Vector2 position, Vector2I gridPosition, double height, int collisionFlags, TowerDefenseEnum.CHARACTER_CAMP camp)
	{
		if (scene == null || !position.IsFinite())
		{
			return;
		}
		Node2D characterNode = TowerDefenseManager.GetCharacterNode();
		if (GodotObject.IsInstanceValid(characterNode))
		{
			Node2D node2D = scene.Instantiate<Node2D>(PackedScene.GenEditState.Disabled);
			if (node2D is TowerDefenseProjectileEffectBase towerDefenseProjectileEffectBase)
			{
				towerDefenseProjectileEffectBase.Init(gridPosition, camp, collisionFlags, null, height);
			}
			node2D.GlobalPosition = position;
			node2D.ZIndex = ResolveEffectZIndex(gridPosition.Y);
			characterNode.AddChild(node2D, forceReadableName: false, Node.InternalMode.Disabled);
		}
	}

	internal static int ResolveEffectZIndex(int gridY)
	{
		return (int)Math.Clamp((long)Mathf.Max(1, gridY) * 15L + 10, -4096L, 4096L);
	}

	private static bool IsEnemyTarget(TowerDefenseProjectile projectile, TowerDefenseCharacter target)
	{
		TowerDefenseEnum.CHARACTER_CAMP cHARACTER_CAMP = (GodotObject.IsInstanceValid(projectile) ? projectile.camp : TowerDefenseEnum.CHARACTER_CAMP.NOONE);
		if (cHARACTER_CAMP != TowerDefenseEnum.CHARACTER_CAMP.PLANT && cHARACTER_CAMP != TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE)
		{
			return true;
		}
		return target.camp != cHARACTER_CAMP;
	}

	private static float ResolveDirSign(TowerDefenseProjectile projectile, TowerDefenseCharacter target)
	{
		if (target.camp == TowerDefenseEnum.CHARACTER_CAMP.PLANT)
		{
			return -1f;
		}
		if (target.camp == TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE)
		{
			return 1f;
		}
		if (GodotObject.IsInstanceValid(projectile) && projectile.velocity.X < 0f)
		{
			return -1f;
		}
		return 1f;
	}
}
