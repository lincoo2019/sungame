using System;
using System.Collections.Generic;

public sealed class InMemoryStateMachineUndoAdapter : IStateMachineUndoAdapter
{
	private readonly Stack<StateMachineEditCommand> _undo = new Stack<StateMachineEditCommand>();

	private readonly Stack<StateMachineEditCommand> _redo = new Stack<StateMachineEditCommand>();

	public bool CanUndo => _undo.Count > 0;

	public bool CanRedo => _redo.Count > 0;

	public void Commit(string label, StateMachineEditCommand command)
	{
		ArgumentNullException.ThrowIfNull(command, "command");
		command.Apply();
		_undo.Push(command);
		_redo.Clear();
	}

	public void Undo()
	{
		if (_undo.Count != 0)
		{
			StateMachineEditCommand stateMachineEditCommand = _undo.Pop();
			stateMachineEditCommand.Revert();
			_redo.Push(stateMachineEditCommand);
		}
	}

	public void Redo()
	{
		if (_redo.Count != 0)
		{
			StateMachineEditCommand stateMachineEditCommand = _redo.Pop();
			stateMachineEditCommand.Apply();
			_undo.Push(stateMachineEditCommand);
		}
	}
}
