using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace PVZHE.ModEditor.Core;

public partial class XWEditorSelectionHistory : RefCounted
{
	public class HistoryObject
	{
		public ulong ObjectId;

		public string Property = "";

		public bool InspectorOnly;

		public HistoryObject(ulong objectId = 0uL, string property = "", bool inspectorOnly = false)
		{
			ObjectId = objectId;
			Property = property;
			InspectorOnly = inspectorOnly;
		}

		public bool IsValid()
		{
			if (ObjectId == 0L)
			{
				return false;
			}
			return GodotObject.IsInstanceValid(GodotObject.InstanceFromId(ObjectId));
		}
	}

	public class HistoryElement
	{
		public List<HistoryObject> Path = new List<HistoryObject>();

		public int Level;
	}

	private readonly List<HistoryElement> _history = new List<HistoryElement>();

	private int _currentElemIdx = -1;

	public void AddObject(ulong objectId, string property = "", bool inspectorOnly = false)
	{
		if (_currentElemIdx >= 0 && _currentElemIdx < _history.Count)
		{
			HistoryElement historyElement = _history[_currentElemIdx];
			if (historyElement.Path.Count > 0)
			{
				HistoryObject historyObject = historyElement.Path[historyElement.Path.Count - 1];
				if (historyObject.ObjectId == objectId && historyObject.Property == property)
				{
					return;
				}
			}
		}
		if (_currentElemIdx + 1 < _history.Count)
		{
			_history.RemoveRange(_currentElemIdx + 1, _history.Count - _currentElemIdx - 1);
		}
		HistoryElement historyElement2 = new HistoryElement();
		if (property != "" && _currentElemIdx >= 0 && _currentElemIdx < _history.Count)
		{
			HistoryElement historyElement3 = _history[_currentElemIdx];
			foreach (HistoryObject item in historyElement3.Path)
			{
				historyElement2.Path.Add(new HistoryObject(item.ObjectId, item.Property, item.InspectorOnly));
			}
			historyElement2.Level = historyElement3.Level + 1;
		}
		else
		{
			historyElement2.Level = 0;
		}
		historyElement2.Path.Add(new HistoryObject(objectId, property, inspectorOnly));
		_history.Add(historyElement2);
		_currentElemIdx = _history.Count - 1;
	}

	public void ReplaceObject(ulong oldObjectId, ulong newObjectId)
	{
		foreach (HistoryElement item in _history)
		{
			foreach (HistoryObject item2 in item.Path)
			{
				if (item2.ObjectId == oldObjectId)
				{
					item2.ObjectId = newObjectId;
				}
			}
		}
	}

	public void CleanupHistory()
	{
		for (int num = _history.Count - 1; num >= 0; num--)
		{
			HistoryElement historyElement = _history[num];
			bool flag = true;
			foreach (HistoryObject item in historyElement.Path)
			{
				if (!item.IsValid())
				{
					flag = false;
					break;
				}
			}
			if (!flag)
			{
				_history.RemoveAt(num);
				if (_currentElemIdx >= _history.Count)
				{
					_currentElemIdx = _history.Count - 1;
				}
				else if (_currentElemIdx > num)
				{
					_currentElemIdx--;
				}
			}
		}
	}

	public bool IsAtBeginning()
	{
		return _currentElemIdx <= 0;
	}

	public bool IsAtEnd()
	{
		return _currentElemIdx >= _history.Count - 1;
	}

	public bool Next()
	{
		CleanupHistory();
		if (_currentElemIdx < _history.Count - 1)
		{
			_currentElemIdx++;
			return true;
		}
		return false;
	}

	public bool Previous()
	{
		CleanupHistory();
		if (_currentElemIdx > 0)
		{
			_currentElemIdx--;
			return true;
		}
		return false;
	}

