using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace PVZHE.ModEditor.Core;

public partial class XWUndoRedoManager : RefCounted
{
	public enum HistoryType
	{
		Global,
		Scene,
		Remote
	}

	public enum MergeMode
	{
		Disabled,
		Merge,
		End
	}

	public class Action
	{
		public string Name;

		public long BeforeStateToken;

		public long AfterStateToken;

		public List<MethodCall> DoMethods = new List<MethodCall>();

		public List<MethodCall> UndoMethods = new List<MethodCall>();

		public List<PropertyChange> DoProperties = new List<PropertyChange>();

		public List<PropertyChange> UndoProperties = new List<PropertyChange>();
	}

	public class MethodCall
	{
		public GodotObject Object;

		public StringName Method;

		public Variant[] Args;
	}

	public class PropertyChange
	{
		public GodotObject Object;

		public StringName Property;

		public Variant Value;
	}

	public class History
	{
		public List<Action> Actions = new List<Action>();

		public int CurrentAction = -1;

		public long CurrentStateToken;

		public long SavedStateToken;

		public bool HasSavedState;

		public bool IsCommitting;
	}

	private const int FirstDynamicHistoryId = 1000;

	private readonly System.Collections.Generic.Dictionary<int, History> _histories = new System.Collections.Generic.Dictionary<int, History>();

	private int _currentHistoryType;

	private bool _mergeMode;

	private string _mergeActionName = "";

	private int _version;

	private int _nextDynamicHistoryId = 1000;

	private long _nextStateToken = 1L;

	private Action _pendingAction;

	private int _pendingHistoryType;

	private bool _isUndoing;

	private bool _isRedoing;

	public Callable OnVersionChanged { get; set; }

	public Callable ActionCommitted { get; set; }

	public Callable ActionUndone { get; set; }

	public Callable ActionRedone { get; set; }

	public event Action<int> HistoryChanged;

	public int CreateHistoryScope()
	{
		while (_histories.ContainsKey(_nextDynamicHistoryId))
		{
			_nextDynamicHistoryId++;
		}
		int num = _nextDynamicHistoryId++;
		GetOrCreateHistory(num);
		return num;
	}

	public bool HasHistory(int historyType)
	{
		return _histories.ContainsKey(historyType);
	}

	public void ReleaseHistory(int historyType)
	{
		int num = ResolveHistoryType(historyType);
		if (_pendingAction != null && _pendingHistoryType == num)
		{
			DiscardPendingAction();
		}
		if (_histories.Remove(num))
		{
			if (_currentHistoryType == num)
			{
				_currentHistoryType = 0;
			}
			_version++;
			NotifyHistoryChanged(num);
		}
	}

	public void CreateAction(string name, bool mergeMode = false, int historyType = 0)
	{
		int num = ResolveHistoryType(historyType);
		if (!mergeMode || !(_mergeActionName == name) || _pendingAction == null || _pendingHistoryType != num)
		{
			CommitPending();
			_currentHistoryType = num;
			_pendingHistoryType = num;
			_pendingAction = new Action
			{
				Name = name
			};
			_mergeMode = mergeMode;
			_mergeActionName = name;
		}
	}

	public void CommitAction()
	{
		if (_pendingAction != null)
		{
			int pendingHistoryType = _pendingHistoryType;
			History orCreateHistory = GetOrCreateHistory(pendingHistoryType);
			string name = _pendingAction.Name;
			orCreateHistory.IsCommitting = true;
			if (_mergeMode && orCreateHistory.CurrentAction >= 0 && orCreateHistory.Actions[orCreateHistory.CurrentAction].Name == _pendingAction.Name)
			{
				Action action = orCreateHistory.Actions[orCreateHistory.CurrentAction];
				action.DoMethods.AddRange(_pendingAction.DoMethods);
				action.UndoMethods.AddRange(_pendingAction.UndoMethods);
				action.DoProperties.AddRange(_pendingAction.DoProperties);
				action.UndoProperties.AddRange(_pendingAction.UndoProperties);
				action.AfterStateToken = NextStateToken();
				orCreateHistory.CurrentStateToken = action.AfterStateToken;
			}
			else
			{
				orCreateHistory.Actions.RemoveRange(orCreateHistory.CurrentAction + 1, orCreateHistory.Actions.Count - orCreateHistory.CurrentAction - 1);
				_pendingAction.BeforeStateToken = orCreateHistory.CurrentStateToken;
				_pendingAction.AfterStateToken = NextStateToken();
				orCreateHistory.Actions.Add(_pendingAction);
				orCreateHistory.CurrentAction = orCreateHistory.Actions.Count - 1;
				orCreateHistory.CurrentStateToken = _pendingAction.AfterStateToken;
			}
			ExecuteDo(_pendingAction);
			_version++;
			orCreateHistory.IsCommitting = false;
			_pendingAction = null;
			_pendingHistoryType = 0;
			_mergeMode = false;
			_mergeActionName = "";
			NotifyHistoryChanged(pendingHistoryType);
			if (!ActionCommitted.Equals(default(Callable)))
			{
				ActionCommitted.Call(name);
			}
		}
	}

