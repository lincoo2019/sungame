using Godot;

[GlobalClass]
public partial class LevelDifficultSlider : HSlider
{
	public TowerDefenseLevelConfig levelConfig;

	public override void _Ready()
	{
		base.DragEnded += DragEnd;
	}

	public void Init(TowerDefenseLevelConfig _levelConfig)
	{
		levelConfig = _levelConfig;
		if (levelConfig != null)
		{
			TowerDefenseLevelWaveManagerConfig waveManager = levelConfig.waveManager;
			base.MaxValue = waveManager.dynamic.Count - 1;
			base.TickCount = 3;
			base.Value = TowerDefenseManager.Instance.currentDynamicLevel;
		}
	}

	public void DragEnd(bool isChanged)
	{
		if (isChanged)
		{
			base.Value = Mathf.Round((float)base.Value);
		}
	}
}
