using Godot;
using Godot.Collections;

public sealed class GrowUpComponent : CharacterComponentRuntime
{
	public delegate void GrowEventHandler(int reach);

	public Array<float> growUpTime = new Array<float>();

	public Array<float> growUpSize = new Array<float>();

	public string growAudio = "Grow";

	public double growTweenDuration = 1.0;

	public bool instantGrowInIZM = true;

	public TowerDefenseCharacter parent;

	public float timer;

	public int growUpReach;

	private static readonly StringName SyncAliveKey = new StringName("_alive");

	private static readonly StringName SyncTimerKey = new StringName("timer");

	private static readonly StringName SyncReachKey = new StringName("growUpReach");

	private readonly Dictionary _syncPayload = new Dictionary();

	private bool _syncPayloadInitialized;

	private float _syncPayloadTimer;

	private int _syncPayloadReach;

	private bool _configured;

	private bool _parentReadyConnected;

	private ulong _parentReadyInitializationGeneration;

	private bool _restoredStateReplayQueued;

	private int _pendingRestoredReach = -1;

	private Tween _growTween;

	private int _stageCount;

	public bool IsApplyingRestoredState { get; private set; }

	public bool ShouldApplyAuthoritativeGrowthEffects
	{
		get
		{
			if (!IsApplyingRestoredState)
			{
				return TowerDefenseManager.HasGameplayAuthority;
			}
			return false;
		}
	}

	private int StageCount => _stageCount;

	private GrowUpComponentDefinition Definition => base.ComponentDefinition as GrowUpComponentDefinition;

	internal override bool WantsPhysicsProcess
	{
		get
		{
			if (base.Alive)
			{
				return growUpReach < StageCount;
			}
			return false;
		}
	}

	public event GrowEventHandler OnGrow;

	protected override void OnBound()
	{
		parent = base.Owner;
		ConfigureOnce();
		ulong initializationGeneration = ++_parentReadyInitializationGeneration;
		if (!GodotObject.IsInstanceValid(parent))
		{
			return;
		}
		if (parent.IsNodeReady())
		{
			Callable.From(delegate
			{
				InitializeAfterParentReadyDeferred(initializationGeneration);
			}).CallDeferred();
		}
		else
		{
			parent.Ready += InitializeAfterParentReady;
			_parentReadyConnected = true;
		}
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		_parentReadyInitializationGeneration++;
		DisconnectParentReady();
		KillGrowTween();
		_restoredStateReplayQueued = false;
		_pendingRestoredReach = -1;
		IsApplyingRestoredState = false;
		ClearSyncPayload();
		parent = null;
	}

	protected override void OnReleased()
	{
		this.OnGrow = null;
		parent = null;
		_restoredStateReplayQueued = false;
		_pendingRestoredReach = -1;
		IsApplyingRestoredState = false;
		_stageCount = 0;
		ClearSyncPayload();
		growUpTime.Clear();
		growUpSize.Clear();
	}

	protected override void OnAliveChanged(bool alive)
	{
		if (!alive)
		{
			KillGrowTween();
		}
	}

	private void ConfigureOnce()
	{
		if (!_configured && Definition != null)
		{
			GrowUpComponentDefinition definition = Definition;
			growUpTime = ((definition.growUpTime != null) ? definition.growUpTime.Duplicate(deep: true) : new Array<float>());
			growUpSize = ((definition.growUpSize != null) ? definition.growUpSize.Duplicate(deep: true) : new Array<float>());
			_stageCount = Mathf.Min(growUpTime.Count, growUpSize.Count);
			growAudio = definition.growAudio;
			growTweenDuration = definition.growTweenDuration;
			instantGrowInIZM = definition.instantGrowInIZM;
			_configured = true;
		}
	}

	private void InitializeAfterParentReady()
	{
		DisconnectParentReady();
		int stageCount = StageCount;
		if (base.Alive && TowerDefenseManager.HasGameplayAuthority && stageCount != 0 && instantGrowInIZM && GodotObject.IsInstanceValid(parent?.transformPoint) && GodotObject.IsInstanceValid(TowerDefenseManager.Instance) && TowerDefenseManager.Instance.IsIZMMode())
		{
			int num = stageCount - 1;
			parent.transformPoint.Scale = Vector2.One * growUpSize[num];
			growUpReach = num;
			this.OnGrow?.Invoke(num);
			growUpReach = stageCount;
			RefreshPhysicsProcessEligibility();
		}
	}

	private void InitializeAfterParentReadyDeferred(ulong initializationGeneration)
	{
		if (initializationGeneration == _parentReadyInitializationGeneration)
		{
			InitializeAfterParentReady();
		}
	}

	private void DisconnectParentReady()
	{
		if (_parentReadyConnected && GodotObject.IsInstanceValid(parent))
		{
			parent.Ready -= InitializeAfterParentReady;
		}
		_parentReadyConnected = false;
	}

	internal override void PhysicsProcess(double delta, ulong physicsFrame)
	{
		if (!TowerDefenseManager.HasGameplayAuthority || !TryGetParent(out var character) || character.die || character.instance.sleep || !character.componentAlive || !GodotObject.IsInstanceValid(TowerDefenseManager.CurrentControl) || !TowerDefenseManager.CurrentControl.isGameRunning)
		{
			return;
		}
		int stageCount = StageCount;
		if (growUpReach >= stageCount)
		{
			RefreshPhysicsProcessEligibility();
			return;
		}
		timer += Mathf.Max(0f, (float)delta);
		int num = growUpReach;
		while (growUpReach < stageCount && timer >= growUpTime[growUpReach])
		{
			this.OnGrow?.Invoke(growUpReach);
			growUpReach++;
		}
		if (growUpReach > num)
		{
			ApplyGrowthScale(growUpReach - 1);
		}
		if (growUpReach >= stageCount)
		{
			RefreshPhysicsProcessEligibility();
		}
	}

