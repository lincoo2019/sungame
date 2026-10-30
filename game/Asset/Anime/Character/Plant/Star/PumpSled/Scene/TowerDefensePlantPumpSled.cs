using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantPumpSled : TowerDefensePlant
{
	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> allEventList = new Array<TowerDefenseCharacterEventBase>();

	public TowerDefenseCharacter carrayCharacter1;

	public TowerDefenseCharacter carrayCharacter2;

	public Array<TowerDefenseCharacter> hitCharacterList = new Array<TowerDefenseCharacter>();

	public bool run;

	public double speed = 200.0;

	private string _pendingCarrayCharacter1Name = "";

	private string _pendingCarrayCharacter2Name = "";

	private Array _pendingHitCharacterListNames = new Array();

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !inGame && !editorMapPreviewMode)
		{
			((PumpSledSprite)base.sprite).Back.ZIndex = 0;
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (_pendingCarrayCharacter1Name != "")
		{
			Node2D node2D = TowerDefenseManager.GetCharacterNode();
			if (GodotObject.IsInstanceValid(node2D))
			{
				Node nodeOrNull = node2D.GetNodeOrNull(_pendingCarrayCharacter1Name);
				if (GodotObject.IsInstanceValid(nodeOrNull))
				{
					carrayCharacter1 = (TowerDefenseCharacter)nodeOrNull;
				}
			}
			_pendingCarrayCharacter1Name = "";
		}
		if (_pendingCarrayCharacter2Name != "")
		{
			Node2D node2D2 = TowerDefenseManager.GetCharacterNode();
			if (GodotObject.IsInstanceValid(node2D2))
			{
				Node nodeOrNull2 = node2D2.GetNodeOrNull(_pendingCarrayCharacter2Name);
				if (GodotObject.IsInstanceValid(nodeOrNull2))
				{
					carrayCharacter2 = (TowerDefenseCharacter)nodeOrNull2;
				}
			}
			_pendingCarrayCharacter2Name = "";
		}
		if (_pendingHitCharacterListNames.Count > 0)
		{
			Node2D node2D3 = TowerDefenseManager.GetCharacterNode();
			if (GodotObject.IsInstanceValid(node2D3))
			{
				foreach (Variant pendingHitCharacterListName in _pendingHitCharacterListNames)
				{
					string text = (string)pendingHitCharacterListName;
					Node nodeOrNull3 = node2D3.GetNodeOrNull(text);
					if (GodotObject.IsInstanceValid(nodeOrNull3))
					{
						hitCharacterList.Add((TowerDefenseCharacter)nodeOrNull3);
					}
				}
			}
			_pendingHitCharacterListNames = new Array();
		}
		ProcessAabbAttacks();
	}

	public override void IdleEntered()
	{
		base.IdleEntered();
		if (!run)
		{
			PuzzleShaderComponent obj = puzzleShaderComponent;
			if (obj != null && !obj.IsReleased)
			{
				puzzleShaderComponent.IdleEntered();
			}
		}
	}

	public override void IdleExited()
	{
		base.IdleExited();
		if (!run)
		{
			PuzzleShaderComponent obj = puzzleShaderComponent;
			if (obj != null && !obj.IsReleased)
			{
				puzzleShaderComponent.IdleExited();
			}
		}
	}

	public override void SleepProcessing(double delta)
	{
		base.SleepProcessing(delta);
		if (!run)
		{
			if (CheckCanRun())
			{
				RunStart();
			}
		}
		else
		{
			RunMovement(delta);
		}
	}

	public override void IdleProcessing(double delta)
	{
		base.IdleProcessing(delta);
		if (!run)
		{
			if (CheckCanRun())
			{
				RunStart();
			}
		}
		else
		{
			RunMovement(delta);
		}
	}

	public void RunMovement(double delta)
	{
		if (base.sprite.timeScale == 0.0)
		{
			base.sprite.timeScale = timeScaleSave;
		}
		ulong currentPhysicsFrame = TowerDefenseProcessModeDispatch.CurrentPhysicsFrame;
		Vector2 globalPositionForPhysicsFrame = GetGlobalPositionForPhysicsFrame(currentPhysicsFrame);
		globalPositionForPhysicsFrame.X += (float)(speed * delta * base.sprite.timeScale * (double)transformPoint.Scale.X * (double)base.Scale.X);
		SetGlobalPositionForPhysicsFrame(globalPositionForPhysicsFrame, currentPhysicsFrame);
		if (GodotObject.IsInstanceValid(carrayCharacter1))
		{
			Vector2 logicalGlobalPosition = carrayCharacter1.GetLogicalGlobalPosition();
			logicalGlobalPosition.X = globalPositionForPhysicsFrame.X;
			carrayCharacter1.SetLogicalGlobalPosition(logicalGlobalPosition);
		}
		if (GodotObject.IsInstanceValid(carrayCharacter2))
		{
			Vector2 logicalGlobalPosition2 = carrayCharacter2.GetLogicalGlobalPosition();
			logicalGlobalPosition2.X = globalPositionForPhysicsFrame.X + 90f;
			carrayCharacter2.SetLogicalGlobalPosition(logicalGlobalPosition2);
		}
		if ((double)globalPositionForPhysicsFrame.X > TowerDefenseManager.Instance.GetMapGroundRight() + 200.0 || (double)globalPositionForPhysicsFrame.X < TowerDefenseManager.Instance.GetMapGroundLeft() - 200.0)
		{
			QueueFree();
			if (GodotObject.IsInstanceValid(carrayCharacter1))
			{
				carrayCharacter1.QueueFree();
			}
			if (GodotObject.IsInstanceValid(carrayCharacter2))
			{
				carrayCharacter2.QueueFree();
			}
		}
		if (Engine.GetPhysicsFrames() % 2 == 0L)
		{
			TowerDefenseExplode.CreateExplode(globalPositionForPhysicsFrame + new Vector2(46f, 0f), new Vector2(0.75f, 0.25f), allEventList, new Array<TowerDefenseCharacter>(), TowerDefenseEnum.CHARACTER_CAMP.ALL, -1);
		}
	}

	public void RunStart()
	{
		run = true;
		if (TowerDefenseManager.Instance.IsIZMMode())
		{
			timeScaleInit = timeScaleSave;
			timeScale = timeScaleSave;
			base.sprite.timeScale = timeScaleSave;
			Idle();
		}
		EmitDestroy();
		if (GodotObject.IsInstanceValid(carrayCharacter1))
		{
			carrayCharacter1.Destroy(freeInstance: false);
		}
		if (GodotObject.IsInstanceValid(carrayCharacter2))
		{
			carrayCharacter2.Destroy(freeInstance: false);
		}
	}

	public bool CheckCanRun()
	{
		if (CheckIceCap())
		{
			return true;
		}
		if (CheckSameCharacter())
		{
			return true;
		}
		return false;
	}

	public bool CheckIceCap()
	{
		TowerDefenseIceCap towerDefenseIceCap = TowerDefenseManager.Instance.GetMapIceCapList()[base.gridPos.Y].As<TowerDefenseIceCap>();
		if (GodotObject.IsInstanceValid(towerDefenseIceCap) && TowerDefenseManager.Instance.GetMapGridPos(towerDefenseIceCap.iceCapSprite.GlobalPosition).X <= base.gridPos.X + 1)
		{
			return true;
		}
		return false;
	}

	public bool CheckSameCharacter()
	{
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(base.gridPos);
		TowerDefenseCellInstance mapCell2 = TowerDefenseManager.GetMapCell(base.gridPos + new Vector2I(1, 0));
		Array<TowerDefenseCharacter> array = new Array<TowerDefenseCharacter>();
		foreach (TowerDefenseCharacter character in mapCell.GetCharacterList())
		{
			if (character is TowerDefensePlant && !character.isDestroy && (character.config.physiqueTypeFlags & 2) == 0 && (character.config.physiqueTypeFlags & 4) == 0 && character != this && !character.config.plantGridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.AIR) && CanCarryCharacterWithinSledCells(character))
			{
				array.Add(character);
			}
		}
		if (GodotObject.IsInstanceValid(mapCell2))
		{
			foreach (TowerDefenseCharacter character2 in mapCell2.GetCharacterList())
			{
				if (character2 is TowerDefensePlant && !character2.isDestroy && (character2.config.physiqueTypeFlags & 2) == 0 && (character2.config.physiqueTypeFlags & 4) == 0 && character2 != this && !character2.config.plantGridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.AIR) && CanCarryCharacterWithinSledCells(character2))
				{
					array.Add(character2);
				}
			}
		}
		for (int i = 0; i < array.Count; i++)
		{
			for (int j = i + 1; j < array.Count; j++)
			{
				if (array[i] != array[j] && array[i].config.name == array[j].config.name)
				{
					carrayCharacter1 = array[i];
					carrayCharacter2 = array[j];
					return true;
				}
			}
		}
		return false;
	}

	private bool CanCarryCharacterWithinSledCells(TowerDefenseCharacter characterCheck)
	{
		if (!IsCarryCell(characterCheck.gridPos))
		{
			return false;
		}
		if (!(characterCheck.config is TowerDefensePlantConfig towerDefensePlantConfig))
		{
			return true;
		}
		foreach (Vector2I item in towerDefensePlantConfig.extendGrid)
		{
			Vector2I vector2I = characterCheck.gridPos + item;
			if (vector2I != base.gridPos && vector2I != base.gridPos + new Vector2I(1, 0))
			{
				return false;
			}
		}
		return true;
	}

	private bool IsCarryCell(Vector2I cell)
	{
		if (!(cell == base.gridPos))
		{
			return cell == base.gridPos + new Vector2I(1, 0);
		}
		return true;
	}

	private void ProcessAabbAttacks()
	{
		if (run && TryGetActiveWorldHitRect(out var rect))
		{
			List<TowerDefenseCharacter> charactersIntersectingRectListExcludingCamp = TowerDefenseManager.Instance.characterRegistry.GetCharactersIntersectingRectListExcludingCamp(rect, base.camp);
			for (int i = 0; i < charactersIntersectingRectListExcludingCamp.Count; i++)
			{
				CheckAttack(charactersIntersectingRectListExcludingCamp[i]);
			}
		}
	}

	public void CheckAttack(TowerDefenseCharacter tdCharacter)
	{
		if (run && GodotObject.IsInstanceValid(tdCharacter) && tdCharacter != this && !hitCharacterList.Contains(tdCharacter) && CanCollision(tdCharacter.instance.maskFlags) && CanTarget(tdCharacter) && tdCharacter.instance.canBeCollection)
		{
			double num = 1800.0;
			if (GodotObject.IsInstanceValid(carrayCharacter1))
			{
				num += carrayCharacter1.GetCurrentHitPoint();
			}
			if (GodotObject.IsInstanceValid(carrayCharacter2))
			{
				num += carrayCharacter2.GetCurrentHitPoint();
			}
			tdCharacter.Hurt(num);
			hitCharacterList.Add(tdCharacter);
		}
	}

	public override Dictionary ExportVariantSave()
	{
		Dictionary dictionary = new Dictionary
		{
			{ "run", run },
			{ "speed", speed }
		};
		if (GodotObject.IsInstanceValid(carrayCharacter1))
		{
			dictionary["carrayCharacter1NodeName"] = carrayCharacter1.Name;
		}
		if (GodotObject.IsInstanceValid(carrayCharacter2))
		{
			dictionary["carrayCharacter2NodeName"] = carrayCharacter2.Name;
		}
		if (hitCharacterList.Count > 0)
		{
			Array array = new Array();
			foreach (TowerDefenseCharacter hitCharacter in hitCharacterList)
			{
				if (GodotObject.IsInstanceValid(hitCharacter))
				{
					array.Add(hitCharacter.Name);
				}
			}
			dictionary["hitCharacterNodeNames"] = array;
		}
		return dictionary;
	}

	public override void ImportVariantSave(Dictionary data)
	{
		run = data.GetValueOrDefault("run", Variant.From<bool>(false)).AsBool();
		speed = data.GetValueOrDefault("speed", Variant.From<double>(200.0)).AsDouble();
		if (data.ContainsKey("carrayCharacter1NodeName"))
		{
			_pendingCarrayCharacter1Name = data["carrayCharacter1NodeName"].AsString();
		}
		if (data.ContainsKey("carrayCharacter2NodeName"))
		{
			_pendingCarrayCharacter2Name = data["carrayCharacter2NodeName"].AsString();
		}
		if (data.ContainsKey("hitCharacterNodeNames"))
		{
			_pendingHitCharacterListNames = (Array)data["hitCharacterNodeNames"];
		}
	}
}
