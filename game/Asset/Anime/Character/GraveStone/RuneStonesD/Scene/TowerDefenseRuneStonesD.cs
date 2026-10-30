using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseRuneStonesD : TowerDefenseGravestone
{
	private const string RUNE_STONESD_WATER_1 = "uid://ccon1sprvwel4";

	private const string RUNE_STONESD_WATER_2 = "uid://c4ry20743py3d";

	private const string RUNE_STONESD_WATER_3 = "uid://b0q0m465yqm8t";

	private const string RUNE_STONESD_WATER_4 = "uid://dvoiq2vlsrfkc";

	private const string RUNE_STONESD_WATER_5 = "uid://b0ohc1j25fydt";

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
				base.sprite.SetAtlasReplace("RuneStones4_1.png", "uid://ccon1sprvwel4");
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
				base.sprite.SetAtlasReplace("RuneStones4_1.png", "uid://ccon1sprvwel4");
				break;
			case "Damage1":
				base.sprite.SetAtlasReplace("RuneStones4_1.png", "uid://c4ry20743py3d");
				break;
			case "Damage2":
				base.sprite.SetAtlasReplace("RuneStones4_1.png", "uid://b0q0m465yqm8t");
				break;
			case "Damage3":
				base.sprite.SetAtlasReplace("RuneStones4_1.png", "uid://dvoiq2vlsrfkc");
				break;
			case "Damage4":
				base.sprite.SetAtlasReplace("RuneStones4_1.png", "uid://b0ohc1j25fydt");
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
