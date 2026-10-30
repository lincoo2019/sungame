using System.Collections.Generic;
using Godot;

[Tool]
public partial class TowerDefensePlantGarlicBird : TowerDefensePlant
{
	private static PackedScene _SUANIAO_SCENE;

	private static PackedScene _GARLIC_BIRD_SCENE;

	private bool _over;

	private static PackedScene SUANIAO_SCENE => _SUANIAO_SCENE ?? (_SUANIAO_SCENE = GD.Load<PackedScene>("uid://d108rh2c5elsf"));

	private static PackedScene GARLIC_BIRD_SCENE => _GARLIC_BIRD_SCENE ?? (_GARLIC_BIRD_SCENE = GD.Load<PackedScene>("uid://5g1exh0phn56"));

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			instance.invincibleHurt = true;
		}
	}

	public override void AttackDeal(TowerDefenseCharacter character, string type, double num)
	{
		base.AttackDeal(character, type, num);
		if (GodotObject.IsInstanceValid(character))
		{
			switch (type)
			{
			case "Eat":
				SkipInvincibleHurt(Mathf.Max(10.0, num));
				ApplySuanNiao(character);
				break;
			case "Smash":
				SkipInvincibleHurt(num);
				break;
			case "Chomp":
				Destroy();
				break;
			}
		}
	}

	public void ApplySuanNiao(TowerDefenseCharacter character)
	{
		ApplySuanNiaoToCharacter(character);
	}

	private static void ApplySuanNiaoToCharacter(TowerDefenseCharacter character)
	{
		TowerDefenseZombie zombie = character as TowerDefenseZombie;
		if (zombie == null || zombie.nearDie || zombie.die || zombie.isGarlicBird || zombie.isChangeLine || IsSuanNiaoImmune(zombie))
		{
			return;
		}
		zombie.isGarlicBird = true;
		zombie.attackComponent.alive = false;
		zombie.Walk();
		Node2D effect = SUANIAO_SCENE.Instantiate<Node2D>(PackedScene.GenEditState.Disabled);
		effect.Position = new Vector2(0f, -100f);
		zombie.AddChild(effect, forceReadableName: false, InternalMode.Disabled);
		Timer timer = new Timer();
		timer.OneShot = true;
		timer.WaitTime = 5.0;
		zombie.AddChild(timer, forceReadableName: false, InternalMode.Disabled);
		timer.Timeout += delegate
		{
			if (GodotObject.IsInstanceValid(effect))
			{
				effect.QueueFree();
			}
			if (GodotObject.IsInstanceValid(zombie) && !zombie.nearDie && !zombie.die)
			{
				zombie.attackComponent.alive = true;
				zombie.ChangeLine();
			}
			if (GodotObject.IsInstanceValid(zombie))
			{
				zombie.isGarlicBird = false;
			}
			if (GodotObject.IsInstanceValid(timer))
			{
				timer.QueueFree();
			}
		};
		timer.Start();
	}

	private static bool IsSuanNiaoImmune(TowerDefenseZombie zombie)
	{
		return !GodotObject.IsInstanceValid(zombie.instance);
	}

	public override void DestroySet()
	{
		if (!_over)
		{
			_over = true;
			base.DestroySet();
			instance.invincible = true;
			Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
			float num = (float)(TowerDefenseManager.Instance.GetMapGroundRight() + 200.0);
			float num2 = num - logicalGlobalPosition.X;
			double duration = Mathf.Max(2.0, (double)num2 / 200.0);
			SceneTree tree = GetTree();
			Node2D node2D = new Node2D();
			node2D.Name = "GarlicBirdDeathrattle";
			node2D.Scale = base.Scale;
			node2D.ZIndex = base.ZIndex;
			TowerDefenseManager.GetCharacterNode().AddChild(node2D, forceReadableName: false, InternalMode.Disabled);
			AdobeAnimateSpriteBase adobeAnimateSpriteBase = GARLIC_BIRD_SCENE.Instantiate<AdobeAnimateSpriteBase>(PackedScene.GenEditState.Disabled);
			node2D.AddChild(adobeAnimateSpriteBase, forceReadableName: false, InternalMode.Disabled);
			adobeAnimateSpriteBase.SetAnimation("Fire", loop: false, 0.2);
			RunDeathrattleCharge(node2D, tree, base.gridPos.Y, base.camp, logicalGlobalPosition, num, duration);
		}
	}

	private static async void RunDeathrattleCharge(Node2D runner, SceneTree tree, int gridY, TowerDefenseEnum.CHARACTER_CAMP sourceCamp, Vector2 startPosition, float targetX, double duration)
	{
		if (!GodotObject.IsInstanceValid(runner) || !GodotObject.IsInstanceValid(tree))
		{
			return;
		}
		runner.GlobalPosition = startPosition;
		Tween chargeTween = runner.CreateTween();
		chargeTween.TweenProperty(runner, "global_position:x", targetX, duration);
		List<TowerDefenseCharacter> hitZombies = new List<TowerDefenseCharacter>();
		while (GodotObject.IsInstanceValid(runner) && chargeTween.IsValid() && chargeTween.IsRunning())
		{
			List<TowerDefenseCharacter> characterLine = TowerDefenseManager.Instance.GetCharacterLine(gridY, fliterGraveStone: false);
			float x = runner.GlobalPosition.X;
			foreach (TowerDefenseCharacter item in characterLine)
			{
				if (GodotObject.IsInstanceValid(item) && item.camp != sourceCamp && !item.nearDie && !item.die && item.HasHitBox && item.IsHitBoxMonitorable && !hitZombies.Contains(item) && Mathf.Abs(item.GetLogicalGlobalPosition().X - x) < 100f)
				{
					item.SkipInvincibleHurt(1000.0);
					ApplySuanNiaoToCharacter(item);
					hitZombies.Add(item);
				}
			}
			await runner.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
		}
		if (GodotObject.IsInstanceValid(runner))
		{
			runner.QueueFree();
		}
	}
}
