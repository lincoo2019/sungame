using System;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseItemMagnet : TowerDefenseItem
{
	private static PackedScene _CHERRY_BOMB_EXPLOSION;

	private MagnetComponent magnetComponent;

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> eventList = new Array<TowerDefenseCharacterEventBase>();

	public TowerDefenseArmorInstance armor;

	public bool over;

	private bool _drawResolved;

	private bool _destroyCountdownActive;

	private double _destroyTimer = 1.0;

	private static PackedScene CHERRY_BOMB_EXPLOSION => _CHERRY_BOMB_EXPLOSION ?? (_CHERRY_BOMB_EXPLOSION = GD.Load<PackedScene>("uid://cibtjjjomdxnh"));

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !editorPreviewMode)
		{
			magnetComponent = componentManager.GetRuntime<MagnetComponent>();
			if (magnetComponent == null)
			{
				GD.PushError("Magnet item is missing its Magnet resource runtime.");
			}
			HitBoxDestroy();
			if (!base.IsProgressRestoreInFlight)
			{
				InitMagnet();
			}
		}
	}

	private async void InitMagnet()
	{
		if (_drawResolved || (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost) || magnetComponent == null)
		{
			return;
		}
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		if (GodotObject.IsInstanceValid(this) && !isDestroy)
		{
			if (await magnetComponent.CanArmorDraw())
			{
				magnetComponent.ArmorDrawNear();
			}
			_drawResolved = true;
			_destroyTimer = 1.0;
			_destroyCountdownActive = true;
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint() && _destroyCountdownActive && !isDestroy)
		{
			_destroyTimer = Math.Max(0.0, _destroyTimer - Math.Max(0.0, delta));
			if (!(_destroyTimer > 0.0))
			{
				_destroyCountdownActive = false;
				Destroy();
			}
		}
	}

	public override void Destroy(bool freeInstance = true)
	{
		_destroyCountdownActive = false;
		magnetComponent?.Destroy();
		base.Destroy(freeInstance);
	}

	public override async void DestroySet()
	{
		if (!over)
		{
			over = true;
			ViewManager.Instance.CameraShake(new Vector2(GD.RandRange(-1, 1), GD.RandRange(-1, 1)), 5.0, 0.05, 4);
			TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(CHERRY_BOMB_EXPLOSION, base.gridPos);
			towerDefenseEffectParticlesOnce.GlobalPosition = GetLogicalGlobalPosition(transformPoint) - new Vector2(0f, 30f);
			TowerDefenseManager.GetCharacterNode().AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, InternalMode.Disabled);
			TowerDefenseExplode.CreateExplode(GetLogicalGlobalPosition(), new Vector2(1.5f, 1.5f), eventList, new Array<TowerDefenseCharacter>(), base.camp, -1);
			AudioManager.Instance.AudioPlay("ExplodeCherrybomb");
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			["drawResolved"] = _drawResolved,
			["destroyCountdownActive"] = _destroyCountdownActive,
			["destroyTimer"] = _destroyTimer,
			["over"] = over
		};
	}

	public override bool ImportVariantSaveWhenEmpty()
	{
		return true;
	}

	public override void ImportVariantSave(Dictionary data)
	{
		if (data == null || !data.ContainsKey("drawResolved"))
		{
			_drawResolved = true;
			_destroyCountdownActive = true;
			_destroyTimer = 0.0;
		}
		else
		{
			_drawResolved = data.GetValueOrDefault("drawResolved", false).AsBool();
			_destroyCountdownActive = data.GetValueOrDefault("destroyCountdownActive", false).AsBool();
			_destroyTimer = Math.Max(0.0, data.GetValueOrDefault("destroyTimer", 0.0).AsDouble());
			over = data.GetValueOrDefault("over", over).AsBool();
		}
	}

	public override void FinalizeProgressRestore()
	{
		base.FinalizeProgressRestore();
		if (!_drawResolved && !_destroyCountdownActive && !over)
		{
			InitMagnet();
		}
	}
}
