using System.Threading.Tasks;
using Godot;

public sealed class DestroyComponent : CharacterComponentRuntime
{
	private enum SpecialDestroyType
	{
		Ash,
		Smash
	}

	public const float DESTROY_DELAY = 1f;

	public const float SMASH_SCALE_Y = 0.25f;

	public float destroyDelay = 1f;

	public float smashScaleY = 0.25f;

	public StringName ashShaderParameter = "ash";

	public bool waitForPhysicsFrame = true;

	public bool pauseSpriteOnSpecialDestroy = true;

	public bool hideShadowOnSmash = true;

	public TowerDefenseCharacter parent;

	public bool isRemoteDestroy;

	private const double DeathSettlementWatchdogIntervalSeconds = 2.0;

	private const int DeathSettlementWatchdogMaxTicks = 10;

	private ulong _destroyVersion;

	private bool _configured;

	private TowerDefenseControlNew _deathSettlementControl;

	private int _deathSettlementId = -1;

	private SceneTree _deathSettlementWatchdogTree;

	private int _deathSettlementWatchdogTicks;

	private int _deathSettlementWatchdogVersion;

	private DestroyComponentDefinition Definition => base.ComponentDefinition as DestroyComponentDefinition;

	public void BeginDeathSettlement()
	{
		if (IsEnteringDeath())
		{
			ReserveSettlement();
		}
	}

	private bool IsEnteringDeath()
	{
		if (!GodotObject.IsInstanceValid(parent))
		{
			return false;
		}
		if (!parent.die && !parent.isDestroy)
		{
			if (GodotObject.IsInstanceValid(parent.instance))
			{
				return parent.instance.die;
			}
			return false;
		}
		return true;
	}

	private void ReserveSettlement()
	{
		if (_deathSettlementId < 0 && GodotObject.IsInstanceValid(parent) && parent.inGame && !parent.editorPreviewMode && !Engine.IsEditorHint() && TowerDefenseManager.HasGameplayAuthority && !parent.skipDestroySet && (!parent.suppressDeathrattles || parent.PreserveDeathTransformation))
		{
			TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
			if (GodotObject.IsInstanceValid(currentControl))
			{
				_deathSettlementControl = currentControl;
				_deathSettlementId = currentControl.BeginPendingBattleOperation();
				StartDeathSettlementWatchdog();
			}
		}
	}

	public void EndDeathSettlement()
	{
		TowerDefenseControlNew deathSettlementControl = _deathSettlementControl;
		int deathSettlementId = _deathSettlementId;
		_deathSettlementControl = null;
		_deathSettlementId = -1;
		StopDeathSettlementWatchdog();
		CompleteDeathSettlement(deathSettlementControl, deathSettlementId);
	}

	private int TakeDeathSettlement(out TowerDefenseControlNew control)
	{
		ReserveSettlement();
		control = _deathSettlementControl;
		int deathSettlementId = _deathSettlementId;
		_deathSettlementControl = null;
		_deathSettlementId = -1;
		StopDeathSettlementWatchdog();
		return deathSettlementId;
	}

	private void StartDeathSettlementWatchdog()
	{
		if (_deathSettlementId >= 0 && GodotObject.IsInstanceValid(parent))
		{
			SceneTree tree = parent.GetTree();
			if (GodotObject.IsInstanceValid(tree))
			{
				_deathSettlementWatchdogTree = tree;
				_deathSettlementWatchdogTicks = 0;
				ScheduleDeathSettlementWatchdogTick(++_deathSettlementWatchdogVersion);
			}
		}
	}

	private void ScheduleDeathSettlementWatchdogTick(int version)
	{
		SceneTree deathSettlementWatchdogTree = _deathSettlementWatchdogTree;
		if (version == _deathSettlementWatchdogVersion && _deathSettlementId >= 0 && GodotObject.IsInstanceValid(deathSettlementWatchdogTree))
		{
			deathSettlementWatchdogTree.CreateTimer(2.0, processAlways: false).Timeout += delegate
			{
				OnDeathSettlementWatchdogTick(version);
			};
		}
	}

