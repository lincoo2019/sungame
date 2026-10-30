using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantPeaWell : TowerDefensePlant
{
	private CharacterTimerComponent timerComponent;

	private double _fireInterval = 3.0;

	private string _projectileName = "Pea";

	[Export(PropertyHint.None, "")]
	public double fireInterval
	{
		get
		{
			return _fireInterval;
		}
		set
		{
			_fireInterval = value;
		}
	}

	[Export(PropertyHint.None, "")]
	public string projectileName
	{
		get
		{
			return _projectileName;
		}
		set
		{
			_projectileName = value;
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			timerComponent = componentManager.GetRuntime<CharacterTimerComponent>("character.timer");
			timerComponent.OnTimeout += Timeout;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		CharacterTimerComponent characterTimerComponent = timerComponent;
		if (characterTimerComponent != null && !characterTimerComponent.IsReleased)
		{
			timerComponent.OnTimeout -= Timeout;
		}
	}

	public override bool CanSleep()
	{
		if (TowerDefenseManager.Instance.IsIZMMode())
		{
			return false;
		}
		return base.CanSleep();
	}

	public void Timeout(string timerName)
	{
		if (!(timerName == "Fire"))
		{
			return;
		}
		if (!base.sprite.pause && (base.sprite.timeScale > 0.0 || TowerDefenseManager.Instance.IsIZMMode()))
		{
			TowerDefenseProjectileCreateData projectileData = CreateProjectileData();
			Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
			float num = (float)(GetGroundHeight(logicalGlobalPosition.Y) + 10.0);
			for (int i = 0; i < 36; i++)
			{
				float num2 = Mathf.DegToRad(i * 10);
				Vector2 vector = Vector2.FromAngle(num2);
				BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
				{
					checkAllOverride = true,
					initialRotationOverride = num2
				};
				FireComponent.CreateProjectilePositionByData(this, null, num, logicalGlobalPosition, vector * 200f, projectileData, -1, base.camp, default(Vector2), overrides);
			}
		}
		if (!TowerDefenseManager.Instance.IsIZMMode())
		{
			timerComponent.Run("Fire", fireInterval / timeScaleInit);
		}
		else
		{
			timerComponent.Run("Fire", fireInterval);
		}
	}

	internal TowerDefenseProjectileCreateData CreateProjectileData()
	{
		TowerDefenseProjectileCreateData towerDefenseProjectileCreateData = new TowerDefenseProjectileCreateData(projectileName);
		TowerDefenseProjectileConfig towerDefenseProjectileConfig = ResolveAuthoredProjectileConfig();
		if (towerDefenseProjectileConfig == null)
		{
			return towerDefenseProjectileCreateData;
		}
		towerDefenseProjectileCreateData.fireMethodFlags = towerDefenseProjectileConfig.fireMethodFlags;
		if ((towerDefenseProjectileConfig.fireMethodFlags & 4) != 0)
		{
			towerDefenseProjectileCreateData.penetrateNum = towerDefenseProjectileConfig.penetrateNum;
			towerDefenseProjectileCreateData.overridePenetrateNum = true;
		}
		return towerDefenseProjectileCreateData;
	}

	private TowerDefenseProjectileConfig ResolveAuthoredProjectileConfig()
	{
		if (string.IsNullOrWhiteSpace(projectileName))
		{
			return null;
		}
		return TowerDefenseManager.GetProjectileConfig(projectileName) ?? TowerDefenseManager.GetProjectileConfig(projectileName + "Default");
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint() && TowerDefenseManager.Instance.IsGameRunning() && inGame && !timerComponent.IsRunning("Fire"))
		{
			if (!TowerDefenseManager.Instance.IsIZMMode())
			{
				timerComponent.Run("Fire", fireInterval / timeScaleInit);
			}
			else
			{
				timerComponent.Run("Fire", fireInterval);
			}
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "projectileName", projectileName },
			{ "fireInterval", fireInterval }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		projectileName = data.GetValueOrDefault("projectileName", "Pea").AsString();
		fireInterval = data.GetValueOrDefault("fireInterval", 3.0).AsDouble();
	}
}
