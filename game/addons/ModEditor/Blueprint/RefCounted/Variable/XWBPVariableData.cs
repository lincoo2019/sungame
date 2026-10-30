using System.Collections.Generic;
using Godot;
using PVZHE.ModEditor.Inspector;

namespace PVZHE.ModEditor.Blueprint;

[GlobalClass]
public partial class XWBPVariableData : RefCounted
{
	[Signal]
	public delegate void ChangeEventHandler();

	[Signal]
	public delegate void RemoveEventHandler();

	[Signal]
	public delegate void RenameEventHandler(string name);

	public XWBPScriptData Owner { get; set; }

	public int Id { get; set; }

	public string Name { get; set; }

	public Variant.Type Type { get; set; }

	public string ClassName { get; set; } = "";

	public Variant DefaultValue { get; set; }

	public void RemoveSelf()
	{
		EmitSignal(SignalName.Remove);
	}

	public void RenameSelf(string newName)
	{
		Name = newName;
		EmitSignal(SignalName.Rename, newName);
	}

	public List<XWInspectorProperty> GetProperties()
	{
		return new List<XWInspectorProperty>
		{
			new XWInspectorProperty(this, "", default(Variant), "属性", "", PropertyHint.None, "", Callable.From(VariableSet))
		};
	}

	public void VariableSet()
	{
		EmitSignal(SignalName.Change);
	}

	public XWBPVariableData Duplicate()
	{
		XWBPVariableSerializeData xWBPVariableSerializeData = new XWBPVariableSerializeData();
		xWBPVariableSerializeData.Serialize(this);
		return xWBPVariableSerializeData.Deserialize();
	}
}
