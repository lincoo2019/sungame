using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantJewelBomb : TowerDefensePlant
{
	private AttackComponent _attackComponent;

	private ExplodeComponent _explodeComponent;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !editorPreviewMode)
		{
			_attackComponent = componentManager.GetRuntime<AttackComponent>("character.attack.0");
			_explodeComponent = componentManager.GetRuntime<ExplodeComponent>();
			_explodeComponent.OnExplode += Explode;
			_attackComponent.SetCheckAreaRectangleSize(0, TowerDefenseManager.Instance.GetMapGridSize() * 2.75f);
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

	public void Explode()
	{
		Dictionary dictionary = new Dictionary();
		foreach (TowerDefenseCharacter item in new List<TowerDefenseCharacter>(_attackComponent.GetCharcterList()))
		{
			if (!(item is TowerDefenseZombie) || item.instance.zombiePhysique != TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS)
			{
				string name = item.config.name;
				if (!dictionary.ContainsKey(name))
				{
					dictionary[name] = new Dictionary
					{
						{ "Num", 0 },
						{
							"Type",
							(item is TowerDefensePlant) ? "Plant" : "Zombie"
						},
						{
							"CharacterList",
							new Array()
						}
					};
				}
				((Dictionary)dictionary[name])["Num"] = ((Dictionary)dictionary[name])["Num"].AsInt32() + 1;
				((Array)((Dictionary)dictionary[name])["CharacterList"]).Add(item);
			}
		}
		foreach (Variant key in dictionary.Keys)
		{
			Dictionary dictionary2 = (Dictionary)dictionary[key];
			if (dictionary2["Num"].AsInt32() < 3)
			{
				continue;
			}
			string text = dictionary2["Type"].AsString();
			if (!(text == "Plant"))
			{
				if (!(text == "Zombie"))
				{
					continue;
				}
				foreach (Variant item2 in (Array)dictionary2["CharacterList"])
				{
					TowerDefenseCharacter obj = (TowerDefenseCharacter)(GodotObject)item2;
					obj.SunCreate(obj.GetLogicalGlobalPosition(), 25L);
					obj.Destroy();
				}
				continue;
			}
			foreach (Variant item3 in (Array)dictionary2["CharacterList"])
			{
				TowerDefenseCharacter towerDefenseCharacter = (TowerDefenseCharacter)(GodotObject)item3;
				towerDefenseCharacter.SunCreate(towerDefenseCharacter.GetLogicalGlobalPosition(), (int)towerDefenseCharacter.cost);
				towerDefenseCharacter.Destroy();
			}
		}
	}
}
