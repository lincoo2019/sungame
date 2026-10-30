using Godot;
using Godot.Collections;
using PVZHE.ModEditor.ModSystem;

[GlobalClass]
public partial class DragMenuSelectItemlevel : DragMenuSelectItem
{
	private static Texture2D _silverTrophy;

	private static Texture2D _goldTrophy;

	private static Texture2D _diamondTrophy;

	private static Texture2D _moreGameStar;

	private Sprite2D cupSprite;

	private Label survivalLabel;

	private static Texture2D SILVER_TROPHY => _silverTrophy ?? (_silverTrophy = GD.Load<Texture2D>("uid://wrip0sbul8kd"));

	private static Texture2D GOLD_TROPHY => _goldTrophy ?? (_goldTrophy = GD.Load<Texture2D>("uid://bilmjv3kylwye"));

	private static Texture2D DIAMOND_TROPHY => _diamondTrophy ?? (_diamondTrophy = GD.Load<Texture2D>("uid://bg1gnn1guh1sl"));

	private static Texture2D MORE_GAME_STAR => _moreGameStar ?? (_moreGameStar = GD.Load<Texture2D>("uid://cvth13h0ui3cq"));

	public override void _Ready()
	{
		base._Ready();
		cupSprite = GetNode<Sprite2D>("%CupSprite");
		survivalLabel = GetNode<Label>("%SurvivalLabel");
	}

	public void Init(string levelKey, XWModLevelIdentity identity = null)
	{
		cupSprite.Texture = null;
		survivalLabel.Visible = false;
		survivalLabel.Text = string.Empty;
		Dictionary dictionary = ((identity == null) ? GameSaveManager.Instance.GetLevelValue(levelKey) : XWModPlayerProgressService.GetLevel(identity));
		bool flag = dictionary.GetValueOrDefault("Difficult", false).AsBool() || dictionary.GetValueOrDefault("Ultimate", false).AsBool();
		bool flag2 = Global.Instance.currentLevelChoose != "Puzzle" && Global.Instance.currentLevelChoose != "MiniGames";
		if (flag2 && flag)
		{
			cupSprite.Texture = DIAMOND_TROPHY;
		}
		else if (flag2 && dictionary.GetValueOrDefault("Mower", false).AsBool())
		{
			cupSprite.Texture = GOLD_TROPHY;
		}
		else if (dictionary.GetValueOrDefault("Key", new Dictionary()).AsGodotDictionary().GetValueOrDefault("Finish", 0)
			.AsInt32() > 0)
		{
			cupSprite.Texture = (flag2 ? SILVER_TROPHY : MORE_GAME_STAR);
		}
		TowerDefenseLevelSaveConfigCSharp levelProgress = GameSaveManager.Instance.GetLevelProgress(levelKey, identity);
		if (levelProgress != null)
		{
			if (!TryGetSaveSection(levelProgress.featureSave, "Wave", out var section))
			{
				TryGetSaveSection(levelProgress.processSave, "main", out section);
			}
			if (section != null && section.GetValueOrDefault("isSurvival", false).AsBool())
			{
				survivalLabel.Visible = true;
				survivalLabel.Text = string.Format("{0}轮完成", section.GetValueOrDefault("survivalRoundNum", 0).AsInt32());
			}
		}
	}

	private static bool TryGetSaveSection(Dictionary container, string sectionName, out Dictionary section)
	{
		if (container != null)
		{
			foreach (Variant key in container.Keys)
			{
				if (!(key.AsString() != sectionName))
				{
					section = container[key].AsGodotDictionary();
					return section != null;
				}
			}
		}
		section = null;
		return false;
	}
}
