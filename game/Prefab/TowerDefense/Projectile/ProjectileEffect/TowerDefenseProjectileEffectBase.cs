using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseProjectileEffectBase : Node2D
{
	public Vector2I gridPos;

	public TowerDefenseEnum.CHARACTER_CAMP camp;

	public int collisionFlag;

	public TowerDefenseCharacter target;

	public double height;

	public bool suppressDeathrattles;

	private bool _networkReplay;

	private bool _networkSeedConfigured;

	private bool _networkPrepared;

	private ulong _networkRandomSeed;

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> Eventlist { get; set; }

	public virtual string NetworkEffectId => "";

	public virtual string NetworkEffectVariant => "";

	public ulong NetworkRandomSeed => _networkRandomSeed;

	public bool NetworkVisualOnly => _networkReplay;

	public virtual void Init(TowerDefenseProjectile projectile)
	{
	}

	public void Init(Vector2I _gridPos, TowerDefenseEnum.CHARACTER_CAMP _camp, int _collisionFlag, TowerDefenseCharacter _target, double _height = 0.0)
	{
		gridPos = _gridPos;
		camp = _camp;
		collisionFlag = _collisionFlag;
		target = _target;
		height = _height;
	}

	public void ConfigureNetworkReplay(ulong randomSeed)
	{
		_networkReplay = true;
		_networkSeedConfigured = true;
		_networkRandomSeed = randomSeed;
	}

	public virtual void ConfigureNetworkVariant(string variant)
	{
	}

	protected bool PrepareNetworkEffect()
	{
		if (_networkPrepared)
		{
			if (_networkReplay)
			{
				return IsInsideTree();
			}
			return true;
		}
		_networkPrepared = true;
		string networkEffectId = NetworkEffectId;
		if (networkEffectId == "")
		{
			EnsureRandomSeed();
			return true;
		}
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost && !_networkReplay)
		{
			QueueFree();
			return false;
		}
		EnsureRandomSeed();
		if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost && !_networkReplay && GodotObject.IsInstanceValid(MultiPlayerManager.Instance))
		{
			MultiPlayerManager.Instance.SendProjectileEffectSpawn(new ProjectileEffectSpawnDto
			{
				effect_id = networkEffectId,
				variant = NetworkEffectVariant,
				random_seed = _networkRandomSeed,
				px = base.GlobalPosition.X,
				py = base.GlobalPosition.Y,
				grid_x = gridPos.X,
				grid_y = gridPos.Y,
				camp = (int)camp,
				collision_flags = collisionFlag,
				height = height
			});
		}
		return true;
	}

	private void EnsureRandomSeed()
	{
		if (!_networkSeedConfigured)
		{
			_networkSeedConfigured = true;
			_networkRandomSeed = GD.Randi();
		}
	}

	public void CraterCreate(bool nolimit = false, string craterName = "CraterDayGround", bool halfCrater = false)
	{
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(gridPos);
		if (GodotObject.IsInstanceValid(mapCell) && (nolimit || mapCell.CanCraterCreate()))
		{
			if (TowerDefenseManager.GetPacketConfig(craterName).Plant(gridPos, playAudio: false, noLimit: true) is TowerDefenseCrater towerDefenseCrater && halfCrater)
			{
				towerDefenseCrater.halfCrater = true;
			}
			if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
			{
				MultiPlayerManager.Instance.SendCraterCreate(gridPos.X, gridPos.Y, craterName);
			}
		}
	}
}
