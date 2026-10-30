using System;
using Godot;
using Godot.Collections;

public sealed class ProduceComponent : CharacterComponentRuntime
{
	private enum SunDropKind
	{
		Sun,
		BrainSun,
		JalapenoSun,
		QXSun,
		MagicSun
	}

	public delegate void ProductEventHandler(int pos, int num);

	private const float MinimumInterval = 0.001f;

	private const int MaximumHealthSegments = 1000;

	private const string SunType = "Sun";

	private const string BrainSunType = "BrainSun";

	private const string JalapenoSunType = "JalaSun";

	private const string QXSunType = "QXSun";

	private const string MagicSunType = "MagicSun";

	private const string CoinType = "Coin";

	private const string PacketType = "Packet";

	private const float DefaultSpawnGravity = 980f;

	private const float PacketAliveTime = 15f;

	private const string ProduceOperationName = "produce";

	private float _produceInterval = 25f;

	private float _effectiveProduceInterval = 25f;

	public Vector2 initialDelayRange = new Vector2(3f, 6f);

	public float glowLeadTime = 1.5f;

	public int healthProductionSegments = 6;

	public int maxCatchUpProductions = 1;

	public float glowBrightness = 0.5f;

	public float glowFadeInTime = 1.5f;

	public float glowFadeOutTime = 0.5f;

	public float timer;

	public bool produceGlowStarted;

	public TowerDefenseCharacter parent;

	public TowerDefenseCharacter currentTarget;

	public float hpNext;

	public float hpNextInterval = 50f;

	public bool isBaseIZM;

	private readonly Array<NodePath> _markerPaths = new Array<NodePath>();

	private NodePath _produceGlowTargetPath = new NodePath();

	private bool _destroyConnected;

	private bool _initialized;

	private bool _runtimeStateImported;

	private bool _configured;

	private bool _deathProductionFlushed;

	private bool _ownerDestroyPreflightObserved;

	private bool _ownerDeathConfirmedAtPreflight;

	private Tween _produceGlowTween;

	private Color _produceGlowBaseSelfModulate = Colors.White;

	private bool _produceGlowBaseCaptured;

	private long _nextOperationSequence;

	private long _lastAppliedOperationSequence = -1L;

	public bool _IZMMode { get; set; }

	public string produceType { get; set; } = "Sun";

	public float produceInterval
	{
		get
		{
			return _produceInterval;
		}
		set
		{
			_produceInterval = value;
			_effectiveProduceInterval = Mathf.Max(0.001f, value);
		}
	}

	public int num { get; set; } = 25;

	public int sunOnceMax { get; set; } = 50;

	public Array<Marker2D> marker { get; } = new Array<Marker2D>();

	public bool onlyEmit { get; set; }

	public AdobeAnimateSpriteBase produceGlowTarget { get; private set; }

	public bool coinRandom { get; set; } = true;

	public Array<string> packetName { get; } = new Array<string>();

	private ProduceComponentDefinition Definition => base.ComponentDefinition as ProduceComponentDefinition;

	internal override bool WantsPhysicsProcess => true;

	public event ProductEventHandler OnProduct;

	protected override void OnBound()
	{
		parent = base.Owner;
		ConfigureOnce();
		ResolveSceneReferences();
	}

	protected override void OnActivated()
	{
		InitializeComponent();
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		StopProduceGlow(restore: true);
		DisconnectDestroySignal();
		if (reason != ComponentDetachReason.TemporaryTreeExit)
		{
			_initialized = false;
			_deathProductionFlushed = false;
		}
		_ownerDestroyPreflightObserved = false;
		_ownerDeathConfirmedAtPreflight = false;
		marker.Clear();
		produceGlowTarget = null;
		parent = null;
	}

	protected override void OnReleased()
	{
		StopProduceGlow(restore: true);
		DisconnectDestroySignal();
		this.OnProduct = null;
		marker.Clear();
		packetName.Clear();
		_markerPaths.Clear();
		produceGlowTarget = null;
		parent = null;
		_initialized = false;
		_deathProductionFlushed = false;
		_ownerDestroyPreflightObserved = false;
		_ownerDeathConfirmedAtPreflight = false;
		_nextOperationSequence = 0L;
		_lastAppliedOperationSequence = -1L;
	}

	protected override void OnAliveChanged(bool alive)
	{
		if (!alive)
		{
			StopProduceGlow(restore: true);
			produceGlowStarted = false;
		}
	}

