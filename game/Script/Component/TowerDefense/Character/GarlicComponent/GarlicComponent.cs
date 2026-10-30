using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public sealed class GarlicComponent : CharacterComponentRuntime
{
	public Texture2D grossoutTexture;

	public float reactionDelay = 0.5f;

	public float grossoutDuration = 0.5f;

	public float changeLineDuration = 1f;

	public float moveDownChance = 0.5f;

	public string reactionAudio = "Yuck";

	public StringName garlicStateEvent = "ToGarlic";

	public Tween.EaseType changeLineEase = Tween.EaseType.InOut;

	public Tween.TransitionType changeLineTransition;

	public TowerDefenseZombie parent;

	public int _syncMoveDir;

	public bool _syncDeserializing;

	private static readonly StringName SyncAliveKey = new StringName("_alive");

	private bool _syncPayloadInitialized;

	private int _syncPayloadMoveDir;

	private bool _syncPayloadIsChangeLine;

	private Tween _changeLineTween;

	private TaskCompletionSource<bool> _changeLineCompletion;

	private Action _changeLineFinishedHandler;

	private ulong _changeLineVersion;

	private ulong _garlicVersion;

	private bool _garlicRunning;

	private Texture2D _savedReplacement;

	private string _savedAtlasReplacementPath = string.Empty;

	private bool[] _savedFilterStates = System.Array.Empty<bool>();

	private bool _grossoutApplied;

	private bool _configured;

	private Dictionary _syncPayload => GetReusableSyncPayload();

	private GarlicComponentDefinition Definition => base.ComponentDefinition as GarlicComponentDefinition;

	protected override void OnBound()
	{
		parent = base.Owner as TowerDefenseZombie;
		ApplyDefinition();
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		CancelActiveWork();
		ClearSyncPayload();
		parent = null;
		if (reason != ComponentDetachReason.TemporaryTreeExit)
		{
			ResetPersistentState();
		}
	}

	protected override void OnReleased()
	{
		CancelActiveWork();
		ClearSyncPayload();
		parent = null;
		ResetPersistentState();
		grossoutTexture = null;
		_configured = false;
	}

	private void ApplyDefinition()
	{
		if (!_configured && Definition != null)
		{
			grossoutTexture = Definition.grossoutTexture;
			reactionDelay = Definition.reactionDelay;
			grossoutDuration = Definition.grossoutDuration;
			changeLineDuration = Definition.changeLineDuration;
			moveDownChance = Definition.moveDownChance;
			reactionAudio = Definition.reactionAudio;
			garlicStateEvent = Definition.garlicStateEvent;
			changeLineEase = Definition.changeLineEase;
			changeLineTransition = Definition.changeLineTransition;
			_configured = true;
		}
	}

	private void CancelActiveWork()
	{
		_garlicVersion++;
		RestoreGrossoutVisual();
		CancelChangeLine();
		if (GodotObject.IsInstanceValid(parent))
		{
			parent.isGarlic = false;
		}
		_garlicRunning = false;
	}

	private void ResetPersistentState()
	{
		_syncMoveDir = 0;
		_syncDeserializing = false;
	}

	public void Garlic()
	{
		if (TryGetParent(out var zombie) && !zombie.isPause && !_garlicRunning && !zombie.isGarlic)
		{
			if (zombie.nearDie || zombie.die)
			{
				zombie.Die();
			}
			else if ((zombie.instance.unUseBuffFlags & 0x80) == 0)
			{
				_garlicRunning = true;
				GarlicAsync(++_garlicVersion);
			}
		}
	}

	private async Task GarlicAsync(ulong version)
	{
		_ = 3;
		try
		{
			TowerDefenseZombie zombie;
			while (TryGetParent(out zombie) && zombie.isRise)
			{
				SceneTree tree = zombie.GetTree();
				if (!GodotObject.IsInstanceValid(tree))
				{
					return;
				}
				await zombie.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
				if (version != _garlicVersion)
				{
					return;
				}
			}
			if (!CanContinueGarlic(version, out zombie))
			{
				return;
			}
			zombie.isGarlic = true;
			if ((zombie.StateMachine?.IsInitialized ?? false) && !garlicStateEvent.IsEmpty)
			{
				zombie.SendStateEvent(garlicStateEvent);
			}
			if (await WaitDelayAsync(version, reactionDelay) && CanContinueGarlic(version, out zombie))
			{
				if (!string.IsNullOrEmpty(reactionAudio) && GodotObject.IsInstanceValid(AudioManager.Instance))
				{
					AudioManager.Instance.AudioPlay(reactionAudio);
				}
				ApplyGrossoutVisual(zombie);
				if (await WaitDelayAsync(version, grossoutDuration) && CanContinueGarlic(version, out zombie))
				{
					RestoreGrossoutVisual();
					zombie.Walk();
					await ChangeLine();
				}
			}
		}
		finally
		{
			RestoreGrossoutVisual();
			if (version == _garlicVersion)
			{
				if (GodotObject.IsInstanceValid(parent))
				{
					parent.isGarlic = false;
				}
				_garlicRunning = false;
			}
		}
	}

	private bool CanContinueGarlic(ulong version, out TowerDefenseZombie zombie)
	{
		zombie = null;
		if (version == _garlicVersion && TryGetParent(out zombie) && !zombie.nearDie)
		{
			return !zombie.die;
		}
		return false;
	}

	private async Task<bool> WaitDelayAsync(ulong version, float seconds)
	{
		if (seconds <= 0f)
		{
			return version == _garlicVersion;
		}
		if (!TryGetParent(out var zombie))
		{
			return false;
		}
		SceneTree tree = zombie.GetTree();
		if (!GodotObject.IsInstanceValid(tree))
		{
			return false;
		}
		SceneTreeTimer source = tree.CreateTimer(seconds, processAlways: false);
		await zombie.ToSignal(source, SceneTreeTimer.SignalName.Timeout);
		TowerDefenseZombie zombie2;
		return version == _garlicVersion && TryGetParent(out zombie2);
	}

	private void ApplyGrossoutVisual(TowerDefenseZombie zombie)
	{
		if (!_grossoutApplied && GodotObject.IsInstanceValid(zombie.sprite))
		{
			int num = zombie.garlicFliters?.Count ?? 0;
			_savedFilterStates = new bool[num];
			for (int i = 0; i < num; i++)
			{
				_savedFilterStates[i] = zombie.sprite.GetFliter(zombie.garlicFliters[i]);
			}
			_savedReplacement = (string.IsNullOrEmpty(zombie.garlicReplace) ? null : zombie.sprite.GetReplace(zombie.garlicReplace));
			_savedAtlasReplacementPath = (string.IsNullOrEmpty(zombie.garlicReplace) ? string.Empty : zombie.sprite.GetAtlasReplacePath(zombie.garlicReplace));
			if (!string.IsNullOrEmpty(zombie.garlicReplace) && GodotObject.IsInstanceValid(grossoutTexture))
			{
				zombie.sprite.SetReplace(zombie.garlicReplace, grossoutTexture);
			}
			if (num > 0)
			{
				zombie.sprite.SetFliters((Godot.Collections.Array?)zombie.garlicFliters, open: false);
			}
			_grossoutApplied = true;
		}
	}

	private void RestoreGrossoutVisual()
	{
		if (!_grossoutApplied)
		{
			return;
		}
		_grossoutApplied = false;
		TowerDefenseZombie towerDefenseZombie = parent;
		if (!GodotObject.IsInstanceValid(towerDefenseZombie?.sprite))
		{
			_savedReplacement = null;
			_savedAtlasReplacementPath = string.Empty;
			_savedFilterStates = System.Array.Empty<bool>();
			return;
		}
		if (!string.IsNullOrEmpty(towerDefenseZombie.garlicReplace))
		{
			if (!string.IsNullOrEmpty(_savedAtlasReplacementPath))
			{
				towerDefenseZombie.sprite.SetAtlasReplace(towerDefenseZombie.garlicReplace, _savedAtlasReplacementPath);
			}
			else
			{
				towerDefenseZombie.sprite.SetReplace(towerDefenseZombie.garlicReplace, _savedReplacement);
			}
		}
		int num = Mathf.Min(_savedFilterStates.Length, towerDefenseZombie.garlicFliters?.Count ?? 0);
		for (int i = 0; i < num; i++)
		{
			if (_savedFilterStates[i])
			{
				towerDefenseZombie.sprite.SetFliter(towerDefenseZombie.garlicFliters[i], open: true);
			}
		}
		_savedReplacement = null;
		_savedAtlasReplacementPath = string.Empty;
		_savedFilterStates = System.Array.Empty<bool>();
	}

	public async Task ChangeLine()
	{
		if (!TryGetParent(out var zombie) || zombie.isChangeLine || !GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			return;
		}
		int num = ResolveMoveDirection(zombie);
		if (num == 0)
		{
			return;
		}
		ulong version = ++_changeLineVersion;
		zombie.isChangeLine = true;
		try
		{
			float num2 = TowerDefenseManager.Instance.GetMapGridSize().Y * (float)num;
			float num3 = Mathf.Max(0.001f, changeLineDuration);
			_changeLineTween = zombie.CreateTween();
			_changeLineTween.SetParallel();
			_changeLineTween.SetEase(changeLineEase);
			_changeLineTween.SetTrans(changeLineTransition);
			_changeLineCompletion = new TaskCompletionSource<bool>();
			_changeLineFinishedHandler = delegate
			{
				_changeLineCompletion?.TrySetResult(result: true);
			};
			_changeLineTween.Finished += _changeLineFinishedHandler;
			float y = zombie.GetLogicalGlobalPosition().Y;
			_changeLineTween.TweenMethod(Callable.From(delegate(float value)
			{
				SetLogicalGlobalPositionY(zombie, value);
			}), y, y + num2, num3);
			ShadowComponent shadowComponent = zombie.shadowComponent;
			if (shadowComponent != null && !shadowComponent.IsReleased)
			{
				zombie.shadowComponent.TweenSaveShadowPositionY(_changeLineTween, zombie.shadowComponent.saveShadowPosition.Y + num2, num3);
			}
			TowerDefenseCharacter dynamicCharacter = GetDynamicCharacter(zombie, "carryCharacter");
			AddCompanionTween(dynamicCharacter, num2, num, num3);
			TowerDefenseCharacter ghostCharacter = zombie.ghostCharacter;
			if (ghostCharacter != dynamicCharacter)
			{
				AddCompanionTween(ghostCharacter, num2, num, num3);
			}
			RefreshChangeLineDiscard(zombie);
			zombie.gridPos = new Vector2I(zombie.gridPos.X, zombie.gridPos.Y + num);
			if (await _changeLineCompletion.Task && version == _changeLineVersion && TryGetParent(out zombie))
			{
				CleanupChangeLineTween(killTween: false);
				RefreshChangeLineDiscard(zombie);
			}
		}
		finally
		{
			if (version == _changeLineVersion && GodotObject.IsInstanceValid(parent))
			{
				parent.isChangeLine = false;
			}
		}
	}

	private int ResolveMoveDirection(TowerDefenseZombie zombie)
	{
		Vector2 vector = TowerDefenseManager.Instance.GetMapGridNum();
		int num = ((zombie.gridPos.Y <= 1) ? 1 : ((zombie.gridPos.Y >= (int)vector.Y) ? (-1) : ((!_syncDeserializing || _syncMoveDir == 0) ? ((GD.Randf() < Mathf.Clamp(moveDownChance, 0f, 1f)) ? 1 : (-1)) : _syncMoveDir)));
		_syncDeserializing = false;
		_syncMoveDir = num;
		return num;
	}

	private void AddCompanionTween(TowerDefenseCharacter companion, float moveY, int moveDirection, float duration)
	{
		if (GodotObject.IsInstanceValid(companion) && GodotObject.IsInstanceValid(_changeLineTween))
		{
			float y = companion.GetLogicalGlobalPosition().Y;
			_changeLineTween.TweenMethod(Callable.From(delegate(float value)
			{
				SetLogicalGlobalPositionY(companion, value);
			}), y, y + moveY, duration);
			companion.gridPos = new Vector2I(companion.gridPos.X, companion.gridPos.Y + moveDirection);
		}
	}

	private static void SetLogicalGlobalPositionY(TowerDefenseCharacter character, float value)
	{
		if (GodotObject.IsInstanceValid(character))
		{
			Vector2 logicalGlobalPosition = character.GetLogicalGlobalPosition();
			logicalGlobalPosition.Y = value;
			character.SetLogicalGlobalPosition(logicalGlobalPosition);
			if (character is TowerDefenseZombie zombie)
			{
				RefreshChangeLineDiscard(zombie);
			}
		}
	}

	private static TowerDefenseCharacter GetDynamicCharacter(TowerDefenseZombie zombie, StringName property)
	{
		Variant variant = zombie.Get(property);
		if (variant.VariantType != Variant.Type.Object)
		{
			return null;
		}
		return variant.AsGodotObject() as TowerDefenseCharacter;
	}

	private static void RefreshChangeLineDiscard(TowerDefenseZombie zombie)
	{
		if (zombie.inWater)
		{
			zombie.InWaterDiscardSet();
		}
		else
		{
			zombie.OutWaterDiscardSet();
		}
	}

	public void CancelChangeLine()
	{
		_changeLineVersion++;
		_changeLineCompletion?.TrySetResult(result: false);
		CleanupChangeLineTween(killTween: true);
		if (GodotObject.IsInstanceValid(parent))
		{
			RefreshChangeLineDiscard(parent);
			parent.isChangeLine = false;
		}
	}

	private void CleanupChangeLineTween(bool killTween)
	{
		if (GodotObject.IsInstanceValid(_changeLineTween) && _changeLineFinishedHandler != null)
		{
			_changeLineTween.Finished -= _changeLineFinishedHandler;
		}
		if (killTween && GodotObject.IsInstanceValid(_changeLineTween))
		{
			_changeLineTween.Kill();
		}
		_changeLineTween = null;
		_changeLineFinishedHandler = null;
		_changeLineCompletion = null;
	}

	public override Dictionary SyncSerialize()
	{
		bool flag = GodotObject.IsInstanceValid(parent) && parent.isChangeLine;
		if (_syncPayloadInitialized)
		{
			if (_syncPayload.Count == 3)
			{
				_syncPayload.Remove(SyncAliveKey);
			}
			if (_syncPayload.Count == 2 && _syncPayloadMoveDir == _syncMoveDir && _syncPayloadIsChangeLine == flag)
			{
				return _syncPayload;
			}
		}
		ClearReusableSyncPayload();
		_syncPayload["move_dir"] = _syncMoveDir;
		_syncPayload["is_change_line"] = flag;
		_syncPayloadMoveDir = _syncMoveDir;
		_syncPayloadIsChangeLine = flag;
		_syncPayloadInitialized = true;
		return _syncPayload;
	}

	private void ClearSyncPayload()
	{
		ClearReusableSyncPayload();
		_syncPayloadInitialized = false;
		_syncPayloadMoveDir = 0;
		_syncPayloadIsChangeLine = false;
	}

	public override void SyncDeserialize(Dictionary data)
	{
		_syncMoveDir = data.GetValueOrDefault("move_dir", 0).AsInt32();
		_syncDeserializing = _syncMoveDir != 0;
		if (data.ContainsKey("is_change_line") && GodotObject.IsInstanceValid(parent))
		{
			parent.isChangeLine = data.GetValueOrDefault("is_change_line", parent.isChangeLine).AsBool();
		}
	}

	private bool TryGetParent(out TowerDefenseZombie zombie)
	{
		zombie = parent;
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(zombie) && GodotObject.IsInstanceValid(zombie.instance))
		{
			return GodotObject.IsInstanceValid(zombie.sprite);
		}
		return false;
	}
}
