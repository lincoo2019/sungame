using System.Threading.Tasks;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseBattleFeatureShovel : TowerDefenseBattleFeature
{
	private static PackedScene _shovelManager;

	public ShovelManager shovelManager;

	public ShovelPickTool shovelPickTool;

	private TowerDefenseBattleFeatureMap _mapFeature;

	private static PackedScene SHOVEL_MANAGER => _shovelManager ?? (_shovelManager = GD.Load<PackedScene>("res://Registry/Battle/Feature/Shovel/ShovelManager/ShovelManager.tscn"));

	public override void Init(Dictionary _data)
	{
		base.Init(_data);
		shovelManager = SHOVEL_MANAGER.Instantiate<ShovelManager>(PackedScene.GenEditState.Disabled);
		control.AddUIToTopPropContainer(shovelManager);
	}

	public override Task GameInit()
	{
		BindMapFeature();
		return Task.CompletedTask;
	}

	public override Task GameInitFromProgress()
	{
		BindMapFeature();
		return Task.CompletedTask;
	}

	public override Task GameEntry()
	{
		RegisterShovelTool();
		return Task.CompletedTask;
	}

	public override Task GameStart()
	{
		if (GodotObject.IsInstanceValid(shovelManager))
		{
			shovelManager.ShovelReset();
		}
		if (GodotObject.IsInstanceValid(shovelPickTool))
		{
			return Task.CompletedTask;
		}
		RegisterShovelTool();
		return Task.CompletedTask;
	}

	private void BindMapFeature()
	{
		_mapFeature = GetFeature("Map") as TowerDefenseBattleFeatureMap;
		if (GodotObject.IsInstanceValid(_mapFeature) && GodotObject.IsInstanceValid(_mapFeature.mapControl) && GodotObject.IsInstanceValid(shovelManager))
		{
			_mapFeature.shovelManager = shovelManager;
			shovelManager.Init(_mapFeature.mapControl, _mapFeature);
		}
	}

	private void RegisterShovelTool()
	{
		if (_mapFeature == null)
		{
			_mapFeature = GetFeature("Map") as TowerDefenseBattleFeatureMap;
		}
		if (!GodotObject.IsInstanceValid(shovelPickTool) && GodotObject.IsInstanceValid(_mapFeature) && GodotObject.IsInstanceValid(_mapFeature.mapControl) && GodotObject.IsInstanceValid(_mapFeature.packetPickControl))
		{
			shovelPickTool = new ShovelPickTool();
			shovelPickTool.Init(_mapFeature.mapControl);
			shovelPickTool.SetShovelManager(shovelManager);
			_mapFeature.packetPickControl.RegisterTool(shovelPickTool);
		}
	}

	public override void Destroy()
	{
		if (GodotObject.IsInstanceValid(_mapFeature) && GodotObject.IsInstanceValid(_mapFeature.packetPickControl) && GodotObject.IsInstanceValid(shovelPickTool))
		{
			_mapFeature.packetPickControl.UnregisterTool(shovelPickTool);
		}
		if (GodotObject.IsInstanceValid(shovelPickTool))
		{
			shovelPickTool.SetShovelManager(null);
			shovelPickTool.Free();
		}
		shovelPickTool = null;
		if (GodotObject.IsInstanceValid(_mapFeature) && _mapFeature.shovelManager == shovelManager)
		{
			_mapFeature.shovelManager = null;
		}
		if (GodotObject.IsInstanceValid(shovelManager))
		{
			shovelManager.QueueFree();
		}
		shovelManager = null;
		_mapFeature = null;
		base.Destroy();
	}
}
