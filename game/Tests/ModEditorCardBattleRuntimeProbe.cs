using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.ResourceEditors.GUI;

public partial class ModEditorCardBattleRuntimeProbe : Node
{
	private const string PacketPath = "res://Asset/Anime/Character/Plant/Chapter0/PeaShooter/Packet/PlantPeaShooter.tres";

	private readonly List<string> _failures = new List<string>();

	private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

	private XWGenericVisualResourceEditor _editor;

	private XWUndoRedoManager _history;

	public override async void _Ready()
	{
		_ = 23;
		try
		{
			ModEditorManager modEditorManager = ModEditorManager.Instance;
			if (!GodotObject.IsInstanceValid(modEditorManager))
			{
				modEditorManager = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Core/ModEditorManager.tscn", null, ResourceLoader.CacheMode.Reuse)?.Instantiate<ModEditorManager>(PackedScene.GenEditState.Disabled);
				if (GodotObject.IsInstanceValid(modEditorManager))
				{
					AddChild(modEditorManager, forceReadableName: false, InternalMode.Disabled);
				}
			}
			Require(GodotObject.IsInstanceValid(modEditorManager), "ModEditorManager could not be instantiated.");
			if (!GodotObject.IsInstanceValid(modEditorManager))
			{
				Finish();
				return;
			}
			await WaitFrames(2);
			Input.ParseInputEvent(new InputEventKey
			{
				Keycode = Key.F3,
				PhysicalKeycode = Key.F3,
				Pressed = true
			});
			Input.ParseInputEvent(new InputEventKey
			{
				Keycode = Key.F3,
				PhysicalKeycode = Key.F3,
				Pressed = false
			});
			bool flag = await WaitForEditor(900);
			Require(flag, "F3 did not initialize the Card editor within 900 frames.");
			if (!flag)
			{
				Finish();
				return;
			}
			bool flag2 = await EnterEditorSurface(900);
			Require(flag2, "F3 editor did not finish loading its main editing surface.");
			if (!flag2)
			{
				Finish();
				return;
			}
			bool window = FindAncestorWindow(_editor) != null;
			TowerDefensePacketConfig packet = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Plant/Chapter0/PeaShooter/Packet/PlantPeaShooter.tres", null, ResourceLoader.CacheMode.Reuse)?.Duplicate(deep: true) as TowerDefensePacketConfig;
			Require(GodotObject.IsInstanceValid(packet), "Peashooter packet fixture could not be duplicated.");
			if (!GodotObject.IsInstanceValid(packet))
			{
				Finish();
				return;
			}
			packet.overrideCost = 75;
			packet.overridePacketCooldown = 0.8;
			XWEditorInterface.Instance.EditResource(packet);
			XWEditorInterface.Instance.FocusPanel("card_editor");
			await WaitFrames(12);
			VBoxContainer vBoxContainer = FindControl<VBoxContainer>("EmbeddedInspectorHost");
			bool inspectorHidden = GodotObject.IsInstanceValid(vBoxContainer) && vBoxContainer.GetChildCount() == 0 && _editor.FindChild("EmbeddedResourceInspector", recursive: true, owned: false) == null;
			Require(inspectorHidden, "Exact packet resource still opened the raw embedded inspector.");
			XWCardVisualResourceEditor cardEditor = _editor as XWCardVisualResourceEditor;
			bool directPropertiesComplete = GodotObject.IsInstanceValid(cardEditor) && cardEditor.DirectMissingPropertyCount == 0;
			Require(directPropertiesComplete, $"Card direct-property surface omitted {cardEditor?.DirectMissingPropertyCount ?? (-1)} editable controls.");
			LineEdit instance = FindControl<LineEdit>("InlineSaveKeyLineEdit");
			LineEdit lineEdit = FindControl<LineEdit>("InlineAnimationLineEdit");
			SpinBox instance2 = FindControl<SpinBox>("InlineCooldownSpinBox");
			SpinBox instance3 = FindControl<SpinBox>("InlineStartingCooldownSpinBox");
			Label overrideBadge = FindControl<Label>("ValueOverrideBadge");
			bool directVisualFields = GodotObject.IsInstanceValid(cardEditor) && GodotObject.IsInstanceValid(instance) && GodotObject.IsInstanceValid(lineEdit) && GodotObject.IsInstanceValid(instance2) && GodotObject.IsInstanceValid(instance3) && GodotObject.IsInstanceValid(overrideBadge);
			Require(directVisualFields, "Card game surface is missing direct SaveKey/animation/cooldown override editors.");
			bool directTextRecovery = false;
			bool directNumbers = false;
			if (directVisualFields)
			{
				string originalClip = packet.packetAnimeClip;
				_history.ClearHistory();
				SimulateTextSession(lineEdit, originalClip + "_probe");
				await WaitFrames(2);
				bool applied = packet.packetAnimeClip == originalClip + "_probe" && _history.HasUndo();
				bool restored = _history.Undo();
				await WaitFrames(3);
				restored = restored && packet.packetAnimeClip == originalClip;
				directTextRecovery = applied && restored;
				instance2 = FindControl<SpinBox>("InlineCooldownSpinBox");
				instance3 = FindControl<SpinBox>("InlineStartingCooldownSpinBox");
				overrideBadge = FindControl<Label>("ValueOverrideBadge");
				if (GodotObject.IsInstanceValid(instance2) && GodotObject.IsInstanceValid(instance3) && GodotObject.IsInstanceValid(overrideBadge))
				{
					SetSpinBoxValue(instance2, 0.6);
					SetSpinBoxValue(instance3, 0.3);
					await WaitFrames(3);
					directNumbers = Math.Abs(packet.overridePacketCooldown - 0.6) < 0.001 && Math.Abs(packet.overrideStartingCooldown - 0.3) < 0.001 && overrideBadge.Text.Contains("冷却 卡牌覆盖 0.6", StringComparison.Ordinal) && overrideBadge.Text.Contains("初始 卡牌覆盖 0.3", StringComparison.Ordinal);
				}
			}
			Require(directTextRecovery, "Direct animation Clip text did not restore through global UndoRedo.");
			Require(directNumbers, $"Direct cooldown overrides did not update the resource and visual value badge: cooldown={packet.overridePacketCooldown:0.###}, starting={packet.overrideStartingCooldown:0.###}, badge={overrideBadge?.Text}.");
			TowerDefenseInGamePacketShow packetShow = _editor.FindChild("CardPacketShowPreview", recursive: true, owned: false) as TowerDefenseInGamePacketShow;
			Button interaction = FindControl<Button>("PacketInteractionButton");
			bool realPacket = GodotObject.IsInstanceValid(packetShow) && packetShow.onlyDraw && packetShow.config == packet && GodotObject.IsInstanceValid(interaction);
			Require(realPacket, "Card surface did not mount the real safe TowerDefenseInGamePacketShow node.");
			bool hover = false;
			bool clickSelect = false;
			if (realPacket)
			{
				interaction.EmitSignal(Control.SignalName.MouseEntered);
				await WaitFrames(3);
				hover = GodotObject.IsInstanceValid(packetShow.sprite) && !packetShow.sprite.IsFrozenPreview;
				interaction.EmitSignal(BaseButton.SignalName.Pressed);
				await WaitFrames(2);
				clickSelect = packetShow.select;
				interaction.EmitSignal(Control.SignalName.MouseExited);
				await WaitFrames(2);
				hover = hover && packetShow.sprite.IsFrozenPreview;
			}
			Require(hover, "Card hover did not run/freeze the real packet animation.");
			Require(clickSelect, "Clicking the visual card did not toggle the real packet selection visual.");
			SpinBox previewSun = FindControl<SpinBox>("PreviewSunSpinBox");
			HFlowContainer host = FindControl<HFlowContainer>("RuntimeStateCards");
			XWGameVisualChoiceCard lockedStateCard = FindVisualChoiceCard(host, "4");
			XWGameVisualChoiceCard availableStateCard = FindVisualChoiceCard(host, "0");
			Label battleStatus = FindControl<Label>("BattleStatusLabel");
			bool visualStateCards = GodotObject.IsInstanceValid(cardEditor) && cardEditor.RuntimeStateVisualCardCount == 5 && GodotObject.IsInstanceValid(lockedStateCard) && GodotObject.IsInstanceValid(lockedStateCard.Icon) && GodotObject.IsInstanceValid(availableStateCard);
			Require(visualStateCards, "Card battle states are not exposed as five icon-first visual cards.");
			bool insufficientSun = false;
			bool locked = false;
			bool stateRestored = false;
			if (GodotObject.IsInstanceValid(previewSun) && visualStateCards && GodotObject.IsInstanceValid(battleStatus) && GodotObject.IsInstanceValid(packetShow))
			{
				previewSun.SetValueNoSignal(0.0);
				previewSun.EmitSignal(Godot.Range.SignalName.ValueChanged, 0.0);
				await WaitFrames(2);
				insufficientSun = packetShow.openShadow && !packetShow.alive && battleStatus.Text.Contains("阳光不足", StringComparison.Ordinal);
				lockedStateCard.EmitSignal(BaseButton.SignalName.Pressed);
				await WaitFrames(2);
				locked = packetShow.@lock && !packetShow.alive && battleStatus.Text.Contains("尚未解锁", StringComparison.Ordinal);
				previewSun.SetValueNoSignal(500.0);
				previewSun.EmitSignal(Godot.Range.SignalName.ValueChanged, 500.0);
				availableStateCard.EmitSignal(BaseButton.SignalName.Pressed);
				await WaitFrames(2);
				stateRestored = !packetShow.openShadow && !packetShow.@lock && packetShow.alive;
			}
			Require(insufficientSun, "Preview sun did not drive the real card insufficient-sun visual.");
			Require(locked, "Locked state did not drive the real card disabled visual.");
			Require(stateRestored, "Card did not recover to the available state after sun/lock simulation.");
			HFlowContainer host2 = FindControl<HFlowContainer>("TypeCards");
			TowerDefenseEnum.PACKET_TYPE originalType = packet.type;
			TowerDefenseEnum.PACKET_TYPE targetType = ((originalType != TowerDefenseEnum.PACKET_TYPE.GOLD) ? TowerDefenseEnum.PACKET_TYPE.GOLD : TowerDefenseEnum.PACKET_TYPE.DIAMOND);
			int num = (int)targetType;
			XWGameVisualChoiceCard xWGameVisualChoiceCard = FindVisualChoiceCard(host2, num.ToString());
			_history.ClearHistory();
			bool packetTypeUndoRedo = false;
			if (GodotObject.IsInstanceValid(xWGameVisualChoiceCard) && GodotObject.IsInstanceValid(xWGameVisualChoiceCard.Icon))
			{
				xWGameVisualChoiceCard.EmitSignal(BaseButton.SignalName.Pressed);
				await WaitFrames(2);
				bool restored = packet.type == targetType && _history.HasUndo();
				bool applied = _history.Undo();
				await WaitFrames(2);
				applied = applied && packet.type == originalType;
				bool redone = _history.Redo();
				await WaitFrames(2);
				packetTypeUndoRedo = restored && applied && redone && packet.type == targetType;
			}
			Require(packetTypeUndoRedo, "Packet type visual card did not round-trip through global UndoRedo.");
			CheckButton mobilePreview = FindControl<CheckButton>("MobilePreviewCheck");
			bool mobileLayout = false;
			bool pcLayoutRestored = false;
			if (GodotObject.IsInstanceValid(mobilePreview) && GodotObject.IsInstanceValid(packetShow))
			{
				mobilePreview.SetPressedNoSignal(pressed: true);
				mobilePreview.EmitSignal(BaseButton.SignalName.Toggled, true);
				await WaitFrames(3);
				mobileLayout = packetShow == _editor.FindChild("CardPacketShowPreview", recursive: true, owned: false) && packetShow.isMobile && packetShow.setMobileLayout && !packetShow.setPcLayout && Approximately(packetShow.backgroundTexture.Size, new Vector2(96f, 60f)) && Approximately(packetShow.button.Size, new Vector2(94f, 60f));
				mobilePreview.SetPressedNoSignal(pressed: false);
				mobilePreview.EmitSignal(BaseButton.SignalName.Toggled, false);
				await WaitFrames(3);
				pcLayoutRestored = packetShow == _editor.FindChild("CardPacketShowPreview", recursive: true, owned: false) && !packetShow.isMobile && packetShow.setPcLayout && !packetShow.setMobileLayout && Approximately(packetShow.backgroundTexture.Size, new Vector2(50f, 70f)) && Approximately(packetShow.button.Size, new Vector2(50f, 70f));
			}
			Require(mobileLayout, "Mobile preview toggle did not apply the real 96x60 PacketShow preset on the reused node.");
			Require(pcLayoutRestored, "Returning to PC preview did not restore the real 50x70 PacketShow preset on the reused node.");
			Button button = FindControl<Button>("PreviewPlantButton");
			double cooldownBefore = 0.0;
			double num2 = 0.0;
			int readoutsBefore = (GodotObject.IsInstanceValid(cardEditor) ? cardEditor.CardRuntimeReadoutRefreshCount : (-1));
			bool editorVisible = false;
			bool editorProcessing = false;
			bool realtimeCooldown = false;
			if (GodotObject.IsInstanceValid(button) && GodotObject.IsInstanceValid(packetShow))
			{
				button.EmitSignal(BaseButton.SignalName.Pressed);
				cooldownBefore = packetShow.coldDownTimer;
				editorVisible = _editor.IsVisibleInTree();
				editorProcessing = _editor.IsProcessing();
				await WaitFrames(12);
				num2 = packetShow.coldDownTimer;
				realtimeCooldown = packetShow.coldDownOpen && packetShow.coldDown > 0.0 && cooldownBefore > num2 && num2 >= 0.0 && Math.Abs(packetShow.coldDownProgressBar.Value - num2) < 0.01;
			}
			Require(realtimeCooldown, $"Configured cooldown did not advance in real seconds ({cooldownBefore:0.000}->{num2:0.000}).");
			int num3 = (GodotObject.IsInstanceValid(cardEditor) ? (cardEditor.CardRuntimeReadoutRefreshCount - readoutsBefore) : 2147483647);
			bool scheduledReadouts = num3 > 0 && num3 <= 6;
			Require(scheduledReadouts, $"Cooldown UI readouts were not throttled ({num3} refreshes / 12 frames).");
			Node2D characterRoot = FindControl<Node2D>("CharacterPreviewRoot");
			TowerDefenseCharacter character = FindRuntimeCharacter(characterRoot);
			bool safeCharacter = GodotObject.IsInstanceValid(character) && character.editorPreviewMode && !character.inGame && !character.IsProcessing() && !character.IsPhysicsProcessing();
			Require(safeCharacter, "Full card character preview was not mounted behind the preview safety boundary.");
			SpinBox spinBox = FindControl<SpinBox>("InlineCostSpinBox");
			_history.ClearHistory();
			bool undoRedo = false;
			bool characterReuse = false;
			if (GodotObject.IsInstanceValid(spinBox))
			{
				int characterBuildsBefore = cardEditor?.CardCharacterPreviewBuildCount ?? (-1);
				spinBox.EmitSignal(Control.SignalName.FocusEntered);
				spinBox.SetValueNoSignal(125.0);
				spinBox.EmitSignal(Godot.Range.SignalName.ValueChanged, 125.0);
				spinBox.EmitSignal(Control.SignalName.FocusExited);
				await WaitFrames(3);
				bool redone = packet.overrideCost == 125 && _history.HasUndo();
				characterReuse = GodotObject.IsInstanceValid(cardEditor) && cardEditor.CardCharacterPreviewBuildCount == characterBuildsBefore && packetShow == _editor.FindChild("CardPacketShowPreview", recursive: true, owned: false) && character == FindRuntimeCharacter(characterRoot);
				bool applied = _history.Undo();
				await WaitFrames(3);
				applied = applied && packet.overrideCost == 75;
				bool restored = _history.Redo();
				await WaitFrames(3);
				restored = restored && packet.overrideCost == 125;
				undoRedo = redone && applied && restored;
			}
			Require(undoRedo, "Direct card cost edit did not round-trip through global UndoRedo.");
			Require(characterReuse, "Changing a card number rebuilt the full character scene unnecessarily.");
			double hiddenCooldownBefore = (GodotObject.IsInstanceValid(packetShow) ? packetShow.coldDownTimer : (-1.0));
			_editor.Hide();
			await WaitFrames(8);
			bool flag3 = GodotObject.IsInstanceValid(cardEditor) && !cardEditor.IsCardPreviewRendering;
			bool hiddenIdle = !_editor.IsProcessing() && (!GodotObject.IsInstanceValid(packetShow) || Math.Abs(packetShow.coldDownTimer - hiddenCooldownBefore) < 0.001) && flag3;
			_editor.Show();
			XWEditorInterface.Instance.FocusPanel("card_editor");
			await WaitFrames(3);
			Require(hiddenIdle, "Hidden card editor continued advancing its cooldown preview.");
			GD.Print($"[MOD_EDITOR_CARD_BATTLE_PROBE] window={window} inspectorHidden={inspectorHidden} realPacket={realPacket} hover={hover} clickSelect={clickSelect} mobileLayout={mobileLayout} pcLayoutRestored={pcLayoutRestored} directPropertiesComplete={directPropertiesComplete} directVisualFields={directVisualFields} directTextRecovery={directTextRecovery} directNumbers={directNumbers} visualStateCards={visualStateCards} insufficientSun={insufficientSun} locked={locked} stateRestored={stateRestored} packetTypeUndoRedo={packetTypeUndoRedo} realtimeCooldown={realtimeCooldown} scheduledReadouts={scheduledReadouts} hiddenIdle={hiddenIdle} safeCharacter={safeCharacter} characterReuse={characterReuse} undoRedo={undoRedo} editorVisible={editorVisible} editorProcessing={editorProcessing} failures={_failures.Count} elapsedMs={_stopwatch.ElapsedMilliseconds}");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		Finish();
	}

	private async Task<bool> WaitForEditor(int maxFrames)
	{
		for (int i = 0; i < maxFrames; i++)
		{
			XWEditorInterface instance = XWEditorInterface.Instance;
			if (instance?.GetResourceEditor("card_editor") is XWGenericVisualResourceEditor xWGenericVisualResourceEditor && GodotObject.IsInstanceValid(xWGenericVisualResourceEditor))
			{
				_editor = xWGenericVisualResourceEditor;
				_history = instance.GetUndoRedoManager();
				return GodotObject.IsInstanceValid(_history);
			}
			await WaitFrames(1);
		}
		return false;
	}

	private async Task<bool> EnterEditorSurface(int maxFrames)
	{
		for (int i = 0; i < maxFrames; i++)
		{
			Control obj = XWEditorInterface.Instance?.GetEditorPanel();
			Node instance = obj?.FindChild("LoadingOverlay", recursive: true, owned: false);
			Control control = obj?.FindChild("ProjectManagerPanel", recursive: true, owned: false) as Control;
			if (GodotObject.IsInstanceValid(obj) && !GodotObject.IsInstanceValid(instance) && GodotObject.IsInstanceValid(control))
			{
				control.Hide();
				XWEditorInterface.Instance.FocusPanel("card_editor");
				await WaitFrames(3);
				return _editor.IsVisibleInTree();
			}
			await WaitFrames(1);
		}
		return false;
	}

	private T FindControl<T>(string name) where T : Node
	{
		return _editor?.FindChild(name, recursive: true, owned: false) as T;
	}

	private static XWGameVisualChoiceCard FindVisualChoiceCard(Node host, string choiceKey)
	{
		if (!GodotObject.IsInstanceValid(host))
		{
			return null;
		}
		foreach (Node child in host.GetChildren())
		{
			if (child is XWGameVisualChoiceCard xWGameVisualChoiceCard && xWGameVisualChoiceCard.ChoiceKey == choiceKey)
			{
				return xWGameVisualChoiceCard;
			}
		}
		return null;
	}

	private static void SimulateTextSession(LineEdit control, string value)
	{
		control.EmitSignal(Control.SignalName.FocusEntered);
		control.Text = value;
		control.EmitSignal(LineEdit.SignalName.TextChanged, value);
		control.EmitSignal(LineEdit.SignalName.TextSubmitted, value);
		control.EmitSignal(Control.SignalName.FocusExited);
	}

	private static void SetSpinBoxValue(SpinBox control, double value)
	{
		control.EmitSignal(Control.SignalName.FocusEntered);
		control.SetValueNoSignal(value);
		control.EmitSignal(Godot.Range.SignalName.ValueChanged, value);
		control.EmitSignal(Control.SignalName.FocusExited);
	}

	private static TowerDefenseCharacter FindRuntimeCharacter(Node root)
	{
		if (root is TowerDefenseCharacter result)
		{
			return result;
		}
		if (!GodotObject.IsInstanceValid(root))
		{
			return null;
		}
		foreach (Node child in root.GetChildren())
		{
			TowerDefenseCharacter towerDefenseCharacter = FindRuntimeCharacter(child);
			if (GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				return towerDefenseCharacter;
			}
		}
		return null;
	}

	private static Window FindAncestorWindow(Node node)
	{
		Node node2 = node;
		while (GodotObject.IsInstanceValid(node2))
		{
			if (node2 is Window result)
			{
				return result;
			}
			node2 = node2.GetParent();
		}
		return null;
	}

	private static bool Approximately(Vector2 actual, Vector2 expected)
	{
		return actual.IsEqualApprox(expected);
	}

	private async Task WaitFrames(int count)
	{
		for (int i = 0; i < count; i++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private void Require(bool condition, string message)
	{
		if (!condition)
		{
			_failures.Add(message);
			GD.PrintErr("[MOD_EDITOR_CARD_BATTLE_PROBE_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_CARD_BATTLE_PROBE_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