	private void ConfigureOnce()
	{
		if (_configured)
		{
			return;
		}
		ProduceComponentDefinition definition = Definition;
		_IZMMode = definition?._IZMMode ?? false;
		produceType = definition?.produceType ?? "Sun";
		produceInterval = definition?.produceInterval ?? 25f;
		num = definition?.num ?? 25;
		sunOnceMax = definition?.sunOnceMax ?? 50;
		onlyEmit = definition?.onlyEmit ?? false;
		initialDelayRange = definition?.initialDelayRange ?? new Vector2(3f, 6f);
		glowLeadTime = definition?.glowLeadTime ?? 1.5f;
		healthProductionSegments = definition?.healthProductionSegments ?? 6;
		maxCatchUpProductions = definition?.maxCatchUpProductions ?? 1;
		glowBrightness = definition?.glowBrightness ?? 0.5f;
		glowFadeInTime = definition?.glowFadeInTime ?? 1.5f;
		glowFadeOutTime = definition?.glowFadeOutTime ?? 0.5f;
		coinRandom = definition?.coinRandom ?? true;
		_markerPaths.Clear();
		if (definition?.markerPaths != null)
		{
			for (int i = 0; i < definition.markerPaths.Count; i++)
			{
				_markerPaths.Add(definition.markerPaths[i]);
			}
		}
		_produceGlowTargetPath = definition?.produceGlowTargetPath ?? new NodePath();
		packetName.Clear();
		if (definition?.packetName != null)
		{
			for (int j = 0; j < definition.packetName.Count; j++)
			{
				packetName.Add(definition.packetName[j]);
			}
		}
		_configured = true;
	}

	private void ResolveSceneReferences()
	{
		marker.Clear();
		if (!GodotObject.IsInstanceValid(parent))
		{
			return;
		}
		for (int i = 0; i < _markerPaths.Count; i++)
		{
			NodePath nodePath = _markerPaths[i];
			if (!nodePath.IsEmpty)
			{
				Marker2D nodeOrNull = parent.GetNodeOrNull<Marker2D>(nodePath);
				if (GodotObject.IsInstanceValid(nodeOrNull))
				{
					marker.Add(nodeOrNull);
				}
			}
		}
		produceGlowTarget = (_produceGlowTargetPath.IsEmpty ? null : parent.GetNodeOrNull<AdobeAnimateSpriteBase>(_produceGlowTargetPath));
	}

	private void InitializeComponent()
	{
		if (base.Lifecycle != ComponentRuntimeLifecycle.Active || !GodotObject.IsInstanceValid(parent) || !GodotObject.IsInstanceValid(parent.instance))
		{
			return;
		}
		if (!_destroyConnected)
		{
			parent.OnDestroy += Destroy;
			_destroyConnected = true;
		}
		if (!_initialized)
		{
			TowerDefenseManager instance = TowerDefenseManager.Instance;
			isBaseIZM = GodotObject.IsInstanceValid(instance) && (instance.IsIZMMode() || instance.IsIZM2Mode());
			if (isBaseIZM)
			{
				_IZMMode = true;
			}
			if (!_runtimeStateImported && !IsRemoteSyncedClient())
			{
				float effectiveProduceInterval = GetEffectiveProduceInterval();
				float num = Mathf.Max(0f, Mathf.Min(initialDelayRange.X, initialDelayRange.Y));
				float num2 = Mathf.Max(num, Mathf.Max(initialDelayRange.X, initialDelayRange.Y));
				timer = effectiveProduceInterval - (float)GD.RandRange(num, num2);
				InitializeHealthProductionState();
			}
			else if (hpNextInterval <= 0f)
			{
				InitializeHealthProductionState();
			}
			_initialized = true;
		}
	}

	private void DisconnectDestroySignal()
	{
		if (_destroyConnected && GodotObject.IsInstanceValid(parent))
		{
			parent.OnDestroy -= Destroy;
		}
		_destroyConnected = false;
	}

	private void InitializeHealthProductionState()
	{
		int effectiveHealthSegments = GetEffectiveHealthSegments();
		float num = Mathf.Max(0f, (float)parent.instance.hitpoints);
		hpNextInterval = num / (float)effectiveHealthSegments;
		hpNext = num - hpNextInterval;
	}

