using System;
using Godot;

[GlobalClass]
public partial class SlotMachineControl : Control
{
	private sealed class ReelView
	{
		public Control Clip;

		public Control Track;

		public CardFace FinalCard;

		public float Offset;

		public float TargetOffset;

		public double StopTime = 0.1;

		public bool Stopped = true;
	}

	private sealed class CardFace
	{
		public Control Root;

		public TowerDefenseInGamePacketShow Packet;

		public string ItemKey = "";

		public string DisplayKey = "";
	}

	private const float ReelWidth = 49f;

	private const float ReelHeight = 47f;

	private const float ReelStep = 82f;

	private const float PacketScale = 0.86f;

	private const float HandleShaftLength = 56f;

	private const float HandleRestDegrees = -50f;

	private const float HandlePullDegrees = 35f;

	private static readonly Vector2 PacketPosition = new Vector2(23.5f, 20f);

	private static readonly Vector2 HandlePivot = new Vector2(286f, 73.5f);

	private static readonly Vector2 HandleBaseVisibleCenter = new Vector2(12.5f, 77f);

	private static readonly Vector2 HandleShaftPivot = new Vector2(4f, 5.5f);

	private static readonly Vector2 HandleBallSize = new Vector2(25f, 25f);

	private static readonly string[] FallbackSpinItems = new string[4] { "PlantPeaShooterSingle", "PlantWallnut", "PlantSnowPea", "PlantSunFlower" };

	private TowerDefenseBattleFeatureSlotMachine _feature;

	private readonly ReelView[] _reels = new ReelView[3];

	private Label _statusLabel;

	private TextureRect _shaftTexture;

	private TextureRect _ballTexture;

	private AudioStreamPlayer _audioPlayer;

	private string[] _spinItems = FallbackSpinItems;

	private string[] _finalItems = new string[3] { "PlantPeaShooterSingle", "PlantWallnut", "PlantSnowPea" };

	private Action<string[]> _spinFinished;

	private double _spinTimer;

	private double _spinDuration = 1.2;

	private bool _spinning;

	private SceneTree _spinSettleTree;

	private Action _spinSettleHandler;

	public void Init(TowerDefenseBattleFeatureSlotMachine feature)
	{
		_feature = feature;
		_spinItems = _feature.GetSpinItemKeys();
		if (_spinItems.Length == 0)
		{
			_spinItems = FallbackSpinItems;
		}
		base.Name = "SlotMachineControl";
		base.CustomMinimumSize = new Vector2(340f, 108f);
		base.Size = base.CustomMinimumSize;
		base.MouseFilter = MouseFilterEnum.Stop;
		base.MouseDefaultCursorShape = CursorShape.PointingHand;
		if (IsNodeReady())
		{
			BindSceneNodes();
		}
		SetStatusText(_feature.GetStatusText());
	}

