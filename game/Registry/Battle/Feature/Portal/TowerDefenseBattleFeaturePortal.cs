using System.ComponentModel;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseBattleFeaturePortal : TowerDefenseBattleFeature
{
	private static PackedScene _towerDefensePortal;

	public Array<TowerDefensePortal> portalList = new Array<TowerDefensePortal>();

	private static PackedScene TOWER_DEFENSE_PORTAL => _towerDefensePortal ?? (_towerDefensePortal = GD.Load<PackedScene>("uid://dral054wqsme5"));

	public TowerDefensePortal PortalCreate(string shape, Vector4I posRange, double changeTime = 0.0)
	{
		bool flag = !base.IsLifetimeActive;
		if (!flag)
		{
			bool flag2;
			switch (shape)
			{
			case "Circle":
			case "Square":
			case "Rhombus":
				flag2 = true;
				break;
			default:
				flag2 = false;
				break;
			}
			flag = !flag2;
		}
		if (flag)
		{
			GD.PushWarning("[Portal] Unsupported portal shape '" + shape + "'.");
			return null;
		}
		Node2D characterNode = TowerDefenseManager.GetCharacterNode();
		if (!GodotObject.IsInstanceValid(characterNode))
		{
			return null;
		}
		TowerDefensePortal towerDefensePortal = TOWER_DEFENSE_PORTAL.Instantiate<TowerDefensePortal>(PackedScene.GenEditState.Disabled);
		characterNode.AddChild(towerDefensePortal, forceReadableName: false, Node.InternalMode.Disabled);
		towerDefensePortal.Init(shape, posRange, changeTime);
		portalList.Add(towerDefensePortal);
		return towerDefensePortal;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public void ProtalCreate(string shape, Vector4I posRange, double changeTime = 0.0)
	{
		PortalCreate(shape, posRange, changeTime);
	}

	public void PortalChangePos()
	{
		foreach (TowerDefensePortal portal in portalList)
		{
			if (GodotObject.IsInstanceValid(portal))
			{
				portal.ChangePos();
			}
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public void ProtalChangePos()
	{
		PortalChangePos();
	}

	public override void GameFail()
	{
		ClearPortals();
	}

	public override Dictionary SyncSerialize()
	{
		Array array = new Array();
		foreach (TowerDefensePortal portal in portalList)
		{
			if (GodotObject.IsInstanceValid(portal))
			{
				array.Add(new Dictionary
				{
					["shape"] = portal.shape,
					["pos_range_x"] = portal.posRange.X,
					["pos_range_y"] = portal.posRange.Y,
					["pos_range_z"] = portal.posRange.Z,
					["pos_range_w"] = portal.posRange.W,
					["change_time"] = portal.changeTime,
					["change_timer"] = portal.changeTimer,
					["grid_pos1_x"] = portal.gridPos1.X,
					["grid_pos1_y"] = portal.gridPos1.Y,
					["grid_pos2_x"] = portal.gridPos2.X,
					["grid_pos2_y"] = portal.gridPos2.Y
				});
			}
		}
		return new Dictionary { ["portals"] = array };
	}

	public override void SyncDeserialize(Dictionary _data)
	{
		if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
		{
			return;
		}
		Array array = _data.GetValueOrDefault("portals", new Array()).AsGodotArray();
		Dictionary dictionary = new Dictionary();
		foreach (TowerDefensePortal portal in portalList)
		{
			if (GodotObject.IsInstanceValid(portal))
			{
				dictionary[portal.GetInstanceId()] = true;
			}
		}
		foreach (Variant item in array)
		{
			if (item.VariantType != Variant.Type.Dictionary)
			{
				continue;
			}
			Dictionary dictionary2 = item.AsGodotDictionary();
			string text = dictionary2.GetValueOrDefault("shape", "").AsString();
			int x = dictionary2.GetValueOrDefault("pos_range_x", 0).AsInt32();
			int y = dictionary2.GetValueOrDefault("pos_range_y", 0).AsInt32();
			int z = dictionary2.GetValueOrDefault("pos_range_z", 0).AsInt32();
			int w = dictionary2.GetValueOrDefault("pos_range_w", 0).AsInt32();
			double changeTime = dictionary2.GetValueOrDefault("change_time", 0.0).AsDouble();
			double changeTimer = dictionary2.GetValueOrDefault("change_timer", 0.0).AsDouble();
			int x2 = dictionary2.GetValueOrDefault("grid_pos1_x", 0).AsInt32();
			int y2 = dictionary2.GetValueOrDefault("grid_pos1_y", 0).AsInt32();
			int x3 = dictionary2.GetValueOrDefault("grid_pos2_x", 0).AsInt32();
			int y3 = dictionary2.GetValueOrDefault("grid_pos2_y", 0).AsInt32();
			bool flag = false;
			foreach (TowerDefensePortal portal2 in portalList)
			{
				if (GodotObject.IsInstanceValid(portal2) && portal2.shape == text && dictionary.ContainsKey(portal2.GetInstanceId()))
				{
					ApplyPortalState(portal2, new Vector4I(x, y, z, w), changeTime, changeTimer, new Vector2I(x2, y2), new Vector2I(x3, y3));
					dictionary.Remove(portal2.GetInstanceId());
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				Vector4I posRange = new Vector4I(x, y, z, w);
				TowerDefensePortal towerDefensePortal = PortalCreate(text, posRange, changeTime);
				if (GodotObject.IsInstanceValid(towerDefensePortal))
				{
					ApplyPortalState(towerDefensePortal, posRange, changeTime, changeTimer, new Vector2I(x2, y2), new Vector2I(x3, y3));
				}
			}
		}
		for (int num = portalList.Count - 1; num >= 0; num--)
		{
			TowerDefensePortal towerDefensePortal2 = portalList[num];
			if (!GodotObject.IsInstanceValid(towerDefensePortal2) || dictionary.ContainsKey(towerDefensePortal2.GetInstanceId()))
			{
				if (GodotObject.IsInstanceValid(towerDefensePortal2))
				{
					towerDefensePortal2.QueueFree();
				}
				portalList.RemoveAt(num);
			}
		}
	}

	public override Dictionary SaveFeature()
	{
		Array array = new Array();
		foreach (TowerDefensePortal portal in portalList)
		{
			if (GodotObject.IsInstanceValid(portal))
			{
				array.Add(new Dictionary
				{
					["shape"] = portal.shape,
					["pos_range_x"] = portal.posRange.X,
					["pos_range_y"] = portal.posRange.Y,
					["pos_range_z"] = portal.posRange.Z,
					["pos_range_w"] = portal.posRange.W,
					["change_time"] = portal.changeTime,
					["change_timer"] = portal.changeTimer,
					["grid_pos1_x"] = portal.gridPos1.X,
					["grid_pos1_y"] = portal.gridPos1.Y,
					["grid_pos2_x"] = portal.gridPos2.X,
					["grid_pos2_y"] = portal.gridPos2.Y
				});
			}
		}
		return new Dictionary { ["portals"] = array };
	}

	public override void LoadFeature(Dictionary _data, TowerDefenseLevelSaveConfigCSharp _owner)
	{
		ClearPortals();
		foreach (Variant item in _data.GetValueOrDefault("portals", new Array()).AsGodotArray())
		{
			Dictionary dictionary = item.AsGodotDictionary();
			string shape = dictionary.GetValueOrDefault("shape", "").AsString();
			int x = dictionary.GetValueOrDefault("pos_range_x", 0).AsInt32();
			int y = dictionary.GetValueOrDefault("pos_range_y", 0).AsInt32();
			int z = dictionary.GetValueOrDefault("pos_range_z", 0).AsInt32();
			int w = dictionary.GetValueOrDefault("pos_range_w", 0).AsInt32();
			double changeTime = dictionary.GetValueOrDefault("change_time", 0.0).AsDouble();
			double changeTimer = dictionary.GetValueOrDefault("change_timer", 0.0).AsDouble();
			int x2 = dictionary.GetValueOrDefault("grid_pos1_x", 0).AsInt32();
			int y2 = dictionary.GetValueOrDefault("grid_pos1_y", 0).AsInt32();
			int x3 = dictionary.GetValueOrDefault("grid_pos2_x", 0).AsInt32();
			int y3 = dictionary.GetValueOrDefault("grid_pos2_y", 0).AsInt32();
			Vector4I posRange = new Vector4I(x, y, z, w);
			TowerDefensePortal towerDefensePortal = PortalCreate(shape, posRange, changeTime);
			if (GodotObject.IsInstanceValid(towerDefensePortal))
			{
				ApplyPortalState(towerDefensePortal, posRange, changeTime, changeTimer, new Vector2I(x2, y2), new Vector2I(x3, y3));
			}
		}
	}

	private static void ApplyPortalState(TowerDefensePortal portal, Vector4I posRange, double changeTime, double changeTimer, Vector2I gridPos1, Vector2I gridPos2)
	{
		portal.posRange = posRange;
		portal.changeTime = Mathf.Max(0.0, changeTime);
		portal.changeTimer = ((portal.changeTime > 0.0) ? Mathf.Clamp(changeTimer, 0.0, portal.changeTime) : 0.0);
		portal.gridPos1 = gridPos1;
		portal.gridPos2 = gridPos2;
		Vector2 mapGridSize = TowerDefenseManager.Instance.GetMapGridSize();
		portal.GetNode<Node2D>("%ProtalNode1").GlobalPosition = TowerDefenseManager.GetMapCellPlantPos(gridPos1) + new Vector2(mapGridSize.X / 2f, 0f);
		portal.GetNode<Node2D>("%ProtalNode2").GlobalPosition = TowerDefenseManager.GetMapCellPlantPos(gridPos2) + new Vector2(mapGridSize.X / 2f, 0f);
		portal.protalSprite1.ZIndex = gridPos1.Y * 15;
		portal.protalSprite2.ZIndex = gridPos2.Y * 15;
	}

	private void ClearPortals()
	{
		foreach (TowerDefensePortal portal in portalList)
		{
			if (GodotObject.IsInstanceValid(portal))
			{
				portal.QueueFree();
			}
		}
		portalList.Clear();
	}

	public override void Destroy()
	{
		ClearPortals();
		base.Destroy();
	}
}
