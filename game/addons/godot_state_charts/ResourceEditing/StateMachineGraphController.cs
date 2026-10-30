using System;
using System.Collections.Generic;
using Godot;

public sealed class StateMachineGraphController : IDisposable
{
	private readonly IStateMachineStableIdProvider _stableIdProvider;

	private readonly StateMachineEditService _editService;

	private readonly StateMachineGraphClipboard _clipboard = new StateMachineGraphClipboard();

	private IStateMachineUndoAdapter _undoAdapter;

	private StateMachineGraphClipboardData _clipboardData;

	private StateMachineRuntime _previewRuntime;

	public StateMachineDefinition Definition { get; private set; }

	public StateMachineLayout Layout { get; private set; }

	public StateMachineGraphViewModel ViewModel { get; }

	public StateMachineValidationResult Diagnostics => ViewModel.Diagnostics;

	public bool HasCompositionError => ViewModel.HasCompositionFailure;

	public bool CanCopySubgraph
	{
		get
		{
			if (Definition != null)
			{
				return !ViewModel.HasCompositionFailure;
			}
			return false;
		}
	}

	public bool CanPasteSubgraph
	{
		get
		{
			if (ViewModel.CanMutate && _clipboardData != null)
			{
				return _clipboardData.States.Count > 0;
			}
			return false;
		}
	}

	public event Action Changed;

	public event Action<StateMachineGraphChange> MutationApplied;

	public StateMachineGraphController(IStateMachineStableIdProvider stableIdProvider = null)
	{
		_stableIdProvider = stableIdProvider ?? new GuidStateMachineStableIdProvider();
		_editService = new StateMachineEditService(_stableIdProvider, executeImmediately: false);
		_undoAdapter = new InMemoryStateMachineUndoAdapter();
		ViewModel = new StateMachineGraphViewModel();
	}

	public void BindUndoAdapter(IStateMachineUndoAdapter undoAdapter)
	{
		_undoAdapter = undoAdapter ?? new InMemoryStateMachineUndoAdapter();
	}

	public StateMachineGraphViewModel LoadDefinition(StateMachineDefinition definition, StateMachineLayout layout)
	{
		Definition = definition ?? throw new ArgumentNullException("definition");
		Layout = layout ?? new StateMachineLayout();
		DisposePreview();
		Refresh();
		return ViewModel;
	}

	public void SetReadOnly(bool readOnly)
	{
		if (ViewModel.IsReadOnly != readOnly)
		{
			ViewModel.IsReadOnly = readOnly;
			if (Definition != null)
			{
				Refresh();
			}
		}
	}

	public string AddState(string displayName, StateMachineStateKind kind = StateMachineStateKind.Atomic, string parentId = "", string authorTypeId = "")
	{
		StateMachineEditCommand stateMachineEditCommand = _editService.AddState(RequireDefinition(), displayName, kind, parentId, "state", authorTypeId);
		Commit("Add state", stateMachineEditCommand);
		if (stateMachineEditCommand.AffectedStableIds.Count <= 0)
		{
			return string.Empty;
		}
		return stateMachineEditCommand.AffectedStableIds[0];
	}

	public string AddStateAtPosition(string displayName, StateMachineStateKind kind, Vector2 position, string parentId = "", string authorTypeId = "")
	{
		StateMachineEditCommand add = _editService.AddState(RequireDefinition(), displayName, kind, parentId, "state", authorTypeId);
		string text = ((add.AffectedStableIds.Count > 0) ? add.AffectedStableIds[0] : string.Empty);
		if (string.IsNullOrWhiteSpace(text))
		{
			return string.Empty;
		}
		StateMachineEditCommand move = _editService.MoveGraphNode(RequireLayout(), text, position);
		StateMachineEditCommand command = new StateMachineEditCommand(delegate
		{
			add.Apply();
			move.Apply();
		}, delegate
		{
			move.Revert();
			add.Revert();
		}, new string[1] { text });
		Commit("Add state", command, StateMachineGraphChangeKind.Definition | StateMachineGraphChangeKind.Layout);
		return text;
	}

