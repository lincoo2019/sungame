using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantDiceBomb : TowerDefensePlant
{
	private ExplodeComponent _explodeComponent;

	private int _rollResult;

	private int _explodeCount;

	private bool _destroyPending;

	private const double ExplodeInterval = 0.35;

	public override void _Ready()
	{
		base._Ready();
		if (Engine.IsEditorHint() || editorPreviewMode)
		{
			return;
		}
		_explodeComponent = componentManager.GetRuntime<ExplodeComponent>();
		if (_explodeComponent != null)
		{
			_explodeComponent.OnExplode += OnExplodeHandler;
			_rollResult = GD.RandRange(1, 6);
			string text = $"Explode{_rollResult}";
			if (base.sprite != null && base.sprite.HasClip(text))
			{
				_explodeComponent.explodeAnimeClips = text;
			}
		}
	}

	public override void _ExitTree()
	{
		ExplodeComponent explodeComponent = _explodeComponent;
		if (explodeComponent != null && !explodeComponent.IsReleased)
		{
			_explodeComponent.OnExplode -= OnExplodeHandler;
		}
		base._ExitTree();
	}

	private void OnExplodeHandler()
	{
		_explodeCount++;
		if (_explodeCount >= _rollResult)
		{
			if (!_destroyPending)
			{
				_destroyPending = true;
				Destroy();
			}
		}
		else
		{
			ScheduleNextExplode();
		}
	}

	private void ScheduleNextExplode()
	{
		SceneTree tree = GetTree();
		if (tree == null)
		{
			return;
		}
		tree.CreateTimer(0.35, processAlways: false).Timeout += delegate
		{
			ExplodeComponent explodeComponent = _explodeComponent;
			if (explodeComponent != null && !explodeComponent.IsReleased && !die)
			{
				_explodeComponent.Explode();
			}
		};
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			["rollResult"] = _rollResult,
			["explodeCount"] = _explodeCount
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		_rollResult = data.GetValueOrDefault("rollResult", 1).AsInt32();
		if (_rollResult < 1)
		{
			_rollResult = 1;
		}
		_explodeCount = data.GetValueOrDefault("explodeCount", 0).AsInt32();
	}
}
