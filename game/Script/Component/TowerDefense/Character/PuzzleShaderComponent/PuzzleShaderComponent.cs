using Godot;
using Godot.Collections;

public sealed class PuzzleShaderComponent : CharacterComponentRuntime
{
	private static readonly StringName SyncAliveKey = new StringName("_alive");

	private static readonly StringName SyncIdleFrozenKey = new StringName("idleFrozen");

	private static readonly StringName SyncShaderAppliedKey = new StringName("shaderApplied");

	private static readonly StringName SyncComponentAliveKey = new StringName("alive");

	public bool enablePuzzleShader = true;

	public bool freezeInIzmIdle = true;

	public StringName shaderParameter = "puzzle";

	public TowerDefensePlant parent;

	private readonly Dictionary _syncPayload = new Dictionary();

	private bool _idleFrozen;

	private bool _syncPayloadIdleFrozen;

	private bool _syncPayloadShaderApplied;

	private bool _syncPayloadAlive;

	private bool _syncPayloadInitialized;

	private double _savedTimeScaleInit;

	private double _savedTimeScale;

	private double _savedSpriteTimeScale;

	private double _savedTimeScaleSave;

	private StringName _appliedShaderParameter;

	private bool _shaderApplied;

	private bool _configured;

	private bool _resumeIdleFreezeAfterTemporaryExit;

	private PuzzleShaderComponentDefinition Definition => base.ComponentDefinition as PuzzleShaderComponentDefinition;

	protected override void OnBound()
	{
		parent = base.Owner as TowerDefensePlant;
		if (!_configured && Definition != null)
		{
			enablePuzzleShader = Definition.enablePuzzleShader;
			freezeInIzmIdle = Definition.freezeInIzmIdle;
			shaderParameter = Definition.shaderParameter;
			_configured = true;
		}
	}

	protected override void OnActivated()
	{
		RefreshConfiguration();
		if (_resumeIdleFreezeAfterTemporaryExit)
		{
			_resumeIdleFreezeAfterTemporaryExit = false;
			if (OwnerRemainsInIdleState())
			{
				IdleEntered();
			}
		}
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		ClearSyncPayload();
		bool resumeIdleFreezeAfterTemporaryExit = reason == ComponentDetachReason.TemporaryTreeExit && _idleFrozen;
		ClearPuzzleShader();
		RestoreIdleSpeed();
		_resumeIdleFreezeAfterTemporaryExit = resumeIdleFreezeAfterTemporaryExit;
		parent = null;
	}

	protected override void OnReleased()
	{
		ClearSyncPayload();
		ClearPuzzleShader();
		RestoreIdleSpeed();
		_resumeIdleFreezeAfterTemporaryExit = false;
		parent = null;
	}

	protected override void OnAliveChanged(bool alive)
	{
		if (!alive)
		{
			ClearPuzzleShader();
			RestoreIdleSpeed();
		}
		else
		{
			RefreshConfiguration();
		}
	}

	public void Init()
	{
		RefreshConfiguration();
	}

	public void ApplyPuzzleShader()
	{
		RefreshConfiguration();
	}

	public void RefreshConfiguration()
	{
		bool flag = base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && enablePuzzleShader && !shaderParameter.IsEmpty && GodotObject.IsInstanceValid(parent) && IsIzmMode();
		if (_shaderApplied && (!flag || _appliedShaderParameter != shaderParameter))
		{
			ClearPuzzleShader();
		}
		if (flag && !_shaderApplied)
		{
			parent.SetSpriteGroupShaderParameter(shaderParameter, true);
			_appliedShaderParameter = shaderParameter;
			_shaderApplied = true;
		}
	}

	private void ClearPuzzleShader()
	{
		if (_shaderApplied)
		{
			if (GodotObject.IsInstanceValid(parent) && !_appliedShaderParameter.IsEmpty)
			{
				parent.SetSpriteGroupShaderParameter(_appliedShaderParameter, false);
			}
			_appliedShaderParameter = null;
			_shaderApplied = false;
		}
	}

