using Godot;

[Tool]
public partial class TowerDefensePlantNutleaf : TowerDefensePlant
{
	private const string NUTLEAF_BODY_2 = "uid://dtdgl22bp0385";

	private const string NUTLEAF_BODY = "uid://d1rii54n6ebjt";

	private const string NUTLEAF_LEAF_1 = "uid://du74itvg0v6tv";

	private const string NUTLEAF_LEAF_1_1 = "uid://bvgoi2t06wkjk";

	private const string NUTLEAF_LEAF_1_2 = "uid://0vubdg55l51c";

	private const string NUTLEAF_LEAF_2 = "uid://befoku44k4xgc";

	private const string NUTLEAF_LEAF_2_1 = "uid://cge2vjl6eavp8";

	private const string NUTLEAF_LEAF_2_2 = "uid://r8qcq2gy4hcr";

	private const string NUTLEAF_LEAF_7 = "uid://bh5hfx1p28j8d";

	private const string NUTLEAF_LEAF_7_1 = "uid://61bj8cbbee7r";

	private const string NUTLEAF_LEAF_7_2 = "uid://470542g6yvnh";

	private const string NUTLEAF_SKIN_1_1 = "uid://3bvuwmhgxhhs";

	private const string NUTLEAF_SKIN_1_2 = "uid://c05sqej8dxhvo";

	private const string NUTLEAF_SKIN_1_3 = "uid://c2o3ut38gtgc3";

	private const string NUTLEAF_SKIN_2_1 = "uid://dloldmb0o56i0";

	private const string NUTLEAF_SKIN_2_2 = "uid://f2hitkqe12ar";

	private const string NUTLEAF_SKIN_2_3 = "uid://bwlwi5in4jse3";

	private const string NUTLEAF_SKIN_7_1 = "uid://bpoowutc8v4dt";

	private const string NUTLEAF_SKIN_7_2 = "uid://qkspyxn7b1sv";

	private const string NUTLEAF_SKIN_7_3 = "uid://c1u4rrfof5oa7";

	private BlockComponent _blockComponent;

	private bool _isBlocking;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_blockComponent = componentManager.GetRuntime<BlockComponent>();
			if (_blockComponent != null)
			{
				_blockComponent.OnBlock += BlockCharacter;
				_blockComponent.SetCheckRectangleSize(TowerDefenseManager.Instance.GetMapGridSize() * 4f);
			}
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		if (_blockComponent != null)
		{
			_blockComponent.OnBlock -= BlockCharacter;
		}
		_blockComponent = null;
	}

	public virtual void BlockCharacter()
	{
		if (!_isBlocking)
		{
			_isBlocking = true;
			base.itemLayer = TowerDefenseEnum.LAYER_GROUNDITEM.EFFECT;
			base.sprite.SetAnimation("Block", loop: false, 0.1);
			base.sprite.AddAnimation("Idle", 0.0);
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (clip == "Block")
		{
			_isBlocking = false;
			base.itemLayer = TowerDefenseEnum.LAYER_GROUNDITEM.PLANT;
		}
	}

	public override void DamagePointReach(string damangePointName)
	{
		base.DamagePointReach(damangePointName);
		switch (damangePointName)
		{
		case "Damage0":
			base.sprite.SetAtlasReplace("Nutleaf_body.png", "uid://d1rii54n6ebjt");
			base.sprite.SetAtlasReplace("Nutleaf_leaf1.png", "uid://du74itvg0v6tv");
			base.sprite.SetAtlasReplace("Nutleaf_leaf2.png", "uid://befoku44k4xgc");
			base.sprite.SetAtlasReplace("Nutleaf_leaf7.png", "uid://bh5hfx1p28j8d");
			if (base.currentCustom.Contains("Custom0"))
			{
				base.sprite.SetAtlasReplace("Nutleaf_skin1_1.png", "uid://3bvuwmhgxhhs");
				base.sprite.SetAtlasReplace("Nutleaf_skin2_1.png", "uid://dloldmb0o56i0");
				base.sprite.SetAtlasReplace("Nutleaf_skin7_1.png", "uid://bpoowutc8v4dt");
			}
			break;
		case "Damage1":
			base.sprite.SetAtlasReplace("Nutleaf_leaf1.png", "uid://bvgoi2t06wkjk");
			base.sprite.SetAtlasReplace("Nutleaf_leaf2.png", "uid://cge2vjl6eavp8");
			base.sprite.SetAtlasReplace("Nutleaf_leaf7.png", "uid://61bj8cbbee7r");
			if (base.currentCustom.Contains("Custom0"))
			{
				base.sprite.SetAtlasReplace("Nutleaf_skin1_1.png", "uid://c05sqej8dxhvo");
				base.sprite.SetAtlasReplace("Nutleaf_skin2_1.png", "uid://f2hitkqe12ar");
				base.sprite.SetAtlasReplace("Nutleaf_skin7_1.png", "uid://qkspyxn7b1sv");
			}
			break;
		case "Damage2":
			base.sprite.SetAtlasReplace("Nutleaf_body.png", "uid://dtdgl22bp0385");
			base.sprite.SetAtlasReplace("Nutleaf_leaf1.png", "uid://0vubdg55l51c");
			base.sprite.SetAtlasReplace("Nutleaf_leaf2.png", "uid://r8qcq2gy4hcr");
			base.sprite.SetAtlasReplace("Nutleaf_leaf7.png", "uid://470542g6yvnh");
			if (base.currentCustom.Contains("Custom0"))
			{
				base.sprite.SetAtlasReplace("Nutleaf_skin1_1.png", "uid://c2o3ut38gtgc3");
				base.sprite.SetAtlasReplace("Nutleaf_skin2_1.png", "uid://bwlwi5in4jse3");
				base.sprite.SetAtlasReplace("Nutleaf_skin7_1.png", "uid://c1u4rrfof5oa7");
			}
			break;
		}
	}
}