	public string AddStateHierarchySafeAtPosition(string displayName, StateMachineStateKind kind, Vector2 position, string parentId = "", string authorTypeId = "")
	{
		string stableId;
		string defaultChildId;
		StateMachineEditCommand add = _editService.AddStateHierarchySafe(RequireDefinition(), displayName, kind, parentId, out stableId, out defaultChildId, "state", authorTypeId);
		if (string.IsNullOrWhiteSpace(stableId))
		{
			return string.Empty;
		}
		Dictionary<string, Vector2> dictionary = new Dictionary<string, Vector2>(StringComparer.Ordinal) { [stableId] = position };
		if (!string.IsNullOrWhiteSpace(defaultChildId))
		{
			dictionary[defaultChildId] = position + new Vector2(42f, 156f);
		}
		StateMachineEditCommand move = _editService.MoveGraphNodes(RequireLayout(), dictionary);
		StateMachineEditCommand command = new StateMachineEditCommand(delegate
		{
			add.Apply();
			move.Apply();
		}, delegate
		{
			move.Revert();
			add.Revert();
		}, add.AffectedStableIds);
		Commit("Add state", command, StateMachineGraphChangeKind.Definition | StateMachineGraphChangeKind.Layout);
		return stableId;
	}

	public void RemoveState(string stableId, string retargetStateId = "")
	{
		Commit("Remove state", _editService.RemoveState(RequireDefinition(), stableId, retargetStateId));
	}

	public void RemoveStates(IEnumerable<string> stableIds)
	{
		Commit("Remove states", _editService.RemoveStates(RequireDefinition(), stableIds));
	}

	public void CreateStateOverride(string stableId)
	{
		Commit("Create state override", _editService.CreateStateOverride(RequireDefinition(), stableId));
	}

	public void RestoreInheritedState(string stableId)
	{
		Commit("Restore inherited state", _editService.RestoreInheritedState(RequireDefinition(), stableId));
	}

	public void RenameState(string stableId, string displayName)
	{
		Commit("Rename state", _editService.RenameState(RequireDefinition(), stableId, displayName));
	}

	public void SetInitialState(string compoundStateId, string initialChildId)
	{
		Commit("Set initial state", _editService.SetInitialState(RequireDefinition(), compoundStateId, initialChildId));
	}

	public void SetRootState(string stableId)
	{
		Commit("Set root state", _editService.SetRootState(RequireDefinition(), stableId));
	}

	public void SetStateKind(string stableId, StateMachineStateKind kind)
	{
		Commit("Set state kind", _editService.SetStateKind(RequireDefinition(), stableId, kind));
	}

	public void SetStateParent(string stableId, string parentId)
	{
		Commit("Set state parent", _editService.SetStateParent(RequireDefinition(), stableId, parentId));
	}

	public void SetStateProcessFlags(string stableId, StateMachineProcessFlags flags)
	{
		Commit("Set state process flags", _editService.SetStateProcessFlags(RequireDefinition(), stableId, flags));
	}

	public void SetStateCallbackKey(string stableId, string callbackKey)
	{
		Commit("Set state callback key", _editService.SetStateCallbackKey(RequireDefinition(), stableId, callbackKey));
	}

	public void SetStateLifecycleCallbackKey(string stableId, StateMachineCallbackPhase phase, string callbackKey)
	{
		Commit($"Set state {phase} callback key", _editService.SetStateLifecycleCallbackKey(RequireDefinition(), stableId, phase, callbackKey));
	}

	public string AddTransition(string sourceStateId, string targetStateId, StateMachineTriggerKind triggerKind = StateMachineTriggerKind.Event, string eventName = "", double delaySeconds = 0.0, int priority = 0, string authorTypeId = "")
	{
		StateMachineEditCommand stateMachineEditCommand = _editService.AddTransition(RequireDefinition(), sourceStateId, targetStateId, triggerKind, eventName, delaySeconds, priority, "transition", authorTypeId);
		Commit("Add transition", stateMachineEditCommand);
		if (stateMachineEditCommand.AffectedStableIds.Count <= 0)
		{
			return string.Empty;
		}
		return stateMachineEditCommand.AffectedStableIds[0];
	}