	public void IdleEntered()
	{
		if (_idleFrozen && (!base.Alive || base.Lifecycle != ComponentRuntimeLifecycle.Active || !freezeInIzmIdle || !IsIzmMode()))
		{
			RestoreIdleSpeed();
		}
		if (!_idleFrozen && base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && freezeInIzmIdle && GodotObject.IsInstanceValid(parent) && GodotObject.IsInstanceValid(parent.sprite) && IsIzmMode())
		{
			_savedTimeScaleInit = parent.timeScaleInit;
			_savedTimeScale = parent.timeScale;
			_savedSpriteTimeScale = parent.sprite.timeScale;
			_savedTimeScaleSave = parent.timeScaleSave;
			parent.timeScaleSave = parent.timeScaleInit;
			parent.timeScaleInit = 0.0;
			parent.timeScale = 0.0;
			parent.sprite.timeScale = 0.0;
			_idleFrozen = true;
		}
	}

	public void IdleExited()
	{
		RestoreIdleSpeed();
	}

	private void RestoreIdleSpeed()
	{
		if (!_idleFrozen)
		{
			return;
		}
		if (GodotObject.IsInstanceValid(parent))
		{
			parent.timeScaleInit = _savedTimeScaleInit;
			parent.timeScale = _savedTimeScale;
			parent.timeScaleSave = _savedTimeScaleSave;
			if (GodotObject.IsInstanceValid(parent.sprite))
			{
				parent.sprite.timeScale = _savedSpriteTimeScale;
			}
		}
		_idleFrozen = false;
	}

	private bool OwnerRemainsInIdleState()
	{
		StateHandle stateHandle = parent?.CurrentStateHandle;
		if (stateHandle != null && stateHandle.IsValid)
		{
			return stateHandle.StableId == "character.idle";
		}
		return true;
	}

	public override Dictionary ExportComponentSave()
	{
		return new Dictionary
		{
			{ "idleFrozen", _idleFrozen },
			{ "shaderApplied", _shaderApplied },
			{ "alive", base.Alive }
		};
	}

	public override void ImportComponentSave(Dictionary data, TowerDefenseLevelSaveConfigCSharp owner)
	{
		bool num = data.GetValueOrDefault("idleFrozen", false).AsBool();
		SetAlive(data.GetValueOrDefault("alive", base.Alive).AsBool());
		RefreshConfiguration();
		if (num)
		{
			IdleEntered();
		}
	}

	public override Dictionary SyncSerialize()
	{
		bool idleFrozen = _idleFrozen;
		bool shaderApplied = _shaderApplied;
		bool alive = base.Alive;
		if (_syncPayloadInitialized)
		{
			if (_syncPayload.Count == 4)
			{
				_syncPayload.Remove(SyncAliveKey);
			}
			if (_syncPayload.Count == 3 && _syncPayloadIdleFrozen == idleFrozen && _syncPayloadShaderApplied == shaderApplied && _syncPayloadAlive == alive)
			{
				return _syncPayload;
			}
		}
		_syncPayload.Clear();
		_syncPayload[SyncIdleFrozenKey] = idleFrozen;
		_syncPayload[SyncShaderAppliedKey] = shaderApplied;
		_syncPayload[SyncComponentAliveKey] = alive;
		_syncPayloadIdleFrozen = idleFrozen;
		_syncPayloadShaderApplied = shaderApplied;
		_syncPayloadAlive = alive;
		_syncPayloadInitialized = true;
		return _syncPayload;
	}

	public override void SyncDeserialize(Dictionary data)
	{
		bool flag = data.GetValueOrDefault("idleFrozen", _idleFrozen).AsBool();
		if (_idleFrozen && !flag)
		{
			RestoreIdleSpeed();
		}
		SetAlive(data.GetValueOrDefault("alive", base.Alive).AsBool());
		RefreshConfiguration();
		if (flag && !_idleFrozen)
		{
			IdleEntered();
		}
	}

	private void ClearSyncPayload()
	{
		_syncPayload.Clear();
		_syncPayloadIdleFrozen = false;
		_syncPayloadShaderApplied = false;
		_syncPayloadAlive = false;
		_syncPayloadInitialized = false;
	}

	private static bool IsIzmMode()
	{
		if (Global.IsEditor && SceneManager.CurrentScene == "LevelEditorStage")
		{
			if (GodotObject.IsInstanceValid(LevelEditorInformationEditor.Instance) && GodotObject.IsInstanceValid(LevelEditorInformationEditor.Instance.levelConfig))
			{
				return LevelEditorInformationEditor.Instance.levelConfig.finishMethod == TowerDefenseEnum.LEVEL_FINISH_METHOD.IZM;
			}
			return false;
		}
		return TowerDefenseManager.GetGameMethod() == TowerDefenseEnum.LEVEL_FINISH_METHOD.IZM;
	}
}
