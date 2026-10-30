using System.Collections.Generic;
using Godot;
using Godot.Collections;

public partial class TowerDefenseMapFrontlawnEmpty : TowerDefenseMapRevealOnEnter
{
	private const string SodRollPath = "res://Asset/Anime/Effect/SodRoll/SodRoll.tscn";

	private const double SodRollRevealDistance = 790.0;

	private Sprite2D[] _frontlawnRow;

	private double[] _rowBeginForm;

	private double[] _rowEndForm;

	public bool alive;

	public int createRowId = -1;

	public AdobeAnimateSprite followSodRoll;

	protected override void RegisterEventFunctions(IDictionary<string, int> functions)
	{
		base.RegisterEventFunctions(functions);
		TowerDefenseMap.RegisterEventFunction(functions, "Row1Create", 0);
		TowerDefenseMap.RegisterEventFunction(functions, "Row3CreateFromRow1", 0);
		TowerDefenseMap.RegisterEventFunction(functions, "Row5CreateFromRow3", 0);
		TowerDefenseMap.RegisterEventFunction(functions, "ShowRow1", 0);
		TowerDefenseMap.RegisterEventFunction(functions, "ShowRow3", 0);
		TowerDefenseMap.RegisterEventFunction(functions, "ShowRow5", 0);
	}

