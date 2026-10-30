using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantWallnutShooter : TowerDefensePlant
{
	private FireComponent _fireComponent;

	private const string WALLNUT_SHOOTER = "uid://dtaxmmgke1nkh";

	private const string WALLNUT_SHOOTERROOM = "uid://cn4ms671wsyqg";

	private const string WALLNUT_SHOOTERROOM_DAMAGE1 = "uid://cwgg1u47m01gs";

	private const string WALLNUT_SHOOTERROOM_DAMAGE2 = "uid://bu75ftg0rf725";

	private const string WALLNUT_SHOOTER_DAMAGE1 = "uid://cpj46i6gydv5s";

	private const string WALLNUT_SHOOTER_DAMAGE2 = "uid://dd7025whr48re";

	private bool _isroom;

	public string currentDamagePoint = "Damage0";

	public bool isroom
	{
		get
		{
			return _isroom;
		}
		set
		{
			SetIsroom(value);
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !editorPreviewMode)
		{
			_fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			_fireComponent.OnRestore += Restore;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		FireComponent fireComponent = _fireComponent;
		if (fireComponent != null && !fireComponent.IsReleased)
		{
			_fireComponent.OnRestore -= Restore;
		}
	}

	public void SetIsroom(bool isroom)
	{
		_isroom = isroom;
		if (_isroom)
		{
			base.sprite.SetFliters(new Array { "anim_face2" }, open: true);
			base.sprite.SetFliters(new Array { "anim_face" }, open: false);
		}
		else
		{
			base.sprite.SetFliters(new Array { "anim_face2" }, open: false);
			base.sprite.SetFliters(new Array { "anim_face" }, open: true);
		}
	}

	public override void DamagePointReach(string damagePointName)
	{
		base.DamagePointReach(damagePointName);
		currentDamagePoint = damagePointName;
		switch (damagePointName)
		{
		case "Damage0":
			base.sprite.SetAtlasReplace("WallnutShooter.png", "uid://dtaxmmgke1nkh");
			base.sprite.SetAtlasReplace("WallnutShooter_boom.png", "uid://cn4ms671wsyqg");
			break;
		case "Damage1":
			base.sprite.SetAtlasReplace("WallnutShooter.png", "uid://cpj46i6gydv5s");
			base.sprite.SetAtlasReplace("WallnutShooter_boom.png", "uid://cwgg1u47m01gs");
			break;
		case "Damage2":
			base.sprite.SetAtlasReplace("WallnutShooter.png", "uid://dd7025whr48re");
			base.sprite.SetAtlasReplace("WallnutShooter_boom.png", "uid://bu75ftg0rf725");
			break;
		}
	}

	public void Restore()
	{
		isroom = (double)GD.Randf() < 0.05;
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (clip == "Fire")
		{
			CreateBowling();
		}
	}

	public void CreateBowling()
	{
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return;
		}
		string packetName = (isroom ? "PlantBowlingWallnutShooterBoom" : "PlantBowlingWallnutShooterNormal");
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		TowerDefenseCharacter towerDefenseCharacter = TowerDefenseCharacter.CreateCharacter(packetName, logicalGlobalPosition, base.gridPos, base.groundHeight);
		if (instance.hypnoses)
		{
			towerDefenseCharacter.Hypnoses();
		}
		if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
		{
			TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
			if (GodotObject.IsInstanceValid(currentControl))
			{
				int nextSyncId = currentControl.GetNextSyncId();
				currentControl.RegisterSyncCharacter(nextSyncId, towerDefenseCharacter);
				MultiPlayerManager.Instance.SendSpawnCharacterAt(packetName, base.gridPos.X, base.gridPos.Y, nextSyncId, 1.0, 1.0, instance.hypnoses, 0.0, useCreate: true, logicalGlobalPosition.X, logicalGlobalPosition.Y, walkAfterSpawn: false, base.groundHeight);
			}
		}
		switch (currentDamagePoint)
		{
		case "Damage0":
			towerDefenseCharacter.sprite.SetAtlasReplace("WallnutShooter.png", "uid://dtaxmmgke1nkh");
			towerDefenseCharacter.sprite.SetAtlasReplace("WallnutShooter_boom.png", "uid://cn4ms671wsyqg");
			break;
		case "Damage1":
			towerDefenseCharacter.sprite.SetAtlasReplace("WallnutShooter.png", "uid://cpj46i6gydv5s");
			towerDefenseCharacter.sprite.SetAtlasReplace("WallnutShooter_boom.png", "uid://cwgg1u47m01gs");
			break;
		case "Damage2":
			towerDefenseCharacter.sprite.SetAtlasReplace("WallnutShooter.png", "uid://dd7025whr48re");
			break;
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "isBoom", isroom },
			{ "currentDamagePoint", currentDamagePoint }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		isroom = data.GetValueOrDefault("isBoom", Variant.From<bool>(false)).AsBool();
		currentDamagePoint = data.GetValueOrDefault("currentDamagePoint", Variant.From<string>("Damage0")).AsString();
	}
}
