using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseRuneStonesDLow : TowerDefenseGravestone
{
	private const string RUNE_STONESD_WATER_1 = "uid://c8inmwponajg";

	private const string RUNE_STONESD_WATER_2 = "uid://cfifstkfddtf5";

	private const string RUNE_STONESD_WATER_3 = "uid://boxryb6nnub52";

	private const string RUNE_STONESD_WATER_4 = "uid://pnm35lep2vu4";

	private const string RUNE_STONESD_WATER_5 = "uid://gnksay5ffiy3";

	private static PackedScene _CHERRY_BOMB_EXPLOSION;

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> eventList = new Array<TowerDefenseCharacterEventBase>();

	public bool boom;

	private static PackedScene CHERRY_BOMB_EXPLOSION => _CHERRY_BOMB_EXPLOSION ?? (_CHERRY_BOMB_EXPLOSION = GD.Load<PackedScene>("uid://cibtjjjomdxnh"));

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			RemoveFromGroup("Gravestone");
			if (GodotObject.IsInstanceValid(cell) && cell.isWater)
			{
				shadowSprite.Visible = false;
				base.sprite.SetAtlasReplace("RuneStones2_1.png", "uid://c8inmwponajg");
			}
		}
	}

	public override void DamagePointReach(string damagePointName)
	{
		base.DamagePointReach(damagePointName);
		if (GodotObject.IsInstanceValid(cell) && cell.isWater)
		{
			switch (damagePointName)
			{
			case "Damage0":
				base.sprite.SetAtlasReplace("RuneStones2_1.png", "uid://c8inmwponajg");
				break;
			case "Damage1":
				base.sprite.SetAtlasReplace("RuneStones2_1.png", "uid://cfifstkfddtf5");
				break;
			case "Damage2":
				base.sprite.SetAtlasReplace("RuneStones2_1.png", "uid://boxryb6nnub52");
				break;
			case "Damage3":
				base.sprite.SetAtlasReplace("RuneStones2_1.png", "uid://pnm35lep2vu4");
				break;
			case "Damage4":
				base.sprite.SetAtlasReplace("RuneStones2_1.png", "uid://gnksay5ffiy3");
				break;
			}
		}
	}

	public override async void DestroySet()
	{
		if (!boom)
		{
			boom = true;
			ViewManager.Instance.CameraShake(new Vector2(GD.RandRange(-1, 1), GD.RandRange(-1, 1)), 5.0, 0.05, 4);
			Vector2 explosionPosition = GetLogicalGlobalPosition();
			TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(CHERRY_BOMB_EXPLOSION, base.gridPos);
			towerDefenseEffectParticlesOnce.GlobalPosition = explosionPosition;
			TowerDefenseManager.GetCharacterNode().AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, InternalMode.Disabled);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			TowerDefenseExplode.CreateExplode(explosionPosition, new Vector2(1.5f, 1.5f), eventList, new Array<TowerDefenseCharacter>(), TowerDefenseEnum.CHARACTER_CAMP.NOONE, -1);
			AudioManager.Instance.AudioPlay("ExplodeCherrybomb");
			if (!Global.IsMultiplayerMode || MultiPlayerManager.IsHost)
			{
				CraterCreate(nolimit: true);
			}
		}
	}
}
