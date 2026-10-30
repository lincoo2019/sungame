using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public sealed class ScaredComponent : CharacterComponentRuntime
{
	public delegate void ScaredDownEventHandler();

	public delegate void ScaredRiseEventHandler();

	private struct ScareTargetPredicate : ITowerDefenseCharacterRectPredicate, ITowerDefenseCharacterCandidatePredicate
	{
		public TowerDefenseCharacter Parent;

		public bool CanConsider(TowerDefenseCharacter candidate)
		{
			if (!GodotObject.IsInstanceValid(candidate) || candidate == Parent || !GodotObject.IsInstanceValid(candidate.instance) || candidate.die || candidate.nearDie || candidate.isDestroy || candidate.instance.invincible || !candidate.instance.canBeCollection || !candidate.HasHitBox || !Parent.CanTarget(candidate))
			{
				return false;
			}
			if (candidate is TowerDefenseCrater || candidate is TowerDefenseGravestone)
			{
				return false;
			}
			if (candidate is TowerDefenseItem towerDefenseItem)
			{
				return towerDefenseItem.canCheck;
			}
			return true;
		}

		public bool Matches(TowerDefenseCharacter candidate)
		{
			return true;
		}
	}

	public bool hostAuthoritative = true;

	public TowerDefenseEnum.CHARACTER_HEIGHT scaredHeight = TowerDefenseEnum.CHARACTER_HEIGHT.LOW;

	public StringName scaredDownStateEvent = "ToScaredDown";

	public StringName scaredGrowStateEvent = "ToScaredGrow";

	public StringName scaredIdleStateEvent = "ToScaredIdle";

	public StringName idleStateEvent = "ToIdle";

	public AdobeAnimateSprite sprite;

	public string scaredDownAnimeClip = "Scared";

	public float scaredDownAnimeTimeScale = 1f;

	public string scaredIdleAnimeClip = "ScaredIdle";

	public float scaredIdleAnimeTimeScale = 1f;

	public string scaredGrowAnimeClip = "Grow";

	public float scaredGrowAnimeTimeScale = 1f;

	public TowerDefenseCharacter parent;

	public TowerDefenseEnum.CHARACTER_HEIGHT height = TowerDefenseEnum.CHARACTER_HEIGHT.NORMAL;

	private StateHandle _idleState;

	private StateHandle _scaredDownState;

	private StateHandle _scaredIdleState;

	private StateHandle _scaredGrowState;

	private bool _stateSignalsConnected;

	private bool _spriteSignalConnected;

	private bool _scaredApplied;

	private bool _configured;

	private string _pendingSyncedState;

	private readonly List<ComponentBase> _controlledNodeComponents = new List<ComponentBase>();

	private readonly List<CharacterComponentRuntime> _controlledRuntimeComponents = new List<CharacterComponentRuntime>();

	private readonly System.Collections.Generic.Dictionary<string, bool> _controlledAliveBeforeScare = new System.Collections.Generic.Dictionary<string, bool>(StringComparer.Ordinal);

	private readonly System.Collections.Generic.Dictionary<string, bool> _pendingControlledAliveRestore = new System.Collections.Generic.Dictionary<string, bool>(StringComparer.Ordinal);

	private readonly Dictionary _syncPayload = new Dictionary();

	public bool alive
	{
		get
		{
			return base.Alive;
		}
		set
		{
			SetAlive(value);
		}
	}

	private ScaredComponentDefinition Definition => base.ComponentDefinition as ScaredComponentDefinition;

	public event ScaredDownEventHandler OnScaredDown;

	public event ScaredRiseEventHandler OnScaredRise;

	protected override void OnBound()
	{
		parent = base.Owner;
		ApplyDefinitionOnce();
		ResolveReferences();
		ConnectSpriteSignal();
	}

	protected override void OnActivated()
	{
		Callable.From(ReattachAfterTreeEntry).CallDeferred();
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		CaptureCurrentStateForReattach();
		DisconnectStateSignals();
		DisconnectSpriteSignal();
		SetScared(scared: false);
		ClearResolvedReferences();
		parent = null;
	}

	protected override void OnReleased()
	{
		DisconnectStateSignals();
		DisconnectSpriteSignal();
		SetScared(scared: false);
		this.OnScaredDown = null;
		this.OnScaredRise = null;
		_controlledAliveBeforeScare.Clear();
		_pendingControlledAliveRestore.Clear();
		_syncPayload.Clear();
		_pendingSyncedState = null;
		_scaredApplied = false;
		ClearResolvedReferences();
		parent = null;
	}

	protected override void OnAliveChanged(bool alive)
	{
		if (!alive)
		{
			SetScared(scared: false);
			SendStateEvent(idleStateEvent, allowWhenInactive: true);
		}
		else
		{
			ResolveControlledComponents();
			ApplyPendingSyncedState();
		}
	}

	private void ApplyDefinitionOnce()
	{
		if (!_configured && Definition != null)
		{
			ScaredComponentDefinition definition = Definition;
			hostAuthoritative = definition.hostAuthoritative;
			scaredHeight = definition.scaredHeight;
			scaredDownStateEvent = definition.scaredDownStateEvent;
			scaredGrowStateEvent = definition.scaredGrowStateEvent;
			scaredIdleStateEvent = definition.scaredIdleStateEvent;
			idleStateEvent = definition.idleStateEvent;
			scaredDownAnimeClip = definition.scaredDownAnimeClip;
			scaredDownAnimeTimeScale = definition.scaredDownAnimeTimeScale;
			scaredIdleAnimeClip = definition.scaredIdleAnimeClip;
			scaredIdleAnimeTimeScale = definition.scaredIdleAnimeTimeScale;
			scaredGrowAnimeClip = definition.scaredGrowAnimeClip;
			scaredGrowAnimeTimeScale = definition.scaredGrowAnimeTimeScale;
			_configured = true;
		}
	}

	private void ResolveReferences()
	{
		if (GodotObject.IsInstanceValid(parent) && Definition != null)
		{
			sprite = ResolveOwnerNode<AdobeAnimateSprite>(Definition.spritePath);
			ResolveControlledComponents();
		}
	}

	private T ResolveOwnerNode<T>(NodePath path) where T : Node
	{
		if (!(path == null) && !path.IsEmpty && GodotObject.IsInstanceValid(parent))
		{
			return parent.GetNodeOrNull<T>(path);
		}
		return null;
	}

	private void ResolveControlledComponents()
	{
		_controlledNodeComponents.Clear();
		_controlledRuntimeComponents.Clear();
		ScaredComponentDefinition definition = Definition;
		ComponentManager manager = base.Manager;
		if (definition == null || !GodotObject.IsInstanceValid(parent) || !GodotObject.IsInstanceValid(manager))
		{
			return;
		}
		Array<NodePath> controlledNodePaths = definition.controlledNodePaths;
		for (int i = 0; i < (controlledNodePaths?.Count ?? 0); i++)
		{
			ComponentBase componentBase = ResolveOwnerNode<ComponentBase>(controlledNodePaths[i]);
			if (GodotObject.IsInstanceValid(componentBase))
			{
				_controlledNodeComponents.Add(componentBase);
			}
		}
		Array<string> controlledRuntimeInstanceIds = definition.controlledRuntimeInstanceIds;
		Array<string> controlledRuntimeTypeIds = definition.controlledRuntimeTypeIds;
		for (int j = 0; j < (controlledRuntimeInstanceIds?.Count ?? 0); j++)
		{
			string text = controlledRuntimeInstanceIds[j]?.Trim();
			if (!string.IsNullOrEmpty(text) && manager.TryGetRuntimeByInstanceId(text, out var runtime))
			{
				string text2 = ((j >= (controlledRuntimeTypeIds?.Count ?? 0)) ? string.Empty : controlledRuntimeTypeIds[j]?.Trim());
				if (string.IsNullOrEmpty(text2) || string.Equals(runtime.ComponentDefinition?.ComponentTypeId, text2, StringComparison.Ordinal))
				{
					_controlledRuntimeComponents.Add(runtime);
				}
			}
		}
	}

	private void ReattachAfterTreeEntry()
	{
		if (base.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(parent) && parent.IsInsideTree())
		{
			ResolveReferences();
			ConnectSpriteSignal();
			ConnectStateSignals();
			ApplyPendingSyncedState();
			if (_scaredApplied)
			{
				ApplyPendingControlledAliveRestore();
			}
		}
	}

	private void ClearResolvedReferences()
	{
		_controlledNodeComponents.Clear();
		_controlledRuntimeComponents.Clear();
		sprite = null;
	}

	protected override void OnStateRuntimeAttached()
	{
		_idleState = base.StateMachine?.GetStateById("scared.idle");
		_scaredDownState = base.StateMachine?.GetStateById("scared.down");
		_scaredIdleState = base.StateMachine?.GetStateById("scared.scared_idle");
		_scaredGrowState = base.StateMachine?.GetStateById("scared.grow");
		ConnectStateSignals();
		ApplyPendingSyncedState();
	}

	protected override void OnStateRuntimeRegistered()
	{
		ConnectStateSignals();
		ResolveControlledComponents();
		bool num = !string.IsNullOrEmpty(_pendingSyncedState);
		ApplyPendingSyncedState();
		if (num)
		{
			ApplyAuthoritativeScaredInvariants();
		}
	}

	protected override void OnStateRuntimeDetaching()
	{
		CaptureCurrentStateForReattach();
		DisconnectStateSignals();
		SetScared(scared: false);
		_idleState = null;
		_scaredDownState = null;
		_scaredIdleState = null;
		_scaredGrowState = null;
	}

	protected override void OnAuthoritativeStateRestorePreparing(bool remote)
	{
		_pendingSyncedState = null;
		SetScared(scared: false);
	}

	protected override void OnAuthoritativeStateRestored(StateMachineSnapshot snapshot, bool remote)
	{
		ApplyAuthoritativeScaredInvariants();
	}

	private void ConnectSpriteSignal()
	{
		if (!_spriteSignalConnected && GodotObject.IsInstanceValid(sprite))
		{
			sprite.OnAnimeCompleted += AnimeCompleted;
			_spriteSignalConnected = true;
		}
	}

	private void DisconnectSpriteSignal()
	{
		if (_spriteSignalConnected && GodotObject.IsInstanceValid(sprite))
		{
			sprite.OnAnimeCompleted -= AnimeCompleted;
		}
		_spriteSignalConnected = false;
	}

	private void ConnectStateSignals()
	{
		if (!_stateSignalsConnected)
		{
			ConnectState(_idleState, IdleEntered, IdleExited, IdleProcessing);
			ConnectState(_scaredDownState, ScaredDownEntered, ScaredDownExited, ScaredDownProcessing);
			ConnectState(_scaredIdleState, ScaredIdleEntered, ScaredIdleExited, ScaredIdleProcessing);
			ConnectState(_scaredGrowState, ScaredGrowEntered, ScaredGrowExited, ScaredGrowProcessing);
			_stateSignalsConnected = true;
		}
	}

	private void DisconnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			DisconnectState(_idleState, IdleEntered, IdleExited, IdleProcessing);
			DisconnectState(_scaredDownState, ScaredDownEntered, ScaredDownExited, ScaredDownProcessing);
			DisconnectState(_scaredIdleState, ScaredIdleEntered, ScaredIdleExited, ScaredIdleProcessing);
			DisconnectState(_scaredGrowState, ScaredGrowEntered, ScaredGrowExited, ScaredGrowProcessing);
			_stateSignalsConnected = false;
		}
	}

	private static void ConnectState(StateHandle state, Action entered, Action exited, Action<double> processing)
	{
		if (state != null && state.IsValid)
		{
			state.Entered += entered;
			state.Exited += exited;
			state.PhysicsProcessing += processing;
		}
	}

	private static void DisconnectState(StateHandle state, Action entered, Action exited, Action<double> processing)
	{
		if (state != null)
		{
			state.Entered -= entered;
			state.Exited -= exited;
			state.PhysicsProcessing -= processing;
		}
	}

	public void IdleEntered()
	{
		if (TryGetRuntime(out var character))
		{
			character.Idle();
		}
	}

	public void IdleProcessing(double delta)
	{
		if (TryGetRuntime(out var character) && !IsRemoteSyncedClient(character) && HasScareTarget())
		{
			character.Component();
			SendStateEvent(scaredDownStateEvent);
		}
	}

	public void IdleExited()
	{
	}

	public void ScaredDownEntered()
	{
		this.OnScaredDown?.Invoke();
		SetScared(scared: true);
		if (GodotObject.IsInstanceValid(sprite))
		{
			sprite.SetAnimation(scaredDownAnimeClip, loop: false, 0.20000000298023224);
		}
	}

	public void ScaredDownProcessing(double delta)
	{
		ApplyAnimationTimeScale(scaredDownAnimeTimeScale);
	}

	public void ScaredDownExited()
	{
	}

	public void ScaredIdleEntered()
	{
		SetScared(scared: true);
		if (GodotObject.IsInstanceValid(sprite))
		{
			sprite.SetAnimation(scaredIdleAnimeClip);
		}
	}

	public void ScaredIdleProcessing(double delta)
	{
		ApplyAnimationTimeScale(scaredIdleAnimeTimeScale);
		if (TryGetRuntime(out var character) && !IsRemoteSyncedClient(character) && !HasScareTarget())
		{
			SendStateEvent(scaredGrowStateEvent);
		}
	}

	public void ScaredIdleExited()
	{
	}

	public void ScaredGrowEntered()
	{
		SetScared(scared: true);
		if (GodotObject.IsInstanceValid(sprite))
		{
			sprite.SetAnimation(scaredGrowAnimeClip, loop: false, 0.20000000298023224);
		}
	}

	public void ScaredGrowProcessing(double delta)
	{
		ApplyAnimationTimeScale(scaredGrowAnimeTimeScale);
	}

	public void ScaredGrowExited()
	{
	}

	private bool HasScareTarget()
	{
		if (!TryGetScareWorldRect(out var rect) || !GodotObject.IsInstanceValid(TowerDefenseManager.Instance?.characterRegistry))
		{
			return false;
		}
		ScareTargetPredicate predicate = new ScareTargetPredicate
		{
			Parent = parent
		};
		TowerDefenseCharacter match;
		return TowerDefenseManager.Instance.characterRegistry.TryFindCharacterIntersectingRectExcludingCamp(rect, -2147483648, includeAllLineCheck: false, parent.camp, ref predicate, out match);
	}

	private bool TryGetScareWorldRect(out Rect2 rect)
	{
		rect = default(Rect2);
		ScaredComponentDefinition definition = Definition;
		AabbShape2DResource aabbShape2DResource = definition?.scareShape;
		if (!GodotObject.IsInstanceValid(parent) || !GodotObject.IsInstanceValid(aabbShape2DResource) || !aabbShape2DResource.Enabled)
		{
			return false;
		}
		ulong physicsFrameForCachedGameplayQuery = TowerDefenseProcessModeDispatch.GetPhysicsFrameForCachedGameplayQuery();
		Transform2D globalTransformForPhysicsFrame = parent.GetGlobalTransformForPhysicsFrame(physicsFrameForCachedGameplayQuery);
		if (definition.scaleScareShapeToMapGrid && aabbShape2DResource.Geometry is RectangleShape2D && GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			Vector2 vector = new Vector2(Mathf.Max(0f, definition.scareGridSpan.X), Mathf.Max(0f, definition.scareGridSpan.Y));
			Vector2 size = TowerDefenseManager.Instance.GetMapGridSize() * vector;
			if (size.X <= 0f || size.Y <= 0f)
			{
				return false;
			}
			rect = AabbShapeUtil.ComputeRectangleWorldRect(globalTransformForPhysicsFrame * aabbShape2DResource.LocalTransform, size);
			return true;
		}
		return aabbShape2DResource.TryGetWorldRect(globalTransformForPhysicsFrame, out rect);
	}

	private void ApplyAnimationTimeScale(float animationScale)
	{
		if (GodotObject.IsInstanceValid(sprite) && GodotObject.IsInstanceValid(parent))
		{
			sprite.timeScale = parent.timeScale * (double)animationScale;
		}
	}

	public void AnimeCompleted(string clip)
	{
		if (!base.Alive)
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine != null && stateMachine.IsInitialized)
		{
			if (clip == scaredDownAnimeClip)
			{
				SendStateEvent(scaredIdleStateEvent);
			}
			else if (clip == scaredGrowAnimeClip)
			{
				this.OnScaredRise?.Invoke();
				SetScared(scared: false);
				SendStateEvent(idleStateEvent);
			}
		}
	}

	public void SetScared(bool scared)
	{
		if (GodotObject.IsInstanceValid(parent?.instance) && _scaredApplied != scared)
		{
			_scaredApplied = scared;
			if (scared)
			{
				height = parent.instance.height;
				parent.instance.height = scaredHeight;
				CaptureAndDisableControlledComponents();
			}
			else
			{
				parent.instance.height = height;
				RestoreControlledComponents();
			}
		}
	}

	private void CaptureAndDisableControlledComponents()
	{
		ResolveControlledComponents();
		_controlledAliveBeforeScare.Clear();
		for (int i = 0; i < _controlledNodeComponents.Count; i++)
		{
			ComponentBase componentBase = _controlledNodeComponents[i];
			if (GodotObject.IsInstanceValid(componentBase))
			{
				string controlledNodePersistenceKey = GetControlledNodePersistenceKey(componentBase);
				_controlledAliveBeforeScare[controlledNodePersistenceKey] = componentBase.alive;
				componentBase.SetAlive(_alive: false);
			}
		}
		for (int j = 0; j < _controlledRuntimeComponents.Count; j++)
		{
			CharacterComponentRuntime characterComponentRuntime = _controlledRuntimeComponents[j];
			if (characterComponentRuntime != null && !characterComponentRuntime.IsReleased)
			{
				string controlledRuntimePersistenceKey = GetControlledRuntimePersistenceKey(characterComponentRuntime);
				_controlledAliveBeforeScare[controlledRuntimePersistenceKey] = characterComponentRuntime.Alive;
				characterComponentRuntime.SetAlive(alive: false);
			}
		}
	}

	private void RestoreControlledComponents()
	{
		ResolveControlledComponents();
		for (int i = 0; i < _controlledNodeComponents.Count; i++)
		{
			ComponentBase componentBase = _controlledNodeComponents[i];
			if (GodotObject.IsInstanceValid(componentBase) && _controlledAliveBeforeScare.TryGetValue(GetControlledNodePersistenceKey(componentBase), out var value))
			{
				componentBase.SetAlive(value);
			}
		}
		for (int j = 0; j < _controlledRuntimeComponents.Count; j++)
		{
			CharacterComponentRuntime characterComponentRuntime = _controlledRuntimeComponents[j];
			if (characterComponentRuntime != null && !characterComponentRuntime.IsReleased && _controlledAliveBeforeScare.TryGetValue(GetControlledRuntimePersistenceKey(characterComponentRuntime), out var value2))
			{
				characterComponentRuntime.SetAlive(value2);
			}
		}
		_controlledAliveBeforeScare.Clear();
	}

	private void ApplyPendingControlledAliveRestore()
	{
		ResolveControlledComponents();
		for (int i = 0; i < _controlledNodeComponents.Count; i++)
		{
			ComponentBase componentBase = _controlledNodeComponents[i];
			if (GodotObject.IsInstanceValid(componentBase))
			{
				string controlledNodePersistenceKey = GetControlledNodePersistenceKey(componentBase);
				if (TryTakePendingControlledAlive(controlledNodePersistenceKey, componentBase.Name.ToString(), componentBase._GetName(), out var value))
				{
					_controlledAliveBeforeScare[controlledNodePersistenceKey] = value;
				}
				componentBase.SetAlive(_alive: false);
			}
		}
		for (int j = 0; j < _controlledRuntimeComponents.Count; j++)
		{
			CharacterComponentRuntime characterComponentRuntime = _controlledRuntimeComponents[j];
			if (characterComponentRuntime != null && !characterComponentRuntime.IsReleased)
			{
				string controlledRuntimePersistenceKey = GetControlledRuntimePersistenceKey(characterComponentRuntime);
				string legacyKey = characterComponentRuntime.ComponentDefinition?.ComponentTypeId ?? string.Empty;
				if (TryTakePendingControlledAlive(controlledRuntimePersistenceKey, legacyKey, GetFirstLegacyName(characterComponentRuntime), out var value2))
				{
					_controlledAliveBeforeScare[controlledRuntimePersistenceKey] = value2;
				}
				characterComponentRuntime.SetAlive(alive: false);
			}
		}
	}

	private bool TryTakePendingControlledAlive(string primaryKey, string legacyKey, string alternateLegacyKey, out bool value)
	{
		if (TryTakePendingControlledAlive(primaryKey, out value) || TryTakePendingControlledAlive(legacyKey, out value) || TryTakePendingControlledAlive(alternateLegacyKey, out value))
		{
			return true;
		}
		value = false;
		return false;
	}

	private bool TryTakePendingControlledAlive(string key, out bool value)
	{
		if (!string.IsNullOrEmpty(key) && _pendingControlledAliveRestore.TryGetValue(key, out value))
		{
			_pendingControlledAliveRestore.Remove(key);
			return true;
		}
		value = false;
		return false;
	}

	private string GetControlledNodePersistenceKey(ComponentBase component)
	{
		if (GodotObject.IsInstanceValid(base.Manager) && base.Manager.TryGetWireKey(component, out var wireKey))
		{
			return wireKey;
		}
		return component.Name.ToString();
	}

	private static string GetControlledRuntimePersistenceKey(CharacterComponentRuntime component)
	{
		string text = component?.ComponentDefinition?.InstanceId?.Trim();
		object obj;
		if (string.IsNullOrEmpty(text))
		{
			obj = component?.ComponentDefinition?.ComponentTypeId;
			if (obj == null)
			{
				return string.Empty;
			}
		}
		else
		{
			obj = text;
		}
		return (string)obj;
	}

	private static string GetFirstLegacyName(CharacterComponentRuntime component)
	{
		Array<StringName> array = component?.ComponentDefinition?.LegacyNodeNames;
		if (array == null || array.Count <= 0)
		{
			return string.Empty;
		}
		return array[0].ToString();
	}

	public override Dictionary ExportComponentSave()
	{
		return SerializeRuntimeState(includeControlledAlive: true, new Dictionary());
	}

	public override void ImportComponentSave(Dictionary data, TowerDefenseLevelSaveConfigCSharp owner)
	{
		DeserializeRuntimeState(data, restoreControlledAlive: true);
	}

	public override Dictionary SyncSerialize()
	{
		_syncPayload.Clear();
		return SerializeRuntimeState(includeControlledAlive: false, _syncPayload);
	}

	public override void SyncDeserialize(Dictionary data)
	{
		DeserializeRuntimeState(data, restoreControlledAlive: false);
	}

	private Dictionary SerializeRuntimeState(bool includeControlledAlive, Dictionary data)
	{
		data["scared"] = _scaredApplied;
		data["base_height"] = (int)height;
		string activeStateName = GetActiveStateName();
		if (!string.IsNullOrEmpty(activeStateName))
		{
			data["state"] = activeStateName;
		}
		if (includeControlledAlive && _controlledAliveBeforeScare.Count > 0)
		{
			Dictionary dictionary = new Dictionary();
			foreach (KeyValuePair<string, bool> item in _controlledAliveBeforeScare)
			{
				dictionary[item.Key] = item.Value;
			}
			data["controlled_alive"] = dictionary;
		}
		return data;
	}

	private void DeserializeRuntimeState(Dictionary data, bool restoreControlledAlive)
	{
		if (data == null)
		{
			return;
		}
		if (restoreControlledAlive)
		{
			_pendingControlledAliveRestore.Clear();
			if (data.ContainsKey("controlled_alive"))
			{
				Dictionary dictionary = data["controlled_alive"].AsGodotDictionary();
				foreach (Variant key in dictionary.Keys)
				{
					_pendingControlledAliveRestore[key.AsString()] = dictionary[key].AsBool();
				}
			}
		}
		TowerDefenseEnum.CHARACTER_HEIGHT cHARACTER_HEIGHT = (TowerDefenseEnum.CHARACTER_HEIGHT)data.GetValueOrDefault("base_height", (int)height).AsInt32();
		bool flag = base.ShouldApplyLegacyStateField && data.ContainsKey("state");
		string text = (flag ? data["state"].AsString() : string.Empty);
		bool flag2 = !string.IsNullOrEmpty(text) && text != "Idle" && text != "scared.idle";
		bool flag3 = data.GetValueOrDefault("scared", flag2).AsBool();
		height = cHARACTER_HEIGHT;
		if (!base.ShouldApplyLegacyStateField)
		{
			_pendingSyncedState = null;
			return;
		}
		if (flag3)
		{
			SetScared(scared: true);
			height = cHARACTER_HEIGHT;
			ApplyPendingControlledAliveRestore();
		}
		else
		{
			SetScared(scared: false);
			_pendingControlledAliveRestore.Clear();
			if (GodotObject.IsInstanceValid(parent?.instance))
			{
				parent.instance.height = height;
			}
		}
		if (flag && !string.IsNullOrEmpty(text))
		{
			_pendingSyncedState = text;
			ApplyPendingSyncedState();
		}
	}

	private void ApplyAuthoritativeScaredInvariants()
	{
		string text = base.StateMachine?.CurrentStateHandle?.StableId ?? string.Empty;
		bool flag = text == "scared.down" || text == "scared.scared_idle" || text == "scared.grow";
		TowerDefenseEnum.CHARACTER_HEIGHT cHARACTER_HEIGHT = height;
		SetScared(flag);
		height = cHARACTER_HEIGHT;
		if (flag)
		{
			ApplyPendingControlledAliveRestore();
			if (GodotObject.IsInstanceValid(parent?.instance))
			{
				parent.instance.height = scaredHeight;
			}
			RestoreScaredAnimation(text);
		}
		else
		{
			_pendingControlledAliveRestore.Clear();
			if (GodotObject.IsInstanceValid(parent?.instance))
			{
				parent.instance.height = height;
			}
		}
	}

	private void RestoreScaredAnimation(string stableId)
	{
		if (GodotObject.IsInstanceValid(sprite))
		{
			if (stableId == "scared.down" && !string.IsNullOrEmpty(scaredDownAnimeClip) && sprite.clip != scaredDownAnimeClip)
			{
				sprite.SetAnimation(scaredDownAnimeClip, loop: false);
			}
			else if (stableId == "scared.scared_idle" && !string.IsNullOrEmpty(scaredIdleAnimeClip) && sprite.clip != scaredIdleAnimeClip)
			{
				sprite.SetAnimation(scaredIdleAnimeClip);
			}
			else if (stableId == "scared.grow" && !string.IsNullOrEmpty(scaredGrowAnimeClip) && sprite.clip != scaredGrowAnimeClip)
			{
				sprite.SetAnimation(scaredGrowAnimeClip, loop: false);
			}
		}
	}

	private void ApplyPendingSyncedState()
	{
		if (base.IsStateMachineRegistered && !string.IsNullOrEmpty(_pendingSyncedState))
		{
			IStateMachineController stateMachine = base.StateMachine;
			if (stateMachine != null && stateMachine.CurrentStateHandle?.IsValid == true && GodotObject.IsInstanceValid(parent) && parent.IsNodeReady() && SyncForceState(base.StateMachine, _pendingSyncedState))
			{
				_pendingSyncedState = null;
			}
		}
	}

	private void CaptureCurrentStateForReattach()
	{
		StateHandle stateHandle = base.StateMachine?.CurrentStateHandle;
		if (string.IsNullOrEmpty(_pendingSyncedState) && stateHandle != null && stateHandle.IsValid)
		{
			_pendingSyncedState = stateHandle.StableId;
		}
	}

	private string GetActiveStateName()
	{
		StateHandle stateHandle = base.StateMachine?.CurrentStateHandle;
		if (stateHandle == null || !stateHandle.IsValid)
		{
			return string.Empty;
		}
		return stateHandle.StableId;
	}

	private static bool SyncForceState(IStateMachineController state, string targetState)
	{
		if (state == null || !state.IsInitialized || string.IsNullOrEmpty(targetState))
		{
			return false;
		}
		StateHandle currentStateHandle = state.CurrentStateHandle;
		StateHandle stateHandle = state.ResolveState(targetState, allowDisplayNameFallback: true);
		if (currentStateHandle == null || !currentStateHandle.IsValid || stateHandle == null || !stateHandle.IsValid)
		{
			return false;
		}
		if (currentStateHandle.StableId == stateHandle.StableId)
		{
			return true;
		}
		StateMachineSnapshot stateMachineSnapshot = state.CaptureSnapshot();
		if (stateMachineSnapshot == null)
		{
			return false;
		}
		stateMachineSnapshot.ActiveStateIds.Clear();
		stateMachineSnapshot.ActiveStateIds.Add(stateHandle.StableId);
		stateMachineSnapshot.PendingTransitionIds.Clear();
		stateMachineSnapshot.PendingDelayRemaining.Clear();
		return state.RestoreSnapshot(stateMachineSnapshot);
	}

	private bool IsRemoteSyncedClient(TowerDefenseCharacter character)
	{
		if (hostAuthoritative && Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return character.syncId >= 0;
		}
		return false;
	}

	private bool TryGetRuntime(out TowerDefenseCharacter character)
	{
		character = parent;
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(character) && GodotObject.IsInstanceValid(character.instance) && character.inGame && character.componentAlive)
		{
			IStateMachineController stateMachine = base.StateMachine;
			if (stateMachine != null && stateMachine.IsInitialized && GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
			{
				return TowerDefenseManager.Instance.IsGameRunning();
			}
		}
		return false;
	}
}