	public override void _Ready()
	{
		BindSceneNodes();
		for (int i = 0; i < _reels.Length; i++)
		{
			SetReelItem(i, GetFinalItem(i));
		}
		if (_feature != null)
		{
			SetStatusText(_feature.GetStatusText());
		}
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton inputEventMouseButton && inputEventMouseButton.ButtonIndex == MouseButton.Left && inputEventMouseButton.Pressed)
		{
			_feature?.RequestSpin();
		}
	}

	public override void _Process(double delta)
	{
		if (!_spinning)
		{
			return;
		}
		_spinTimer -= delta;
		double elapsed = _spinDuration - _spinTimer;
		bool flag = true;
		for (int i = 0; i < _reels.Length; i++)
		{
			ReelView reelView = _reels[i];
			if (reelView != null)
			{
				SpinReel(reelView, elapsed);
				flag &= reelView.Stopped;
			}
		}
		double num = 1.0 - (double)Mathf.Clamp((float)(_spinTimer / _spinDuration), 0f, 1f);
		SetHandleProgress(Mathf.Sin((float)num * (float)Math.PI));
		if (_spinTimer <= 0.0 || flag)
		{
			_spinning = false;
			SetReelsToFinalOffset();
			SetHandleProgress(0f);
			_audioPlayer?.Stop();
			ScheduleSpinCompletion();
		}
	}

	public void PlaySpin(string[] items, double duration, Action<string[]> spinFinished)
	{
		_finalItems = items;
		_spinFinished = spinFinished;
		_spinDuration = Mathf.Max(0.35f, (float)duration);
		_spinTimer = _spinDuration;
		_spinning = true;
		for (int i = 0; i < _reels.Length; i++)
		{
			ReelView reelView = _reels[i];
			if (reelView != null)
			{
				string[] array = new string[8 + i * 2];
				for (int j = 0; j < array.Length - 1; j++)
				{
					array[j] = GetRandomSpinItem();
				}
				array[^1] = GetFinalItem(i);
				BuildReelStrip(reelView, array);
				reelView.Offset = 0f;
				reelView.TargetOffset = (float)(array.Length - 1) * 82f;
				reelView.StopTime = Math.Max(0.1, GetReelStopTime(i));
				reelView.Stopped = false;
				ApplyReelPosition(reelView);
			}
		}
		_audioPlayer?.Play();
	}

	public void SetStatusText(string text)
	{
		if (_statusLabel != null)
		{
			_statusLabel.Text = text;
		}
	}

	public void SetInteractable(bool interactable)
	{
		if (_spinning)
		{
			base.Modulate = Colors.White;
			base.MouseDefaultCursorShape = CursorShape.Arrow;
		}
		else
		{
			base.Modulate = (interactable ? Colors.White : new Color(0.72f, 0.72f, 0.72f, 0.92f));
			base.MouseDefaultCursorShape = (CursorShape)(interactable ? 2 : 0);
		}
	}

	private void BindSceneNodes()
	{
		for (int i = 0; i < _reels.Length; i++)
		{
			Control nodeOrNull = GetNodeOrNull<Control>($"%Reel{i + 1}");
			Control nodeOrNull2 = GetNodeOrNull<Control>($"%Reel{i + 1}Track");
			if (!GodotObject.IsInstanceValid(nodeOrNull) || !GodotObject.IsInstanceValid(nodeOrNull2))
			{
				GD.PushWarning($"[SlotMachine] Missing reel scene nodes: Reel{i + 1}");
			}
			else
			{
				_reels[i] = new ReelView
				{
					Clip = nodeOrNull,
					Track = nodeOrNull2,
					Stopped = true
				};
			}
		}
		_statusLabel = GetNodeOrNull<Label>("%StatusLabel");
		_shaftTexture = GetNodeOrNull<TextureRect>("%HandleShaft");
		_ballTexture = GetNodeOrNull<TextureRect>("%HandleBall");
		_audioPlayer = GetNodeOrNull<AudioStreamPlayer>("%SlotMachineAudio");
		SetHandleProgress(0f);
	}

	private void BuildReelStrip(ReelView reel, string[] itemKeys)
	{
		ClearTrack(reel);
		for (int i = 0; i < itemKeys.Length; i++)
		{
			CardFace cardFace = CreateCard(itemKeys[i], new Vector2(0f, (float)(-i) * 82f));
			reel.Track.AddChild(cardFace.Root, forceReadableName: false, InternalMode.Disabled);
			ApplyCard(cardFace);
			if (i == itemKeys.Length - 1)
			{
				reel.FinalCard = cardFace;
			}
		}
	}

	private void ClearTrack(ReelView reel)
	{
		reel.FinalCard = null;
		foreach (Node child in reel.Track.GetChildren())
		{
			reel.Track.RemoveChild(child);
			child.QueueFree();
		}
	}

	private CardFace CreateCard(string itemKey, Vector2 position)
	{
		Control control = new Control
		{
			Position = position,
			Size = new Vector2(49f, 47f),
			MouseFilter = MouseFilterEnum.Ignore
		};
		TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = TowerDefenseManager.CreatePacketShow();
		towerDefenseInGamePacketShow.Name = "PacketShow";
		towerDefenseInGamePacketShow.Position = PacketPosition;
		towerDefenseInGamePacketShow.Scale = new Vector2(0.86f, 0.86f);
		towerDefenseInGamePacketShow.MouseFilter = MouseFilterEnum.Ignore;
		control.AddChild(towerDefenseInGamePacketShow, forceReadableName: false, InternalMode.Disabled);
		return new CardFace
		{
			Root = control,
			Packet = towerDefenseInGamePacketShow,
			ItemKey = itemKey,
			DisplayKey = GetDisplayPacketName(itemKey)
		};
	}

	private void ApplyCard(CardFace card)
	{
		if (!GodotObject.IsInstanceValid(card?.Packet))
		{
			return;
		}
		ConfigurePacketShow(card.Packet);
		if (!card.Packet.IsNodeReady())
		{
			card.Packet.Visible = false;
			GD.PushWarning("[SlotMachine] Packet preview did not become ready after entering the reel tree.");
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(card.DisplayKey);
		if (!GodotObject.IsInstanceValid(packetConfig))
		{
			GD.PushWarning("[SlotMachine] Invalid packet display: " + card.DisplayKey);
			packetConfig = TowerDefenseManager.GetPacketConfig(FallbackSpinItems[0]);
		}
		if (!GodotObject.IsInstanceValid(packetConfig))
		{
			card.Packet.Visible = false;
			return;
		}
		card.Packet.Visible = true;
		card.Packet.Init(packetConfig);
		ConfigurePacketShow(card.Packet);
		ShowPacketViewport(card.Packet);
	}

	private void ConfigurePacketShow(TowerDefenseInGamePacketShow packet)
	{
		packet.onlyDraw = true;
		packet.showCost = false;
		packet.showLove = false;
		packet.start = false;
		packet.select = false;
		packet.alive = true;
		packet.@lock = false;
		packet.coldDownOpen = false;
		packet.coldDownTimer = 0.0;
		packet.pressDelayTimer = 0.0;
		packet.MouseFilter = MouseFilterEnum.Ignore;
		packet.Position = PacketPosition;
		packet.Scale = new Vector2(0.86f, 0.86f);
		packet.ClearEventHandlers();
		if (GodotObject.IsInstanceValid(packet.itemCostLabel))
		{
			packet.itemCostLabel.Visible = false;
		}
		if (GodotObject.IsInstanceValid(packet.button))
		{
			packet.button.Visible = false;
			packet.button.MouseFilter = MouseFilterEnum.Ignore;
		}
		if (GodotObject.IsInstanceValid(packet.coldDownProgressBar))
		{
			packet.coldDownProgressBar.Visible = false;
		}
		if (GodotObject.IsInstanceValid(packet.loveButton))
		{
			packet.loveButton.Visible = false;
			packet.loveButton.MouseFilter = MouseFilterEnum.Ignore;
		}
		if (GodotObject.IsInstanceValid(packet.selectTexture))
		{
			packet.selectTexture.Visible = false;
		}
	}

	private void ShowPacketViewport(TowerDefenseInGamePacketShow packet)
	{
		if (GodotObject.IsInstanceValid(packet.previewClip))
		{
			packet.previewClip.Visible = true;
		}
		if (GodotObject.IsInstanceValid(packet.sprite))
		{
			packet.sprite.Visible = true;
			packet.sprite.pause = false;
			packet.sprite.ProcessMode = ProcessModeEnum.Inherit;
		}
	}

	private void SpinReel(ReelView reel, double elapsed)
	{
		if (!reel.Stopped)
		{
			float num = Mathf.Clamp((float)(elapsed / reel.StopTime), 0f, 1f);
			float num2 = 1f - Mathf.Pow(1f - num, 3f);
			reel.Offset = reel.TargetOffset * num2;
			if (num >= 1f)
			{
				reel.Offset = reel.TargetOffset;
				reel.Stopped = true;
			}
			ApplyReelPosition(reel);
		}
	}

	private void ApplyReelPosition(ReelView reel)
	{
		reel.Track.Position = new Vector2(0f, reel.Offset);
	}

	private double GetReelStopTime(int index)
	{
		double num = Math.Min(0.55, Math.Max(0.18, _spinDuration * 0.5));
		double num2 = num / (double)_reels.Length;
		return _spinDuration - num + num2 * ((double)index + 0.65);
	}

	private void SetFinalItems()
	{
		for (int i = 0; i < _reels.Length; i++)
		{
			SetReelItem(i, GetFinalItem(i));
		}
	}

	private void SetReelsToFinalOffset()
	{
		for (int i = 0; i < _reels.Length; i++)
		{
			ReelView reelView = _reels[i];
			if (reelView != null)
			{
				reelView.Offset = reelView.TargetOffset;
				reelView.Stopped = true;
				ApplyReelPosition(reelView);
			}
		}
	}

	private void ScheduleSpinCompletion()
	{
		if (_spinFinished != null && _spinSettleHandler == null && IsInsideTree())
		{
			_spinSettleTree = GetTree();
			if (GodotObject.IsInstanceValid(_spinSettleTree))
			{
				_spinSettleHandler = CompleteSpinOnNextFrame;
				_spinSettleTree.ProcessFrame += _spinSettleHandler;
			}
		}
	}

	private void CompleteSpinOnNextFrame()
	{
		CancelSpinSettleCallback();
		Action<string[]> spinFinished = _spinFinished;
		_spinFinished = null;
		if (IsInsideTree())
		{
			spinFinished?.Invoke(GetCurrentItems());
		}
	}

	private void CancelSpinSettleCallback()
	{
		if (GodotObject.IsInstanceValid(_spinSettleTree) && _spinSettleHandler != null)
		{
			_spinSettleTree.ProcessFrame -= _spinSettleHandler;
		}
		_spinSettleTree = null;
		_spinSettleHandler = null;
	}

	public void CancelPendingWork()
	{
		CancelSpinSettleCallback();
		_spinFinished = null;
		_spinning = false;
		_spinTimer = 0.0;
		_audioPlayer?.Stop();
		_feature = null;
	}

	public override void _ExitTree()
	{
		CancelPendingWork();
		base._ExitTree();
	}

	private string[] GetCurrentItems()
	{
		string[] array = new string[_reels.Length];
		for (int i = 0; i < _reels.Length; i++)
		{
			array[i] = (_reels[i]?.FinalCard)?.ItemKey ?? GetFinalItem(i);
		}
		return array;
	}

	private void SetReelItem(int index, string itemKey)
	{
		ReelView reelView = _reels[index];
		if (reelView != null)
		{
			BuildReelStrip(reelView, new string[1] { itemKey });
			reelView.Offset = 0f;
			reelView.TargetOffset = 0f;
			reelView.StopTime = 0.1;
			reelView.Stopped = true;
			ApplyReelPosition(reelView);
		}
	}

	private string GetFinalItem(int index)
	{
		if (index >= 0 && index < _finalItems.Length)
		{
			return _finalItems[index];
		}
		return FallbackSpinItems[0];
	}

	private string GetRandomSpinItem()
	{
		return _spinItems[GD.RandRange(0, _spinItems.Length - 1)];
	}

	private static string GetDisplayPacketName(string itemKey)
	{
		string text = (itemKey ?? "").Trim();
		int num = text.LastIndexOf('@');
		if (num >= 0)
		{
			text = text.Substring(0, num).Trim();
		}
		if (text.StartsWith("SUN:", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "SUN", StringComparison.OrdinalIgnoreCase))
		{
			return "PlantSunFlower";
		}
		if (text.StartsWith("BRAIN:", StringComparison.OrdinalIgnoreCase) || text.StartsWith("BRAINSUN:", StringComparison.OrdinalIgnoreCase) || text.StartsWith("SUNBRAIN:", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "BRAIN", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "BRAINSUN", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "SUNBRAIN", StringComparison.OrdinalIgnoreCase))
		{
			return "ItemBrain";
		}
		if (text.StartsWith("DIAMOND:", StringComparison.OrdinalIgnoreCase) || text.StartsWith("COIN:", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "DIAMOND", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "COIN", StringComparison.OrdinalIgnoreCase))
		{
			return "PlantDiamondseed";
		}
		return itemKey;
	}

	private void SetHandleProgress(float progress)
	{
		float num = Mathf.Lerp(-50f, 35f, Mathf.Clamp(progress, 0f, 1f));
		float s = Mathf.DegToRad(num);
		Vector2 vector = new Vector2(Mathf.Cos(s), Mathf.Sin(s));
		if (_shaftTexture != null)
		{
			_shaftTexture.Position = HandlePivot - HandleShaftPivot;
			_shaftTexture.RotationDegrees = num;
		}
		if (_ballTexture != null)
		{
			Vector2 vector2 = HandlePivot + vector * 56f;
			_ballTexture.Position = vector2 - HandleBallSize * 0.5f;
		}
	}
}
