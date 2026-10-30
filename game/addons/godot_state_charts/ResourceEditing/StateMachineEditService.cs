using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public sealed class StateMachineEditService
{
	private sealed class StateSnapshot
	{
		public StateMachineStateDefinition ResourceInstance;

		public string StableId;

		public StringName DisplayName;

		public StateMachineStateKind Kind;

		public string ParentId;

		public string InitialChildId;

		public StateMachineProcessFlags ProcessFlags;

		public StringName CallbackKey;

		public StringName EnterCallbackKey;

		public StringName ExitCallbackKey;

		public StringName ProcessCallbackKey;

		public StringName PhysicsProcessCallbackKey;
	}

	private sealed class TransitionSnapshot
	{
		public StateMachineTransitionDefinition ResourceInstance;

		public string StableId;

		public string SourceStateId;

		public string TargetStateId;

		public StateMachineTriggerKind TriggerKind;

		public StringName EventName;

		public double DelaySeconds;

		public int Priority;

		public int DeclarationOrder;

		public Resource GuardDefinition;
	}

	private sealed class DefinitionSnapshot
	{
		public int SchemaVersion;

		public string DefinitionId;

		public StateMachineDefinition BaseDefinition;

		public string RootStateId;

		public readonly List<StateSnapshot> States = new List<StateSnapshot>();

		public readonly List<TransitionSnapshot> Transitions = new List<TransitionSnapshot>();

		public readonly List<KeyValuePair<string, string>> Aliases = new List<KeyValuePair<string, string>>();
	}

	private readonly Stack<StateMachineEditCommand> _undo = new Stack<StateMachineEditCommand>();

	private readonly Stack<StateMachineEditCommand> _redo = new Stack<StateMachineEditCommand>();

	private readonly IStateMachineStableIdProvider _stableIdProvider;

	private readonly bool _executeImmediately;

	private readonly bool _usesDefaultStableIdProvider;

	public int UndoCount => _undo.Count;

	public int RedoCount => _redo.Count;

	public StateMachineEditService(IStateMachineStableIdProvider stableIdProvider = null, bool executeImmediately = true)
	{
		_usesDefaultStableIdProvider = stableIdProvider == null;
		_stableIdProvider = stableIdProvider ?? new GuidStateMachineStableIdProvider();
		_executeImmediately = executeImmediately;
	}

	public StateMachineEditCommand AddState(StateMachineDefinition definition, string displayName, StateMachineStateKind kind = StateMachineStateKind.Atomic, string parentId = "", string stableIdScope = "state", string authorTypeId = "")
	{
		ArgumentNullException.ThrowIfNull(definition, "definition");
		if (!string.IsNullOrWhiteSpace(parentId) && FindVisibleState(definition, parentId) == null)
		{
			throw new ArgumentException("Parent state '" + parentId + "' does not exist.", "parentId");
		}
		string text = CreateEditorStableId(definition, stableIdScope);
		DefinitionSnapshot definitionSnapshot = CaptureDefinition(definition);
		StateMachineDefinition stateMachineDefinition = CreateDefinition(definitionSnapshot);
		StateMachineStateDefinition stateMachineStateDefinition = CreateAuthoredState(authorTypeId);
		stateMachineStateDefinition.StableId = text;
		stateMachineStateDefinition.DisplayName = new StringName(displayName ?? string.Empty);
		stateMachineStateDefinition.Kind = kind;
		stateMachineStateDefinition.ParentId = parentId ?? string.Empty;
		stateMachineStateDefinition.InitialChildId = string.Empty;
		stateMachineDefinition.States.Add(stateMachineStateDefinition);
		if (stateMachineDefinition.States.Count == 1 && string.IsNullOrWhiteSpace(stateMachineDefinition.RootStateId))
		{
			stateMachineDefinition.RootStateId = text;
		}
		return ExecuteDefinitionCommand(definition, definitionSnapshot, CaptureDefinition(stateMachineDefinition), text);
	}

	public StateMachineEditCommand AddStateHierarchySafe(StateMachineDefinition definition, string displayName, StateMachineStateKind kind, string parentId, out string stableId, out string defaultChildId, string stableIdScope = "state", string authorTypeId = "")
	{
		ArgumentNullException.ThrowIfNull(definition, "definition");
		stableId = CreateEditorStableId(definition, stableIdScope);
		defaultChildId = string.Empty;
		DefinitionSnapshot definitionSnapshot = CaptureDefinition(definition);
		StateMachineDefinition stateMachineDefinition = CreateDefinition(definitionSnapshot);
		string text = parentId ?? string.Empty;
		StateMachineDefinition stateMachineDefinition2 = ComposeOrThrow(stateMachineDefinition);
		if ((stateMachineDefinition2?.States?.Count).GetValueOrDefault() > 0 && string.IsNullOrWhiteSpace(text))
		{
			text = stateMachineDefinition2.RootStateId ?? string.Empty;
		}
		StateMachineStateDefinition stateMachineStateDefinition = (string.IsNullOrWhiteSpace(text) ? null : EnsureLocalStateOverride(stateMachineDefinition, text));
		if (!string.IsNullOrWhiteSpace(text) && stateMachineStateDefinition == null)
		{
			throw new ArgumentException("Parent state '" + text + "' does not exist.", "parentId");
		}
		if (kind == StateMachineStateKind.History && (stateMachineStateDefinition == null || stateMachineStateDefinition.Kind != StateMachineStateKind.Compound))
		{
			throw new InvalidOperationException("History states require a Compound parent.");
		}
		if (stateMachineStateDefinition != null && stateMachineStateDefinition.Kind == StateMachineStateKind.Atomic)
		{
			if (!string.Equals(stateMachineStateDefinition.StableId, stateMachineDefinition.RootStateId, StringComparison.Ordinal))
			{
				throw new InvalidOperationException("Only Compound or Parallel states can contain child states.");
			}
			stateMachineStateDefinition.Kind = StateMachineStateKind.Compound;
		}
		StateMachineStateDefinition stateMachineStateDefinition2 = CreateAuthoredState(authorTypeId);
		stateMachineStateDefinition2.StableId = stableId;
		stateMachineStateDefinition2.DisplayName = new StringName(displayName ?? string.Empty);
		stateMachineStateDefinition2.Kind = kind;
		stateMachineStateDefinition2.ParentId = text;
		stateMachineStateDefinition2.InitialChildId = string.Empty;
		stateMachineDefinition.States.Add(stateMachineStateDefinition2);
		if (stateMachineDefinition.States.Count == 1 && string.IsNullOrWhiteSpace(stateMachineDefinition.RootStateId))
		{
			stateMachineDefinition.RootStateId = stableId;
		}
		if (stateMachineStateDefinition != null && stateMachineStateDefinition.Kind == StateMachineStateKind.Compound && string.IsNullOrWhiteSpace(stateMachineStateDefinition.InitialChildId))
		{
			stateMachineStateDefinition.InitialChildId = stableId;
		}
		if ((uint)(kind - 1) <= 1u)
		{
			defaultChildId = CreateEditorStableId(stateMachineDefinition, stableIdScope + ".child");
			stateMachineDefinition.States.Add(new StateMachineStateDefinition
			{
				StableId = defaultChildId,
				DisplayName = "初始子状态",
				Kind = StateMachineStateKind.Atomic,
				ParentId = stableId
			});
			if (kind == StateMachineStateKind.Compound)
			{
				stateMachineStateDefinition2.InitialChildId = defaultChildId;
			}
		}
		return ExecuteDefinitionCommand(definition, definitionSnapshot, CaptureDefinition(stateMachineDefinition), (!string.IsNullOrWhiteSpace(defaultChildId)) ? new string[2] { stableId, defaultChildId } : new string[1] { stableId });
	}

	public StateMachineEditCommand RemoveState(StateMachineDefinition definition, string stableId, string retargetStateId = "")
	{
		ArgumentNullException.ThrowIfNull(definition, "definition");
		if (string.Equals(definition.RootStateId, stableId, StringComparison.Ordinal))
		{
			throw new InvalidOperationException("The root state cannot be removed.");
		}
		if (FindState(definition, stableId) == null)
		{
			if (FindVisibleState(definition, stableId) != null)
			{
				throw new InvalidOperationException("Inherited states cannot be removed from a derived definition.");
			}
			throw new ArgumentException("State '" + stableId + "' does not exist.", "stableId");
		}
		DefinitionSnapshot definitionSnapshot = CaptureDefinition(definition);
		StateMachineDefinition stateMachineDefinition = CreateDefinition(definitionSnapshot);
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal) { stableId };
		bool flag;
		do
		{
			flag = false;
			for (int i = 0; i < stateMachineDefinition.States.Count; i++)
			{
				StateMachineStateDefinition stateMachineStateDefinition = stateMachineDefinition.States[i];
				if (stateMachineStateDefinition != null && hashSet.Contains(stateMachineStateDefinition.ParentId) && hashSet.Add(stateMachineStateDefinition.StableId))
				{
					flag = true;
				}
			}
		}
		while (flag);
		bool flag2 = !string.IsNullOrWhiteSpace(retargetStateId) && !hashSet.Contains(retargetStateId) && FindVisibleState(stateMachineDefinition, retargetStateId) != null;
		for (int num = stateMachineDefinition.Transitions.Count - 1; num >= 0; num--)
		{
			StateMachineTransitionDefinition stateMachineTransitionDefinition = stateMachineDefinition.Transitions[num];
			bool flag3 = stateMachineTransitionDefinition != null && hashSet.Contains(stateMachineTransitionDefinition.SourceStateId);
			bool flag4 = stateMachineTransitionDefinition != null && hashSet.Contains(stateMachineTransitionDefinition.TargetStateId);
			if (flag3 || flag4)
			{
				if (!flag2)
				{
					stateMachineDefinition.Transitions.RemoveAt(num);
				}
				else
				{
					if (flag3)
					{
						stateMachineTransitionDefinition.SourceStateId = retargetStateId;
					}
					if (flag4)
					{
						stateMachineTransitionDefinition.TargetStateId = retargetStateId;
					}
				}
			}
		}
		for (int j = 0; j < stateMachineDefinition.States.Count; j++)
		{
			StateMachineStateDefinition stateMachineStateDefinition2 = stateMachineDefinition.States[j];
			if (stateMachineStateDefinition2 == null || !hashSet.Contains(stateMachineStateDefinition2.InitialChildId))
			{
				continue;
			}
			StateMachineStateDefinition stateMachineStateDefinition3 = (flag2 ? FindVisibleState(stateMachineDefinition, retargetStateId) : null);
			if (stateMachineStateDefinition3 != null && string.Equals(stateMachineStateDefinition3.ParentId, stateMachineStateDefinition2.StableId, StringComparison.Ordinal))
			{
				stateMachineStateDefinition2.InitialChildId = retargetStateId;
				continue;
			}
			stateMachineStateDefinition2.InitialChildId = string.Empty;
			for (int k = 0; k < stateMachineDefinition.States.Count; k++)
			{
				StateMachineStateDefinition stateMachineStateDefinition4 = stateMachineDefinition.States[k];
				if (stateMachineStateDefinition4 != null && !hashSet.Contains(stateMachineStateDefinition4.StableId) && string.Equals(stateMachineStateDefinition4.ParentId, stateMachineStateDefinition2.StableId, StringComparison.Ordinal))
				{
					stateMachineStateDefinition2.InitialChildId = stateMachineStateDefinition4.StableId;
					break;
				}
			}
		}
		for (int num2 = stateMachineDefinition.States.Count - 1; num2 >= 0; num2--)
		{
			if (stateMachineDefinition.States[num2] != null && hashSet.Contains(stateMachineDefinition.States[num2].StableId))
			{
				stateMachineDefinition.States.RemoveAt(num2);
			}
		}
		List<string> list = new List<string>();
		foreach (string key in stateMachineDefinition.Aliases.Keys)
		{
			if (hashSet.Contains(stateMachineDefinition.Aliases[key]))
			{
				list.Add(key);
			}
		}
		for (int l = 0; l < list.Count; l++)
		{
			stateMachineDefinition.Aliases.Remove(list[l]);
		}
		return ExecuteDefinitionCommand(definition, definitionSnapshot, CaptureDefinition(stateMachineDefinition), hashSet);
	}

	public StateMachineEditCommand CreateStateOverride(StateMachineDefinition definition, string stableId)
	{
		ArgumentNullException.ThrowIfNull(definition, "definition");
		if (FindState(definition, stableId) != null)
		{
			throw new InvalidOperationException("State '" + stableId + "' already has a local override.");
		}
		StateMachineStateDefinition source = FindInheritedState(definition, stableId) ?? throw new InvalidOperationException("State '" + stableId + "' is not inherited from the base definition.");
		DefinitionSnapshot definitionSnapshot = CaptureDefinition(definition);
		StateMachineDefinition stateMachineDefinition = CreateDefinition(definitionSnapshot);
		stateMachineDefinition.States.Add(CloneState(source));
		return ExecuteDefinitionCommand(definition, definitionSnapshot, CaptureDefinition(stateMachineDefinition), stableId);
	}

	public StateMachineEditCommand RestoreInheritedState(StateMachineDefinition definition, string stableId)
	{
		ArgumentNullException.ThrowIfNull(definition, "definition");
		if (FindInheritedState(definition, stableId) == null)
		{
			throw new InvalidOperationException("State '" + stableId + "' is not inherited from the base definition.");
		}
		if (FindState(definition, stableId) == null)
		{
			throw new InvalidOperationException("State '" + stableId + "' does not have a local override.");
		}
		DefinitionSnapshot definitionSnapshot = CaptureDefinition(definition);
		StateMachineDefinition stateMachineDefinition = CreateDefinition(definitionSnapshot);
		int index = FindStateIndex(stateMachineDefinition, stableId);
		stateMachineDefinition.States.RemoveAt(index);
		EnsureValidRestoredDefinition(stateMachineDefinition, "state '" + stableId + "'");
		return ExecuteDefinitionCommand(definition, definitionSnapshot, CaptureDefinition(stateMachineDefinition), stableId);
	}

	public StateMachineEditCommand RemoveStates(StateMachineDefinition definition, IEnumerable<string> stableIds)
	{
		ArgumentNullException.ThrowIfNull(definition, "definition");
		HashSet<string> hashSet = new HashSet<string>(stableIds ?? System.Array.Empty<string>(), StringComparer.Ordinal);
		hashSet.RemoveWhere(string.IsNullOrWhiteSpace);
		if (hashSet.Count == 0)
		{
			throw new ArgumentException("At least one state is required.", "stableIds");
		}
		foreach (string item in hashSet)
		{
			if (string.Equals(definition.RootStateId, item, StringComparison.Ordinal))
			{
				throw new InvalidOperationException("The root state cannot be removed.");
			}
			if (FindState(definition, item) == null && FindVisibleState(definition, item) != null)
			{
				throw new InvalidOperationException("Inherited states cannot be removed from a derived definition.");
			}
		}
		DefinitionSnapshot definitionSnapshot = CaptureDefinition(definition);
		StateMachineDefinition definition2 = CreateDefinition(definitionSnapshot);
		HashSet<string> hashSet2 = new HashSet<string>(StringComparer.Ordinal);
		StateMachineEditService stateMachineEditService = new StateMachineEditService(_stableIdProvider);
		foreach (string item2 in hashSet)
		{
			if (FindState(definition2, item2) == null)
			{
				continue;
			}
			foreach (string affectedStableId in stateMachineEditService.RemoveState(definition2, item2).AffectedStableIds)
			{
				hashSet2.Add(affectedStableId);
			}
		}
		if (hashSet2.Count == 0)
		{
			throw new ArgumentException("None of the selected states can be removed.", "stableIds");
		}
		return ExecuteDefinitionCommand(definition, definitionSnapshot, CaptureDefinition(definition2), hashSet2);
	}

	public StateMachineEditCommand RenameState(StateMachineDefinition definition, string stableId, string displayName)
	{
		ArgumentNullException.ThrowIfNull(definition, "definition");
		DefinitionSnapshot definitionSnapshot = CaptureDefinition(definition);
		StateMachineDefinition definition2 = CreateDefinition(definitionSnapshot);
		(EnsureLocalStateOverride(definition2, stableId) ?? throw new ArgumentException("State '" + stableId + "' does not exist.", "stableId")).DisplayName = new StringName(displayName ?? string.Empty);
		return ExecuteDefinitionCommand(definition, definitionSnapshot, CaptureDefinition(definition2), stableId);
	}

	public StateMachineEditCommand SetInitialState(StateMachineDefinition definition, string compoundStateId, string initialChildId)
	{
		ArgumentNullException.ThrowIfNull(definition, "definition");
		DefinitionSnapshot definitionSnapshot = CaptureDefinition(definition);
		StateMachineDefinition definition2 = CreateDefinition(definitionSnapshot);
		StateMachineStateDefinition stateMachineStateDefinition = EnsureLocalStateOverride(definition2, compoundStateId) ?? throw new ArgumentException("State '" + compoundStateId + "' does not exist.", "compoundStateId");
		StateMachineStateDefinition stateMachineStateDefinition2 = FindVisibleState(definition2, initialChildId) ?? throw new ArgumentException("State '" + initialChildId + "' does not exist.", "initialChildId");
		if (stateMachineStateDefinition.Kind != StateMachineStateKind.Compound || !string.Equals(stateMachineStateDefinition2.ParentId, stateMachineStateDefinition.StableId, StringComparison.Ordinal))
		{
			throw new InvalidOperationException("The initial state must be a direct child of a Compound state.");
		}
		stateMachineStateDefinition.InitialChildId = stateMachineStateDefinition2.StableId;
		return ExecuteDefinitionCommand(definition, definitionSnapshot, CaptureDefinition(definition2), compoundStateId, initialChildId);
	}

	public StateMachineEditCommand SetRootState(StateMachineDefinition definition, string stableId)
	{
		ArgumentNullException.ThrowIfNull(definition, "definition");
		if (string.IsNullOrWhiteSpace(stableId) || FindVisibleState(definition, stableId) == null)
		{
			throw new ArgumentException("State '" + stableId + "' does not exist.", "stableId");
		}
		DefinitionSnapshot definitionSnapshot = CaptureDefinition(definition);
		StateMachineDefinition stateMachineDefinition = CreateDefinition(definitionSnapshot);
		if (string.Equals(stateMachineDefinition.RootStateId, stableId, StringComparison.Ordinal))
		{
			return ExecuteDefinitionCommand(definition, definitionSnapshot, CaptureDefinition(stateMachineDefinition), stableId);
		}
		string text = stateMachineDefinition.RootStateId ?? string.Empty;
		StateMachineStateDefinition stateMachineStateDefinition = EnsureLocalStateOverride(stateMachineDefinition, stableId) ?? throw new ArgumentException("State '" + stableId + "' does not exist.", "stableId");
		StateMachineStateDefinition obj = EnsureLocalStateOverride(stateMachineDefinition, text) ?? throw new InvalidOperationException("The current root state does not exist.");
		string text2 = stateMachineStateDefinition.ParentId ?? string.Empty;
		stateMachineStateDefinition.ParentId = string.Empty;
		obj.ParentId = stableId;
		stateMachineDefinition.RootStateId = stableId;
		List<string> list = new List<string> { stableId, text };
		StateMachineStateKind kind = stateMachineStateDefinition.Kind;
		if ((kind == StateMachineStateKind.Atomic || kind == StateMachineStateKind.History) ? true : false)
		{
			stateMachineStateDefinition.Kind = StateMachineStateKind.Compound;
		}
		if (stateMachineStateDefinition.Kind == StateMachineStateKind.Compound)
		{
			List<StateMachineStateDefinition> children = FindVisibleDirectChildren(stateMachineDefinition, stableId);
			stateMachineStateDefinition.InitialChildId = (FindChildById(children, stateMachineStateDefinition.InitialChildId) ?? FindFirstNonHistoryChild(children))?.StableId ?? text;
		}
		else
		{
			stateMachineStateDefinition.InitialChildId = string.Empty;
		}
		if (!string.IsNullOrWhiteSpace(text2))
		{
			StateMachineStateDefinition stateMachineStateDefinition2 = EnsureLocalStateOverride(stateMachineDefinition, text2);
			bool flag = stateMachineStateDefinition2 != null;
			if (flag)
			{
				kind = stateMachineStateDefinition2.Kind;
				bool flag2 = (uint)(kind - 1) <= 1u;
				flag = flag2;
			}
			if (flag)
			{
				list.Add(text2);
				List<StateMachineStateDefinition> children2 = FindVisibleDirectChildren(stateMachineDefinition, text2);
				StateMachineStateDefinition stateMachineStateDefinition3 = FindChildById(children2, stateMachineStateDefinition2.InitialChildId) ?? FindFirstNonHistoryChild(children2);
				if (stateMachineStateDefinition3 == null)
				{
					string text3 = CreateEditorStableId(stateMachineDefinition, "state.child");
					stateMachineStateDefinition3 = new StateMachineStateDefinition
					{
						StableId = text3,
						DisplayName = "初始子状态",
						Kind = StateMachineStateKind.Atomic,
						ParentId = text2
					};
					stateMachineDefinition.States.Add(stateMachineStateDefinition3);
					list.Add(text3);
				}
				stateMachineStateDefinition2.InitialChildId = ((stateMachineStateDefinition2.Kind == StateMachineStateKind.Compound) ? stateMachineStateDefinition3.StableId : string.Empty);
			}
		}
		return ExecuteDefinitionCommand(definition, definitionSnapshot, CaptureDefinition(stateMachineDefinition), list);
	}

	public StateMachineEditCommand SetStateKind(StateMachineDefinition definition, string stableId, StateMachineStateKind kind)
	{
		ArgumentNullException.ThrowIfNull(definition, "definition");
		DefinitionSnapshot definitionSnapshot = CaptureDefinition(definition);
		StateMachineDefinition stateMachineDefinition = CreateDefinition(definitionSnapshot);
		StateMachineStateDefinition stateMachineStateDefinition = EnsureLocalStateOverride(stateMachineDefinition, stableId) ?? throw new ArgumentException("State '" + stableId + "' does not exist.", "stableId");
		if (stateMachineStateDefinition.Kind == kind)
		{
			return ExecuteDefinitionCommand(definition, definitionSnapshot, CaptureDefinition(stateMachineDefinition), stableId);
		}
		List<StateMachineStateDefinition> list = FindVisibleDirectChildren(stateMachineDefinition, stableId);
		bool flag = ((kind == StateMachineStateKind.Atomic || kind == StateMachineStateKind.History) ? true : false);
		if (flag && list.Count > 0)
		{
			throw new InvalidOperationException("A state with child states cannot become Atomic or History.");
		}
		if (kind == StateMachineStateKind.History)
		{
			if (string.Equals(stateMachineDefinition.RootStateId, stableId, StringComparison.Ordinal))
			{
				throw new InvalidOperationException("The root state cannot become History.");
			}
			StateMachineStateDefinition stateMachineStateDefinition2 = FindVisibleState(stateMachineDefinition, stateMachineStateDefinition.ParentId);
			if (stateMachineStateDefinition2 == null || stateMachineStateDefinition2.Kind != StateMachineStateKind.Compound)
			{
				throw new InvalidOperationException("History states require a Compound parent.");
			}
		}
		if (kind == StateMachineStateKind.Parallel)
		{
			for (int i = 0; i < list.Count; i++)
			{
				StateMachineStateDefinition stateMachineStateDefinition3 = list[i];
				if (stateMachineStateDefinition3 != null && stateMachineStateDefinition3.Kind == StateMachineStateKind.History)
				{
					throw new InvalidOperationException("Parallel states cannot contain History children.");
				}
			}
		}
		stateMachineStateDefinition.Kind = kind;
		List<string> list2 = new List<string> { stableId };
		if ((uint)(kind - 1) <= 1u)
		{
			StateMachineStateDefinition stateMachineStateDefinition4 = FindFirstNonHistoryChild(list);
			if (stateMachineStateDefinition4 == null)
			{
				string text = CreateEditorStableId(stateMachineDefinition, "state.child");
				stateMachineStateDefinition4 = new StateMachineStateDefinition
				{
					StableId = text,
					DisplayName = "初始子状态",
					Kind = StateMachineStateKind.Atomic,
					ParentId = stableId
				};
				stateMachineDefinition.States.Add(stateMachineStateDefinition4);
				list2.Add(text);
			}
			stateMachineStateDefinition.InitialChildId = ((kind == StateMachineStateKind.Compound) ? stateMachineStateDefinition4.StableId : string.Empty);
		}
		else
		{
			stateMachineStateDefinition.InitialChildId = string.Empty;
		}
		return ExecuteDefinitionCommand(definition, definitionSnapshot, CaptureDefinition(stateMachineDefinition), list2);
	}

	public StateMachineEditCommand SetStateParent(StateMachineDefinition definition, string stableId, string parentId)
	{
		ArgumentNullException.ThrowIfNull(definition, "definition");
		StateMachineStateDefinition stateMachineStateDefinition = FindVisibleState(definition, stableId);
		if (stateMachineStateDefinition == null)
		{
			throw new ArgumentException("State '" + stableId + "' does not exist.", "stableId");
		}
		if (parentId == null)
		{
			parentId = string.Empty;
		}
		if (string.Equals(definition.RootStateId, stableId, StringComparison.Ordinal))
		{
			if (!string.IsNullOrWhiteSpace(parentId))
			{
				throw new InvalidOperationException("The root state must remain top-level.");
			}
			return UpdateState(definition, stableId, delegate(StateMachineStateDefinition state)
			{
				state.ParentId = string.Empty;
			});
		}
		if (string.IsNullOrWhiteSpace(parentId))
		{
			throw new InvalidOperationException("Every non-root state requires a Compound or Parallel parent.");
		}
		StateMachineStateDefinition stateMachineStateDefinition2 = (string.IsNullOrWhiteSpace(parentId) ? null : FindVisibleState(definition, parentId));
		if (!string.IsNullOrWhiteSpace(parentId) && stateMachineStateDefinition2 == null)
		{
			throw new ArgumentException("Parent state '" + parentId + "' does not exist.", "parentId");
		}
		if (string.Equals(stableId, parentId, StringComparison.Ordinal))
		{
			throw new InvalidOperationException("A state cannot be its own parent.");
		}
		StateMachineStateKind kind = stateMachineStateDefinition2.Kind;
		if ((uint)(kind - 1) > 1u)
		{
			throw new InvalidOperationException("Only Compound or Parallel states can contain child states.");
		}
		if (stateMachineStateDefinition.Kind == StateMachineStateKind.History && stateMachineStateDefinition2.Kind != StateMachineStateKind.Compound)
		{
			throw new InvalidOperationException("History states require a Compound parent.");
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		while (stateMachineStateDefinition2 != null && !string.IsNullOrWhiteSpace(stateMachineStateDefinition2.StableId))
		{
			if (!hashSet.Add(stateMachineStateDefinition2.StableId))
			{
				throw new InvalidOperationException("The selected parent chain already contains a cycle.");
			}
			if (string.Equals(stateMachineStateDefinition2.StableId, stableId, StringComparison.Ordinal))
			{
				throw new InvalidOperationException("A state cannot be parented under one of its descendants.");
			}
			stateMachineStateDefinition2 = (string.IsNullOrWhiteSpace(stateMachineStateDefinition2.ParentId) ? null : FindVisibleState(definition, stateMachineStateDefinition2.ParentId));
		}
		DefinitionSnapshot definitionSnapshot = CaptureDefinition(definition);
		StateMachineDefinition stateMachineDefinition = CreateDefinition(definitionSnapshot);
		StateMachineStateDefinition stateMachineStateDefinition3 = EnsureLocalStateOverride(stateMachineDefinition, stableId) ?? throw new ArgumentException("State '" + stableId + "' does not exist.", "stableId");
		string text = stateMachineStateDefinition3.ParentId ?? string.Empty;
		if (string.Equals(text, parentId, StringComparison.Ordinal))
		{
			return ExecuteDefinitionCommand(definition, definitionSnapshot, CaptureDefinition(stateMachineDefinition), stableId);
		}
		stateMachineStateDefinition3.ParentId = parentId;
		List<string> list = new List<string> { stableId, parentId };
		StateMachineStateDefinition stateMachineStateDefinition4 = EnsureLocalStateOverride(stateMachineDefinition, parentId);
		if (stateMachineStateDefinition4 != null && stateMachineStateDefinition4.Kind == StateMachineStateKind.Compound && string.IsNullOrWhiteSpace(stateMachineStateDefinition4.InitialChildId))
		{
			stateMachineStateDefinition4.InitialChildId = stableId;
		}
		if (!string.IsNullOrWhiteSpace(text))
		{
			StateMachineStateDefinition stateMachineStateDefinition5 = EnsureLocalStateOverride(stateMachineDefinition, text);
			if (stateMachineStateDefinition5 != null)
			{
				list.Add(text);
				List<StateMachineStateDefinition> children = FindVisibleDirectChildren(stateMachineDefinition, text);
				StateMachineStateDefinition stateMachineStateDefinition6 = FindChildById(children, stateMachineStateDefinition5.InitialChildId) ?? FindFirstNonHistoryChild(children);
				bool flag = stateMachineStateDefinition6 == null;
				if (flag)
				{
					kind = stateMachineStateDefinition5.Kind;
					bool flag2 = (uint)(kind - 1) <= 1u;
					flag = flag2;
				}
				if (flag)
				{
					string text2 = CreateEditorStableId(stateMachineDefinition, "state.child");
					stateMachineStateDefinition6 = new StateMachineStateDefinition
					{
						StableId = text2,
						DisplayName = "初始子状态",
						Kind = StateMachineStateKind.Atomic,
						ParentId = text
					};
					stateMachineDefinition.States.Add(stateMachineStateDefinition6);
					list.Add(text2);
				}
				stateMachineStateDefinition5.InitialChildId = ((stateMachineStateDefinition5.Kind != StateMachineStateKind.Compound) ? string.Empty : (stateMachineStateDefinition6?.StableId ?? string.Empty));
			}
		}
		return ExecuteDefinitionCommand(definition, definitionSnapshot, CaptureDefinition(stateMachineDefinition), list);
	}

	public StateMachineEditCommand SetStateProcessFlags(StateMachineDefinition definition, string stableId, StateMachineProcessFlags flags)
	{
		return UpdateState(definition, stableId, delegate(StateMachineStateDefinition state)
		{
			state.ProcessFlags = flags;
		});
	}

	public StateMachineEditCommand SetStateCallbackKey(StateMachineDefinition definition, string stableId, string callbackKey)
	{
		return UpdateState(definition, stableId, delegate(StateMachineStateDefinition state)
		{
			state.CallbackKey = new StringName(callbackKey ?? string.Empty);
		});
	}

	public StateMachineEditCommand SetStateLifecycleCallbackKey(StateMachineDefinition definition, string stableId, StateMachineCallbackPhase phase, string callbackKey)
	{
		return UpdateState(definition, stableId, delegate(StateMachineStateDefinition state)
		{
			StringName stringName = new StringName(callbackKey ?? string.Empty);
			switch (phase)
			{
			case StateMachineCallbackPhase.Enter:
				state.EnterCallbackKey = stringName;
				break;
			case StateMachineCallbackPhase.Exit:
				state.ExitCallbackKey = stringName;
				break;
			case StateMachineCallbackPhase.Process:
				state.ProcessCallbackKey = stringName;
				break;
			case StateMachineCallbackPhase.PhysicsProcess:
				state.PhysicsProcessCallbackKey = stringName;
				break;
			default:
				throw new ArgumentOutOfRangeException("phase", phase, "Guard callbacks belong to transition Guard definitions.");
			}
		});
	}

	public StateMachineEditCommand AddTransition(StateMachineDefinition definition, string sourceStateId, string targetStateId, StateMachineTriggerKind triggerKind = StateMachineTriggerKind.Event, string eventName = "", double delaySeconds = 0.0, int priority = 0, string stableIdScope = "transition", string authorTypeId = "")
	{
		ArgumentNullException.ThrowIfNull(definition, "definition");
		if (FindVisibleState(definition, sourceStateId) == null || FindVisibleState(definition, targetStateId) == null)
		{
			throw new ArgumentException("Transition source and target states must exist.");
		}
		if (!double.IsFinite(delaySeconds) || delaySeconds < 0.0)
		{
			throw new ArgumentOutOfRangeException("delaySeconds");
		}
		string text = CreateEditorStableId(definition, stableIdScope);
		DefinitionSnapshot definitionSnapshot = CaptureDefinition(definition);
		StateMachineDefinition stateMachineDefinition = CreateDefinition(definitionSnapshot);
		StateMachineTransitionDefinition stateMachineTransitionDefinition = CreateAuthoredTransition(authorTypeId);
		stateMachineTransitionDefinition.StableId = text;
		stateMachineTransitionDefinition.SourceStateId = sourceStateId;
		stateMachineTransitionDefinition.TargetStateId = targetStateId;
		stateMachineTransitionDefinition.TriggerKind = triggerKind;
		stateMachineTransitionDefinition.EventName = new StringName(eventName ?? string.Empty);
		stateMachineTransitionDefinition.DelaySeconds = delaySeconds;
		stateMachineTransitionDefinition.Priority = priority;
		stateMachineTransitionDefinition.DeclarationOrder = stateMachineDefinition.Transitions.Count;
		stateMachineDefinition.Transitions.Add(stateMachineTransitionDefinition);
		return ExecuteDefinitionCommand(definition, definitionSnapshot, CaptureDefinition(stateMachineDefinition), text, sourceStateId, targetStateId);
	}

	public StateMachineEditCommand RemoveTransition(StateMachineDefinition definition, string stableId)
	{
		ArgumentNullException.ThrowIfNull(definition, "definition");
		DefinitionSnapshot definitionSnapshot = CaptureDefinition(definition);
		StateMachineDefinition stateMachineDefinition = CreateDefinition(definitionSnapshot);
		int num = FindTransitionIndex(stateMachineDefinition, stableId);
		if (num < 0)
		{
			if (FindVisibleTransitionIndex(stateMachineDefinition, stableId) >= 0)
			{
				throw new InvalidOperationException("Inherited transitions cannot be removed from a derived definition.");
			}
			throw new ArgumentException("Transition '" + stableId + "' does not exist.", "stableId");
		}
		StateMachineTransitionDefinition stateMachineTransitionDefinition = stateMachineDefinition.Transitions[num];
		stateMachineDefinition.Transitions.RemoveAt(num);
		return ExecuteDefinitionCommand(definition, definitionSnapshot, CaptureDefinition(stateMachineDefinition), stableId, stateMachineTransitionDefinition.SourceStateId, stateMachineTransitionDefinition.TargetStateId);
	}

	public StateMachineEditCommand CreateTransitionOverride(StateMachineDefinition definition, string stableId)
	{
		ArgumentNullException.ThrowIfNull(definition, "definition");
		if (FindTransitionIndex(definition, stableId) >= 0)
		{
			throw new InvalidOperationException("Transition '" + stableId + "' already has a local override.");
		}
		StateMachineTransitionDefinition source = FindInheritedTransition(definition, stableId) ?? throw new InvalidOperationException("Transition '" + stableId + "' is not inherited from the base definition.");
		DefinitionSnapshot definitionSnapshot = CaptureDefinition(definition);
		StateMachineDefinition stateMachineDefinition = CreateDefinition(definitionSnapshot);
		stateMachineDefinition.Transitions.Add(CloneTransition(source));
		return ExecuteDefinitionCommand(definition, definitionSnapshot, CaptureDefinition(stateMachineDefinition), stableId);
	}

	public StateMachineEditCommand RestoreInheritedTransition(StateMachineDefinition definition, string stableId)
	{
		ArgumentNullException.ThrowIfNull(definition, "definition");
		if (FindInheritedTransition(definition, stableId) == null)
		{
			throw new InvalidOperationException("Transition '" + stableId + "' is not inherited from the base definition.");
		}
		if (FindTransitionIndex(definition, stableId) < 0)
		{
			throw new InvalidOperationException("Transition '" + stableId + "' does not have a local override.");
		}
		DefinitionSnapshot definitionSnapshot = CaptureDefinition(definition);
		StateMachineDefinition stateMachineDefinition = CreateDefinition(definitionSnapshot);
		stateMachineDefinition.Transitions.RemoveAt(FindTransitionIndex(stateMachineDefinition, stableId));
		EnsureValidRestoredDefinition(stateMachineDefinition, "transition '" + stableId + "'");
		return ExecuteDefinitionCommand(definition, definitionSnapshot, CaptureDefinition(stateMachineDefinition), stableId);
	}

	public StateMachineEditCommand UpdateTransition(StateMachineDefinition definition, string stableId, string sourceStateId, string targetStateId, StateMachineTriggerKind triggerKind, string eventName, double delaySeconds, int priority, int declarationOrder)
	{
		ArgumentNullException.ThrowIfNull(definition, "definition");
		if (FindVisibleState(definition, sourceStateId) == null || FindVisibleState(definition, targetStateId) == null)
		{
			throw new ArgumentException("Transition source and target states must exist.");
		}
		if (!double.IsFinite(delaySeconds) || delaySeconds < 0.0)
		{
			throw new ArgumentOutOfRangeException("delaySeconds");
		}
		DefinitionSnapshot definitionSnapshot = CaptureDefinition(definition);
		StateMachineDefinition definition2 = CreateDefinition(definitionSnapshot);
		StateMachineTransitionDefinition obj = EnsureLocalTransitionOverride(definition2, stableId) ?? throw new ArgumentException("Transition '" + stableId + "' does not exist.", "stableId");
		obj.SourceStateId = sourceStateId;
		obj.TargetStateId = targetStateId;
		obj.TriggerKind = triggerKind;
		obj.EventName = new StringName(eventName ?? string.Empty);
		obj.DelaySeconds = delaySeconds;
		obj.Priority = priority;
		obj.DeclarationOrder = declarationOrder;
		return ExecuteDefinitionCommand(definition, definitionSnapshot, CaptureDefinition(definition2), stableId, sourceStateId, targetStateId);
	}

	public StateMachineEditCommand SetTransitionGuard(StateMachineDefinition definition, string stableId, Resource guardDefinition)
	{
		ArgumentNullException.ThrowIfNull(definition, "definition");
		DefinitionSnapshot definitionSnapshot = CaptureDefinition(definition);
		StateMachineDefinition definition2 = CreateDefinition(definitionSnapshot);
		(EnsureLocalTransitionOverride(definition2, stableId) ?? throw new ArgumentException("Transition '" + stableId + "' does not exist.", "stableId")).GuardDefinition = guardDefinition;
		return ExecuteDefinitionCommand(definition, definitionSnapshot, CaptureDefinition(definition2), stableId);
	}

	public StateMachineEditCommand AddAlias(StateMachineDefinition definition, string alias, string targetStateId)
	{
		ArgumentNullException.ThrowIfNull(definition, "definition");
		if (string.IsNullOrWhiteSpace(alias))
		{
			throw new ArgumentException("Alias is required.", "alias");
		}
		if (FindVisibleState(definition, targetStateId) == null)
		{
			throw new ArgumentException("Alias target '" + targetStateId + "' does not exist.", "targetStateId");
		}
		DefinitionSnapshot definitionSnapshot = CaptureDefinition(definition);
		StateMachineDefinition stateMachineDefinition = CreateDefinition(definitionSnapshot);
		stateMachineDefinition.Aliases[alias] = targetStateId;
		return ExecuteDefinitionCommand(definition, definitionSnapshot, CaptureDefinition(stateMachineDefinition), alias, targetStateId);
	}

	public StateMachineEditCommand RemoveAlias(StateMachineDefinition definition, string alias)
	{
		ArgumentNullException.ThrowIfNull(definition, "definition");
		DefinitionSnapshot definitionSnapshot = CaptureDefinition(definition);
		StateMachineDefinition stateMachineDefinition = CreateDefinition(definitionSnapshot);
		if (!stateMachineDefinition.Aliases.Remove(alias ?? string.Empty))
		{
			throw new ArgumentException("Alias '" + alias + "' does not exist.", "alias");
		}
		return ExecuteDefinitionCommand(definition, definitionSnapshot, CaptureDefinition(stateMachineDefinition), alias);
	}

	public StateMachineEditCommand MoveGraphNode(StateMachineLayout layout, string stableId, Vector2 position)
	{
		ArgumentNullException.ThrowIfNull(layout, "layout");
		if (string.IsNullOrWhiteSpace(stableId))
		{
			throw new ArgumentException("Stable ID is required.", "stableId");
		}
		Vector2 previous;
		bool hadPrevious = layout.Positions.TryGetValue(stableId, out previous);
		StateMachineEditCommand command = new StateMachineEditCommand(delegate
		{
			layout.Positions[stableId] = position;
			layout.EmitChanged();
		}, delegate
		{
			if (hadPrevious)
			{
				layout.Positions[stableId] = previous;
			}
			else
			{
				layout.Positions.Remove(stableId);
			}
			layout.EmitChanged();
		}, new string[1] { stableId });
		return Execute(command);
	}

	public StateMachineEditCommand MoveGraphNodes(StateMachineLayout layout, IReadOnlyDictionary<string, Vector2> positions)
	{
		ArgumentNullException.ThrowIfNull(layout, "layout");
		ArgumentNullException.ThrowIfNull(positions, "positions");
		System.Collections.Generic.Dictionary<string, Vector2> next = new System.Collections.Generic.Dictionary<string, Vector2>(StringComparer.Ordinal);
		System.Collections.Generic.Dictionary<string, Vector2> previous = new System.Collections.Generic.Dictionary<string, Vector2>(StringComparer.Ordinal);
		HashSet<string> previousKeys = new HashSet<string>(StringComparer.Ordinal);
		foreach (KeyValuePair<string, Vector2> position in positions)
		{
			if (string.IsNullOrWhiteSpace(position.Key))
			{
				throw new ArgumentException("Stable IDs are required for every moved state.", "positions");
			}
			next[position.Key] = position.Value;
			if (layout.Positions.TryGetValue(position.Key, out var value))
			{
				previous[position.Key] = value;
				previousKeys.Add(position.Key);
			}
		}
		StateMachineEditCommand command = new StateMachineEditCommand(delegate
		{
			foreach (KeyValuePair<string, Vector2> item in next)
			{
				layout.Positions[item.Key] = item.Value;
			}
			layout.EmitChanged();
		}, delegate
		{
			foreach (string key in next.Keys)
			{
				if (previousKeys.Contains(key))
				{
					layout.Positions[key] = previous[key];
				}
				else
				{
					layout.Positions.Remove(key);
				}
			}
			layout.EmitChanged();
		}, next.Keys);
		return Execute(command);
	}

	public StateMachineEditCommand SetCollapsed(StateMachineLayout layout, string stableId, bool collapsed)
	{
		ArgumentNullException.ThrowIfNull(layout, "layout");
		if (string.IsNullOrWhiteSpace(stableId))
		{
			throw new ArgumentException("Stable ID is required.", "stableId");
		}
		bool previous;
		bool hadPrevious = layout.Collapsed.TryGetValue(stableId, out previous);
		StateMachineEditCommand command = new StateMachineEditCommand(delegate
		{
			layout.Collapsed[stableId] = collapsed;
			layout.EmitChanged();
		}, delegate
		{
			if (hadPrevious)
			{
				layout.Collapsed[stableId] = previous;
			}
			else
			{
				layout.Collapsed.Remove(stableId);
			}
			layout.EmitChanged();
		}, new string[1] { stableId });
		return Execute(command);
	}

	public bool Undo()
	{
		if (_undo.Count == 0)
		{
			return false;
		}
		StateMachineEditCommand stateMachineEditCommand = _undo.Pop();
		stateMachineEditCommand.Revert();
		_redo.Push(stateMachineEditCommand);
		return true;
	}

	public bool Redo()
	{
		if (_redo.Count == 0)
		{
			return false;
		}
		StateMachineEditCommand stateMachineEditCommand = _redo.Pop();
		stateMachineEditCommand.Apply();
		_undo.Push(stateMachineEditCommand);
		return true;
	}

	public void ClearHistory()
	{
		_undo.Clear();
		_redo.Clear();
	}

	private StateMachineEditCommand Execute(StateMachineEditCommand command)
	{
		if (!_executeImmediately)
		{
			return command;
		}
		command.Apply();
		_undo.Push(command);
		_redo.Clear();
		return command;
	}

	private StateMachineEditCommand ExecuteDefinitionCommand(StateMachineDefinition definition, DefinitionSnapshot before, DefinitionSnapshot after, params string[] affectedStableIds)
	{
		return ExecuteDefinitionCommand(definition, before, after, (IEnumerable<string>)affectedStableIds);
	}

	private StateMachineEditCommand ExecuteDefinitionCommand(StateMachineDefinition definition, DefinitionSnapshot before, DefinitionSnapshot after, IEnumerable<string> affectedStableIds)
	{
		ReuseSurvivingResourceInstances(before, after);
		StateMachineEditCommand command = new StateMachineEditCommand(delegate
		{
			RestoreDefinition(definition, after);
		}, delegate
		{
			RestoreDefinition(definition, before);
		}, affectedStableIds);
		return Execute(command);
	}

	private string CreateEditorStableId(StateMachineDefinition definition, string scope)
	{
		string text = NormalizeScope(scope);
		string text2;
		do
		{
			text2 = (_usesDefaultStableIdProvider ? (text + "." + Guid.NewGuid().ToString("N")) : _stableIdProvider.CreateStableId(text));
		}
		while (FindVisibleState(definition, text2) != null || FindVisibleTransitionIndex(definition, text2) >= 0);
		return text2;
	}

	private static string NormalizeScope(string scope)
	{
		if (string.IsNullOrWhiteSpace(scope))
		{
			return "state";
		}
		char[] array = new char[scope.Length];
		int num = 0;
		for (int i = 0; i < scope.Length; i++)
		{
			char c = char.ToLowerInvariant(scope[i]);
			bool flag = char.IsLetterOrDigit(c);
			if (!flag)
			{
				bool flag2 = ((c == '-' || c == '.' || c == '_') ? true : false);
				flag = flag2;
			}
			if (flag)
			{
				array[num++] = c;
			}
		}
		if (num != 0)
		{
			return new string(array, 0, num).Trim('.');
		}
		return "state";
	}

	private static StateMachineStateDefinition FindState(StateMachineDefinition definition, string stableId)
	{
		if (definition?.States == null)
		{
			return null;
		}
		for (int i = 0; i < definition.States.Count; i++)
		{
			StateMachineStateDefinition stateMachineStateDefinition = definition.States[i];
			if (stateMachineStateDefinition != null && string.Equals(stateMachineStateDefinition.StableId, stableId, StringComparison.Ordinal))
			{
				return stateMachineStateDefinition;
			}
		}
		return null;
	}

	private static int FindStateIndex(StateMachineDefinition definition, string stableId)
	{
		if (definition?.States == null)
		{
			return -1;
		}
		for (int i = 0; i < definition.States.Count; i++)
		{
			StateMachineStateDefinition stateMachineStateDefinition = definition.States[i];
			if (stateMachineStateDefinition != null && string.Equals(stateMachineStateDefinition.StableId, stableId, StringComparison.Ordinal))
			{
				return i;
			}
		}
		return -1;
	}

	private static StateMachineStateDefinition FindInheritedState(StateMachineDefinition definition, string stableId)
	{
		if (definition?.BaseDefinition == null)
		{
			return null;
		}
		return FindState(ComposeOrThrow(definition.BaseDefinition), stableId);
	}

	private static StateMachineTransitionDefinition FindInheritedTransition(StateMachineDefinition definition, string stableId)
	{
		if (definition?.BaseDefinition == null)
		{
			return null;
		}
		StateMachineDefinition stateMachineDefinition = ComposeOrThrow(definition.BaseDefinition);
		int num = FindTransitionIndex(stateMachineDefinition, stableId);
		if (num < 0)
		{
			return null;
		}
		return stateMachineDefinition.Transitions[num];
	}

	private static StateMachineStateDefinition CloneState(StateMachineStateDefinition source)
	{
		return StateMachineResourceClone.CloneState(source);
	}

	private static StateMachineDefinition ComposeOrThrow(StateMachineDefinition definition)
	{
		if (StateMachineDefinitionComposer.TryCompose(definition, out var composed, out var validation))
		{
			return composed;
		}
		StateMachineDiagnostic stateMachineDiagnostic = ((validation != null && validation.Diagnostics?.Count > 0) ? validation.Diagnostics[0] : null);
		throw new InvalidOperationException((stateMachineDiagnostic == null) ? "State-machine inheritance could not be composed." : ("[" + stateMachineDiagnostic.Code + "] " + stateMachineDiagnostic.Message));
	}

	private static StateMachineStateDefinition CreateAuthoredState(string authorTypeId)
	{
		StateMachineStateDefinition stateMachineStateDefinition = StateMachineAuthorTypeRegistry.CreateState(authorTypeId);
		if (!GodotObject.IsInstanceValid(stateMachineStateDefinition) || !string.IsNullOrWhiteSpace(stateMachineStateDefinition.ResourcePath))
		{
			throw new InvalidOperationException("State author type must create a fresh unsaved Resource.");
		}
		return stateMachineStateDefinition;
	}

	private static StateMachineTransitionDefinition CreateAuthoredTransition(string authorTypeId)
	{
		StateMachineTransitionDefinition stateMachineTransitionDefinition = StateMachineAuthorTypeRegistry.CreateTransition(authorTypeId);
		if (!GodotObject.IsInstanceValid(stateMachineTransitionDefinition) || !string.IsNullOrWhiteSpace(stateMachineTransitionDefinition.ResourcePath))
		{
			throw new InvalidOperationException("Transition author type must create a fresh unsaved Resource.");
		}
		return stateMachineTransitionDefinition;
	}

	private static StateMachineTransitionDefinition CloneTransition(StateMachineTransitionDefinition source)
	{
		return StateMachineResourceClone.CloneTransition(source);
	}

	private static void EnsureValidRestoredDefinition(StateMachineDefinition definition, string restoredItem)
	{
		if (!StateMachineValidator.Validate(definition).IsValid)
		{
			throw new InvalidOperationException("Cannot restore inheritance for " + restoredItem + " because local dependent items would become invalid.");
		}
	}

	private static StateMachineStateDefinition FindVisibleState(StateMachineDefinition definition, string stableId)
	{
		return FindState(ComposeOrThrow(definition), stableId);
	}

	private static List<StateMachineStateDefinition> FindVisibleDirectChildren(StateMachineDefinition definition, string parentId)
	{
		StateMachineDefinition stateMachineDefinition = ComposeOrThrow(definition);
		List<StateMachineStateDefinition> list = new List<StateMachineStateDefinition>();
		if (stateMachineDefinition.States == null)
		{
			return list;
		}
		for (int i = 0; i < stateMachineDefinition.States.Count; i++)
		{
			StateMachineStateDefinition stateMachineStateDefinition = stateMachineDefinition.States[i];
			if (stateMachineStateDefinition != null && string.Equals(stateMachineStateDefinition.ParentId, parentId, StringComparison.Ordinal))
			{
				list.Add(stateMachineStateDefinition);
			}
		}
		return list;
	}

	private static StateMachineStateDefinition FindFirstNonHistoryChild(IReadOnlyList<StateMachineStateDefinition> children)
	{
		if (children == null)
		{
			return null;
		}
		for (int i = 0; i < children.Count; i++)
		{
			StateMachineStateDefinition stateMachineStateDefinition = children[i];
			if (stateMachineStateDefinition != null && stateMachineStateDefinition.Kind != StateMachineStateKind.History)
			{
				return stateMachineStateDefinition;
			}
		}
		return null;
	}

	private static StateMachineStateDefinition FindChildById(IReadOnlyList<StateMachineStateDefinition> children, string stableId)
	{
		if (children == null || string.IsNullOrWhiteSpace(stableId))
		{
			return null;
		}
		for (int i = 0; i < children.Count; i++)
		{
			StateMachineStateDefinition stateMachineStateDefinition = children[i];
			if (stateMachineStateDefinition != null && stateMachineStateDefinition.Kind != StateMachineStateKind.History && string.Equals(stateMachineStateDefinition.StableId, stableId, StringComparison.Ordinal))
			{
				return stateMachineStateDefinition;
			}
		}
		return null;
	}

	private static StateMachineStateDefinition EnsureLocalStateOverride(StateMachineDefinition definition, string stableId)
	{
		StateMachineStateDefinition stateMachineStateDefinition = FindState(definition, stableId);
		if (stateMachineStateDefinition != null)
		{
			return stateMachineStateDefinition;
		}
		StateMachineStateDefinition stateMachineStateDefinition2 = FindVisibleState(definition, stableId);
		if (stateMachineStateDefinition2 == null)
		{
			return null;
		}
		stateMachineStateDefinition = CloneState(stateMachineStateDefinition2);
		definition.States.Add(stateMachineStateDefinition);
		return stateMachineStateDefinition;
	}

	private StateMachineEditCommand UpdateState(StateMachineDefinition definition, string stableId, Action<StateMachineStateDefinition> update)
	{
		ArgumentNullException.ThrowIfNull(definition, "definition");
		DefinitionSnapshot definitionSnapshot = CaptureDefinition(definition);
		StateMachineDefinition definition2 = CreateDefinition(definitionSnapshot);
		StateMachineStateDefinition obj = EnsureLocalStateOverride(definition2, stableId) ?? throw new ArgumentException("State '" + stableId + "' does not exist.", "stableId");
		update(obj);
		return ExecuteDefinitionCommand(definition, definitionSnapshot, CaptureDefinition(definition2), stableId);
	}

	private static int FindTransitionIndex(StateMachineDefinition definition, string stableId)
	{
		if (definition?.Transitions == null)
		{
			return -1;
		}
		for (int i = 0; i < definition.Transitions.Count; i++)
		{
			StateMachineTransitionDefinition stateMachineTransitionDefinition = definition.Transitions[i];
			if (stateMachineTransitionDefinition != null && string.Equals(stateMachineTransitionDefinition.StableId, stableId, StringComparison.Ordinal))
			{
				return i;
			}
		}
		return -1;
	}

	private static int FindVisibleTransitionIndex(StateMachineDefinition definition, string stableId)
	{
		return FindTransitionIndex(ComposeOrThrow(definition), stableId);
	}

	private static StateMachineTransitionDefinition EnsureLocalTransitionOverride(StateMachineDefinition definition, string stableId)
	{
		int num = FindTransitionIndex(definition, stableId);
		if (num >= 0)
		{
			return definition.Transitions[num];
		}
		StateMachineDefinition stateMachineDefinition = ComposeOrThrow(definition);
		int num2 = FindTransitionIndex(stateMachineDefinition, stableId);
		if (num2 < 0)
		{
			return null;
		}
		StateMachineTransitionDefinition stateMachineTransitionDefinition = CloneTransition(stateMachineDefinition.Transitions[num2]);
		definition.Transitions.Add(stateMachineTransitionDefinition);
		return stateMachineTransitionDefinition;
	}

	private static DefinitionSnapshot CaptureDefinition(StateMachineDefinition definition)
	{
		DefinitionSnapshot definitionSnapshot = new DefinitionSnapshot
		{
			SchemaVersion = definition.SchemaVersion,
			DefinitionId = (definition.DefinitionId ?? string.Empty),
			BaseDefinition = definition.BaseDefinition,
			RootStateId = (definition.RootStateId ?? string.Empty)
		};
		if (definition.States != null)
		{
			for (int i = 0; i < definition.States.Count; i++)
			{
				StateMachineStateDefinition stateMachineStateDefinition = definition.States[i];
				if (stateMachineStateDefinition == null)
				{
					definitionSnapshot.States.Add(null);
					continue;
				}
				definitionSnapshot.States.Add(new StateSnapshot
				{
					ResourceInstance = stateMachineStateDefinition,
					StableId = (stateMachineStateDefinition.StableId ?? string.Empty),
					DisplayName = stateMachineStateDefinition.DisplayName,
					Kind = stateMachineStateDefinition.Kind,
					ParentId = (stateMachineStateDefinition.ParentId ?? string.Empty),
					InitialChildId = (stateMachineStateDefinition.InitialChildId ?? string.Empty),
					ProcessFlags = stateMachineStateDefinition.ProcessFlags,
					CallbackKey = stateMachineStateDefinition.CallbackKey,
					EnterCallbackKey = stateMachineStateDefinition.EnterCallbackKey,
					ExitCallbackKey = stateMachineStateDefinition.ExitCallbackKey,
					ProcessCallbackKey = stateMachineStateDefinition.ProcessCallbackKey,
					PhysicsProcessCallbackKey = stateMachineStateDefinition.PhysicsProcessCallbackKey
				});
			}
		}
		if (definition.Transitions != null)
		{
			for (int j = 0; j < definition.Transitions.Count; j++)
			{
				StateMachineTransitionDefinition stateMachineTransitionDefinition = definition.Transitions[j];
				if (stateMachineTransitionDefinition == null)
				{
					definitionSnapshot.Transitions.Add(null);
					continue;
				}
				definitionSnapshot.Transitions.Add(new TransitionSnapshot
				{
					ResourceInstance = stateMachineTransitionDefinition,
					StableId = (stateMachineTransitionDefinition.StableId ?? string.Empty),
					SourceStateId = (stateMachineTransitionDefinition.SourceStateId ?? string.Empty),
					TargetStateId = (stateMachineTransitionDefinition.TargetStateId ?? string.Empty),
					TriggerKind = stateMachineTransitionDefinition.TriggerKind,
					EventName = stateMachineTransitionDefinition.EventName,
					DelaySeconds = stateMachineTransitionDefinition.DelaySeconds,
					Priority = stateMachineTransitionDefinition.Priority,
					DeclarationOrder = stateMachineTransitionDefinition.DeclarationOrder,
					GuardDefinition = stateMachineTransitionDefinition.GuardDefinition
				});
			}
		}
		if (definition.Aliases != null)
		{
			List<string> list = new List<string>();
			foreach (string key in definition.Aliases.Keys)
			{
				list.Add(key);
			}
			list.Sort(StringComparer.Ordinal);
			for (int k = 0; k < list.Count; k++)
			{
				definitionSnapshot.Aliases.Add(new KeyValuePair<string, string>(list[k], definition.Aliases[list[k]]));
			}
		}
		return definitionSnapshot;
	}

	private static StateMachineDefinition CreateDefinition(DefinitionSnapshot snapshot)
	{
		StateMachineDefinition stateMachineDefinition = new StateMachineDefinition();
		RestoreDefinition(stateMachineDefinition, snapshot, emitChanged: false, duplicateResources: true);
		return stateMachineDefinition;
	}

	private static void ReuseSurvivingResourceInstances(DefinitionSnapshot before, DefinitionSnapshot after)
	{
		System.Collections.Generic.Dictionary<string, Queue<StateMachineStateDefinition>> dictionary = new System.Collections.Generic.Dictionary<string, Queue<StateMachineStateDefinition>>(StringComparer.Ordinal);
		for (int i = 0; i < before.States.Count; i++)
		{
			StateSnapshot stateSnapshot = before.States[i];
			if (stateSnapshot?.ResourceInstance != null && !string.IsNullOrWhiteSpace(stateSnapshot.StableId))
			{
				if (!dictionary.TryGetValue(stateSnapshot.StableId, out var value))
				{
					value = new Queue<StateMachineStateDefinition>();
					dictionary[stateSnapshot.StableId] = value;
				}
				value.Enqueue(stateSnapshot.ResourceInstance);
			}
		}
		for (int j = 0; j < after.States.Count; j++)
		{
			StateSnapshot stateSnapshot2 = after.States[j];
			if (stateSnapshot2 != null && !string.IsNullOrWhiteSpace(stateSnapshot2.StableId) && dictionary.TryGetValue(stateSnapshot2.StableId, out var value2) && value2.Count > 0)
			{
				stateSnapshot2.ResourceInstance = value2.Dequeue();
			}
		}
		System.Collections.Generic.Dictionary<string, Queue<StateMachineTransitionDefinition>> dictionary2 = new System.Collections.Generic.Dictionary<string, Queue<StateMachineTransitionDefinition>>(StringComparer.Ordinal);
		for (int k = 0; k < before.Transitions.Count; k++)
		{
			TransitionSnapshot transitionSnapshot = before.Transitions[k];
			if (transitionSnapshot?.ResourceInstance != null && !string.IsNullOrWhiteSpace(transitionSnapshot.StableId))
			{
				if (!dictionary2.TryGetValue(transitionSnapshot.StableId, out var value3))
				{
					value3 = new Queue<StateMachineTransitionDefinition>();
					dictionary2[transitionSnapshot.StableId] = value3;
				}
				value3.Enqueue(transitionSnapshot.ResourceInstance);
			}
		}
		for (int l = 0; l < after.Transitions.Count; l++)
		{
			TransitionSnapshot transitionSnapshot2 = after.Transitions[l];
			if (transitionSnapshot2 != null && !string.IsNullOrWhiteSpace(transitionSnapshot2.StableId) && dictionary2.TryGetValue(transitionSnapshot2.StableId, out var value4) && value4.Count > 0)
			{
				transitionSnapshot2.ResourceInstance = value4.Dequeue();
			}
		}
	}

	private static void RestoreDefinition(StateMachineDefinition definition, DefinitionSnapshot snapshot, bool emitChanged = true, bool duplicateResources = false)
	{
		definition.SchemaVersion = snapshot.SchemaVersion;
		definition.DefinitionId = snapshot.DefinitionId;
		definition.BaseDefinition = snapshot.BaseDefinition;
		definition.RootStateId = snapshot.RootStateId;
		definition.States = new Array<StateMachineStateDefinition>();
		for (int i = 0; i < snapshot.States.Count; i++)
		{
			StateSnapshot stateSnapshot = snapshot.States[i];
			if (stateSnapshot == null)
			{
				definition.States.Add(null);
				continue;
			}
			StateMachineStateDefinition stateMachineStateDefinition = (duplicateResources ? (stateSnapshot.ResourceInstance?.Duplicate(deep: true) as StateMachineStateDefinition) : stateSnapshot.ResourceInstance);
			if (stateMachineStateDefinition == null)
			{
				stateMachineStateDefinition = new StateMachineStateDefinition();
			}
			stateMachineStateDefinition.StableId = stateSnapshot.StableId;
			stateMachineStateDefinition.DisplayName = stateSnapshot.DisplayName;
			stateMachineStateDefinition.Kind = stateSnapshot.Kind;
			stateMachineStateDefinition.ParentId = stateSnapshot.ParentId;
			stateMachineStateDefinition.InitialChildId = stateSnapshot.InitialChildId;
			stateMachineStateDefinition.ProcessFlags = stateSnapshot.ProcessFlags;
			stateMachineStateDefinition.CallbackKey = stateSnapshot.CallbackKey;
			stateMachineStateDefinition.EnterCallbackKey = stateSnapshot.EnterCallbackKey;
			stateMachineStateDefinition.ExitCallbackKey = stateSnapshot.ExitCallbackKey;
			stateMachineStateDefinition.ProcessCallbackKey = stateSnapshot.ProcessCallbackKey;
			stateMachineStateDefinition.PhysicsProcessCallbackKey = stateSnapshot.PhysicsProcessCallbackKey;
			definition.States.Add(stateMachineStateDefinition);
		}
		definition.Transitions = new Array<StateMachineTransitionDefinition>();
		for (int j = 0; j < snapshot.Transitions.Count; j++)
		{
			TransitionSnapshot transitionSnapshot = snapshot.Transitions[j];
			if (transitionSnapshot == null)
			{
				definition.Transitions.Add(null);
				continue;
			}
			StateMachineTransitionDefinition stateMachineTransitionDefinition = (duplicateResources ? (transitionSnapshot.ResourceInstance?.Duplicate(deep: true) as StateMachineTransitionDefinition) : transitionSnapshot.ResourceInstance);
			StateMachineTransitionDefinition stateMachineTransitionDefinition2 = stateMachineTransitionDefinition;
			if (stateMachineTransitionDefinition2 == null)
			{
				stateMachineTransitionDefinition2 = new StateMachineTransitionDefinition();
			}
			stateMachineTransitionDefinition2.StableId = transitionSnapshot.StableId;
			stateMachineTransitionDefinition2.SourceStateId = transitionSnapshot.SourceStateId;
			stateMachineTransitionDefinition2.TargetStateId = transitionSnapshot.TargetStateId;
			stateMachineTransitionDefinition2.TriggerKind = transitionSnapshot.TriggerKind;
			stateMachineTransitionDefinition2.EventName = transitionSnapshot.EventName;
			stateMachineTransitionDefinition2.DelaySeconds = transitionSnapshot.DelaySeconds;
			stateMachineTransitionDefinition2.Priority = transitionSnapshot.Priority;
			stateMachineTransitionDefinition2.DeclarationOrder = transitionSnapshot.DeclarationOrder;
			if (!duplicateResources)
			{
				stateMachineTransitionDefinition2.GuardDefinition = transitionSnapshot.GuardDefinition;
			}
			else if (stateMachineTransitionDefinition == null)
			{
				stateMachineTransitionDefinition2.GuardDefinition = transitionSnapshot.GuardDefinition?.Duplicate(deep: true);
			}
			definition.Transitions.Add(stateMachineTransitionDefinition2);
		}
		definition.Aliases = new Godot.Collections.Dictionary<string, string>();
		for (int k = 0; k < snapshot.Aliases.Count; k++)
		{
			definition.Aliases[snapshot.Aliases[k].Key] = snapshot.Aliases[k].Value;
		}
		if (emitChanged)
		{
			definition.EmitChanged();
		}
	}
}
