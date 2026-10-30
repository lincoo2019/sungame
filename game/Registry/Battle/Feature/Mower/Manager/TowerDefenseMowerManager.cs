using Godot;

[GlobalClass]
public partial class TowerDefenseMowerManager : Control
{
	public TowerDefenseBattleFeatureMower mowerFeature;

	public TowerDefenseBattleFeatureMowerConfig config;

	private bool _isDraggingPreview;

	private AdobeAnimateSprite _previewSprite;

	private Vector2I _previewGridPosition;

	private Tween _previewTween;

	private Vector2 GetMowerPreviewPosition(Vector2I gridPos)
	{
		double num = 0.0;
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(gridPos);
		if (GodotObject.IsInstanceValid(mapCell))
		{
			num = mapCell.GetGroundHeight(0.0);
		}
		double num2 = config?.previewGroundOffset ?? 10.0;
		return TowerDefenseManager.GetMapCellPlantPos(gridPos) - new Vector2(0f, (float)(num + num2));
	}

	public bool NeedsInputProcessing()
	{
		if (_isDraggingPreview || Input.IsActionJustPressed("Press") || Input.IsActionJustReleased("Press"))
		{
			return true;
		}
		if (!GodotObject.IsInstanceValid(_previewSprite))
		{
			return false;
		}
		if (!Input.IsActionJustPressed("P1Up") && !Input.IsActionJustPressed("P1Down") && !Input.IsActionJustPressed("P1Left"))
		{
			return Input.IsActionJustPressed("P1Right");
		}
		return true;
	}

	public void ProcessMowerInput(TowerDefenseCellInstance cell, Vector2I gridPos)
	{
		TowerDefenseBattleFeatureMap mapFeature = TowerDefenseManager.GetMapFeature();
		if (!GodotObject.IsInstanceValid(mapFeature))
		{
			return;
		}
		if (Input.IsActionJustPressed("Press") && GodotObject.IsInstanceValid(cell) && cell.CanMowerMove())
		{
			if (!GodotObject.IsInstanceValid(_previewSprite))
			{
				_previewSprite = TowerDefenseManager.GetCharacterSprite(config?.previewSpriteName ?? "MowerDefault");
				if (!GodotObject.IsInstanceValid(_previewSprite))
				{
					return;
				}
				_previewSprite.GlobalPosition = GetMowerPreviewPosition(gridPos);
				_previewSprite.Scale = Vector2.One * (float)(config?.previewScale ?? 1.0);
				Node2D characterNode = TowerDefenseManager.GetCharacterNode();
				if (!GodotObject.IsInstanceValid(characterNode))
				{
					_previewSprite.QueueFree();
					_previewSprite = null;
					return;
				}
				characterNode.AddChild(_previewSprite, forceReadableName: false, InternalMode.Disabled);
			}
			MovePreviewTo(gridPos);
			_isDraggingPreview = true;
		}
		if (Input.IsActionJustReleased("Press"))
		{
			_isDraggingPreview = false;
		}
		if (_isDraggingPreview && GodotObject.IsInstanceValid(cell) && gridPos - _previewGridPosition != Vector2I.Zero)
		{
			TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(_previewGridPosition);
			if (GodotObject.IsInstanceValid(mapCell) && mapCell.CanMoveToCell(cell))
			{
				MovePreviewTo(gridPos);
				mapCell.MoveToCell(cell);
			}
		}
		if (!GodotObject.IsInstanceValid(_previewSprite))
		{
			return;
		}
		int num = 0;
		int num2 = 0;
		bool flag = false;
		if (_previewGridPosition.Y - 1 >= 1 && Input.IsActionJustPressed("P1Up"))
		{
			num2--;
			flag = true;
		}
		if (_previewGridPosition.Y + 1 <= mapFeature.config.gridNum.Y && Input.IsActionJustPressed("P1Down"))
		{
			num2++;
			flag = true;
		}
		if (_previewGridPosition.X - 1 >= 1 && Input.IsActionJustPressed("P1Left"))
		{
			num--;
			flag = true;
		}
		if (_previewGridPosition.X + 1 <= mapFeature.config.gridNum.X && Input.IsActionJustPressed("P1Right"))
		{
			num++;
			flag = true;
		}
		if (!flag)
		{
			return;
		}
		Vector2I vector2I = new Vector2I(num, num2);
		if (vector2I != Vector2I.Zero)
		{
			TowerDefenseCellInstance mapCell2 = TowerDefenseManager.GetMapCell(_previewGridPosition);
			TowerDefenseCellInstance mapCell3 = TowerDefenseManager.GetMapCell(_previewGridPosition + vector2I);
			if (GodotObject.IsInstanceValid(mapCell2) && GodotObject.IsInstanceValid(mapCell3) && mapCell2.CanMoveToCell(mapCell3))
			{
				MovePreviewTo(_previewGridPosition + vector2I);
				mapCell2.MoveToCell(mapCell3);
			}
		}
	}

	private void MovePreviewTo(Vector2I gridPosition)
	{
		if (GodotObject.IsInstanceValid(_previewSprite))
		{
			_previewTween?.Kill();
			_previewTween = CreateTween();
			_previewTween.SetEase(Tween.EaseType.Out);
			_previewTween.SetTrans(Tween.TransitionType.Quart);
			_previewTween.TweenProperty(_previewSprite, "global_position", GetMowerPreviewPosition(gridPosition), config?.previewTweenDuration ?? 0.5);
			_previewGridPosition = gridPosition;
			_previewSprite.ZIndex = gridPosition.Y * 15 + 1;
		}
	}

	public void DisposeBattleState()
	{
		_previewTween?.Kill();
		_previewTween = null;
		if (GodotObject.IsInstanceValid(_previewSprite))
		{
			_previewSprite.QueueFree();
		}
		_previewSprite = null;
		_previewGridPosition = Vector2I.Zero;
		_isDraggingPreview = false;
		mowerFeature = null;
		config = null;
	}

	public override void _ExitTree()
	{
		DisposeBattleState();
		base._ExitTree();
	}
}