	private void OnDeathSettlementWatchdogTick(int version)
	{
		if (version == _deathSettlementWatchdogVersion && !base.IsReleased && _deathSettlementId >= 0)
		{
			if (++_deathSettlementWatchdogTicks < 10)
			{
				ScheduleDeathSettlementWatchdogTick(version);
				return;
			}
			GodotObject godotObject = (GodotObject.IsInstanceValid(parent) ? parent : null);
			GD.Print($"[DestroyComponent] death settlement watchdog released owner={godotObject?.GetType().Name ?? "null"} id={_deathSettlementId}");
			EndDeathSettlement();
		}
	}

	private void StopDeathSettlementWatchdog()
	{
		_deathSettlementWatchdogTicks = 0;
		_deathSettlementWatchdogTree = null;
		_deathSettlementWatchdogVersion++;
	}

	private static void CompleteDeathSettlement(TowerDefenseControlNew control, int operationId)
	{
		if (operationId < 0)
		{
			return;
		}
		Callable.From(delegate
		{
			if (GodotObject.IsInstanceValid(control))
			{
				control.CompletePendingBattleOperation(operationId);
			}
		}).CallDeferred();
	}

	protected override void OnBound()
	{
		parent = base.Owner;
		if (parent.die || parent.nearDie)
		{
			BeginDeathSettlement();
		}
		if (!_configured)
		{
			DestroyComponentDefinition definition = Definition;
			destroyDelay = definition?.destroyDelay ?? 1f;
			smashScaleY = definition?.smashScaleY ?? 0.25f;
			ashShaderParameter = definition?.ashShaderParameter ?? new StringName("ash");
			waitForPhysicsFrame = definition?.waitForPhysicsFrame ?? true;
			pauseSpriteOnSpecialDestroy = definition?.pauseSpriteOnSpecialDestroy ?? true;
			hideShadowOnSmash = definition?.hideShadowOnSmash ?? true;
			_configured = true;
		}
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		CancelPendingDestroy();
		parent = null;
	}

	protected override void OnReleased()
	{
		CancelPendingDestroy();
		parent = null;
	}

	protected override void OnAliveChanged(bool alive)
	{
		if (!alive)
		{
			CancelPendingDestroy();
		}
	}

	private void CancelPendingDestroy()
	{
		EndDeathSettlement();
		_destroyVersion++;
		isRemoteDestroy = false;
	}

	public void ShovelDestroy()
	{
		if (TryGetParent(out var character))
		{
			character.isShovel = true;
			Destroy();
		}
	}

	public void Destroy(bool freeInstance = true)
	{
		if (TryGetParent(out var _))
		{
			DestroyAsync(freeInstance, 0.0);
		}
	}

	public void DestroyWithVisualDelay(double delaySeconds)
	{
		if (TryGetParent(out var _))
		{
			DestroyAsync(freeInstance: true, Mathf.Max(0.0, delaySeconds));
		}
	}

	private async Task DestroyAsync(bool freeInstance, double visualReleaseDelaySeconds)
	{
		if (!TryGetParent(out var character) || character.isDestroy || !HasDestroyAuthority(character))
		{
			return;
		}
		TowerDefenseControlNew settlementControl;
		int settlementId = TakeDeathSettlement(out settlementControl);
		try
		{
			ulong version = ++_destroyVersion;
			SceneTree tree = GetParentTreeOrNull(character);
			NotifyOwnerBeforeDestroy(character);
			MarkDestroyState(character);
			HitBoxDestroy();
			if (Global.IsEditor && SceneManager.CurrentScene == "LevelEditorStage")
			{
				character.EmitDestroy();
				UnregisterCharacter(character);
				QueueFreeParent(version);
				return;
			}
			float num = (GodotObject.IsInstanceValid(character.transformPoint) ? character.transformPoint.Scale.X : 1f);
			float num2 = (GodotObject.IsInstanceValid(character.instance) ? ((float)character.instance.hitpointScale) : 1f);
			character.EmitDestroy();
			if (GodotObject.IsInstanceValid(BattleEventBus.Instance))
			{
				BattleEventBus.Instance.EmitCharacterDestroy(character.packet, character.GetLogicalGlobalPosition(), character.gridPos, character.camp, num, num2);
			}
			if (freeInstance && !character.suppressDeathrattles)
			{
				ExecuteDeathEvents(character);
			}
			if (character.isExplode && (character.config == null || character.config.ashScene == null))
			{
				await SpecialDestroyAsync(SpecialDestroyType.Ash, tree, version, emitDestroy: false);
				return;
			}
			if (character.isSmash)
			{
				await SpecialDestroyAsync(SpecialDestroyType.Smash, tree, version, emitDestroy: false);
				return;
			}
			if (freeInstance)
			{
				UnregisterCharacter(character);
			}
			if (await ApplyDestroySetAsync(tree, version) && await WaitVisualReleaseDelayAsync(tree, version, visualReleaseDelaySeconds) && freeInstance)
			{
				QueueFreeParent(version);
			}
		}
		finally
		{
			CompleteDeathSettlement(settlementControl, settlementId);
		}
	}