	public ulong GetCurrent()
	{
		if (_currentElemIdx < 0 || _currentElemIdx >= _history.Count)
		{
			return 0uL;
		}
		HistoryElement historyElement = _history[_currentElemIdx];
		if (historyElement.Path.Count == 0)
		{
			return 0uL;
		}
		return historyElement.Path[historyElement.Path.Count - 1].ObjectId;
	}

	public GodotObject GetCurrentObject()
	{
		ulong current = GetCurrent();
		if (current == 0L)
		{
			return null;
		}
		return GodotObject.InstanceFromId(current);
	}

	public bool IsCurrentInspectorOnly()
	{
		if (_currentElemIdx < 0 || _currentElemIdx >= _history.Count)
		{
			return false;
		}
		HistoryElement historyElement = _history[_currentElemIdx];
		if (historyElement.Path.Count == 0)
		{
			return false;
		}
		return historyElement.Path[historyElement.Path.Count - 1].InspectorOnly;
	}

	public int GetHistoryLen()
	{
		return _history.Count;
	}

	public int GetHistoryPos()
	{
		return _currentElemIdx;
	}

	public int GetPathSize()
	{
		if (_currentElemIdx < 0 || _currentElemIdx >= _history.Count)
		{
			return 0;
		}
		return _history[_currentElemIdx].Path.Count;
	}

	public ulong GetPathObject(int index)
	{
		if (_currentElemIdx < 0 || _currentElemIdx >= _history.Count)
		{
			return 0uL;
		}
		HistoryElement historyElement = _history[_currentElemIdx];
		if (index < 0 || index >= historyElement.Path.Count)
		{
			return 0uL;
		}
		return historyElement.Path[index].ObjectId;
	}

	public string GetPathProperty(int index)
	{
		if (_currentElemIdx < 0 || _currentElemIdx >= _history.Count)
		{
			return "";
		}
		HistoryElement historyElement = _history[_currentElemIdx];
		if (index < 0 || index >= historyElement.Path.Count)
		{
			return "";
		}
		return historyElement.Path[index].Property;
	}

	public void Clear()
	{
		_history.Clear();
		_currentElemIdx = -1;
	}

	public Dictionary SaveState()
	{
		Dictionary dictionary = new Dictionary();
		Array array = new Array();
		foreach (HistoryElement item in _history)
		{
			Array array2 = new Array();
			foreach (HistoryObject item2 in item.Path)
			{
				array2.Add(new Dictionary
				{
					{
						"object_id",
						(long)item2.ObjectId
					},
					{ "property", item2.Property },
					{ "inspector_only", item2.InspectorOnly }
				});
			}
			array.Add(new Dictionary
			{
				{ "path", array2 },
				{ "level", item.Level }
			});
		}
		dictionary["history"] = array;
		dictionary["current"] = _currentElemIdx;
		return dictionary;
	}

	public void RestoreState(Dictionary state)
	{
		Clear();
		if (!state.ContainsKey("history"))
		{
			return;
		}
		foreach (Variant item in (Array)state["history"])
		{
			Dictionary dictionary = (Dictionary)item;
			HistoryElement historyElement = new HistoryElement
			{
				Level = (int)dictionary.GetValueOrDefault("level", 0)
			};
			foreach (Variant item2 in (Array)dictionary.GetValueOrDefault("path", new Array()))
			{
				Dictionary dictionary2 = (Dictionary)item2;
				historyElement.Path.Add(new HistoryObject((ulong)(long)dictionary2.GetValueOrDefault("object_id", 0L), (string)dictionary2.GetValueOrDefault("property", ""), (bool)dictionary2.GetValueOrDefault("inspector_only", false)));
			}
			_history.Add(historyElement);
		}
		_currentElemIdx = (int)state.GetValueOrDefault("current", -1);
	}

	public Array GetHistory()
	{
		Array array = new Array();
		foreach (HistoryElement item in _history)
		{
			if (item.Path.Count == 0)
			{
				array.Add(0L);
			}
			else
			{
				array.Add((long)item.Path[item.Path.Count - 1].ObjectId);
			}
		}
		return array;
	}
}