	public void RemoveTransition(string stableId)
	{
		Commit("Remove transition", _editService.RemoveTransition(RequireDefinition(), stableId));
	}

	public void CreateTransitionOverride(string stableId)
	{
		Commit("Create transition override", _editService.CreateTransitionOverride(RequireDefinition(), stableId));
	}

	public void RestoreInheritedTransition(string stableId)
	{
		Commit("Restore inherited transition", _editService.RestoreInheritedTransition(RequireDefinition(), stableId));
	}

	public void UpdateTransition(string stableId, string sourceStateId, string targetStateId, StateMachineTriggerKind triggerKind, string eventName, double delaySeconds, int priority, int declarationOrder)
	{
		Commit("Update transition", _editService.UpdateTransition(RequireDefinition(), stableId, sourceStateId, targetStateId, triggerKind, eventName, delaySeconds, priority, declarationOrder));
	}

	public void SetTransitionGuard(string stableId, Resource guardDefinition)
	{
		Commit("Set transition guard", _editService.SetTransitionGuard(RequireDefinition(), stableId, guardDefinition));
	}

	public void SetDefinitionProperty(StringName property, Variant value)
	{
		if (property.ToString() == "BaseDefinition")
		{
			throw new InvalidOperationException("BaseDefinition must be changed through the cycle-safe command.");
		}
		SetResourceProperty(RequireDefinition(), property, value, "Set state-machine property", string.Empty);
	}

	public bool SetBaseDefinition(StateMachineDefinition baseDefinition)
	{
		StateMachineDefinition definition = RequireDefinition();
		if (definition.BaseDefinition == baseDefinition)
		{
			return true;
		}
		if (!StateMachineDefinitionComposer.TryValidateBaseDefinitionCandidate(definition, baseDefinition, out var validation))
		{
			ViewModel.Diagnostics = validation;
			ViewModel.CompositionDiagnostics = validation;
			this.Changed?.Invoke();
			return false;
		}
		StateMachineDefinition previous = definition.BaseDefinition;
		Commit("Set state-machine base definition", new StateMachineEditCommand(delegate
		{
			definition.BaseDefinition = baseDefinition;
			definition.EmitChanged();
		}, delegate
		{
			definition.BaseDefinition = previous;
			definition.EmitChanged();
		}, Array.Empty<string>()), StateMachineGraphChangeKind.Definition, allowCompositionRepair: true);
		return true;
	}

	public void SetStateProperty(string stableId, StringName property, Variant value)
	{
		StateMachineStateDefinition target = FindLocalState(stableId) ?? throw new InvalidOperationException("Inherited states must be overridden through a semantic state command before generic editing.");
		SetResourceProperty(target, property, value, "Set state property", stableId);
	}

	public void SetTransitionProperty(string stableId, StringName property, Variant value)
	{
		StateMachineTransitionDefinition target = FindLocalTransition(stableId) ?? throw new InvalidOperationException("Inherited transitions must be overridden through a semantic transition command before generic editing.");
		SetResourceProperty(target, property, value, "Set transition property", stableId);
	}

	public void AddAlias(string alias, string targetStateId)
	{
		Commit("Add alias", _editService.AddAlias(RequireDefinition(), alias, targetStateId));
	}

	public void RemoveAlias(string alias)
	{
		Commit("Remove alias", _editService.RemoveAlias(RequireDefinition(), alias));
	}

	public void MoveGraphNode(string stableId, Vector2 position)
	{
		Commit("Move state", _editService.MoveGraphNode(RequireLayout(), stableId, position), StateMachineGraphChangeKind.Layout);
	}

	public void MoveGraphNodes(IReadOnlyDictionary<string, Vector2> positions)
	{
		Commit("Move states", _editService.MoveGraphNodes(RequireLayout(), positions), StateMachineGraphChangeKind.Layout);
	}

