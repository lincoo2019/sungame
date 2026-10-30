using System.Threading.Tasks;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseBattleFeatureGlove : TowerDefenseBattleFeature
{
	private static PackedScene _gloveManager;

	public GloveManager gloveManager;

	public GlovePickTool glovePickTool;

	public bool glovePick;

	public TowerDefenseCharacter pickedCharacter;

	public Vector2I sourceGridPos = Vector2I.Zero;

	public TowerDefenseCellInstance sourceCell;

	private TowerDefenseBattleFeatureMap _mapFeature;

	private static PackedScene GLOVE_MANAGER => _gloveManager ?? (_gloveManager = GD.Load<PackedScene>("uid://d2i0ohax0btl4"));

	public override void Init(Dictionary _data)
	{
		base.Init(_data);
		gloveManager = GLOVE_MANAGER.Instantiate<GloveManager>(PackedScene.GenEditState.Disabled);
		control.AddUIToTopPropContainer(gloveManager);
	}

	public override Task GameInit()
	{
		InitializeManager();
		return Task.CompletedTask;
	}

	public override Task GameInitFromProgress()
	{
		InitializeManager();
		return Task.CompletedTask;
	}

	public override Task GameStart()
	{
		if (!GodotObject.IsInstanceValid(gloveManager))
		{
			return Task.CompletedTask;
		}
		if (GodotObject.IsInstanceValid(_mapFeature) && GodotObject.IsInstanceValid(_mapFeature.packetPickControl) && !GodotObject.IsInstanceValid(glovePickTool))
		{
			glovePickTool = new GlovePickTool();
			glovePickTool.Init(_mapFeature.mapControl);
			glovePickTool.SetGloveFeature(this);
			_mapFeature.packetPickControl.RegisterTool(glovePickTool);
		}
		return Task.CompletedTask;
	}

	private void InitializeManager()
	{
		if (GodotObject.IsInstanceValid(gloveManager))
		{
			_mapFeature = GetFeature<TowerDefenseBattleFeatureMap>("Map");
			if (GodotObject.IsInstanceValid(_mapFeature) && GodotObject.IsInstanceValid(_mapFeature.mapControl))
			{
				_mapFeature.gloveManager = gloveManager;
				gloveManager.Init(_mapFeature.mapControl, _mapFeature);
			}
		}
	}

	public override void Process(double _delta)
	{
		if (!GodotObject.IsInstanceValid(gloveManager))
		{
			return;
		}
		if (glovePick)
		{
			TowerDefenseBattleFeatureMap mapFeature = TowerDefenseManager.GetMapFeature();
			if (GodotObject.IsInstanceValid(mapFeature) && GodotObject.IsInstanceValid(mapFeature.mapControl))
			{
				gloveManager.UpdateGloveSprite(mapFeature.mapControl);
			}
		}
		if (!GodotObject.IsInstanceValid(pickedCharacter) || !GodotObject.IsInstanceValid(gloveManager.previewSprite))
		{
			return;
		}
		gloveManager.previewSprite.Visible = true;
		TowerDefenseBattleFeatureMap mapFeature2 = TowerDefenseManager.GetMapFeature();
		if (GodotObject.IsInstanceValid(mapFeature2) && GodotObject.IsInstanceValid(mapFeature2.mapControl))
		{
			Vector2 globalMousePosition = mapFeature2.mapControl.GetGlobalMousePosition();
			Vector2I mapGridPosFromMouse = TowerDefenseManager.Instance.GetMapGridPosFromMouse(globalMousePosition);
			TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(mapGridPosFromMouse);
			bool flag = false;
			if (mapGridPosFromMouse != sourceGridPos && TowerDefenseManager.Instance.CheckMapGridPosIn(mapGridPosFromMouse) && GodotObject.IsInstanceValid(mapCell) && GodotObject.IsInstanceValid(pickedCharacter) && GodotObject.IsInstanceValid(pickedCharacter.packet))
			{
				flag = mapCell.CanPacketPlant(pickedCharacter.packet);
			}
			if (flag)
			{
				Vector2 mapCellPlantPos = TowerDefenseManager.GetMapCellPlantPos(mapGridPosFromMouse);
				gloveManager.previewSprite.GlobalPosition = mapCellPlantPos;
			}
			else
			{
				gloveManager.previewSprite.Position = mapFeature2.mapControl.spriteNode.GetLocalMousePosition() - new Vector2(0f, 20f);
			}
		}
	}

	public override void Destroy()
	{
		if (GodotObject.IsInstanceValid(_mapFeature) && GodotObject.IsInstanceValid(_mapFeature.packetPickControl) && GodotObject.IsInstanceValid(glovePickTool))
		{
			_mapFeature.packetPickControl.UnregisterTool(glovePickTool);
		}
		if (GodotObject.IsInstanceValid(glovePickTool))
		{
			glovePickTool.SetGloveFeature(null);
			glovePickTool.Free();
		}
		glovePickTool = null;
		if (GodotObject.IsInstanceValid(_mapFeature) && _mapFeature.gloveManager == gloveManager)
		{
			_mapFeature.gloveManager = null;
		}
		ClearPickedState();
		if (GodotObject.IsInstanceValid(gloveManager))
		{
			gloveManager.glovePressedAwait = false;
			gloveManager.QueueFree();
		}
		gloveManager = null;
		_mapFeature = null;
		base.Destroy();
	}

	public void PickGlove(bool open)
	{
		TowerDefenseBattleFeatureMap mapFeature = TowerDefenseManager.GetMapFeature();
		if (GodotObject.IsInstanceValid(mapFeature) && GodotObject.IsInstanceValid(mapFeature.packetPickControl))
		{
			mapFeature.packetPickControl.PacketPickRelease();
		}
		if (open)
		{
			AudioManager.Instance.AudioPlay("Shovel");
			glovePick = true;
			gloveManager.ShowMapGloveSprite(show: true);
			gloveManager.SetMapGloveSpritePos(new Vector2(-100f, -100f));
		}
		else
		{
			AudioManager.Instance.AudioPlay("ShovelDeny");
			glovePick = false;
			gloveManager.ShowMapGloveSprite(show: false);
			ClearPickedState();
		}
	}

	public void ProcessGlovePick(TowerDefenseCellInstance cell, Vector2I gridPos, Vector2 mousePos)
	{
		if (!GodotObject.IsInstanceValid(pickedCharacter))
		{
			ProcessGloveSelect(cell, gridPos, mousePos);
		}
		else
		{
			ProcessGlovePlace(cell, gridPos, mousePos);
		}
	}

	public void ProcessGloveSelect(TowerDefenseCellInstance cell, Vector2I gridPos, Vector2 mousePos)
	{
		if (TowerDefenseManager.Instance.CheckMapGridPosIn(gridPos) && GodotObject.IsInstanceValid(cell))
		{
			double groundHeight = TowerDefenseManager.GetMapFeature().GetGroundHeight(cell);
			Vector2 mapCellPos = TowerDefenseManager.Instance.GetMapCellPos(gridPos);
			Vector2 mapGridSize = TowerDefenseManager.Instance.GetMapGridSize();
			double percentage = (mousePos - mapCellPos + new Vector2(0f, (float)groundHeight)).Y / mapGridSize.Y;
			TowerDefenseCharacter shovelCharacter = cell.GetShovelCharacter(percentage);
			shovelCharacter?.Bright();
			if (cell.CanShovel(percentage) && shovelCharacter is TowerDefensePlant && GodotObject.IsInstanceValid(gloveManager) && gloveManager.mapControl.IsConfirmInput())
			{
				SelectCharacter(shovelCharacter, cell, gridPos);
			}
		}
	}

	public void ProcessGlovePlace(TowerDefenseCellInstance cell, Vector2I gridPos, Vector2 mousePos)
	{
		if (gridPos == sourceGridPos || !TowerDefenseManager.Instance.CheckMapGridPosIn(gridPos) || !GodotObject.IsInstanceValid(cell))
		{
			return;
		}
		bool flag = false;
		if (GodotObject.IsInstanceValid(pickedCharacter) && GodotObject.IsInstanceValid(pickedCharacter.packet))
		{
			flag = cell.CanPacketPlant(pickedCharacter.packet);
		}
		if (flag)
		{
			Vector2 mapCellPlantPos = TowerDefenseManager.GetMapCellPlantPos(gridPos);
			if (GodotObject.IsInstanceValid(gloveManager) && GodotObject.IsInstanceValid(gloveManager.previewSprite))
			{
				gloveManager.previewSprite.GlobalPosition = mapCellPlantPos;
			}
			if (GodotObject.IsInstanceValid(gloveManager) && gloveManager.mapControl.IsConfirmInput())
			{
				PlaceCharacter(cell, gridPos);
			}
		}
	}

	public void SelectCharacter(TowerDefenseCharacter character, TowerDefenseCellInstance cell, Vector2I gridPos)
	{
		pickedCharacter = character;
		sourceGridPos = gridPos;
		sourceCell = cell;
		AudioManager.Instance.AudioPlay("ShovelDig");
		gloveManager.CreatePreviewSprite(character);
	}

	public void PlaceCharacter(TowerDefenseCellInstance targetCell, Vector2I targetGridPos)
	{
		if (!GodotObject.IsInstanceValid(pickedCharacter) || !GodotObject.IsInstanceValid(sourceCell) || !GodotObject.IsInstanceValid(targetCell) || targetGridPos == sourceGridPos)
		{
			ClearPickedState();
			return;
		}
		TowerDefenseCharacter towerDefenseCharacter = pickedCharacter;
		Vector2I fromGridPos = sourceGridPos;
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			if (towerDefenseCharacter.syncId >= 0)
			{
				MultiPlayerManager.Instance?.SendMoveCharacter(towerDefenseCharacter.syncId, fromGridPos, targetGridPos, "glove");
			}
			FinishGlovePlacement();
			return;
		}
		towerDefenseCharacter.EmitDestroy();
		sourceCell.MoveCharacterToCell(towerDefenseCharacter, targetCell);
		if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost && towerDefenseCharacter.syncId >= 0)
		{
			MultiPlayerManager.Instance?.SendMoveCharacter(towerDefenseCharacter.syncId, fromGridPos, targetGridPos, "glove");
		}
		AudioManager.Instance.AudioPlay("ShovelDig");
		FinishGlovePlacement();
	}

	private void FinishGlovePlacement()
	{
		ClearPickedState();
		TowerDefenseBattleFeatureMap mapFeature = TowerDefenseManager.GetMapFeature();
		if (GodotObject.IsInstanceValid(mapFeature) && GodotObject.IsInstanceValid(mapFeature.packetPickControl))
		{
			mapFeature.packetPickControl.Release();
		}
	}

	public void ClearPickedState()
	{
		pickedCharacter = null;
		sourceCell = null;
		sourceGridPos = Vector2I.Zero;
		if (GodotObject.IsInstanceValid(gloveManager))
		{
			gloveManager.FreePreviewSprite();
		}
	}

	public void GloveRelease()
	{
		if (glovePick)
		{
			AudioManager.Instance.AudioPlay("ShovelDeny");
		}
		ClearPickedState();
		glovePick = false;
		gloveManager.ShowMapGloveSprite(show: false);
		gloveManager.SetGloveButtonPressed(pressed: false);
	}

	public void GloveReset()
	{
		ClearPickedState();
		glovePick = false;
		gloveManager.ShowMapGloveSprite(show: false);
		gloveManager.SetGloveButtonPressed(pressed: false);
	}

	public void GloveButtonPressed()
	{
		RunLifetimeTask(GloveButtonPressedAsync, "GloveButtonPressed");
	}

	private async Task GloveButtonPressedAsync()
	{
		if (!base.IsLifetimeActive || !GodotObject.IsInstanceValid(gloveManager))
		{
			return;
		}
		if (!gloveManager.gloveShow && (!GodotObject.IsInstanceValid(TowerDefenseManager.CurrentControl) || !TowerDefenseManager.CurrentControl.isGameRunning))
		{
			gloveManager.SetGloveButtonPressed(pressed: false);
		}
		else if (!gloveManager.glovePressedAwait)
		{
			ClearPickedState();
			PickGlove(gloveManager.IsGloveButtonPressed());
			gloveManager.glovePressedAwait = true;
			await ToSignal(control.GetTree().CreateTimer(0.1, processAlways: false), SceneTreeTimer.SignalName.Timeout);
			if (base.IsLifetimeActive && GodotObject.IsInstanceValid(gloveManager))
			{
				gloveManager.glovePressedAwait = false;
			}
		}
	}
}
