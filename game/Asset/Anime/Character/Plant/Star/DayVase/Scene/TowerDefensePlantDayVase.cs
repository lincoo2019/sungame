using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantDayVase : TowerDefensePlant
{
	private static PackedScene _DAY_VASE_CHUNKS;

	private AdobeAnimateSpriteBase _hammer;

	private MousePressComponent _mousePressComponent;

	private StateHandle _attackState;

	private bool _roleStateSignalsConnected;

	public bool isMoseIn;

	public bool pressed;

	public bool over;

	public PackedScene chunksEffect;

	public bool pressAwait;

	private static PackedScene DAY_VASE_CHUNKS => _DAY_VASE_CHUNKS ?? (_DAY_VASE_CHUNKS = GD.Load<PackedScene>("uid://ctsxe8qhievw0"));

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !editorPreviewMode)
		{
			_hammer = GetNode<AdobeAnimateSpriteBase>("%Hammer");
			_hammer.OnAnimeCompleted += HammerAnimeCompleted;
			_mousePressComponent = componentManager?.GetRuntime<MousePressComponent>();
			MousePressComponent mousePressComponent = _mousePressComponent;
			if (mousePressComponent != null && !mousePressComponent.IsReleased)
			{
				_mousePressComponent.OnPressed += HandleVasePressed;
			}
			chunksEffect = DAY_VASE_CHUNKS;
			instance.ClearHitpointsEmptyListeners();
			instance.hitpointsEmpty += Change;
			_attackState = base.StateMachine?.GetStateById("plant.day_vase.attack");
			ConnectRoleStateSignals();
		}
	}

	public override void _ExitTree()
	{
		MousePressComponent mousePressComponent = _mousePressComponent;
		if (mousePressComponent != null && !mousePressComponent.IsReleased)
		{
			_mousePressComponent.OnPressed -= HandleVasePressed;
		}
		_mousePressComponent = null;
		base._ExitTree();
	}

	private void ConnectRoleStateSignals()
	{
		if (!_roleStateSignalsConnected)
		{
			StateHandle attackState = _attackState;
			if (attackState != null && attackState.IsValid)
			{
				_attackState.Entered += AttackEntered;
				_attackState.Exited += AttackExited;
				_attackState.PhysicsProcessing += AttackProcessing;
				_roleStateSignalsConnected = true;
			}
		}
	}

	private void DisconnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
		{
			_attackState.Entered -= AttackEntered;
			_attackState.Exited -= AttackExited;
			_attackState.PhysicsProcessing -= AttackProcessing;
			_attackState = null;
			_roleStateSignalsConnected = false;
		}
	}

	public override void BatchUpdate(double delta)
	{
		if (!Engine.IsEditorHint())
		{
			base.BatchUpdate(delta);
			isMoseIn = _mousePressComponent?.IsMouseInside ?? false;
			if (!Global.IsEditor || !(SceneManager.CurrentScene == "LevelEditorStage"))
			{
				TowerDefenseManager.Instance.IsGameRunning();
			}
		}
	}

	private void HandleVasePressed(Vector2 pointerPosition)
	{
		if (pressAwait || instance.hypnoses || pressed)
		{
			return;
		}
		TowerDefenseManager towerDefenseManager = TowerDefenseManager.Instance;
		if (GodotObject.IsInstanceValid(towerDefenseManager) && !(towerDefenseManager.GetMapGridPosFromMouse(pointerPosition) != base.gridPos))
		{
			PacketPickControl packetPickControl = towerDefenseManager.GetPacketPickControl();
			if (!GodotObject.IsInstanceValid(packetPickControl) || !GodotObject.IsInstanceValid(packetPickControl.packetPick))
			{
				_hammer.Visible = true;
				AudioManager.Instance.AudioPlay("Swing");
				_hammer.SetAnimation("OpenPot", loop: false);
				pressed = true;
				pressAwait = true;
				CallDeferred("_ResetPressAwait");
			}
		}
	}

	private async void _ResetPressAwait()
	{
		await ToSignal(GetTree().CreateTimer(0.1, processAlways: false), SceneTreeTimer.SignalName.Timeout);
		pressAwait = false;
	}

	public virtual void AttackEntered()
	{
		if (TowerDefenseManager.GetMapIsNight())
		{
			base.sprite.SetAnimation("Day", loop: false, 0.1);
		}
		else
		{
			base.sprite.SetAnimation("Night", loop: false, 0.1);
		}
		if (!over)
		{
			TryStartDayNightSwitch();
			over = true;
		}
	}

	private void TryStartDayNightSwitch()
	{
		TowerDefenseBattleFeatureMap mapFeature = TowerDefenseManager.GetMapFeature();
		if (!GodotObject.IsInstanceValid(mapFeature) || !mapFeature.isChange)
		{
			TowerDefenseManager.Instance.MapDayNightSwitch(5.0);
		}
	}

	public virtual void AttackProcessing(double delta)
	{
		base.sprite.timeScale = timeScale;
	}

	public virtual void AttackExited()
	{
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (clip == "Day" || clip == "Night")
		{
			TowerDefenseManager.Instance.CharacterUnregister(this);
			RemoveFromGroup("Character");
			QueueFree();
		}
	}

	public void Change()
	{
		pressed = true;
		SendStateEvent("ToAttack");
		Break();
		Destroy(freeInstance: false);
	}

	public async void Break()
	{
		AudioManager.Instance.AudioPlay("VaseBreaking");
		TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(chunksEffect, base.gridPos);
		towerDefenseEffectParticlesOnce.GlobalPosition = GetLogicalGlobalPosition(transformPoint) - new Vector2(0f, 30f);
		TowerDefenseManager.GetCharacterNode().AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, InternalMode.Disabled);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
	}

	public void HammerAnimeCompleted(string clip)
	{
		if (clip == "OpenPot")
		{
			_hammer.Visible = false;
			Change();
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "isMoseIn", isMoseIn },
			{ "pressed", pressed },
			{ "over", over },
			{ "pressAwait", pressAwait }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		isMoseIn = data.GetValueOrDefault("isMoseIn", Variant.From<bool>(false)).AsBool();
		pressed = data.GetValueOrDefault("pressed", Variant.From<bool>(false)).AsBool();
		over = data.GetValueOrDefault("over", Variant.From<bool>(false)).AsBool();
		pressAwait = data.GetValueOrDefault("pressAwait", Variant.From<bool>(false)).AsBool();
	}
}