	public void SetCollapsed(string stableId, bool collapsed)
	{
		Commit("Set state collapsed", _editService.SetCollapsed(RequireLayout(), stableId, collapsed), StateMachineGraphChangeKind.Layout);
	}

	public bool CopySubgraph(IEnumerable<string> stableIds, IReadOnlyDictionary<string, Vector2> visualPositions = null)
	{
		if (!CanCopySubgraph)
		{
			return false;
		}
		StateMachineGraphClipboardData stateMachineGraphClipboardData = _clipboard.CopySubgraph(RequireDefinition(), RequireLayout(), stableIds);
		if (visualPositions == null)
		{
			_clipboardData = stateMachineGraphClipboardData;
			return true;
		}
		foreach (KeyValuePair<string, Vector2> visualPosition in visualPositions)
		{
			for (int i = 0; i < stateMachineGraphClipboardData.States.Count; i++)
			{
				if (stateMachineGraphClipboardData.States[i]?.StableId == visualPosition.Key)
				{
					stateMachineGraphClipboardData.Positions[visualPosition.Key] = visualPosition.Value;
					break;
				}
			}
		}
		_clipboardData = stateMachineGraphClipboardData;
		return true;
	}

	public IReadOnlyDictionary<string, string> PasteSubgraph(Vector2 offset)
	{
		if (!CanPasteSubgraph)
		{
			return new Dictionary<string, string>();
		}
		StateMachineDefinition definition = RequireDefinition();
		StateMachineGraphPasteResult paste = _clipboard.PasteSubgraph(_clipboardData, _stableIdProvider, offset, definition);
		StateMachineLayout layout = RequireLayout();
		StateMachineEditCommand command = new StateMachineEditCommand(delegate
		{
			for (int i = 0; i < paste.States.Count; i++)
			{
				definition.States.Add(paste.States[i]);
			}
			for (int j = 0; j < paste.Transitions.Count; j++)
			{
				definition.Transitions.Add(paste.Transitions[j]);
			}
			foreach (KeyValuePair<string, Vector2> position in paste.Positions)
			{
				layout.Positions[position.Key] = position.Value;
			}
			definition.EmitChanged();
			layout.EmitChanged();
		}, delegate
		{
			for (int i = 0; i < paste.Transitions.Count; i++)
			{
				definition.Transitions.Remove(paste.Transitions[i]);
			}
			for (int j = 0; j < paste.States.Count; j++)
			{
				definition.States.Remove(paste.States[j]);
				layout.Positions.Remove(paste.States[j].StableId);
			}
			definition.EmitChanged();
			layout.EmitChanged();
		}, paste.StableIdRemap.Values);
		Commit("Paste states", command, StateMachineGraphChangeKind.Definition | StateMachineGraphChangeKind.Layout);
		return paste.StableIdRemap;
	}

	public IReadOnlyDictionary<string, string> PasteSubgraphAt(Vector2 graphPosition)
	{
		if (!CanPasteSubgraph)
		{
			return new Dictionary<string, string>();
		}
		Vector2 vector = graphPosition;
		bool flag = false;
		foreach (Vector2 value in _clipboardData.Positions.Values)
		{
			vector = (flag ? new Vector2(Mathf.Min(vector.X, value.X), Mathf.Min(vector.Y, value.Y)) : value);
			flag = true;
		}
		Vector2 offset = (flag ? (graphPosition - vector) : graphPosition);
		return PasteSubgraph(offset);
	}

	public StateMachineValidationResult Validate()
	{
		Refresh();
		return ViewModel.Diagnostics;
	}

	public bool CompilePreview(out StateMachineProgram program, out StateMachineValidationResult validation)
	{
		DisposePreview();
		if (!StateMachineCompiler.TryCompile(RequireDefinition(), out program, out validation))
		{
			return false;
		}
		_previewRuntime = new StateMachineRuntime();
		_previewRuntime.Initialize(program);
		_previewRuntime.EnterInitialState();
		return true;
	}

	public bool InjectPreviewEvent(StringName eventName)
	{
		if (_previewRuntime != null)
		{
			return _previewRuntime.SendEvent(eventName);
		}
		return false;
	}

