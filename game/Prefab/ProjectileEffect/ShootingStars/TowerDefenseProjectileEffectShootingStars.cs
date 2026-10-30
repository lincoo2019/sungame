using System;
using System.Threading.Tasks;
using Godot;

[GlobalClass]
public partial class TowerDefenseProjectileEffectShootingStars : TowerDefenseProjectileEffectBase
{
	private string _projectileName = "StarFull";

	private RandomNumberGenerator _random;

	private TowerDefenseProjectileCreateData _normalProjectileData;

	private TowerDefenseProjectileCreateData _bigProjectileData;

	public override string NetworkEffectId => "shooting_stars_rain";

	public override string NetworkEffectVariant => _projectileName;

	public event Action OnCompleted;

	public override void ConfigureNetworkVariant(string variant)
	{
		_projectileName = (string.IsNullOrWhiteSpace(variant) ? "StarFull" : variant);
	}

	public override void _Ready()
	{
		if (PrepareNetworkEffect())
		{
			_random = new RandomNumberGenerator
			{
				Seed = base.NetworkRandomSeed
			};
			_normalProjectileData = new TowerDefenseProjectileCreateData(new StringName(_projectileName));
			_bigProjectileData = new TowerDefenseProjectileCreateData(new StringName("ShootingStartsBigStarFull"));
			_bigProjectileData.SetScale(new Vector2(2f, 2f));
			CreateRainAsync();
		}
	}

	public override void _ExitTree()
	{
		_random?.Dispose();
		_normalProjectileData?.Dispose();
		_bigProjectileData?.Dispose();
		this.OnCompleted = null;
	}

	private async Task CreateRainAsync()
	{
		Vector2I gridNum = TowerDefenseManager.Instance.GetMapGridNum();
		Vector2 gridSize = TowerDefenseManager.Instance.GetMapGridSize();
		for (int wave = 0; wave < 25; wave++)
		{
			for (int i = 1; i <= gridNum.X; i++)
			{
				for (int j = 1; j <= gridNum.Y; j++)
				{
					TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(new Vector2I(i, j));
					if (GodotObject.IsInstanceValid(mapCell))
					{
						Vector2 pos = TowerDefenseManager.GetMapCellPlantPos(new Vector2I(i, j)) - new Vector2(150f, 0f) + new Vector2(_random.RandfRange((0f - gridSize.X) / 2f, gridSize.X / 2f), 0f);
						double num = _random.RandfRange(0f, 200f);
						bool flag = _random.Randf() < 0.05f;
						BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
						{
							useFall = true,
							gridYOverride = j,
							zOverride = 600.0 + num,
							ySpeedOverride = 400.0,
							suppressGameplay = base.NetworkVisualOnly
						};
						FireComponent.CreateProjectilePositionByData(null, null, (float)(height + 30.0 - mapCell.GetGroundHeight()), pos, new Vector2(_random.RandfRange(50f, 150f), 0f), flag ? _bigProjectileData : _normalProjectileData, collisionFlag, camp, default(Vector2), overrides);
					}
				}
			}
			await ToSignal(GetTree().CreateTimer(0.3, processAlways: false), SceneTreeTimer.SignalName.Timeout);
		}
		this.OnCompleted?.Invoke();
		QueueFree();
	}
}
