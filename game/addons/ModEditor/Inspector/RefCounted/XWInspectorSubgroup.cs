using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorSubgroup : RefCounted
{
	public XWInspectorSubgroupContainer SubgroupContainer;

	public StringName SubgroupName = "";

	public XWInspectorSubgroup()
	{
	}

	public XWInspectorSubgroup(StringName subgroupName)
	{
		SubgroupName = subgroupName;
	}

	public void SetContainer(XWInspectorSubgroupContainer container)
	{
		SubgroupContainer = container;
	}
}
