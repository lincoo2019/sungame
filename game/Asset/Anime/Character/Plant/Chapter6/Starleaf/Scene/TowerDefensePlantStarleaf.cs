using Godot;

[Tool]
public partial class TowerDefensePlantStarleaf : TowerDefensePlant
{
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

	public async void BlockCharacter()
	{
		if (!_isBlocking)
		{
			_isBlocking = true;
			base.itemLayer = TowerDefenseEnum.LAYER_GROUNDITEM.EFFECT;
			base.sprite.SetAnimation("Block", loop: false, 0.1);
			base.sprite.AddAnimation("Idle", 0.0);
		}
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return;
		}
		TowerDefenseCharacter characterTargetNearest = TowerDefenseManager.Instance.GetCharacterTargetNearest(this, TowerDefenseEnum.TARGET_NEAR_METHOD.POSITION);
		if (characterTargetNearest != null)
		{
			TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(characterTargetNearest.gridPos);
			double num = 0.0;
			if (GodotObject.IsInstanceValid(mapCell))
			{
				num = 0.0 - mapCell.GetGroundHeight() + 30.0;
			}
			TowerDefenseProjectileCreateData towerDefenseProjectileCreateData = new TowerDefenseProjectileCreateData("MeteorStar");
			towerDefenseProjectileCreateData.baseDamage = 100.0;
			towerDefenseProjectileCreateData.damageFlags = 2;
			if ((double)GD.Randf() < 0.2)
			{
				towerDefenseProjectileCreateData.projectileName = "MeteorStarS";
				towerDefenseProjectileCreateData.baseDamage = 200.0;
				towerDefenseProjectileCreateData.damageFlags = 2;
				Health(100.0);
			}
			BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
			{
				useFall = true,
				gridYOverride = characterTargetNearest.gridPos.Y,
				zOverride = 600.0,
				ySpeedOverride = 800.0
			};
			FireComponent.CreateProjectilePositionByData(null, null, (float)num, characterTargetNearest.GetLogicalGlobalPosition() - new Vector2(150f, 0f), new Vector2(GD.RandRange(50, 150), 0f), towerDefenseProjectileCreateData, -1, base.camp, default(Vector2), overrides);
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
}
