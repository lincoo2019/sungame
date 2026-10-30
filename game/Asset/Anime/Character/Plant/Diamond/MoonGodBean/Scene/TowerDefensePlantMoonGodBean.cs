using System.Collections.Generic;
using Godot;

[Tool]
public partial class TowerDefensePlantMoonGodBean : TowerDefensePlant
{
	private ExplodeComponent _explodeComponent;

	private const int SpawnBatchSize = 8;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_explodeComponent = componentManager.GetRuntime<ExplodeComponent>();
			_explodeComponent.OnExplode += Explode;
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
		List<TowerDefenseCharacter> list = new List<TowerDefenseCharacter>();
		foreach (Variant item in TowerDefenseManager.Instance.GetCampFriendly(base.camp))
		{
			TowerDefenseCharacter towerDefenseCharacter = item.As<TowerDefenseCharacter>();
			towerDefenseCharacter.WakeUp();
			list.Add(towerDefenseCharacter);
		}
		SceneTree tree = GetTree();
		int num = (list.Count + 8 - 1) / 8;
		for (int i = 0; i < num; i++)
		{
			int num2 = i * 8;
			int num3 = Mathf.Min(num2 + 8, list.Count);
			List<TowerDefenseCharacter> range = list.GetRange(num2, num3 - num2);
			if (i == 0)
			{
				foreach (TowerDefenseCharacter item2 in range)
				{
					if (GodotObject.IsInstanceValid(item2))
					{
						item2.SpawnZombie();
					}
				}
				continue;
			}
			TowerDefenseCharacter[] capturedBatch = range.ToArray();
			tree.CreateTimer(0.0).Timeout += delegate
			{
				TowerDefenseCharacter[] array = capturedBatch;
				foreach (TowerDefenseCharacter towerDefenseCharacter3 in array)
				{
					if (GodotObject.IsInstanceValid(towerDefenseCharacter3))
					{
						towerDefenseCharacter3.SpawnZombie();
					}
				}
			};
		}
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return;
		}
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		TowerDefenseCharacter towerDefenseCharacter2 = TowerDefenseCharacter.CreateCharacter("ZombieFlag", logicalGlobalPosition, base.gridPos, 0.0);
		towerDefenseCharacter2.Rise(2.5);
		if (!instance.hypnoses)
		{
			towerDefenseCharacter2.Hypnoses();
		}
		if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
		{
			TowerDefenseControlNew currentControl = TowerDefenseManager.Instance.currentControl;
			if (GodotObject.IsInstanceValid(currentControl))
			{
				int nextSyncId = currentControl.GetNextSyncId();
				currentControl.RegisterSyncCharacter(nextSyncId, towerDefenseCharacter2);
				MultiPlayerManager.Instance.SendSpawnCharacterAt("ZombieFlag", base.gridPos.X, base.gridPos.Y, nextSyncId, instance.hitpointScale, transformPoint.Scale.X, !instance.hypnoses, 2.5, useCreate: true, logicalGlobalPosition.X, logicalGlobalPosition.Y);
			}
		}
	}
}
