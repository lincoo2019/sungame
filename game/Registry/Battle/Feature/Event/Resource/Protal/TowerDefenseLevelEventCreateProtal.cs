using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseLevelEventCreateProtal : TowerDefenseLevelEventBase
{
	[Export(PropertyHint.None, "")]
	public string protalShape = "Circle";

	[Export(PropertyHint.None, "")]
	public Vector4I posRange = new Vector4I(3, 1, 9, 5);

	[Export(PropertyHint.None, "")]
	public double changeTime;

	public override string _GetName()
	{
		return "LEVLE_EVENT_CREATE_PROTAL";
	}

	public override void Execute()
	{
		if (Global.Instance.isMultiplayerMode && !MultiPlayerManager.Instance.isHost)
		{
			return;
		}
		TowerDefenseBattleFeaturePortal portalFeature = GetPortalFeature();
		if (portalFeature == null)
		{
			TowerDefenseControlNew currentControl = TowerDefenseManager.Instance.currentControl;
			if (GodotObject.IsInstanceValid(currentControl))
			{
				currentControl.AddFeature("Portal", new Dictionary());
				portalFeature = GetPortalFeature();
			}
		}
		if (GodotObject.IsInstanceValid(portalFeature))
		{
			portalFeature.PortalCreate(protalShape, posRange, changeTime);
		}
	}

	public override void Init(Dictionary valueDictionary)
	{
		protalShape = valueDictionary.GetValueOrDefault("Shape", "Circle").AsString();
		Dictionary dictionary = (valueDictionary.ContainsKey("PosRange") ? valueDictionary["PosRange"].AsGodotDictionary() : new Dictionary());
		posRange = new Vector4I(dictionary.GetValueOrDefault("x", -1).AsInt32(), dictionary.GetValueOrDefault("y", -1).AsInt32(), dictionary.GetValueOrDefault("z", -1).AsInt32(), dictionary.GetValueOrDefault("w", -1).AsInt32());
		changeTime = valueDictionary.GetValueOrDefault("ChangeTime", 0.0).AsDouble();
	}

	public override Dictionary Export()
	{
		return new Dictionary
		{
			["EventName"] = "CreateProtal",
			["Value"] = new Dictionary
			{
				["Shape"] = protalShape,
				["PosRange"] = new Dictionary
				{
					["x"] = posRange.X,
					["y"] = posRange.Y,
					["z"] = posRange.Z,
					["w"] = posRange.W
				},
				["ChangeTime"] = changeTime
			}
		};
	}

	public override Dictionary GetProperty()
	{
		Dictionary property = base.GetProperty();
		property["改变地图"] = new Dictionary
		{
			["地图"] = new Dictionary
			{
				["Object"] = this,
				["Type"] = "Enum",
				["Property"] = "protalShape",
				["Hint"] = new Dictionary
				{
					["Circle"] = "Circle",
					["Square"] = "Square",
					["Rhombus"] = "Rhombus"
				},
				["Rest"] = "Circle"
			},
			["范围"] = new Dictionary
			{
				["Object"] = this,
				["Type"] = "Vector4i",
				["Property"] = "posRange",
				["Rest"] = new Vector4I(3, 1, 9, 5)
			},
			["位置改变时间"] = new Dictionary
			{
				["Object"] = this,
				["Type"] = "Float",
				["Property"] = "changeTime",
				["Rest"] = 0.0
			}
		};
		return property;
	}

	public TowerDefenseBattleFeaturePortal GetPortalFeature()
	{
		TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
		if (GodotObject.IsInstanceValid(currentControl))
		{
			return currentControl.GetFeature("Portal") as TowerDefenseBattleFeaturePortal;
		}
		return null;
	}
}
