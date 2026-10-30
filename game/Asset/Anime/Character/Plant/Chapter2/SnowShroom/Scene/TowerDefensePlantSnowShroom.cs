using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantSnowShroom : TowerDefensePlant
{
	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> eventList = new Array<TowerDefenseCharacterEventBase>();

	public string projectileName = "SnowPea";

	public string skinName = "Default";

	private bool _activated;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && base.currentCustom.Contains("Custom0"))
		{
			skinName = "Sward";
		}
	}

	public override void OnCustomSwitched(string customKey)
	{
		if (customKey == "Custom0")
		{
			skinName = "Sward";
		}
		else
		{
			skinName = "Default";
		}
	}

	public override void SleepEntered()
	{
		base.SleepEntered();
		instance.invincible = false;
	}

	public override void SleepProcessing(double delta)
	{
		base.SleepProcessing(delta);
		instance.invincible = false;
	}

	public override void IdleEntered()
	{
		if (GodotObject.IsInstanceValid(TowerDefenseManager.Instance.currentControl) && TowerDefenseManager.Instance.currentControl.isGameRunning && inGame)
		{
			if (CanSleep())
			{
				Sleep();
			}
			else if (IsIzmOneShotGateOpen())
			{
				Activate();
			}
		}
	}

	private void Activate()
	{
		if (!_activated)
		{
			_activated = true;
			base.IdleEntered();
			CreateProjectile();
			HitBoxDestroy();
			instance.invincible = true;
		}
	}

	public override void IdleProcessing(double delta)
	{
		base.IdleProcessing(delta);
		if (!_activated && IsIzmOneShotGateOpen())
		{
			Activate();
		}
	}

	public async void CreateProjectile()
	{
		bool isRemoteClient = Global.IsMultiplayerMode && !MultiPlayerManager.IsHost;
		Vector2I gridNum = default(Vector2I);
		Vector2 gridSize = default(Vector2);
		double height = 600.0;
		double ownerGroundHeight = 0.0;
		TowerDefenseProjectileConfig projectileConfig = null;
		if (!isRemoteClient)
		{
			gridNum = TowerDefenseManager.Instance.GetMapGridNum();
			gridSize = TowerDefenseManager.Instance.GetMapGridSize();
			TowerDefenseProjectileCreateData towerDefenseProjectileCreateData = new TowerDefenseProjectileCreateData(projectileName);
			towerDefenseProjectileCreateData.skinName = skinName;
			towerDefenseProjectileCreateData.baseDamage = 50.0;
			towerDefenseProjectileCreateData.damageFlags = 2;
			towerDefenseProjectileCreateData.collisionFlags = 35;
			projectileConfig = towerDefenseProjectileCreateData.BuildConfig();
			ownerGroundHeight = GetGroundHeight(GetLogicalGlobalPosition().Y);
		}
		for (int id = 0; id < 25; id++)
		{
			if (!isRemoteClient)
			{
				for (int i = 1; i <= gridNum.X; i++)
				{
					for (int j = 1; j <= gridNum.Y; j++)
					{
						TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(new Vector2I(i, j));
						Vector2 pos = TowerDefenseManager.GetMapCellPlantPos(new Vector2I(i, j)) - new Vector2(150f, 0f) + new Vector2((float)GD.RandRange((0f - gridSize.X) / 2f, gridSize.X / 2f), 0f);
						double num = GD.RandRange(0, 200);
						BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
						{
							useFall = true,
							gridYOverride = j,
							zOverride = height + num,
							ySpeedOverride = 400.0
						};
						FireComponent.CreateProjectilePositionByConfig(null, null, (float)(ownerGroundHeight + 30.0 - mapCell.GetGroundHeight()), pos, new Vector2(GD.RandRange(50, 150), 0f), projectileConfig, -1, base.camp, default(Vector2), overrides);
					}
				}
			}
			await ToSignal(GetTree().CreateTimer(0.3, processAlways: false), SceneTreeTimer.SignalName.Timeout);
		}
		if (isRemoteClient)
		{
			TowerDefenseCharacter.CreateColdVisualEffect(base.gridPos);
		}
		else
		{
			TowerDefenseCharacter.CreateColdEffect(base.camp, base.gridPos);
		}
		Destroy();
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "projectileName", projectileName },
			{ "skinName", skinName }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		projectileName = data.GetValueOrDefault("projectileName", "SnowPea").AsString();
		skinName = data.GetValueOrDefault("skinName", "Default").AsString();
	}
}
