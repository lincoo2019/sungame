using System;
using System.Collections.Generic;
using Godot;
using PVZHE.ModEditor.Core;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public sealed partial class XWStateMachineUndoAdapter : RefCounted, IStateMachineUndoAdapter
{
	private readonly XWUndoRedoManager _undoRedo;

	private readonly Dictionary<long, StateMachineEditCommand> _commands = new Dictionary<long, StateMachineEditCommand>();

	private readonly int _historyScope = -1;

	private long _nextCommandId;

	private bool _historyReleased;

	public int HistoryScope => _historyScope;

	public bool IsHistoryReleased => _historyReleased;

	public bool CanUndo
	{
		get
		{
			if (!_historyReleased && _historyScope >= 0)
			{
				return _undoRedo?.HasUndo(_historyScope) ?? false;
			}
			return false;
		}
	}

	public bool CanRedo
	{
		get
		{
			if (!_historyReleased && _historyScope >= 0)
			{
				return _undoRedo?.HasRedo(_historyScope) ?? false;
			}
			return false;
		}
	}

	public XWStateMachineUndoAdapter()
	{
	}

	public XWStateMachineUndoAdapter(XWUndoRedoManager undoRedo)
	{
		_undoRedo = undoRedo;
		_historyScope = _undoRedo?.CreateHistoryScope() ?? (-1);
	}

	public void Commit(string label, StateMachineEditCommand command)
	{
		if (command != null)
		{
			if (_historyReleased)
			{
				throw new ObjectDisposedException("XWStateMachineUndoAdapter");
			}
			if (_undoRedo == null)
			{
				command.Apply();
				return;
			}
			long num = ++_nextCommandId;
			_commands[num] = command;
			_undoRedo.CreateAction(string.IsNullOrWhiteSpace(label) ? "编辑状态机" : label, mergeMode: false, _historyScope);
			_undoRedo.AddDoMethod(this, "ApplyCommand", num);
			_undoRedo.AddUndoMethod(this, "RevertCommand", num);
			_undoRedo.CommitAction();
		}
	}

	public void Undo()
	{
		if (CanUndo)
		{
			_undoRedo.SetCurrentHistoryType(_historyScope);
			_undoRedo.Undo();
		}
	}

	public void Redo()
	{
		if (CanRedo)
		{
			_undoRedo.SetCurrentHistoryType(_historyScope);
			_undoRedo.Redo();
		}
	}

	public void ReleaseHistory()
	{
		if (!_historyReleased)
		{
			_historyReleased = true;
			_commands.Clear();
			if (_historyScope >= 0)
			{
				_undoRedo?.ReleaseHistory(_historyScope);
			}
		}
	}

	public void Release()
	{
		ReleaseHistory();
	}

	public void ApplyCommand(long id)
	{
		if (_commands.TryGetValue(id, out var value))
		{
			value.Apply();
		}
	}

	public void RevertCommand(long id)
	{
		if (_commands.TryGetValue(id, out var value))
		{
			value.Revert();
		}
	}
}
