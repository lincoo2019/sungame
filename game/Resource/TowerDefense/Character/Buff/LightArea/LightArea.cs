using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class LightArea : AabbArea2D
{
	public override void _PhysicsProcess(double delta)
	{
		if (!AabbShapeUtil.TryComputeAreaWorldRect(this, out var rect))
		{
			return;
		}
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (GodotObject.IsInstanceValid(instance) && instance.characterRegistry != null)
		{
			List<TowerDefenseCharacter> charactersIntersectingRectList = instance.characterRegistry.GetCharactersIntersectingRectList(rect);
			for (int i = 0; i < charactersIntersectingRectList.Count; i++)
			{
				RevealCharacter(charactersIntersectingRectList[i]);
			}
		}
	}

	private void RevealCharacter(TowerDefenseCharacter tdChar)
	{
		if (GodotObject.IsInstanceValid(tdChar) && GodotObject.IsInstanceValid(tdChar.sprite) && tdChar.sprite.invisible)
		{
			tdChar.sprite.invisible = false;
		}
	}
}
