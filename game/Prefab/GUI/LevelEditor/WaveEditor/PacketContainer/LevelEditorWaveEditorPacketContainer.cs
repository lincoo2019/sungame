using Godot;
using Godot.Collections;

[GlobalClass]
public partial class LevelEditorWaveEditorPacketContainer : HBoxContainer
{
	public delegate void SelectedEventHandler(LevelEditorWaveEditorPacketContainer packetContainer);

	public MainButton mainButton;

	private ScrollContainer _scrollContainer;

	private HBoxContainer _packetSlotContainer;

	private HBoxContainer _packetContainer;

	public int packetNum;

	public Array<TowerDefenseInGamePacketShow> packetList = new Array<TowerDefenseInGamePacketShow>();

	public event SelectedEventHandler OnSelected;

	public override void _Ready()
	{
		mainButton = GetNode<MainButton>("%MainButton");
		_scrollContainer = GetNode<ScrollContainer>("%ScrollContainer");
		_packetSlotContainer = GetNode<HBoxContainer>("%PacketSlotContainer");
		_packetContainer = GetNode<HBoxContainer>("%PacketContainer");
		mainButton.Pressed += HandleSelected;
	}

	public void Clear()
	{
		foreach (TowerDefenseInGamePacketShow packet in packetList)
		{
			packet.QueueFree();
		}
		packetList.Clear();
		packetNum = 0;
	}

	public bool CanAddPacket()
	{
		return packetNum < 50;
	}

	public Vector2 GetPacketPos(int id)
	{
		if (!TryGetPacketPos(id, out var position))
		{
			return base.GlobalPosition;
		}
		return position;
	}

	public bool TryGetPacketPos(int id, out Vector2 position)
	{
		position = base.GlobalPosition;
		if (!GodotObject.IsInstanceValid(_packetSlotContainer) || id < 0 || id >= _packetSlotContainer.GetChildCount())
		{
			return false;
		}
		Control childOrNull = _packetSlotContainer.GetChildOrNull<Control>(id);
		if (childOrNull == null)
		{
			return false;
		}
		position = childOrNull.GlobalPosition;
		return true;
	}

	public TowerDefenseInGamePacketShow AddPacket(TowerDefensePacketConfig _packetConfig)
	{
		packetNum++;
		TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = TowerDefenseManager.CreatePacketShow();
		towerDefenseInGamePacketShow.setPcLayout = true;
		towerDefenseInGamePacketShow.enforceRuntimeAvailabilityOnPress = false;
		_packetContainer.AddChild(towerDefenseInGamePacketShow, forceReadableName: false, InternalMode.Disabled);
		towerDefenseInGamePacketShow.Init(_packetConfig);
		towerDefenseInGamePacketShow.onlyDraw = false;
		towerDefenseInGamePacketShow.OnPressed += DeletePacket;
		packetList.Add(towerDefenseInGamePacketShow);
		return towerDefenseInGamePacketShow;
	}

	public void DeletePacket(TowerDefenseInGamePacketShow _packet)
	{
		LevelEditorWaveEditor.Instance.levelConfig.canExport = false;
		int index = packetList.IndexOf(_packet);
		packetList.RemoveAt(index);
		_packet.QueueFree();
		packetNum--;
	}

	public void HandleSelected()
	{
		this.OnSelected?.Invoke(this);
	}
}
