using System;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieSkeletuar : TowerDefenseZombieGargantuarBase, INetworkSpawnStateReceiver
{
	public bool over;

	private const string ZOMBIE_SKELETUAR_HEAD2 = "uid://c4pcthsh66g7r";

	public override async void _Ready()
	{
		base._Ready();
		if (Engine.IsEditorHint())
		{
			return;
		}
		if (over)
		{
			ApplyRevivedThrowContract();
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			if (GodotObject.IsInstanceValid(base.sprite) && over && !die)
			{
				base.sprite.SetAnimation("Relife", loop: false, 0.2);
			}
		}
		ConfigureWaterLineVisualLayers("Zombie_duckytube");
	}

	private void ApplyRevivedThrowContract()
	{
		if (over)
		{
			impThrowDamagePointName = "";
			impThrowFlag = false;
			SetImpFilter();
		}
	}

	public override Dictionary ExportVariantSave()
	{
		Dictionary dictionary = base.ExportVariantSave();
		dictionary["over"] = over;
		return dictionary;
	}

	public override void ImportVariantSave(Dictionary data)
	{
		base.ImportVariantSave(data);
		over = data.GetValueOrDefault("over", over).AsBool();
		ApplyRevivedThrowContract();
	}

	public void ImportNetworkSpawnState(Dictionary data)
	{
		over = data.GetValueOrDefault("skeletuar_revived", over).AsBool();
		ApplyRevivedThrowContract();
	}

	public override Dictionary ExportNetworkSpawnState()
	{
		return new Dictionary { ["skeletuar_revived"] = over };
	}

	public override Dictionary ExportNetworkSpecialState()
	{
		return new Dictionary
		{
			["rev"] = GetNetworkSpecialStateRevision(),
			["over"] = over
		};
	}

	public override void ImportNetworkSpecialState(Dictionary data)
	{
		over = data.GetValueOrDefault("over", over).AsBool();
		ApplyRevivedThrowContract();
	}

	public override int GetNetworkSpecialStateRevision()
	{
		return over ? 1 : 0;
	}

	public override void DamagePointReach(string damagePointName)
	{
		base.DamagePointReach(damagePointName);
		if (damagePointName == "Head")
		{
			base.sprite.SetAtlasReplace("Zombie_Skeletuar_head.png", "uid://c4pcthsh66g7r");
		}
	}

	public override async void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (clip == "Relife")
		{
			Walk();
		}
		string[] array = base.dieAnimeClip.Split("&", StringSplitOptions.RemoveEmptyEntries);
		for (int i = 0; i < array.Length; i++)
		{
			if (!(array[i] == clip))
			{
				continue;
			}
			if (base.inWater || over)
			{
				break;
			}
			if (!TowerDefenseManager.HasGameplayAuthority)
			{
				over = true;
				await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
				Destroy();
				break;
			}
			over = true;
			TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("Skeleton");
			if (!GodotObject.IsInstanceValid(packetConfig))
			{
				break;
			}
			Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
			TowerDefenseCharacter skeleton = (base.EconomyOwnerAccountId.IsValid ? packetConfig.Create(base.EconomyOwnerAccountId, logicalGlobalPosition, base.gridPos) : packetConfig.Create(logicalGlobalPosition, base.gridPos));
			if (GodotObject.IsInstanceValid(skeleton))
			{
				TowerDefenseManager.GetCharacterNode().AddChild(skeleton, forceReadableName: false, InternalMode.Disabled);
				double hitpointScale = instance.hitpointScale;
				Vector2 scale = transformPoint.Scale;
				skeleton.CallDeferred("SetHitpointAndScale", hitpointScale, scale);
				skeleton.SetDeferred("invisible", base.invisible);
				Dictionary spawnState = new Dictionary { ["spawn_invisible"] = base.invisible };
				Callable.From(() => TowerDefenseManager.PublishSpawnedCharacter("Skeleton", skeleton, useCreate: true, 0.0, walkAfterSpawn: false, "", spawnState)).CallDeferred();
				await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
				Destroy();
			}
			break;
		}
	}

	public override void InWater()
	{
		base.InWater();
		base.sprite.SetFliter("Zombie_whitewater", open: true);
	}

	public override void OutWater()
	{
		base.OutWater();
		base.sprite.SetFliter("Zombie_whitewater", open: false);
	}
}
