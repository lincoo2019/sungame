using System.Threading.Tasks;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseBattleFeatureCamera : TowerDefenseBattleFeature
{
	private static PackedScene _towerDefenseCameraControlScene;

	public TowerDefenseCameraControl cameraControl;

	private static PackedScene TowerDefenseCameraControlScene => _towerDefenseCameraControlScene ?? (_towerDefenseCameraControlScene = GD.Load<PackedScene>("uid://7i6bh10be5o0"));

	public override void Init(Dictionary data)
	{
		base.Init(data);
		if (!GodotObject.IsInstanceValid(TowerDefenseCameraControlScene) || !GodotObject.IsInstanceValid(control))
		{
			GD.PushError("[Camera] Cannot create Camera control without its scene and battle control.");
			return;
		}
		cameraControl = TowerDefenseCameraControlScene.Instantiate<TowerDefenseCameraControl>(PackedScene.GenEditState.Disabled);
		control.AddNode(cameraControl);
	}

	public override Task GameInit()
	{
		TowerDefenseBattleFeatureMap feature = GetFeature<TowerDefenseBattleFeatureMap>("Map");
		if (GodotObject.IsInstanceValid(cameraControl) && GodotObject.IsInstanceValid(feature) && GodotObject.IsInstanceValid(feature.mapConfig))
		{
			cameraControl.ApplyMapConfig(feature.mapConfig);
		}
		return Task.CompletedTask;
	}

	public override Task GameInitFromProgress()
	{
		return GameInit();
	}

	public override void Destroy()
	{
		if (GodotObject.IsInstanceValid(cameraControl))
		{
			cameraControl.QueueFree();
		}
		cameraControl = null;
		base.Destroy();
	}
}
