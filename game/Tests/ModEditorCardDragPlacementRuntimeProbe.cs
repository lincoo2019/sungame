using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.ResourceEditors.GUI;

public partial class ModEditorCardDragPlacementRuntimeProbe : Node
{
	private const string PacketPath = "res://Asset/Anime/Character/Plant/Chapter0/PeaShooter/Packet/PlantPeaShooter.tres";

	private readonly List<string> _failures = new List<string>();

	private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

	private XWCardVisualResourceEditor _editor;

	private XWUndoRedoManager _history;

	private Window _editorWindow;

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
			_editorWindow = FindAncestorWindow(_editor);
			bool window = GodotObject.IsInstanceValid(_editorWindow);
			TowerDefensePacketConfig packet = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Plant/Chapter0/PeaShooter/Packet/PlantPeaShooter.tres", null, ResourceLoader.CacheMode.Reuse)?.Duplicate(deep: true) as TowerDefensePacketConfig;
			Require(GodotObject.IsInstanceValid(packet), "Peashooter packet fixture could not be duplicated.");
			if (!GodotObject.IsInstanceValid(packet))
			{
				Finish();
				return;
			}
			packet.overrideCost = 75;
			packet.overridePacketCooldown = 0.8;
			packet.pressedActions = new Array<CardActionBehaviorDefinition>
			{
				new ModEditorCardDragPlacementSideEffectEvent()
			};
			packet.useSucceededActions = new Array<CardActionBehaviorDefinition>
			{
				new ModEditorCardDragPlacementSideEffectEvent()
			};
			ModEditorCardDragPlacementSideEffectEvent.ExecutionCount = 0;
			XWEditorInterface.Instance.EditResource(packet);
			XWEditorInterface.Instance.FocusPanel("card_editor");
			await WaitFrames(12);
			Button interaction = FindControl<Button>("PacketInteractionButton");
			Control dropSurface = FindControl<Control>("CardBattleDropSurface");
			ColorRect instance = FindControl<ColorRect>("CardDragTargetHighlight");
			TowerDefenseInGamePacketShow packetShow = _editor.FindChild("CardPacketShowPreview", recursive: true, owned: false) as TowerDefenseInGamePacketShow;
			SpinBox previewSun = FindControl<SpinBox>("PreviewSunSpinBox");
			bool surface = GodotObject.IsInstanceValid(interaction) && GodotObject.IsInstanceValid(dropSurface) && GodotObject.IsInstanceValid(instance) && GodotObject.IsInstanceValid(packetShow) && GodotObject.IsInstanceValid(previewSun);
			Require(surface, "Card drag source, battle drop surface, highlight, or PacketShow is missing.");
			if (!surface)
			{
				Finish();
				return;
			}
			Node inspectorSentinel = new Node
			{
				Name = "CardDragInspectorSentinel"
			};
			AddChild(inspectorSentinel, forceReadableName: false, InternalMode.Disabled);
			XWInspector inspector = FindNodeOfType<XWInspector>(GetTree().Root);
			XWEditorInterface.Instance.InspectObject(inspectorSentinel);
			await WaitFrames(2);
			GodotObject inspectedBefore = inspector?.CurrentObject;
			int historyVersionBefore = _history.GetVersion();
			int liveCharactersBefore = CountNonPreviewCharacters(GetTree().Root);
			string nameBefore = packet.name;
			string keyBefore = packet.saveKey;
			long costBefore = packet.overrideCost;
			double cooldownBefore = packet.overridePacketCooldown;
			bool flipBefore = packet.packetFlip;
			TowerDefenseCharacterConfig characterBefore = packet.characterConfig;
			Vector2 sourcePoint = ToCanvasPoint(interaction, interaction.Size * 0.5f);
			Vector2 validPoint = ToCanvasPoint(dropSurface, dropSurface.Size * new Vector2(0.5f, 0.55f));
			Vector2 invalidPoint = ToCanvasPoint(dropSurface, dropSurface.Size * new Vector2(0.03f, 0.03f));
			Vector2 outsidePoint = new Vector2(-128f, -128f);
			int placementsBefore = _editor.CardDragPreviewPlacementCount;
			int rejectedBefore = _editor.CardDragRejectedCount;
			int canceledBefore = _editor.CardDragCancelCount;
			BeginMouseDrag(sourcePoint);
			MoveMouseDrag(sourcePoint, sourcePoint + new Vector2(24f, 0f));
			MoveMouseDrag(sourcePoint + new Vector2(24f, 0f), invalidPoint);
			await WaitFrames(3);
			bool redInvalid = _editor.IsCardDragActive && !_editor.IsCardDragTargetValid && _editor.HasCardDragGhost && _editor.CardDragTargetColor.A > 0f && _editor.CardDragTargetColor.R > _editor.CardDragTargetColor.G;
			EndMouseDrag(invalidPoint);
			await WaitFrames(3);
			redInvalid = redInvalid && !_editor.IsCardDragActive && !_editor.HasCardDragGhost && _editor.CardDragRejectedCount == rejectedBefore + 1 && _editor.CardDragPreviewPlacementCount == placementsBefore && !packetShow.coldDownOpen;
			Require(redInvalid, "Invalid in-board cell did not show red feedback and reject placement.");
			BeginMouseDrag(sourcePoint);
			MoveMouseDrag(sourcePoint, sourcePoint + new Vector2(24f, 0f));
			MoveMouseDrag(sourcePoint + new Vector2(24f, 0f), outsidePoint);
			await WaitFrames(2);
			EndMouseDrag(outsidePoint);
			await WaitFrames(3);
			bool outsideCancel = !_editor.IsCardDragActive && !_editor.HasCardDragGhost && _editor.CardDragCancelCount == canceledBefore + 1 && _editor.CardDragPreviewPlacementCount == placementsBefore && !packetShow.coldDownOpen;
			Require(outsideCancel, "Releasing outside the lawn did not cancel without placement/cooldown.");
			BeginMouseDrag(sourcePoint);
			MoveMouseDrag(sourcePoint, sourcePoint + new Vector2(24f, 0f));
			MoveMouseDrag(sourcePoint + new Vector2(24f, 0f), validPoint);
			await WaitFrames(3);
			Vector2I validCell = _editor.CardDragPreviewCell;
			bool greenTarget = _editor.IsCardDragActive && _editor.IsCardDragTargetValid && _editor.HasCardDragGhost && validCell == new Vector2I(5, 3) && _editor.CardDragTargetColor.A > 0f && _editor.CardDragTargetColor.G > _editor.CardDragTargetColor.R;
			EndMouseDrag(validPoint);
			await WaitFrames(4);
			bool validPlacement = greenTarget && !_editor.IsCardDragActive && !_editor.HasCardDragGhost && _editor.CardDragPreviewPlacementCount == placementsBefore + 1 && _editor.CardPlacedPreviewCell == validCell && packetShow.coldDownOpen && packetShow.coldDownTimer > 0.0 && Math.Abs(previewSun.Value - 425.0) < 0.01;
			Require(validPlacement, $"Valid mouse drop did not place the safe preview and enter cooldown: green={greenTarget} cell={validCell} active={_editor.IsCardDragActive} ghost={_editor.HasCardDragGhost} placements={_editor.CardDragPreviewPlacementCount}/{placementsBefore + 1} placed={_editor.CardPlacedPreviewCell} cooldown={packetShow.coldDownOpen}:{packetShow.coldDownTimer:0.###} sun={previewSun.Value:0.###}.");
			int placementsAfterValid = _editor.CardDragPreviewPlacementCount;
			BeginMouseDrag(sourcePoint);
			MoveMouseDrag(sourcePoint, sourcePoint + new Vector2(30f, 0f));
			await WaitFrames(2);
			bool cooldownGate = !_editor.IsCardDragActive && !_editor.HasCardDragGhost && _editor.CardDragPreviewPlacementCount == placementsAfterValid;
			EndMouseDrag(sourcePoint + new Vector2(30f, 0f));
			Require(cooldownGate, "Cooldown state allowed a second drag to start.");
			await ResetRuntime();
			previewSun.SetValueNoSignal(0.0);
			previewSun.EmitSignal(Godot.Range.SignalName.ValueChanged, 0.0);
			await WaitFrames(2);
			BeginMouseDrag(sourcePoint);
			MoveMouseDrag(sourcePoint, sourcePoint + new Vector2(30f, 0f));
			await WaitFrames(2);
			bool costGate = !_editor.IsCardDragActive && !_editor.HasCardDragGhost && _editor.CardDragPreviewPlacementCount == placementsAfterValid;
			EndMouseDrag(sourcePoint + new Vector2(30f, 0f));
			Require(costGate, "Insufficient-sun state allowed card drag to start.");
			previewSun.SetValueNoSignal(500.0);
			previewSun.EmitSignal(Godot.Range.SignalName.ValueChanged, 500.0);
			FindVisualChoiceCard(FindControl<HFlowContainer>("RuntimeStateCards"), "4")?.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(2);
			BeginMouseDrag(sourcePoint);
			MoveMouseDrag(sourcePoint, sourcePoint + new Vector2(30f, 0f));
			await WaitFrames(2);
			bool lockGate = !_editor.IsCardDragActive && !_editor.HasCardDragGhost && _editor.CardDragPreviewPlacementCount == placementsAfterValid;
			EndMouseDrag(sourcePoint + new Vector2(30f, 0f));
			Require(lockGate, "Locked state allowed card drag to start.");
			await ResetRuntime();
			int beforeTouchPlacement = _editor.CardDragPreviewPlacementCount;
			BeginTouchDrag(sourcePoint);
			MoveTouchDrag(sourcePoint + new Vector2(24f, 0f), new Vector2(24f, 0f));
			MoveTouchDrag(validPoint, validPoint - sourcePoint - new Vector2(24f, 0f));
			await WaitFrames(3);
			bool touchActive = _editor.IsCardDragActive && _editor.IsCardDragTargetValid && _editor.HasCardDragGhost;
			EndTouchDrag(validPoint);
			await WaitFrames(4);
			bool touchDrag = touchActive && !_editor.IsCardDragActive && !_editor.HasCardDragGhost && _editor.CardDragPreviewPlacementCount == beforeTouchPlacement + 1 && packetShow.coldDownOpen;
			Require(touchDrag, "Touch press/drag/release did not complete a valid placement.");
			await ResetRuntime();
			BeginMouseDrag(sourcePoint);
			MoveMouseDrag(sourcePoint, sourcePoint + new Vector2(24f, 0f));
			MoveMouseDrag(sourcePoint + new Vector2(24f, 0f), validPoint);
			await WaitFrames(2);
			bool activeBeforeHide = _editor.IsCardDragActive && _editor.HasCardDragGhost;
			_editor.Hide();
			await WaitFrames(6);
			bool hiddenCleanup = activeBeforeHide && !_editor.IsCardDragActive && !_editor.HasCardDragGhost && !_editor.IsProcessing() && !_editor.IsCardPreviewRendering;
			EndMouseDrag(validPoint);
			_editor.Show();
			XWEditorInterface.Instance.FocusPanel("card_editor");
			await WaitFrames(3);
			Require(hiddenCleanup, "Hiding the editor did not cancel drag, remove ghost, and stop preview work.");
			bool flag3 = packet.name == nameBefore && packet.saveKey == keyBefore && packet.overrideCost == costBefore && Math.Abs(packet.overridePacketCooldown - cooldownBefore) < 0.0001 && packet.packetFlip == flipBefore && packet.characterConfig == characterBefore && packet.pressedActions.Count == 1 && packet.useSucceededActions.Count == 1;
			bool flag4 = GodotObject.IsInstanceValid(inspector) && inspector.CurrentObject == inspectedBefore && inspector.CurrentObject == inspectorSentinel;
			bool flag5 = ModEditorCardDragPlacementSideEffectEvent.ExecutionCount == 0 && _history.GetVersion() == historyVersionBefore && CountNonPreviewCharacters(GetTree().Root) == liveCharactersBefore;
			Require(flag3, "Card drag preview mutated the packet resource.");
			Require(flag4, "Card drag preview redirected the raw Inspector.");
			Require(flag5, "Card drag preview executed packet events, changed UndoRedo, or spawned a live battle character.");
			bool value = redInvalid && validPlacement;
			GD.Print($"[MOD_EDITOR_CARD_DRAG_PLACEMENT_PROBE] window={window} surface={surface} mouseDrag={value} redInvalid={redInvalid} outsideCancel={outsideCancel} validPlacement={validPlacement} cooldownGate={cooldownGate} costGate={costGate} lockGate={lockGate} touchDrag={touchDrag} hiddenCleanup={hiddenCleanup} resourceUntouched={flag3} inspectorUntouched={flag4} globalUntouched={flag5} failures={_failures.Count} elapsedMs={_stopwatch.ElapsedMilliseconds}");
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
			if (XWEditorInterface.Instance?.GetResourceEditor("card_editor") is XWCardVisualResourceEditor xWCardVisualResourceEditor && GodotObject.IsInstanceValid(xWCardVisualResourceEditor))
			{
				_editor = xWCardVisualResourceEditor;
				_history = XWEditorInterface.Instance.GetUndoRedoManager();
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

	private async Task ResetRuntime()
	{
		FindControl<Button>("ResetRuntimeButton")?.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(3);
	}

	private void BeginMouseDrag(Vector2 point)
	{
		InputEventMouseButton inputEventMouseButton = new InputEventMouseButton
		{
			Position = point,
			GlobalPosition = point,
			ButtonIndex = MouseButton.Left,
			ButtonMask = MouseButtonMask.Left,
			Pressed = true
		};
		_editor.GetViewport().PushInput(inputEventMouseButton, inLocalCoords: true);
	}

	private void MoveMouseDrag(Vector2 previous, Vector2 point)
	{
		InputEventMouseMotion inputEventMouseMotion = new InputEventMouseMotion
		{
			Position = point,
			GlobalPosition = point,
			Relative = point - previous,
			ButtonMask = MouseButtonMask.Left
		};
		_editor.GetViewport().PushInput(inputEventMouseMotion, inLocalCoords: true);
	}

	private void EndMouseDrag(Vector2 point)
	{
		InputEventMouseButton inputEventMouseButton = new InputEventMouseButton
		{
			Position = point,
			GlobalPosition = point,
			ButtonIndex = MouseButton.Left,
			ButtonMask = (MouseButtonMask)0L,
			Pressed = false
		};
		_editor.GetViewport().PushInput(inputEventMouseButton, inLocalCoords: true);
	}

	private void BeginTouchDrag(Vector2 point)
	{
		InputEventScreenTouch inputEventScreenTouch = new InputEventScreenTouch
		{
			Index = 0,
			Position = point,
			Pressed = true
		};
		_editor.GetViewport().PushInput(inputEventScreenTouch, inLocalCoords: true);
	}

	private void MoveTouchDrag(Vector2 point, Vector2 relative)
	{
		InputEventScreenDrag inputEventScreenDrag = new InputEventScreenDrag
		{
			Index = 0,
			Position = point,
			Relative = relative
		};
		_editor.GetViewport().PushInput(inputEventScreenDrag, inLocalCoords: true);
	}

	private void EndTouchDrag(Vector2 point)
	{
		InputEventScreenTouch inputEventScreenTouch = new InputEventScreenTouch
		{
			Index = 0,
			Position = point,
			Pressed = false
		};
		_editor.GetViewport().PushInput(inputEventScreenTouch, inLocalCoords: true);
	}

	private T FindControl<T>(string name) where T : Node
	{
		return _editor?.FindChild(name, recursive: true, owned: false) as T;
	}

	private static Vector2 ToCanvasPoint(Control control, Vector2 localPoint)
	{
		return control.GetGlobalTransformWithCanvas() * localPoint;
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

	private static int CountNonPreviewCharacters(Node root)
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return 0;
		}
		int num = ((root is TowerDefenseCharacter { editorPreviewMode: false }) ? 1 : 0);
		foreach (Node child in root.GetChildren())
		{
			num += CountNonPreviewCharacters(child);
		}
		return num;
	}

	private static T FindNodeOfType<T>(Node root) where T : Node
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return null;
		}
		if (root is T result)
		{
			return result;
		}
		foreach (Node child in root.GetChildren())
		{
			T val = FindNodeOfType<T>(child);
			if (GodotObject.IsInstanceValid(val))
			{
				return val;
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
			GD.PrintErr("[MOD_EDITOR_CARD_DRAG_PLACEMENT_PROBE_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_CARD_DRAG_PLACEMENT_PROBE_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
