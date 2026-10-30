using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public sealed class BuffComponent : CharacterComponentRuntime
{
	public delegate void BuffAddEventHandler(string key);

	public delegate void BuffDeleteEventHandler(string key);

	private enum BuffRemovalReason
	{
		Removed,
		Expired,
		ReadOnlyReplica
	}

	private sealed class CachedBuffVisual
	{
		public Sprite2D Sprite;

		public AdobeAnimateExternalVisualDescriptor Descriptor;

		public AdobeAnimateExternalVisualHandle Handle = AdobeAnimateExternalVisualHandle.Invalid;

		public bool Visible;
	}

	public TowerDefenseCharacter parent;

	private System.Collections.Generic.Dictionary<string, TowerDefenseCharacterBuffConfig> _buffDictionary;

	private List<string> _updateKeysSnapshotBuffer;

	private List<string> _keysSnapshotBuffer;

	private List<TowerDefenseCharacterBuffConfig> _incomingDamageModifiers;

	private List<KeyValuePair<string, TowerDefenseCharacterBuffConfig>> _expiredBuffBuffer;

	private HashSet<string> _syncKeysBuffer;

	private HashSet<string> _restoreKeysBuffer;

	private TowerDefenseCharacterInstance _connectedInstance;

	private TowerDefenseCharacterBuffConfig _enteringBuff;

	private bool _waitingForParentReady;

	private System.Collections.Generic.Dictionary<string, CachedBuffVisual> _cachedVisuals;

	private List<string> _invalidVisualKeys;

	public bool over;

	public bool is_syncing;

	private static readonly string[] BUFF_COMMON_SAVE_FIELDS = new string[2] { "refresh", "canFliter" };

	public static readonly System.Collections.Generic.Dictionary<string, string[]> BUFF_SAVE_FIELDS = new System.Collections.Generic.Dictionary<string, string[]>(StringComparer.Ordinal)
	{
		{
			"RuneStormSlow",
			new string[3] { "timeScaleValue", "time", "currentTime" }
		},
		{
			"RuneFogHaste",
			new string[3] { "timeScaleValue", "time", "currentTime" }
		},
		{
			"RuneFogDizzyImmune",
			new string[3] { "timeScaleValue", "time", "currentTime" }
		},
		{
			"AttackSpeedDown",
			new string[3] { "timeScaleValue", "time", "currentTime" }
		},
		{
			"Frozen",
			new string[3] { "time", "iceSpeedDownTime", "currentTime" }
		},
		{
			"Burn",
			new string[7] { "time", "dpsAttack", "splatSceneType", "splatScene", "splatInterval", "currentTime", "splatTime" }
		},
		{
			"Hypnoses",
			new string[15]
			{
				"time", "currentTime", "saveCamp", "enableTorchwood", "torchwoodChangeName", "torchwoodAudio", "torchwoodAreaSize", "enableDeathFreeze", "deathFreezeTime", "deathSlowTime",
				"deathDamage", "deathFreezeOver", "enableSunProduce", "sunProduceInterval", "sunProduceNum"
			}
		},
		{
			"IceSpeedDown",
			new string[2] { "time", "currentTime" }
		},
		{
			"EMSpeedDown",
			new string[2] { "time", "currentTime" }
		},
		{
			"EMP",
			new string[2] { "time", "currentTime" }
		},
		{
			"Dizziness",
			new string[2] { "time", "currentTime" }
		},
		{
			"MagicImmobilize",
			new string[2] { "time", "currentTime" }
		},
		{
			"MagicRootHaste",
			new string[3] { "timeScaleValue", "time", "currentTime" }
		},
		{
			"SealMagic",
			new string[2] { "time", "currentTime" }
		},
		{
			"Butter",
			new string[2] { "time", "currentTime" }
		},
		{
			"ButterGene",
			new string[2] { "time", "currentTime" }
		},
		{
			"Cherry",
			new string[2] { "time", "currentTime" }
		},
		{
			"Pogo",
			new string[4] { "time", "jumpSpeed", "pogoGravity", "currentTime" }
		},
		{
			"Sleep",
			new string[2] { "time", "currentTime" }
		},
		{
			"FireHit",
			System.Array.Empty<string>()
		},
		{
			"JalaHit",
			System.Array.Empty<string>()
		},
		{
			"RedHeat",
			new string[2] { "time", "currentTime" }
		},
		{
			"Poisoning",
			new string[3] { "time", "currentTime", "timer" }
		},
		{
			"Fluorescence",
			new string[2] { "time", "currentTime" }
		},
		{
			"Radiance",
			new string[7] { "permanent", "time", "currentTime", "flashOnDeath", "radianceRemoveOnCampFlip", "deathFlashRequireSameCamp", "baselineCamp" }
		},
		{
			"NormalHit",
			System.Array.Empty<string>()
		},
		{
			"Squid",
			new string[2] { "time", "currentTime" }
		},
		{
			"TimeMagic",
			new string[3] { "timeScaleValue", "time", "currentTime" }
		},
		{
			"TabooBean",
			new string[3] { "time", "currentTime", "blink" }
		},
		{
			"Coffee",
			new string[4] { "timeScaleValue", "time", "currentTime", "blink" }
		}
	};

	private const string NETWORK_VALUE_TYPE_KEY = "__type";

	private const string NETWORK_VALUE_RESOURCE = "resource";

	private const string NETWORK_VALUE_VECTOR2 = "vector2";

	private static readonly string[] PLAYBACK_BLOCKING_HARD_CONTROL_KEYS = new string[7] { "Frozen", "Butter", "ButterGene", "EMP", "Dizziness", "MagicImmobilize", "SealMagic" };

	private BuffComponentDefinition Definition => base.ComponentDefinition as BuffComponentDefinition;

	public System.Collections.Generic.Dictionary<string, TowerDefenseCharacterBuffConfig> buffDictionary => _buffDictionary ?? (_buffDictionary = new System.Collections.Generic.Dictionary<string, TowerDefenseCharacterBuffConfig>());

	public bool HasActiveBuffs
	{
		get
		{
			System.Collections.Generic.Dictionary<string, TowerDefenseCharacterBuffConfig> dictionary = _buffDictionary;
			if (dictionary == null)
			{
				return false;
			}
			return dictionary.Count > 0;
		}
	}

	public bool HasFrameUpdateWork
	{
		get
		{
			if (!base.IsReleased && base.Alive)
			{
				System.Collections.Generic.Dictionary<string, TowerDefenseCharacterBuffConfig> dictionary = _buffDictionary;
				if (dictionary == null)
				{
					return false;
				}
				return dictionary.Count > 0;
			}
			return false;
		}
	}

	public bool HasIncomingDamageModifiers
	{
		get
		{
			List<TowerDefenseCharacterBuffConfig> incomingDamageModifiers = _incomingDamageModifiers;
			if (incomingDamageModifiers == null)
			{
				return false;
			}
			return incomingDamageModifiers.Count > 0;
		}
	}

	public event BuffAddEventHandler OnBuffAdd;

	public event BuffDeleteEventHandler OnBuffDelete;

	protected override void OnBound()
	{
		parent = base.Owner;
	}

	protected override void OnActivated()
	{
		if (GodotObject.IsInstanceValid(parent))
		{
			if (!parent.IsNodeReady())
			{
				parent.Ready += OnParentReady;
				_waitingForParentReady = true;
			}
			else
			{
				ConnectSignals();
			}
			AttachCachedBuffVisuals();
		}
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		DisconnectSignals();
		DetachCachedBuffVisuals();
		parent = null;
	}

	protected override void OnReleased()
	{
		if ((_buffDictionary?.Count ?? 0) > 0)
		{
			List<string> list = _keysSnapshotBuffer ?? (_keysSnapshotBuffer = new List<string>());
			SnapshotKeys(list);
			for (int i = 0; i < list.Count; i++)
			{
				if (!_buffDictionary.TryGetValue(list[i], out var value) || value == null)
				{
					continue;
				}
				try
				{
					value.Cancel();
				}
				catch (Exception value2)
				{
					GD.PushError($"Failed to cancel Buff '{list[i]}' during final release: {value2}");
				}
				finally
				{
					value.character = null;
				}
			}
		}
		ReleaseCachedBuffVisuals();
		_buffDictionary?.Clear();
		_updateKeysSnapshotBuffer?.Clear();
		_keysSnapshotBuffer?.Clear();
		_incomingDamageModifiers?.Clear();
		_expiredBuffBuffer?.Clear();
		_syncKeysBuffer?.Clear();
		_restoreKeysBuffer?.Clear();
		_invalidVisualKeys?.Clear();
		_enteringBuff = null;
		this.OnBuffAdd = null;
		this.OnBuffDelete = null;
		parent = null;
	}

	private void OnParentReady()
	{
		if (_waitingForParentReady && GodotObject.IsInstanceValid(parent))
		{
			parent.Ready -= OnParentReady;
		}
		_waitingForParentReady = false;
		ConnectSignals();
	}

	private void ConnectSignals()
	{
		if (GodotObject.IsInstanceValid(parent) && GodotObject.IsInstanceValid(parent.instance) && _connectedInstance != parent.instance)
		{
			DisconnectInstanceSignals();
			_connectedInstance = parent.instance;
			_connectedInstance.hitpointsNearDie += Destroy;
			_connectedInstance.hitpointsEmpty += Destroy;
		}
	}

	private void DisconnectSignals()
	{
		if (_waitingForParentReady && GodotObject.IsInstanceValid(parent))
		{
			parent.Ready -= OnParentReady;
		}
		_waitingForParentReady = false;
		DisconnectInstanceSignals();
	}

	private void DisconnectInstanceSignals()
	{
		if (GodotObject.IsInstanceValid(_connectedInstance))
		{
			_connectedInstance.hitpointsNearDie -= Destroy;
			_connectedInstance.hitpointsEmpty -= Destroy;
		}
		_connectedInstance = null;
	}

	public void BuffUpdate(double delta)
	{
		if (base.Alive)
		{
			System.Collections.Generic.Dictionary<string, TowerDefenseCharacterBuffConfig> dictionary = _buffDictionary;
			if (dictionary != null && dictionary.Count != 0)
			{
				List<string> list = _updateKeysSnapshotBuffer ?? (_updateKeysSnapshotBuffer = new List<string>());
				SnapshotKeys(list);
				if (IsReadOnlyClient())
				{
					for (int i = 0; i < list.Count; i++)
					{
						if (_buffDictionary.TryGetValue(list[i], out var value))
						{
							value.StepReadOnlyClient(delta);
						}
					}
					return;
				}
				List<KeyValuePair<string, TowerDefenseCharacterBuffConfig>> list2 = _expiredBuffBuffer ?? (_expiredBuffBuffer = new List<KeyValuePair<string, TowerDefenseCharacterBuffConfig>>());
				list2.Clear();
				for (int j = 0; j < list.Count; j++)
				{
					string key = list[j];
					if (_buffDictionary.TryGetValue(key, out var value2) && value2.Step(delta))
					{
						list2.Add(new KeyValuePair<string, TowerDefenseCharacterBuffConfig>(key, value2));
					}
				}
				for (int k = 0; k < list2.Count; k++)
				{
					KeyValuePair<string, TowerDefenseCharacterBuffConfig> keyValuePair = list2[k];
					RemoveBuffCore(keyValuePair.Key, keyValuePair.Value, notify: true, BuffRemovalReason.Expired);
				}
				return;
			}
		}
		ApplyFrameMeshColor(Colors.White);
	}

	private void RebuildFrameMeshColor()
	{
		if (!GodotObject.IsInstanceValid(parent?.sprite))
		{
			return;
		}
		Color white = Colors.White;
		if (_buffDictionary != null)
		{
			foreach (TowerDefenseCharacterBuffConfig value in _buffDictionary.Values)
			{
				if (value != null)
				{
					white *= value.FrameMeshColorMultiplier;
				}
			}
		}
		ApplyFrameMeshColor(white);
	}

	private void ApplyFrameMeshColor(Color color)
	{
		AdobeAnimateSprite adobeAnimateSprite = parent?.sprite;
		if (GodotObject.IsInstanceValid(adobeAnimateSprite) && adobeAnimateSprite.meshColor != color)
		{
			adobeAnimateSprite.meshColor = color;
		}
	}

	public bool BuffHas(string key)
	{
		if (!string.IsNullOrEmpty(key))
		{
			return _buffDictionary?.ContainsKey(key) ?? false;
		}
		return false;
	}

	public TowerDefenseCharacterBuffConfig BuffGet(string key)
	{
		if (string.IsNullOrEmpty(key) || _buffDictionary == null || !_buffDictionary.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public double GetAttackSpeedMultiplier()
	{
		double num = ((BuffGet("AttackSpeedDown") is TowerDefenseCharacterBuffAttackSpeedDown towerDefenseCharacterBuffAttackSpeedDown) ? towerDefenseCharacterBuffAttackSpeedDown.GetAttackSpeedMultiplier() : 1.0);
		if (num > 0.0 && parent is TowerDefensePlant && TowerDefenseProcessModeDispatch.IsIZMModeForCurrentPhysicsFrame && BuffGet("TimeMagic") is TowerDefenseCharacterBuffTimeMagic towerDefenseCharacterBuffTimeMagic)
		{
			num *= Mathf.Max(0.0, towerDefenseCharacterBuffTimeMagic.timeScaleValue);
		}
		return num;
	}

	public void AddBuff(TowerDefenseCharacterBuffConfig buffConfig)
	{
		if (buffConfig == null || (IsReadOnlyClient() && !is_syncing) || (GodotObject.IsInstanceValid(parent?.instance) && parent.instance.hologram && !(buffConfig is TowerDefenseCharacterBuffHypnoses)))
		{
			return;
		}
		if (string.IsNullOrEmpty(buffConfig.key))
		{
			buffConfig._Init();
		}
		if (string.IsNullOrEmpty(buffConfig.key) || TryConsumeIncomingBuff(buffConfig, notifyExistingRemoval: true))
		{
			return;
		}
		if (buffConfig.refresh && buffDictionary.TryGetValue(buffConfig.key, out var value))
		{
			value.Refresh(buffConfig);
			RebuildFrameMeshColor();
			this.OnBuffAdd?.Invoke(buffConfig.key);
			return;
		}
		if (buffDictionary.TryGetValue(buffConfig.key, out value))
		{
			RemoveBuffCore(buffConfig.key, value, notify: true);
		}
		EnterBuff(buffConfig, notify: true);
	}

	public void ApplyFireHit(TowerDefenseCharacterBuffFireHit source = null)
	{
		if (IsReadOnlyClient() && !is_syncing)
		{
			return;
		}
		if ((source == null || source.refresh) && buffDictionary.TryGetValue("FireHit", out var value) && value is TowerDefenseCharacterBuffFireHit towerDefenseCharacterBuffFireHit)
		{
			TowerDefenseCharacterBuffFireHit towerDefenseCharacterBuffFireHit2 = source ?? towerDefenseCharacterBuffFireHit;
			if (!TryConsumeIncomingBuff(towerDefenseCharacterBuffFireHit2, notifyExistingRemoval: true))
			{
				value.Refresh(towerDefenseCharacterBuffFireHit2);
				this.OnBuffAdd?.Invoke("FireHit");
			}
		}
		else
		{
			TowerDefenseCharacterBuffFireHit towerDefenseCharacterBuffFireHit3 = ((source == null) ? new TowerDefenseCharacterBuffFireHit() : (source.CreateRuntimeInstance() as TowerDefenseCharacterBuffFireHit));
			if (towerDefenseCharacterBuffFireHit3 != null)
			{
				AddBuff(towerDefenseCharacterBuffFireHit3);
			}
		}
	}

	public void DeleteBuff(string key)
	{
		if (!string.IsNullOrEmpty(key) && (!IsReadOnlyClient() || is_syncing))
		{
			RemoveBuffCore(key, null, notify: true);
		}
	}

	public void BuffClear()
	{
		if (IsReadOnlyClient() && !is_syncing)
		{
			return;
		}
		System.Collections.Generic.Dictionary<string, TowerDefenseCharacterBuffConfig> dictionary = _buffDictionary;
		if (dictionary != null && dictionary.Count != 0)
		{
			List<string> list = _keysSnapshotBuffer ?? (_keysSnapshotBuffer = new List<string>());
			SnapshotKeys(list);
			for (int i = 0; i < list.Count; i++)
			{
				RemoveBuffCore(list[i], null, notify: true);
			}
		}
	}

	public void Destroy()
	{
		if (over)
		{
			return;
		}
		TowerDefensePerfProfiler.SpikeProbe probe = TowerDefensePerfProfiler.BeginSpikeProbe();
		over = true;
		if (RemoveDeathHardControlBuff("Frozen") | RemoveDeathHardControlBuff("Butter"))
		{
			ResumeDeathAnimationAfterHardControlRemoval();
		}
		if (IsReadOnlyClient())
		{
			TowerDefensePerfProfiler.End("damage.terminal.buffDestroy", probe.StartTicks);
			return;
		}
		int items = 0;
		if ((_buffDictionary?.Count ?? 0) > 0)
		{
			List<string> list = _keysSnapshotBuffer ?? (_keysSnapshotBuffer = new List<string>());
			SnapshotKeys(list);
			items = list.Count;
			for (int i = 0; i < list.Count; i++)
			{
				if (_buffDictionary.TryGetValue(list[i], out var value))
				{
					value.Destroy();
				}
			}
		}
		TowerDefensePerfProfiler.End("damage.terminal.buffDestroy", probe.StartTicks, items);
		TowerDefensePerfProfiler.EndSpikeProbe("damage.terminal.buffDestroy", in probe, items, 0.5);
	}

	private bool RemoveDeathHardControlBuff(string key)
	{
		BuffRemovalReason reason = (IsReadOnlyClient() ? BuffRemovalReason.ReadOnlyReplica : BuffRemovalReason.Removed);
		return RemoveBuffCore(key, null, notify: true, reason);
	}

	public bool HasPlaybackBlockingHardControl()
	{
		for (int i = 0; i < PLAYBACK_BLOCKING_HARD_CONTROL_KEYS.Length; i++)
		{
			if (BuffHas(PLAYBACK_BLOCKING_HARD_CONTROL_KEYS[i]))
			{
				return true;
			}
		}
		return false;
	}

	public bool ResumeAnimationIfNoPlaybackBlockingHardControl()
	{
		if (!GodotObject.IsInstanceValid(parent?.sprite) || HasPlaybackBlockingHardControl())
		{
			return false;
		}
		double timeScale = ((Math.Abs(parent.timeScale) <= 1E-06) ? parent.timeScaleInit : parent.timeScale);
		parent.sprite.timeScale = timeScale;
		parent.sprite.SetPlaybackBlocked(blocked: false);
		return true;
	}

	private void ResumeDeathAnimationAfterHardControlRemoval()
	{
		ResumeAnimationIfNoPlaybackBlockingHardControl();
	}

	public bool TryGetVisualDefinition(string buffKey, out BuffVisualDefinition visualDefinition)
	{
		visualDefinition = null;
		if (string.IsNullOrEmpty(buffKey) || Definition?.visualDefinitions == null)
		{
			return false;
		}
		Array<BuffVisualDefinition> visualDefinitions = Definition.visualDefinitions;
		for (int i = 0; i < visualDefinitions.Count; i++)
		{
			BuffVisualDefinition buffVisualDefinition = visualDefinitions[i];
			if (buffVisualDefinition != null && string.Equals(buffVisualDefinition.buffKey.ToString(), buffKey, StringComparison.Ordinal))
			{
				visualDefinition = buffVisualDefinition;
				return true;
			}
		}
		return false;
	}

	public Sprite2D GetCachedBuffVisual(string buffKey)
	{
		if (string.IsNullOrEmpty(buffKey) || _cachedVisuals == null || !_cachedVisuals.TryGetValue(buffKey, out var value) || !GodotObject.IsInstanceValid(value.Sprite))
		{
			return null;
		}
		return value.Sprite;
	}

	public bool RegisterBuffVisual(string buffKey, Sprite2D sprite, AdobeAnimateExternalVisualDescriptor descriptor)
	{
		if (string.IsNullOrEmpty(buffKey) || !GodotObject.IsInstanceValid(sprite) || !GodotObject.IsInstanceValid(parent))
		{
			return false;
		}
		System.Collections.Generic.Dictionary<string, CachedBuffVisual> dictionary = _cachedVisuals ?? (_cachedVisuals = new System.Collections.Generic.Dictionary<string, CachedBuffVisual>(StringComparer.Ordinal));
		if (dictionary.TryGetValue(buffKey, out var value))
		{
			if (value.Sprite != sprite)
			{
				DetachCachedBuffVisual(value);
				if (GodotObject.IsInstanceValid(value.Sprite))
				{
					value.Sprite.QueueFree();
				}
				value.Sprite = sprite;
				value.Visible = sprite.Visible;
			}
			value.Descriptor = descriptor;
		}
		else
		{
			value = new CachedBuffVisual
			{
				Sprite = sprite,
				Descriptor = descriptor,
				Visible = sprite.Visible
			};
			dictionary.Add(buffKey, value);
		}
		AttachCachedBuffVisual(value);
		return value.Handle.IsValid;
	}

	public bool SetBuffVisualVisible(string buffKey, bool visible)
	{
		if (string.IsNullOrEmpty(buffKey) || _cachedVisuals == null || !_cachedVisuals.TryGetValue(buffKey, out var value) || !GodotObject.IsInstanceValid(value.Sprite))
		{
			return false;
		}
		value.Visible = visible;
		value.Sprite.Visible = visible;
		if (value.Handle.IsValid && GodotObject.IsInstanceValid(parent?.sprite))
		{
			parent.sprite.SetExternalVisualVisible(value.Handle, visible);
		}
		return true;
	}

	private void AttachCachedBuffVisuals()
	{
		System.Collections.Generic.Dictionary<string, CachedBuffVisual> cachedVisuals = _cachedVisuals;
		if (cachedVisuals == null || cachedVisuals.Count == 0)
		{
			return;
		}
		List<string> list = _invalidVisualKeys ?? (_invalidVisualKeys = new List<string>());
		list.Clear();
		foreach (KeyValuePair<string, CachedBuffVisual> cachedVisual in _cachedVisuals)
		{
			CachedBuffVisual value = cachedVisual.Value;
			if (!GodotObject.IsInstanceValid(value.Sprite))
			{
				list.Add(cachedVisual.Key);
			}
			else
			{
				AttachCachedBuffVisual(value);
			}
		}
		for (int i = 0; i < list.Count; i++)
		{
			_cachedVisuals.Remove(list[i]);
		}
		list.Clear();
	}

	private void AttachCachedBuffVisual(CachedBuffVisual cached)
	{
		if (GodotObject.IsInstanceValid(cached?.Sprite) && GodotObject.IsInstanceValid(parent?.sprite))
		{
			if (!cached.Handle.IsValid)
			{
				cached.Handle = parent.RegisterCharacterExternalVisual(cached.Sprite, cached.Descriptor);
			}
			cached.Sprite.Visible = cached.Visible;
			if (cached.Handle.IsValid)
			{
				parent.sprite.SetExternalVisualVisible(cached.Handle, cached.Visible);
			}
		}
	}

	private void DetachCachedBuffVisuals()
	{
		if (_cachedVisuals == null)
		{
			return;
		}
		foreach (CachedBuffVisual value in _cachedVisuals.Values)
		{
			DetachCachedBuffVisual(value);
		}
	}

	private void DetachCachedBuffVisual(CachedBuffVisual cached)
	{
		if (cached == null)
		{
			return;
		}
		if (cached.Handle.IsValid && GodotObject.IsInstanceValid(parent))
		{
			if (GodotObject.IsInstanceValid(parent.sprite))
			{
				parent.sprite.SetExternalVisualVisible(cached.Handle, visible: false);
			}
			parent.UnregisterCharacterExternalVisual(cached.Handle);
		}
		cached.Handle = AdobeAnimateExternalVisualHandle.Invalid;
		if (GodotObject.IsInstanceValid(cached.Sprite))
		{
			cached.Sprite.Visible = false;
		}
	}

	private void ReleaseCachedBuffVisuals()
	{
		if (_cachedVisuals == null)
		{
			return;
		}
		DetachCachedBuffVisuals();
		foreach (CachedBuffVisual value in _cachedVisuals.Values)
		{
			if (GodotObject.IsInstanceValid(value.Sprite))
			{
				value.Sprite.QueueFree();
			}
		}
		_cachedVisuals.Clear();
	}

	internal bool TryGetBuffExternalVisual(string buffKey, out AdobeAnimateExternalVisualSnapshot visual)
	{
		if (!string.IsNullOrEmpty(buffKey) && _cachedVisuals != null && _cachedVisuals.TryGetValue(buffKey, out var value) && value.Handle.IsValid && GodotObject.IsInstanceValid(parent?.sprite))
		{
			return parent.sprite.TryGetExternalVisualForRender(value.Handle, out visual);
		}
		visual = default(AdobeAnimateExternalVisualSnapshot);
		return false;
	}

	public double SetAttackNum(double num)
	{
		List<TowerDefenseCharacterBuffConfig> incomingDamageModifiers = _incomingDamageModifiers;
		if (incomingDamageModifiers == null || incomingDamageModifiers.Count == 0)
		{
			return num;
		}
		List<TowerDefenseCharacterBuffConfig> incomingDamageModifiers2 = _incomingDamageModifiers;
		long startTicks = TowerDefensePerfProfiler.BeginHotPath();
		if (IsReadOnlyClient())
		{
			TowerDefensePerfProfiler.End("damage.buffModifier", startTicks);
			return num;
		}
		int num2 = 0;
		int num3 = incomingDamageModifiers2.Count;
		int num4 = 0;
		while (num4 < incomingDamageModifiers2.Count && num3 > 0)
		{
			TowerDefenseCharacterBuffConfig towerDefenseCharacterBuffConfig = incomingDamageModifiers2[num4];
			num3--;
			if (!GodotObject.IsInstanceValid(towerDefenseCharacterBuffConfig))
			{
				incomingDamageModifiers2.RemoveAt(num4);
				continue;
			}
			num2++;
			num = towerDefenseCharacterBuffConfig.SetAttackNum(num);
			if (num4 < incomingDamageModifiers2.Count && incomingDamageModifiers2[num4] == towerDefenseCharacterBuffConfig)
			{
				num4++;
			}
		}
		TowerDefensePerfProfiler.End("damage.buffModifier", startTicks, num2);
		return num;
	}

	public Array<Dictionary> ExportSave()
	{
		Array<Dictionary> array = new Array<Dictionary>();
		foreach (KeyValuePair<string, TowerDefenseCharacterBuffConfig> item in buffDictionary)
		{
			Dictionary dictionary = new Dictionary { { "key", item.Key } };
			WriteBuffFields(item.Value, dictionary);
			array.Add(dictionary);
		}
		return array;
	}

	private Array<Dictionary> ExportNetworkSnapshot()
	{
		Array<Dictionary> array = new Array<Dictionary>();
		foreach (KeyValuePair<string, TowerDefenseCharacterBuffConfig> item in buffDictionary)
		{
			Dictionary dictionary = new Dictionary { { "key", item.Key } };
			WriteBuffFields(item.Value, dictionary, networkSafe: true);
			array.Add(dictionary);
		}
		return array;
	}

	public void ImportSave(Array<Dictionary> data)
	{
		if (data == null)
		{
			return;
		}
		HashSet<string> hashSet = _restoreKeysBuffer ?? (_restoreKeysBuffer = new HashSet<string>());
		hashSet.Clear();
		foreach (Dictionary datum in data)
		{
			string text = datum.GetValueOrDefault("key", "").AsString();
			if (!string.IsNullOrEmpty(text))
			{
				hashSet.Add(text);
			}
		}
		List<string> list = _keysSnapshotBuffer ?? (_keysSnapshotBuffer = new List<string>());
		SnapshotKeys(list);
		for (int i = 0; i < list.Count; i++)
		{
			string text2 = list[i];
			if (!hashSet.Contains(text2))
			{
				RemoveBuffCore(text2, null, notify: true);
			}
		}
		foreach (Dictionary datum2 in data)
		{
			RestoreBuff(datum2, notify: true);
		}
		hashSet.Clear();
	}

	public override Dictionary ExportComponentSave()
	{
		return new Dictionary { { "over", over } };
	}

	public override void ImportComponentSave(Dictionary data, TowerDefenseLevelSaveConfigCSharp owner)
	{
		over = data.GetValueOrDefault("over", over).AsBool();
	}

	public override Dictionary SyncSerialize()
	{
		return new Dictionary
		{
			{ "over", over },
			{
				"buffs",
				ExportNetworkSnapshot()
			}
		};
	}

	public override void SyncDeserialize(Dictionary data)
	{
		over = data.GetValueOrDefault("over", over).AsBool();
		if (over && (RemoveDeathHardControlBuff("Frozen") | RemoveDeathHardControlBuff("Butter")))
		{
			ResumeDeathAnimationAfterHardControlRemoval();
		}
		if (!data.ContainsKey("buffs"))
		{
			return;
		}
		HashSet<string> hashSet = _syncKeysBuffer ?? (_syncKeysBuffer = new HashSet<string>());
		hashSet.Clear();
		is_syncing = true;
		try
		{
			foreach (Variant item in data["buffs"].AsGodotArray())
			{
				Dictionary dictionary = item.AsGodotDictionary();
				string text = dictionary.GetValueOrDefault("key", "").AsString();
				if (!string.IsNullOrEmpty(text))
				{
					hashSet.Add(text);
					if (buffDictionary.TryGetValue(text, out var value))
					{
						ApplyBuffFields(value, dictionary);
						value.SyncPresentationReadOnlyClient();
					}
					else
					{
						RestoreBuff(dictionary, notify: true, readOnlyReplica: true);
					}
				}
			}
			List<string> list = _keysSnapshotBuffer ?? (_keysSnapshotBuffer = new List<string>());
			SnapshotKeys(list);
			for (int i = 0; i < list.Count; i++)
			{
				string text2 = list[i];
				if (!hashSet.Contains(text2))
				{
					RemoveBuffCore(text2, null, notify: true, BuffRemovalReason.ReadOnlyReplica);
				}
			}
		}
		finally
		{
			is_syncing = false;
			hashSet.Clear();
			RebuildFrameMeshColor();
		}
	}

	internal void ApplyLegacyNetworkSnapshot(Godot.Collections.Array buffsData)
	{
		Array<Dictionary> array = new Array<Dictionary>();
		if (buffsData != null)
		{
			foreach (Variant buffsDatum in buffsData)
			{
				if (buffsDatum.VariantType != Variant.Type.Dictionary)
				{
					continue;
				}
				Dictionary dictionary = buffsDatum.AsGodotDictionary();
				string text = dictionary.GetValueOrDefault("k", "").AsString();
				if (!string.IsNullOrEmpty(text))
				{
					Dictionary dictionary2 = new Dictionary { { "key", text } };
					if (dictionary.ContainsKey("t"))
					{
						dictionary2["time"] = dictionary["t"];
					}
					if (dictionary.ContainsKey("ct"))
					{
						dictionary2["currentTime"] = dictionary["ct"];
					}
					array.Add(dictionary2);
				}
			}
		}
		SyncDeserialize(new Dictionary { { "buffs", array } });
	}

	private bool EnterBuff(TowerDefenseCharacterBuffConfig buff, bool notify, bool readOnlyReplica = false)
	{
		if (buff == null || string.IsNullOrEmpty(buff.key) || !GodotObject.IsInstanceValid(parent))
		{
			return false;
		}
		if (over && IsDeathHardControlKey(buff.key))
		{
			buff.character = null;
			return false;
		}
		buff.character = parent;
		buffDictionary[buff.key] = buff;
		if (buff.MayModifyIncomingDamage)
		{
			(_incomingDamageModifiers ?? (_incomingDamageModifiers = new List<TowerDefenseCharacterBuffConfig>())).Add(buff);
		}
		TowerDefenseCharacterBuffConfig enteringBuff = _enteringBuff;
		_enteringBuff = buff;
		try
		{
			if (readOnlyReplica)
			{
				buff.EnterReadOnlyClient();
			}
			else
			{
				buff.Enter();
			}
		}
		finally
		{
			_enteringBuff = enteringBuff;
		}
		if (!buffDictionary.TryGetValue(buff.key, out var value) || value != buff)
		{
			_incomingDamageModifiers?.Remove(buff);
			return false;
		}
		RebuildFrameMeshColor();
		if (notify)
		{
			this.OnBuffAdd?.Invoke(buff.key);
		}
		return true;
	}

	private static bool IsDeathHardControlKey(string key)
	{
		if (!(key == "Frozen"))
		{
			return key == "Butter";
		}
		return true;
	}

	private bool RemoveBuffCore(string key, TowerDefenseCharacterBuffConfig expectedBuff, bool notify, BuffRemovalReason reason = BuffRemovalReason.Removed)
	{
		if (!buffDictionary.TryGetValue(key, out var value))
		{
			return false;
		}
		if (expectedBuff != null && expectedBuff != value)
		{
			return false;
		}
		buffDictionary.Remove(key);
		_incomingDamageModifiers?.Remove(value);
		bool flag = _enteringBuff != value;
		if (flag)
		{
			switch (reason)
			{
			case BuffRemovalReason.ReadOnlyReplica:
				value.ExitReadOnlyClient();
				break;
			case BuffRemovalReason.Expired:
				value.Exit();
				break;
			default:
				value.Remove();
				break;
			}
		}
		RebuildFrameMeshColor();
		if (notify && flag)
		{
			this.OnBuffDelete?.Invoke(key);
		}
		return true;
	}

	private bool RestoreBuff(Dictionary buffData, bool notify, bool readOnlyReplica = false)
	{
		string text = buffData.GetValueOrDefault("key", "").AsString();
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		TowerDefenseCharacterBuffConfig towerDefenseCharacterBuffConfig = TowerDefenseCharacterBuffConfig.CreateBuffByKey(text);
		if (towerDefenseCharacterBuffConfig == null)
		{
			return false;
		}
		towerDefenseCharacterBuffConfig.character = parent;
		ApplyBuffFields(towerDefenseCharacterBuffConfig, buffData);
		if (TryConsumeIncomingBuff(towerDefenseCharacterBuffConfig, notify))
		{
			return true;
		}
		if (buffDictionary.TryGetValue(text, out var value))
		{
			RemoveBuffCore(text, value, notify, readOnlyReplica ? BuffRemovalReason.ReadOnlyReplica : BuffRemovalReason.Removed);
		}
		if (!EnterBuff(towerDefenseCharacterBuffConfig, notify: false, readOnlyReplica))
		{
			return false;
		}
		ApplyBuffFields(towerDefenseCharacterBuffConfig, buffData);
		if (readOnlyReplica)
		{
			towerDefenseCharacterBuffConfig.SyncPresentationReadOnlyClient();
		}
		if (notify)
		{
			this.OnBuffAdd?.Invoke(towerDefenseCharacterBuffConfig.key);
		}
		return true;
	}

	private bool TryConsumeIncomingBuff(TowerDefenseCharacterBuffConfig buff, bool notifyExistingRemoval)
	{
		if (buff == null || string.IsNullOrEmpty(buff.key) || !GodotObject.IsInstanceValid(parent) || !parent.TryConsumeIncomingBuff(buff))
		{
			return false;
		}
		if (buffDictionary.TryGetValue(buff.key, out var value))
		{
			RemoveBuffCore(buff.key, value, notifyExistingRemoval, IsReadOnlyClient() ? BuffRemovalReason.ReadOnlyReplica : BuffRemovalReason.Removed);
		}
		return true;
	}

	private static void WriteBuffFields(TowerDefenseCharacterBuffConfig buff, Dictionary data, bool networkSafe = false)
	{
		for (int i = 0; i < BUFF_COMMON_SAVE_FIELDS.Length; i++)
		{
			string field = BUFF_COMMON_SAVE_FIELDS[i];
			WriteBuffField(buff, data, field, networkSafe);
		}
		string[] buffSaveFields = GetBuffSaveFields(buff.key);
		for (int j = 0; j < buffSaveFields.Length; j++)
		{
			WriteBuffField(buff, data, buffSaveFields[j], networkSafe);
		}
	}

	private static void WriteBuffField(TowerDefenseCharacterBuffConfig buff, Dictionary data, string field, bool networkSafe)
	{
		Variant variant = buff.Get(field);
		data[field] = (networkSafe ? EncodeNetworkValue(variant) : variant);
	}

	private static Variant EncodeNetworkValue(Variant value)
	{
		switch (value.VariantType)
		{
		case Variant.Type.Object:
		{
			GodotObject godotObject = value.AsGodotObject();
			if (godotObject == null)
			{
				return value;
			}
			if (!(godotObject is Resource resource))
			{
				return default(Variant);
			}
			return Variant.From<Dictionary>(new Dictionary
			{
				{ "__type", "resource" },
				{
					"path",
					resource.ResourcePath ?? string.Empty
				}
			});
		}
		case Variant.Type.Vector2:
		{
			Vector2 vector = value.AsVector2();
			Dictionary from = new Dictionary
			{
				{ "__type", "vector2" },
				{ "x", vector.X },
				{ "y", vector.Y }
			};
			return Variant.From(in from);
		}
		default:
			return value;
		}
	}

	private static void ApplyBuffFields(TowerDefenseCharacterBuffConfig buff, Dictionary buffData)
	{
		for (int i = 0; i < BUFF_COMMON_SAVE_FIELDS.Length; i++)
		{
			string text = BUFF_COMMON_SAVE_FIELDS[i];
			if (buffData.ContainsKey(text))
			{
				ApplyBuffField(buff, text, buffData[text]);
			}
		}
		string[] buffSaveFields = GetBuffSaveFields(buff.key);
		foreach (string text2 in buffSaveFields)
		{
			if (buffData.ContainsKey(text2))
			{
				ApplyBuffField(buff, text2, buffData[text2]);
			}
		}
	}

	private static void ApplyBuffField(TowerDefenseCharacterBuffConfig buff, string field, Variant value)
	{
		if (value.VariantType != Variant.Type.Dictionary)
		{
			buff.Set(field, value);
			return;
		}
		Dictionary dictionary = value.AsGodotDictionary();
		string text = dictionary.GetValueOrDefault("__type", string.Empty).AsString();
		if (!(text == "resource"))
		{
			if (text == "vector2")
			{
				buff.Set(field, new Vector2((float)dictionary.GetValueOrDefault("x", 0.0).AsDouble(), (float)dictionary.GetValueOrDefault("y", 0.0).AsDouble()));
			}
			else
			{
				buff.Set(field, value);
			}
		}
		else
		{
			string text2 = dictionary.GetValueOrDefault("path", string.Empty).AsString();
			Resource resource = ((!string.IsNullOrEmpty(text2) && text2.StartsWith("res://", StringComparison.Ordinal) && ResourceLoader.Exists(text2)) ? ResourceLoader.Load(text2, "", ResourceLoader.CacheMode.Reuse) : null);
			buff.Set(field, resource);
		}
	}

	private static string[] GetBuffSaveFields(string key)
	{
		if (string.IsNullOrEmpty(key) || !BUFF_SAVE_FIELDS.TryGetValue(key, out var value))
		{
			return System.Array.Empty<string>();
		}
		return value;
	}

	private void SnapshotKeys(List<string> destination)
	{
		destination.Clear();
		if (_buffDictionary == null)
		{
			return;
		}
		foreach (string key in _buffDictionary.Keys)
		{
			destination.Add(key);
		}
	}

	private static bool IsReadOnlyClient()
	{
		if (Global.Instance.isMultiplayerMode)
		{
			return !MultiPlayerManager.Instance.isHost;
		}
		return false;
	}
}
