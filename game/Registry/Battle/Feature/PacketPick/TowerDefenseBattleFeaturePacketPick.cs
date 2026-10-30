using System.Threading.Tasks;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseBattleFeaturePacketPick : TowerDefenseBattleFeature
{
	public PacketPickControl packetPickControl;

	public TowerDefenseBattleFeaturePacketPickConfig config;

	private TowerDefenseBattleFeatureMap _mapFeature;

	public override void Init(Dictionary _data)
	{
		base.Init(_data);
		config = new TowerDefenseBattleFeaturePacketPickConfig();
		config.Init(data);
		packetPickControl = new PacketPickControl();
	}

	public override Task GameInit()
	{
		AttachPacketPickControl("GameInit");
		return Task.CompletedTask;
	}

	public override Task GameInitFromProgress()
	{
		AttachPacketPickControl("GameInitFromProgress");
		return Task.CompletedTask;
	}

	private void AttachPacketPickControl(string operation)
	{
		_mapFeature = GetFeature("Map") as TowerDefenseBattleFeatureMap;
		if (!GodotObject.IsInstanceValid(_mapFeature) || !GodotObject.IsInstanceValid(_mapFeature.mapControl) || !GodotObject.IsInstanceValid(packetPickControl))
		{
			GD.PushError("[PacketPick." + operation + "] Map, mapControl, or packetPickControl is invalid.");
			return;
		}
		_mapFeature.packetPickControl = packetPickControl;
		packetPickControl.Init(_mapFeature.mapControl, _mapFeature, config);
		if (packetPickControl.GetParent() == null)
		{
			_mapFeature.mapControl.AddChild(packetPickControl, forceReadableName: false, Node.InternalMode.Disabled);
		}
	}

	public override void Destroy()
	{
		if (GodotObject.IsInstanceValid(_mapFeature) && _mapFeature.packetPickControl == packetPickControl)
		{
			_mapFeature.packetPickControl = null;
		}
		if (GodotObject.IsInstanceValid(packetPickControl))
		{
			packetPickControl.DisposeBattleState();
			packetPickControl.QueueFree();
		}
		packetPickControl = null;
		config = null;
		_mapFeature = null;
		base.Destroy();
	}
}
