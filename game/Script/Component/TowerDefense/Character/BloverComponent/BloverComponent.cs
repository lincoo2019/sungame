using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public sealed class BloverComponent : CharacterComponentRuntime
{
	public delegate void BlowOverEventHandler();

	public double blowTime = 2.0;

	public double blowLength = 0.5;

	public double blowPhysiqueHugeLength = 0.5;

	public double blowAirCharacterLength = 0.5;

	public bool blowAirCharacterOut = true;

	public bool checkLine;

	public bool checkCollision;

	public bool checkPhysiqueHuge;

	public string blowAudio = "Blover";

	public int projectileRowNum = 20;

	public float projectileBatchInterval = 0.1f;

	public Vector2 projectileHeightOffsetRange = new Vector2(10f, 60f);

	public Vector2 projectileSpeedRange = new Vector2(400f, 800f);

	public float projectileLeftSpawnX = -50f;

	public Array<TowerDefenseProjectileCreateData> projectileDataList = new Array<TowerDefenseProjectileCreateData>();

	public Array<TowerDefenseCharacterEventBase> eventList = new Array<TowerDefenseCharacterEventBase>();

	public TowerDefenseCharacter parent;

	public List<System.Collections.Generic.Dictionary<string, Variant>> _syncProjectileData = new List<System.Collections.Generic.Dictionary<string, Variant>>();

	public bool _syncDeserializing;

	private readonly List<TowerDefenseCharacter> _targetBuffer = new List<TowerDefenseCharacter>(32);

	private readonly List<TowerDefenseProjectileConfig> _projectileConfigBuffer = new List<TowerDefenseProjectileConfig>(4);

	private bool _executionRunning;

	private ulong _executionVersion;

	private bool _configured;

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

	private BloverComponentDefinition Definition => base.ComponentDefinition as BloverComponentDefinition;

	public event BlowOverEventHandler OnBlowOver;

	protected override void OnBound()
	{
		parent = base.Owner;
		ApplyDefinition();
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		CancelExecution(clearSyncState: true);
		parent = null;
	}

	protected override void OnReleased()
	{
		CancelExecution(clearSyncState: true);
		parent = null;
		projectileDataList.Clear();
		eventList.Clear();
		_configured = false;
	}

	private void ApplyDefinition()
	{
		if (!_configured && Definition != null)
		{
			blowTime = Definition.blowTime;
			blowLength = Definition.blowLength;
			blowPhysiqueHugeLength = Definition.blowPhysiqueHugeLength;
			blowAirCharacterLength = Definition.blowAirCharacterLength;
			blowAirCharacterOut = Definition.blowAirCharacterOut;
			checkLine = Definition.checkLine;
			checkCollision = Definition.checkCollision;
			checkPhysiqueHuge = Definition.checkPhysiqueHuge;
			blowAudio = Definition.blowAudio;
			projectileRowNum = Definition.projectileRowNum;
			projectileBatchInterval = Definition.projectileBatchInterval;
			projectileHeightOffsetRange = Definition.projectileHeightOffsetRange;
			projectileSpeedRange = Definition.projectileSpeedRange;
			projectileLeftSpawnX = Definition.projectileLeftSpawnX;
			CloneDefinitionResources();
			_configured = true;
		}
	}

	private void CloneDefinitionResources()
	{
		projectileDataList.Clear();
		int num = Definition.projectileDataList?.Count ?? 0;
		for (int i = 0; i < num; i++)
		{
			TowerDefenseProjectileCreateData towerDefenseProjectileCreateData = Definition.projectileDataList[i];
			TowerDefenseProjectileCreateData towerDefenseProjectileCreateData2 = (GodotObject.IsInstanceValid(towerDefenseProjectileCreateData) ? (towerDefenseProjectileCreateData.Duplicate(deep: true) as TowerDefenseProjectileCreateData) : null);
			if (GodotObject.IsInstanceValid(towerDefenseProjectileCreateData2))
			{
				projectileDataList.Add(towerDefenseProjectileCreateData2);
			}
		}
		eventList.Clear();
		int num2 = Definition.eventList?.Count ?? 0;
		for (int j = 0; j < num2; j++)
		{
			TowerDefenseCharacterEventBase towerDefenseCharacterEventBase = Definition.eventList[j];
			TowerDefenseCharacterEventBase towerDefenseCharacterEventBase2 = (GodotObject.IsInstanceValid(towerDefenseCharacterEventBase) ? (towerDefenseCharacterEventBase.Duplicate(deep: true) as TowerDefenseCharacterEventBase) : null);
			if (GodotObject.IsInstanceValid(towerDefenseCharacterEventBase2))
			{
				eventList.Add(towerDefenseCharacterEventBase2);
			}
		}
	}

	private void CancelExecution(bool clearSyncState)
	{
		_executionVersion++;
		_executionRunning = false;
		_targetBuffer.Clear();
		_projectileConfigBuffer.Clear();
		if (clearSyncState)
		{
			_syncProjectileData.Clear();
			_syncDeserializing = false;
		}
	}

	public void Execute()
	{
		if (TryGetRuntime(out var owner, out var manager))
		{
			if (GodotObject.IsInstanceValid(BattleEventBus.Instance))
			{
				BattleEventBus.Instance.EmitBlowAllEffectEmit();
			}
			StartExecution(manager.GetCharacterTarget(owner, checkLine, checkCollision));
		}
	}

	public void Execult()
	{
		Execute();
	}

	public void ExecuteLine(int line)
	{
		if (TryGetRuntime(out var owner, out var manager))
		{
			if (GodotObject.IsInstanceValid(BattleEventBus.Instance))
			{
				BattleEventBus.Instance.EmitBlowLineEffectEmit(line);
			}
			StartExecution(manager.GetCharacterTargetLine(owner));
		}
	}

	public void ExecuteTargetList(List<TowerDefenseCharacter> targetList)
	{
		StartExecution(targetList);
	}

	private void StartExecution(List<TowerDefenseCharacter> targetList)
	{
		if (!_executionRunning && TryGetRuntime(out var _, out var _))
		{
			_targetBuffer.Clear();
			if (targetList != null)
			{
				_targetBuffer.AddRange(targetList);
			}
			_executionRunning = true;
			ExecuteTargetListAsync(++_executionVersion);
		}
	}

	private async Task ExecuteTargetListAsync(ulong version)
	{
		_ = 3;
		try
		{
			if (IsCurrent(version, out var owner, out var manager))
			{
				PlayAudio();
				if (!IsRemoteClient)
				{
					ExecuteCharacterEffects(owner);
				}
				BuildProjectileConfigs();
				if (_projectileConfigBuffer.Count == 0)
				{
					await WaitAsync(Mathf.Max(0f, (float)blowTime), version);
				}
				else if (!IsRemoteClient)
				{
					await SpawnProjectileWaves(version);
				}
				else if (!_syncDeserializing || _syncProjectileData.Count <= 0)
				{
					await WaitAsync(Mathf.Max(0f, (float)blowTime), version);
				}
				else
				{
					await ReplaySyncedProjectiles(version);
				}
				if (IsCurrent(version, out var _, out manager) && !IsRemoteClient)
				{
					this.OnBlowOver?.Invoke();
				}
			}
		}
		finally
		{
			_targetBuffer.Clear();
			_projectileConfigBuffer.Clear();
			if (version == _executionVersion)
			{
				_executionRunning = false;
			}
		}
	}

	private void ExecuteCharacterEffects(TowerDefenseCharacter owner)
	{
		double num = ((owner.Scale.X < 0f) ? (-1.0) : 1.0);
		for (int i = 0; i < _targetBuffer.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = _targetBuffer[i];
			if (!CanProcessTarget(towerDefenseCharacter))
			{
				continue;
			}
			ExecuteEvents(towerDefenseCharacter);
			if ((towerDefenseCharacter.instance.unUseBuffFlags & 0x200) != 0 || towerDefenseCharacter.instance.maskFlags == 0)
			{
				continue;
			}
			bool flag = (towerDefenseCharacter.instance.maskFlags & 2) != 0;
			if (flag && blowAirCharacterOut)
			{
				towerDefenseCharacter.Blow();
				continue;
			}
			double num2 = (flag ? blowAirCharacterLength : blowLength);
			if (!flag && towerDefenseCharacter is TowerDefenseZombie && towerDefenseCharacter.instance.zombiePhysique >= TowerDefenseEnum.ZOMBIE_PHYSIQUE.HUGE)
			{
				num2 = blowPhysiqueHugeLength;
			}
			towerDefenseCharacter.BlowBack(num2 * num, blowTime);
		}
	}

	private bool CanProcessTarget(TowerDefenseCharacter target)
	{
		if (!GodotObject.IsInstanceValid(target) || !GodotObject.IsInstanceValid(target.instance) || target is TowerDefensePlant || target is TowerDefenseGravestone || target is TowerDefenseItem)
		{
			return false;
		}
		int maskFlags = target.instance.maskFlags;
		if ((maskFlags & 0x10) != 0 || (maskFlags & 0x20) != 0)
		{
			return false;
		}
		if (!checkPhysiqueHuge && target is TowerDefenseZombie && target.instance.zombiePhysique >= TowerDefenseEnum.ZOMBIE_PHYSIQUE.HUGE && (maskFlags & 2) == 0)
		{
			return false;
		}
		return true;
	}

	private void ExecuteEvents(TowerDefenseCharacter target)
	{
		int num = eventList?.Count ?? 0;
		for (int i = 0; i < num; i++)
		{
			TowerDefenseCharacterEventBase towerDefenseCharacterEventBase = eventList[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacterEventBase))
			{
				towerDefenseCharacterEventBase.Execute(target.GetLogicalGlobalPosition(), target);
			}
		}
	}

	private void BuildProjectileConfigs()
	{
		_projectileConfigBuffer.Clear();
		int num = projectileDataList?.Count ?? 0;
		for (int i = 0; i < num; i++)
		{
			TowerDefenseProjectileCreateData towerDefenseProjectileCreateData = projectileDataList[i];
			if (GodotObject.IsInstanceValid(towerDefenseProjectileCreateData))
			{
				TowerDefenseProjectileConfig towerDefenseProjectileConfig = towerDefenseProjectileCreateData.BuildConfig();
				if (GodotObject.IsInstanceValid(towerDefenseProjectileConfig))
				{
					_projectileConfigBuffer.Add(towerDefenseProjectileConfig);
				}
			}
		}
	}

	private async Task SpawnProjectileWaves(ulong version)
	{
		if (!IsCurrent(version, out var owner, out var manager))
		{
			return;
		}
		_syncProjectileData.Clear();
		int lineCount = Mathf.Max(0, manager.GetMapGridNum().Y);
		int waveCount = Mathf.Max(0, projectileRowNum);
		double baseHeight = owner.GetGroundHeight(owner.GetLogicalGlobalPosition().Y);
		for (int wave = 0; wave < waveCount; wave++)
		{
			for (int i = 1; i <= lineCount; i++)
			{
				if (!IsCurrent(version, out owner, out manager))
				{
					return;
				}
				int num = (int)(GD.Randi() % (uint)_projectileConfigBuffer.Count);
				double num2 = RandomRange(projectileHeightOffsetRange);
				double num3 = RandomRange(projectileSpeedRange) * ((owner.Scale.X < 0f) ? (-1.0) : 1.0);
				_syncProjectileData.Add(new System.Collections.Generic.Dictionary<string, Variant>
				{
					{ "line", i },
					{ "height_offset", num2 },
					{ "velocity_x", num3 },
					{ "config_index", num }
				});
				SpawnProjectile(owner, manager, i, baseHeight, num2, num3, _projectileConfigBuffer[num]);
			}
			if (!(await WaitAsync(Mathf.Max(0f, projectileBatchInterval), version)))
			{
				break;
			}
		}
	}

	private async Task ReplaySyncedProjectiles(ulong version)
	{
		if (!IsCurrent(version, out var owner, out var manager))
		{
			return;
		}
		int lineCount = Mathf.Max(0, manager.GetMapGridNum().Y);
		double baseHeight = owner.GetGroundHeight(owner.GetLogicalGlobalPosition().Y);
		for (int index = 0; index < _syncProjectileData.Count; index++)
		{
			System.Collections.Generic.Dictionary<string, Variant> dictionary = _syncProjectileData[index];
			int num = dictionary.GetValueOrDefault("line", 1).AsInt32();
			int num2 = dictionary.GetValueOrDefault("config_index", 0).AsInt32();
			if (num >= 1 && num <= lineCount && num2 >= 0 && num2 < _projectileConfigBuffer.Count)
			{
				double heightOffset = dictionary.GetValueOrDefault("height_offset", 0.0).AsDouble();
				double speed = dictionary.GetValueOrDefault("velocity_x", 0.0).AsDouble();
				SpawnProjectile(owner, manager, num, baseHeight, heightOffset, speed, _projectileConfigBuffer[num2]);
				bool flag = lineCount > 0 && (index + 1) % lineCount == 0;
				if (flag)
				{
					flag = !(await WaitAsync(Mathf.Max(0f, projectileBatchInterval), version));
				}
				if (flag)
				{
					return;
				}
			}
		}
		_syncProjectileData.Clear();
		_syncDeserializing = false;
	}

	private void SpawnProjectile(TowerDefenseCharacter owner, TowerDefenseManager manager, int line, double baseHeight, double heightOffset, double speed, TowerDefenseProjectileConfig config)
	{
		Vector2 pos = new Vector2(projectileLeftSpawnX, TowerDefenseManager.GetMapCellPlantPos(new Vector2I(0, line)).Y);
		if (owner.Scale.X < 0f)
		{
			pos.X = (float)manager.GetMapGroundRight();
		}
		FireComponent.CreateProjectilePositionByConfig(null, null, baseHeight + heightOffset, pos, new Vector2((float)speed, 0f), config, -1, owner.camp, default(Vector2), new BulletFieldSpawnOverrides
		{
			gridYOverride = line,
			flipXOverride = (owner.Scale.X < 0f)
		});
	}

	private async Task<bool> WaitAsync(float duration, ulong version)
	{
		TowerDefenseCharacter owner;
		TowerDefenseManager manager;
		if (duration <= 0f)
		{
			return IsCurrent(version, out owner, out manager);
		}
		if (!IsCurrent(version, out var owner2, out manager))
		{
			return false;
		}
		SceneTree tree = owner2.GetTree();
		if (!GodotObject.IsInstanceValid(tree))
		{
			return false;
		}
		SceneTreeTimer source = tree.CreateTimer(duration, processAlways: false);
		await owner2.ToSignal(source, SceneTreeTimer.SignalName.Timeout);
		return IsCurrent(version, out owner, out manager);
	}

	private static double RandomRange(Vector2 range)
	{
		return GD.RandRange(Mathf.Min(range.X, range.Y), Mathf.Max(range.X, range.Y));
	}

	private void PlayAudio()
	{
		if (!string.IsNullOrEmpty(blowAudio) && GodotObject.IsInstanceValid(AudioManager.Instance))
		{
			AudioManager.Instance.AudioPlay(blowAudio);
		}
	}

	protected override void OnAliveChanged(bool alive)
	{
		if (!alive)
		{
			CancelExecution(clearSyncState: false);
		}
	}

	private bool IsCurrent(ulong version, out TowerDefenseCharacter owner, out TowerDefenseManager manager)
	{
		owner = null;
		manager = null;
		if (version == _executionVersion)
		{
			return TryGetRuntime(out owner, out manager);
		}
		return false;
	}

	private bool TryGetRuntime(out TowerDefenseCharacter owner, out TowerDefenseManager manager)
	{
		owner = parent;
		manager = TowerDefenseManager.Instance;
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(owner) && GodotObject.IsInstanceValid(owner.instance))
		{
			return GodotObject.IsInstanceValid(manager);
		}
		return false;
	}

	public override Dictionary SyncSerialize()
	{
		Dictionary dictionary = new Dictionary();
		if (_syncProjectileData.Count == 0)
		{
			return dictionary;
		}
		Array array = new Array();
		for (int i = 0; i < _syncProjectileData.Count; i++)
		{
			Dictionary dictionary2 = new Dictionary();
			foreach (KeyValuePair<string, Variant> item in _syncProjectileData[i])
			{
				dictionary2[item.Key] = item.Value;
			}
			array.Add(dictionary2);
		}
		dictionary["projectile_data"] = array;
		return dictionary;
	}

	public override void SyncDeserialize(Dictionary data)
	{
		if (!data.ContainsKey("projectile_data"))
		{
			return;
		}
		_syncProjectileData.Clear();
		foreach (Variant item in data.GetValueOrDefault("projectile_data").AsGodotArray())
		{
			Dictionary dictionary = item.AsGodotDictionary();
			System.Collections.Generic.Dictionary<string, Variant> dictionary2 = new System.Collections.Generic.Dictionary<string, Variant>();
			foreach (Variant key in dictionary.Keys)
			{
				dictionary2[key.AsString()] = dictionary[key];
			}
			_syncProjectileData.Add(dictionary2);
		}
		_syncDeserializing = true;
	}
}
