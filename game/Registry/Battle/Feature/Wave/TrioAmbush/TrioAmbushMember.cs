using System;
using Godot;
using Godot.Collections;

public partial class TrioAmbushMember : Node
{
	private enum EntryPhase
	{
		Emerging,
		Descending,
		Leaving,
		Repelled,
		Complete
	}

	public const string StateKey = "trio_ambush";

	public const string CoralSaveKey = "trio_coral";

	private const string CarrierScene = "res://Registry/Battle/Feature/Wave/TrioAmbush/TowerDefenseTrioBungi.tscn";

	private TowerDefenseZombie _zombie;

	private TowerDefenseTrioBungi _carrier;

	private TowerDefenseBattleFeatureWave _wave;

	private BattleEventBus _bus;

	private int _operation = -1;

	private bool _coral;

	private bool _initialized;

	private bool _savedGravity;

	private bool _savedInvincible;

	private bool _savedGroundHeightAlive;

	private readonly string[] _waterLayers = new string[3] { "Zombie_duckytube", "Zombie_whitewater", "Zombie_whitewater2" };

	private readonly bool[] _savedWaterFilters = new bool[3];

	private int _savedMask;

	private int _savedCollision;

	private double _ground;

	private double _height;

	private double _elapsed;

	private float _unitScale = 1f;

	private EntryPhase _phase;

	public int SyncId = -1;

	public bool CanRepel
	{
		get
		{
			if (_phase == EntryPhase.Descending)
			{
				return _height <= 40.0;
			}
			return false;
		}
	}

	public bool EntryPending => _phase != EntryPhase.Complete;

	public bool IsCoral => _coral;

	public double Height => _height;

	public static TrioAmbushMember Attach(TowerDefenseZombie zombie, bool coral, double height, TowerDefenseBattleFeatureWave wave = null, int operation = -1, bool restored = false)
	{
		TrioAmbushMember nodeOrNull = zombie.GetNodeOrNull<TrioAmbushMember>("TrioAmbushMember");
		if (GodotObject.IsInstanceValid(nodeOrNull))
		{
			return nodeOrNull;
		}
		TrioAmbushMember trioAmbushMember = new TrioAmbushMember
		{
			Name = "TrioAmbushMember",
			_zombie = zombie,
			_coral = coral,
			_height = height,
			_wave = wave,
			_operation = operation,
			_phase = (restored ? EntryPhase.Complete : ((!coral) ? EntryPhase.Descending : EntryPhase.Emerging))
		};
		if (coral)
		{
			zombie.SetMeta("trio_coral", true);
		}
		zombie.AddChild(trioAmbushMember, forceReadableName: false, InternalMode.Disabled);
		return trioAmbushMember;
	}

	public override void _Ready()
	{
		base.ProcessMode = ProcessModeEnum.Pausable;
		base.ProcessPhysicsPriority = 10001;
		_bus = BattleEventBus.Instance;
		if (!_coral && GodotObject.IsInstanceValid(_bus))
		{
			_bus.OnColdEffectEmit += FreezeCarriedPayload;
		}
		if (_zombie.IsNodeReady())
		{
			Initialize();
		}
	}

	private void Initialize()
	{
		if (_initialized)
		{
			return;
		}
		_initialized = true;
		_savedGravity = _zombie.gravityUse;
		_savedInvincible = _zombie.instance.invincible;
		_savedGroundHeightAlive = _zombie.groundHeightComponent?.Alive ?? false;
		_savedMask = _zombie.instance.maskFlags;
		_savedCollision = _zombie.instance.collisionFlags;
		_ground = (_zombie.inWater ? (0.0 - _zombie.waterHeight) : (_zombie.cell?.GetGroundHeight() ?? 0.0));
		_unitScale = Math.Max(0.01f, TowerDefenseManager.Instance.gridSize.X / 80f);
		if (_coral)
		{
			TrioSeaweed.Attach(_zombie);
			if (_phase == EntryPhase.Complete)
			{
				SetPhysicsProcess(enable: false);
				return;
			}
			_zombie.isRise = true;
			_zombie.groundHeightComponent?.SetAlive(alive: false);
			_zombie.OnRiseStart();
			_zombie.CreateSplash();
		}
		else
		{
			CreateCarrier();
			_zombie.Idle();
			_zombie.instance.canBeCollection = false;
			_zombie.instance.maskFlags = 1;
			_zombie.instance.invincible = true;
			_zombie.instance.collisionFlags = 0;
			for (int i = 0; i < _waterLayers.Length; i++)
			{
				_savedWaterFilters[i] = _zombie.sprite.GetFliter(_waterLayers[i]);
				_zombie.sprite.SetFliter(_waterLayers[i], open: false);
			}
			if (GodotObject.IsInstanceValid(_zombie.waterLineSprite))
			{
				_zombie.waterLineSprite.Visible = false;
			}
			if (GodotObject.IsInstanceValid(_zombie.duckytobeSprite))
			{
				_zombie.duckytobeSprite.Visible = false;
			}
		}
		_zombie.forceLocalRenderDuringZMotion = true;
		_zombie.gravityUse = false;
		_zombie.isGround = false;
		_zombie.ySpeed = 0.0;
		_zombie.shadowSprite.Visible = false;
		StopMovement();
		Present();
	}

