using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantPuffShroomElf : TowerDefensePlant
{
	private const string ElfFadeSceneUid = "uid://bq3mw7keas2vt";

	private const string ElfFadeClip = "Quake";

	private const string FadeAnimeClip = "Fade";

	private const double LifetimeSeconds = 50.0;

	private FireComponent _fireComponent;

	private double _lifetimeRemaining = 50.0;

	private ulong _lifetimeLastPhysicsFrame = 18446744073709551615uL;

	private bool _vanishing;

	private bool _fadePlaying;

	private double _fireInterval = 1.5;

	private int _fireNum = 1;

	private string _projectileName = "PuffShroomElfPuff";

	[Export(PropertyHint.None, "")]
	public double fireInterval
	{
		get
		{
			return _fireInterval;
		}
		set
		{
			_fireInterval = value;
			if (IsNodeReady() && _fireComponent != null)
			{
				FireComponent fireComponent = _fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased)
				{
					_fireComponent.fireInterval = (float)value;
				}
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public int fireNum
	{
		get
		{
			return _fireNum;
		}
		set
		{
			_fireNum = value;
			if (IsNodeReady() && _fireComponent != null)
			{
				FireComponent fireComponent = _fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased)
				{
					_fireComponent.fireNum = value;
				}
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public string projectileName
	{
		get
		{
			return _projectileName;
		}
		set
		{
			_projectileName = value;
			if (IsNodeReady() && _fireComponent != null)
			{
				FireComponent fireComponent = _fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased && _fireComponent.fireCheckList.Count > 0)
				{
					((FireComponentProjectileSingle)_fireComponent.fireCheckList[0].projectile).projectileName = value;
				}
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !editorPreviewMode)
		{
			TargetRegistrationComponent obj = targetRegistrationComponent;
			if (obj != null && !obj.IsReleased)
			{
				targetRegistrationComponent.UnregisterTarget();
			}
			if (inGame)
			{
				_fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			}
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		TickLifetime(delta);
	}

	private void TickLifetime(double delta)
	{
		if (Engine.IsEditorHint() || editorPreviewMode || _vanishing || !inGame || die || isDestroy || !TowerDefenseManager._IsGameRunning())
		{
			return;
		}
		ulong currentPhysicsFrame = TowerDefenseProcessModeDispatch.CurrentPhysicsFrame;
		if (currentPhysicsFrame != 18446744073709551615uL)
		{
			if (_lifetimeLastPhysicsFrame == currentPhysicsFrame)
			{
				return;
			}
			_lifetimeLastPhysicsFrame = currentPhysicsFrame;
		}
		_lifetimeRemaining -= delta;
		if (!(_lifetimeRemaining > 0.0))
		{
			_lifetimeRemaining = 0.0;
			_vanishing = true;
			BeginFadeVanish();
		}
	}

	private void BeginFadeVanish()
	{
		die = true;
		TargetRegistrationComponent obj = targetRegistrationComponent;
		if (obj != null && !obj.IsReleased)
		{
			targetRegistrationComponent.UnregisterTarget();
		}
		_fadePlaying = true;
		if (GodotObject.IsInstanceValid(base.sprite) && base.sprite.HasClip("Fade"))
		{
			base.sprite.SetAnimation("Fade", loop: false);
		}
		else
		{
			FinishFadeVanish();
		}
	}

	private void FinishFadeVanish()
	{
		_fadePlaying = false;
		PlayElfFade();
		Destroy();
	}

	private void PlayElfFade()
	{
		PackedScene packedScene = GD.Load<PackedScene>("uid://bq3mw7keas2vt");
		if (packedScene == null)
		{
			return;
		}
		TowerDefenseEffectSpriteOnce towerDefenseEffectSpriteOnce = TowerDefenseManager.CreateEffectSpriteOnce(packedScene, base.gridPos, "Quake");
		if (GodotObject.IsInstanceValid(towerDefenseEffectSpriteOnce))
		{
			Node2D node2D = TowerDefenseManager.GetCharacterNode();
			if (!GodotObject.IsInstanceValid(node2D))
			{
				towerDefenseEffectSpriteOnce.QueueFree();
				return;
			}
			node2D.AddChild(towerDefenseEffectSpriteOnce, forceReadableName: false, InternalMode.Disabled);
			towerDefenseEffectSpriteOnce.gridPos = base.gridPos;
			towerDefenseEffectSpriteOnce.GlobalPosition = GetLogicalGlobalPosition();
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (_fadePlaying && !(clip != "Fade"))
		{
			FinishFadeVanish();
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			["fireNum"] = fireNum,
			["projectileName"] = projectileName,
			["fireInterval"] = fireInterval,
			["lifetimeRemaining"] = _lifetimeRemaining
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		fireNum = ((!data.ContainsKey("fireNum")) ? 1 : data["fireNum"].AsInt32());
		projectileName = (data.ContainsKey("projectileName") ? data["projectileName"].AsString() : "PuffShroomElfPuff");
		fireInterval = (data.ContainsKey("fireInterval") ? data["fireInterval"].AsDouble() : 1.5);
		_lifetimeRemaining = (data.ContainsKey("lifetimeRemaining") ? Mathf.Max(0.0, data["lifetimeRemaining"].AsDouble()) : 50.0);
	}
}
