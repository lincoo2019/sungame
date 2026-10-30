using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieWorker : TowerDefenseZombieDigger
{
	private bool _signPlaced;

	private Vector2I _emergeGrid;

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

	public override void DigEntered()
	{
		base.DigEntered();
		UpdateFacing(true);
	}

	public override void DigProcessing(double delta)
	{
		if (IsRemoteClient)
		{
			base.sprite.timeScale = timeScale;
			return;
		}
		ulong currentPhysicsFrame = TowerDefenseProcessModeDispatch.CurrentPhysicsFrame;
		Vector2 globalPositionForPhysicsFrame = GetGlobalPositionForPhysicsFrame(currentPhysicsFrame);
		float x = globalPositionForPhysicsFrame.X;
		if (attackComponent.CanAttack())
		{
			if (!nearDie && !base.sprite.pause && base.sprite.timeScale > 0.0 && base.useAttackDps && GodotObject.IsInstanceValid(attackComponent.target))
			{
				if ((attackComponent.target.instance.collisionFlags & 0x10) != 0)
				{
					attackComponent.AttackDpsExecute(delta, ((TowerDefenseZombieConfig)base.config).attack);
				}
				else
				{
					attackComponent.target = null;
				}
			}
		}
		else if (!base.sprite.pause)
		{
			double num = speed * delta * base.sprite.timeScale * (double)transformPoint.Scale.X * (double)((!base.sprite.playBack) ? 1 : (-1));
			if ((double)globalPositionForPhysicsFrame.X > TowerDefenseManager.Instance.GetMapGroundRight())
			{
				num *= 2.0;
			}
			globalPositionForPhysicsFrame.X -= (float)num;
			SetGlobalPositionForPhysicsFrame(globalPositionForPhysicsFrame, currentPhysicsFrame);
		}
		base.sprite.timeScale = timeScale;
		if (digOver || base.sprite.pause || timeScale <= 0.0)
		{
			return;
		}
		TowerDefenseWorkerSign towerDefenseWorkerSign = TowerDefenseWorkerSign.FindInLane(GetTree(), base.gridPos.Y, instance.hypnoses);
		if (GodotObject.IsInstanceValid(towerDefenseWorkerSign))
		{
			float x2 = towerDefenseWorkerSign.GetLogicalGlobalPosition().X;
			if ((x >= x2 && globalPositionForPhysicsFrame.X <= x2) || Mathf.Abs(globalPositionForPhysicsFrame.X - x2) < 1f)
			{
				globalPositionForPhysicsFrame.X = x2;
				SetGlobalPositionForPhysicsFrame(globalPositionForPhysicsFrame, currentPhysicsFrame);
				base.gridPos = towerDefenseWorkerSign.gridPos;
				digOver = true;
				SendStateEvent("ToDrill");
				return;
			}
		}
		if ((double)globalPositionForPhysicsFrame.X < TowerDefenseManager.Instance.GetMapGroundLeft() + 30.0)
		{
			digOver = true;
			SendStateEvent("ToDrill");
		}
	}

	public override void DrillEntered()
	{
		if (!IsRemoteClient && TowerDefenseManager.Instance != null)
		{
			_emergeGrid = TowerDefenseManager.Instance.GetMapGridPos(GetLogicalGlobalPosition());
			Vector2I mapGridNum = TowerDefenseManager.Instance.GetMapGridNum();
			_emergeGrid = new Vector2I(Mathf.Clamp(_emergeGrid.X, 1, mapGridNum.X), base.gridPos.Y);
		}
		UpdateFacing(false);
		base.DrillEntered();
	}

	public override void LandEntered()
	{
		instance.collisionFlags = 1;
		instance.maskFlags = 9;
		base.sprite.SetAnimation("Land", loop: false);
		base.sprite.SetFliter("Zombie_digger_pickaxe", open: false);
		if (!IsRemoteClient)
		{
			UpdateFacing(false);
			PlaceSign();
		}
	}

	protected internal override void OnHypnosisStateChanged()
	{
		base.OnHypnosisStateChanged();
		UpdateFacing();
	}

	private void UpdateFacing(bool? digging = null)
	{
		if (!IsRemoteClient && GodotObject.IsInstanceValid(instance))
		{
			int num = TowerDefenseManager.Instance?.GetMapGridNum().X ?? 0;
			bool flag = (digging ?? (base.CurrentStateHandle?.StableId == "zombie.digger.dig")) || (!instance.hypnoses && (!digOver || (_emergeGrid.X >= 3 && _emergeGrid.X <= num)));
			float num2 = Mathf.Abs(base.Scale.X) * (float)(flag ? 1 : (-1));
			if (base.Scale.X != num2)
			{
				base.Scale = new Vector2(num2, base.Scale.Y);
				base.sprite?.NotifyAncestorTransformChangedForRender();
				groundMoveComponent?.RefreshDirectionCache();
			}
		}
	}

	private void PlaceSign()
	{
		if (_signPlaced || !inGame || editorPreviewMode || suppressDeathrattles || IsRemoteClient || !GodotObject.IsInstanceValid(TowerDefenseManager.GetMapCell(_emergeGrid)))
		{
			return;
		}
		bool flag = GodotObject.IsInstanceValid(instance) && instance.hypnoses;
		TowerDefenseCharacter towerDefenseCharacter = TowerDefenseManager.GetPacketConfig("WorkerSign").Plant(_emergeGrid, playAudio: false, noLimit: true, default(EconomyAccountId), skipPlacementCheck: true);
		if (GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			_signPlaced = true;
			if (flag)
			{
				towerDefenseCharacter.Hypnoses();
			}
			TowerDefenseManager.PublishSpawnedCharacter("WorkerSign", towerDefenseCharacter, useCreate: false, 0.0, walkAfterSpawn: false, "", new Dictionary
			{
				["plant_skip_placement_check"] = true,
				["plant_play_audio"] = false,
				["hypnoses"] = flag
			});
		}
	}

	public override void AnimeCompleted(string clip)
	{
		if (clip == "Land")
		{
			if (!IsRemoteClient && !die && !nearDie)
			{
				WalkWithoutTransitionDelay();
			}
		}
		else if (!IsRemoteClient)
		{
			base.AnimeCompleted(clip);
		}
	}

	public override Dictionary ExportVariantSave()
	{
		Dictionary dictionary = base.ExportVariantSave();
		dictionary["signPlaced"] = _signPlaced;
		dictionary["emergeGrid"] = _emergeGrid;
		return dictionary;
	}

	public override void ImportVariantSave(Dictionary data)
	{
		base.ImportVariantSave(data);
		_signPlaced = data.GetValueOrDefault("signPlaced", false).AsBool();
		_emergeGrid = data.GetValueOrDefault("emergeGrid", base.gridPos).AsVector2I();
	}

	public override Dictionary ExportNetworkSpecialState()
	{
		Dictionary dictionary = base.ExportNetworkSpecialState();
		dictionary["digOver"] = digOver;
		dictionary["signPlaced"] = _signPlaced;
		dictionary["emergeGrid"] = _emergeGrid;
		return dictionary;
	}

	public override void ImportNetworkSpecialState(Dictionary data)
	{
		digOver = data.GetValueOrDefault("digOver", digOver).AsBool();
		_signPlaced = data.GetValueOrDefault("signPlaced", _signPlaced).AsBool();
		_emergeGrid = data.GetValueOrDefault("emergeGrid", _emergeGrid).AsVector2I();
	}

	public override int GetNetworkSpecialStateRevision()
	{
		return (int)((digOver ? 1u : 0u) | (uint)(_signPlaced ? 2 : 0)) | (_emergeGrid.X << 2);
	}

	public override bool IsNetworkSpecialMovementActive()
	{
		switch (base.CurrentStateHandle?.StableId)
		{
		case "zombie.digger.dig":
		case "zombie.digger.drill":
		case "zombie.digger.land":
			return true;
		default:
			return false;
		}
	}
}
