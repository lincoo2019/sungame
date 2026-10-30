using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantHypnoPatch : TowerDefensePlant
{
	private const string HYPNO_PATCH_BODY2 = "uid://trisvt6u5l4b";

	private const string HYPNO_PATCH_BODY3 = "uid://byo6orm75quhp";

	private const string HYPNO_PATCH_BODY = "uid://btsule42cwjrl";

	private const string HYPNO_PATCH_HEAD2 = "uid://bwfpe1t5e5b75";

	private const string HYPNO_PATCH_HEAD3 = "uid://by6prmaaf1ocu";

	private const string HYPNO_PATCH_HEAD = "uid://2hdb3rhrsahj";

	public bool over;

	public int eatTime;

	public TowerDefensePacketConfig spawnPacket;

	public double maxHitpoint = -1.0;

	public double spawnTimer;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			ApplyCapturedVisualState();
		}
	}

	public override void IdleProcessing(double delta)
	{
		base.IdleProcessing(delta);
		if (!TowerDefenseManager.HasGameplayAuthority || eatTime <= 0)
		{
			return;
		}
		spawnTimer += delta;
		if (!(spawnTimer >= 30.0) || !GodotObject.IsInstanceValid(spawnPacket))
		{
			return;
		}
		base.sprite.SetAnimation("Spawn", loop: false, 0.2);
		spawnTimer = 0.0;
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		TowerDefenseCharacter towerDefenseCharacter = (base.HasEconomyOwner ? spawnPacket.Create(base.EconomyOwnerAccountId, logicalGlobalPosition, base.gridPos, base.groundHeight) : spawnPacket.Create(logicalGlobalPosition, base.gridPos, base.groundHeight));
		if (GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			TowerDefenseManager.GetCharacterNode().AddChild(towerDefenseCharacter, forceReadableName: false, InternalMode.Disabled);
			Vector2 logicalGlobalPosition2 = towerDefenseCharacter.GetLogicalGlobalPosition();
			towerDefenseCharacter.SetLogicalGlobalPosition(new Vector2(logicalGlobalPosition.X, logicalGlobalPosition2.Y));
			if (!instance.hypnoses)
			{
				towerDefenseCharacter.Hypnoses();
			}
			TowerDefenseManager.PublishSpawnedCharacter(spawnPacket.saveKey, towerDefenseCharacter, useCreate: true, 0.0, walkAfterSpawn: true);
			Tween tween = towerDefenseCharacter.CreateTween();
			tween.SetEase(Tween.EaseType.Out);
			tween.SetTrans(Tween.TransitionType.Back);
			tween.SetParallel();
			tween.TweenProperty(towerDefenseCharacter.transformPoint, "scale", Vector2.One, 0.5).From(Vector2.One * 0.5f);
			towerDefenseCharacter.sprite.pause = false;
			towerDefenseCharacter.SetHitBoxMonitorable(monitorable: true);
			if (towerDefenseCharacter is TowerDefenseZombie zombie)
			{
				StartSummonedZombieWalking(zombie);
			}
		}
	}

	private async void StartSummonedZombieWalking(TowerDefenseZombie zombie)
	{
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		if (GodotObject.IsInstanceValid(zombie))
		{
			zombie.Walk();
		}
	}

	public override void AttackDeal(TowerDefenseCharacter character, string type, double num)
	{
		base.AttackDeal(character, type, num);
		if (!TowerDefenseManager.HasGameplayAuthority || instance.sleep || !GodotObject.IsInstanceValid(character) || !(type == "Eat"))
		{
			return;
		}
		character.Hypnoses();
		if (instance.hypnoses != character.instance.hypnoses)
		{
			eatTime++;
			double totalHitPoint = character.GetTotalHitPoint();
			if (totalHitPoint > maxHitpoint)
			{
				maxHitpoint = totalHitPoint;
				spawnPacket = character.packet;
			}
			ApplyCapturedVisualState();
		}
		if (eatTime >= 3)
		{
			Destroy();
		}
	}

	private void ApplyCapturedVisualState()
	{
		if (GodotObject.IsInstanceValid(base.sprite))
		{
			int num = Mathf.Clamp(eatTime, 0, 2);
			switch (num)
			{
			case 1:
				base.sprite.SetAtlasReplace("HypnoPatch_body.png", "uid://trisvt6u5l4b");
				base.sprite.SetAtlasReplace("HypnoPatch_head.png", "uid://bwfpe1t5e5b75");
				break;
			case 2:
				base.sprite.SetAtlasReplace("HypnoPatch_body.png", "uid://byo6orm75quhp");
				base.sprite.SetAtlasReplace("HypnoPatch_head.png", "uid://by6prmaaf1ocu");
				break;
			default:
				base.sprite.SetAtlasReplace("HypnoPatch_body.png", "uid://btsule42cwjrl");
				base.sprite.SetAtlasReplace("HypnoPatch_head.png", "uid://2hdb3rhrsahj");
				break;
			}
			base.sprite.SetFliters(new Array { "piece1" }, num < 1);
			base.sprite.SetFliters(new Array { "piece4_1", "piece4_2" }, num < 2);
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (clip == "Spawn")
		{
			Idle();
		}
	}

	public override void Cover(TowerDefenseCharacter character)
	{
		base.Cover(character);
		if (character.config.name == "PlantHypnoShroom" && character.instance.wakeUp)
		{
			instance.wakeUp = true;
		}
	}

	public override Dictionary ExportVariantSave()
	{
		Dictionary dictionary = new Dictionary
		{
			["over"] = over,
			["eatTime"] = eatTime,
			["maxHitpoint"] = maxHitpoint,
			["spawnTimer"] = spawnTimer
		};
		if (GodotObject.IsInstanceValid(spawnPacket))
		{
			dictionary["spawnPacketName"] = spawnPacket.saveKey;
		}
		return dictionary;
	}

	public override void ImportVariantSave(Dictionary data)
	{
		over = data.ContainsKey("over") && data["over"].AsBool();
		eatTime = (data.ContainsKey("eatTime") ? data["eatTime"].AsInt32() : 0);
		maxHitpoint = (data.ContainsKey("maxHitpoint") ? data["maxHitpoint"].AsDouble() : (-1.0));
		spawnTimer = (data.ContainsKey("spawnTimer") ? data["spawnTimer"].AsDouble() : 0.0);
		if (data.ContainsKey("spawnPacketName"))
		{
			spawnPacket = TowerDefenseManager.GetPacketConfig(data["spawnPacketName"].AsString());
		}
		ApplyCapturedVisualState();
	}

	public override Dictionary ExportNetworkSpecialState()
	{
		Dictionary dictionary = base.ExportNetworkSpecialState();
		dictionary["eat_time"] = eatTime;
		return dictionary;
	}

	public override void ImportNetworkSpecialState(Dictionary data)
	{
		if (data != null)
		{
			base.ImportNetworkSpecialState(data);
			if (data.ContainsKey("eat_time"))
			{
				eatTime = data["eat_time"].AsInt32();
			}
			ApplyCapturedVisualState();
		}
	}
}