	public void AshDestroy()
	{
		if (TryGetParent(out var _))
		{
			SpecialDestroyEntryAsync(SpecialDestroyType.Ash);
		}
	}

	public void SmashDestroy()
	{
		if (TryGetParent(out var _))
		{
			SpecialDestroyEntryAsync(SpecialDestroyType.Smash);
		}
	}

	private async Task SpecialDestroyEntryAsync(SpecialDestroyType type)
	{
		if (!TryGetParent(out var character) || character.isDestroy)
		{
			return;
		}
		TowerDefenseControlNew settlementControl;
		int settlementId = TakeDeathSettlement(out settlementControl);
		try
		{
			ulong version = ++_destroyVersion;
			SceneTree parentTreeOrNull = GetParentTreeOrNull(character);
			NotifyOwnerBeforeDestroy(character);
			MarkDestroyState(character);
			HitBoxDestroy();
			await SpecialDestroyAsync(type, parentTreeOrNull, version, emitDestroy: true);
		}
		finally
		{
			CompleteDeathSettlement(settlementControl, settlementId);
		}
	}

	private async Task SpecialDestroyAsync(SpecialDestroyType type, SceneTree tree, ulong version, bool emitDestroy)
	{
		if (!IsCurrent(version, out var character))
		{
			return;
		}
		if (emitDestroy)
		{
			character.EmitDestroy();
		}
		UnregisterCharacter(character);
		if (!(await ApplyDestroySetAsync(tree, version)) || !IsCurrent(version, out character))
		{
			return;
		}
		if (character.inWater || (type == SpecialDestroyType.Smash && IsVase(character)))
		{
			QueueFreeParent(version);
			return;
		}
		if (type == SpecialDestroyType.Ash)
		{
			ApplyAshVisual(character);
		}
		else
		{
			ApplySmashVisual(character);
		}
		if (await WaitDestroyDelayAsync(tree, version))
		{
			QueueFreeParent(version);
		}
	}

	private async Task<bool> ApplyDestroySetAsync(SceneTree tree, ulong version)
	{
		if (!IsCurrent(version, out var character))
		{
			return false;
		}
		if ((GodotObject.IsInstanceValid(character.instance) && character.instance.hologram) || character.skipDestroySet || (character.suppressDeathrattles && !character.PreserveDeathTransformation) || !GodotObject.IsInstanceValid(tree))
		{
			return true;
		}
		if (FallingObjectReplicationPolicy.ShouldSuppressRemoteDestroySet(character))
		{
			return true;
		}
		using (FallingObjectReplicationPolicy.BeginCapture(character))
		{
			character.DestroySet();
		}
		TowerDefenseCharacter character2;
		if (!waitForPhysicsFrame)
		{
			return IsCurrent(version, out character2);
		}
		await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
		return IsCurrent(version, out character2);
	}

	private async Task<bool> WaitDestroyDelayAsync(SceneTree tree, ulong version)
	{
		float num = Mathf.Max(0f, destroyDelay);
		TowerDefenseCharacter character;
		if (num <= 0f || !GodotObject.IsInstanceValid(tree))
		{
			return IsCurrent(version, out character);
		}
		SceneTreeTimer source = tree.CreateTimer(num, processAlways: false);
		await tree.ToSignal(source, SceneTreeTimer.SignalName.Timeout);
		return IsCurrent(version, out character);
	}

	private async Task<bool> WaitVisualReleaseDelayAsync(SceneTree tree, ulong version, double delaySeconds)
	{
		TowerDefenseCharacter character;
		if (delaySeconds <= 0.0 || !GodotObject.IsInstanceValid(tree))
		{
			return IsCurrent(version, out character);
		}
		SceneTreeTimer source = tree.CreateTimer(delaySeconds, processAlways: false);
		await tree.ToSignal(source, SceneTreeTimer.SignalName.Timeout);
		return IsCurrent(version, out character);
	}

