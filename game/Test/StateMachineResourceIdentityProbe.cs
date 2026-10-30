using System;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.ResourceEditors.GUI;

public partial class StateMachineResourceIdentityProbe : Node
{
	public override void _Ready()
	{
		bool flag = ProbeDerivedResourceIdentity();
		bool flag2 = ProbeUndoHistoryIsolation();
		GD.Print($"[STATE_MACHINE_RESOURCE_IDENTITY_PROBE] identity={flag} isolatedUndo={flag2}");
		GetTree().Quit((!(flag && flag2)) ? 1 : 0);
	}

	private static bool ProbeDerivedResourceIdentity()
	{
		StateMachineStateDefinition stateMachineStateDefinition = new StateMachineStateDefinition
		{
			StableId = "Root",
			DisplayName = "Root",
			Kind = StateMachineStateKind.Compound,
			InitialChildId = "ModState"
		};
		StateMachineProbeDerivedState stateMachineProbeDerivedState = new StateMachineProbeDerivedState
		{
			StableId = "ModState",
			DisplayName = "Before",
			ParentId = "Root",
			ModPayload = "keep-me",
			ResourceName = "DerivedStateResource"
		};
		StateMachineProbeDerivedTransition stateMachineProbeDerivedTransition = new StateMachineProbeDerivedTransition
		{
			StableId = "ModTransition",
			SourceStateId = "ModState",
			TargetStateId = "Root",
			TriggerKind = StateMachineTriggerKind.Event,
			EventName = "go",
			ModWeight = 73,
			ResourceName = "DerivedTransitionResource"
		};
		StateMachineDefinition stateMachineDefinition = new StateMachineDefinition
		{
			DefinitionId = "identity-probe",
			RootStateId = "Root"
		};
		stateMachineDefinition.States.Add(stateMachineStateDefinition);
		stateMachineDefinition.States.Add(stateMachineProbeDerivedState);
		stateMachineDefinition.Transitions.Add(stateMachineProbeDerivedTransition);
		StateMachineEditService stateMachineEditService = new StateMachineEditService();
		stateMachineEditService.RenameState(stateMachineDefinition, "ModState", "After");
		bool flag = stateMachineDefinition.States[0] == stateMachineStateDefinition && stateMachineDefinition.States[1] == stateMachineProbeDerivedState && stateMachineDefinition.States[1] is StateMachineProbeDerivedState stateMachineProbeDerivedState2 && stateMachineProbeDerivedState2.DisplayName.ToString() == "After" && stateMachineProbeDerivedState2.ModPayload == "keep-me" && stateMachineProbeDerivedState2.ResourceName == "DerivedStateResource" && stateMachineDefinition.Transitions[0] == stateMachineProbeDerivedTransition && stateMachineProbeDerivedTransition.ModWeight == 73 && stateMachineProbeDerivedTransition.ResourceName == "DerivedTransitionResource";
		bool flag2 = stateMachineEditService.Undo() && stateMachineDefinition.States[1] == stateMachineProbeDerivedState && stateMachineProbeDerivedState.DisplayName.ToString() == "Before" && stateMachineProbeDerivedState.ModPayload == "keep-me";
		bool flag3 = stateMachineEditService.Redo() && stateMachineDefinition.States[1] == stateMachineProbeDerivedState && stateMachineProbeDerivedState.DisplayName.ToString() == "After" && stateMachineProbeDerivedState.ModPayload == "keep-me";
		stateMachineEditService.RemoveState(stateMachineDefinition, "ModState");
		bool flag4 = !stateMachineDefinition.States.Contains(stateMachineProbeDerivedState) && !stateMachineDefinition.Transitions.Contains(stateMachineProbeDerivedTransition);
		bool flag5 = stateMachineEditService.Undo() && stateMachineDefinition.States.Contains(stateMachineProbeDerivedState) && stateMachineDefinition.Transitions.Contains(stateMachineProbeDerivedTransition) && stateMachineProbeDerivedState.ModPayload == "keep-me" && stateMachineProbeDerivedTransition.ModWeight == 73;
		return flag && flag2 && flag3 && flag4 && flag5;
	}

	private static bool ProbeUndoHistoryIsolation()
	{
		XWUndoRedoManager xWUndoRedoManager = new XWUndoRedoManager();
		XWStateMachineUndoAdapter xWStateMachineUndoAdapter = new XWStateMachineUndoAdapter(xWUndoRedoManager);
		int firstValue = 0;
		xWStateMachineUndoAdapter.Commit("first", new StateMachineEditCommand(delegate
		{
			firstValue = 1;
		}, delegate
		{
			firstValue = 0;
		}, Array.Empty<string>()));
		XWStateMachineUndoAdapter xWStateMachineUndoAdapter2 = new XWStateMachineUndoAdapter(xWUndoRedoManager);
		bool num = xWStateMachineUndoAdapter.HistoryScope != xWStateMachineUndoAdapter2.HistoryScope && xWStateMachineUndoAdapter.CanUndo && !xWStateMachineUndoAdapter2.CanUndo;
		int secondValue = 0;
		xWStateMachineUndoAdapter2.Commit("second", new StateMachineEditCommand(delegate
		{
			secondValue = 1;
		}, delegate
		{
			secondValue = 0;
		}, Array.Empty<string>()));
		xWStateMachineUndoAdapter2.Undo();
		bool flag = secondValue == 0 && firstValue == 1;
		xWStateMachineUndoAdapter.Undo();
		bool flag2 = firstValue == 0 && secondValue == 0;
		int historyScope = xWStateMachineUndoAdapter.HistoryScope;
		int historyScope2 = xWStateMachineUndoAdapter2.HistoryScope;
		xWStateMachineUndoAdapter.ReleaseHistory();
		xWStateMachineUndoAdapter2.ReleaseHistory();
		bool flag3 = xWStateMachineUndoAdapter.IsHistoryReleased && xWStateMachineUndoAdapter2.IsHistoryReleased && !xWUndoRedoManager.HasHistory(historyScope) && !xWUndoRedoManager.HasHistory(historyScope2) && !xWStateMachineUndoAdapter.CanUndo && !xWStateMachineUndoAdapter2.CanUndo;
		return num && flag && flag2 && flag3;
	}
}
