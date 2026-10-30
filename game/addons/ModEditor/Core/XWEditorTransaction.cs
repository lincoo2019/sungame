using System;
using System.Collections.Generic;

namespace PVZHE.ModEditor.Core;

public sealed class XWEditorTransaction
{
	public sealed class Operation
	{
		public string Label { get; set; } = "";

		public Action Do { get; set; }

		public Action Undo { get; set; }

		public Action Redo => Do;
	}

	private readonly Stack<Operation> _undo = new Stack<Operation>();

	private readonly Stack<Operation> _redo = new Stack<Operation>();

	public bool CanUndo => _undo.Count > 0;

	public bool CanRedo => _redo.Count > 0;

	public void Commit(string label, Action doAction, Action undoAction)
	{
		Operation operation = new Operation
		{
			Label = label,
			Do = doAction,
			Undo = undoAction
		};
		operation.Do?.Invoke();
		_undo.Push(operation);
		_redo.Clear();
	}

	public void Undo()
	{
		if (_undo.Count != 0)
		{
			Operation operation = _undo.Pop();
			operation.Undo?.Invoke();
			_redo.Push(operation);
		}
	}

	public void Redo()
	{
		if (_redo.Count != 0)
		{
			Operation operation = _redo.Pop();
			operation.Redo?.Invoke();
			_undo.Push(operation);
		}
	}
}