	public string AddComment(string text, Vector2 position, Vector2 size)
	{
		StateMachineLayout layout = RequireLayout();
		StateMachineLayoutComment comment = new StateMachineLayoutComment
		{
			StableId = _stableIdProvider.CreateStableId("comment"),
			Text = (text ?? string.Empty),
			Position = position,
			Size = size
		};
		Commit("Add comment", new StateMachineEditCommand(delegate
		{
			layout.Comments.Add(comment);
			layout.EmitChanged();
		}, delegate
		{
			layout.Comments.Remove(comment);
			layout.EmitChanged();
		}, new string[1] { comment.StableId }), StateMachineGraphChangeKind.Layout);
		return comment.StableId;
	}

	public void UpdateComment(string stableId, string text, Vector2 position, Vector2 size)
	{
		StateMachineLayoutComment comment = FindComment(stableId);
		string oldText = comment.Text;
		Vector2 oldPosition = comment.Position;
		Vector2 oldSize = comment.Size;
		Commit("Update comment", new StateMachineEditCommand(delegate
		{
			comment.Text = text ?? string.Empty;
			comment.Position = position;
			comment.Size = size;
			Layout.EmitChanged();
		}, delegate
		{
			comment.Text = oldText;
			comment.Position = oldPosition;
			comment.Size = oldSize;
			Layout.EmitChanged();
		}, new string[1] { stableId }), StateMachineGraphChangeKind.Layout);
	}

	public void RemoveComment(string stableId)
	{
		StateMachineLayout layout = RequireLayout();
		StateMachineLayoutComment comment = FindComment(stableId);
		int index = layout.Comments.IndexOf(comment);
		Commit("Remove comment", new StateMachineEditCommand(delegate
		{
			layout.Comments.Remove(comment);
			layout.EmitChanged();
		}, delegate
		{
			layout.Comments.Insert(Math.Min(index, layout.Comments.Count), comment);
			layout.EmitChanged();
		}, new string[1] { stableId }), StateMachineGraphChangeKind.Layout);
	}

	public void SetViewport(Vector2 scrollOffset, float zoom)
	{
		StateMachineLayout layout = RequireLayout();
		Vector2 oldOffset = layout.ScrollOffset;
		float oldZoom = layout.Zoom;
		Commit("Set graph viewport", new StateMachineEditCommand(delegate
		{
			layout.ScrollOffset = scrollOffset;
			layout.Zoom = zoom;
			layout.EmitChanged();
		}, delegate
		{
			layout.ScrollOffset = oldOffset;
			layout.Zoom = oldZoom;
			layout.EmitChanged();
		}, Array.Empty<string>()), StateMachineGraphChangeKind.Layout);
	}

	public bool UpdateViewportState(Vector2 scrollOffset, float zoom)
	{
		StateMachineLayout stateMachineLayout = RequireLayout();
		float num = Mathf.Clamp(zoom, 0.25f, 2f);
		if (stateMachineLayout.ScrollOffset.IsEqualApprox(scrollOffset) && Mathf.IsEqualApprox(stateMachineLayout.Zoom, num))
		{
			return false;
		}
		stateMachineLayout.ScrollOffset = scrollOffset;
		stateMachineLayout.Zoom = num;
		stateMachineLayout.EmitChanged();
		return true;
	}

	public void Undo()
	{
		if (_undoAdapter.CanUndo)
		{
			_undoAdapter.Undo();
		}
	}

	public void Redo()
	{
		if (_undoAdapter.CanRedo)
		{
			_undoAdapter.Redo();
		}
	}

	public void Dispose()
	{
		DisposePreview();
		Definition = null;
		Layout = null;
		this.Changed = null;
		this.MutationApplied = null;
	}

