using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieGhost : TowerDefenseZombie, INetworkSpawnStateReceiver
{
	private struct GhostOverlapSelector(TowerDefenseZombieGhost owner, TowerDefenseZombie currentTarget, bool needsTarget, bool needsCarrier) : ITowerDefenseCharacterRectSelector
	{
		private readonly TowerDefenseZombieGhost _owner = owner;

		private readonly TowerDefenseZombie _currentTarget = currentTarget;

		private readonly bool _needsTarget = needsTarget;

		private readonly bool _needsCarrier = needsCarrier;

		private bool _candidateIsTarget = false;

		private bool _candidateIsCarrier = false;

		public TowerDefenseZombie FirstTarget { get; private set; } = null;

		public bool CurrentTargetFound { get; private set; } = false;

		public TowerDefenseZombie Carrier { get; private set; } = null;

		public bool CanConsider(TowerDefenseCharacter character)
		{
			_candidateIsTarget = false;
			_candidateIsCarrier = false;
			if (!(character is TowerDefenseZombie towerDefenseZombie) || towerDefenseZombie == _owner)
			{
				return false;
			}
			_candidateIsTarget = _needsTarget && towerDefenseZombie.config.name == _owner.config.name && towerDefenseZombie.camp != _owner.camp;
			_candidateIsCarrier = _needsCarrier && _owner.CanCarryZombie(towerDefenseZombie);
			if (!_candidateIsTarget)
			{
				return _candidateIsCarrier;
			}
			return true;
		}

		public bool Visit(TowerDefenseCharacter character, ref TowerDefenseCharacter match)
		{
			TowerDefenseZombie towerDefenseZombie = (TowerDefenseZombie)character;
			if (_candidateIsTarget)
			{
				if (FirstTarget == null)
				{
					TowerDefenseZombie towerDefenseZombie2 = (FirstTarget = towerDefenseZombie);
				}
				if (towerDefenseZombie == _currentTarget)
				{
					CurrentTargetFound = true;
				}
			}
			if (_candidateIsCarrier && Carrier == null)
			{
				TowerDefenseZombie towerDefenseZombie2 = (Carrier = towerDefenseZombie);
			}
			bool num = !_needsTarget || (GodotObject.IsInstanceValid(_currentTarget) ? CurrentTargetFound : GodotObject.IsInstanceValid(FirstTarget));
			bool flag = !_needsCarrier || GodotObject.IsInstanceValid(Carrier);
			if (!num || !flag)
			{
				return false;
			}
			match = (GodotObject.IsInstanceValid(Carrier) ? Carrier : (GodotObject.IsInstanceValid(_currentTarget) ? _currentTarget : FirstTarget));
			return true;
		}
	}

	public TowerDefenseZombie carryCharacter;

	public double ghostTimeScaleSave = 1.0;

	private int _pendingCarrierSyncId = -1;

	private double _pendingCarrierBaseTimeScale = 0.0 / 0.0;

	private string _pendingCarrierNodeName = "";

	private bool _carrierRelationApplied;

	private bool _suppressAutomaticCarrierSearch;

	private bool _ghost = true;

	private bool _ghostVisualApplied;

	private float _ghostOpaqueAlpha = 1f;

	public double timer;

	public double time = 40.0;

	public bool ghost
	{
		get
		{
			return _ghost;
		}
		set
		{
			_ghost = value;
			ApplyGhostPresentation();
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (Engine.IsEditorHint() || editorPreviewMode)
		{
			return;
		}
		targetRegistrationComponent.canCarry = false;
		ghost = true;
		if (GodotObject.IsInstanceValid(carryCharacter))
		{
			if (_carrierRelationApplied)
			{
				ApplyCarrierPresentation();
			}
			else
			{
				Carry(carryCharacter);
			}
		}
		else
		{
			ResolvePendingCarrier();
			ResolvePendingProgressCarrier();
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (Engine.IsEditorHint())
		{
			return;
		}
		ApplyGhostPresentation();
		if (!GodotObject.IsInstanceValid(TowerDefenseManager.Instance.currentControl) || !TowerDefenseManager.Instance.currentControl.isGameRunning || !inGame)
		{
			return;
		}
		ResolvePendingCarrier();
		ResolvePendingProgressCarrier();
		if (GodotObject.IsInstanceValid(carryCharacter))
		{
			UpdateCarrierPresentation();
		}
		if (base.IsRemoteNetworkReplica || die || nearDie)
		{
			return;
		}
		if (GodotObject.IsInstanceValid(carryCharacter))
		{
			if (carryCharacter.nearDie || carryCharacter.die)
			{
				Die();
				return;
			}
			if (carryCharacter.isDestroy)
			{
				float x = carryCharacter.GetLogicalGlobalPosition().X;
				if ((double)x > carryCharacter.groundRight + 150.0 || (double)x < TowerDefenseManager.Instance.GetMapGroundLeft() - 150.0)
				{
					DetachGhost();
				}
				else
				{
					Die();
				}
				return;
			}
		}
		else if (carryCharacter != null)
		{
			DetachGhost();
			return;
		}
		if (!base.IsInsideComponentBattlefield)
		{
			return;
		}
		bool flag = CheckGhost();
		if (ghost != flag)
		{
			ghost = flag;
		}
		ProcessHitBoxOverlaps();
		if (instance.hypnoses && !base.sprite.pause)
		{
			if (timer < time)
			{
				timer += delta * timeScale;
			}
			else if (!GodotObject.IsInstanceValid(carryCharacter))
			{
				Die();
			}
		}
	}

	private void ApplyGhostPresentation()
	{
		if (GodotObject.IsInstanceValid(base.sprite) && GodotObject.IsInstanceValid(instance))
		{
			if (!_ghostVisualApplied)
			{
				_ghostOpaqueAlpha = base.sprite.meshColor.A;
			}
			Color meshColor = base.sprite.meshColor;
			meshColor.A = (_ghost ? (_ghostOpaqueAlpha * 0.5f) : _ghostOpaqueAlpha);
			base.sprite.meshColor = meshColor;
			instance.canBeCollection = !_ghost;
			TargetRegistrationComponent obj = targetRegistrationComponent;
			if (obj != null && !obj.IsReleased)
			{
				targetRegistrationComponent.canProjectileCheck = !_ghost;
			}
			if (!_ghost)
			{
				instance.maskFlags = 9;
			}
			_ghostVisualApplied = true;
		}
	}

	public override void WalkProcessing(double delta)
	{
		base.WalkProcessing(delta);
		if (ghost && !nearDie && !base.sprite.pause && base.sprite.timeScale > 0.0 && base.useAttackDps && attackComponent.target != null)
		{
			attackComponent.AttackDpsExecute(delta, ((TowerDefenseZombieConfig)base.config).attack);
		}
	}

	public override Dictionary ExportNetworkSpecialState()
	{
		int num = (GodotObject.IsInstanceValid(carryCharacter) ? carryCharacter.syncId : _pendingCarrierSyncId);
		return new Dictionary
		{
			["rev"] = GetNetworkSpecialStateRevision(),
			["ghost"] = ghost,
			["carrierSyncId"] = num,
			["ghostTimeScaleSave"] = ghostTimeScaleSave
		};
	}

	public override void ImportNetworkSpecialState(Dictionary data)
	{
		ghost = data.GetValueOrDefault("ghost", ghost).AsBool();
		QueueNetworkCarrier(data);
	}

	public void ImportNetworkSpawnState(Dictionary data)
	{
		if (data != null)
		{
			QueueNetworkCarrier(data);
		}
	}

	public override int GetNetworkSpecialStateRevision()
	{
		int num = (GodotObject.IsInstanceValid(carryCharacter) ? carryCharacter.syncId : _pendingCarrierSyncId);
		return (int)((ghost ? 1u : 0u) ^ (uint)(num * 397)) ^ (Mathf.RoundToInt(ghostTimeScaleSave * 1000.0) * 31);
	}

	private void QueueNetworkCarrier(Dictionary data)
	{
		int num = data.GetValueOrDefault("carrierSyncId", -1).AsInt32();
		if (num < 0)
		{
			_pendingCarrierSyncId = -1;
			_pendingCarrierBaseTimeScale = 0.0 / 0.0;
			DetachCarrierRelation();
		}
		else
		{
			_pendingCarrierSyncId = num;
			_pendingCarrierBaseTimeScale = data.GetValueOrDefault("ghostTimeScaleSave", ghostTimeScaleSave).AsDouble();
			ResolvePendingCarrier();
		}
	}

	private void ResolvePendingCarrier()
	{
		if (_pendingCarrierSyncId >= 0)
		{
			TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
			if (GodotObject.IsInstanceValid(currentControl) && currentControl._syncCharacters.TryGetValue(_pendingCarrierSyncId, out var value) && value is TowerDefenseZombie carrier && GodotObject.IsInstanceValid(carrier))
			{
				double baseTimeScale = (double.IsNaN(_pendingCarrierBaseTimeScale) ? ghostTimeScaleSave : _pendingCarrierBaseTimeScale);
				_pendingCarrierSyncId = -1;
				_pendingCarrierBaseTimeScale = 0.0 / 0.0;
				AttachCarrierRelation(carrier, baseTimeScale);
			}
		}
	}

	private void ResolvePendingProgressCarrier()
	{
		if (string.IsNullOrEmpty(_pendingCarrierNodeName))
		{
			return;
		}
		Node2D node2D = TowerDefenseManager.GetCharacterNode();
		if (GodotObject.IsInstanceValid(node2D))
		{
			TowerDefenseZombie nodeOrNull = node2D.GetNodeOrNull<TowerDefenseZombie>(new NodePath(_pendingCarrierNodeName));
			if (GodotObject.IsInstanceValid(nodeOrNull))
			{
				_pendingCarrierNodeName = "";
				AttachCarrierRelation(nodeOrNull, ghostTimeScaleSave);
			}
		}
	}

	private void ProcessHitBoxOverlaps()
	{
		if (!GodotObject.IsInstanceValid(TowerDefenseManager.Instance.currentControl) || !TowerDefenseManager.Instance.currentControl.isGameRunning || !inGame || nearDie || die)
		{
			return;
		}
		TowerDefenseBattleCharacterRegistry characterRegistry = TowerDefenseManager.Instance.characterRegistry;
		bool flag = ghost && characterRegistry.HasCharacterOutsideCampWithConfigName(base.camp, base.config.name);
		bool flag2 = !_suppressAutomaticCarrierSearch && !GodotObject.IsInstanceValid(carryCharacter) && characterRegistry.HasCharacterInCampWithDifferentConfigName(base.camp, base.config.name);
		Rect2 rect;
		if (!flag && !flag2)
		{
			attackComponent.target = null;
		}
		else if (TryGetActiveWorldHitRect(out rect))
		{
			TowerDefenseZombie towerDefenseZombie = attackComponent.target as TowerDefenseZombie;
			if (!GodotObject.IsInstanceValid(towerDefenseZombie) || towerDefenseZombie.config.name != base.config.name || towerDefenseZombie.camp == base.camp)
			{
				towerDefenseZombie = null;
			}
			GhostOverlapSelector selector = new GhostOverlapSelector(this, towerDefenseZombie, flag, flag2);
			characterRegistry.TrySelectCharacterIntersectingRectClassified(rect, base.camp, base.config.name, flag, flag2, ref selector, out var _);
			if (flag2 && GodotObject.IsInstanceValid(selector.Carrier))
			{
				Carry(selector.Carrier);
			}
			attackComponent.target = ((!ghost) ? null : (selector.CurrentTargetFound ? towerDefenseZombie : selector.FirstTarget));
		}
	}

	private bool CanCarryZombie(TowerDefenseZombie zombie)
	{
		if (!GodotObject.IsInstanceValid(zombie))
		{
			return false;
		}
		if (zombie.isRise)
		{
			return false;
		}
		if (zombie.config.name == base.config.name)
		{
			return false;
		}
		if (zombie.hasGhost)
		{
			return false;
		}
		if ((zombie.instance.maskFlags & 0x10) != 0 && (zombie.instance.maskFlags & 1) == 0)
		{
			return false;
		}
		if (zombie.instance.zombiePhysique == TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS)
		{
			return false;
		}
		if (zombie.camp != base.camp)
		{
			return false;
		}
		if (!zombie.targetRegistrationComponent.canCarry)
		{
			return false;
		}
		return true;
	}

	public void Carry(TowerDefenseZombie character)
	{
		if (GodotObject.IsInstanceValid(character) && (!_carrierRelationApplied || carryCharacter != character))
		{
			double baseTimeScale = character.timeScaleInit;
			AttachCarrierRelation(character, baseTimeScale);
			character.Health(instance.hitpoints);
		}
	}

	public void DetachGhost()
	{
		DetachCarrierRelation();
	}

	private void AttachCarrierRelation(TowerDefenseZombie carrier, double baseTimeScale)
	{
		if (GodotObject.IsInstanceValid(carrier))
		{
			if (GodotObject.IsInstanceValid(carryCharacter) && carryCharacter != carrier)
			{
				DetachCarrierRelation(reconnectHitBox: false);
			}
			carryCharacter = carrier;
			ghostTimeScaleSave = ((baseTimeScale > 0.0) ? baseTimeScale : 1.0);
			carryCharacter.hasGhost = true;
			carryCharacter.ghostCharacter = this;
			carryCharacter.timeScaleInit = ghostTimeScaleSave * 2.0;
			_carrierRelationApplied = true;
			_suppressAutomaticCarrierSearch = false;
			UpdateCarrierPresentation();
		}
	}

	private void ApplyCarrierPresentation()
	{
		if (GodotObject.IsInstanceValid(carryCharacter))
		{
			base.groundHeight = carryCharacter.groundHeight;
			base.z = 40.0;
		}
	}

	private void UpdateCarrierPresentation()
	{
		ApplyCarrierPresentation();
		if (GodotObject.IsInstanceValid(carryCharacter))
		{
			ulong num = TowerDefenseProcessModeDispatch.CurrentPhysicsFrame;
			if (num == 18446744073709551615uL)
			{
				num = Engine.GetPhysicsFrames();
			}
			Vector2 globalPositionForPhysicsFrame = carryCharacter.GetGlobalPositionForPhysicsFrame(num);
			float x = ((GodotObject.IsInstanceValid(carryCharacter.instance) && carryCharacter.instance.hypnoses) ? (-5f) : 5f);
			SetGlobalPositionForPhysicsFrame(globalPositionForPhysicsFrame + new Vector2(x, 0f), num);
			base.gridPos = new Vector2I(base.gridPos.X, carryCharacter.gridPos.Y);
		}
	}

	private void DetachCarrierRelation(bool reconnectHitBox = true)
	{
		if (GodotObject.IsInstanceValid(carryCharacter) && (carryCharacter.ghostCharacter == this || !GodotObject.IsInstanceValid(carryCharacter.ghostCharacter)))
		{
			carryCharacter.timeScaleInit = ghostTimeScaleSave;
			carryCharacter.hasGhost = false;
			carryCharacter.ghostCharacter = null;
		}
		carryCharacter = null;
		_carrierRelationApplied = false;
		_pendingCarrierSyncId = -1;
		_pendingCarrierBaseTimeScale = 0.0 / 0.0;
		base.groundHeight = 0.0;
		base.z = 0.0;
		if (reconnectHitBox)
		{
			_ReconnectHitBox();
		}
	}

	public void _ReconnectHitBox()
	{
		if (base.HasHitBox)
		{
			SetHitBoxEnabled(enabled: true);
			if (!base.IsRemoteNetworkReplica)
			{
				ProcessHitBoxOverlaps();
			}
		}
	}

	public bool CheckGhost()
	{
		bool flag = true;
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(base.gridPos + new Vector2I(i, j));
				if (GodotObject.IsInstanceValid(mapCell) && mapCell.HasLight())
				{
					flag = false;
					break;
				}
			}
			if (!flag)
			{
				break;
			}
		}
		return flag;
	}

	public override void Hypnoses(double time = -1.0, bool canFliter = true, TowerDefenseCharacterBuffHypnoses hypnosesConfig = null)
	{
		base.Hypnoses(time, canFliter, hypnosesConfig);
		DetachCarrierRelation();
	}

	public override async void DestroySet()
	{
		DetachCarrierRelation(reconnectHitBox: false);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
	}

	public override Dictionary ExportVariantSave()
	{
		Dictionary dictionary = new Dictionary
		{
			{ "ghost", ghost },
			{ "ghostTimeScaleSave", ghostTimeScaleSave },
			{ "timer", timer },
			{ "time", time },
			{ "carrierRelationVersion", 1 }
		};
		if (GodotObject.IsInstanceValid(carryCharacter))
		{
			dictionary["carryCharacterNodeName"] = carryCharacter.Name;
		}
		return dictionary;
	}

	public override bool ImportVariantSaveWhenEmpty()
	{
		return true;
	}

	public override void ImportVariantSave(Dictionary data)
	{
		ghost = data.GetValueOrDefault("ghost", true).AsBool();
		ghostTimeScaleSave = data.GetValueOrDefault("ghostTimeScaleSave", 1.0).AsDouble();
		timer = data.GetValueOrDefault("timer", timer).AsDouble();
		time = data.GetValueOrDefault("time", time).AsDouble();
		_pendingCarrierNodeName = data.GetValueOrDefault("carryCharacterNodeName", "").AsString();
		_suppressAutomaticCarrierSearch = !data.ContainsKey("carrierRelationVersion") && string.IsNullOrEmpty(_pendingCarrierNodeName);
		ResolvePendingProgressCarrier();
	}

	public override void FinalizeProgressRestore()
	{
		base.FinalizeProgressRestore();
		ResolvePendingProgressCarrier();
		if (_suppressAutomaticCarrierSearch)
		{
			CallDeferred("TryResolveLegacyCarrier");
		}
	}

	private void TryResolveLegacyCarrier()
	{
		if (!_suppressAutomaticCarrierSearch || GodotObject.IsInstanceValid(carryCharacter))
		{
			return;
		}
		Node2D node2D = TowerDefenseManager.GetCharacterNode();
		if (!GodotObject.IsInstanceValid(node2D))
		{
			return;
		}
		TowerDefenseZombie carrier = null;
		foreach (Node child in node2D.GetChildren())
		{
			if (child is TowerDefenseZombie towerDefenseZombie && towerDefenseZombie != this && towerDefenseZombie.hasGhost && !GodotObject.IsInstanceValid(towerDefenseZombie.ghostCharacter) && towerDefenseZombie.camp == base.camp && Mathf.IsEqualApprox(towerDefenseZombie.timeScaleInit, ghostTimeScaleSave * 2.0))
			{
				if (GodotObject.IsInstanceValid(carrier))
				{
					return;
				}
				carrier = towerDefenseZombie;
			}
		}
		if (GodotObject.IsInstanceValid(carrier))
		{
			AttachCarrierRelation(carrier, ghostTimeScaleSave);
		}
	}
}
