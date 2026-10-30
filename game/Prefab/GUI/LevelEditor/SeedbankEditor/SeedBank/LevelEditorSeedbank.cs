using Godot;
using Godot.Collections;

[GlobalClass]
public partial class LevelEditorSeedbank : Control
{
	public static Texture2D SEED_BANK_ZOMBIE;

	public static Texture2D SEED_BANK;

	public static Texture2D CONVEYOR_BELT_SUN;

	public static Texture2D CONVEYOR_BELT_SUN_BACKDROP;

	public static Texture2D CONVEYOR_BELT;

	public static Texture2D CONVEYOR_BELT_BACKDROP;

	private Control _posMask;

	public Label sunLabel;

	public MarginContainer seedContanin;

	public Control conveyor;

	private HBoxContainer _packetSlotContainer;

	public HBoxContainer packetContainer;

	public NinePatchRect seedBankTexture;

	public NinePatchRect conveyorBeltRectPC;

	public TextureRect belt;

	public TextureRect conveyorSunBankTexture;

	public Label conveyorSunLabel;

	public static LevelEditorSeedbank Instance;

	public int packetNum;

	public Array<TowerDefenseInGamePacketShow> packetList = new Array<TowerDefenseInGamePacketShow>();

	public override void _Ready()
	{
		SEED_BANK_ZOMBIE = GD.Load<Texture2D>("uid://1styv80tn18a");
		SEED_BANK = GD.Load<Texture2D>("uid://c2ihkj4xv50hg");
		CONVEYOR_BELT_SUN = GD.Load<Texture2D>("uid://i6rwl358wldw");
		CONVEYOR_BELT_SUN_BACKDROP = GD.Load<Texture2D>("uid://6h0njlvpp86");
		CONVEYOR_BELT = GD.Load<Texture2D>("uid://dbyiqyhraff1b");
		CONVEYOR_BELT_BACKDROP = GD.Load<Texture2D>("uid://ddglfhcpxg451");
		_posMask = GetNode<Control>("%PosMask");
		sunLabel = GetNode<Label>("%SunLabel");
		seedContanin = GetNode<MarginContainer>("%SeedContanin");
		conveyor = GetNode<Control>("%Conveyor");
		_packetSlotContainer = GetNode<HBoxContainer>("%PacketSlotContainer");
		packetContainer = GetNode<HBoxContainer>("%PacketContainer");
		seedBankTexture = GetNode<NinePatchRect>("%SeedBankTexture");
		conveyorBeltRectPC = GetNode<NinePatchRect>("%ConveyorBeltRectPC");
		belt = GetNode<TextureRect>("%Belt");
		conveyorSunBankTexture = GetNode<TextureRect>("%ConveyorSunBankTexture");
		conveyorSunLabel = GetNode<Label>("%ConveyorSunLabel");
		Instance = this;
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
		return packetNum < 16;
	}

	public Vector2 GetPacketPos(int id)
	{
		return _posMask.GlobalPosition + new Vector2(51 * id, 0f);
	}

	public TowerDefenseInGamePacketShow AddPacket(TowerDefensePacketConfig _packetConfig)
	{
		packetNum++;
		TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = TowerDefenseManager.CreatePacketShow();
		towerDefenseInGamePacketShow.setPcLayout = true;
		towerDefenseInGamePacketShow.enforceRuntimeAvailabilityOnPress = false;
		packetContainer.AddChild(towerDefenseInGamePacketShow, forceReadableName: false, InternalMode.Disabled);
		towerDefenseInGamePacketShow.Init(_packetConfig);
		towerDefenseInGamePacketShow.onlyDraw = false;
		towerDefenseInGamePacketShow.OnPressed += DeletePacket;
		packetList.Add(towerDefenseInGamePacketShow);
		return towerDefenseInGamePacketShow;
	}

	public void DeletePacket(TowerDefenseInGamePacketShow _packet)
	{
		LevelEditorSeedbankEditor.Instance.levelConfig.canExport = false;
		int index = packetList.IndexOf(_packet);
		packetList.RemoveAt(index);
		_packet.QueueFree();
		packetNum--;
	}
}
