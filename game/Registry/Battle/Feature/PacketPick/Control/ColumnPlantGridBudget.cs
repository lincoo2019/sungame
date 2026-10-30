using System.Collections.Generic;
using Godot;

internal sealed class ColumnPlantGridBudget
{
	private readonly TowerDefensePacketConfig _packet;

	private readonly int _limit;

	private readonly HashSet<Vector2I> _occupied = new HashSet<Vector2I>();

	internal ColumnPlantGridBudget(TowerDefensePacketConfig packet, int? limit = null)
	{
		_packet = packet;
		TowerDefenseControlNew currentControl = TowerDefenseManager.Instance.currentControl;
		_limit = limit ?? (currentControl?.levelConfig as TowerDefenseLevelConfig)?.limitGridPlantNum ?? (-1);
		if (!(packet.characterConfig is TowerDefensePlantConfig) || !packet.plantUseCell || !packet.IsLimitGridNum() || (Global.IsEditor && SceneManager.CurrentScene == "LevelEditorStage") || (TowerDefenseManager.Instance.IsIZMMode() && packet.izmPlantAllCell))
		{
			_limit = -1;
		}
		if (_limit < 0)
		{
			return;
		}
		TowerDefenseBattleFeatureMap mapFeature = TowerDefenseManager.GetMapFeature();
		for (int i = 1; i <= mapFeature.config.gridNum.X; i++)
		{
			for (int j = 1; j <= mapFeature.config.gridNum.Y; j++)
			{
				Vector2I vector2I = new Vector2I(i, j);
				if (TowerDefenseManager.GetMapCell(vector2I).HasPlant())
				{
					_occupied.Add(vector2I);
				}
			}
		}
		if (!GodotObject.IsInstanceValid(currentControl))
		{
			return;
		}
		foreach (TowerDefenseCharacter value in currentControl._syncCharacters.Values)
		{
			if (GodotObject.IsInstanceValid(value) && !value.IsInsideTree() && !value.IsQueuedForDeletion() && value.config is TowerDefensePlantConfig && GodotObject.IsInstanceValid(value.packet) && value.packet.plantUseCell && value.itemLayer != TowerDefenseEnum.LAYER_GROUNDITEM.EFFECT)
			{
				AddFootprint(value.packet, value.gridPos);
			}
		}
	}

	internal bool CanPlace(Vector2I position)
	{
		if (_limit < 0)
		{
			return true;
		}
		HashSet<Vector2I> hashSet = new HashSet<Vector2I>();
		if (!_occupied.Contains(position))
		{
			hashSet.Add(position);
		}
		foreach (Vector2I item in ((TowerDefensePlantConfig)_packet.characterConfig).extendGrid)
		{
			if (!_occupied.Contains(position + item))
			{
				hashSet.Add(position + item);
			}
		}
		if (hashSet.Count != 0)
		{
			return _occupied.Count + hashSet.Count <= _limit;
		}
		return true;
	}

	internal void Reserve(Vector2I position)
	{
		if (_limit >= 0)
		{
			AddFootprint(_packet, position);
		}
	}

	private void AddFootprint(TowerDefensePacketConfig packet, Vector2I position)
	{
		_occupied.Add(position);
		foreach (Vector2I item in ((TowerDefensePlantConfig)packet.characterConfig).extendGrid)
		{
			_occupied.Add(position + item);
		}
	}
}
