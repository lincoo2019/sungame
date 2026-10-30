using Godot;
using Godot.Collections;

[GlobalClass]
public partial class StarExchangeItem : Control
{
	private Label costLabel;

	private Button button;

	private Control packetNode;

	private TowerDefenseInGamePacketShow packet;

	private TextureRect finishTexture;

	public Dictionary data;

	public int finishNum;

	public override void _Ready()
	{
		costLabel = GetNode<Label>("%CostLabel");
		button = GetNode<Button>("%Button");
		packetNode = GetNode<Control>("%PacketNode");
		packet = GetNode<TowerDefenseInGamePacketShow>("%Packet");
		finishTexture = GetNode<TextureRect>("%FinishTexture");
		button.Pressed += Pressed;
	}

	public void Init(Dictionary _data, int _finishNum = 0)
	{
		data = _data;
		finishNum = _finishNum;
		if (data["Type"].AsString() == "Packet")
		{
			packet.Init(TowerDefenseManager.GetPacketConfig(data["Key"].AsString()));
		}
		costLabel.Text = string.Format("x{0}", data["FinishNum"].AsInt32());
		if (finishNum >= data["FinishNum"].AsInt32() && GameSaveManager.Instance.GetTowerDefensePacketValue(data["Key"].AsString()).GetValueOrDefault("Unlock", false).AsBool())
		{
			finishTexture.Visible = true;
		}
	}

	public void Pressed()
	{
		if (finishNum >= data["FinishNum"].AsInt32())
		{
			Dictionary towerDefensePacketValue = GameSaveManager.Instance.GetTowerDefensePacketValue(data["Key"].AsString());
			if (!towerDefensePacketValue.GetValueOrDefault("Unlock", false).AsBool())
			{
				towerDefensePacketValue["Unlock"] = true;
				finishTexture.Visible = true;
				GameSaveManager.Instance.SetTowerDefensePacketValue(data["Key"].AsString(), towerDefensePacketValue);
				GameSaveManager.Instance.Save();
				DialogManager.Instance.DialogCreate("DialogBoxTips").Set("text", "[center][font_size=24]您成功兑换该植物[/font_size][/center]");
			}
			else
			{
				DialogManager.Instance.DialogCreate("DialogBoxTips").Set("text", "[center][font_size=24]您已经兑换该植物[/font_size][/center]");
			}
		}
		else
		{
			DialogManager.Instance.DialogCreate("DialogBoxTips").Set("text", "[center][font_size=24]您的星星不足[/font_size][/center]");
		}
	}
}
