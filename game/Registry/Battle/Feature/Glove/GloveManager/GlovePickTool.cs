using Godot;

[GlobalClass]
public partial class GlovePickTool : PacketPickTool
{
	public TowerDefenseBattleFeatureGlove gloveFeature;

	public override void Init(TowerDefenseMapControl _mapControl)
	{
		base.Init(_mapControl);
	}

	public void SetGloveFeature(TowerDefenseBattleFeatureGlove _gloveFeature)
	{
		gloveFeature = _gloveFeature;
	}

	public override bool IsPicking()
	{
		if (GodotObject.IsInstanceValid(gloveFeature))
		{
			return gloveFeature.glovePick;
		}
		return false;
	}

	public override void PickTool(bool open)
	{
		if (GodotObject.IsInstanceValid(gloveFeature))
		{
			gloveFeature.PickGlove(open);
		}
	}

	public override void ProcessPick(TowerDefenseCellInstance cell, Vector2I gridPos, Vector2 mousePos)
	{
		if (GodotObject.IsInstanceValid(gloveFeature))
		{
			gloveFeature.ProcessGlovePick(cell, gridPos, mousePos);
		}
	}

	public override void ToolRelease()
	{
		if (GodotObject.IsInstanceValid(gloveFeature))
		{
			gloveFeature.GloveRelease();
		}
	}

	public override void ToolReset()
	{
		if (GodotObject.IsInstanceValid(gloveFeature))
		{
			gloveFeature.GloveReset();
		}
	}

	public override Node2D GetMapSprite()
	{
		if (GodotObject.IsInstanceValid(gloveFeature) && GodotObject.IsInstanceValid(gloveFeature.gloveManager) && GodotObject.IsInstanceValid(gloveFeature.gloveManager.mapGloveSprite))
		{
			return gloveFeature.gloveManager.mapGloveSprite;
		}
		return null;
	}
}