	public override void _Ready()
	{
		base._Ready();
		_frontlawnRow = new Sprite2D[3]
		{
			GetNode<Sprite2D>("%FrontlawnSod1Row"),
			GetNode<Sprite2D>("%FrontlawnSod3Row"),
			GetNode<Sprite2D>("%FrontlawnSod5Row")
		};
		_rowBeginForm = new double[3] { 0.0, 0.0, 10.0 };
		_rowEndForm = new double[3] { 0.0, 2.0, 0.0 };
		for (int i = 0; i < _frontlawnRow.Length; i++)
		{
			_frontlawnRow[i].Visible = false;
			Rect2 regionRect = _frontlawnRow[i].RegionRect;
			_frontlawnRow[i].RegionRect = new Rect2(regionRect.Position, new Vector2((float)_rowBeginForm[i], regionRect.Size.Y));
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		if (alive && GodotObject.IsInstanceValid(followSodRoll))
		{
			double progress = followSodRoll.GetProgress();
			Rect2 regionRect = _frontlawnRow[createRowId].RegionRect;
			_frontlawnRow[createRowId].RegionRect = new Rect2(regionRect.Position, new Vector2((float)(_rowBeginForm[createRowId] + progress * 790.0 + _rowEndForm[createRowId]), regionRect.Size.Y));
		}
	}

	public override Dictionary SaveMapBase()
	{
		Dictionary dictionary = base.SaveMapBase();
		Array array = new Array();
		Array array2 = new Array();
		if (_frontlawnRow != null)
		{
			Sprite2D[] frontlawnRow = _frontlawnRow;
			foreach (Sprite2D sprite2D in frontlawnRow)
			{
				if (GodotObject.IsInstanceValid(sprite2D))
				{
					array.Add(sprite2D.Visible);
					array2.Add(sprite2D.RegionRect.Size.X);
				}
				else
				{
					array.Add(false);
					array2.Add(0.0);
				}
			}
		}
		dictionary["rowVisible"] = array;
		dictionary["rowRegionWidth"] = array2;
		return dictionary;
	}

	public override void LoadMapBase(Dictionary data)
	{
		base.LoadMapBase(data);
		if (_frontlawnRow == null)
		{
			return;
		}
		Array array = data.GetValueOrDefault("rowVisible", new Array()).AsGodotArray();
		Array array2 = data.GetValueOrDefault("rowRegionWidth", new Array()).AsGodotArray();
		for (int i = 0; i < _frontlawnRow.Length; i++)
		{
			if (GodotObject.IsInstanceValid(_frontlawnRow[i]))
			{
				if (i < array.Count)
				{
					_frontlawnRow[i].Visible = array[i].AsBool();
				}
				if (i < array2.Count)
				{
					Rect2 regionRect = _frontlawnRow[i].RegionRect;
					_frontlawnRow[i].RegionRect = new Rect2(regionRect.Position, new Vector2(array2[i].AsSingle(), regionRect.Size.Y));
				}
			}
		}
		LoadLegacyRevealVisibility(data, "frontlawnFloorVisible", "%FrontlawnFloor");
		LoadLegacyRevealVisibility(data, "frontlawnDoorVisible", "%FrontlawnDoor");
		alive = false;
		createRowId = -1;
		followSodRoll = null;
	}

	public override bool CanSaveProgress(out string reason)
	{
		if (alive || GodotObject.IsInstanceValid(followSodRoll))
		{
			reason = "the lawn expansion animation is still running";
			return false;
		}
		return base.CanSaveProgress(out reason);
	}

	private void LoadLegacyRevealVisibility(Dictionary data, string stateKey, string nodePath)
	{
		if (data.ContainsKey(stateKey))
		{
			CanvasItem nodeOrNull = GetNodeOrNull<CanvasItem>(nodePath);
			if (GodotObject.IsInstanceValid(nodeOrNull))
			{
				nodeOrNull.Visible = data[stateKey].AsBool();
			}
		}
	}

	public void Row1Create()
	{
		AudioManager.Instance.AudioPlay("DirtRiseLong");
		TowerDefenseManager.Instance.SetMapLineUse(3, use: true);
		_frontlawnRow[0].Visible = true;
		TowerDefenseCellConfig towerDefenseCellConfig = new TowerDefenseCellConfig();
		towerDefenseCellConfig.gridType = new Array<TowerDefenseEnum.PLANTGRIDTYPE>
		{
			TowerDefenseEnum.PLANTGRIDTYPE.GROUND,
			TowerDefenseEnum.PLANTGRIDTYPE.AIR
		};
		towerDefenseCellConfig.pos = new Vector4I(1, 3, 9, 3);
		TowerDefenseManager.Instance.SetMapGridType(towerDefenseCellConfig);
		followSodRoll = CreateSodRoll(2);
		followSodRoll.OnAnimeCompleted += Finish;
		createRowId = 0;
		alive = true;
	}

	public void Row1Finish(string clip = "")
	{
		TowerDefenseManager.Instance.SetMapLineUse(3, use: true);
		_frontlawnRow[0].Visible = true;
		_frontlawnRow[1].Visible = false;
		_frontlawnRow[2].Visible = false;
		TowerDefenseCellConfig towerDefenseCellConfig = new TowerDefenseCellConfig();
		towerDefenseCellConfig.gridType = new Array<TowerDefenseEnum.PLANTGRIDTYPE>
		{
			TowerDefenseEnum.PLANTGRIDTYPE.GROUND,
			TowerDefenseEnum.PLANTGRIDTYPE.AIR
		};
		towerDefenseCellConfig.pos = new Vector4I(1, 3, 9, 3);
		TowerDefenseManager.Instance.SetMapGridType(towerDefenseCellConfig);
	}

	public void Row3CreateFromRow1()
	{
		AudioManager.Instance.AudioPlay("DirtRiseLong");
		TowerDefenseManager.Instance.SetMapLineUse(2, use: true);
		TowerDefenseManager.Instance.SetMapLineUse(4, use: true);
		TowerDefenseManager.Instance.CreateMower(2);
		TowerDefenseManager.Instance.CreateMower(4);
		followSodRoll = CreateSodRoll(1);
		followSodRoll.OnAnimeCompleted += delegate
		{
			_frontlawnRow[0].Visible = false;
			Finish();
		};
		CreateSodRoll(3);
		_frontlawnRow[0].Visible = true;
		_frontlawnRow[1].Visible = true;
		TowerDefenseCellConfig towerDefenseCellConfig = new TowerDefenseCellConfig();
		towerDefenseCellConfig.gridType = new Array<TowerDefenseEnum.PLANTGRIDTYPE>
		{
			TowerDefenseEnum.PLANTGRIDTYPE.GROUND,
			TowerDefenseEnum.PLANTGRIDTYPE.AIR
		};
		towerDefenseCellConfig.pos = new Vector4I(1, 2, 9, 2);
		TowerDefenseManager.Instance.SetMapGridType(towerDefenseCellConfig);
		towerDefenseCellConfig.pos = new Vector4I(1, 4, 9, 4);
		TowerDefenseManager.Instance.SetMapGridType(towerDefenseCellConfig);
		createRowId = 1;
		alive = true;
	}

	public void Row3Finish(string clip = "")
	{
		TowerDefenseManager.Instance.SetMapLineUse(2, use: true);
		TowerDefenseManager.Instance.SetMapLineUse(3, use: true);
		TowerDefenseManager.Instance.SetMapLineUse(4, use: true);
		_frontlawnRow[0].Visible = false;
		_frontlawnRow[1].Visible = true;
		_frontlawnRow[2].Visible = false;
		TowerDefenseCellConfig towerDefenseCellConfig = new TowerDefenseCellConfig();
		towerDefenseCellConfig.gridType = new Array<TowerDefenseEnum.PLANTGRIDTYPE>
		{
			TowerDefenseEnum.PLANTGRIDTYPE.GROUND,
			TowerDefenseEnum.PLANTGRIDTYPE.AIR
		};
		towerDefenseCellConfig.pos = new Vector4I(1, 2, 9, 4);
		TowerDefenseManager.Instance.SetMapGridType(towerDefenseCellConfig);
	}

	public void Row5CreateFromRow3()
	{
		AudioManager.Instance.AudioPlay("DirtRiseLong");
		TowerDefenseManager.Instance.SetMapLineUse(1, use: true);
		TowerDefenseManager.Instance.SetMapLineUse(5, use: true);
		TowerDefenseManager.Instance.CreateMower(1);
		TowerDefenseManager.Instance.CreateMower(5);
		followSodRoll = CreateSodRoll(0);
		followSodRoll.OnAnimeCompleted += delegate
		{
			_frontlawnRow[1].Visible = false;
			Finish();
		};
		CreateSodRoll(4);
		_frontlawnRow[1].Visible = true;
		_frontlawnRow[2].Visible = true;
		TowerDefenseCellConfig towerDefenseCellConfig = new TowerDefenseCellConfig();
		towerDefenseCellConfig.gridType = new Array<TowerDefenseEnum.PLANTGRIDTYPE>
		{
			TowerDefenseEnum.PLANTGRIDTYPE.GROUND,
			TowerDefenseEnum.PLANTGRIDTYPE.AIR
		};
		towerDefenseCellConfig.pos = new Vector4I(1, 1, 9, 1);
		TowerDefenseManager.Instance.SetMapGridType(towerDefenseCellConfig);
		towerDefenseCellConfig.pos = new Vector4I(1, 5, 9, 5);
		TowerDefenseManager.Instance.SetMapGridType(towerDefenseCellConfig);
		createRowId = 2;
		alive = true;
	}

	public void Row5Finish(string clip = "")
	{
		TowerDefenseManager.Instance.SetMapLineUse(1, use: true);
		TowerDefenseManager.Instance.SetMapLineUse(2, use: true);
		TowerDefenseManager.Instance.SetMapLineUse(3, use: true);
		TowerDefenseManager.Instance.SetMapLineUse(4, use: true);
		TowerDefenseManager.Instance.SetMapLineUse(5, use: true);
		_frontlawnRow[0].Visible = false;
		_frontlawnRow[1].Visible = false;
		_frontlawnRow[2].Visible = true;
		TowerDefenseCellConfig towerDefenseCellConfig = new TowerDefenseCellConfig();
		towerDefenseCellConfig.gridType = new Array<TowerDefenseEnum.PLANTGRIDTYPE>
		{
			TowerDefenseEnum.PLANTGRIDTYPE.GROUND,
			TowerDefenseEnum.PLANTGRIDTYPE.AIR
		};
		towerDefenseCellConfig.pos = new Vector4I(1, 1, 9, 1);
		TowerDefenseManager.Instance.SetMapGridType(towerDefenseCellConfig);
		towerDefenseCellConfig.pos = new Vector4I(1, 5, 9, 5);
		TowerDefenseManager.Instance.SetMapGridType(towerDefenseCellConfig);
	}

	public void Finish(string clip = "")
	{
		Rect2 regionRect = _frontlawnRow[createRowId].RegionRect;
		_frontlawnRow[createRowId].RegionRect = new Rect2(regionRect.Position, new Vector2((float)(_rowBeginForm[createRowId] + 790.0 + _rowEndForm[createRowId]), regionRect.Size.Y));
		alive = false;
		createRowId = -1;
		followSodRoll = null;
	}

	public void Delete(string clip = "", AdobeAnimateSprite instance = null)
	{
		if (GodotObject.IsInstanceValid(instance))
		{
			instance.QueueFree();
		}
	}

	public AdobeAnimateSprite CreateSodRoll(int line)
	{
		PackedScene packedScene = GD.Load<PackedScene>("res://Asset/Anime/Effect/SodRoll/SodRoll.tscn");
		AdobeAnimateSprite instance = packedScene.Instantiate<AdobeAnimateSprite>(PackedScene.GenEditState.Disabled);
		instance.Position = new Vector2(210f, 110 + 100 * line);
		instance.SetAnimation("Idle");
		instance.OnAnimeCompleted += delegate(string clip)
		{
			Delete(clip, instance);
		};
		AddChild(instance, forceReadableName: false, InternalMode.Disabled);
		return instance;
	}

	public void ShowRow1()
	{
		_frontlawnRow[0].Visible = true;
		Rect2 regionRect = _frontlawnRow[0].RegionRect;
		_frontlawnRow[0].RegionRect = new Rect2(regionRect.Position, new Vector2((float)(_rowBeginForm[0] + 790.0 + _rowEndForm[0]), regionRect.Size.Y));
		Row1Finish();
	}

	public void ShowRow3()
	{
		_frontlawnRow[1].Visible = true;
		Rect2 regionRect = _frontlawnRow[1].RegionRect;
		_frontlawnRow[1].RegionRect = new Rect2(regionRect.Position, new Vector2((float)(_rowBeginForm[1] + 790.0 + _rowEndForm[1]), regionRect.Size.Y));
		Row3Finish();
	}

	public void ShowRow5()
	{
		_frontlawnRow[2].Visible = true;
		Rect2 regionRect = _frontlawnRow[2].RegionRect;
		_frontlawnRow[2].RegionRect = new Rect2(regionRect.Position, new Vector2((float)(_rowBeginForm[2] + 790.0 + _rowEndForm[2]), regionRect.Size.Y));
		Row5Finish();
	}
}
