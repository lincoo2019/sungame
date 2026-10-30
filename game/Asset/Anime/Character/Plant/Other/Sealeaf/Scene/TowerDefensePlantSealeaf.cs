using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantSealeaf : TowerDefensePlant
{
	private const double KnockbackInterval = 0.5;

	private const double KnockbackGrid = 1.0;

	private const double KnockbackDuration = 0.5;

	private const double KnockbackHurt = 100.0;

	private const double DiveStunTime = 3.0;

	private FireComponent _fireComponent;

	private BlockComponent _blockComponent;

	private bool _isBlocking;

	private double _fireInterval = 1.5;

	private double _knockbackTimer;

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

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			_blockComponent = componentManager.GetRuntime<BlockComponent>();
			if (_blockComponent != null)
			{
				_blockComponent.OnBlock += BlockCharacter;
				_blockComponent.SetCheckRectangleSize(TowerDefenseManager.Instance.GetMapGridSize() * 3.5f);
			}
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		if (_blockComponent != null)
		{
			_blockComponent.OnBlock -= BlockCharacter;
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint() && inGame && GodotObject.IsInstanceValid(instance) && !instance.sleep)
		{
			_knockbackTimer += (float)delta;
			if (_knockbackTimer >= 0.5)
			{
				_knockbackTimer = 0.0;
				TryKnockbackNearby();
			}
		}
	}

	public void BlockCharacter()
	{
		if (!_isBlocking)
		{
			_isBlocking = true;
			base.itemLayer = TowerDefenseEnum.LAYER_GROUNDITEM.EFFECT;
			base.sprite.SetAnimation("Block", loop: false, 0.1);
			base.sprite.AddAnimation("Idle", 0.0);
		}
	}

	private void PlayBlock2()
	{
		if (base.sprite != null)
		{
			base.sprite.SetAnimation("Block2", loop: false, 0.1);
			base.sprite.AddAnimation("Idle", 0.0);
		}
	}

	private void TryKnockbackNearby()
	{
		if ((Global.IsMultiplayerMode && !MultiPlayerManager.IsHost) || !GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			return;
		}
		Vector2 mapGridSize = TowerDefenseManager.Instance.GetMapGridSize();
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		foreach (TowerDefenseCharacter item in TowerDefenseManager.Instance.GetCharacterTarget(this, checkLine: true))
		{
			if (item is TowerDefenseZombie && GodotObject.IsInstanceValid(item.instance) && !item.die && !item.nearDie && item.camp != base.camp && item.instance.zombiePhysique < TowerDefenseEnum.ZOMBIE_PHYSIQUE.HUGE && (item.instance.unUseBuffFlags & 0x200) == 0 && (item.instance.unUseBuffFlags & -1) != -1 && !((item.GetLogicalGlobalPosition() - logicalGlobalPosition).Abs().X > mapGridSize.X * 1.25f))
			{
				double num = (instance.hypnoses ? (-1.0) : 1.0);
				if ((item.instance.maskFlags & 0x20) != 0)
				{
					item.BlowBack(1.0 * num, 0.3);
					TowerDefenseCharacterBuffDizziness towerDefenseCharacterBuffDizziness = new TowerDefenseCharacterBuffDizziness();
					towerDefenseCharacterBuffDizziness.time = 3.0;
					item.BuffAdd(towerDefenseCharacterBuffDizziness);
					RaiseDiveHeight(item);
				}
				else
				{
					item.BlowBack(1.0 * num, 0.5);
					TowerDefenseCharacterBuffDizziness towerDefenseCharacterBuffDizziness2 = new TowerDefenseCharacterBuffDizziness();
					towerDefenseCharacterBuffDizziness2.time = 0.5;
					item.BuffAdd(towerDefenseCharacterBuffDizziness2);
				}
				Hurt(100.0);
				PlayBlock2();
			}
		}
	}

	private async void RaiseDiveHeight(TowerDefenseCharacter target)
	{
		if (GodotObject.IsInstanceValid(target) && GodotObject.IsInstanceValid(target.instance))
		{
			Vector2 offset = target.sprite.offset;
			int originalMaskFlags = target.instance.maskFlags;
			target.instance.maskFlags = 9;
			Vector2 vector = offset + new Vector2(0f, 30f);
			Tween tween = target.CreateTween();
			tween.TweenProperty(target.sprite, "offset", vector, 0.2);
			tween.TweenInterval(3.0);
			tween.TweenProperty(target.sprite, "offset", offset, 0.2);
			await ToSignal(tween, Tween.SignalName.Finished);
			if (GodotObject.IsInstanceValid(target) && GodotObject.IsInstanceValid(target.instance) && !target.die && !target.nearDie)
			{
				target.instance.maskFlags = originalMaskFlags;
			}
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (clip == "Block" || clip == "Block2")
		{
			_isBlocking = false;
			base.itemLayer = TowerDefenseEnum.LAYER_GROUNDITEM.PLANT;
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			["fireInterval"] = fireInterval,
			["knockbackTimer"] = _knockbackTimer
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		fireInterval = data.GetValueOrDefault("fireInterval", 1.5).AsDouble();
		_knockbackTimer = data.GetValueOrDefault("knockbackTimer", 0.0).AsDouble();
	}
}