	public void AddDoMethod(GodotObject obj, string method, params Variant[] args)
	{
		if (_pendingAction != null)
		{
			_pendingAction.DoMethods.Add(new MethodCall
			{
				Object = obj,
				Method = new StringName(method),
				Args = args
			});
		}
	}

	public void AddUndoMethod(GodotObject obj, string method, params Variant[] args)
	{
		if (_pendingAction != null)
		{
			_pendingAction.UndoMethods.Add(new MethodCall
			{
				Object = obj,
				Method = new StringName(method),
				Args = args
			});
		}
	}

	public void AddDoProperty(GodotObject obj, StringName property, Variant value)
	{
		if (_pendingAction != null)
		{
			_pendingAction.DoProperties.Add(new PropertyChange
			{
				Object = obj,
				Property = property,
				Value = value
			});
		}
	}

	public void AddUndoProperty(GodotObject obj, StringName property, Variant value)
	{
		if (_pendingAction != null)
		{
			_pendingAction.UndoProperties.Add(new PropertyChange
			{
				Object = obj,
				Property = property,
				Value = value
			});
		}
	}

	public bool Undo()
	{
		CommitPending();
		History history = GetHistory(_currentHistoryType);
		if (history == null || history.CurrentAction < 0)
		{
			return false;
		}
		Action action = history.Actions[history.CurrentAction];
		_isUndoing = true;
		ExecuteUndo(action);
		_isUndoing = false;
		history.CurrentAction--;
		history.CurrentStateToken = action.BeforeStateToken;
		_version++;
		NotifyHistoryChanged(_currentHistoryType);
		if (!ActionUndone.Equals(default(Callable)))
		{
			ActionUndone.Call(action.Name);
		}
		return true;
	}

	public bool Redo()
	{
		CommitPending();
		History history = GetHistory(_currentHistoryType);
		if (history == null || history.CurrentAction >= history.Actions.Count - 1)
		{
			return false;
		}
		history.CurrentAction++;
		Action action = history.Actions[history.CurrentAction];
		_isRedoing = true;
		ExecuteDo(action);
		_isRedoing = false;
		history.CurrentStateToken = action.AfterStateToken;
		_version++;
		NotifyHistoryChanged(_currentHistoryType);
		if (!ActionRedone.Equals(default(Callable)))
		{
			ActionRedone.Call(action.Name);
		}
		return true;
	}

	public bool IsUndoing()
	{
		return _isUndoing;
	}

	public bool IsRedoing()
	{
		return _isRedoing;
	}

	public bool HasUndo(int historyType = -1)
	{
		History history = GetHistory((historyType == -1) ? _currentHistoryType : ResolveHistoryType(historyType));
		if (history != null)
		{
			return history.CurrentAction >= 0;
		}
		return false;
	}

	public bool HasRedo(int historyType = -1)
	{
		History history = GetHistory((historyType == -1) ? _currentHistoryType : ResolveHistoryType(historyType));
		if (history != null)
		{
			return history.CurrentAction < history.Actions.Count - 1;
		}
		return false;
	}

	public string GetCurrentActionName()
	{
		History history = GetHistory(_currentHistoryType);
		if (history == null || history.CurrentAction < 0 || history.CurrentAction >= history.Actions.Count)
		{
			return "";
		}
		return history.Actions[history.CurrentAction].Name;
	}

	public long GetCurrentActionStateToken()
	{
		History history = GetHistory(_currentHistoryType);
		if (history == null || history.CurrentAction < 0 || history.CurrentAction >= history.Actions.Count)
		{
			return 0L;
		}
		return history.Actions[history.CurrentAction].AfterStateToken;
	}

	public long GetNextRedoActionStateToken()
	{
		History history = GetHistory(_currentHistoryType);
		int num = ((history == null) ? (-1) : (history.CurrentAction + 1));
		if (history == null || num < 0 || num >= history.Actions.Count)
		{
			return 0L;
		}
		return history.Actions[num].AfterStateToken;
	}

