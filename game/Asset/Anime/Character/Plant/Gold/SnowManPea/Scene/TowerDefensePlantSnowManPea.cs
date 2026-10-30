using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantSnowManPea : TowerDefensePlant
{
	private CharacterTimerComponent _timerComponent;

	private FireComponent _fireComponent;

	public int fireProjectileNum;

	private bool _isPower;

	public bool over;

	private string _projectileName = "SnowBullet";

	public bool isPower
	{
		get
		{
			return _isPower;
		}
		set
		{
			SetPower(value);
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
			if (IsNodeReady() && _fireComponent != null)
			{
				((FireComponentProjectileSingle)_fireComponent.fireCheckList[0].projectile).projectileName = _projectileName;
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_timerComponent = componentManager.GetRuntime<CharacterTimerComponent>("character.timer");
			_fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			_fireComponent.OnFireVolley += FireVolley;
			_timerComponent.OnTimeout += Timeout;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		FireComponent fireComponent = _fireComponent;
		if (fireComponent != null && !fireComponent.IsReleased)
		{
			_fireComponent.OnFireVolley -= FireVolley;
		}
		CharacterTimerComponent timerComponent = _timerComponent;
		if (timerComponent != null && !timerComponent.IsReleased)
		{
			_timerComponent.OnTimeout -= Timeout;
		}
	}

	public void SetPower(bool value)
	{
		_isPower = value;
		if (_isPower)
		{
			((FireComponentProjectileSingle)_fireComponent.fireCheckList[0].projectile).projectileData.baseDamage = 60.0;
			((FireComponentProjectileSingle)_fireComponent.fireCheckList[0].projectile).projectileData.scale = Vector2.One;
			_fireComponent.fireInterval = 0.3f;
			_fireComponent.checkLength = -1f;
			base.sprite.SetFliters(new Array { "shoot_blink2", "mouth2", "head2", "ice1", "body2", "ice2" }, open: true);
		}
		else
		{
			_fireComponent.fireInterval = 0.5f;
			_fireComponent.checkLength = 6f;
			base.sprite.SetFliters(new Array { "shoot_blink2", "mouth2", "head2", "ice1", "body2", "ice2" }, open: false);
		}
	}

	public override void DestroySet()
	{
		if (over)
		{
			return;
		}
		over = true;
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return;
		}
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		TowerDefenseItemSnowBall towerDefenseItemSnowBall = TowerDefenseCharacter.CreateCharacter("ItemSnowBall", logicalGlobalPosition, base.gridPos, base.groundHeight) as TowerDefenseItemSnowBall;
		if (instance.hypnoses)
		{
			towerDefenseItemSnowBall.Hypnoses();
		}
		towerDefenseItemSnowBall.SetSize("Max");
		if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
		{
			TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
			if (GodotObject.IsInstanceValid(currentControl))
			{
				int nextSyncId = currentControl.GetNextSyncId();
				currentControl.RegisterSyncCharacter(nextSyncId, towerDefenseItemSnowBall);
				MultiPlayerManager.Instance.SendSpawnCharacterAt("ItemSnowBall", base.gridPos.X, base.gridPos.Y, nextSyncId, 1.0, 1.0, instance.hypnoses, 0.0, useCreate: true, logicalGlobalPosition.X, logicalGlobalPosition.Y, walkAfterSpawn: false, base.groundHeight, "Max");
			}
		}
	}

	public void FireVolley(ulong randomSeed)
	{
		if (!isPower)
		{
			if (fireProjectileNum < 100)
			{
				fireProjectileNum++;
				return;
			}
			_timerComponent.Run("Power", 30.0);
			isPower = true;
		}
	}

	public void Timeout(string timerName)
	{
		if (timerName == "Power")
		{
			isPower = false;
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "fireProjectileNum", fireProjectileNum },
			{ "isPower", isPower },
			{ "projectileName", projectileName }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		fireProjectileNum = (int)data.GetValueOrDefault("fireProjectileNum", 0);
		isPower = (bool)data.GetValueOrDefault("isPower", false);
		projectileName = (string)data.GetValueOrDefault("projectileName", "SnowBullet");
	}
}
