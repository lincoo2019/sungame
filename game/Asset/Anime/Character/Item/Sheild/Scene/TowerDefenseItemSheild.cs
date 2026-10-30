using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseItemSheild : TowerDefenseItem
{
	public const int MAX_LAYERS = 8;

	public const double HP_PER_LAYER = 1000.0;

	public const double MAX_HP = 8000.0;

	private StringName _shieldType = new StringName("Default");

	private double _pendingShieldHitpoints = -1.0;

	private Label _layerLabel;

	private double _lastLabelHitpoints = 0.0 / 0.0;

	[Export(PropertyHint.None, "")]
	public StringName shieldType
	{
		get
		{
			return _shieldType;
		}
		set
		{
			StringName stringName = _shieldType;
			_shieldType = value;
			if (IsNodeReady() && stringName != _shieldType)
			{
				_ShieldUpdateAppearance();
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public double shieldHitpoints
	{
		get
		{
			if (GodotObject.IsInstanceValid(instance))
			{
				return instance.hitpoints;
			}
			if (!(_pendingShieldHitpoints >= 0.0))
			{
				return 1000.0;
			}
			return _pendingShieldHitpoints;
		}
		set
		{
			_pendingShieldHitpoints = Mathf.Clamp(value, 0.0, 8000.0);
			ApplyPendingShieldHitpoints();
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !editorPreviewMode)
		{
			_layerLabel = GetNodeOrNull<Label>("%LayerLabel");
			_RefreshProjectileTargetability();
			ApplyPendingShieldHitpoints();
			_ShieldUpdateAppearance();
			_ShieldUpdateLabel();
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint() && GodotObject.IsInstanceValid(instance))
		{
			_RefreshProjectileTargetability();
			_RefreshLabelIfHitpointsChanged();
		}
	}

	private void _RefreshLabelIfHitpointsChanged()
	{
		if (GodotObject.IsInstanceValid(_layerLabel) && instance.hitpoints != _lastLabelHitpoints)
		{
			_ShieldUpdateLabel();
		}
	}

	private void _RefreshProjectileTargetability()
	{
		if (GodotObject.IsInstanceValid(instance))
		{
			instance.canBeCollection = !_HasGroundProjectileCarrier();
		}
	}

	private bool _HasGroundProjectileCarrier()
	{
		if (!GodotObject.IsInstanceValid(cell))
		{
			return false;
		}
		foreach (TowerDefenseCharacter character in cell.characterList)
		{
			if (_CanCarryGroundProjectile(character))
			{
				return true;
			}
		}
		return _CanCarryGroundProjectile(cell.characterSurround);
	}

	private bool _CanCarryGroundProjectile(TowerDefenseCharacter character)
	{
		if (!(character is TowerDefensePlant) || !GodotObject.IsInstanceValid(character.instance))
		{
			return false;
		}
		if (!character.instance.canBeCollection || character.instance.hypnoses != instance.hypnoses)
		{
			return false;
		}
		int num = 6;
		if ((character.instance.physiqueTypeFlags & num) != 0)
		{
			return false;
		}
		return (character.instance.maskFlags & 1) != 0;
	}

	private void ApplyPendingShieldHitpoints()
	{
		if (!(_pendingShieldHitpoints < 0.0) && GodotObject.IsInstanceValid(instance))
		{
			instance.hitpoints = Mathf.Min(_pendingShieldHitpoints, 8000.0);
			_ShieldUpdateLabel();
		}
	}

	public void _ShieldUpdateLabel()
	{
		if (GodotObject.IsInstanceValid(_layerLabel))
		{
			_lastLabelHitpoints = instance.hitpoints;
			_layerLabel.Text = ((int)instance.hitpoints).ToString();
		}
	}

	public int ShieldGetLayerCount()
	{
		return Mathf.Clamp((int)(instance.hitpoints / 1000.0), 0, 8);
	}

	public void ShieldAddHitpoints(double num, StringName newShieldType = null)
	{
		if (!(num <= 0.0))
		{
			if ((object)newShieldType == null)
			{
				newShieldType = new StringName("Default");
			}
			instance.hitpoints = Mathf.Min(instance.hitpoints + num, 8000.0);
			if (_shieldType != newShieldType)
			{
				_shieldType = newShieldType;
				_ShieldUpdateAppearance();
			}
			_ShieldUpdateLabel();
		}
	}

	public double ShieldAbsorbDamage(double num)
	{
		if (num <= 0.0 || instance.hitpoints <= 0.0)
		{
			return num;
		}
		double num2 = Mathf.Min(num, instance.hitpoints);
		instance.hitpoints -= num2;
		_ShieldUpdateLabel();
		if (instance.hitpoints <= 0.0)
		{
			instance.hitpoints = 0.0;
			instance.EmitHitpointsEmpty();
		}
		return num - num2;
	}

	public bool ShieldBlockLethal()
	{
		if (instance.hitpoints <= 0.0)
		{
			return false;
		}
		double num = Mathf.Min(1000.0, instance.hitpoints);
		instance.hitpoints -= num;
		_ShieldUpdateLabel();
		if (instance.hitpoints <= 0.0)
		{
			instance.hitpoints = 0.0;
			instance.EmitHitpointsEmpty();
		}
		return true;
	}

	public bool ShieldBlockCharm()
	{
		return ShieldBlockLethal();
	}

	public void ShieldDeflateVehicle(TowerDefenseCharacter attacker)
	{
		if (GodotObject.IsInstanceValid(attacker) && attacker is TowerDefenseZombie towerDefenseZombie && towerDefenseZombie.instance.zombiePhysique == TowerDefenseEnum.ZOMBIE_PHYSIQUE.CAR)
		{
			towerDefenseZombie.Die();
		}
	}

	public void _ShieldUpdateAppearance()
	{
		AdobeAnimateSpriteBase adobeAnimateSpriteBase = base.sprite as AdobeAnimateSpriteBase;
		if (GodotObject.IsInstanceValid(adobeAnimateSpriteBase))
		{
			adobeAnimateSpriteBase.SetFliters(new Array { "body", "body2", "left", "left2", "right", "right2", "TX", "TX2" }, open: false);
			if ((string?)_shieldType == "MG")
			{
				adobeAnimateSpriteBase.SetFliters(new Array { "body2", "left2", "right2", "TX2" }, open: true);
			}
			else
			{
				adobeAnimateSpriteBase.SetFliters(new Array { "body", "left", "right", "TX" }, open: true);
			}
		}
	}

	public static void CreateOnCell(TowerDefenseCellInstance cell, StringName newShieldType = null, int layers = 1)
	{
		CreateOnCellWithHP(cell, newShieldType, (double)layers * 1000.0);
	}

	public static void CreateOnCellWithHP(TowerDefenseCellInstance cell, StringName newShieldType, double hp)
	{
		if (!GodotObject.IsInstanceValid(cell))
		{
			return;
		}
		if ((object)newShieldType == null)
		{
			newShieldType = new StringName("Default");
		}
		if (GodotObject.IsInstanceValid(cell.itemShield))
		{
			cell.itemShield.ShieldAddHitpoints(hp, newShieldType);
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("ItemSheild");
		if (packetConfig == null)
		{
			return;
		}
		TowerDefenseCharacter towerDefenseCharacter = packetConfig.Plant(cell.gridPos, playAudio: false, noLimit: true);
		TowerDefenseItemSheild shield = towerDefenseCharacter as TowerDefenseItemSheild;
		if (shield == null)
		{
			return;
		}
		shield.shieldType = newShieldType;
		shield.shieldHitpoints = hp;
		Callable.From(delegate
		{
			if (GodotObject.IsInstanceValid(shield))
			{
				shield.instance.hitpoints = Mathf.Min(hp, 8000.0);
				shield._ShieldUpdateLabel();
			}
		}).CallDeferred();
	}
}
