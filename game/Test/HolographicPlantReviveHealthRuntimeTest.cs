using System;
using System.Reflection;
using Godot;

public partial class HolographicPlantReviveHealthRuntimeTest : Node
{
	private const string WallnutConfigPath = "res://Asset/Anime/Character/Plant/Chapter9/WallnutQX/Config/TowerDefensePlantWallnutQX.tres";

	private const string WallnutSpritePath = "res://Asset/Anime/Character/Plant/Chapter9/WallnutQX/WallnutQX.tscn";

	private const string WallnutMediaName = "WallnutQX_body.png";

	private const string GravestoneConfigPath = "res://Asset/Anime/Character/GraveStone/TargetQX/Config/TowerDefenseGraveStoneTargetQX.tres";

	private const string GravestoneSpritePath = "res://Asset/Anime/Character/GraveStone/TargetQX/GraveStoneTargetQX.tscn";

	private const string GravestoneMediaName = "TargetStonesQX_1.png";

	private int _checks;

	private int _failures;

	public override void _Ready()
	{
		try
		{
			VerifySharedPlantRecovery();
			VerifyWallnutDamageStageRecovery();
			VerifyGravestoneDamageStageRecovery();
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[HolographicPlantReviveHealthRuntimeTest] Unexpected exception: {value}");
		}
		bool flag = _failures == 0 && _checks == 23;
		GD.Print($"HOLOGRAPHIC_PLANT_REVIVE_HEALTH_RUNTIME_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private void VerifySharedPlantRecovery()
	{
		TowerDefensePlant towerDefensePlant = new TowerDefensePlant();
		TowerDefensePlantConfig towerDefensePlantConfig = (TowerDefensePlantConfig)(towerDefensePlant.config = new TowerDefensePlantConfig
		{
			hitpoints = 475.0,
			collisionFlags = 25,
			maskFlags = 25
		});
		towerDefensePlant.instance = new TowerDefenseCharacterInstance();
		towerDefensePlant.instance._Init(towerDefensePlant, towerDefensePlantConfig);
		MarkDead(towerDefensePlant);
		MethodInfo method = typeof(TowerDefensePlant).GetMethod("RestoreFullHealthAfterRevive", BindingFlags.Instance | BindingFlags.NonPublic);
		Check(method != null, "TowerDefensePlant must expose the protected revive-health operation.");
		method?.Invoke(towerDefensePlant, null);
		Check(Mathf.IsEqualApprox(towerDefensePlant.instance.hitpoints, towerDefensePlant.instance.hitpointsSave), "Revived plants must restore their runtime maximum hitpoints.");
		Check(!towerDefensePlant.instance.die && !towerDefensePlant.die, "Revived plants must clear both instance and character death flags.");
		Check(!towerDefensePlant.instance.nearDie && !towerDefensePlant.nearDie, "Revived plants must clear both instance and character near-death flags.");
		Check(towerDefensePlant.instance.collisionFlags == towerDefensePlantConfig.collisionFlags, "Revived plants must restore their configured collision identity.");
		Check(towerDefensePlant.instance.maskFlags == towerDefensePlantConfig.maskFlags, "Revived plants must restore their configured projectile target mask.");
		towerDefensePlant.Free();
	}

	private void VerifyWallnutDamageStageRecovery()
	{
		TowerDefensePlantConfig towerDefensePlantConfig = ResourceLoader.Load<TowerDefensePlantConfig>("res://Asset/Anime/Character/Plant/Chapter9/WallnutQX/Config/TowerDefensePlantWallnutQX.tres", null, ResourceLoader.CacheMode.Ignore);
		PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter9/WallnutQX/WallnutQX.tscn", null, ResourceLoader.CacheMode.Ignore);
		Check(GodotObject.IsInstanceValid(towerDefensePlantConfig), "The production holographic Wallnut config must load.");
		Check(GodotObject.IsInstanceValid(packedScene), "The production holographic Wallnut sprite must load.");
		if (GodotObject.IsInstanceValid(towerDefensePlantConfig) && GodotObject.IsInstanceValid(packedScene))
		{
			TowerDefensePlantWallnutQX towerDefensePlantWallnutQX = new TowerDefensePlantWallnutQX();
			AdobeAnimateSprite adobeAnimateSprite = packedScene.Instantiate<AdobeAnimateSprite>(PackedScene.GenEditState.Disabled);
			towerDefensePlantWallnutQX.config = towerDefensePlantConfig;
			towerDefensePlantWallnutQX.sprite = adobeAnimateSprite;
			towerDefensePlantWallnutQX.AddChild(adobeAnimateSprite, forceReadableName: false, InternalMode.Disabled);
			towerDefensePlantWallnutQX.instance = new TowerDefenseCharacterInstance();
			towerDefensePlantWallnutQX.instance._Init(towerDefensePlantWallnutQX, towerDefensePlantConfig);
			MarkDead(towerDefensePlantWallnutQX);
			ApplyDamageStage(towerDefensePlantWallnutQX, "Damage2", 3, "WallnutQX_body.png");
			typeof(TowerDefensePlantWallnutQX).GetMethod("RestoreFullHealthAfterRevive", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(towerDefensePlantWallnutQX, null);
			Check(Mathf.IsEqualApprox(towerDefensePlantWallnutQX.instance.hitpoints, towerDefensePlantWallnutQX.instance.hitpointsSave), "Holographic Wallnut must restore full hitpoints.");
			Check(towerDefensePlantWallnutQX.instance.damagePointIndex == 0, "Holographic Wallnut must reset damagePointIndex to zero at full hitpoints.");
			Check(string.IsNullOrEmpty(adobeAnimateSprite.GetAtlasReplacePath("WallnutQX_body.png")), "Holographic Wallnut must clear its damaged atlas replacement at full hitpoints.");
			towerDefensePlantWallnutQX.Free();
		}
	}

	private void VerifyGravestoneDamageStageRecovery()
	{
		TowerDefenseGravestoneConfig towerDefenseGravestoneConfig = ResourceLoader.Load<TowerDefenseGravestoneConfig>("res://Asset/Anime/Character/GraveStone/TargetQX/Config/TowerDefenseGraveStoneTargetQX.tres", null, ResourceLoader.CacheMode.Ignore);
		PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/GraveStone/TargetQX/GraveStoneTargetQX.tscn", null, ResourceLoader.CacheMode.Ignore);
		Check(GodotObject.IsInstanceValid(towerDefenseGravestoneConfig), "The production holographic Gravestone config must load.");
		Check(GodotObject.IsInstanceValid(packedScene), "The production holographic Gravestone sprite must load.");
		if (GodotObject.IsInstanceValid(towerDefenseGravestoneConfig) && GodotObject.IsInstanceValid(packedScene))
		{
			TowerDefenseGraveStoneTargetQX towerDefenseGraveStoneTargetQX = new TowerDefenseGraveStoneTargetQX();
			AdobeAnimateSprite adobeAnimateSprite = packedScene.Instantiate<AdobeAnimateSprite>(PackedScene.GenEditState.Disabled);
			towerDefenseGraveStoneTargetQX.config = towerDefenseGravestoneConfig;
			towerDefenseGraveStoneTargetQX.sprite = adobeAnimateSprite;
			towerDefenseGraveStoneTargetQX.AddChild(adobeAnimateSprite, forceReadableName: false, InternalMode.Disabled);
			towerDefenseGraveStoneTargetQX.instance = new TowerDefenseCharacterInstance();
			towerDefenseGraveStoneTargetQX.instance._Init(towerDefenseGraveStoneTargetQX, towerDefenseGravestoneConfig);
			MarkDead(towerDefenseGraveStoneTargetQX);
			towerDefenseGraveStoneTargetQX.instance.canBeCollection = false;
			ApplyDamageStage(towerDefenseGraveStoneTargetQX, "Damage4", 5, "TargetStonesQX_1.png");
			MethodInfo method = typeof(TowerDefenseGraveStoneTargetQX).GetMethod("RestoreNormalState", BindingFlags.Instance | BindingFlags.NonPublic);
			Check(method != null, "Holographic Gravestone must expose its private normal-state restore operation.");
			method?.Invoke(towerDefenseGraveStoneTargetQX, null);
			Check(Mathf.IsEqualApprox(towerDefenseGraveStoneTargetQX.instance.hitpoints, towerDefenseGraveStoneTargetQX.instance.hitpointsSave), "Holographic Gravestone must restore full hitpoints.");
			Check(!towerDefenseGraveStoneTargetQX.instance.die && !towerDefenseGraveStoneTargetQX.die && !towerDefenseGraveStoneTargetQX.instance.nearDie && !towerDefenseGraveStoneTargetQX.nearDie, "Holographic Gravestone must clear its death and near-death flags.");
			Check(towerDefenseGraveStoneTargetQX.instance.damagePointIndex == 0, "Holographic Gravestone must reset damagePointIndex to zero at full hitpoints.");
			Check(string.IsNullOrEmpty(adobeAnimateSprite.GetAtlasReplacePath("TargetStonesQX_1.png")), "Holographic Gravestone must clear its damaged atlas replacement at full hitpoints.");
			Check(towerDefenseGraveStoneTargetQX.instance.canBeCollection, "Holographic Gravestone must restore its targetable state.");
			Check(towerDefenseGraveStoneTargetQX.instance.maskFlags == towerDefenseGravestoneConfig.maskFlags, "Holographic Gravestone must restore its projectile-blocking mask.");
			Check((GroundProjectileFlags() & towerDefenseGraveStoneTargetQX.instance.maskFlags) != 0, "Holographic Gravestone must block ordinary ground projectiles after revive.");
			towerDefenseGraveStoneTargetQX.Free();
		}
	}

	private static void MarkDead(TowerDefenseCharacter character)
	{
		character.instance.hitpoints = 0.0;
		character.instance.die = true;
		character.instance.nearDie = true;
		character.instance.collisionFlags = 0;
		character.instance.maskFlags = 4;
		character.die = true;
		character.nearDie = true;
	}

	private static int GroundProjectileFlags()
	{
		return 9;
	}

	private void ApplyDamageStage(TowerDefenseCharacter character, string damagePointName, int damagePointIndex, StringName mediaName)
	{
		character.instance.damagePointData.SetDamagePointFliters(character.sprite, damagePointName);
		character.instance.damagePointIndex = damagePointIndex;
		Check(!string.IsNullOrEmpty(character.sprite.GetAtlasReplacePath(mediaName)), "The damaged fixture for " + character.GetType().Name + " must install an atlas replacement.");
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[HolographicPlantReviveHealthRuntimeTest] " + message);
		}
	}
}
