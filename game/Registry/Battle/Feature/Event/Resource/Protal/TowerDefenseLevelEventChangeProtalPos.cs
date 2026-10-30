using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseLevelEventChangeProtalPos : TowerDefenseLevelEventBase
{
	public override string _GetName()
	{
		return "LEVLE_EVENT_CHAGE_PROTAL_POS";
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
			portalFeature.PortalChangePos();
		}
	}

	public override Dictionary Export()
	{
		return new Dictionary
		{
			["EventName"] = "ChangeProtalPos",
			["Value"] = new Dictionary()
		};
	}

	public TowerDefenseBattleFeaturePortal GetPortalFeature()
	{
		TowerDefenseControlNew currentControl = TowerDefenseManager.Instance.currentControl;
		if (GodotObject.IsInstanceValid(currentControl))
		{
			return currentControl.GetFeature("Portal") as TowerDefenseBattleFeaturePortal;
		}
		return null;
	}
}
