using System;
using Godot;

public sealed class DragMoveComponent : CharacterComponentRuntime
{
	public bool allowMouseDrag = true;

	public bool allowDirectionalInput = true;

	public StringName pressAction = "Press";

	public StringName moveUpAction = "P1Up";

	public StringName moveDownAction = "P1Down";

	public StringName moveLeftAction = "P1Left";

	public StringName moveRightAction = "P1Right";

	public int requestRetryPhysicsFrames = 30;

	public TowerDefenseCharacter parent;

	public static TowerDefenseCharacter CurrentMoveCharacter;

	public bool drag;

	private static ulong _cachedInputEventId;

	private static Vector2I _cachedInputGridPosition;

	private static bool _cachedInputGridInMap;

	private bool _moveRequestPending;

	private Vector2I _requestedFromGridPosition;

	private ulong _moveRequestFrame;

	private int _capturedTouchIndex = -1;

	private bool _configured;

	protected override bool AllowInputOutsideComponentBattlefield => true;

	internal override bool WantsInput => true;

	private DragMoveComponentDefinition Definition => base.ComponentDefinition as DragMoveComponentDefinition;

	protected override void OnBound()
	{
		parent = base.Owner;
		if (!_configured && Definition != null)
		{
			DragMoveComponentDefinition definition = Definition;
			allowMouseDrag = definition.allowMouseDrag;
			allowDirectionalInput = definition.allowDirectionalInput;
			pressAction = definition.pressAction;
			moveUpAction = definition.moveUpAction;
			moveDownAction = definition.moveDownAction;
			moveLeftAction = definition.moveLeftAction;
			moveRightAction = definition.moveRightAction;
			requestRetryPhysicsFrames = Math.Max(1, definition.requestRetryPhysicsFrames);
			_configured = true;
		}
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		ClearInteraction();
		parent = null;
	}

	protected override void OnReleased()
	{
		ClearInteraction();
		parent = null;
	}

	protected override void OnAliveChanged(bool alive)
	{
		if (!alive)
		{
			ClearInteraction();
		}
	}

	internal static void ClearCurrentMoveCharacter()
	{
		CurrentMoveCharacter = null;
		_cachedInputEventId = 0uL;
	}

	internal override void ProcessInput(InputEvent inputEvent)
	{
		if (!CanReceiveInput(inputEvent) || ShouldIgnoreTouchEvent(inputEvent))
		{
			return;
		}
		RefreshPendingRequest();
		ResolveInputGrid(inputEvent, out var gridPos, out var gridPosInMap);
		if (allowMouseDrag && IsPointerPress(inputEvent))
		{
			if (gridPosInMap && ContainsGridPosition(gridPos))
			{
				if (inputEvent is InputEventScreenTouch inputEventScreenTouch)
				{
					_capturedTouchIndex = inputEventScreenTouch.Index;
				}
				CurrentMoveCharacter = parent;
				drag = true;
			}
			else if (CurrentMoveCharacter == parent)
			{
				ClearInteraction();
			}
		}
		else if (allowMouseDrag && IsPointerRelease(inputEvent))
		{
			drag = false;
			_capturedTouchIndex = -1;
		}
		else
		{
			if (CurrentMoveCharacter != parent)
			{
				return;
			}
			if (allowMouseDrag && drag && gridPosInMap && gridPos != parent.gridPos)
			{
				TryMoveTo(gridPos);
			}
			if (allowDirectionalInput)
			{
				Vector2I vector2I = ResolveDirectionalInput(inputEvent);
				if (vector2I != Vector2I.Zero)
				{
					TryMoveTo(parent.gridPos + vector2I);
				}
			}
		}
	}

