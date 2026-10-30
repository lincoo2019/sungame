using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewIzmUnexpectedStormRuntimeTest : Node
{
	private static readonly int[] AffectedLevels = new int[5] { 31, 32, 35, 36, 38 };

	private const string LevelDirectory = "res://Asset/Config/Level/TowerDefense/IZM";

	private const string ScreenEffectControlPath = "res://Registry/Battle/Feature/ScreenEffect/ScreenEffectControl/ScreenEffectControl.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		IzmUnexpectedStormRuntimeControlStub control = null;
		try
		{
			control = new IzmUnexpectedStormRuntimeControlStub
			{
				Name = "IzmUnexpectedStormRuntimeControl",
				isGameRunning = false,
				isInit = true
			};
			AddChild(control, forceReadableName: false, InternalMode.Disabled);
			Check(GodotObject.IsInstanceValid(control) && control.IsInsideTree(), "The focused scene must mount a production TowerDefenseControlNew host.");
			int[] affectedLevels = AffectedLevels;
			foreach (int levelNumber in affectedLevels)
			{
				await VerifyLevel(control, levelNumber);
			}
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[BugOverviewIzmUnexpectedStormRuntimeTest] Unexpected exception: {value}");
		}
		finally
		{
			if (GodotObject.IsInstanceValid(control) && !control.IsQueuedForDeletion())
			{
				control.QueueFree();
			}
			await WaitFrames(3);
		}
		bool flag = _failures == 0 && _checks == 41;
		GD.Print($"IZM_UNEXPECTED_STORM_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task VerifyLevel(IzmUnexpectedStormRuntimeControlStub control, int levelNumber)
	{
		string text = $"{"res://Asset/Config/Level/TowerDefense/IZM"}/IZMLevel{levelNumber}.tres";
		string jsonPath = $"{"res://Asset/Config/Level/TowerDefense/IZM"}/IZMLevel{levelNumber}.json";
		TowerDefenseLevelConfig towerDefenseLevelConfig = ResourceLoader.Load<TowerDefenseLevelConfig>(text, null, ResourceLoader.CacheMode.Ignore);
		Check(GodotObject.IsInstanceValid(towerDefenseLevelConfig) && towerDefenseLevelConfig.ResourcePath == text && towerDefenseLevelConfig.name == $"IZM_Level{levelNumber}", $"IZM {levelNumber} must load its real production level resource.");
		if (!GodotObject.IsInstanceValid(towerDefenseLevelConfig))
		{
			throw new InvalidOperationException($"IZM {levelNumber} resource did not load.");
		}
		Check(towerDefenseLevelConfig.processName == new StringName("IZM") && !towerDefenseLevelConfig.featureData.ContainsKey(new StringName("RainMode")), $"IZM {levelNumber} must retain the IZM process without a RainMode feature.");
		Dictionary value;
		bool flag = towerDefenseLevelConfig.featureData.TryGetValue(new StringName("ScreenEffect"), out value);
		Check(flag && value != null, $"IZM {levelNumber} must expose its authored ScreenEffect data.");
		if (!flag || value == null)
		{
			throw new InvalidOperationException($"IZM {levelNumber} has no ScreenEffect data.");
		}
		Check(!value.GetValueOrDefault("StormOpen", false).AsBool(), $"IZM {levelNumber} must not request the storm weather pair.");
		string a = value.GetValueOrDefault("PacketBankMethod", "NOONE").AsString();
		Check(!string.Equals(a, "RAIN", StringComparison.OrdinalIgnoreCase), $"IZM {levelNumber} must not request rain through PacketBankMethod.");
		Check(JsonScreenEffectIsClear(jsonPath), $"IZM {levelNumber} JSON source must agree with the runtime resource and keep StormOpen false.");
		TowerDefenseBattleFeatureScreenEffect feature = new TowerDefenseBattleFeatureScreenEffect
		{
			control = control
		};
		feature.Init(value.Duplicate(deep: true));
		await WaitFrames(1);
		Check(GodotObject.IsInstanceValid(feature.screenEffectControl) && feature.screenEffectControl.SceneFilePath == "res://Registry/Battle/Feature/ScreenEffect/ScreenEffectControl/ScreenEffectControl.tscn", $"IZM {levelNumber} must exercise the real ScreenEffect production container.");
		Check(!feature.HasScreenEffect("Rain") && !feature.HasScreenEffect("Storm") && feature.screenEffectControl.GetChildCount() == 0, $"IZM {levelNumber} must mount neither the real Rain nor Storm effect node.");
		feature.Destroy();
		await WaitFrames(2);
	}

	private static bool JsonScreenEffectIsClear(string jsonPath)
	{
		Variant variant = Json.ParseString(FileAccess.GetFileAsString(jsonPath));
		if (variant.VariantType != Variant.Type.Dictionary)
		{
			return false;
		}
		foreach (Variant item in variant.AsGodotDictionary().GetValueOrDefault("Feature", new Godot.Collections.Array()).AsGodotArray())
		{
			if (item.VariantType == Variant.Type.Dictionary)
			{
				Dictionary dictionary = item.AsGodotDictionary();
				if (string.Equals(dictionary.GetValueOrDefault("Name", "").AsString(), "ScreenEffect", StringComparison.Ordinal))
				{
					return !dictionary.GetValueOrDefault("Data", new Dictionary()).AsGodotDictionary().GetValueOrDefault("StormOpen", false)
						.AsBool();
				}
			}
		}
		return false;
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BugOverviewIzmUnexpectedStormRuntimeTest] " + message);
		}
	}
}
