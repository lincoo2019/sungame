using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantHotDog : TowerDefensePlant
{
	private const string HOT_DOG_SKIN31 = "uid://b15hq5knf1462";

	private const string HOT_DOG_SKIN32 = "uid://dpb542keiv7j8";

	private const string HOT_DOG_SKIN33 = "uid://dp84ritbd4lbw";

	private const string HOT_DOG_SKIN41 = "uid://bwhn21u7l6hxa";

	private const string HOT_DOG_SKIN42 = "uid://b3c8rks3u7ldu";

	private const string HOT_DOG_SKIN43 = "uid://doxybk50iiwhe";

	private const string HOT_DOG_SKIN61 = "uid://b6lxv4s7mmh1c";

	private const string HOT_DOG_SKIN62 = "uid://bbbscktm0lt4a";

	private const string HOT_DOG_SKIN63 = "uid://dciued0lcnkxo";

	private const string HOT_DOG_SKIN72 = "uid://kn34pg26s2n";

	private const string HOT_DOG_SKIN722 = "uid://ciw3bqm4vccey";

	private const string HOT_DOG_SKIN723 = "uid://bj8vrr866kdo2";

	private const string HOT_DOG_SKIN73 = "uid://ja25qci6b505";

	private const string HOT_DOG_SKIN733 = "uid://bw0wgcemhi40e";

	private const string HOT_DOG_SKIN74 = "uid://b2iah3f5ft2t5";

	private const string HOT_DOG_SKIN742 = "uid://cii3syd5r3q5q";

	private const string HOT_DOG_SKIN743 = "uid://dlsvytiqbcbt2";

	private const string HOT_DOG_SKIN75 = "uid://cghcs27icauh4";

	private const string HOT_DOG_SKIN753 = "uid://bdwsmked5boiu";

	private const string HOT_DOG_SKIN76 = "uid://df0v50tov07ky";

	private const string HOT_DOG_SKIN762 = "uid://b3r7s1txwktrs";

	private const string HOT_DOG_SKIN763 = "uid://cd6irfup6sokx";

	private const string HOT_DOG00019 = "uid://6xhwx5v733xs";

	private const string HOT_DOG00019_CRACKED1 = "uid://kddmvlhxk80f";

	private const string HOT_DOG00019_CRACKED2 = "uid://bt2crx2aj2b2b";

	private const string HOT_DOG00028 = "uid://8dnfdnmf4xxe";

	private const string HOT_DOG00028_CRACKED1 = "uid://u14pwe08qshs";

	private const string HOT_DOG00028_CRACKED2 = "uid://vtktjc5ueuwf";

	private const string HOT_DOG00046 = "uid://bmgefsot5bcmf";

	private const string HOT_DOG00046_CRACKED1 = "uid://clxq2sslv2v7o";

	private const string HOT_DOG00046_CRACKED2 = "uid://b7ejbiskk5r6v";

	private ProduceComponent _produceComponent;

	private FireComponent _fireComponent;

	private double _produceInterval = 25.0;

	private int _sunNum = 50;

	private double _fireInterval = 2.0;

	private int _fireNum = 1;

	[Export(PropertyHint.None, "")]
	public double produceInterval
	{
		get
		{
			return _produceInterval;
		}
		set
		{
			_produceInterval = value;
			if (IsNodeReady() && _produceComponent != null)
			{
				ProduceComponent produceComponent = _produceComponent;
				if (produceComponent != null && !produceComponent.IsReleased)
				{
					_produceComponent.produceInterval = (float)value;
				}
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public int sunNum
	{
		get
		{
			return _sunNum;
		}
		set
		{
			_sunNum = value;
			if (IsNodeReady() && _produceComponent != null)
			{
				ProduceComponent produceComponent = _produceComponent;
				if (produceComponent != null && !produceComponent.IsReleased)
				{
					_produceComponent.num = value;
				}
			}
		}
	}

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

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_produceComponent = componentManager.GetRuntime<ProduceComponent>();
			_fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			if (base.currentCustom.Contains("Custom0"))
			{
				((FireComponentProjectileSingle)((FireComponentProjectileWeight)((FireComponentProjectileWeight)_fireComponent.fireCheckList[0].projectile).projectileWeight[0].projectileResource).projectileWeight[0].projectileResource).projectileData.skinName = "Pow";
				((FireComponentProjectileSingle)((FireComponentProjectileWeight)((FireComponentProjectileWeight)_fireComponent.fireCheckList[0].projectile).projectileWeight[0].projectileResource).projectileWeight[1].projectileResource).projectileData.skinName = "Pow";
				((FireComponentProjectileSingle)((FireComponentProjectileWeight)((FireComponentProjectileWeight)_fireComponent.fireCheckList[0].projectile).projectileWeight[0].projectileResource).projectileWeight[2].projectileResource).projectileData.skinName = "Cask";
				((FireComponentProjectileSingle)((FireComponentProjectileWeight)_fireComponent.fireCheckList[0].projectile).projectileWeight[1].projectileResource).projectileData.skinName = "Cask";
			}
		}
	}

	public override void OnCustomSwitched(string customKey)
	{
		if (customKey == "Custom0")
		{
			((FireComponentProjectileSingle)((FireComponentProjectileWeight)((FireComponentProjectileWeight)_fireComponent.fireCheckList[0].projectile).projectileWeight[0].projectileResource).projectileWeight[0].projectileResource).projectileData.skinName = "Pow";
			((FireComponentProjectileSingle)((FireComponentProjectileWeight)((FireComponentProjectileWeight)_fireComponent.fireCheckList[0].projectile).projectileWeight[0].projectileResource).projectileWeight[1].projectileResource).projectileData.skinName = "Pow";
			((FireComponentProjectileSingle)((FireComponentProjectileWeight)((FireComponentProjectileWeight)_fireComponent.fireCheckList[0].projectile).projectileWeight[0].projectileResource).projectileWeight[2].projectileResource).projectileData.skinName = "Cask";
			((FireComponentProjectileSingle)((FireComponentProjectileWeight)_fireComponent.fireCheckList[0].projectile).projectileWeight[1].projectileResource).projectileData.skinName = "Cask";
		}
		else
		{
			((FireComponentProjectileSingle)((FireComponentProjectileWeight)((FireComponentProjectileWeight)_fireComponent.fireCheckList[0].projectile).projectileWeight[0].projectileResource).projectileWeight[0].projectileResource).projectileData.skinName = "Default";
			((FireComponentProjectileSingle)((FireComponentProjectileWeight)((FireComponentProjectileWeight)_fireComponent.fireCheckList[0].projectile).projectileWeight[0].projectileResource).projectileWeight[1].projectileResource).projectileData.skinName = "Default";
			((FireComponentProjectileSingle)((FireComponentProjectileWeight)((FireComponentProjectileWeight)_fireComponent.fireCheckList[0].projectile).projectileWeight[0].projectileResource).projectileWeight[2].projectileResource).projectileData.skinName = "Default";
			((FireComponentProjectileSingle)((FireComponentProjectileWeight)_fireComponent.fireCheckList[0].projectile).projectileWeight[1].projectileResource).projectileData.skinName = "Default";
		}
	}

	public override void DamagePointReach(string damangePointName)
	{
		base.DamagePointReach(damangePointName);
		switch (damangePointName)
		{
		case "Damage0":
			base.sprite.SetAtlasReplace("HotDog_0001_9.png", "uid://6xhwx5v733xs");
			base.sprite.SetAtlasReplace("HotDog_0002_8.png", "uid://8dnfdnmf4xxe");
			base.sprite.SetAtlasReplace("HotDog_0004_6.png", "uid://bmgefsot5bcmf");
			if (base.currentCustom.Contains("Custom0"))
			{
				base.sprite.SetAtlasReplace("HotDog_skin3_1.png", "uid://b15hq5knf1462");
				base.sprite.SetAtlasReplace("HotDog_skin4_1.png", "uid://bwhn21u7l6hxa");
				base.sprite.SetAtlasReplace("HotDog_skin6_1.png", "uid://b6lxv4s7mmh1c");
				base.sprite.SetAtlasReplace("HotDog_skin7_2.png", "uid://kn34pg26s2n");
				base.sprite.SetAtlasReplace("HotDog_skin7_3.png", "uid://ja25qci6b505");
				base.sprite.SetAtlasReplace("HotDog_skin7_4.png", "uid://b2iah3f5ft2t5");
				base.sprite.SetAtlasReplace("HotDog_skin7_5.png", "uid://cghcs27icauh4");
				base.sprite.SetAtlasReplace("HotDog_skin7_6.png", "uid://df0v50tov07ky");
			}
			break;
		case "Damage1":
			base.sprite.SetAtlasReplace("HotDog_0001_9.png", "uid://kddmvlhxk80f");
			base.sprite.SetAtlasReplace("HotDog_0002_8.png", "uid://u14pwe08qshs");
			base.sprite.SetAtlasReplace("HotDog_0004_6.png", "uid://clxq2sslv2v7o");
			if (base.currentCustom.Contains("Custom0"))
			{
				base.sprite.SetAtlasReplace("HotDog_skin3_1.png", "uid://dpb542keiv7j8");
				base.sprite.SetAtlasReplace("HotDog_skin4_1.png", "uid://b3c8rks3u7ldu");
				base.sprite.SetAtlasReplace("HotDog_skin6_1.png", "uid://bbbscktm0lt4a");
				base.sprite.SetAtlasReplace("HotDog_skin7_2.png", "uid://ciw3bqm4vccey");
				base.sprite.SetAtlasReplace("HotDog_skin7_4.png", "uid://cii3syd5r3q5q");
				base.sprite.SetAtlasReplace("HotDog_skin7_6.png", "uid://b3r7s1txwktrs");
			}
			break;
		case "Damage2":
			base.sprite.SetAtlasReplace("HotDog_0001_9.png", "uid://bt2crx2aj2b2b");
			base.sprite.SetAtlasReplace("HotDog_0002_8.png", "uid://vtktjc5ueuwf");
			base.sprite.SetAtlasReplace("HotDog_0004_6.png", "uid://b7ejbiskk5r6v");
			if (base.currentCustom.Contains("Custom0"))
			{
				base.sprite.SetAtlasReplace("HotDog_skin3_1.png", "uid://dp84ritbd4lbw");
				base.sprite.SetAtlasReplace("HotDog_skin4_1.png", "uid://doxybk50iiwhe");
				base.sprite.SetAtlasReplace("HotDog_skin6_1.png", "uid://dciued0lcnkxo");
				base.sprite.SetAtlasReplace("HotDog_skin7_2.png", "uid://bj8vrr866kdo2");
				base.sprite.SetAtlasReplace("HotDog_skin7_3.png", "uid://bw0wgcemhi40e");
				base.sprite.SetAtlasReplace("HotDog_skin7_4.png", "uid://dlsvytiqbcbt2");
				base.sprite.SetAtlasReplace("HotDog_skin7_5.png", "uid://bdwsmked5boiu");
				base.sprite.SetAtlasReplace("HotDog_skin7_6.png", "uid://cd6irfup6sokx");
			}
			break;
		}
	}

	protected internal override void OnHypnosisStateChanged()
	{
		base.OnHypnosisStateChanged();
		_produceComponent.produceType = (instance.hypnoses ? "BrainSun" : "Sun");
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			["produceInterval"] = produceInterval,
			["sunNum"] = sunNum,
			["fireNum"] = fireNum,
			["fireInterval"] = fireInterval
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		produceInterval = (data.ContainsKey("produceInterval") ? data["produceInterval"].AsDouble() : 25.0);
		sunNum = (data.ContainsKey("sunNum") ? data["sunNum"].AsInt32() : 50);
		fireNum = ((!data.ContainsKey("fireNum")) ? 1 : data["fireNum"].AsInt32());
		fireInterval = (data.ContainsKey("fireInterval") ? data["fireInterval"].AsDouble() : 2.0);
	}
}
