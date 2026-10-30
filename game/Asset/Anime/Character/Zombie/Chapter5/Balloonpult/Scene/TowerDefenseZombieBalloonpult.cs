using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieBalloonpult : TowerDefenseZombie
{
	private FireComponent _fireComponent;

	private CatapultComponent _catapultComponent;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			_catapultComponent = componentManager.GetRuntime<CatapultComponent>();
		}
	}

	public override void IdleEntered()
	{
		base.IdleEntered();
	}

	public override void IdleProcessing(double delta)
	{
		base.IdleProcessing(delta);
		_catapultComponent.IdleProcessing(delta);
	}

	public override void IdleExited()
	{
		base.IdleExited();
	}

	public override void WalkEntered()
	{
		_catapultComponent.WalkEntered();
	}

	public override void WalkProcessing(double delta)
	{
		_catapultComponent.WalkProcessing(delta);
	}

	public override void HitpointsEmpty()
	{
		base.HitpointsEmpty();
		_catapultComponent.CreateDeathEffect();
		Destroy();
	}

	public override void DamagePointReach(string damangePointName)
	{
		base.DamagePointReach(damangePointName);
		_catapultComponent.OnDamagePoint(damangePointName);
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		base.AnimeEvent(command, argument);
		if (command == "fire")
		{
			_catapultComponent.OnFireAnimeEvent();
			CreateBoom();
			UpdateProjectileVisual();
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (!(clip == "Bounce"))
		{
			if (clip == "Fire")
			{
				_catapultComponent.OnFireAnimeCompleted();
			}
		}
		else
		{
			_catapultComponent.CreateDeathEffect();
		}
	}

	private void UpdateProjectileVisual()
	{
		int currentProjectileNum = _catapultComponent.currentProjectileNum;
		int projectileNum = _catapultComponent.projectileNum;
		if (currentProjectileNum <= 0)
		{
			base.sprite.SetFliters(new Array
			{
				"balloon1", "balloon_bottom1", "balloon_string1", "balloon2", "balloon_bottom2", "balloon_string2", "balloon3", "balloon_bottom3", "balloon_string3", "balloon4",
				"balloon_bottom4", "balloon_string4", "Zombie_catapult_basketball", "Zombie_catapult_basketball2", "Zombie_catapult_basketball3", "Zombie_catapult_basketball4"
			}, open: false);
		}
		else if ((double)currentProjectileNum < (double)projectileNum / 4.0 * 1.0)
		{
			base.sprite.SetFliters(new Array { "balloon3", "balloon_bottom3", "balloon_string3", "Zombie_catapult_basketball3" }, open: false);
		}
		else if ((double)currentProjectileNum < (double)projectileNum / 4.0 * 2.0)
		{
			base.sprite.SetFliters(new Array { "balloon2", "balloon_bottom2", "balloon_string2", "Zombie_catapult_basketball2" }, open: false);
		}
		else if ((double)currentProjectileNum <= (double)projectileNum / 4.0 * 3.0)
		{
			base.sprite.SetFliters(new Array { "balloon1", "balloon_bottom1", "balloon_string1", "Zombie_catapult_basketball" }, open: false);
		}
	}

	public void CreateBoom()
	{
		if (Global.Instance.isMultiplayerMode && !MultiPlayerManager.Instance.isHost)
		{
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("ZombieBalloonBomb");
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		float x = logicalGlobalPosition.X;
		if (_fireComponent.firePosMarker.Count > 0 && GodotObject.IsInstanceValid(_fireComponent.firePosMarker[0]))
		{
			x = GetLogicalGlobalPosition(_fireComponent.firePosMarker[0]).X;
		}
		TowerDefenseCharacter character = packetConfig.Create(new Vector2(x, logicalGlobalPosition.Y), base.gridPos);
		TowerDefenseGroundItemBase.characterNode.AddChild(character, forceReadableName: false, InternalMode.Disabled);
		character.CallDeferred("Walk");
		if (instance.hypnoses)
		{
			Callable.From(delegate
			{
				character.Hypnoses();
			}).CallDeferred();
		}
	}
}
