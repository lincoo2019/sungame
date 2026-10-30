using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieRaker : TowerDefenseZombiePolevaulter
{
	private static readonly string[] RakeLayers = new string[2] { "Zombie_polevaulter_pole2 复制", "Zombie_polevaulter_pole 复制" };

	private bool _rakePlaced;

	private bool _deathRakePending;

	private bool _rakeVisualHidden;

	private Vector2I _jumpGrid;

	private int _jumpDirection = -1;

	private static bool IsRemoteClient
	{
		get
		{
			if (Global.IsMultiplayerMode)
			{
				return !MultiPlayerManager.IsHost;
			}
			return false;
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		RefreshRakeVisual();
	}

	private void RefreshRakeVisual()
	{
		if (!_rakeVisualHidden && _rakePlaced && GodotObject.IsInstanceValid(base.sprite))
		{
			Array array = new Array();
			string[] rakeLayers = RakeLayers;
			foreach (string text in rakeLayers)
			{
				array.Add(text);
			}
			base.sprite.SetFliters(array, open: false);
			_rakeVisualHidden = true;
		}
	}

	public override void DamagePointReach(string damagePointName)
	{
		base.DamagePointReach(damagePointName);
		if (damagePointName == "Head")
		{
			RefreshRakerAttachments();
		}
	}

	private void RefreshRakerAttachments()
	{
		if (base.sprite is ZombieRakerSprite zombieRakerSprite)
		{
			zombieRakerSprite.RefreshAttachments();
		}
		else if (base.sprite is ZombieRakerWallnutSprite zombieRakerWallnutSprite)
		{
			zombieRakerWallnutSprite.RefreshRakeAttachment();
		}
	}

	public override void JumpEntered()
	{
		if (!IsRemoteClient)
		{
			TowerDefenseCharacter towerDefenseCharacter = componentManager.GetRuntime<AttackComponent>("character.attack.1")?.target;
			_jumpGrid = (GodotObject.IsInstanceValid(towerDefenseCharacter) ? towerDefenseCharacter.gridPos : base.gridPos);
			_jumpDirection = ((!(base.Scale.X * transformPoint.Scale.X >= 0f)) ? 1 : (-1));
		}
		base.JumpEntered();
	}

	public override void JumpExited()
	{
		if (jumpOver && !die && !nearDie)
		{
			PlaceRake(_jumpGrid + new Vector2I(isBlock ? (-_jumpDirection) : _jumpDirection, 0));
		}
		base.JumpExited();
	}

	public override void Block(TowerDefenseCharacter target)
	{
		if (!IsRemoteClient && GodotObject.IsInstanceValid(target))
		{
			_jumpGrid = target.gridPos;
			_jumpDirection = ((!(base.Scale.X * transformPoint.Scale.X >= 0f)) ? 1 : (-1));
			PlaceRake(_jumpGrid + new Vector2I(-_jumpDirection, 0));
		}
		base.Block(target);
	}

	public override void DestroySet()
	{
		if ((_deathRakePending || !jumpOver) && TowerDefenseManager.Instance != null)
		{
			PlaceRake(TowerDefenseManager.Instance.GetMapGridPos(GetLogicalGlobalPosition()));
		}
		base.DestroySet();
	}

	public override void DieEntered()
	{
		if (!_rakePlaced)
		{
			_deathRakePending = true;
		}
		base.DieEntered();
	}

	private void PlaceRake(Vector2I at)
	{
		if (_rakePlaced || IsRemoteClient || !inGame || editorPreviewMode || suppressDeathrattles || !GodotObject.IsInstanceValid(TowerDefenseManager.GetMapCell(at)))
		{
			return;
		}
		bool hypnoses = GodotObject.IsInstanceValid(instance) && instance.hypnoses;
		TowerDefenseEnum.CHARACTER_CAMP sourceCamp = base.camp;
		TowerDefenseCharacter rake = TowerDefenseManager.GetPacketConfig("ReverseRake").Plant(at, playAudio: false, noLimit: true, default(EconomyAccountId), skipPlacementCheck: true);
		if (!GodotObject.IsInstanceValid(rake))
		{
			return;
		}
		_rakePlaced = true;
		_deathRakePending = false;
		RefreshRakeVisual();
		Dictionary spawnState = new Dictionary
		{
			["plant_skip_placement_check"] = true,
			["plant_play_audio"] = false,
			["hypnoses"] = hypnoses,
			["rake_camp"] = (int)sourceCamp
		};
		if (rake is TowerDefenseReverseRake towerDefenseReverseRake)
		{
			towerDefenseReverseRake.ImportNetworkSpawnState(spawnState);
		}
		Callable.From(delegate
		{
			if (GodotObject.IsInstanceValid(rake) && !rake.IsQueuedForDeletion() && !rake.isDestroy)
			{
				if (hypnoses && !rake.instance.hypnoses)
				{
					rake.Hypnoses();
				}
				rake.camp = sourceCamp;
				TowerDefenseManager.PublishSpawnedCharacter("ReverseRake", rake, useCreate: false, 0.0, walkAfterSpawn: false, "", spawnState);
			}
		}).CallDeferred();
	}

	public override void AnimeCompleted(string clip)
	{
		if (!IsRemoteClient || (!(clip == "Jump") && !(clip == "SwimJump")))
		{
			base.AnimeCompleted(clip);
		}
	}

	public override Dictionary ExportVariantSave()
	{
		Dictionary dictionary = base.ExportVariantSave();
		dictionary["rakePlaced"] = _rakePlaced;
		dictionary["deathRakePending"] = _deathRakePending;
		dictionary["jumpGrid"] = _jumpGrid;
		dictionary["jumpDirection"] = _jumpDirection;
		return dictionary;
	}

	public override void ImportVariantSave(Dictionary data)
	{
		base.ImportVariantSave(data);
		_rakePlaced = data.GetValueOrDefault("rakePlaced", false).AsBool();
		_deathRakePending = data.GetValueOrDefault("deathRakePending", false).AsBool();
		_jumpGrid = data.GetValueOrDefault("jumpGrid", base.gridPos).AsVector2I();
		_jumpDirection = data.GetValueOrDefault("jumpDirection", -1).AsInt32();
	}

	public override Dictionary ExportNetworkSpecialState()
	{
		Dictionary dictionary = base.ExportNetworkSpecialState();
		dictionary["rakePlaced"] = _rakePlaced;
		dictionary["deathRakePending"] = _deathRakePending;
		return dictionary;
	}

	public override void ImportNetworkSpecialState(Dictionary data)
	{
		base.ImportNetworkSpecialState(data);
		_rakePlaced = data.GetValueOrDefault("rakePlaced", _rakePlaced).AsBool();
		_deathRakePending = data.GetValueOrDefault("deathRakePending", _deathRakePending).AsBool();
	}

	public override int GetNetworkSpecialStateRevision()
	{
		return base.GetNetworkSpecialStateRevision() | (_rakePlaced ? 16 : 0) | (_deathRakePending ? 256 : 0);
	}
}
