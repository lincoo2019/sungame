using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantPorTallnut : TowerDefensePlant, IProjectileZone
{
	private const string POR_TALLNUT_BODY = "uid://bs7ofo8ns0gja";

	private const string POR_TALLNUT_CRACKED1 = "uid://co7m11ber6yxy";

	private const string POR_TALLNUT_CRACKED2 = "uid://b0n2gnnrjp7nn";

	private const string POR_TALLNUT_LEFT1 = "uid://gmk2eahfgc02";

	private const string POR_TALLNUT_LEFT2 = "uid://bg8metd818nty";

	private const string POR_TALLNUT_LEFT3 = "uid://dbnfbqj81lk5n";

	private const string POR_TALLNUT_MID1 = "uid://llr1lytnmv6p";

	private const string POR_TALLNUT_MID2 = "uid://dquduwep24k4x";

	private const string POR_TALLNUT_MID3 = "uid://d3r08nkknpepr";

	private const string POR_TALLNUT_RIGHT1 = "uid://c1cblsxkffip8";

	private const string POR_TALLNUT_RIGHT2 = "uid://dajcpk5gab5i8";

	private const string POR_TALLNUT_RIGHT3 = "uid://c3qtg8ng54gxx";

	private AttackComponent _attackComponent;

	private MousePressComponent _mousePressComponent;

	private Rect2 _zoneRect;

	private StateHandle _openState;

	private StateHandle _openIdleState;

	private StateHandle _closeState;

	private bool _roleStateSignalsConnected;

	private bool _portalTransportActive;

	public bool open = true;

	public Rect2 WorldRect => _zoneRect;

	public int GridY => base.gridPos.Y;

	public int RowSpan => 0;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !editorPreviewMode)
		{
			_attackComponent = componentManager.GetRuntime<AttackComponent>("character.attack.0");
			_mousePressComponent = componentManager.GetRuntime<MousePressComponent>();
			_mousePressComponent.OnPressed += ToggleOpen;
			AddToGroup("PorTallnut");
			open = true;
			_openState = base.StateMachine?.GetStateById("plant.por_tallnut.open");
			_openIdleState = base.StateMachine?.GetStateById("plant.por_tallnut.open_idle");
			_closeState = base.StateMachine?.GetStateById("plant.por_tallnut.close");
			ConnectRoleStateSignals();
			if (BulletField.Instance != null)
			{
				BulletField.Instance.RegisterZone(this);
			}
		}
	}

	public override void _ExitTree()
	{
		if (!Engine.IsEditorHint() && BulletField.Instance != null)
		{
			BulletField.Instance.UnregisterZone(this);
		}
		if (_mousePressComponent != null)
		{
			_mousePressComponent.OnPressed -= ToggleOpen;
		}
		base._ExitTree();
	}

	private void ConnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
		{
			return;
		}
		StateHandle openState = _openState;
		if (openState == null || !openState.IsValid)
		{
			return;
		}
		StateHandle openIdleState = _openIdleState;
		if (openIdleState != null && openIdleState.IsValid)
		{
			StateHandle closeState = _closeState;
			if (closeState != null && closeState.IsValid)
			{
				_openState.Entered += OpenEntered;
				_openState.Exited += OpenExited;
				_openState.PhysicsProcessing += OpenProcessing;
				_openIdleState.Entered += OpenIdleEntered;
				_openIdleState.Exited += OpenIdleExited;
				_openIdleState.PhysicsProcessing += OpenIdleProcessing;
				_closeState.Entered += CloseEntered;
				_closeState.Exited += CloseExited;
				_closeState.PhysicsProcessing += CloseProcessing;
				_roleStateSignalsConnected = true;
			}
		}
	}

	private void DisconnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
		{
			_openState.Entered -= OpenEntered;
			_openState.Exited -= OpenExited;
			_openState.PhysicsProcessing -= OpenProcessing;
			_openIdleState.Entered -= OpenIdleEntered;
			_openIdleState.Exited -= OpenIdleExited;
			_openIdleState.PhysicsProcessing -= OpenIdleProcessing;
			_closeState.Entered -= CloseEntered;
			_closeState.Exited -= CloseExited;
			_closeState.PhysicsProcessing -= CloseProcessing;
			_openState = null;
			_openIdleState = null;
			_closeState = null;
			_roleStateSignalsConnected = false;
		}
	}

	public override void DamagePointReach(string damagePointName)
	{
		base.DamagePointReach(damagePointName);
		switch (damagePointName)
		{
		case "Damage0":
			base.sprite.SetAtlasReplace("PorTallnut_body.png", "uid://bs7ofo8ns0gja");
			base.sprite.SetAtlasReplace("PorTallnut_left1.png", "uid://gmk2eahfgc02");
			base.sprite.SetAtlasReplace("PorTallnut_mid1.png", "uid://llr1lytnmv6p");
			base.sprite.SetAtlasReplace("PorTallnut_right1.png", "uid://c1cblsxkffip8");
			break;
		case "Damage1":
			base.sprite.SetAtlasReplace("PorTallnut_body.png", "uid://co7m11ber6yxy");
			base.sprite.SetAtlasReplace("PorTallnut_left1.png", "uid://bg8metd818nty");
			base.sprite.SetAtlasReplace("PorTallnut_mid1.png", "uid://dquduwep24k4x");
			base.sprite.SetAtlasReplace("PorTallnut_right1.png", "uid://dajcpk5gab5i8");
			break;
		case "Damage2":
			base.sprite.SetAtlasReplace("PorTallnut_body.png", "uid://b0n2gnnrjp7nn");
			base.sprite.SetAtlasReplace("PorTallnut_left1.png", "uid://dbnfbqj81lk5n");
			base.sprite.SetAtlasReplace("PorTallnut_mid1.png", "uid://d3r08nkknpepr");
			base.sprite.SetAtlasReplace("PorTallnut_right1.png", "uid://c3qtg8ng54gxx");
			break;
		}
	}

	public override void IdleProcessing(double delta)
	{
		base.IdleProcessing(delta);
		if (open && GetTree().GetNodeCountInGroup("PorTallnut") >= 2)
		{
			SendStateEvent("ToOpen");
		}
	}

	public virtual void OpenEntered()
	{
		AudioManager.Instance.AudioPlay("Portal");
		base.sprite.SetAnimation("Open", loop: false, 0.2);
	}

	public virtual void OpenProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 1.0;
	}

	public virtual void OpenExited()
	{
	}

	public virtual void OpenIdleEntered()
	{
		_portalTransportActive = true;
		instance.canBeCollection = true;
		base.sprite.SetAnimation("OpenIdle", loop: true, 0.2);
	}

	public virtual void OpenIdleProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 1.0;
		if (!open || GetTree().GetNodeCountInGroup("PorTallnut") < 2)
		{
			SendStateEvent("ToClose");
		}
		ProcessHitBoxOverlaps();
	}

	public virtual void OpenIdleExited()
	{
		_portalTransportActive = false;
		instance.canBeCollection = true;
	}

	public virtual void CloseEntered()
	{
		AudioManager.Instance.AudioPlay("Portal");
		base.sprite.SetAnimation("Close", loop: true, 0.2);
	}

	public virtual void CloseProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 1.0;
	}

	public virtual void CloseExited()
	{
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (!(clip == "Open"))
		{
			if (clip == "Close")
			{
				Idle();
			}
		}
		else
		{
			SendStateEvent("ToOpenIdle");
		}
	}

	private void ProcessHitBoxOverlaps()
	{
		if (TryGetActiveWorldHitRect(out var rect))
		{
			List<TowerDefenseCharacter> charactersIntersectingRectListExcludingCamp = TowerDefenseManager.Instance.characterRegistry.GetCharactersIntersectingRectListExcludingCamp(rect, base.camp);
			for (int i = 0; i < charactersIntersectingRectListExcludingCamp.Count; i++)
			{
				HandleOverlap(charactersIntersectingRectListExcludingCamp[i]);
			}
		}
	}

	private void HandleOverlap(Node character)
	{
		if (!open)
		{
			return;
		}
		if (character is TowerDefenseProjectile towerDefenseProjectile)
		{
			if (TryGetProjectileDestination(towerDefenseProjectile.GlobalPosition, towerDefenseProjectile.velocity, towerDefenseProjectile.camp, out var destination))
			{
				towerDefenseProjectile.GlobalPosition = destination.GetLogicalGlobalPosition() + new Vector2(instance.hypnoses ? 11 : (-11), 0f);
				towerDefenseProjectile.gridPos = destination.gridPos;
			}
		}
		else
		{
			if (!(character is TowerDefenseGroundItemBase) || (character is TowerDefensePlant && !(character is TowerDefensePlantBowlingBase)) || (character is TowerDefenseItem && !(character is TowerDefenseMower)) || character is TowerDefenseCrater || character is TowerDefenseGravestone || !(character is TowerDefenseCharacter towerDefenseCharacter))
			{
				return;
			}
			Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
			Vector2 logicalGlobalPosition2 = towerDefenseCharacter.GetLogicalGlobalPosition();
			if (character is TowerDefenseZombie)
			{
				if (((TowerDefenseZombie)character).instance.zombiePhysique == TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS)
				{
					return;
				}
				if (((TowerDefenseZombie)character).isChangeLine)
				{
					Vector2 mapGridSize = TowerDefenseManager.Instance.GetMapGridSize();
					if (Mathf.Abs(logicalGlobalPosition2.Y - logicalGlobalPosition.Y) > mapGridSize.Y * 0.5f)
					{
						return;
					}
				}
				else if (((TowerDefenseZombie)character).gridPos.Y != base.gridPos.Y)
				{
					return;
				}
			}
			if (logicalGlobalPosition2.X < logicalGlobalPosition.X - 10f || !towerDefenseCharacter.instance.canBeCollection || !CanTarget(towerDefenseCharacter))
			{
				return;
			}
			Array<Node> nodesInGroup = GetTree().GetNodesInGroup("PorTallnut");
			nodesInGroup.Remove(this);
			if (nodesInGroup.Count <= 0)
			{
				return;
			}
			TowerDefensePlantPorTallnut towerDefensePlantPorTallnut = nodesInGroup.PickRandom() as TowerDefensePlantPorTallnut;
			if (!GodotObject.IsInstanceValid(towerDefensePlantPorTallnut))
			{
				return;
			}
			if (character is TowerDefenseZombie { isChangeLine: not false } towerDefenseZombie)
			{
				GarlicComponent garlicComponent = towerDefenseZombie.garlicComponent;
				if (garlicComponent != null && !garlicComponent.IsReleased)
				{
					towerDefenseZombie.garlicComponent.CancelChangeLine();
				}
			}
			Vector2 logicalGlobalPosition3 = towerDefensePlantPorTallnut.GetLogicalGlobalPosition();
			towerDefenseCharacter.shadowComponent.saveShadowPosition.Y += logicalGlobalPosition3.Y - logicalGlobalPosition2.Y;
			towerDefenseCharacter.SetLogicalGlobalPosition(logicalGlobalPosition3 - new Vector2(11f, 0f));
			towerDefenseCharacter.gridPos = towerDefensePlantPorTallnut.gridPos;
		}
	}

	public void UpdateRect()
	{
		if (open && _portalTransportActive && TryGetActiveWorldHitRect(out var rect))
		{
			_zoneRect = rect;
		}
		else
		{
			_zoneRect = new Rect2(Vector2.Zero, Vector2.Zero);
		}
	}

	public void OnBulletIntersect(ref BulletData bullet, int index)
	{
		if (bullet.active && !bullet.over && bullet.config != null && TryGetProjectileDestination(bullet.pos, bullet.vel, bullet.camp, out var destination) && GodotObject.IsInstanceValid(BulletField.Instance))
		{
			Vector2 newPos = destination.GetLogicalGlobalPosition() + new Vector2(instance.hypnoses ? 11 : (-11), 0f);
			BulletField.Instance.TeleportBullet(index, newPos, destination.gridPos.Y);
			bullet.gridPos = destination.gridPos;
		}
	}

	public void OnProjectileIntersect(TowerDefenseProjectile projectile)
	{
		HandleOverlap(projectile);
	}

	private bool TryGetProjectileDestination(Vector2 projectilePosition, Vector2 projectileVelocity, TowerDefenseEnum.CHARACTER_CAMP projectileCamp, out TowerDefensePlantPorTallnut destination)
	{
		destination = null;
		if (!open || !_portalTransportActive || projectileCamp != base.camp)
		{
			return false;
		}
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		if (!instance.hypnoses)
		{
			if (projectilePosition.X < logicalGlobalPosition.X - 10f || projectileVelocity.X > 0f)
			{
				return false;
			}
		}
		else if (projectilePosition.X > logicalGlobalPosition.X + 10f || projectileVelocity.X < 0f)
		{
			return false;
		}
		Array<Node> nodesInGroup = GetTree().GetNodesInGroup("PorTallnut");
		nodesInGroup.Remove(this);
		if (nodesInGroup.Count == 0)
		{
			return false;
		}
		destination = nodesInGroup.PickRandom() as TowerDefensePlantPorTallnut;
		return GodotObject.IsInstanceValid(destination);
	}

	public void ToggleOpen(Vector2 pos)
	{
		open = !open;
		if (open)
		{
			AddToGroup("PorTallnut");
		}
		else
		{
			RemoveFromGroup("PorTallnut");
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { { "open", open } };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		open = data.GetValueOrDefault("open", Variant.From<bool>(true)).AsBool();
	}
}
