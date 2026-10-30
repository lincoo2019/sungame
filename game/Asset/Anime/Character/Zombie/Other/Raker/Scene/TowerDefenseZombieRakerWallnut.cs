using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieRakerWallnut : TowerDefenseZombieRaker
{
	private const string WALLNUT_BODY_MEDIA = "Wallnut_body.png";

	private const string WALLNUT_CRACKED_1 = "uid://dpnwmtm6ypomi";

	private const string WALLNUT_CRACKED_2 = "uid://dq2ayfqxj5mfx";

	private ZombieRakerWallnutSprite _zombieSprite;

	private int _wallnutHeadDamageStage;

	private int _appliedWallnutHeadDamageStage = -1;

	private bool _headLost;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_zombieSprite = base.sprite as ZombieRakerWallnutSprite;
			ApplyWallnutHeadState();
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (_appliedWallnutHeadDamageStage != _wallnutHeadDamageStage)
		{
			ApplyWallnutHeadState();
		}
	}

	public override void DamagePointReach(string damagePointName)
	{
		base.DamagePointReach(damagePointName);
		switch (damagePointName)
		{
		case "Damage1":
			_wallnutHeadDamageStage = 1;
			ApplyWallnutHeadState();
			break;
		case "Damage2":
			_wallnutHeadDamageStage = 2;
			ApplyWallnutHeadState();
			break;
		case "Head":
			if (!_headLost && GodotObject.IsInstanceValid(_zombieSprite?.head) && (!Global.IsMultiplayerMode || MultiPlayerManager.IsHost))
			{
				DamagePartCreate("Head", _zombieSprite.head, new Vector2(GD.RandRange(-100, 100), -300f), keepSlotScale: false, new Vector2(-25f, -30f), fromSync: false, null, 0L);
			}
			_headLost = true;
			ApplyWallnutHeadState();
			break;
		}
	}

	private void ApplyWallnutHeadState()
	{
		if (GodotObject.IsInstanceValid(_zombieSprite) && GodotObject.IsInstanceValid(_zombieSprite.head) && _zombieSprite.head.GetParent() == _zombieSprite)
		{
			_zombieSprite.head.Visible = !_headLost;
			string textureReference = _wallnutHeadDamageStage switch
			{
				1 => "uid://dpnwmtm6ypomi", 
				2 => "uid://dq2ayfqxj5mfx", 
				_ => string.Empty, 
			};
			if (_zombieSprite.head.SetAtlasReplace("Wallnut_body.png", textureReference))
			{
				_appliedWallnutHeadDamageStage = _wallnutHeadDamageStage;
			}
		}
	}

	public override Dictionary ExportVariantSave()
	{
		Dictionary dictionary = base.ExportVariantSave();
		dictionary["wallnutHeadDamageStage"] = _wallnutHeadDamageStage;
		dictionary["wallnutHeadLost"] = _headLost;
		return dictionary;
	}

	public override void ImportVariantSave(Dictionary data)
	{
		base.ImportVariantSave(data);
		_wallnutHeadDamageStage = Mathf.Clamp(data.GetValueOrDefault("wallnutHeadDamageStage", 0).AsInt32(), 0, 2);
		_headLost = data.GetValueOrDefault("wallnutHeadLost", false).AsBool();
		ApplyWallnutHeadState();
	}

	public override Dictionary ExportNetworkSpecialState()
	{
		Dictionary dictionary = base.ExportNetworkSpecialState();
		dictionary["wallnutHeadDamageStage"] = _wallnutHeadDamageStage;
		dictionary["wallnutHeadLost"] = _headLost;
		return dictionary;
	}

	public override void ImportNetworkSpecialState(Dictionary data)
	{
		base.ImportNetworkSpecialState(data);
		_wallnutHeadDamageStage = Mathf.Clamp(data.GetValueOrDefault("wallnutHeadDamageStage", _wallnutHeadDamageStage).AsInt32(), 0, 2);
		_headLost = data.GetValueOrDefault("wallnutHeadLost", _headLost).AsBool();
		ApplyWallnutHeadState();
	}

	public override int GetNetworkSpecialStateRevision()
	{
		return base.GetNetworkSpecialStateRevision() | (_wallnutHeadDamageStage << 5) | (_headLost ? 128 : 0);
	}
}
