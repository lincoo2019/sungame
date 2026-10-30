using System.Collections.Generic;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TimerComponent : ComponentBase
{
	public delegate void TimeoutEventHandler(string timerName);

	[Export(PropertyHint.None, "")]
	public Dictionary timerDictionary = new Dictionary();

	[Export(PropertyHint.None, "")]
	public double timeScale = 1.0;

	public System.Collections.Generic.Dictionary<string, bool> timerRunning = new System.Collections.Generic.Dictionary<string, bool>();

	public System.Collections.Generic.Dictionary<string, double> timerWaitTime = new System.Collections.Generic.Dictionary<string, double>();

	public System.Collections.Generic.Dictionary<string, double> timerCurrent = new System.Collections.Generic.Dictionary<string, double>();

	private readonly List<string> _activeTimers = new List<string>();

	public Node parent;

	protected override bool UseSharedPhysicsBatch => true;

	public event TimeoutEventHandler OnTimeout;

	public override string _GetName()
	{
		return "TimerComponent";
	}

	public override void _EnterTree()
	{
		base._EnterTree();
		parent = GetParent();
		RebuildActiveTimers();
	}

	public override void _Ready()
	{
		parent = GetParent();
		ResetRuntimeTimersFromConfig();
	}

	public override void _ExitTree()
	{
		_activeTimers.Clear();
		this.OnTimeout = null;
		parent = null;
	}

	private void RebuildActiveTimers()
	{
		_activeTimers.Clear();
		foreach (KeyValuePair<string, bool> item in timerRunning)
		{
			if (item.Value && timerCurrent.ContainsKey(item.Key) && timerWaitTime.ContainsKey(item.Key))
			{
				_activeTimers.Add(item.Key);
			}
		}
	}

	private void ResetRuntimeTimersFromConfig()
	{
		timerRunning.Clear();
		timerWaitTime.Clear();
		timerCurrent.Clear();
		_activeTimers.Clear();
		foreach (Variant key2 in timerDictionary.Keys)
		{
			string key = key2.AsString();
			timerRunning[key] = false;
			timerWaitTime[key] = timerDictionary[key2].AsDouble();
			timerCurrent[key] = 0.0;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (base.alive)
		{
			PhysicsProcessValidated(delta);
		}
	}

	internal override void SharedBatchPhysicsProcess(double delta)
	{
		PhysicsProcessValidated(delta);
	}

	private void PhysicsProcessValidated(double delta)
	{
		if (!TowerDefenseManager._IsGameRunning() || _activeTimers.Count == 0)
		{
			return;
		}
		int num = 0;
		while (num < _activeTimers.Count)
		{
			string text = _activeTimers[num];
			if (!timerRunning.TryGetValue(text, out var value) || !value || !timerCurrent.ContainsKey(text) || !timerWaitTime.ContainsKey(text))
			{
				RemoveActiveTimerAt(num);
				continue;
			}
			if (timerCurrent[text] < timerWaitTime[text])
			{
				timerCurrent[text] += delta * timeScale;
				num++;
				continue;
			}
			timerCurrent[text] = 0.0;
			timerRunning[text] = false;
			RemoveActiveTimerAt(num);
			CallDeferred(MethodName.EmitTimeout, text);
		}
	}

	private void RemoveActiveTimerAt(int index)
	{
		int index2 = _activeTimers.Count - 1;
		_activeTimers[index] = _activeTimers[index2];
		_activeTimers.RemoveAt(index2);
	}

	private void RemoveActiveTimer(string timerName)
	{
		int num = _activeTimers.IndexOf(timerName);
		if (num >= 0)
		{
			RemoveActiveTimerAt(num);
		}
	}

	private void EmitTimeout(string key)
	{
		this.OnTimeout?.Invoke(key);
	}

	private bool IsTimerRunning(string timerName)
	{
		bool value;
		return timerRunning.TryGetValue(timerName, out value) && value;
	}

	public void AddTimer(string timerName, Variant time = default(Variant))
	{
		if (!string.IsNullOrEmpty(timerName))
		{
			double num = ((time.VariantType == Variant.Type.Nil) ? 1.0 : time.AsDouble());
			RemoveActiveTimer(timerName);
			timerDictionary[timerName] = num;
			timerRunning[timerName] = false;
			timerWaitTime[timerName] = num;
			timerCurrent[timerName] = 0.0;
		}
	}

	public void Run(string timerName, double time = -1.0)
	{
		if (timerDictionary.ContainsKey(timerName))
		{
			if (!IsTimerRunning(timerName))
			{
				_activeTimers.Add(timerName);
			}
			timerWaitTime[timerName] = ((time == -1.0) ? timerDictionary[timerName].AsDouble() : time);
			timerCurrent[timerName] = 0.0;
			timerRunning[timerName] = true;
		}
	}

	public void Stop(string timerName)
	{
		if (timerDictionary.ContainsKey(timerName))
		{
			RemoveActiveTimer(timerName);
			timerCurrent[timerName] = 0.0;
			timerRunning[timerName] = false;
		}
	}

	public bool IsRunning(string timerName)
	{
		return IsTimerRunning(timerName);
	}

	public override Dictionary ExportComponentSave()
	{
		Dictionary dictionary = new Dictionary();
		Dictionary dictionary2 = new Dictionary();
		Dictionary dictionary3 = new Dictionary();
		foreach (string key5 in timerRunning.Keys)
		{
			dictionary[key5] = timerRunning[key5];
			dictionary2[key5] = (timerWaitTime.TryGetValue(key5, out var value) ? value : 0.0);
			dictionary3[key5] = (timerCurrent.TryGetValue(key5, out var value2) ? value2 : 0.0);
		}
		return new Dictionary
		{
			["timerRunning"] = dictionary,
			["timerWaitTime"] = dictionary2,
			["timerCurrent"] = dictionary3,
			["timeScale"] = timeScale
		};
	}

	public override void ImportComponentSave(Dictionary data, TowerDefenseLevelSaveConfigCSharp owner)
	{
		ResetRuntimeTimersFromConfig();
		Dictionary dictionary = data.GetValueOrDefault("timerRunning", new Dictionary()).AsGodotDictionary();
		Dictionary dictionary2 = data.GetValueOrDefault("timerWaitTime", new Dictionary()).AsGodotDictionary();
		Dictionary dictionary3 = data.GetValueOrDefault("timerCurrent", new Dictionary()).AsGodotDictionary();
		timeScale = data.GetValueOrDefault("timeScale", 1.0).AsDouble();
		foreach (Variant key in dictionary.Keys)
		{
			string text = key.AsString();
			double num = dictionary2.GetValueOrDefault(key, 0.0).AsDouble();
			if (!timerDictionary.ContainsKey(text))
			{
				timerDictionary[text] = num;
			}
			timerRunning[text] = dictionary[key].AsBool();
			timerWaitTime[text] = num;
			timerCurrent[text] = dictionary3.GetValueOrDefault(key, 0.0).AsDouble();
		}
		_activeTimers.Clear();
		foreach (KeyValuePair<string, bool> item in timerRunning)
		{
			if (item.Value)
			{
				_activeTimers.Add(item.Key);
			}
		}
	}

	public override Dictionary SyncSerialize()
	{
		return ExportComponentSave();
	}

	public override void SyncDeserialize(Dictionary data)
	{
		ImportComponentSave(data, null);
	}
}
