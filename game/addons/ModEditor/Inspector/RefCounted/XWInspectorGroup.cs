using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorGroup : RefCounted
{
	public XWInspectorGroupContainer GroupContainer;

	public StringName GroupName = "";

	public XWInspectorGroup()
	{
	}

	public XWInspectorGroup(StringName groupName)
	{
		GroupName = groupName;
	}

	public void SetContainer(XWInspectorGroupContainer container)
	{
		GroupContainer = container;
	}
}