	internal override void PhysicsProcess(double delta, ulong physicsFrame)
	{
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(parent))
		{
			PhysicsProcessValidated(delta, physicsFrame);
		}
	}

	private void PhysicsProcessValidated(double deltaDouble, ulong physicsFrame)
	{
		if (_initialized && GodotObject.IsInstanceValid(parent.instance) && !parent.die && !parent.nearDie && !parent.instance.sleep && parent.componentAlive && !IsRemoteSyncedClient() && (TowerDefenseManager.CurrentControl == null || TowerDefenseManager.CurrentControl.isGameRunning))
		{
			float num = Mathf.Max(0f, (float)deltaDouble);
			BuffComponent buff = parent.buff;
			float num2 = ((buff != null && !buff.IsReleased && parent.buff.BuffHas("TabooBean")) ? 3f : 1f);
			if (parent is TowerDefensePlant && _IZMMode)
			{
				ProcessHealthProduction(physicsFrame);
			}
			else
			{
				ProcessTimedProduction(num * num2, physicsFrame);
			}
		}
	}

	private void ProcessTimedProduction(float scaledDelta, ulong physicsFrame)
	{
		float effectiveProduceInterval = GetEffectiveProduceInterval();
		timer += scaledDelta;
		if (!produceGlowStarted && timer >= effectiveProduceInterval - Mathf.Max(0f, glowLeadTime))
		{
			_StartProduceGlow();
			produceGlowStarted = true;
		}
		int num = 0;
		int num2 = Math.Max(1, maxCatchUpProductions);
		while (timer >= effectiveProduceInterval && num < num2)
		{
			timer -= effectiveProduceInterval;
			ProduceAtConfiguredPositions(this.num, emitEvent: true, physicsFrame);
			num++;
		}
		if (num > 0)
		{
			produceGlowStarted = false;
		}
	}

	private void ProcessHealthProduction(ulong physicsFrame)
	{
		if (!(hpNextInterval <= 0f))
		{
			int num = 0;
			int num2 = Math.Max(1, maxCatchUpProductions);
			while (parent.instance.hitpoints <= (double)hpNext && hpNextInterval > 0f && num < num2)
			{
				hpNext -= hpNextInterval;
				ProduceAtConfiguredPositions(this.num, emitEvent: true, physicsFrame);
				num++;
			}
		}
	}

	private void ProduceAtConfiguredPositions(int amount, bool emitEvent, ulong physicsFrame = 18446744073709551615uL)
	{
		if (!GodotObject.IsInstanceValid(parent) || amount <= 0)
		{
			return;
		}
		bool flag = onlyEmit && emitEvent && !Global.IsMultiplayerMode;
		bool flag2 = false;
		for (int i = 0; i < marker.Count; i++)
		{
			Marker2D marker2D = marker[i];
			if (GodotObject.IsInstanceValid(marker2D))
			{
				flag2 = true;
				if (flag)
				{
					this.OnProduct?.Invoke(i, amount);
				}
				else
				{
					ProduceAuthoritative(parent.GetLogicalGlobalPosition(marker2D), amount, i, emitEvent);
				}
			}
		}
		if (!flag2)
		{
			if (flag)
			{
				this.OnProduct?.Invoke(-1, amount);
				return;
			}
			Vector2 pos = ((physicsFrame != 18446744073709551615uL) ? parent.GetGlobalPositionForPhysicsFrame(physicsFrame) : parent.GetLogicalGlobalPosition());
			ProduceAuthoritative(pos, amount, -1, emitEvent);
		}
	}

	public void _StartProduceGlow()
	{
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(parent))
		{
			if (GodotObject.IsInstanceValid(produceGlowTarget))
			{
				StartProduceTargetGlow();
			}
			else
			{
				parent.Bright(0.0, 0.0, Mathf.Max(0f, glowBrightness), Mathf.Max(0f, glowFadeInTime), Mathf.Max(0f, glowFadeOutTime));
			}
		}
	}

	private void StartProduceTargetGlow()
	{
		StopProduceGlow(restore: true);
		if (GodotObject.IsInstanceValid(produceGlowTarget) && GodotObject.IsInstanceValid(parent))
		{
			_produceGlowBaseSelfModulate = produceGlowTarget.SelfModulate;
			_produceGlowBaseCaptured = true;
			SetProduceTargetGlowStrength(0.0);
			_produceGlowTween = parent.CreateTween();
			_produceGlowTween.TweenMethod(Callable.From<double>(SetProduceTargetGlowStrength), 0.0, Mathf.Max(0f, glowBrightness), Mathf.Max(0.001f, glowFadeInTime));
			_produceGlowTween.TweenMethod(Callable.From<double>(SetProduceTargetGlowStrength), Mathf.Max(0f, glowBrightness), 0.0, Mathf.Max(0.001f, glowFadeOutTime));
			_produceGlowTween.Finished += OnProduceGlowFinished;
		}
	}

	private void SetProduceTargetGlowStrength(double strength)
	{
		if (GodotObject.IsInstanceValid(produceGlowTarget))
		{
			float num = 1f + Mathf.Max(0f, (float)strength);
			produceGlowTarget.SetRenderSelfModulate(new Color(_produceGlowBaseSelfModulate.R * num, _produceGlowBaseSelfModulate.G * num, _produceGlowBaseSelfModulate.B * num, _produceGlowBaseSelfModulate.A));
		}
	}

	private void OnProduceGlowFinished()
	{
		StopProduceGlow(restore: true);
	}

	private void StopProduceGlow(bool restore)
	{
		if (GodotObject.IsInstanceValid(_produceGlowTween))
		{
			_produceGlowTween.Finished -= OnProduceGlowFinished;
			_produceGlowTween.Kill();
		}
		_produceGlowTween = null;
		if (restore && _produceGlowBaseCaptured && GodotObject.IsInstanceValid(produceGlowTarget))
		{
			produceGlowTarget.SetRenderSelfModulate(_produceGlowBaseSelfModulate);
		}
		_produceGlowBaseCaptured = false;
	}

	public void Create(Vector2 pos, int amount)
	{
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(parent) && amount > 0 && !IsRemoteSyncedClient())
		{
			ProduceAuthoritative(pos, amount, -1, emitEvent: false);
		}
	}

	private void ProduceAuthoritative(Vector2 pos, int amount, int markerIndex, bool emitEvent)
	{
		if (IsRemoteSyncedClient() || amount <= 0)
		{
			return;
		}
		string text = NormalizeProduceType(produceType);
		if (!onlyEmit)
		{
			switch (text)
			{
			case "Sun":
			{
				bool flag = isBaseIZM || (parent is TowerDefensePlant && parent.instance.hypnoses);
				CreateSunChunks(pos, amount, flag ? SunDropKind.BrainSun : SunDropKind.Sun, markerIndex, emitEvent);
				break;
			}
			case "BrainSun":
				CreateSunChunks(pos, amount, SunDropKind.BrainSun, markerIndex, emitEvent);
				break;
			case "JalaSun":
			{
				bool jalaZombie = parent is TowerDefensePlant && parent.instance.hypnoses;
				CreateSunChunks(pos, amount, SunDropKind.JalapenoSun, markerIndex, emitEvent, delegate(TowerDefenseSunBase sun)
				{
					if (sun is TowerDefenseSunJalapeno towerDefenseSunJalapeno)
					{
						towerDefenseSunJalapeno.zombieCamp = jalaZombie;
					}
				});
				break;
			}
			case "QXSun":
			{
				bool qxSurcharge = parent is TowerDefensePlant && parent.instance.hypnoses;
				CreateSunChunks(pos, amount, SunDropKind.QXSun, markerIndex, emitEvent, delegate(TowerDefenseSunBase sun)
				{
					if (sun is TowerDefenseSunQX towerDefenseSunQX)
					{
						towerDefenseSunQX.surcharge = qxSurcharge;
					}
				});
				break;
			}
			case "MagicSun":
			{
				bool magicZombieCamp = parent is TowerDefensePlant && parent.instance.hypnoses;
				CreateSunChunks(pos, amount, SunDropKind.MagicSun, markerIndex, emitEvent, delegate(TowerDefenseSunBase sun)
				{
					if (sun is TowerDefenseSunMagic towerDefenseSunMagic)
					{
						towerDefenseSunMagic.zombieCamp = magicZombieCamp;
					}
				});
				break;
			}
			case "Coin":
				CreateCoin(pos, amount, markerIndex, emitEvent);
				break;
			case "Packet":
				CreatePacket(pos, markerIndex, emitEvent);
				break;
			default:
				GD.PushWarning("ProduceComponent: 不支持的 produceType '" + text + "'，本次未产出任何掉落。");
				break;
			}
		}
		if (emitEvent)
		{
			QueueProduceOperation(text, amount, "", pos, Vector2.Zero, 980f, markerIndex, emitEvent: true, emitOnly: true, overrideHypnoses: false);
		}
		if (emitEvent)
		{
			this.OnProduct?.Invoke(markerIndex, amount);
		}
	}

	private bool CreateSunChunks(Vector2 pos, int amount, SunDropKind kind, int markerIndex, bool emitEvent, Action<TowerDefenseSunBase> onCreated = null)
	{
		if (parent.resourceSpawnComponent == null || parent.resourceSpawnComponent.IsReleased)
		{
			return false;
		}
		int effectiveSunChunk = GetEffectiveSunChunk();
		int num = amount;
		bool result = false;
		while (num > 0)
		{
			int num2 = Math.Min(num, effectiveSunChunk);
			TowerDefenseSunBase towerDefenseSunBase = null;
			switch (kind)
			{
			case SunDropKind.Sun:
				towerDefenseSunBase = parent.SunCreate(pos, num2);
				break;
			case SunDropKind.BrainSun:
				towerDefenseSunBase = parent.BrainSunCreate(pos, num2);
				break;
			case SunDropKind.JalapenoSun:
				towerDefenseSunBase = parent.JalapenoSunCreate(pos, num2);
				break;
			case SunDropKind.QXSun:
				towerDefenseSunBase = parent.QXSunCreate(pos, num2);
				break;
			case SunDropKind.MagicSun:
				towerDefenseSunBase = parent.MagicSunCreate(pos, num2);
				break;
			}
			num -= num2;
			if (GodotObject.IsInstanceValid(towerDefenseSunBase))
			{
				result = true;
				onCreated?.Invoke(towerDefenseSunBase);
				string kind2 = kind switch
				{
					SunDropKind.BrainSun => "BrainSun", 
					SunDropKind.JalapenoSun => "JalaSun", 
					SunDropKind.QXSun => "QXSun", 
					SunDropKind.MagicSun => "MagicSun", 
					_ => "Sun", 
				};
				Vector2 syncLastSunVelocity = parent.resourceSpawnComponent._syncLastSunVelocity;
				QueueProduceOperation(kind2, num2, "", pos, syncLastSunVelocity, 980f, markerIndex, emitEvent: false, emitOnly: false, overrideHypnoses: false);
			}
		}
		return result;
	}

	private bool CreateCoin(Vector2 pos, int amount, int markerIndex, bool emitEvent)
	{
		if (parent.resourceSpawnComponent == null || parent.resourceSpawnComponent.IsReleased)
		{
			return false;
		}
		int amount2 = amount;
		if (coinRandom)
		{
			amount2 = 10;
			if (GD.Randf() < 0.02f)
			{
				amount2 = 1000;
			}
			else if (GD.Randf() < 0.2f)
			{
				amount2 = 50;
			}
		}
		parent.CoinCreate(pos, amount2);
		Vector2 velocity = parent.resourceSpawnComponent?._syncLastCoinVelocity ?? Vector2.Zero;
		QueueProduceOperation("Coin", amount2, "", pos, velocity, 980f, markerIndex, emitEvent: false, emitOnly: false, overrideHypnoses: false);
		return true;
	}

	private bool CreatePacket(Vector2 pos, int markerIndex, bool emitEvent)
	{
		if (packetName.Count == 0)
		{
			return false;
		}
		string text = packetName.PickRandom();
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(text);
		if (!GodotObject.IsInstanceValid(packetConfig))
		{
			return false;
		}
		TowerDefensePacketConfig towerDefensePacketConfig = packetConfig.Duplicate(deep: true) as TowerDefensePacketConfig;
		if (!GodotObject.IsInstanceValid(towerDefensePacketConfig))
		{
			return false;
		}
		bool flag = GodotObject.IsInstanceValid(parent.instance) && parent.instance.hypnoses;
		if (flag)
		{
			towerDefensePacketConfig.overrideHypnoses = true;
		}
		TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = parent.SpawnPacket(towerDefensePacketConfig, pos, 15.0, isFall: false);
		if (!GodotObject.IsInstanceValid(towerDefenseInGamePacketShow))
		{
			return false;
		}
		Vector2 velocity = (GodotObject.IsInstanceValid(towerDefenseInGamePacketShow.moveComponent) ? towerDefenseInGamePacketShow.moveComponent.velocity : Vector2.Zero);
		QueueProduceOperation("Packet", 0, text, pos, velocity, 980f, markerIndex, emitEvent: false, emitOnly: false, flag);
		return true;
	}

	private Dictionary CreateOperation(string kind, int amount, string packetId, Vector2 position, Vector2 velocity, float gravity, int markerIndex, bool emitEvent, bool emitOnly, bool overrideHypnoses)
	{
		return new Dictionary
		{
			{
				"kind",
				kind ?? ""
			},
			{ "amount", amount },
			{
				"packet_id",
				packetId ?? ""
			},
			{ "position_x", position.X },
			{ "position_y", position.Y },
			{ "velocity_x", velocity.X },
			{ "velocity_y", velocity.Y },
			{ "gravity", gravity },
			{
				"economy_owner",
				GodotObject.IsInstanceValid(parent) ? parent.EconomyOwnerAccountId.ToString() : ""
			},
			{ "marker_index", markerIndex },
			{ "emit_event", emitEvent },
			{ "emit_only", emitOnly },
			{ "override_hypnoses", overrideHypnoses }
		};
	}

	private void QueueProduceOperation(string kind, int amount, string packetId, Vector2 position, Vector2 velocity, float gravity, int markerIndex, bool emitEvent, bool emitOnly, bool overrideHypnoses)
	{
		long sequence = ++_nextOperationSequence;
		if (CanSendProduceOperation(sequence))
		{
			SendNetworkOperation("produce", sequence, CreateOperation(kind, amount, packetId, position, velocity, gravity, markerIndex, emitEvent, emitOnly, overrideHypnoses));
		}
	}

	private bool CanSendProduceOperation(long sequence)
	{
		if (!Global.IsMultiplayerMode || !MultiPlayerManager.IsHost || base.Owner == null || base.Owner.syncId < 0 || base.ComponentDefinition == null || string.IsNullOrWhiteSpace(base.ComponentDefinition.InstanceId) || string.IsNullOrWhiteSpace(base.ComponentDefinition.ComponentTypeId) || sequence < 0)
		{
			return false;
		}
		return GodotObject.IsInstanceValid(MultiPlayerManager.Instance);
	}

	public override void ApplyNetworkOperation(string operationName, long sequence, Dictionary operation)
	{
		if (!string.Equals(operationName, "produce", StringComparison.Ordinal) || operation == null || !IsRemoteSyncedClient() || !GodotObject.IsInstanceValid(parent) || sequence <= _lastAppliedOperationSequence)
		{
			return;
		}
		string text = NormalizeProduceType(operation.GetValueOrDefault("kind", "").AsString());
		if (!IsSupportedProduceType(text))
		{
			return;
		}
		int num = operation.GetValueOrDefault("amount", 0).AsInt32();
		int num2 = operation.GetValueOrDefault("marker_index", -1).AsInt32();
		bool flag = operation.GetValueOrDefault("emit_event", false).AsBool();
		bool flag2 = operation.GetValueOrDefault("emit_only", false).AsBool();
		string value = operation.GetValueOrDefault("packet_id", "").AsString();
		if (num2 < -1 || (flag2 && (!flag || num <= 0)) || (!flag2 && text != "Packet" && num <= 0) || (!flag2 && text == "Packet" && (num != 0 || string.IsNullOrWhiteSpace(value))))
		{
			return;
		}
		float num3 = operation.GetValueOrDefault("position_x", 0f).AsSingle();
		float num4 = operation.GetValueOrDefault("position_y", 0f).AsSingle();
		float num5 = operation.GetValueOrDefault("velocity_x", 0f).AsSingle();
		float num6 = operation.GetValueOrDefault("velocity_y", 0f).AsSingle();
		float num7 = operation.GetValueOrDefault("gravity", 980f).AsSingle();
		if (!float.IsFinite(num3) || !float.IsFinite(num4) || !float.IsFinite(num5) || !float.IsFinite(num6) || !float.IsFinite(num7))
		{
			return;
		}
		string value2 = operation.GetValueOrDefault("economy_owner", "").AsString();
		EconomyAccountId accountId;
		long balance;
		if (string.IsNullOrEmpty(value2))
		{
			if (parent.HasEconomyOwner)
			{
				return;
			}
		}
		else if (!EconomyAccountId.TryParse(value2, out accountId) || !GodotObject.IsInstanceValid(TowerDefenseManager.Instance) || !TowerDefenseManager.Instance.TryGetSun(accountId, out balance) || !parent.TryAssignEconomyOwner(accountId))
		{
			return;
		}
		bool flag3 = flag2;
		if (!flag2)
		{
			Vector2 pos = new Vector2(num3, num4);
			Vector2 velocity = new Vector2(num5, num6);
			switch (text)
			{
			case "Sun":
				ClearLegacySpawnVelocityReplay();
				flag3 = GodotObject.IsInstanceValid(parent.SunCreate(pos, num, TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, velocity, num7));
				break;
			case "BrainSun":
				ClearLegacySpawnVelocityReplay();
				flag3 = GodotObject.IsInstanceValid(parent.BrainSunCreate(pos, num, TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, velocity, num7));
				break;
			case "JalaSun":
				ClearLegacySpawnVelocityReplay();
				flag3 = GodotObject.IsInstanceValid(parent.JalapenoSunCreate(pos, num, TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, velocity, num7));
				break;
			case "QXSun":
				ClearLegacySpawnVelocityReplay();
				flag3 = GodotObject.IsInstanceValid(parent.QXSunCreate(pos, num, TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, velocity, num7));
				break;
			case "MagicSun":
				ClearLegacySpawnVelocityReplay();
				flag3 = GodotObject.IsInstanceValid(parent.MagicSunCreate(pos, num, TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, velocity, num7));
				break;
			case "Coin":
			{
				ResourceSpawnComponent resourceSpawnComponent = parent.resourceSpawnComponent;
				if (resourceSpawnComponent == null || resourceSpawnComponent.IsReleased || !GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
				{
					return;
				}
				ClearLegacySpawnVelocityReplay();
				parent.CoinCreate(pos, num, velocity, num7);
				flag3 = true;
				break;
			}
			case "Packet":
				flag3 = true;
				break;
			}
		}
		if (flag3)
		{
			_lastAppliedOperationSequence = sequence;
			if (flag)
			{
				this.OnProduct?.Invoke(num2, num);
			}
		}
	}

	private void ClearLegacySpawnVelocityReplay()
	{
		ResourceSpawnComponent resourceSpawnComponent = parent.resourceSpawnComponent;
		if (resourceSpawnComponent != null && !resourceSpawnComponent.IsReleased)
		{
			parent.resourceSpawnComponent._syncDeserializing = false;
		}
	}

	protected override void OnOwnerBeforeDestroy()
	{
		_ownerDestroyPreflightObserved = true;
		_ownerDeathConfirmedAtPreflight = IsConfirmedDeathBeforeDestroy(parent);
		FlushAuthoritativePreDestroyProduction(parent, _ownerDeathConfirmedAtPreflight);
	}

	public void Destroy(TowerDefenseCharacter character)
	{
		bool confirmedDeath = (_ownerDestroyPreflightObserved ? _ownerDeathConfirmedAtPreflight : IsZeroHealthDeath(character));
		_ownerDestroyPreflightObserved = false;
		_ownerDeathConfirmedAtPreflight = false;
		FlushAuthoritativePreDestroyProduction(character, confirmedDeath);
	}

	private void FlushAuthoritativePreDestroyProduction(TowerDefenseCharacter character, bool confirmedDeath)
	{
		if (!confirmedDeath || _deathProductionFlushed || base.Lifecycle != ComponentRuntimeLifecycle.Active || IsRemoteSyncedClient() || !GodotObject.IsInstanceValid(character) || !GodotObject.IsInstanceValid(parent) || character != parent || !GodotObject.IsInstanceValid(parent.instance) || character.isShovel || parent.instance.sleep || !parent.componentAlive)
		{
			return;
		}
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		bool flag = _IZMMode || (GodotObject.IsInstanceValid(instance) && (instance.IsIZMMode() || instance.IsIZM2Mode()));
		if (parent is TowerDefensePlant && flag && !(hpNextInterval <= 0f))
		{
			_deathProductionFlushed = true;
			int num = 0;
			int effectiveHealthSegments = GetEffectiveHealthSegments();
			while (hpNext >= 0f && hpNextInterval > 0f && num < effectiveHealthSegments)
			{
				hpNext -= hpNextInterval;
				ProduceAtConfiguredPositions(this.num, emitEvent: false, 18446744073709551615uL);
				num++;
			}
		}
	}

	public void ImmediateProduct()
	{
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && !IsRemoteSyncedClient())
		{
			timer = GetEffectiveProduceInterval();
		}
	}

	public override Dictionary ExportComponentSave()
	{
		return new Dictionary
		{
			{ "timer", timer },
			{ "hpNext", hpNext },
			{ "hpNextInterval", hpNextInterval },
			{ "produceType", produceType },
			{ "num", num }
		};
	}

	public override void ImportComponentSave(Dictionary data, TowerDefenseLevelSaveConfigCSharp owner)
	{
		timer = data.GetValueOrDefault("timer", timer).AsSingle();
		hpNext = data.GetValueOrDefault("hpNext", hpNext).AsSingle();
		hpNextInterval = data.GetValueOrDefault("hpNextInterval", hpNextInterval).AsSingle();
		produceType = data.GetValueOrDefault("produceType", produceType).AsString();
		num = data.GetValueOrDefault("num", num).AsInt32();
		_runtimeStateImported = true;
	}

	public override Dictionary SyncSerialize()
	{
		return new Dictionary
		{
			{ "timer", timer },
			{ "hpNext", hpNext },
			{ "hpNextInterval", hpNextInterval },
			{ "produceType", produceType },
			{ "num", num }
		};
	}

	public override void SyncDeserialize(Dictionary data)
	{
		timer = data.GetValueOrDefault("timer", timer).AsSingle();
		hpNext = data.GetValueOrDefault("hpNext", hpNext).AsSingle();
		hpNextInterval = data.GetValueOrDefault("hpNextInterval", hpNextInterval).AsSingle();
		produceType = data.GetValueOrDefault("produceType", produceType).AsString();
		num = data.GetValueOrDefault("num", num).AsInt32();
		_runtimeStateImported = true;
	}

	private bool IsRemoteSyncedClient()
	{
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost && GodotObject.IsInstanceValid(parent))
		{
			return parent.syncId >= 0;
		}
		return false;
	}

	private int GetEffectiveSunChunk()
	{
		return Math.Max(1, sunOnceMax);
	}

	private float GetEffectiveProduceInterval()
	{
		return _effectiveProduceInterval;
	}

	private int GetEffectiveHealthSegments()
	{
		return Math.Clamp(healthProductionSegments, 1, 1000);
	}

	private static bool IsConfirmedDeathBeforeDestroy(TowerDefenseCharacter character)
	{
		if (GodotObject.IsInstanceValid(character) && GodotObject.IsInstanceValid(character.instance))
		{
			if (!character.die && !character.instance.die)
			{
				return character.instance.hitpoints <= 0.0;
			}
			return true;
		}
		return false;
	}

	private static bool IsZeroHealthDeath(TowerDefenseCharacter character)
	{
		if (GodotObject.IsInstanceValid(character) && GodotObject.IsInstanceValid(character.instance))
		{
			return character.instance.hitpoints <= 0.0;
		}
		return false;
	}

	private static bool IsSupportedProduceType(string value)
	{
		switch (value)
		{
		default:
			return value == "MagicSun";
		case "Sun":
		case "BrainSun":
		case "JalaSun":
		case "Coin":
		case "Packet":
		case "QXSun":
			return true;
		}
	}

	private static string NormalizeProduceType(string value)
	{
		if (string.Equals(value, "Sun", StringComparison.OrdinalIgnoreCase))
		{
			return "Sun";
		}
		if (string.Equals(value, "BrainSun", StringComparison.OrdinalIgnoreCase))
		{
			return "BrainSun";
		}
		if (string.Equals(value, "JalaSun", StringComparison.OrdinalIgnoreCase))
		{
			return "JalaSun";
		}
		if (string.Equals(value, "QXSun", StringComparison.OrdinalIgnoreCase))
		{
			return "QXSun";
		}
		if (string.Equals(value, "MagicSun", StringComparison.OrdinalIgnoreCase))
		{
			return "MagicSun";
		}
		if (string.Equals(value, "Coin", StringComparison.OrdinalIgnoreCase))
		{
			return "Coin";
		}
		if (string.Equals(value, "Packet", StringComparison.OrdinalIgnoreCase))
		{
			return "Packet";
		}
		return value ?? "";
	}
}
