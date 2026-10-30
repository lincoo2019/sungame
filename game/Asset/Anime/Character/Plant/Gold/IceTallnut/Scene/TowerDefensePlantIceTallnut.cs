using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantIceTallnut : TowerDefensePlant
{
	private const string ICE_TALLNUT_SKIN11 = "res://Asset/AtlasSource/Custom/Anime/Character/Plant/Gold/IceTallnut/Custom/IceTallnut_skin1_1.png";

	private const string ICE_TALLNUT_SKIN12 = "res://Asset/AtlasSource/Custom/Anime/Character/Plant/Gold/IceTallnut/Custom/IceTallnut_skin1_2.png";

	private const string ICE_TALLNUT_SKIN13 = "res://Asset/AtlasSource/Custom/Anime/Character/Plant/Gold/IceTallnut/Custom/IceTallnut_skin1_3.png";

	private const string ICE_TALLNUT_SKIN21 = "res://Asset/AtlasSource/Custom/Anime/Character/Plant/Gold/IceTallnut/Custom/IceTallnut_skin2_1.png";

	private const string ICE_TALLNUT_SKIN22 = "res://Asset/AtlasSource/Custom/Anime/Character/Plant/Gold/IceTallnut/Custom/IceTallnut_skin2_2.png";

	private const string ICE_TALLNUT_SKIN23 = "res://Asset/AtlasSource/Custom/Anime/Character/Plant/Gold/IceTallnut/Custom/IceTallnut_skin2_3.png";

	private const string ICE_TALLNUT_SKIN31 = "res://Asset/AtlasSource/Custom/Anime/Character/Plant/Gold/IceTallnut/Custom/IceTallnut_skin3_1.png";

	private const string ICE_TALLNUT_SKIN32 = "res://Asset/AtlasSource/Custom/Anime/Character/Plant/Gold/IceTallnut/Custom/IceTallnut_skin3_2.png";

	private const string ICE_TALLNUT_SKIN33 = "res://Asset/AtlasSource/Custom/Anime/Character/Plant/Gold/IceTallnut/Custom/IceTallnut_skin3_3.png";

	private FireComponent _fireComponent;

	private double _fireInterval = 1.5;

	private int _fireNum = 1;

	private string _projectileName = "SnowPea";

	private string _skinName = "Default";

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
			if (IsNodeReady() && _fireComponent != null)
			{
				FireComponent fireComponent = _fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased)
				{
					_fireComponent.fireInterval = (float)value;
				}
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public int fireNum
	{
		get
		{
			return _fireNum;
		}
		set
		{
			_fireNum = value;
			if (IsNodeReady() && _fireComponent != null)
			{
				FireComponent fireComponent = _fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased)
				{
					_fireComponent.fireNum = value;
				}
			}
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
				FireComponent fireComponent = _fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased && _fireComponent.fireCheckList.Count > 0)
				{
					((FireComponentProjectileSingle)_fireComponent.fireCheckList[0].projectile).projectileName = value;
				}
			}
		}
	}

	public string skinName
	{
		get
		{
			return _skinName;
		}
		set
		{
			_skinName = value;
			if (IsNodeReady() && _fireComponent != null)
			{
				FireComponent fireComponent = _fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased && _fireComponent.fireCheckList.Count > 0)
				{
					((FireComponentProjectileSingle)_fireComponent.fireCheckList[0].projectile).projectileData.skinName = value;
				}
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			if (base.currentCustom.Contains("Custom0"))
			{
				skinName = "Armor";
			}
		}
	}

	public override void OnCustomSwitched(string customKey)
	{
		if (customKey == "Custom0")
		{
			skinName = "Armor";
		}
		else
		{
			skinName = "Default";
		}
	}

	public override async void DestroySet()
	{
		TowerDefenseCharacter.CreateColdEffect(base.camp, base.gridPos);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
	}

	public override void DamagePointReach(string damangePointName)
	{
		base.DamagePointReach(damangePointName);
		switch (damangePointName)
		{
		case "Damage0":
			if (base.currentCustom.Contains("Custom0"))
			{
				base.sprite.SetAtlasReplace("IceTallnut_skin1_1.png", "res://Asset/AtlasSource/Custom/Anime/Character/Plant/Gold/IceTallnut/Custom/IceTallnut_skin1_1.png", queueUpdate: false);
				base.sprite.SetAtlasReplace("IceTallnut_skin2_1.png", "res://Asset/AtlasSource/Custom/Anime/Character/Plant/Gold/IceTallnut/Custom/IceTallnut_skin2_1.png", queueUpdate: false);
				base.sprite.SetAtlasReplace("IceTallnut_skin3_1.png", "res://Asset/AtlasSource/Custom/Anime/Character/Plant/Gold/IceTallnut/Custom/IceTallnut_skin3_1.png", queueUpdate: false);
				base.sprite.QueueUpdateMediaReplace();
			}
			break;
		case "Damage1":
			if (base.currentCustom.Contains("Custom0"))
			{
				base.sprite.SetAtlasReplace("IceTallnut_skin1_1.png", "res://Asset/AtlasSource/Custom/Anime/Character/Plant/Gold/IceTallnut/Custom/IceTallnut_skin1_2.png", queueUpdate: false);
				base.sprite.SetAtlasReplace("IceTallnut_skin2_1.png", "res://Asset/AtlasSource/Custom/Anime/Character/Plant/Gold/IceTallnut/Custom/IceTallnut_skin2_2.png", queueUpdate: false);
				base.sprite.SetAtlasReplace("IceTallnut_skin3_1.png", "res://Asset/AtlasSource/Custom/Anime/Character/Plant/Gold/IceTallnut/Custom/IceTallnut_skin3_2.png", queueUpdate: false);
				base.sprite.QueueUpdateMediaReplace();
			}
			break;
		case "Damage2":
			if (base.currentCustom.Contains("Custom0"))
			{
				base.sprite.SetAtlasReplace("IceTallnut_skin1_1.png", "res://Asset/AtlasSource/Custom/Anime/Character/Plant/Gold/IceTallnut/Custom/IceTallnut_skin1_3.png", queueUpdate: false);
				base.sprite.SetAtlasReplace("IceTallnut_skin2_1.png", "res://Asset/AtlasSource/Custom/Anime/Character/Plant/Gold/IceTallnut/Custom/IceTallnut_skin2_3.png", queueUpdate: false);
				base.sprite.SetAtlasReplace("IceTallnut_skin3_1.png", "res://Asset/AtlasSource/Custom/Anime/Character/Plant/Gold/IceTallnut/Custom/IceTallnut_skin3_3.png", queueUpdate: false);
				base.sprite.QueueUpdateMediaReplace();
			}
			break;
		}
	}

	public override void AttackDeal(TowerDefenseCharacter character, string type, double num)
	{
		base.AttackDeal(character, type, num);
		if (GodotObject.IsInstanceValid(character))
		{
			character.Hurt(num / 2.0, playSplatAudio: false);
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			["fireNum"] = fireNum,
			["projectileName"] = projectileName,
			["skinName"] = skinName,
			["fireInterval"] = fireInterval
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		fireNum = ((!data.ContainsKey("fireNum")) ? 1 : data["fireNum"].AsInt32());
		projectileName = (data.ContainsKey("projectileName") ? data["projectileName"].AsString() : "SnowPea");
		skinName = (data.ContainsKey("skinName") ? data["skinName"].AsString() : "Default");
		fireInterval = (data.ContainsKey("fireInterval") ? data["fireInterval"].AsDouble() : 1.5);
	}
}
