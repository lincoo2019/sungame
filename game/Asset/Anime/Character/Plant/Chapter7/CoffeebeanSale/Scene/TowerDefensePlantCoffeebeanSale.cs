using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantCoffeebeanSale : TowerDefensePlant
{
	private ExplodeComponent _explodeComponent;

	[Export(PropertyHint.None, "")]
	public TowerDefensePacketChangeCost changeCost;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_explodeComponent = componentManager.GetRuntime<ExplodeComponent>();
			_explodeComponent.OnExplode += Explode;
			changeCost = (TowerDefensePacketChangeCost)changeCost.Duplicate();
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		ExplodeComponent explodeComponent = _explodeComponent;
		if (explodeComponent != null && !explodeComponent.IsReleased)
		{
			_explodeComponent.OnExplode -= Explode;
		}
	}

	public override void PreSpawn()
	{
		base.PreSpawn();
		if (!GodotObject.IsInstanceValid(cell))
		{
			return;
		}
		List<TowerDefenseCharacter> characterList = cell.GetCharacterList();
		Array<TowerDefenseCharacter> array = new Array<TowerDefenseCharacter>();
		foreach (TowerDefenseCharacter item in characterList)
		{
			if (item is TowerDefensePlant && item.config.name != base.config.name && item.camp == base.camp)
			{
				array.Add(item);
			}
		}
		if (array.Count <= 0)
		{
			return;
		}
		foreach (TowerDefenseCharacter item2 in array)
		{
			if (GodotObject.IsInstanceValid(item2))
			{
				item2.WakeUp();
				if (!instance.hypnoses)
				{
					item2.packet.ChangeCostAdd(changeCost);
				}
			}
		}
	}

	public void Explode()
	{
		if (!GodotObject.IsInstanceValid(cell))
		{
			return;
		}
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		List<TowerDefenseCharacter> characterList = cell.GetCharacterList();
		Array<TowerDefenseCharacter> array = new Array<TowerDefenseCharacter>();
		foreach (TowerDefenseCharacter item in characterList)
		{
			if (item is TowerDefensePlant && item.config.name != base.config.name && item.camp == base.camp)
			{
				array.Add(item);
			}
		}
		if (array.Count <= 0)
		{
			return;
		}
		foreach (TowerDefenseCharacter item2 in array)
		{
			if (GodotObject.IsInstanceValid(item2))
			{
				item2.WakeUp();
				if (instance.hypnoses || !item2.packet.ChangeCostAdd(changeCost))
				{
					if (instance.hypnoses)
					{
						BrainSunCreate(logicalGlobalPosition, 50L);
					}
					else
					{
						SunCreate(logicalGlobalPosition, 50L);
					}
				}
			}
			else if (instance.hypnoses)
			{
				BrainSunCreate(logicalGlobalPosition, 50L);
			}
			else
			{
				SunCreate(logicalGlobalPosition, 50L);
			}
		}
	}
}
