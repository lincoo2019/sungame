using System.Collections.Generic;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseBattleFeatureWarningLine : TowerDefenseBattleFeature
{
	private static PackedScene _warningLineScene;

	private readonly List<WarningLine> _warningLines = new List<WarningLine>();

	private bool _triggered;

	private static PackedScene WarningLineScene => _warningLineScene ?? (_warningLineScene = GD.Load<PackedScene>("uid://7e2ylijvno4n"));

	public void AddWarningLine(int row)
	{
		AddWarningColumn(row);
	}

	public void AddWarningColumn(int column)
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		Node2D characterNode = TowerDefenseManager.GetCharacterNode();
		if (!GodotObject.IsInstanceValid(instance) || !GodotObject.IsInstanceValid(characterNode) || !GodotObject.IsInstanceValid(WarningLineScene))
		{
			return;
		}
		int x = instance.GetMapGridNum().X;
		if (column < 0 || column >= x)
		{
			GD.PushWarning($"[WarningLine] Column {column} is outside the map range [0, {x}).");
			return;
		}
		foreach (WarningLine warningLine2 in _warningLines)
		{
			if (GodotObject.IsInstanceValid(warningLine2) && warningLine2.column == column)
			{
				return;
			}
		}
		WarningLine warningLine = WarningLineScene.Instantiate<WarningLine>(PackedScene.GenEditState.Disabled);
		characterNode.AddChild(warningLine, forceReadableName: false, Node.InternalMode.Disabled);
		warningLine.GlobalPosition = instance.GetMapCellPos(new Vector2I(column + 1, 1)) + new Vector2(-10f, 0f);
		Sprite2D node = warningLine.GetNode<Sprite2D>("%Sprite");
		Vector2 scale = node.Scale;
		scale.Y = (instance.GetMapGridSize().X * (float)instance.GetMapGridNum().X - 20f) / 502f;
		node.Scale = scale;
		warningLine.feature = this;
		warningLine.column = column;
		_warningLines.Add(warningLine);
	}

	public override void Process(double delta)
	{
		if (_triggered || (Global.IsMultiplayerMode && (MultiPlayerManager.Instance == null || !MultiPlayerManager.Instance.isHost)))
		{
			return;
		}
		for (int num = _warningLines.Count - 1; num >= 0; num--)
		{
			WarningLine warningLine = _warningLines[num];
			if (!GodotObject.IsInstanceValid(warningLine))
			{
				_warningLines.RemoveAt(num);
			}
			else
			{
				warningLine.UpdateOverlaps();
			}
		}
	}

	public void OnWarningLineTriggered()
	{
		if (_triggered)
		{
			return;
		}
		_triggered = true;
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (GodotObject.IsInstanceValid(instance?.characterRegistry))
		{
			foreach (TowerDefenseCharacter cleanCharacters in instance.characterRegistry.GetCleanCharactersList())
			{
				cleanCharacters.ProcessMode = Node.ProcessModeEnum.Disabled;
			}
		}
		control.GameFail(null);
	}

	public override Dictionary SaveFeature()
	{
		return SerializeState();
	}

	public override Dictionary SyncSerialize()
	{
		return SerializeState();
	}

	private Dictionary SerializeState()
	{
		Array array = new Array();
		foreach (WarningLine warningLine in _warningLines)
		{
			if (GodotObject.IsInstanceValid(warningLine))
			{
				array.Add(warningLine.column);
			}
		}
		return new Dictionary
		{
			{ "columns", array },
			{ "triggered", _triggered }
		};
	}

	public override void LoadFeature(Dictionary _data, TowerDefenseLevelSaveConfigCSharp _owner)
	{
		ApplyState(_data);
	}

	public override void SyncDeserialize(Dictionary _data)
	{
		ApplyState(_data);
	}

	private void ApplyState(Dictionary state)
	{
		bool flag = state.GetValueOrDefault("triggered", false).AsBool();
		HashSet<int> hashSet = new HashSet<int>();
		if (!flag)
		{
			foreach (Variant item in state.GetValueOrDefault("columns", state.GetValueOrDefault("rows", new Array())).AsGodotArray())
			{
				hashSet.Add(item.AsInt32());
			}
		}
		for (int num = _warningLines.Count - 1; num >= 0; num--)
		{
			WarningLine warningLine = _warningLines[num];
			if (!GodotObject.IsInstanceValid(warningLine) || !hashSet.Contains(warningLine.column))
			{
				_warningLines.RemoveAt(num);
				if (GodotObject.IsInstanceValid(warningLine))
				{
					warningLine.feature = null;
					warningLine.QueueFree();
				}
			}
		}
		_triggered = flag;
		if (_triggered)
		{
			return;
		}
		foreach (int item2 in hashSet)
		{
			AddWarningColumn(item2);
		}
	}

	public override void Destroy()
	{
		ClearWarningLines();
		_triggered = false;
		base.Destroy();
	}

	private void ClearWarningLines()
	{
		foreach (WarningLine warningLine in _warningLines)
		{
			if (GodotObject.IsInstanceValid(warningLine))
			{
				warningLine.feature = null;
				warningLine.QueueFree();
			}
		}
		_warningLines.Clear();
	}
}