	private void ApplyGrowthScale(int scaleIndex)
	{
		if (TryGetParent(out var character) && scaleIndex >= 0 && scaleIndex < StageCount)
		{
			if (!string.IsNullOrEmpty(growAudio) && GodotObject.IsInstanceValid(AudioManager.Instance))
			{
				AudioManager.Instance.AudioPlay(growAudio);
			}
			KillGrowTween();
			Vector2 vector = Vector2.One * growUpSize[scaleIndex];
			if (growTweenDuration <= 0.0)
			{
				character.transformPoint.Scale = vector;
				return;
			}
			_growTween = character.CreateTween();
			_growTween.TweenProperty(character.transformPoint, "scale", vector, growTweenDuration);
		}
	}

	private void KillGrowTween()
	{
		if (GodotObject.IsInstanceValid(_growTween))
		{
			_growTween.Kill();
		}
		_growTween = null;
	}

	private bool TryGetParent(out TowerDefenseCharacter character)
	{
		character = parent;
		if (base.Alive && GodotObject.IsInstanceValid(character) && GodotObject.IsInstanceValid(character.instance))
		{
			return GodotObject.IsInstanceValid(character.transformPoint);
		}
		return false;
	}

	public override Dictionary ExportComponentSave()
	{
		return new Dictionary
		{
			{ "timer", timer },
			{ "growUpReach", growUpReach }
		};
	}

	public override void ImportComponentSave(Dictionary data, TowerDefenseLevelSaveConfigCSharp owner)
	{
		if (data != null)
		{
			timer = Mathf.Max(0f, data.GetValueOrDefault("timer", 0.0).AsSingle());
			growUpReach = Mathf.Clamp(data.GetValueOrDefault("growUpReach", 0).AsInt32(), 0, StageCount);
			QueueRestoredStateReplay(growUpReach);
			RefreshPhysicsProcessEligibility();
		}
	}

	public override Dictionary SyncSerialize()
	{
		float num = timer;
		int num2 = growUpReach;
		if (_syncPayloadInitialized)
		{
			if (_syncPayload.Count == 3)
			{
				_syncPayload.Remove(SyncAliveKey);
			}
			if (_syncPayload.Count == 2 && _syncPayloadTimer == num && _syncPayloadReach == num2)
			{
				return _syncPayload;
			}
		}
		_syncPayload.Clear();
		_syncPayload[SyncTimerKey] = num;
		_syncPayload[SyncReachKey] = num2;
		_syncPayloadTimer = num;
		_syncPayloadReach = num2;
		_syncPayloadInitialized = true;
		return _syncPayload;
	}

	private void ClearSyncPayload()
	{
		_syncPayload.Clear();
		_syncPayloadInitialized = false;
		_syncPayloadTimer = 0f;
		_syncPayloadReach = 0;
	}

	public override void SyncDeserialize(Dictionary data)
	{
		if (data != null)
		{
			int num = growUpReach;
			timer = Mathf.Max(0f, data.GetValueOrDefault("timer", timer).AsSingle());
			int num2 = Mathf.Clamp(data.GetValueOrDefault("growUpReach", growUpReach).AsInt32(), 0, StageCount);
			if (num2 > 0 && StageCount > 0 && TryGetParent(out var character))
			{
				int index = Mathf.Clamp(num2 - 1, 0, StageCount - 1);
				character.transformPoint.Scale = Vector2.One * growUpSize[index];
			}
			growUpReach = num2;
			if (num2 > num)
			{
				ReplayRestoredStages(num, num2);
			}
			RefreshPhysicsProcessEligibility();
		}
	}

	private void QueueRestoredStateReplay(int nextReach)
	{
		_pendingRestoredReach = Mathf.Clamp(nextReach, 0, StageCount);
		if (!_restoredStateReplayQueued && GodotObject.IsInstanceValid(parent))
		{
			_restoredStateReplayQueued = true;
			Callable.From(ApplyQueuedRestoredStateReplay).CallDeferred();
		}
	}

	private void ApplyQueuedRestoredStateReplay()
	{
		_restoredStateReplayQueued = false;
		int pendingRestoredReach = _pendingRestoredReach;
		_pendingRestoredReach = -1;
		if (base.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(parent))
		{
			ReplayRestoredStages(0, pendingRestoredReach);
		}
	}

	private void ReplayRestoredStages(int firstStage, int nextReach)
	{
		int num = Mathf.Clamp(nextReach, 0, StageCount);
		int num2 = Mathf.Clamp(firstStage, 0, num);
		if (num2 >= num)
		{
			return;
		}
		bool isApplyingRestoredState = IsApplyingRestoredState;
		IsApplyingRestoredState = true;
		try
		{
			for (int i = num2; i < num; i++)
			{
				this.OnGrow?.Invoke(i);
			}
		}
		finally
		{
			IsApplyingRestoredState = isApplyingRestoredState;
		}
	}
}