	private bool CanReceiveInput(InputEvent inputEvent)
	{
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && inputEvent != null && !inputEvent.IsEcho() && GodotObject.IsInstanceValid(parent) && parent.IsInsideTree() && parent.inGame && !parent.die && GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			return TowerDefenseManager.Instance.IsGameRunning();
		}
		return false;
	}

	private bool ShouldIgnoreTouchEvent(InputEvent inputEvent)
	{
		if (inputEvent is InputEventScreenDrag inputEventScreenDrag)
		{
			if (_capturedTouchIndex >= 0)
			{
				return inputEventScreenDrag.Index != _capturedTouchIndex;
			}
			return true;
		}
		if (!(inputEvent is InputEventScreenTouch inputEventScreenTouch))
		{
			return false;
		}
		if (inputEventScreenTouch.Pressed)
		{
			if (_capturedTouchIndex >= 0)
			{
				return inputEventScreenTouch.Index != _capturedTouchIndex;
			}
			return false;
		}
		if (_capturedTouchIndex >= 0)
		{
			return inputEventScreenTouch.Index != _capturedTouchIndex;
		}
		return true;
	}

	private bool IsPointerPress(InputEvent inputEvent)
	{
		if (pressAction.IsEmpty || !inputEvent.IsActionPressed(pressAction))
		{
			if (inputEvent is InputEventScreenTouch inputEventScreenTouch)
			{
				return inputEventScreenTouch.Pressed;
			}
			return false;
		}
		return true;
	}

	private bool IsPointerRelease(InputEvent inputEvent)
	{
		if (pressAction.IsEmpty || !inputEvent.IsActionReleased(pressAction))
		{
			if (inputEvent is InputEventScreenTouch inputEventScreenTouch)
			{
				return !inputEventScreenTouch.Pressed;
			}
			return false;
		}
		return true;
	}

	private bool ContainsGridPosition(Vector2I gridPos)
	{
		if (gridPos == parent.gridPos)
		{
			return true;
		}
		if (!(parent.config is TowerDefensePlantConfig towerDefensePlantConfig))
		{
			return false;
		}
		foreach (Vector2I item in towerDefensePlantConfig.extendGrid)
		{
			if (gridPos - item == parent.gridPos)
			{
				return true;
			}
		}
		return false;
	}

	private Vector2I ResolveDirectionalInput(InputEvent inputEvent)
	{
		if (!moveUpAction.IsEmpty && inputEvent.IsActionPressed(moveUpAction))
		{
			return Vector2I.Up;
		}
		if (!moveDownAction.IsEmpty && inputEvent.IsActionPressed(moveDownAction))
		{
			return Vector2I.Down;
		}
		if (!moveLeftAction.IsEmpty && inputEvent.IsActionPressed(moveLeftAction))
		{
			return Vector2I.Left;
		}
		if (!moveRightAction.IsEmpty && inputEvent.IsActionPressed(moveRightAction))
		{
			return Vector2I.Right;
		}
		return Vector2I.Zero;
	}

	private void ResolveInputGrid(InputEvent inputEvent, out Vector2I gridPos, out bool gridPosInMap)
	{
		ulong instanceId = inputEvent.GetInstanceId();
		if (_cachedInputEventId != instanceId)
		{
			_cachedInputEventId = instanceId;
			Vector2 pos = ResolvePointerCanvasPosition(inputEvent);
			_cachedInputGridPosition = TowerDefenseManager.Instance.GetMapGridPosFromMouse(pos);
			_cachedInputGridInMap = TowerDefenseManager.Instance.CheckMapGridPosIn(_cachedInputGridPosition);
		}
		gridPos = _cachedInputGridPosition;
		gridPosInMap = _cachedInputGridInMap;
	}

	private Vector2 ResolvePointerCanvasPosition(InputEvent inputEvent)
	{
		bool flag = true;
		Vector2 vector = ((inputEvent is InputEventScreenTouch inputEventScreenTouch) ? inputEventScreenTouch.Position : ((inputEvent is InputEventScreenDrag inputEventScreenDrag) ? inputEventScreenDrag.Position : ((!(inputEvent is InputEventMouse inputEventMouse)) ? default(Vector2) : inputEventMouse.Position)));
		Vector2 vector2 = vector;
		if (!(inputEvent is InputEventScreenTouch) && !(inputEvent is InputEventScreenDrag) && !(inputEvent is InputEventMouse))
		{
			flag = false;
		}
		TowerDefenseBattleFeatureMap mapFeature = TowerDefenseManager.GetMapFeature();
		CanvasItem canvasItem = (GodotObject.IsInstanceValid(mapFeature?.mapControl) ? ((Node2D)mapFeature.mapControl) : ((Node2D)parent));
		if (flag && GodotObject.IsInstanceValid(canvasItem))
		{
			return canvasItem.GetCanvasTransform().AffineInverse() * vector2;
		}
		if (!GodotObject.IsInstanceValid(mapFeature?.mapControl))
		{
			return parent.GetGlobalMousePosition();
		}
		return mapFeature.mapControl.GetGlobalMousePosition();
	}

	private void RefreshPendingRequest()
	{
		if (_moveRequestPending)
		{
			ulong num = (ulong)Math.Max(1, requestRetryPhysicsFrames);
			if (parent.gridPos != _requestedFromGridPosition || Engine.GetPhysicsFrames() - _moveRequestFrame >= num)
			{
				_moveRequestPending = false;
			}
		}
	}

	private bool TryMoveTo(Vector2I targetGridPosition)
	{
		if (_moveRequestPending || targetGridPosition == parent.gridPos || !TowerDefenseManager.Instance.CheckMapGridPosIn(targetGridPosition))
		{
			return false;
		}
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(parent.gridPos);
		TowerDefenseCellInstance mapCell2 = TowerDefenseManager.GetMapCell(targetGridPosition);
		if (!GodotObject.IsInstanceValid(mapCell) || !GodotObject.IsInstanceValid(mapCell2) || !mapCell2.CanMoveCharacterHere(parent))
		{
			return false;
		}
		Vector2I gridPos = parent.gridPos;
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			if (parent.syncId < 0 || !GodotObject.IsInstanceValid(MultiPlayerManager.Instance))
			{
				return false;
			}
			_moveRequestPending = true;
			_requestedFromGridPosition = gridPos;
			_moveRequestFrame = Engine.GetPhysicsFrames();
			MultiPlayerManager.Instance.SendMoveCharacter(parent.syncId, gridPos, targetGridPosition);
			return true;
		}
		parent.EmitDestroy();
		mapCell.MoveCharacterToCell(parent, mapCell2);
		if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost && parent.syncId >= 0)
		{
			MultiPlayerManager.Instance?.SendMoveCharacter(parent.syncId, gridPos, targetGridPosition);
		}
		return true;
	}

	private void ClearInteraction()
	{
		if (CurrentMoveCharacter == parent)
		{
			CurrentMoveCharacter = null;
		}
		drag = false;
		_moveRequestPending = false;
		_capturedTouchIndex = -1;
	}
}
