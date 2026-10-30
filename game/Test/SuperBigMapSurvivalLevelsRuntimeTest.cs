using Godot;
using Godot.Collections;

public partial class SuperBigMapSurvivalLevelsRuntimeTest : Node
{
	private const int ExpectedChecks = 16;

	private int _checks;

	private int _failures;

	public override void _Ready()
	{
		string[] array = new string[3] { "uid://c1ub442g55xea", "uid://xa8u0i3o3rk6", "uid://4eyin8cfok3f" };
		string[] array2 = new string[3] { "res://Asset/Config/Level/TowerDefense/Survival/Classic/Survival_Level20_1.tres", "res://Asset/Config/Level/TowerDefense/Survival/Classic/Survival_Level20_2.tres", "res://Asset/Config/Level/TowerDefense/Survival/Classic/Survival_Level20_3.tres" };
		string[] array3 = new string[3] { "超级大地图生存（基础）", "超级大地图生存（进阶）", "超级大地图生存（无尽）" };
		string[] array4 = new string[3] { "FrontlawnBaseNormal", "FrontlawnAdvancedNormal", "FrontlawnEndlessNormal" };
		string[] array5 = new string[3] { "uid://ubjw8jx6x7k0", "uid://sdnlpsre05ty", "uid://btx0q3ujvqijo" };
		string[] array6 = new string[3] { "res://Asset/Texture/GUI/TowerDefense/Level/Survival/Classic/Endless_LEVEL_A058.png", "res://Asset/Texture/GUI/TowerDefense/Level/Survival/Classic/Endless_LEVEL_A059.png", "res://Asset/Texture/GUI/TowerDefense/Level/Survival/Classic/Endless_LEVEL_A060.png" };
		for (int i = 0; i < array.Length; i++)
		{
			TowerDefenseLevelConfig towerDefenseLevelConfig = ResourceLoader.Load<TowerDefenseLevelConfig>(array2[i], null, ResourceLoader.CacheMode.IgnoreDeep);
			long id = ResourceUid.TextToId(array[i]);
			if (!ResourceUid.HasId(id))
			{
				ResourceUid.AddId(id, array2[i]);
			}
			TowerDefenseLevelConfig towerDefenseLevelConfig2 = ResourceLoader.Load<TowerDefenseLevelConfig>(array[i], null, ResourceLoader.CacheMode.IgnoreDeep);
			Check(GodotObject.IsInstanceValid(towerDefenseLevelConfig) && GodotObject.IsInstanceValid(towerDefenseLevelConfig2) && ResourceUid.HasId(id) && ResourceUid.GetIdPath(id) == array2[i], $"第 {i + 1} 档超级大地图生存关卡必须能够通过资源路径注册并由正式 UID 加载。");
			if (!GodotObject.IsInstanceValid(towerDefenseLevelConfig2))
			{
				towerDefenseLevelConfig?.Dispose();
				continue;
			}
			Check(towerDefenseLevelConfig2.name == $"Survival_Level20_{i + 1}" && towerDefenseLevelConfig2.levelName == array3[i] && towerDefenseLevelConfig2.levelNumber == i + 1, $"第 {i + 1} 档关卡身份、名称和等级必须一致。");
			Dictionary value;
			Dictionary dictionary = (towerDefenseLevelConfig2.featureData.TryGetValue(new StringName("Map"), out value) ? value : new Dictionary());
			Dictionary value2;
			Dictionary dictionary2 = (towerDefenseLevelConfig2.featureData.TryGetValue(new StringName("Wave"), out value2) ? value2 : new Dictionary());
			Check(towerDefenseLevelConfig2.map == "FrontlawnSuperBig" && dictionary.GetValueOrDefault("MapName", "").AsString() == "FrontlawnSuperBig", $"第 {i + 1} 档关卡顶层地图和 Map Feature 必须共同指向超级大地图。");
			bool flag = towerDefenseLevelConfig2.featureData.ContainsKey(new StringName("Map")) && towerDefenseLevelConfig2.featureData.ContainsKey(new StringName("Progress")) && towerDefenseLevelConfig2.featureData.ContainsKey(new StringName("Camera")) && towerDefenseLevelConfig2.featureData.ContainsKey(new StringName("Wave")) && towerDefenseLevelConfig2.featureData.ContainsKey(new StringName("Mower")) && towerDefenseLevelConfig2.featureData.ContainsKey(new StringName("Sun")) && towerDefenseLevelConfig2.featureData.ContainsKey(new StringName("SeedBank")) && towerDefenseLevelConfig2.featureData.ContainsKey(new StringName("PacketBank"));
			Check(towerDefenseLevelConfig2.processName == (StringName)"Wave" && towerDefenseLevelConfig2.processData.GetValueOrDefault("MowerUse", false).AsBool() && dictionary2.GetValueOrDefault("Survival", "").AsString() == array4[i] && flag, $"第 {i + 1} 档关卡必须由 Wave Process 编排，让 Wave Feature 使用 {array4[i]}，并配置全部必需及已使用的可选 Feature。");
			long id2 = ResourceUid.TextToId(array5[i]);
			Texture2D texture2D = ResourceLoader.Load<Texture2D>(array5[i], null, ResourceLoader.CacheMode.IgnoreDeep);
			Check(GodotObject.IsInstanceValid(texture2D) && ResourceUid.HasId(id2) && ResourceUid.GetIdPath(id2) == array6[i] && texture2D.GetSize() == new Vector2(411f, 342f), $"第 {i + 1} 档超级大地图封面必须能够通过独立 UID 加载，并保持 411x342 尺寸。");
			texture2D?.Dispose();
			towerDefenseLevelConfig?.Dispose();
			towerDefenseLevelConfig2.Dispose();
		}
		TowerDefenseMapConfig towerDefenseMapConfig = ResourceLoader.Load<TowerDefenseMapConfig>("uid://d3supermapday", null, ResourceLoader.CacheMode.IgnoreDeep);
		Check(GodotObject.IsInstanceValid(towerDefenseMapConfig) && towerDefenseMapConfig.gridNum == new Vector2I(27, 15) && towerDefenseMapConfig.maximumFps == 60, "三档生存关卡使用的超级大地图必须保持 27x15 和 60 FPS 上限。");
		towerDefenseMapConfig?.Dispose();
		bool flag2 = _failures == 0 && _checks == 16;
		GD.Print($"SUPER_BIG_MAP_SURVIVAL_LEVELS_RESULT passed={flag2} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag2) ? 2 : 0);
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[SuperBigMapSurvivalLevelsRuntimeTest] " + message);
		}
	}
}
