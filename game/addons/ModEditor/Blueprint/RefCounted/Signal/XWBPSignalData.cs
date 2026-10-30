using System.Collections.Generic;
using Godot;
using PVZHE.ModEditor.Inspector;

namespace PVZHE.ModEditor.Blueprint;

[GlobalClass]
public partial class XWBPSignalData : RefCounted
{
	[Signal]
	public delegate void ChangeEventHandler();

	[Signal]
	public delegate void RemoveEventHandler();

	[Signal]
	public delegate void RenameEventHandler(string name);

	public XWBPScriptData Owner { get; set; }

	public int Id { get; set; }

	public string Name { get; set; } = "新信号";

	public List<XWBPNodePortData> Inputs { get; set; } = new List<XWBPNodePortData>();

	public void RemoveSelf()
	{
		EmitSignal(SignalName.Remove);
	}

	public void RenameSelf(string newName)
	{
		Name = newName;
		EmitSignal(SignalName.Rename, newName);
	}

	public void AddInput(string inputName, int inputType, StringName inputClassName = null)
	{
		XWBPNodePortData item = new XWBPNodePortData(inputName, XWBPNodePortData.Direction.Input, (XWBPNodePortData.PortType)inputType, inputClassName, default(Variant));
		Inputs.Add(item);
		EnsureUniquePortNames();
		EmitSignal(SignalName.Change);
	}

	public void RemoveInput(int index)
	{
		if (index >= 0 && index < Inputs.Count)
		{
			Inputs.RemoveAt(index);
			EmitSignal(SignalName.Change);
		}
	}

	public int GetInputCount()
	{
		return Inputs.Count;
	}

	public List<XWInspectorProperty> GetProperties()
	{
		return new List<XWInspectorProperty>
		{
			new XWInspectorProperty(this, "inputs", default(Variant), "信号", "", PropertyHint.None, "", Callable.From(InputsSet))
		};
	}

	public void InputsSet()
	{
		foreach (XWBPNodePortData input in Inputs)
		{
			input.PortDirection = XWBPNodePortData.Direction.Input;
		}
		EnsureUniquePortNames();
		EmitSignal(SignalName.Change);
	}

	public void EnsureUniquePortNames()
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		for (int i = 0; i < Inputs.Count; i++)
		{
			XWBPNodePortData xWBPNodePortData = Inputs[i];
			string name = xWBPNodePortData.Name;
			if (dictionary.TryGetValue(name, out var value))
			{
				string text = name + value;
				while (dictionary.ContainsKey(text))
				{
					value++;
					text = name + value;
				}
				xWBPNodePortData.Name = text;
				dictionary[name] = value + 1;
				dictionary[text] = 1;
			}
			else
			{
				dictionary[name] = 1;
			}
		}
	}

	public XWBPSignalData Duplicate()
	{
		XWBPSignalData xWBPSignalData = new XWBPSignalData
		{
			Name = Name
		};
		foreach (XWBPNodePortData input in Inputs)
		{
			XWBPNodePortData item = new XWBPNodePortData(input.Name, input.PortDirection, input.PortTypeValue, input.ClassName, input.DefaultValue);
			xWBPSignalData.Inputs.Add(item);
		}
		return xWBPSignalData;
	}
}
