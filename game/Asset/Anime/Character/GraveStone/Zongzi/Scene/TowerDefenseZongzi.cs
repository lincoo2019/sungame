using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZongzi : TowerDefenseGravestone
{
	private static readonly Vector2I[] SURROUND_OFFSETS = new Vector2I[9]
	{
		new Vector2I(-1, -1),
		new Vector2I(0, -1),
		new Vector2I(1, -1),
		new Vector2I(-1, 0),
		new Vector2I(0, 0),
		new Vector2I(1, 0),
		new Vector2I(-1, 1),
		new Vector2I(0, 1),
		new Vector2I(1, 1)
	};

	private const float HEAL_PERCENTAGE = 0.3f;

	public EntryAnimationComponent entryAnimationComponent;

	public bool over;

	public override void _Ready()
	{
		base._Ready();
		if (Engine.IsEditorHint())
		{
			return;
		}
		RemoveFromGroup("Gravestone");
		if (GodotObject.IsInstanceValid(componentManager))
		{
			entryAnimationComponent = componentManager.GetRuntime<EntryAnimationComponent>();
			EntryAnimationComponent obj = entryAnimationComponent;
			if (obj != null && !obj.IsReleased && (!Global.IsEditor || !(SceneManager.CurrentScene == "LevelEditorStage")) && (!GodotObject.IsInstanceValid(TowerDefenseManager.CurrentControl) || !TowerDefenseManager.CurrentControl.hasProgress))
			{
				entryAnimationComponent.PlayConfiguredFallBounce((float)GD.RandRange(0.25, 1.0));
			}
		}
	}

	public override void AttackDeal(TowerDefenseCharacter character, string type, double num)
	{
		base.AttackDeal(character, type, num);
		if (character is TowerDefenseZombie towerDefenseZombie && !(towerDefenseZombie.instance.hitpoints >= towerDefenseZombie.instance.hitpointsSave))
		{
			towerDefenseZombie.Health(num);
			if (towerDefenseZombie.instance.hitpoints > towerDefenseZombie.instance.hitpointsSave)
			{
				towerDefenseZombie.instance.hitpoints = towerDefenseZombie.instance.hitpointsSave;
			}
			ShowHealthComponent obj = towerDefenseZombie.showHealthComponent;
			if (obj != null && !obj.IsReleased)
			{
				towerDefenseZombie.showHealthComponent.MarkDirty();
			}
		}
	}

	public override async void DestroySet()
	{
		if (over)
		{
			return;
		}
		over = true;
		if (!Global.IsMultiplayerMode || MultiPlayerManager.IsHost)
		{
			Vector2I[] sURROUND_OFFSETS = SURROUND_OFFSETS;
			foreach (Vector2I vector2I in sURROUND_OFFSETS)
			{
				TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(base.gridPos + vector2I);
				if (!GodotObject.IsInstanceValid(mapCell))
				{
					continue;
				}
				foreach (TowerDefenseCharacter character in mapCell.GetCharacterList())
				{
					if (GodotObject.IsInstanceValid(character) && character is TowerDefensePlant && !(character is TowerDefensePlantBowlingBase) && !character.die && !character.nearDie && !(character.instance.hitpoints >= character.instance.hitpointsSave))
					{
						double num = character.instance.hitpointsSave * 0.30000001192092896;
						character.Health(num);
						if (character.instance.hitpoints > character.instance.hitpointsSave)
						{
							character.instance.hitpoints = character.instance.hitpointsSave;
						}
					}
				}
			}
		}
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { ["over"] = over };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		over = data.GetValueOrDefault("over", false).AsBool();
	}
}
