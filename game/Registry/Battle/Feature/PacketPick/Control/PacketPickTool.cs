using Godot;

[GlobalClass]
public partial class PacketPickTool : Node
{
	public TowerDefenseMapControl mapControl;

	public bool toolPick;

	public virtual void Init(TowerDefenseMapControl _mapControl)
	{
		mapControl = _mapControl;
	}

	public virtual bool IsPicking()
	{
		return toolPick;
	}

	public virtual void PickTool(bool open)
	{
		toolPick = open;
	}

	public virtual void ProcessPick(TowerDefenseCellInstance cell, Vector2I gridPos, Vector2 mousePos)
	{
	}

	public virtual void ToolRelease()
	{
		toolPick = false;
	}

	public virtual void ToolReset()
	{
		toolPick = false;
	}

	public virtual Node2D GetMapSprite()
	{
		return null;
	}
}
