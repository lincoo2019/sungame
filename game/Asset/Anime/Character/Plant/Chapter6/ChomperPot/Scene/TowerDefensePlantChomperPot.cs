using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantChomperPot : TowerDefensePlant
{
	public AttackComponent attackComponent;

	public AttackComponent attackComponent2;

	public ChomperComponent chomperComponent;

	private double _chewTime = 30.0;

	[Export(PropertyHint.None, "")]
	public double chewTime
	{
		get
		{
			return _chewTime;
		}
		set
		{
			_chewTime = value;
			if (IsNodeReady() && chomperComponent != null && !chomperComponent.IsReleased)
			{
				chomperComponent.chewTime = (float)value;
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			attackComponent = componentManager.GetRuntime<AttackComponent>("character.attack.0");
			attackComponent2 = componentManager.GetRuntime<AttackComponent>("character.attack.1");
			chomperComponent = componentManager.GetRuntime<ChomperComponent>();
			attackComponent.SetCheckAreaRectangleWidth(0, TowerDefenseManager.Instance.GetMapGridSize().X * 1.25f);
			AddToGroup("ChomperPot");
			if (chomperComponent != null && !chomperComponent.IsReleased)
			{
				chomperComponent.OnChewProcessing += ChewProcessing;
			}
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		if (chomperComponent != null && !chomperComponent.IsReleased)
		{
			chomperComponent.OnChewProcessing -= ChewProcessing;
		}
	}

	public virtual void ChewProcessing(double delta)
	{
		Teleport();
	}

	public virtual void Teleport()
	{
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return;
		}
		ChomperComponent obj = chomperComponent;
		if (obj == null || obj.IsReleased || !chomperComponent.isChew)
		{
			return;
		}
		AttackComponent obj2 = attackComponent2;
		if (obj2 == null || obj2.IsReleased || !attackComponent2.CanAttackOnContact())
		{
			return;
		}
		List<TowerDefenseCharacter> list = new List<TowerDefenseCharacter>(attackComponent2.GetTargetList());
		if (list.Count == 0)
		{
			return;
		}
		List<TowerDefenseCharacter> list2 = new List<TowerDefenseCharacter>();
		foreach (Node item in GetTree().GetNodesInGroup("ChomperPot"))
		{
			if (item is TowerDefensePlantChomperPot towerDefensePlantChomperPot && towerDefensePlantChomperPot != this && !towerDefensePlantChomperPot.die && !towerDefensePlantChomperPot.isDestroy && !towerDefensePlantChomperPot.IsQueuedForDeletion())
			{
				ChomperComponent obj3 = towerDefensePlantChomperPot.chomperComponent;
				if (obj3 != null && !obj3.IsReleased && towerDefensePlantChomperPot.chomperComponent.isChew)
				{
					list2.Add(towerDefensePlantChomperPot);
				}
			}
		}
		if (list2.Count == 0)
		{
			return;
		}
		TowerDefenseCharacter towerDefenseCharacter = list2[GD.RandRange(0, list2.Count - 1)];
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		Vector2 logicalGlobalPosition2 = towerDefenseCharacter.GetLogicalGlobalPosition();
		foreach (TowerDefenseCharacter item2 in list)
		{
			if (item2 is TowerDefenseZombie && item2.instance.zombiePhysique <= TowerDefenseEnum.ZOMBIE_PHYSIQUE.NORMAL)
			{
				Vector2 logicalGlobalPosition3 = item2.GetLogicalGlobalPosition();
				if (!(logicalGlobalPosition3.X < logicalGlobalPosition.X - 10f))
				{
					double num = (0f - item2.Scale.X) * TowerDefenseManager.Instance.GetMapGridSize().X * 0.5f;
					item2.shadowComponent.saveShadowPosition.Y += logicalGlobalPosition2.Y - logicalGlobalPosition3.Y;
					item2.SetLogicalGlobalPosition(logicalGlobalPosition2 + new Vector2((float)num, 0f));
					item2.gridPos = towerDefenseCharacter.gridPos;
				}
			}
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { { "chewTime", chewTime } };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		chewTime = data.GetValueOrDefault("chewTime", 30.0).AsDouble();
	}
}