	private void StopMovement()
	{
		AttackComponent attackComponent = _zombie.attackComponent;
		if (attackComponent != null && !attackComponent.IsReleased)
		{
			_zombie.attackComponent.alive = false;
		}
		GroundMoveComponent groundMoveComponent = _zombie.groundMoveComponent;
		if (groundMoveComponent != null && !groundMoveComponent.IsReleased)
		{
			_zombie.groundMoveComponent.SetAlive(false);
		}
	}

	private void CreateCarrier()
	{
		if (!GodotObject.IsInstanceValid(_carrier))
		{
			_carrier = GD.Load<PackedScene>("res://Registry/Battle/Feature/Wave/TrioAmbush/TowerDefenseTrioBungi.tscn").Instantiate<TowerDefenseTrioBungi>(PackedScene.GenEditState.Disabled);
			_carrier.Member = this;
			_carrier.gridPos = _zombie.gridPos;
			_carrier.SetLogicalGlobalPosition(_zombie.GetLogicalGlobalPosition() + new Vector2(15f * _unitScale, 0f));
			_carrier.z = _height * (double)_unitScale;
			_zombie.GetParent().AddChild(_carrier, forceReadableName: false, InternalMode.Disabled);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!GodotObject.IsInstanceValid(_zombie) || _zombie.IsQueuedForDeletion())
		{
			CompleteOperation();
		}
		else
		{
			if (!_zombie.IsNodeReady())
			{
				return;
			}
			if (!_initialized)
			{
				Initialize();
			}
			if (!TowerDefenseManager.Instance.IsGameRunning() || GetTree().Paused)
			{
				return;
			}
			if (_zombie.die || _zombie.nearDie)
			{
				CancelEntry();
				return;
			}
			if (_wave != null && (!_wave.IsLifetimeActive || (_operation >= 0 && !_wave.IsPendingSpawnOperationCurrent(_operation))))
			{
				CancelEntry();
				return;
			}
			switch (_phase)
			{
			case EntryPhase.Emerging:
				StopMovement();
				if (!_zombie.buff.buffDictionary.ContainsKey("Frozen"))
				{
					_elapsed += delta;
				}
				Present();
				if (_elapsed + 1E-09 >= 0.5)
				{
					Land();
				}
				break;
			case EntryPhase.Descending:
			{
				StopMovement();
				double height = _height;
				_height = Math.Max(0.0, _height - 800.0 * delta);
				if (height >= 1500.0 && _height < 1500.0)
				{
					AudioManager.Instance.AudioPlay("BungeeScream");
				}
				Present();
				if (CanRepel && (!Global.IsMultiplayerMode || MultiPlayerManager.IsHost))
				{
					foreach (Variant item in TowerDefenseManager.Instance.GetCampTarget(_carrier.camp))
					{
						((TowerDefenseCharacter)(GodotObject)item).componentManager?.GetRuntime<BlockComponent>()?.BlockCheckCharacter(_carrier);
					}
				}
				if (_phase == EntryPhase.Descending && _height <= 0.0)
				{
					Land();
				}
				break;
			}
			case EntryPhase.Leaving:
			case EntryPhase.Repelled:
				_height += 800.0 * delta;
				Present();
				if (_height >= 600.0)
				{
					bool num = _phase == EntryPhase.Repelled;
					_phase = EntryPhase.Complete;
					CompleteOperation();
					ReleaseCarrier();
					if (num && (!Global.IsMultiplayerMode || MultiPlayerManager.IsHost))
					{
						_zombie.Destroy();
					}
					SetPhysicsProcess(enable: false);
				}
				break;
			}
		}
	}

