using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieNormalWallnut : TowerDefenseZombie
{
	private const string WALLNUT_BODY_MEDIA = "Wallnut_body.png";

	private const string WALLNUT_CRACKED_1 = "uid://dpnwmtm6ypomi";

	private const string WALLNUT_CRACKED_2 = "uid://dq2ayfqxj5mfx";

	private const string WALLNUT_DAMAGE_STAGE_KEY = "wallnutHeadDamageStage";

	private const int WALLNUT_HEAD_DAMAGE_STAGE_NONE = 0;

	private const int WALLNUT_HEAD_DAMAGE_STAGE_ONE = 1;

	private const int WALLNUT_HEAD_DAMAGE_STAGE_TWO = 2;

	private ZombieNormalWallnutSprite _zombieSprite;

	private int _wallnutHeadDamageStage;

	private int _appliedWallnutHeadDamageStage = -1;

	public bool halfHp;

	public bool isAttack;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_zombieSprite = base.sprite as ZombieNormalWallnutSprite;
			TryApplyWallnutHeadDamageStage();
			ConfigureWaterLineVisualLayers("Zombie_duckytube", "Zombie_whitewater", "Zombie_whitewater2");
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (_appliedWallnutHeadDamageStage != _wallnutHeadDamageStage)
		{
			TryApplyWallnutHeadDamageStage();
		}
	}

	public override void AttackEntered()
	{
		base.AttackEntered();
		isAttack = true;
		if (HasShield())
		{
			base.sprite.SetFliters(new Array { "Zombie_outerarm_upper" }, open: true);
			if (!halfHp)
			{
				base.sprite.SetFliters(new Array { "Zombie_outerarm_hand", "Zombie_outerarm_lower" }, open: true);
			}
		}
	}

	public override void AttackExited()
	{
		base.AttackExited();
		isAttack = false;
		if (HasShield())
		{
			base.sprite.SetFliters(new Array { "Zombie_outerarm_upper", "Zombie_outerarm_hand", "Zombie_outerarm_lower" }, open: false);
		}
	}

	public override void DieProcessing(double delta)
	{
		base.DieProcessing(delta);
		base.sprite.timeScale = timeScale * 2.0;
	}

	public override void DamagePointReach(string damagePointName)
	{
		base.DamagePointReach(damagePointName);
		switch (damagePointName)
		{
		case "Arm":
			halfHp = true;
			base.sprite.SetFliters(new Array { "Zombie_outerarm_upper" }, open: true);
			break;
		case "Head":
			DamagePartCreate("Head", _zombieSprite.head, new Vector2(GD.RandRange(-100, 100), -300f), keepSlotScale: false, new Vector2(-25f, -30f), fromSync: false, null, 0L);
			break;
		case "Damage1":
			SetWallnutHeadDamageStage(1);
			break;
		case "Damage2":
			SetWallnutHeadDamageStage(2);
			break;
		}
	}

	public override Dictionary ExportVariantSave()
	{
		Dictionary dictionary = base.ExportVariantSave();
		dictionary["wallnutHeadDamageStage"] = _wallnutHeadDamageStage;
		return dictionary;
	}

	public override void ImportVariantSave(Dictionary data)
	{
		base.ImportVariantSave(data);
		int wallnutHeadDamageStage = data["wallnutHeadDamageStage"].AsInt32();
		SetWallnutHeadDamageStage(wallnutHeadDamageStage);
	}

	private void SetWallnutHeadDamageStage(int stage)
	{
		if (stage < 0)
		{
			stage = 0;
		}
		if (stage > 2)
		{
			stage = 2;
		}
		_wallnutHeadDamageStage = stage;
		TryApplyWallnutHeadDamageStage();
	}

	private bool TryApplyWallnutHeadDamageStage()
	{
		if (!GodotObject.IsInstanceValid(_zombieSprite) || !GodotObject.IsInstanceValid(_zombieSprite.head))
		{
			return false;
		}
		string text = _wallnutHeadDamageStage switch
		{
			1 => "uid://dpnwmtm6ypomi", 
			2 => "uid://dq2ayfqxj5mfx", 
			_ => string.Empty, 
		};
		if (_appliedWallnutHeadDamageStage == _wallnutHeadDamageStage && _zombieSprite.head.GetAtlasReplacePath("Wallnut_body.png") == text)
		{
			return true;
		}
		if (!_zombieSprite.head.SetAtlasReplace("Wallnut_body.png", text))
		{
			return false;
		}
		_appliedWallnutHeadDamageStage = _wallnutHeadDamageStage;
		return true;
	}

	public override void ArmorDamagePointReach(string armorName, int stage)
	{
		base.ArmorDamagePointReach(armorName, stage);
		if (isAttack && HasShield() && stage > 0)
		{
			base.sprite.SetFliters(new Array { "Zombie_outerarm_upper" }, open: true);
			if (!halfHp)
			{
				base.sprite.SetFliters(new Array { "Zombie_outerarm_hand", "Zombie_outerarm_lower" }, open: true);
			}
		}
	}

	public override void Purify()
	{
		if (!GodotObject.IsInstanceValid(cell))
		{
			return;
		}
		if (Global.IsMultiplayerMode && !MultiPlayerManager.Instance.isHost)
		{
			Destroy();
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("PlantWallnut");
		if (cell.CanPacketPlant(packetConfig))
		{
			TowerDefenseCharacter towerDefenseCharacter = packetConfig.Plant(base.gridPos);
			towerDefenseCharacter.WakeUp();
			if (instance.hypnoses)
			{
				towerDefenseCharacter.Hypnoses();
			}
			if (Global.IsMultiplayerMode && MultiPlayerManager.Instance.isHost)
			{
				TowerDefenseControlNew currentControl = TowerDefenseManager.Instance.currentControl;
				if (GodotObject.IsInstanceValid(currentControl))
				{
					int nextSyncId = currentControl.GetNextSyncId();
					currentControl.RegisterSyncCharacter(nextSyncId, towerDefenseCharacter);
					MultiPlayerManager.Instance.SendSpawnCharacterAt("PlantWallnut", base.gridPos.X, base.gridPos.Y, nextSyncId);
				}
			}
		}
		Destroy();
	}
}
