using Godot;
using Godot.Collections;

[GlobalClass]
public partial class UnlockConditionLevelSurvivalRoundConfig : UnlockConditionBaseConfig
{
	[Export(PropertyHint.None, "")]
	public string levelSaveKey = "";

	[Export(PropertyHint.None, "")]
	public int roundNum = 10;

	public override bool Check()
	{
		if (GameSaveManager.Instance == null || !GameSaveManager.Instance.HasLevelProgress(levelSaveKey))
		{
			return false;
		}
		return CheckProgress(GameSaveManager.Instance.GetLevelProgress(levelSaveKey));
	}

	public bool CheckProgress(TowerDefenseLevelSaveConfigCSharp progress)
	{
		if (progress == null)
		{
			return false;
		}
		if (TryGetSaveSection(progress.featureSave, "Wave", out var section) && HasRequiredRound(section))
		{
			return true;
		}
		if (TryGetSaveSection(progress.processSave, "main", out var section2))
		{
			return HasRequiredRound(section2);
		}
		return false;
	}

	private bool HasRequiredRound(Dictionary data)
	{
		if (data.GetValueOrDefault("isSurvival", false).AsBool())
		{
			return data.GetValueOrDefault("survivalRoundNum", 0).AsInt32() >= roundNum;
		}
		return false;
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