	private void Present()
	{
		if (_phase == EntryPhase.Emerging)
		{
			_zombie.isGround = true;
			_zombie.groundHeight = Mathf.Lerp(-150f * _unitScale, _ground, Math.Clamp(_elapsed / 0.5, 0.0, 1.0));
			_zombie.z = _zombie.groundHeight;
			_zombie.spriteGroup.Position = new Vector2(_zombie.spriteGroup.Position.X, 0f - (float)_zombie.z);
			Transform2D screenTransform = _zombie.GetViewport().GetScreenTransform();
			screenTransform.Origin = Vector2.Zero;
			float y = (screenTransform * (_zombie.GetLogicalGlobalPosition(_zombie.transformPoint) + new Vector2(0f, (float)_zombie.z * _zombie.spriteGroup.GlobalScale.Y))).Y;
			_zombie.SetSpriteGroupShaderParameter("discardDownPos", y);
		}
		else
		{
			if (!GodotObject.IsInstanceValid(_carrier))
			{
				return;
			}
			_carrier.z = _ground + _height * (double)_unitScale;
			_carrier.spriteGroup.Position = new Vector2(_carrier.spriteGroup.Position.X, 0f - (float)_carrier.z);
			_carrier.sprite.timeScale = 3.0;
			if (_phase == EntryPhase.Leaving)
			{
				return;
			}
			string[] waterLayers = _waterLayers;
			foreach (string text in waterLayers)
			{
				if (_zombie.sprite.GetFliter(text))
				{
					_zombie.sprite.SetFliter(text, open: false);
				}
			}
			if (GodotObject.IsInstanceValid(_zombie.waterLineSprite))
			{
				_zombie.waterLineSprite.Visible = false;
			}
			if (GodotObject.IsInstanceValid(_zombie.duckytobeSprite))
			{
				_zombie.duckytobeSprite.Visible = false;
			}
			_zombie.z = _carrier.z;
			_zombie.spriteGroup.Position = new Vector2(_zombie.spriteGroup.Position.X, 0f - (float)_zombie.z);
			_zombie.SetSpriteGroupShaderParameter("discardDownPos", 10000.0);
		}
	}

	private void Land()
	{
		_zombie.gravityUse = _savedGravity;
		_zombie.instance.invincible = _savedInvincible;
		_zombie.isGround = true;
		_zombie.isRise = false;
		_zombie.groundHeight = _ground;
		if (_coral)
		{
			_zombie.groundHeightComponent?.SetAlive(_savedGroundHeightAlive);
		}
		_zombie.z = _ground;
		_zombie.ySpeed = 0.0;
		_zombie.spriteGroup.Position = new Vector2(_zombie.spriteGroup.Position.X, 0f - (float)_ground);
		_zombie.instance.maskFlags = _savedMask;
		_zombie.instance.collisionFlags = _savedCollision;
		_zombie.instance.canBeCollection = true;
		_zombie.shadowSprite.Visible = !_zombie.inWater && !_zombie.invisible;
		if (!_coral)
		{
			for (int i = 0; i < _waterLayers.Length; i++)
			{
				_zombie.sprite.SetFliter(_waterLayers[i], _savedWaterFilters[i]);
			}
		}
		if (_zombie.inWater)
		{
			_zombie.waterInteractionComponent?.InWaterDiscardSet();
			if (GodotObject.IsInstanceValid(_zombie.waterLineSprite))
			{
				_zombie.waterLineSprite.Visible = true;
			}
			if (GodotObject.IsInstanceValid(_zombie.duckytobeSprite))
			{
				_zombie.duckytobeSprite.Visible = true;
			}
		}
		else
		{
			_zombie.SetSpriteGroupShaderParameter("discardDownPos", 10000.0);
		}
		_zombie.groundMoveComponent?.SetAlive(true);
		_zombie.OnRiseEnd();
		_phase = (_coral ? EntryPhase.Complete : EntryPhase.Leaving);
		if (_coral)
		{
			CompleteOperation();
			SetPhysicsProcess(enable: false);
		}
		else if (GodotObject.IsInstanceValid(_carrier))
		{
			_carrier.sprite.SetAnimation("Raise", loop: false, 0.2);
		}
		PublishPhase();
	}

	public void Repel()
	{
		if (CanRepel)
		{
			_phase = EntryPhase.Repelled;
			StopMovement();
			PublishPhase();
		}
	}

	public void FreezeCarriedPayload()
	{
		if (_coral || _phase != EntryPhase.Descending || !_initialized || (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost))
		{
			return;
		}
		BuffComponent buff = _zombie.buff;
		if (buff == null || buff.IsReleased)
		{
			return;
		}
		bool flag = _zombie.buff.buffDictionary.ContainsKey("Frozen") || _zombie.iceSpeedDown;
		int maskFlags = _zombie.instance.maskFlags;
		try
		{
			_zombie.instance.maskFlags = 1;
			_zombie.buff.AddBuff(new TowerDefenseCharacterBuffFrozen
			{
				time = (flag ? GD.RandRange(3.0, 4.0) : GD.RandRange(4.0, 6.0)),
				iceSpeedDownTime = 20.0
			});
		}
		finally
		{
			_zombie.instance.maskFlags = maskFlags;
		}
	}

