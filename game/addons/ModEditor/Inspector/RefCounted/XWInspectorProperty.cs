using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorProperty : RefCounted
{
	public Callable SetCallable;

	public GodotObject Object { get; set; }

	public StringName PropName { get; set; } = "";

	public Variant RestValue { get; set; }

	public StringName GroupName { get; set; } = "";

	public StringName SubgroupName { get; set; } = "";

	public StringName CategoryName { get; set; } = "";

	public StringName GroupBase { get; set; } = "";

	public StringName SubgroupBase { get; set; } = "";

	public int SectionDepth { get; set; }

	public PropertyHint Hint { get; set; }

	public string HintString { get; set; } = "";

	public bool ReadOnly { get; set; }

	public string Description { get; set; } = "";

	public long PropertyUsage { get; set; }

	public string LabelOverride { get; set; } = "";

	public XWInspectorProperty()
	{
	}

	public XWInspectorProperty(GodotObject obj, StringName propName, Variant restValue = default(Variant), StringName groupName = null, StringName subgroupName = null, PropertyHint hint = PropertyHint.None, string hintString = "", Callable setCallable = default(Callable))
	{
		Object = obj;
		PropName = propName;
		RestValue = restValue;
		GroupName = groupName ?? ((StringName)"");
		SubgroupName = subgroupName ?? ((StringName)"");
		Hint = hint;
		HintString = hintString;
		SetCallable = setCallable;
	}

	public void SetCall()
	{
		if (!SetCallable.Equals(default(Callable)))
		{
			SetCallable.Call();
		}
	}
}