	private void ApplyAshVisual(TowerDefenseCharacter character)
	{
		if (!ashShaderParameter.IsEmpty)
		{
			character.SetSpriteGroupShaderParameter(ashShaderParameter.ToString(), true);
		}
		if (pauseSpriteOnSpecialDestroy && GodotObject.IsInstanceValid(character.sprite))
		{
			character.sprite.pause = true;
		}
	}

	private void ApplySmashVisual(TowerDefenseCharacter character)
	{
		if (pauseSpriteOnSpecialDestroy && GodotObject.IsInstanceValid(character.sprite))
		{
			character.sprite.pause = true;
		}
		if (hideShadowOnSmash && GodotObject.IsInstanceValid(character.shadowSprite))
		{
			character.shadowSprite.Visible = false;
		}
		if (GodotObject.IsInstanceValid(character.transformPoint))
		{
			character.transformPoint.Scale = new Vector2(character.transformPoint.Scale.X, Mathf.Max(0.01f, smashScaleY));
		}
	}

	private static bool IsVase(TowerDefenseCharacter character)
	{
		if (GodotObject.IsInstanceValid(character.instance))
		{
			return (character.instance.physiqueTypeFlags & 0x20) != 0;
		}
		return false;
	}

	private static void ExecuteDeathEvents(TowerDefenseCharacter character)
	{
		int num = character.dieEvent?.Count ?? 0;
		for (int i = 0; i < num; i++)
		{
			TowerDefenseCharacterEventBase towerDefenseCharacterEventBase = character.dieEvent[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacterEventBase))
			{
				towerDefenseCharacterEventBase.Execute(character.GetLogicalGlobalPosition(), character);
			}
		}
	}

	private static void MarkDestroyState(TowerDefenseCharacter character)
	{
		character.isDestroy = true;
		character.die = true;
		if (GodotObject.IsInstanceValid(character.instance))
		{
			character.instance.die = true;
		}
	}

	private static void NotifyOwnerBeforeDestroy(TowerDefenseCharacter character)
	{
		if (GodotObject.IsInstanceValid(character?.componentManager))
		{
			character.componentManager.NotifyOwnerBeforeDestroy();
		}
	}

	private static void UnregisterCharacter(TowerDefenseCharacter character)
	{
		if (GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			TowerDefenseManager.Instance.CharacterUnregister(character);
		}
		character.RemoveFromGroup("Character");
	}

	private bool HasDestroyAuthority(TowerDefenseCharacter character)
	{
		if (!Global.IsMultiplayerMode || MultiPlayerManager.IsHost || isRemoteDestroy)
		{
			return true;
		}
		if (character is TowerDefensePlant)
		{
			return false;
		}
		if (character is TowerDefenseZombie towerDefenseZombie)
		{
			return towerDefenseZombie.syncId < 0;
		}
		return true;
	}

	private static SceneTree GetParentTreeOrNull(TowerDefenseCharacter character)
	{
		if (!GodotObject.IsInstanceValid(character) || !character.IsInsideTree())
		{
			return null;
		}
		SceneTree tree = character.GetTree();
		if (!GodotObject.IsInstanceValid(tree))
		{
			return null;
		}
		return tree;
	}

	private void QueueFreeParent(ulong version)
	{
		if (IsCurrent(version, out var character) && !character.IsQueuedForDeletion())
		{
			character.QueueFree();
		}
	}

	public void HitBoxDestroy()
	{
		if (TryGetParent(out var character))
		{
			character.DestroyHitBoxRuntime();
			TargetRegistrationComponent targetRegistrationComponent = character.targetRegistrationComponent;
			if (targetRegistrationComponent != null && !targetRegistrationComponent.IsReleased)
			{
				character.targetRegistrationComponent.UnregisterTarget();
				character.targetRegistrationComponent.canProjectileCheck = false;
			}
		}
	}

	private bool IsCurrent(ulong version, out TowerDefenseCharacter character)
	{
		character = null;
		if (version == _destroyVersion)
		{
			return TryGetParent(out character);
		}
		return false;
	}

	private bool TryGetParent(out TowerDefenseCharacter character)
	{
		character = parent;
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active)
		{
			return GodotObject.IsInstanceValid(character);
		}
		return false;
	}
}