	private void CompleteOperation()
	{
		if (_operation >= 0 && GodotObject.IsInstanceValid(_wave))
		{
			_wave.CompletePendingSpawnOperation(_operation);
		}
		_operation = -1;
	}

	private void ReleaseCarrier()
	{
		if (GodotObject.IsInstanceValid(_carrier) && !_carrier.IsQueuedForDeletion())
		{
			_carrier.Destroy();
		}
		_carrier = null;
	}

	public void CancelEntry()
	{
		bool num = _phase != EntryPhase.Complete;
		_phase = EntryPhase.Complete;
		CompleteOperation();
		ReleaseCarrier();
		if (num && GodotObject.IsInstanceValid(_zombie))
		{
			_zombie.gravityUse = _savedGravity;
			_zombie.isRise = false;
			if (!_zombie.die && !_zombie.nearDie && !_zombie.isDestroy && (!Global.IsMultiplayerMode || MultiPlayerManager.IsHost))
			{
				_zombie.Destroy();
			}
		}
		_phase = EntryPhase.Complete;
		SetPhysicsProcess(enable: false);
	}

	public override void _ExitTree()
	{
		if (GodotObject.IsInstanceValid(_bus))
		{
			_bus.OnColdEffectEmit -= FreezeCarriedPayload;
		}
		CancelEntry();
	}

	public static Dictionary ExportSpawnState(TowerDefenseCharacter character)
	{
		TrioAmbushMember nodeOrNull = character.GetNodeOrNull<TrioAmbushMember>("TrioAmbushMember");
		if (!GodotObject.IsInstanceValid(nodeOrNull) || (!nodeOrNull._coral && nodeOrNull._phase == EntryPhase.Complete))
		{
			return new Dictionary();
		}
		return new Dictionary { ["trio_ambush"] = new Dictionary
		{
			["coral"] = nodeOrNull._coral,
			["height"] = nodeOrNull._height,
			["elapsed"] = nodeOrNull._elapsed,
			["phase"] = (int)nodeOrNull._phase
		} };
	}

	private void PublishPhase()
	{
		if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost && SyncId >= 0)
		{
			MultiPlayerManager.Instance.SendSpawnCharacterAt(_zombie.packet.saveKey, _zombie.gridPos.X, _zombie.gridPos.Y, SyncId, 1.0, 1.0, hypnoses: false, 0.0, useCreate: false, 0.0, 0.0, walkAfterSpawn: false, 0.0, "", ExportSpawnState(_zombie));
		}
	}

	public static void ApplyNetworkState(TowerDefenseCharacter character, Dictionary state)
	{
		if (!(character is TowerDefenseZombie towerDefenseZombie) || state == null || !state.ContainsKey("trio_ambush"))
		{
			return;
		}
		Dictionary dictionary = state["trio_ambush"].AsGodotDictionary();
		bool flag = dictionary.GetValueOrDefault("coral", false).AsBool();
		int num = dictionary.GetValueOrDefault("phase", 0).AsInt32();
		if (!flag && num == 4)
		{
			return;
		}
		TrioAmbushMember trioAmbushMember = Attach(towerDefenseZombie, flag, dictionary.GetValueOrDefault("height", 0.0).AsDouble(), null, -1, flag && num == 4);
		trioAmbushMember._elapsed = Math.Max(trioAmbushMember._elapsed, dictionary.GetValueOrDefault("elapsed", 0.0).AsDouble());
		switch (num)
		{
		case 3:
			trioAmbushMember._phase = EntryPhase.Repelled;
			trioAmbushMember._height = dictionary.GetValueOrDefault("height", 0.0).AsDouble();
			if (trioAmbushMember._initialized)
			{
				trioAmbushMember.CreateCarrier();
				towerDefenseZombie.gravityUse = false;
				towerDefenseZombie.isGround = false;
				towerDefenseZombie.instance.invincible = true;
				towerDefenseZombie.instance.canBeCollection = false;
				towerDefenseZombie.instance.collisionFlags = 0;
				trioAmbushMember.StopMovement();
				trioAmbushMember.SetPhysicsProcess(enable: true);
			}
			break;
		case 2:
			if (trioAmbushMember._phase == EntryPhase.Descending && trioAmbushMember._initialized)
			{
				trioAmbushMember.Land();
			}
			break;
		}
	}
}