	public void ClearHistory(int historyType = -1)
	{
		if (historyType == -1)
		{
			DiscardPendingAction();
			List<int> list = new List<int>(_histories.Keys);
			_histories.Clear();
			_currentHistoryType = 0;
			_version++;
			{
				foreach (int item in list)
				{
					NotifyHistoryChanged(item);
				}
				return;
			}
		}
		int num = ResolveHistoryType(historyType);
		if (_pendingAction != null && _pendingHistoryType == num)
		{
			DiscardPendingAction();
		}
		if (_histories.Remove(num))
		{
			_version++;
			NotifyHistoryChanged(num);
		}
	}

	public void SetHistoryAsSaved(int historyType = -1)
	{
		if (historyType == -1)
		{
			foreach (KeyValuePair<int, History> history in _histories)
			{
				history.Value.SavedStateToken = history.Value.CurrentStateToken;
				history.Value.HasSavedState = true;
				NotifyHistoryChanged(history.Key);
			}
			return;
		}
		int num = ResolveHistoryType(historyType);
		History orCreateHistory = GetOrCreateHistory(num);
		orCreateHistory.SavedStateToken = orCreateHistory.CurrentStateToken;
		orCreateHistory.HasSavedState = true;
		NotifyHistoryChanged(num);
	}

	public bool IsHistoryUnsaved(int historyType = -1)
	{
		if (historyType == -1)
		{
			foreach (KeyValuePair<int, History> history2 in _histories)
			{
				if (IsHistoryUnsaved(history2.Value))
				{
					return true;
				}
			}
			return false;
		}
		History history = GetHistory(ResolveHistoryType(historyType));
		if (history == null)
		{
			return false;
		}
		return IsHistoryUnsaved(history);
	}

	public void SetCurrentHistoryType(int type)
	{
		CommitPending();
		_currentHistoryType = ResolveHistoryType(type);
	}

	public int GetCurrentHistoryType()
	{
		return _currentHistoryType;
	}

	public int GetVersion()
	{
		return _version;
	}

	public long GetHistoryStateToken(int historyType)
	{
		return GetHistory(ResolveHistoryType(historyType))?.CurrentStateToken ?? 0;
	}

	private History GetOrCreateHistory(int type)
	{
		if (!_histories.TryGetValue(type, out var value))
		{
			value = new History();
			_histories[type] = value;
		}
		return value;
	}

	private History GetHistory(int type)
	{
		if (!_histories.TryGetValue(type, out var value))
		{
			return null;
		}
		return value;
	}

	private int ResolveHistoryType(int requestedHistory)
	{
		if (requestedHistory == 1 && _currentHistoryType >= 1000)
		{
			return _currentHistoryType;
		}
		return requestedHistory;
	}

	private static bool IsHistoryUnsaved(History history)
	{
		if (!history.HasSavedState)
		{
			return history.CurrentAction >= 0;
		}
		return history.CurrentStateToken != history.SavedStateToken;
	}

	private long NextStateToken()
	{
		return _nextStateToken++;
	}

	private void NotifyHistoryChanged(int historyType)
	{
		if (!OnVersionChanged.Equals(default(Callable)))
		{
			OnVersionChanged.Call();
		}
		this.HistoryChanged?.Invoke(historyType);
	}

	private void CommitPending()
	{
		if (_pendingAction != null)
		{
			CommitAction();
		}
	}

	private void DiscardPendingAction()
	{
		_pendingAction = null;
		_pendingHistoryType = 0;
		_mergeMode = false;
		_mergeActionName = "";
	}

	private void ExecuteDo(Action action)
	{
		foreach (PropertyChange doProperty in action.DoProperties)
		{
			if (GodotObject.IsInstanceValid(doProperty.Object))
			{
				doProperty.Object.Set(doProperty.Property, doProperty.Value);
			}
		}
		foreach (MethodCall doMethod in action.DoMethods)
		{
			if (GodotObject.IsInstanceValid(doMethod.Object))
			{
				doMethod.Object.Callv(doMethod.Method, new Godot.Collections.Array(doMethod.Args));
			}
		}
	}

	private void ExecuteUndo(Action action)
	{
		foreach (PropertyChange undoProperty in action.UndoProperties)
		{
			if (GodotObject.IsInstanceValid(undoProperty.Object))
			{
				undoProperty.Object.Set(undoProperty.Property, undoProperty.Value);
			}
		}
		for (int num = action.UndoMethods.Count - 1; num >= 0; num--)
		{
			MethodCall methodCall = action.UndoMethods[num];
			if (GodotObject.IsInstanceValid(methodCall.Object))
			{
				methodCall.Object.Callv(methodCall.Method, new Godot.Collections.Array(methodCall.Args));
			}
		}
	}
}
