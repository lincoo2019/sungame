using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseItemMagnetWave : TowerDefenseItem
{
	private MagnetComponent magnetComponent;

	public Array<TowerDefenseMagnet> armorList = new Array<TowerDefenseMagnet>();

	public bool over;

	public int drawNum = 1;

	private int _drawCompleted;

	private bool _drawLoopRunning;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !editorPreviewMode)
		{
			magnetComponent = componentManager.GetRuntime<MagnetComponent>();
			if (magnetComponent == null)
			{
				GD.PushError("Magnet Wave is missing its Magnet resource runtime.");
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
		if (_drawLoopRunning || _drawCompleted >= drawNum || (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost) || magnetComponent == null)
		{
			return;
		}
		_drawLoopRunning = true;
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		while (_drawCompleted < drawNum && GodotObject.IsInstanceValid(this) && !isDestroy)
		{
			if (await magnetComponent.CanArmorDraw())
			{
				magnetComponent.ArmorDrawNear();
				TowerDefenseMagnet drawnMagnet = magnetComponent.magnet;
				magnetComponent.magnet = null;
				magnetComponent.breakDownArmor = null;
				_drawCompleted++;
				await ToSignal(GetTree().CreateTimer(0.1, processAlways: false), SceneTreeTimer.SignalName.Timeout);
				if (GodotObject.IsInstanceValid(drawnMagnet))
				{
					armorList.Add(drawnMagnet);
				}
			}
			else
			{
				_drawCompleted++;
			}
		}
		_drawLoopRunning = false;
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (Engine.IsEditorHint())
		{
			return;
		}
		foreach (TowerDefenseMagnet armor in armorList)
		{
			if (!GodotObject.IsInstanceValid(armor))
			{
				continue;
			}
			Marker2D marker2D = magnetComponent?.posMarker;
			if (marker2D != null)
			{
				Vector2 logicalGlobalPosition = GetLogicalGlobalPosition(marker2D);
				if ((double)armor.GlobalPosition.DistanceTo(logicalGlobalPosition) >= 0.01)
				{
					armor.GlobalPosition = armor.GlobalPosition.Lerp(logicalGlobalPosition, (float)(10.0 * delta));
					armor.Scale = armor.Scale.Lerp(Vector2.Zero, (float)(10.0 * delta));
				}
			}
		}
	}

	public override void Destroy(bool freeInstance = true)
	{
		magnetComponent?.Destroy();
		base.Destroy(freeInstance);
	}

	public override void AnimeCompleted(string clip)
	{
		if (clip == "Idle")
		{
			Destroy();
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { ["drawCompleted"] = _drawCompleted };
	}

	public override bool ImportVariantSaveWhenEmpty()
	{
		return true;
	}

	public override void ImportVariantSave(Dictionary data)
	{
		_drawLoopRunning = false;
		if (data == null || !data.ContainsKey("drawCompleted"))
		{
			_drawCompleted = Mathf.Max(0, drawNum);
		}
		else
		{
			_drawCompleted = Mathf.Clamp(data.GetValueOrDefault("drawCompleted", 0).AsInt32(), 0, Mathf.Max(0, drawNum));
		}
	}

	public override void FinalizeProgressRestore()
	{
		base.FinalizeProgressRestore();
		if (_drawCompleted < drawNum)
		{
			InitMagnet();
		}
	}
}
