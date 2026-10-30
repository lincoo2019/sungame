using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class ComponentManager : Resource
{
	private sealed class ComponentRegistration
	{
		public string TypeName;

		public int TypeIndex;

		public WeakReference<ComponentBase> ActiveComponent;

		public CharacterComponentRuntime ActiveRuntime;

		public string InstanceId;

		public string DefinitionId;

		public bool HasProcessWork;

		public bool HasPhysicsWork;

		public bool HasInputWork;

		public bool HasUnhandledInputWork;

		public int PhysicsStatefulIndex = -1;

		public string WireKey;
	}

	private static readonly System.Collections.Generic.Dictionary<string, string> LegacyComponentAliases = new System.Collections.Generic.Dictionary<string, string>(StringComparer.Ordinal)
	{
		["ImpThrowComponent"] = "ImpFlightComponent",
		["ThrowImpComponent"] = "ImpThrowerComponent",
		["WaterComponent"] = "WaterEnvironmentComponent",
		["ReverseInjuryComponent"] = "PeriodicAreaEventComponent"
	};

	public List<ComponentBase> componentList = new List<ComponentBase>();

	private readonly HashSet<ComponentBase> _componentSet = new HashSet<ComponentBase>(ReferenceEqualityComparer.Instance);

	public System.Collections.Generic.Dictionary<string, List<ComponentBase>> componentDictionary = new System.Collections.Generic.Dictionary<string, List<ComponentBase>>();

	private readonly List<CharacterComponentRuntime> _resourceComponents = new List<CharacterComponentRuntime>();

	private readonly List<CharacterComponentRuntime> _ownerGameplayActivationRuntimes = new List<CharacterComponentRuntime>();

	private CharacterComponentRuntime _singleOwnerGameplayActivationRuntime;

	private readonly System.Collections.Generic.Dictionary<CharacterComponentRuntime, ComponentRegistration> _registrationByRuntime = new System.Collections.Generic.Dictionary<CharacterComponentRuntime, ComponentRegistration>(ReferenceEqualityComparer.Instance);

	private readonly System.Collections.Generic.Dictionary<string, CharacterComponentRuntime> _runtimeByInstanceId = new System.Collections.Generic.Dictionary<string, CharacterComponentRuntime>(StringComparer.Ordinal);

	private CharacterComponentSet _resourceComponentSet;

	private CharacterComponentCreationPlan _appliedCreationPlan;

	private readonly List<ComponentBase> _statefulComponents = new List<ComponentBase>();

	private readonly System.Collections.Generic.Dictionary<ComponentBase, int> _statefulIndices = new System.Collections.Generic.Dictionary<ComponentBase, int>(ReferenceEqualityComparer.Instance);

	private readonly List<CharacterComponentRuntime> _statefulRuntimes = new List<CharacterComponentRuntime>();

	private readonly System.Collections.Generic.Dictionary<CharacterComponentRuntime, int> _statefulRuntimeIndices = new System.Collections.Generic.Dictionary<CharacterComponentRuntime, int>(ReferenceEqualityComparer.Instance);

	private List<ComponentBase> _physicsStatefulComponents;

	private List<CharacterComponentRuntime> _physicsStatefulRuntimes;

	private readonly List<CharacterComponentRuntime> _physicsRuntimes = new List<CharacterComponentRuntime>();

	private readonly System.Collections.Generic.Dictionary<CharacterComponentRuntime, int> _physicsRuntimeIndices = new System.Collections.Generic.Dictionary<CharacterComponentRuntime, int>(ReferenceEqualityComparer.Instance);

	private System.Collections.Generic.Dictionary<Type, string> _componentStatePhysicsMetricNames;

	private System.Collections.Generic.Dictionary<Type, string> _runtimeStatePhysicsMetricNames;

	private System.Collections.Generic.Dictionary<Type, string> _runtimePhysicsMetricNames;

	private int _processStateMachineCount;

	private int _physicsStateMachineCount;

	private int _inputRuntimeCount;

	private int _unhandledInputRuntimeCount;

	private readonly System.Collections.Generic.Dictionary<ComponentBase, ComponentRegistration> _registrationByComponent = new System.Collections.Generic.Dictionary<ComponentBase, ComponentRegistration>(ReferenceEqualityComparer.Instance);

	private readonly System.Collections.Generic.Dictionary<string, List<ComponentRegistration>> _slotsByType = new System.Collections.Generic.Dictionary<string, List<ComponentRegistration>>(StringComparer.Ordinal);

	private System.Collections.Generic.Dictionary<string, Dictionary> _pendingNetworkStateByWireKey;

	private ComponentTimerService _timerService;

	private bool _isExitingTree;

	private bool _resourceStateRegistrationScheduled;

	private bool _ownerAttached;

	public Node parent;

	[Export(PropertyHint.None, "")]
	public CharacterComponentSet ComponentSet { get; set; }

	public IReadOnlyList<CharacterComponentRuntime> ResourceComponents => _resourceComponents;

	private TowerDefenseCharacter Owner => parent as TowerDefenseCharacter;

	private bool IsOwnerInsideTree
	{
		get
		{
			if (GodotObject.IsInstanceValid(Owner))
			{
				return Owner.IsInsideTree();
			}
			return false;
		}
	}

	public StringName Name { get; set; } = "ComponentManager";

	public Node.ProcessModeEnum ProcessMode
	{
		get
		{
			if (!GodotObject.IsInstanceValid(Owner))
			{
				return Node.ProcessModeEnum.Inherit;
			}
			return Owner.ProcessMode;
		}
		set
		{
			if (GodotObject.IsInstanceValid(Owner))
			{
				Owner.ProcessMode = value;
			}
		}
	}

	internal bool HasStateMachineProcessWork => _processStateMachineCount > 0;

	internal bool HasStateMachinePhysicsWork => _physicsStateMachineCount > 0;

	internal bool HasRuntimePhysicsWork => _physicsRuntimes.Count > 0;

	internal bool HasComponentTimerWork => _timerService?.HasActiveTimers ?? false;

	internal bool HasRuntimeInputWork => _inputRuntimeCount > 0;

	internal bool HasRuntimeUnhandledInputWork => _unhandledInputRuntimeCount > 0;

	internal bool CanDispatchOwnerGameplay
	{
		get
		{
			if (parent is TowerDefenseCharacter towerDefenseCharacter)
			{
				return towerDefenseCharacter.IsInsideComponentBattlefield;
			}
			return true;
		}
	}

	public ComponentManager()
	{
		base.ResourceLocalToScene = true;
	}

	public bool IsInsideTree()
	{
		return IsOwnerInsideTree;
	}

	public Node GetParent()
	{
		return parent;
	}

	public void AddChild(Node child)
	{
		if (GodotObject.IsInstanceValid(Owner) && GodotObject.IsInstanceValid(child))
		{
			Owner.AddChild(child, forceReadableName: false, Node.InternalMode.Disabled);
		}
	}

	public void RemoveChild(Node child)
	{
		if (GodotObject.IsInstanceValid(Owner) && GodotObject.IsInstanceValid(child) && child.GetParent() == Owner)
		{
			Owner.RemoveChild(child);
		}
	}

	public void AttachOwner(TowerDefenseCharacter owner)
	{
		if (!GodotObject.IsInstanceValid(owner))
		{
			return;
		}
		if (_ownerAttached && parent == owner)
		{
			RegisterExistingChildren();
			RefreshInputProcessMode();
			return;
		}
		if (_ownerAttached)
		{
			DetachOwner();
		}
		parent = owner;
		_ownerAttached = true;
		_isExitingTree = false;
		owner.SetProcessInput(enable: false);
		owner.SetProcessUnhandledInput(enable: false);
		owner.ChildEnteredTree += OnChildEnteredTree;
		owner.ChildExitingTree += OnChildExitingTree;
		RegisterExistingChildren();
		if (_resourceComponents.Count > 0)
		{
			RebindResourceComponents(owner);
			Callable.From(ActivateResourceComponents).CallDeferred();
		}
		RefreshInputProcessMode();
	}

	public void DetachOwner(ComponentDetachReason reason = ComponentDetachReason.TemporaryTreeExit)
	{
		TowerDefenseCharacter owner = Owner;
		if (GodotObject.IsInstanceValid(owner))
		{
			owner.SetProcessInput(enable: false);
			owner.SetProcessUnhandledInput(enable: false);
			owner.ChildEnteredTree -= OnChildEnteredTree;
			owner.ChildExitingTree -= OnChildExitingTree;
		}
		_isExitingTree = true;
		DetachResourceComponents(reason);
		while (componentList.Count > 0)
		{
			List<ComponentBase> list = componentList;
			UnregisterComponent(list[list.Count - 1]);
		}
		_statefulComponents.Clear();
		_statefulIndices.Clear();
		_statefulRuntimes.Clear();
		_statefulRuntimeIndices.Clear();
		_physicsStatefulComponents?.Clear();
		_physicsStatefulRuntimes?.Clear();
		_physicsRuntimes.Clear();
		_physicsRuntimeIndices.Clear();
		_componentStatePhysicsMetricNames?.Clear();
		_runtimeStatePhysicsMetricNames?.Clear();
		_runtimePhysicsMetricNames?.Clear();
		_processStateMachineCount = 0;
		_physicsStateMachineCount = 0;
		_inputRuntimeCount = 0;
		_unhandledInputRuntimeCount = 0;
		componentList.Clear();
		_componentSet.Clear();
		componentDictionary.Clear();
		_pendingNetworkStateByWireKey?.Clear();
		_resourceStateRegistrationScheduled = false;
		_ownerAttached = false;
		if (GodotObject.IsInstanceValid(owner))
		{
			owner.RefreshComponentStateMachineDispatch();
		}
		parent = null;
		_isExitingTree = false;
	}

	public void Release()
	{
		if (_ownerAttached || parent != null)
		{
			DetachOwner(ComponentDetachReason.OwnerReleased);
		}
		ReleaseResourceComponents(ComponentDetachReason.OwnerReleased);
		_registrationByComponent.Clear();
		_registrationByRuntime.Clear();
		_runtimeByInstanceId.Clear();
		_slotsByType.Clear();
		_pendingNetworkStateByWireKey?.Clear();
		_timerService?.Clear();
		_physicsStatefulComponents?.Clear();
		_physicsStatefulRuntimes?.Clear();
		_physicsRuntimes.Clear();
		_physicsRuntimeIndices.Clear();
		_componentStatePhysicsMetricNames?.Clear();
		_runtimeStatePhysicsMetricNames?.Clear();
		_runtimePhysicsMetricNames?.Clear();
		_processStateMachineCount = 0;
		_physicsStateMachineCount = 0;
		_inputRuntimeCount = 0;
		_unhandledInputRuntimeCount = 0;
	}

	public override void _Notification(int what)
	{
		if ((long)what == 1)
		{
			Release();
		}
	}

	public void InitializeResourceComponents()
	{
		CharacterComponentSet componentSet = ComponentSet;
		if (Engine.IsEditorHint() || !(parent is TowerDefenseCharacter towerDefenseCharacter))
		{
			return;
		}
		CharacterComponentCreationPlan characterComponentCreationPlan = componentSet?.GetCreationPlan();
		if (_resourceComponentSet == componentSet && _appliedCreationPlan == characterComponentCreationPlan && _resourceComponents.Count > 0)
		{
			RebindResourceComponents(towerDefenseCharacter);
			return;
		}
		if (_resourceComponents.Count > 0)
		{
			ReleaseResourceComponents(ComponentDetachReason.DefinitionReplaced);
		}
		_resourceComponentSet = componentSet;
		_appliedCreationPlan = characterComponentCreationPlan;
		if (componentSet == null)
		{
			towerDefenseCharacter.RefreshResourceComponentFacades();
			return;
		}
		_resourceComponents.EnsureCapacity(_resourceComponents.Count + characterComponentCreationPlan.RuntimeCapacityHint);
		_runtimeByInstanceId.EnsureCapacity(_runtimeByInstanceId.Count + characterComponentCreationPlan.InstanceIdCapacityHint);
		_registrationByRuntime.EnsureCapacity(_registrationByRuntime.Count + characterComponentCreationPlan.RuntimeCapacityHint);
		_slotsByType.EnsureCapacity(_slotsByType.Count + characterComponentCreationPlan.ComponentTypeCapacityHint);
		for (int i = 0; i < characterComponentCreationPlan.Count; i++)
		{
			CharacterComponentCreationPlan.Entry entry = characterComponentCreationPlan[i];
			CharacterComponentRuntime characterComponentRuntime = null;
			try
			{
				characterComponentRuntime = entry.Definition.CreateRuntime();
				if (characterComponentRuntime == null)
				{
					GD.PushError("Character component definition '" + entry.Definition.ResourcePath + "' returned no runtime.");
					continue;
				}
				if (!RegisterRuntimeWireSlot(characterComponentRuntime, entry.Definition, entry.ResolvedComponentTypeId, entry.InstanceId, entry.ResolvedWireIndex, entry.WireKey, entry.WireSlotCapacityHint))
				{
					characterComponentRuntime.Release();
					continue;
				}
				_resourceComponents.Add(characterComponentRuntime);
				RegisterOwnerGameplayActivationRuntime(characterComponentRuntime, entry.Definition);
				_runtimeByInstanceId[entry.InstanceId] = characterComponentRuntime;
				characterComponentRuntime.Bind(this, towerDefenseCharacter, entry.Definition);
				if (_pendingNetworkStateByWireKey != null && _pendingNetworkStateByWireKey.Remove(entry.WireKey, out var value))
				{
					characterComponentRuntime.ApplyAuthoritativeSync(value);
				}
			}
			catch (Exception exception)
			{
				RollbackFailedRuntime(characterComponentRuntime, exception);
			}
		}
		towerDefenseCharacter.RefreshResourceComponentFacades();
	}

	public void ActivateResourceComponents()
	{
		if (!IsOwnerInsideTree)
		{
			return;
		}
		for (int i = 0; i < _resourceComponents.Count; i++)
		{
			CharacterComponentRuntime characterComponentRuntime = _resourceComponents[i];
			try
			{
				characterComponentRuntime.Activate();
				RefreshRuntimePhysicsRegistration(characterComponentRuntime, refreshOwner: false);
				RefreshRuntimeInputRegistration(characterComponentRuntime, refreshOwner: false);
			}
			catch (Exception exception)
			{
				RollbackFailedRuntime(characterComponentRuntime, exception);
				i--;
			}
		}
		RefreshOwnerProcessMembership();
		ScheduleResourceStateRegistration();
	}

	internal void NotifyOwnerGameplayActivated()
	{
		CharacterComponentRuntime singleOwnerGameplayActivationRuntime = _singleOwnerGameplayActivationRuntime;
		if (singleOwnerGameplayActivationRuntime != null)
		{
			singleOwnerGameplayActivationRuntime.OwnerGameplayActivated();
			if (_ownerGameplayActivationRuntimes.Count > 1)
			{
				for (int i = 1; i < _ownerGameplayActivationRuntimes.Count; i++)
				{
					_ownerGameplayActivationRuntimes[i].OwnerGameplayActivated();
				}
			}
		}
		else
		{
			for (int j = 0; j < _ownerGameplayActivationRuntimes.Count; j++)
			{
				_ownerGameplayActivationRuntimes[j].OwnerGameplayActivated();
			}
		}
	}

	internal void NotifyOwnerBeforeDestroy()
	{
		for (int i = 0; i < _resourceComponents.Count; i++)
		{
			_resourceComponents[i].OwnerBeforeDestroy();
		}
	}

	private void ScheduleResourceStateRegistration()
	{
		if (!_resourceStateRegistrationScheduled && IsOwnerInsideTree)
		{
			_resourceStateRegistrationScheduled = true;
			Callable.From(RegisterResourceStateRuntimes).CallDeferred();
		}
	}

	private void RegisterResourceStateRuntimes()
	{
		_resourceStateRegistrationScheduled = false;
		if (!IsOwnerInsideTree)
		{
			return;
		}
		for (int i = 0; i < _resourceComponents.Count; i++)
		{
			CharacterComponentRuntime characterComponentRuntime = _resourceComponents[i];
			try
			{
				characterComponentRuntime.RegisterStateRuntime();
				RefreshRuntimeStateMachineRegistration(characterComponentRuntime, refreshOwner: false);
			}
			catch (Exception exception)
			{
				RollbackFailedRuntime(characterComponentRuntime, exception);
				i--;
			}
		}
		RefreshOwnerProcessMembership();
	}

	public T GetRuntime<T>() where T : CharacterComponentRuntime
	{
		for (int i = 0; i < _resourceComponents.Count; i++)
		{
			if (_resourceComponents[i] is T val && !val.IsReleased)
			{
				return val;
			}
		}
		return null;
	}

	public T GetRuntime<T>(string instanceId) where T : CharacterComponentRuntime
	{
		if (string.IsNullOrWhiteSpace(instanceId) || !_runtimeByInstanceId.TryGetValue(instanceId, out var value) || value.IsReleased)
		{
			return null;
		}
		return value as T;
	}

	public T GetComponent<T>() where T : class
	{
		for (int i = 0; i < componentList.Count; i++)
		{
			if (componentList[i] is T result && GodotObject.IsInstanceValid(componentList[i]))
			{
				return result;
			}
		}
		for (int j = 0; j < _resourceComponents.Count; j++)
		{
			if (_resourceComponents[j] is T result2 && !_resourceComponents[j].IsReleased)
			{
				return result2;
			}
		}
		return null;
	}

	public CharacterComponentRuntime AddRuntimeComponent(CharacterComponentDefinition definition)
	{
		if (definition == null || !(parent is TowerDefenseCharacter towerDefenseCharacter))
		{
			return null;
		}
		string text = ResolveLegacyComponentName(definition.ComponentTypeId?.Trim());
		string text2 = definition.InstanceId?.Trim();
		if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(text2))
		{
			GD.PushError("Dynamic character component definition requires stable ComponentTypeId and InstanceId values.");
			return null;
		}
		if (_runtimeByInstanceId.TryGetValue(text2, out var value) && value != null && !value.IsReleased)
		{
			return value;
		}
		CharacterComponentRuntime characterComponentRuntime = null;
		try
		{
			characterComponentRuntime = definition.CreateRuntime();
			if (characterComponentRuntime == null)
			{
				return null;
			}
			if (!RegisterRuntimeWireSlot(characterComponentRuntime, definition, text))
			{
				characterComponentRuntime.Release(ComponentDetachReason.DefinitionReplaced);
				return null;
			}
			_resourceComponents.Add(characterComponentRuntime);
			RegisterOwnerGameplayActivationRuntime(characterComponentRuntime, definition);
			_runtimeByInstanceId[text2] = characterComponentRuntime;
			characterComponentRuntime.Bind(this, towerDefenseCharacter, definition);
			if (_pendingNetworkStateByWireKey != null && TryGetWireKey(characterComponentRuntime, out var wireKey) && _pendingNetworkStateByWireKey.Remove(wireKey, out var value2))
			{
				characterComponentRuntime.ApplyAuthoritativeSync(value2);
			}
			if (IsOwnerInsideTree)
			{
				characterComponentRuntime.Activate();
				characterComponentRuntime.RegisterStateRuntime();
				RefreshRuntimePhysicsRegistration(characterComponentRuntime, refreshOwner: false);
				RefreshRuntimeInputRegistration(characterComponentRuntime, refreshOwner: false);
				RefreshRuntimeStateMachineRegistration(characterComponentRuntime, refreshOwner: false);
				RefreshOwnerProcessMembership();
			}
			towerDefenseCharacter.RefreshResourceComponentFacades();
			return characterComponentRuntime;
		}
		catch (Exception exception)
		{
			RollbackFailedRuntime(characterComponentRuntime, exception);
			return null;
		}
	}

	private void RollbackFailedRuntime(CharacterComponentRuntime runtime, Exception exception)
	{
		if (runtime != null && !RemoveRuntimeComponent(runtime))
		{
			runtime.Release(ComponentDetachReason.DefinitionReplaced);
			DeactivateRuntimeWireSlot(runtime);
		}
		GD.PushWarning("[ComponentManager] 组件创建或生命周期失败，已回滚：" + exception.GetBaseException().Message);
	}

	public bool RemoveRuntimeComponent(CharacterComponentRuntime runtime)
	{
		if (runtime == null || !_resourceComponents.Remove(runtime))
		{
			return false;
		}
		UnregisterOwnerGameplayActivationRuntime(runtime);
		string[] array = (from pair in _runtimeByInstanceId
			where pair.Value == runtime
			select pair.Key).ToArray();
		foreach (string key in array)
		{
			_runtimeByInstanceId.Remove(key);
		}
		RemoveStatefulRuntime(runtime);
		RemovePhysicsRuntime(runtime);
		ClearRuntimeInputWorkRegistration(runtime);
		runtime.Release(ComponentDetachReason.DefinitionReplaced);
		DeactivateRuntimeWireSlot(runtime);
		if (parent is TowerDefenseCharacter towerDefenseCharacter)
		{
			towerDefenseCharacter.RefreshResourceComponentFacades();
		}
		RefreshOwnerProcessMembership();
		return true;
	}

	private void RegisterOwnerGameplayActivationRuntime(CharacterComponentRuntime runtime, CharacterComponentDefinition definition)
	{
		if (ShouldRegisterOwnerGameplayActivationRuntime(runtime, definition))
		{
			_ownerGameplayActivationRuntimes.Add(runtime);
			_singleOwnerGameplayActivationRuntime = ((_ownerGameplayActivationRuntimes.Count == 1) ? runtime : null);
		}
	}

	private static bool ShouldRegisterOwnerGameplayActivationRuntime(CharacterComponentRuntime runtime, CharacterComponentDefinition definition)
	{
		if (runtime == null || definition == null)
		{
			return false;
		}
		if (runtime.HasOwnerGameplayActivationWork || definition is ModCharacterComponentDefinition)
		{
			return true;
		}
		if (!(runtime.GetType().Assembly != typeof(CharacterComponentRuntime).Assembly))
		{
			return definition.GetType().Assembly != typeof(CharacterComponentDefinition).Assembly;
		}
		return true;
	}

	private void UnregisterOwnerGameplayActivationRuntime(CharacterComponentRuntime runtime)
	{
		if (runtime != null && _ownerGameplayActivationRuntimes.Remove(runtime))
		{
			if (_ownerGameplayActivationRuntimes.Count == 1)
			{
				_singleOwnerGameplayActivationRuntime = _ownerGameplayActivationRuntimes[0];
			}
			else
			{
				_singleOwnerGameplayActivationRuntime = null;
			}
		}
	}

	private void RebindResourceComponents(TowerDefenseCharacter owner)
	{
		for (int i = 0; i < _resourceComponents.Count; i++)
		{
			CharacterComponentRuntime characterComponentRuntime = _resourceComponents[i];
			try
			{
				if (!characterComponentRuntime.IsReleased)
				{
					characterComponentRuntime.Bind(this, owner, characterComponentRuntime.ComponentDefinition);
				}
			}
			catch (Exception exception)
			{
				RollbackFailedRuntime(characterComponentRuntime, exception);
				i--;
			}
		}
	}

	private void DetachResourceComponents(ComponentDetachReason reason)
	{
		for (int i = 0; i < _resourceComponents.Count; i++)
		{
			CharacterComponentRuntime characterComponentRuntime = _resourceComponents[i];
			RemoveStatefulRuntime(characterComponentRuntime);
			RemovePhysicsRuntime(characterComponentRuntime);
			ClearRuntimeInputWorkRegistration(characterComponentRuntime);
			try
			{
				characterComponentRuntime.Detach(reason);
			}
			catch (Exception exception)
			{
				RollbackFailedRuntime(characterComponentRuntime, exception);
				i--;
			}
		}
		RefreshOwnerProcessMembership();
	}

	private void ReleaseResourceComponents(ComponentDetachReason reason)
	{
		for (int num = _resourceComponents.Count - 1; num >= 0; num--)
		{
			CharacterComponentRuntime characterComponentRuntime = _resourceComponents[num];
			UnregisterOwnerGameplayActivationRuntime(characterComponentRuntime);
			RemoveStatefulRuntime(characterComponentRuntime);
			RemovePhysicsRuntime(characterComponentRuntime);
			ClearRuntimeInputWorkRegistration(characterComponentRuntime);
			characterComponentRuntime.Release(reason);
			DeactivateRuntimeWireSlot(characterComponentRuntime);
		}
		_resourceComponents.Clear();
		_ownerGameplayActivationRuntimes.Clear();
		_singleOwnerGameplayActivationRuntime = null;
		_runtimeByInstanceId.Clear();
		_resourceComponentSet = null;
		_appliedCreationPlan = null;
	}

	private void RegisterExistingChildren()
	{
		TowerDefenseCharacter owner = Owner;
		if (!GodotObject.IsInstanceValid(owner) || !owner.IsInsideTree())
		{
			return;
		}
		foreach (Node child in owner.GetChildren())
		{
			if (child is ComponentBase component)
			{
				RegisterComponent(component);
			}
		}
	}

	private void OnChildEnteredTree(Node child)
	{
		if (child is ComponentBase component)
		{
			RegisterComponent(component);
		}
	}

	private void OnChildExitingTree(Node child)
	{
		if (child is ComponentBase component)
		{
			UnregisterComponent(component);
		}
	}

	public void RegisterComponent(ComponentBase component)
	{
		TowerDefenseCharacter owner = Owner;
		if (GodotObject.IsInstanceValid(component) && GodotObject.IsInstanceValid(owner) && component.IsInsideTree() && component.GetParent() == owner && _componentSet.Add(component))
		{
			componentList.Add(component);
			string componentTypeName = GetComponentTypeName(component);
			if (!componentDictionary.TryGetValue(componentTypeName, out var value))
			{
				value = new List<ComponentBase>();
				componentDictionary[componentTypeName] = value;
			}
			value.Add(component);
			RegisterWireSlot(component, componentTypeName);
			Callable.From(delegate
			{
				AttachRegisteredComponent(component);
			}).CallDeferred();
		}
	}

	public void UnregisterComponent(ComponentBase component)
	{
		if (component == null || !_componentSet.Remove(component))
		{
			return;
		}
		RemoveStatefulComponent(component);
		if (TryGetWireKey(component, out var wireKey))
		{
			if (component.IsQueuedForDeletion())
			{
				_timerService?.RemoveComponent(component, wireKey);
			}
			else
			{
				_timerService?.DetachComponent(component, wireKey);
			}
		}
		DeactivateWireSlot(component);
		component.DetachFromManager(this);
		componentList.Remove(component);
		string componentTypeName = GetComponentTypeName(component);
		if (componentDictionary.TryGetValue(componentTypeName, out var value))
		{
			value.Remove(component);
			if (value.Count == 0)
			{
				componentDictionary.Remove(componentTypeName);
			}
		}
		RefreshOwnerProcessMembership();
	}

	private void AttachRegisteredComponent(ComponentBase component)
	{
		TowerDefenseCharacter owner = Owner;
		if (IsOwnerInsideTree && GodotObject.IsInstanceValid(component) && _componentSet.Contains(component) && component.GetParent() == owner)
		{
			component.AttachToManager(this);
			if (TryGetWireKey(component, out var wireKey))
			{
				_timerService?.AttachComponent(component, wireKey);
			}
			if (_pendingNetworkStateByWireKey != null && TryGetWireKey(component, out var wireKey2) && _pendingNetworkStateByWireKey.Remove(wireKey2, out var value))
			{
				component.ApplyAuthoritativeSync(value);
			}
		}
	}

	internal void ApplyOrQueueNetworkState(string wireKey, Dictionary data)
	{
		if (string.IsNullOrWhiteSpace(wireKey) || data == null)
		{
			return;
		}
		if (TryGetComponentByWireKey(wireKey, out var component))
		{
			component.ApplyAuthoritativeSync(data);
			return;
		}
		if (TryGetRuntimeByWireKey(wireKey, out var runtime))
		{
			runtime.ApplyAuthoritativeSync(data);
			return;
		}
		Dictionary dictionary = data.Duplicate(deep: true);
		if (!dictionary.ContainsKey("sm") && _pendingNetworkStateByWireKey != null && _pendingNetworkStateByWireKey.TryGetValue(wireKey, out var value) && value.ContainsKey("sm"))
		{
			dictionary["sm"] = value["sm"];
		}
		if (_pendingNetworkStateByWireKey == null)
		{
			_pendingNetworkStateByWireKey = new System.Collections.Generic.Dictionary<string, Dictionary>(StringComparer.Ordinal);
		}
		_pendingNetworkStateByWireKey[wireKey] = dictionary;
	}

	internal void RefreshStateMachineRegistration(ComponentBase component)
	{
		if (!GodotObject.IsInstanceValid(component) || !_componentSet.Contains(component) || !component.HasStateMachine)
		{
			RemoveStatefulComponent(component);
		}
		else if (_statefulIndices.TryAdd(component, _statefulComponents.Count))
		{
			_statefulComponents.Add(component);
		}
		RefreshStateMachineWorkRegistration(component);
		RefreshOwnerProcessMembership();
	}

	internal void NotifyComponentAliveChanged(ComponentBase component)
	{
		if (component != null && _componentSet.Contains(component))
		{
			RefreshStateMachineWorkRegistration(component);
			RefreshOwnerProcessMembership();
		}
	}

	internal void NotifyRuntimeAliveChanged(CharacterComponentRuntime runtime)
	{
		if (runtime != null && _registrationByRuntime.ContainsKey(runtime))
		{
			RefreshRuntimeStateMachineRegistration(runtime, refreshOwner: false);
			RefreshRuntimePhysicsRegistration(runtime, refreshOwner: false);
			RefreshRuntimeInputRegistration(runtime, refreshOwner: false);
			RefreshOwnerProcessMembership();
		}
	}

	internal void DispatchRuntimeInput(InputEvent inputEvent)
	{
		if (inputEvent == null || _inputRuntimeCount <= 0)
		{
			return;
		}
		for (int i = 0; i < _resourceComponents.Count; i++)
		{
			CharacterComponentRuntime characterComponentRuntime = _resourceComponents[i];
			if (characterComponentRuntime != null && characterComponentRuntime.HasRuntimeInputWork && characterComponentRuntime.CanDispatchInputWork)
			{
				characterComponentRuntime.ProcessInput(inputEvent);
			}
		}
	}

	public void _Input(InputEvent inputEvent)
	{
		DispatchRuntimeInput(inputEvent);
	}

	internal void DispatchRuntimeUnhandledInput(InputEvent inputEvent)
	{
		if (inputEvent == null || _unhandledInputRuntimeCount <= 0)
		{
			return;
		}
		for (int i = 0; i < _resourceComponents.Count; i++)
		{
			CharacterComponentRuntime characterComponentRuntime = _resourceComponents[i];
			if (characterComponentRuntime != null && characterComponentRuntime.HasRuntimeUnhandledInputWork && characterComponentRuntime.CanDispatchInputWork)
			{
				characterComponentRuntime.ProcessUnhandledInput(inputEvent);
			}
		}
	}

	public void _UnhandledInput(InputEvent inputEvent)
	{
		DispatchRuntimeUnhandledInput(inputEvent);
	}

	internal void TickStateMachineProcess(double delta)
	{
		int num = 0;
		while (num < _statefulComponents.Count)
		{
			ComponentBase componentBase = _statefulComponents[num];
			if (!GodotObject.IsInstanceValid(componentBase) || !componentBase.HasStateMachine)
			{
				RemoveStatefulAtSwap(num);
				continue;
			}
			if (componentBase.HasStateMachineProcessWork && componentBase.CanDispatchStateMachineWork)
			{
				componentBase.StateMachine.TickProcess(delta);
			}
			if (num < _statefulComponents.Count && _statefulComponents[num] == componentBase)
			{
				num++;
			}
		}
		int num2 = 0;
		while (num2 < _statefulRuntimes.Count)
		{
			CharacterComponentRuntime characterComponentRuntime = _statefulRuntimes[num2];
			if (characterComponentRuntime.IsReleased || !characterComponentRuntime.HasStateMachine)
			{
				RemoveStatefulRuntimeAtSwap(num2);
				continue;
			}
			if (characterComponentRuntime.HasStateMachineProcessWork && characterComponentRuntime.CanDispatchStateMachineWork)
			{
				characterComponentRuntime.StateMachine.TickProcess(delta);
			}
			if (num2 < _statefulRuntimes.Count && _statefulRuntimes[num2] == characterComponentRuntime)
			{
				num2++;
			}
		}
	}

	internal void TickStateMachinePhysics(double delta, bool ownerInsideComponentBattlefield)
	{
		bool flag = TowerDefensePerfProfiler.Enabled && TowerDefensePerfProfiler.DetailedHotPathMetrics;
		int num = 0;
		while (num < (_physicsStatefulComponents?.Count ?? 0))
		{
			ComponentBase componentBase = _physicsStatefulComponents[num];
			if (!GodotObject.IsInstanceValid(componentBase) || !componentBase.HasStateMachine)
			{
				RemoveStatefulComponent(componentBase);
				continue;
			}
			if (!componentBase.HasStateMachinePhysicsWork)
			{
				RefreshStateMachineWorkRegistration(componentBase);
				continue;
			}
			if (componentBase.CanDispatchStateMachineWork)
			{
				if (flag)
				{
					string componentStatePhysicsMetricName = GetComponentStatePhysicsMetricName(componentBase);
					long startTicks = TowerDefensePerfProfiler.BeginHotPath();
					componentBase.StateMachine.TickPhysics(delta);
					TowerDefensePerfProfiler.End(componentStatePhysicsMetricName, startTicks);
				}
				else
				{
					componentBase.StateMachine.TickPhysics(delta);
				}
			}
			if (num < _physicsStatefulComponents.Count && _physicsStatefulComponents[num] == componentBase)
			{
				num++;
			}
		}
		int num2 = 0;
		while (num2 < (_physicsStatefulRuntimes?.Count ?? 0))
		{
			CharacterComponentRuntime characterComponentRuntime = _physicsStatefulRuntimes[num2];
			if (characterComponentRuntime.IsReleased || !characterComponentRuntime.HasStateMachine)
			{
				RemoveStatefulRuntime(characterComponentRuntime);
				continue;
			}
			if (!characterComponentRuntime.HasStateMachinePhysicsWork)
			{
				RefreshRuntimeStateMachineWorkRegistration(characterComponentRuntime);
				continue;
			}
			if (characterComponentRuntime.CanDispatchStateMachineWorkForOwnerState(ownerInsideComponentBattlefield))
			{
				if (flag)
				{
					string runtimeStatePhysicsMetricName = GetRuntimeStatePhysicsMetricName(characterComponentRuntime);
					long startTicks2 = TowerDefensePerfProfiler.BeginHotPath();
					TickRuntimeStateMachinePhysics(characterComponentRuntime, delta);
					TowerDefensePerfProfiler.End(runtimeStatePhysicsMetricName, startTicks2);
				}
				else
				{
					TickRuntimeStateMachinePhysics(characterComponentRuntime, delta);
				}
			}
			if (num2 < _physicsStatefulRuntimes.Count && _physicsStatefulRuntimes[num2] == characterComponentRuntime)
			{
				num2++;
			}
		}
	}

	private static void TickRuntimeStateMachinePhysics(CharacterComponentRuntime runtime, double delta)
	{
		if (!runtime.TryTickFlatStateMachinePhysics(delta))
		{
			runtime.StateMachine.TickPhysics(delta);
		}
	}

	private string GetComponentStatePhysicsMetricName(ComponentBase component)
	{
		Type type = component.GetType();
		if (_componentStatePhysicsMetricNames != null && _componentStatePhysicsMetricNames.TryGetValue(type, out var value))
		{
			return value;
		}
		value = "batch.component.state." + type.Name;
		if (_componentStatePhysicsMetricNames == null)
		{
			_componentStatePhysicsMetricNames = new System.Collections.Generic.Dictionary<Type, string>();
		}
		_componentStatePhysicsMetricNames.Add(type, value);
		return value;
	}

	private string GetRuntimeStatePhysicsMetricName(CharacterComponentRuntime runtime)
	{
		Type type = runtime.GetType();
		if (_runtimeStatePhysicsMetricNames != null && _runtimeStatePhysicsMetricNames.TryGetValue(type, out var value))
		{
			return value;
		}
		value = "batch.component.runtimeState." + type.Name;
		if (_runtimeStatePhysicsMetricNames == null)
		{
			_runtimeStatePhysicsMetricNames = new System.Collections.Generic.Dictionary<Type, string>();
		}
		_runtimeStatePhysicsMetricNames.Add(type, value);
		return value;
	}

	internal void TickRuntimePhysics(double delta, ulong physicsFrame, bool ownerInsideComponentBattlefield)
	{
		bool flag = TowerDefensePerfProfiler.Enabled && TowerDefensePerfProfiler.DetailedHotPathMetrics;
		int num = 0;
		while (num < _physicsRuntimes.Count)
		{
			CharacterComponentRuntime characterComponentRuntime = _physicsRuntimes[num];
			if (characterComponentRuntime == null || !characterComponentRuntime.HasRuntimePhysicsWork)
			{
				RemovePhysicsRuntimeAtSwap(num);
				continue;
			}
			if (characterComponentRuntime.CanDispatchPhysicsWorkForOwnerState(ownerInsideComponentBattlefield))
			{
				if (flag)
				{
					long startTicks = TowerDefensePerfProfiler.BeginHotPath();
					characterComponentRuntime.PhysicsProcess(delta, physicsFrame);
					TowerDefensePerfProfiler.End(GetRuntimePhysicsMetricName(characterComponentRuntime), startTicks);
				}
				else
				{
					characterComponentRuntime.PhysicsProcess(delta, physicsFrame);
				}
			}
			if (num < _physicsRuntimes.Count && _physicsRuntimes[num] == characterComponentRuntime)
			{
				num++;
			}
		}
	}

	private string GetRuntimePhysicsMetricName(CharacterComponentRuntime runtime)
	{
		Type type = runtime.GetType();
		if (_runtimePhysicsMetricNames != null && _runtimePhysicsMetricNames.TryGetValue(type, out var value))
		{
			return value;
		}
		value = "batch.component.runtime." + type.Name;
		if (_runtimePhysicsMetricNames == null)
		{
			_runtimePhysicsMetricNames = new System.Collections.Generic.Dictionary<Type, string>();
		}
		_runtimePhysicsMetricNames.Add(type, value);
		return value;
	}

	internal void TickComponentTimers(double delta, bool ownerInsideComponentBattlefield)
	{
		if (ownerInsideComponentBattlefield)
		{
			ComponentTimerService timerService = _timerService;
			if (timerService != null && timerService.HasActiveTimers && TowerDefenseManager._IsGameRunning())
			{
				double ownerTimeScale = ((parent is TowerDefenseCharacter towerDefenseCharacter) ? towerDefenseCharacter.timeScale : 1.0);
				_timerService.Tick(delta, ownerTimeScale);
			}
		}
	}

	internal bool StartComponentTimer(ComponentBase component, StringName timerName, double duration, bool useOwnerTimeScale)
	{
		if (!TryGetComponentTimerKey(component, timerName, out var key))
		{
			return false;
		}
		if (_timerService == null)
		{
			_timerService = new ComponentTimerService();
		}
		return _timerService.Start(component, key, duration, useOwnerTimeScale);
	}

	internal bool StopComponentTimer(ComponentBase component, StringName timerName)
	{
		if (_timerService != null && TryGetComponentTimerKey(component, timerName, out var key))
		{
			return _timerService.Stop(component, key);
		}
		return false;
	}

	internal bool PauseComponentTimer(ComponentBase component, StringName timerName)
	{
		if (_timerService != null && TryGetComponentTimerKey(component, timerName, out var key))
		{
			return _timerService.Pause(component, key);
		}
		return false;
	}

	internal bool ResumeComponentTimer(ComponentBase component, StringName timerName)
	{
		if (_timerService != null && TryGetComponentTimerKey(component, timerName, out var key))
		{
			return _timerService.Resume(component, key);
		}
		return false;
	}

	internal bool IsComponentTimerRunning(ComponentBase component, StringName timerName)
	{
		if (_timerService != null && TryGetComponentTimerKey(component, timerName, out var key))
		{
			return _timerService.IsRunning(component, key);
		}
		return false;
	}

	internal double GetComponentTimerRemaining(ComponentBase component, StringName timerName)
	{
		if (_timerService == null || !TryGetComponentTimerKey(component, timerName, out var key))
		{
			return 0.0;
		}
		return _timerService.Remaining(component, key);
	}

	private bool TryGetComponentTimerKey(ComponentBase component, StringName timerName, out ComponentTimerKey key)
	{
		key = default(ComponentTimerKey);
		if (component == null || timerName.IsEmpty || !TryGetWireKey(component, out var wireKey))
		{
			return false;
		}
		key = new ComponentTimerKey(wireKey, timerName);
		return true;
	}

	internal bool TryGetWireKey(ComponentBase component, out string wireKey)
	{
		wireKey = string.Empty;
		if (component == null || !_registrationByComponent.TryGetValue(component, out var value) || !TryGetActiveComponent(value, out var component2) || component2 != component)
		{
			return false;
		}
		wireKey = value.WireKey;
		return true;
	}

	internal bool TryGetWireKey(CharacterComponentRuntime runtime, out string wireKey)
	{
		wireKey = string.Empty;
		if (runtime == null || runtime.IsReleased || !_registrationByRuntime.TryGetValue(runtime, out var value) || value.ActiveRuntime != runtime)
		{
			return false;
		}
		wireKey = value.WireKey;
		return true;
	}

	internal bool TryGetComponentByWireKey(string wireKey, out ComponentBase component)
	{
		component = null;
		if (!TryParseWireKey(wireKey, out var typeName, out var typeIndex) || !_slotsByType.TryGetValue(typeName, out var value) || typeIndex < 0 || typeIndex >= value.Count)
		{
			return false;
		}
		return TryGetActiveComponent(value[typeIndex], out component);
	}

	internal bool TryGetRuntimeByWireKey(string wireKey, out CharacterComponentRuntime runtime)
	{
		runtime = null;
		if (!TryParseWireKey(wireKey, out var typeName, out var typeIndex) || !_slotsByType.TryGetValue(typeName, out var value) || typeIndex < 0 || typeIndex >= value.Count)
		{
			return false;
		}
		return TryGetActiveRuntime(value[typeIndex], out runtime);
	}

	internal bool TryGetRuntimeByInstanceId(string instanceId, out CharacterComponentRuntime runtime)
	{
		if (!string.IsNullOrEmpty(instanceId) && _runtimeByInstanceId.TryGetValue(instanceId, out runtime) && !runtime.IsReleased)
		{
			return true;
		}
		runtime = null;
		return false;
	}

	internal bool TryGetRuntimeByLegacyNodeName(string nodeName, out CharacterComponentRuntime runtime)
	{
		if (!string.IsNullOrEmpty(nodeName))
		{
			for (int i = 0; i < _resourceComponents.Count; i++)
			{
				CharacterComponentRuntime characterComponentRuntime = _resourceComponents[i];
				if (characterComponentRuntime.IsReleased)
				{
					continue;
				}
				Array<StringName> array = characterComponentRuntime.ComponentDefinition?.LegacyNodeNames;
				if (array == null)
				{
					continue;
				}
				for (int j = 0; j < array.Count; j++)
				{
					if (string.Equals(array[j].ToString(), nodeName, StringComparison.Ordinal))
					{
						runtime = characterComponentRuntime;
						return true;
					}
				}
			}
		}
		runtime = null;
		return false;
	}

	private void RegisterWireSlot(ComponentBase component, string typeName)
	{
		if (!_registrationByComponent.ContainsKey(component))
		{
			if (!_slotsByType.TryGetValue(typeName, out var value))
			{
				value = new List<ComponentRegistration>();
				_slotsByType[typeName] = value;
			}
			ulong instanceId = GetInstanceId();
			ComponentRegistration componentRegistration = null;
			if (component.TryGetWireSlotToken(instanceId, typeName, out var typeIndex) && typeIndex >= 0 && typeIndex < value.Count && !TryGetActiveComponent(value[typeIndex], out var _) && !TryGetActiveRuntime(value[typeIndex], out var _))
			{
				componentRegistration = value[typeIndex];
			}
			if (componentRegistration == null)
			{
				componentRegistration = new ComponentRegistration
				{
					TypeName = typeName,
					TypeIndex = value.Count,
					WireKey = BuildWireKey(typeName, value.Count)
				};
				value.Add(componentRegistration);
			}
			componentRegistration.ActiveComponent = new WeakReference<ComponentBase>(component);
			_registrationByComponent[component] = componentRegistration;
			component.SetWireSlotToken(instanceId, typeName, componentRegistration.TypeIndex);
		}
	}

	private bool RegisterRuntimeWireSlot(CharacterComponentRuntime runtime, CharacterComponentDefinition definition, string typeName)
	{
		string instanceId = definition?.InstanceId?.Trim();
		return RegisterRuntimeWireSlot(runtime, definition, typeName, instanceId, definition?.WireIndex ?? (-1), null, 1);
	}

	private bool RegisterRuntimeWireSlot(CharacterComponentRuntime runtime, CharacterComponentDefinition definition, string typeName, string instanceId, int wireIndex, string wireKey, int wireSlotCapacityHint)
	{
		if (runtime == null || definition == null || _registrationByRuntime.ContainsKey(runtime))
		{
			return false;
		}
		if (!_slotsByType.TryGetValue(typeName, out var value))
		{
			value = new List<ComponentRegistration>(Math.Max(1, wireSlotCapacityHint));
			_slotsByType[typeName] = value;
		}
		else
		{
			value.EnsureCapacity(Math.Max(value.Count, wireSlotCapacityHint));
		}
		int num = wireIndex;
		ComponentBase component;
		CharacterComponentRuntime runtime2;
		if (num < 0)
		{
			for (int i = 0; i < value.Count; i++)
			{
				ComponentRegistration componentRegistration = value[i];
				if (string.Equals(componentRegistration.InstanceId, instanceId, StringComparison.Ordinal) && !TryGetActiveComponent(componentRegistration, out component) && !TryGetActiveRuntime(componentRegistration, out runtime2))
				{
					num = i;
					break;
				}
			}
			if (num < 0)
			{
				num = value.Count;
			}
		}
		while (value.Count <= num)
		{
			value.Add(new ComponentRegistration
			{
				TypeName = typeName,
				TypeIndex = value.Count,
				WireKey = BuildWireKey(typeName, value.Count)
			});
		}
		ComponentRegistration componentRegistration2 = value[num];
		if (!string.IsNullOrEmpty(componentRegistration2.InstanceId) && !string.Equals(componentRegistration2.InstanceId, instanceId, StringComparison.Ordinal))
		{
			GD.PushError($"Character component wire slot '{componentRegistration2.WireKey}' belongs to InstanceId '{componentRegistration2.InstanceId}', not '{instanceId}'.");
			return false;
		}
		if (TryGetActiveComponent(componentRegistration2, out component) || TryGetActiveRuntime(componentRegistration2, out runtime2))
		{
			GD.PushError("Character component wire slot '" + componentRegistration2.WireKey + "' is already occupied.");
			return false;
		}
		componentRegistration2.ActiveRuntime = runtime;
		componentRegistration2.InstanceId = instanceId;
		componentRegistration2.DefinitionId = definition.DefinitionId;
		componentRegistration2.WireKey = (string.IsNullOrEmpty(wireKey) ? BuildWireKey(typeName, num) : wireKey);
		_registrationByRuntime[runtime] = componentRegistration2;
		return true;
	}

	private static string BuildWireKey(string typeName, int typeIndex)
	{
		if (typeIndex != 0)
		{
			return $"{typeName}#{typeIndex}";
		}
		return typeName;
	}

	private void DeactivateWireSlot(ComponentBase component)
	{
		if (_registrationByComponent.Remove(component, out var value) && TryGetActiveComponent(value, out var component2) && component2 == component)
		{
			value.ActiveComponent = null;
		}
	}

	private void DeactivateRuntimeWireSlot(CharacterComponentRuntime runtime)
	{
		if (runtime != null && _registrationByRuntime.Remove(runtime, out var value) && value.ActiveRuntime == runtime)
		{
			value.ActiveRuntime = null;
		}
	}

	private static bool TryGetActiveComponent(ComponentRegistration registration, out ComponentBase component)
	{
		component = null;
		if (registration?.ActiveComponent != null && registration.ActiveComponent.TryGetTarget(out component))
		{
			return GodotObject.IsInstanceValid(component);
		}
		return false;
	}

	private static bool TryGetActiveRuntime(ComponentRegistration registration, out CharacterComponentRuntime runtime)
	{
		runtime = registration?.ActiveRuntime;
		if (runtime != null)
		{
			return !runtime.IsReleased;
		}
		return false;
	}

	internal static bool TryParseWireKey(string wireKey, out string typeName, out int typeIndex)
	{
		typeName = string.Empty;
		typeIndex = 0;
		if (string.IsNullOrEmpty(wireKey))
		{
			return false;
		}
		int num = wireKey.LastIndexOf('#');
		if (num < 0)
		{
			typeName = ResolveLegacyComponentName(wireKey);
			return true;
		}
		if (num != 0 && num != wireKey.Length - 1)
		{
			int num2 = num + 1;
			if (int.TryParse(wireKey.Substring(num2, wireKey.Length - num2), out typeIndex) && typeIndex >= 0)
			{
				typeName = ResolveLegacyComponentName(wireKey.Substring(0, num));
				return true;
			}
		}
		return false;
	}

	internal void RefreshRuntimeStateMachineRegistration(CharacterComponentRuntime runtime)
	{
		RefreshRuntimeStateMachineRegistration(runtime, refreshOwner: true);
	}

	internal void RefreshRuntimePhysicsRegistration(CharacterComponentRuntime runtime)
	{
		RefreshRuntimePhysicsRegistration(runtime, refreshOwner: false);
	}

	private void RefreshRuntimePhysicsRegistration(CharacterComponentRuntime runtime, bool refreshOwner)
	{
		if (runtime == null || runtime.IsReleased || !_registrationByRuntime.ContainsKey(runtime) || !runtime.HasRuntimePhysicsWork)
		{
			RemovePhysicsRuntime(runtime);
		}
		else if (_physicsRuntimeIndices.TryAdd(runtime, _physicsRuntimes.Count))
		{
			_physicsRuntimes.Add(runtime);
		}
		if (refreshOwner)
		{
			RefreshOwnerProcessMembership();
		}
	}

	private void RemovePhysicsRuntime(CharacterComponentRuntime runtime)
	{
		if (runtime != null && _physicsRuntimeIndices.TryGetValue(runtime, out var value))
		{
			RemovePhysicsRuntimeAtSwap(value);
		}
	}

	private void RemovePhysicsRuntimeAtSwap(int index)
	{
		int num = _physicsRuntimes.Count - 1;
		CharacterComponentRuntime key = _physicsRuntimes[index];
		if (index != num)
		{
			CharacterComponentRuntime characterComponentRuntime = _physicsRuntimes[num];
			_physicsRuntimes[index] = characterComponentRuntime;
			_physicsRuntimeIndices[characterComponentRuntime] = index;
		}
		_physicsRuntimes.RemoveAt(num);
		_physicsRuntimeIndices.Remove(key);
	}

	private void RefreshRuntimeInputRegistration(CharacterComponentRuntime runtime, bool refreshOwner)
	{
		if (runtime != null && _registrationByRuntime.TryGetValue(runtime, out var value))
		{
			bool hasRuntimeInputWork = runtime.HasRuntimeInputWork;
			if (value.HasInputWork != hasRuntimeInputWork)
			{
				_inputRuntimeCount += (hasRuntimeInputWork ? 1 : (-1));
				value.HasInputWork = hasRuntimeInputWork;
			}
			bool hasRuntimeUnhandledInputWork = runtime.HasRuntimeUnhandledInputWork;
			if (value.HasUnhandledInputWork != hasRuntimeUnhandledInputWork)
			{
				_unhandledInputRuntimeCount += (hasRuntimeUnhandledInputWork ? 1 : (-1));
				value.HasUnhandledInputWork = hasRuntimeUnhandledInputWork;
			}
		}
		if (refreshOwner)
		{
			RefreshOwnerProcessMembership();
		}
	}

	private void ClearRuntimeInputWorkRegistration(CharacterComponentRuntime runtime)
	{
		if (runtime != null && _registrationByRuntime.TryGetValue(runtime, out var value))
		{
			if (value.HasInputWork)
			{
				value.HasInputWork = false;
				_inputRuntimeCount--;
			}
			if (value.HasUnhandledInputWork)
			{
				value.HasUnhandledInputWork = false;
				_unhandledInputRuntimeCount--;
			}
		}
	}

	private void RefreshRuntimeStateMachineRegistration(CharacterComponentRuntime runtime, bool refreshOwner)
	{
		if (runtime == null || runtime.IsReleased || !_registrationByRuntime.ContainsKey(runtime) || !runtime.HasStateMachine)
		{
			RemoveStatefulRuntime(runtime);
		}
		else if (_statefulRuntimeIndices.TryAdd(runtime, _statefulRuntimes.Count))
		{
			_statefulRuntimes.Add(runtime);
		}
		RefreshRuntimeStateMachineWorkRegistration(runtime);
		if (refreshOwner)
		{
			RefreshOwnerProcessMembership();
		}
	}

	private void RemoveStatefulRuntime(CharacterComponentRuntime runtime)
	{
		ClearRuntimeStateMachineWorkRegistration(runtime);
		if (runtime != null && _statefulRuntimeIndices.TryGetValue(runtime, out var value))
		{
			RemoveStatefulRuntimeAtSwap(value);
		}
	}

	private void RemoveStatefulRuntimeAtSwap(int index)
	{
		int num = _statefulRuntimes.Count - 1;
		CharacterComponentRuntime characterComponentRuntime = _statefulRuntimes[index];
		ClearRuntimeStateMachineWorkRegistration(characterComponentRuntime);
		if (index != num)
		{
			CharacterComponentRuntime characterComponentRuntime2 = _statefulRuntimes[num];
			_statefulRuntimes[index] = characterComponentRuntime2;
			_statefulRuntimeIndices[characterComponentRuntime2] = index;
		}
		_statefulRuntimes.RemoveAt(num);
		_statefulRuntimeIndices.Remove(characterComponentRuntime);
	}

	internal void RefreshRuntimeStateMachineWorkRegistration(CharacterComponentRuntime runtime)
	{
		if (runtime != null && _registrationByRuntime.TryGetValue(runtime, out var value))
		{
			bool hasStateMachineProcessWork = runtime.HasStateMachineProcessWork;
			if (value.HasProcessWork != hasStateMachineProcessWork)
			{
				_processStateMachineCount += (hasStateMachineProcessWork ? 1 : (-1));
				value.HasProcessWork = hasStateMachineProcessWork;
			}
			bool hasStateMachinePhysicsWork = runtime.HasStateMachinePhysicsWork;
			if (value.HasPhysicsWork != hasStateMachinePhysicsWork)
			{
				UpdatePhysicsStatefulRuntimeMembership(runtime, value, hasStateMachinePhysicsWork);
				_physicsStateMachineCount += (hasStateMachinePhysicsWork ? 1 : (-1));
				value.HasPhysicsWork = hasStateMachinePhysicsWork;
			}
		}
	}

	private void ClearRuntimeStateMachineWorkRegistration(CharacterComponentRuntime runtime)
	{
		if (runtime != null && _registrationByRuntime.TryGetValue(runtime, out var value))
		{
			if (value.HasProcessWork)
			{
				value.HasProcessWork = false;
				_processStateMachineCount--;
			}
			if (value.HasPhysicsWork)
			{
				UpdatePhysicsStatefulRuntimeMembership(runtime, value, hasPhysicsWork: false);
				value.HasPhysicsWork = false;
				_physicsStateMachineCount--;
			}
		}
	}

	private void UpdatePhysicsStatefulRuntimeMembership(CharacterComponentRuntime runtime, ComponentRegistration registration, bool hasPhysicsWork)
	{
		if (runtime == null || registration == null)
		{
			return;
		}
		if (hasPhysicsWork)
		{
			if (registration.PhysicsStatefulIndex < 0)
			{
				if (_physicsStatefulRuntimes == null)
				{
					_physicsStatefulRuntimes = new List<CharacterComponentRuntime>();
				}
				registration.PhysicsStatefulIndex = _physicsStatefulRuntimes.Count;
				_physicsStatefulRuntimes.Add(runtime);
			}
		}
		else if (registration.PhysicsStatefulIndex >= 0)
		{
			RemovePhysicsStatefulRuntimeAtSwap(registration.PhysicsStatefulIndex);
		}
	}

	private void RemovePhysicsStatefulRuntimeAtSwap(int index)
	{
		int num = _physicsStatefulRuntimes.Count - 1;
		CharacterComponentRuntime key = _physicsStatefulRuntimes[index];
		ComponentRegistration componentRegistration = _registrationByRuntime[key];
		if (index != num)
		{
			CharacterComponentRuntime characterComponentRuntime = _physicsStatefulRuntimes[num];
			_physicsStatefulRuntimes[index] = characterComponentRuntime;
			_registrationByRuntime[characterComponentRuntime].PhysicsStatefulIndex = index;
		}
		_physicsStatefulRuntimes.RemoveAt(num);
		componentRegistration.PhysicsStatefulIndex = -1;
	}

	private void RemoveStatefulComponent(ComponentBase component)
	{
		ClearStateMachineWorkRegistration(component);
		if (component != null && _statefulIndices.TryGetValue(component, out var value))
		{
			RemoveStatefulAtSwap(value);
		}
	}

	private void RemoveStatefulAtSwap(int index)
	{
		int num = _statefulComponents.Count - 1;
		ComponentBase componentBase = _statefulComponents[index];
		ClearStateMachineWorkRegistration(componentBase);
		if (index != num)
		{
			ComponentBase componentBase2 = _statefulComponents[num];
			_statefulComponents[index] = componentBase2;
			_statefulIndices[componentBase2] = index;
		}
		_statefulComponents.RemoveAt(num);
		_statefulIndices.Remove(componentBase);
	}

	private void RefreshStateMachineWorkRegistration(ComponentBase component)
	{
		if (component != null && _registrationByComponent.TryGetValue(component, out var value))
		{
			bool hasStateMachineProcessWork = component.HasStateMachineProcessWork;
			if (value.HasProcessWork != hasStateMachineProcessWork)
			{
				_processStateMachineCount += (hasStateMachineProcessWork ? 1 : (-1));
				value.HasProcessWork = hasStateMachineProcessWork;
			}
			bool hasStateMachinePhysicsWork = component.HasStateMachinePhysicsWork;
			if (value.HasPhysicsWork != hasStateMachinePhysicsWork)
			{
				UpdatePhysicsStatefulComponentMembership(component, value, hasStateMachinePhysicsWork);
				_physicsStateMachineCount += (hasStateMachinePhysicsWork ? 1 : (-1));
				value.HasPhysicsWork = hasStateMachinePhysicsWork;
			}
		}
	}

	private void ClearStateMachineWorkRegistration(ComponentBase component)
	{
		if (component != null && _registrationByComponent.TryGetValue(component, out var value))
		{
			if (value.HasProcessWork)
			{
				value.HasProcessWork = false;
				_processStateMachineCount--;
			}
			if (value.HasPhysicsWork)
			{
				UpdatePhysicsStatefulComponentMembership(component, value, hasPhysicsWork: false);
				value.HasPhysicsWork = false;
				_physicsStateMachineCount--;
			}
		}
	}

	private void UpdatePhysicsStatefulComponentMembership(ComponentBase component, ComponentRegistration registration, bool hasPhysicsWork)
	{
		if (component == null || registration == null)
		{
			return;
		}
		if (hasPhysicsWork)
		{
			if (registration.PhysicsStatefulIndex < 0)
			{
				if (_physicsStatefulComponents == null)
				{
					_physicsStatefulComponents = new List<ComponentBase>();
				}
				registration.PhysicsStatefulIndex = _physicsStatefulComponents.Count;
				_physicsStatefulComponents.Add(component);
			}
		}
		else if (registration.PhysicsStatefulIndex >= 0)
		{
			RemovePhysicsStatefulComponentAtSwap(registration.PhysicsStatefulIndex);
		}
	}

	private void RemovePhysicsStatefulComponentAtSwap(int index)
	{
		int num = _physicsStatefulComponents.Count - 1;
		ComponentBase key = _physicsStatefulComponents[index];
		ComponentRegistration componentRegistration = _registrationByComponent[key];
		if (index != num)
		{
			ComponentBase componentBase = _physicsStatefulComponents[num];
			_physicsStatefulComponents[index] = componentBase;
			_registrationByComponent[componentBase].PhysicsStatefulIndex = index;
		}
		_physicsStatefulComponents.RemoveAt(num);
		componentRegistration.PhysicsStatefulIndex = -1;
	}

	private void RefreshOwnerProcessMembership()
	{
		if (!_isExitingTree)
		{
			RefreshInputProcessMode();
			if (parent is TowerDefenseCharacter towerDefenseCharacter && GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				towerDefenseCharacter.RefreshComponentStateMachineDispatch();
			}
		}
	}

	private void RefreshInputProcessMode()
	{
		TowerDefenseCharacter owner = Owner;
		bool flag = IsOwnerInsideTree && GodotObject.IsInstanceValid(owner) && owner.HasValidRuntimeConfiguration && owner.inGame && !owner.editorPreviewMode && !owner.isDestroy;
		if (GodotObject.IsInstanceValid(owner))
		{
			owner.SetProcessInput(flag && HasRuntimeInputWork);
			owner.SetProcessUnhandledInput(flag && HasRuntimeUnhandledInputWork);
		}
	}

	private static string GetComponentTypeName(ComponentBase component)
	{
		string text = component._GetName();
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		return component.GetType().Name;
	}

	public ComponentBase GetComponentFromName(string _name)
	{
		ComponentBase componentBase = FindComponentByNodeName(_name);
		if (GodotObject.IsInstanceValid(componentBase))
		{
			return componentBase;
		}
		string text = ResolveLegacyComponentName(_name);
		if (!(text == _name))
		{
			return FindComponentByNodeName(text);
		}
		return null;
	}

	public ComponentBase GetComponentFromType(string _type, int id = 0)
	{
		if (id < 0)
		{
			return null;
		}
		_type = ResolveLegacyComponentName(_type);
		if (componentDictionary.TryGetValue(_type, out var value))
		{
			for (int num = value.Count - 1; num >= 0; num--)
			{
				if (!GodotObject.IsInstanceValid(value[num]))
				{
					value.RemoveAt(num);
				}
			}
			if (id < value.Count)
			{
				return value[id];
			}
		}
		return null;
	}

	private ComponentBase FindComponentByNodeName(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return null;
		}
		for (int i = 0; i < componentList.Count; i++)
		{
			ComponentBase componentBase = componentList[i];
			if (GodotObject.IsInstanceValid(componentBase) && componentBase.Name == (StringName)name)
			{
				return componentBase;
			}
		}
		return null;
	}

	internal static string ResolveLegacyComponentName(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return name;
		}
		if (LegacyComponentAliases.TryGetValue(name, out var value))
		{
			return value;
		}
		foreach (KeyValuePair<string, string> legacyComponentAlias in LegacyComponentAliases)
		{
			if (!name.StartsWith(legacyComponentAlias.Key, StringComparison.Ordinal) || name.Length == legacyComponentAlias.Key.Length)
			{
				continue;
			}
			bool flag = true;
			for (int i = legacyComponentAlias.Key.Length; i < name.Length; i++)
			{
				if (!char.IsAsciiDigit(name[i]))
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				string value2 = legacyComponentAlias.Value;
				int length = legacyComponentAlias.Key.Length;
				return value2 + name.Substring(length, name.Length - length);
			}
		}
		return name;
	}

	internal void NotifyOwnerGameplayActivatedLegacyFullScanForTests()
	{
		for (int i = 0; i < _resourceComponents.Count; i++)
		{
			_resourceComponents[i].OwnerGameplayActivated();
		}
	}
}
