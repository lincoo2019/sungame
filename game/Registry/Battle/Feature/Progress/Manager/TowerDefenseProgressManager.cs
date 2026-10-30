using Godot;

[GlobalClass]
public partial class TowerDefenseProgressManager : Control
{
	public Label levelNameLabel;

	public Label difficultLabel;

	public Label survivalLabel;

	public GeneralProgressMeter progressMeter;

	public TextureProgressBar progressBar;

	public TowerDefenseBattleFeatureProgress progressFeature;

	public override void _Ready()
	{
		levelNameLabel = GetNode<Label>("%LevelNameLabel");
		difficultLabel = GetNode<Label>("%DifficultLabel");
		survivalLabel = GetNode<Label>("%SurvivalLabel");
		progressMeter = GetNode<GeneralProgressMeter>("%GeneralProgressMeter");
		progressBar = progressMeter.GetNode<TextureProgressBar>("%ProgressBar");
	}

	public void SetupUI(string difficult, string levelName, bool isSurvival = false, int survivalRoundNum = 0)
	{
		string newValue = "";
		switch (difficult)
		{
		case "Normal":
			newValue = "正常";
			difficultLabel.Modulate = new Color(difficultLabel.Modulate.R, 1f, difficultLabel.Modulate.B, difficultLabel.Modulate.A);
			break;
		case "Difficult":
			newValue = "困难";
			difficultLabel.Modulate = new Color(difficultLabel.Modulate.R, 0f, difficultLabel.Modulate.B, difficultLabel.Modulate.A);
			break;
		case "Ultimate":
			newValue = "极限";
			difficultLabel.Modulate = new Color(difficultLabel.Modulate.R, 0f, difficultLabel.Modulate.B, difficultLabel.Modulate.A);
			break;
		}
		levelNameLabel.Text = levelName;
		difficultLabel.Text = Tr("INGAME_DIFFICULT").Replace("%s", newValue);
		if (isSurvival)
		{
			survivalLabel.Text = Tr("PLAYERS_SURVIVAL_LEVEL_DESCRIBE").Replace("%d", survivalRoundNum.ToString());
		}
	}
}
