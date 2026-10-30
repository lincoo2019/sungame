using Godot;

[GlobalClass]
public partial class ShovelPickTool : PacketPickTool
{
	public ShovelManager shovelManager;

	public override void Init(TowerDefenseMapControl _mapControl)
	{
		base.Init(_mapControl);
	}

	public void SetShovelManager(ShovelManager _shovelManager)
	{
		shovelManager = _shovelManager;
	}

	public override bool IsPicking()
	{
		if (GodotObject.IsInstanceValid(shovelManager))
		{
			return shovelManager.shovelPick;
		}
		return false;
	}

	public override void PickTool(bool open)
	{
		if (GodotObject.IsInstanceValid(shovelManager))
		{
			shovelManager.PickShovel(open);
		}
	}

	public override void ProcessPick(TowerDefenseCellInstance cell, Vector2I gridPos, Vector2 mousePos)
	{
		if (GodotObject.IsInstanceValid(shovelManager))
		{
			shovelManager.ProcessShovelPick(cell, gridPos, mousePos);
		}
	}

	public override void ToolRelease()
	{
		if (GodotObject.IsInstanceValid(shovelManager))
		{
			shovelManager.ShovelRelease();
		}
	}

	public override void ToolReset()
	{
		if (GodotObject.IsInstanceValid(shovelManager))
		{
			shovelManager.ShovelReset();
		}
	}

	public override Node2D GetMapSprite()
	{
		if (GodotObject.IsInstanceValid(shovelManager) && GodotObject.IsInstanceValid(shovelManager.mapShovelSprite))
		{
			return shovelManager.mapShovelSprite;
		}
		return null;
	}
}
