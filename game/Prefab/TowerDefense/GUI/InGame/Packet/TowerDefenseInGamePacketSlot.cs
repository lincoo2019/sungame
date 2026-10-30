using Godot;

[GlobalClass]
public partial class TowerDefenseInGamePacketSlot : Control
{
	private static Texture2D _packetSilhouettePc;

	private static Texture2D _packetSilhouetteMobile;

	private TextureRect _textureRect;

	private bool _reserveContainerFootprint;

	public bool isMobileSlot;

	private static Texture2D PACKET_SILHOUETTE_PC => _packetSilhouettePc ?? (_packetSilhouettePc = GD.Load<Texture2D>("uid://d131d07o455pm"));

	private static Texture2D PACKET_SILHOUETTE_MOBILE => _packetSilhouetteMobile ?? (_packetSilhouetteMobile = GD.Load<Texture2D>("uid://celgn60f027kl"));

	public override void _Ready()
	{
		_textureRect = GetNodeOrNull<TextureRect>("TextureRect");
		ApplyStyle();
	}

	public void SetMobileMode(bool enabled)
	{
		isMobileSlot = enabled;
		if (IsNodeReady())
		{
			ApplyStyle();
		}
	}

	public void SetContainerFootprint(bool enabled)
	{
		_reserveContainerFootprint = enabled;
		if (IsNodeReady())
		{
			ApplyStyle();
		}
	}

	private void ApplyStyle()
	{
		if (_textureRect != null)
		{
			if (isMobileSlot)
			{
				base.CustomMinimumSize = (_reserveContainerFootprint ? new Vector2(96f, 60f) : Vector2.Zero);
				_textureRect.Texture = PACKET_SILHOUETTE_MOBILE;
				_textureRect.Size = new Vector2(96f, 60f);
				_textureRect.Position = (_reserveContainerFootprint ? Vector2.Zero : new Vector2(-48f, -30f));
				Color modulate = _textureRect.Modulate;
				modulate.A = 0.5f;
				_textureRect.Modulate = modulate;
			}
			else
			{
				base.CustomMinimumSize = (_reserveContainerFootprint ? new Vector2(50f, 70f) : Vector2.Zero);
				_textureRect.Texture = PACKET_SILHOUETTE_PC;
				_textureRect.Size = new Vector2(50f, 70f);
				_textureRect.Position = (_reserveContainerFootprint ? Vector2.Zero : new Vector2(-25f, -33f));
				Color modulate2 = _textureRect.Modulate;
				modulate2.A = 1f;
				_textureRect.Modulate = modulate2;
			}
		}
	}

	public Vector2 GetVisualCenterGlobalPosition()
	{
		if (!_reserveContainerFootprint)
		{
			return base.GlobalPosition;
		}
		return base.GlobalPosition + base.Size / 2f;
	}
}