	private void Commit(string label, StateMachineEditCommand command, StateMachineGraphChangeKind kind = StateMachineGraphChangeKind.Definition, bool allowCompositionRepair = false)
	{
		if (!allowCompositionRepair && (kind & StateMachineGraphChangeKind.Definition) != StateMachineGraphChangeKind.None && ViewModel.HasCompositionFailure)
		{
			throw new InvalidOperationException("State-machine inheritance composition failed. Clear or replace BaseDefinition before editing the graph.");
		}
		DisposePreview();
		StateMachineEditCommand command2 = new StateMachineEditCommand(delegate
		{
			command.Apply();
			PublishMutation(kind, command.AffectedStableIds);
		}, delegate
		{
			command.Revert();
			PublishMutation(kind, command.AffectedStableIds);
		}, command.AffectedStableIds);
		_undoAdapter.Commit(label, command2);
	}

	private void PublishMutation(StateMachineGraphChangeKind kind, IReadOnlyList<string> affectedStableIds)
	{
		if ((kind & StateMachineGraphChangeKind.Definition) != StateMachineGraphChangeKind.None)
		{
			Refresh();
		}
		else
		{
			this.Changed?.Invoke();
		}
		this.MutationApplied?.Invoke(new StateMachineGraphChange
		{
			Kind = kind,
			AffectedStableIds = (affectedStableIds ?? Array.Empty<string>())
		});
	}

	private void Refresh()
	{
		ViewModel.Refresh(Definition, Layout);
		this.Changed?.Invoke();
	}

	private StateMachineDefinition RequireDefinition()
	{
		return Definition ?? throw new InvalidOperationException("Load a state-machine definition before editing.");
	}

	private StateMachineLayout RequireLayout()
	{
		return Layout ?? throw new InvalidOperationException("Load a state-machine layout before editing.");
	}

	private StateMachineLayoutComment FindComment(string stableId)
	{
		StateMachineLayout stateMachineLayout = RequireLayout();
		for (int i = 0; i < stateMachineLayout.Comments.Count; i++)
		{
			StateMachineLayoutComment stateMachineLayoutComment = stateMachineLayout.Comments[i];
			if (stateMachineLayoutComment != null && string.Equals(stateMachineLayoutComment.StableId, stableId, StringComparison.Ordinal))
			{
				return stateMachineLayoutComment;
			}
		}
		throw new ArgumentException("Comment '" + stableId + "' does not exist.", "stableId");
	}

	private StateMachineStateDefinition FindLocalState(string stableId)
	{
		StateMachineDefinition stateMachineDefinition = RequireDefinition();
		for (int i = 0; i < stateMachineDefinition.States.Count; i++)
		{
			StateMachineStateDefinition stateMachineStateDefinition = stateMachineDefinition.States[i];
			if (stateMachineStateDefinition != null && string.Equals(stateMachineStateDefinition.StableId, stableId, StringComparison.Ordinal))
			{
				return stateMachineStateDefinition;
			}
		}
		return null;
	}

	private StateMachineTransitionDefinition FindLocalTransition(string stableId)
	{
		StateMachineDefinition stateMachineDefinition = RequireDefinition();
		for (int i = 0; i < stateMachineDefinition.Transitions.Count; i++)
		{
			StateMachineTransitionDefinition stateMachineTransitionDefinition = stateMachineDefinition.Transitions[i];
			if (stateMachineTransitionDefinition != null && string.Equals(stateMachineTransitionDefinition.StableId, stableId, StringComparison.Ordinal))
			{
				return stateMachineTransitionDefinition;
			}
		}
		return null;
	}

	private void SetResourceProperty(Resource target, StringName property, Variant value, string label, string stableId)
	{
		Variant previous = target.Get(property);
		Commit(label, new StateMachineEditCommand(delegate
		{
			target.Set(property, value);
			target.EmitChanged();
			Definition?.EmitChanged();
		}, delegate
		{
			target.Set(property, previous);
			target.EmitChanged();
			Definition?.EmitChanged();
		}, string.IsNullOrEmpty(stableId) ? ((IEnumerable<string>)Array.Empty<string>()) : ((IEnumerable<string>)new string[1] { stableId })));
	}

	private void DisposePreview()
	{
		_previewRuntime?.Dispose();
		_previewRuntime = null;
	}
}
