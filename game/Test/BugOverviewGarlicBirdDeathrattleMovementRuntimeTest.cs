using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewGarlicBirdDeathrattleMovementRuntimeTest : Node
{
	private const string GarlicBirdScenePath = "res://Asset/Anime/Character/Plant/Chapter8/GarlicBird/Scene/TowerDefensePlantGarlicBird.tscn";

	private const string NormalZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private const string ZamboniScenePath = "res://Asset/Anime/Character/Zombie/Chapter2/Zamboni/Scene/Normal/TowerDefenseZombieZamboni.tscn";

	private const string BossScenePath = "res://Asset/Anime/Character/Zombie/Boss/Boss/Scene/TowerDefenseZombieBoss.tscn";

	private const string BossDaveScenePath = "res://Asset/Anime/Character/Zombie/Boss/BossDave/Scene/TowerDefenseZombieBossDave.tscn";

	private const int ReverseSampleFrames = 210;

	private const float ForwardTeleportThreshold = -8f;

	private readonly List<string> _failures = new List<string>();

	private int _checks;

	public override async void _Ready()
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		bool previousUseBatch = TowerDefenseZombie.UseBatch;
		TowerDefensePlantGarlicBird garlicBird = null;
		TowerDefenseZombieNormal normalZombie = null;
		TowerDefenseZombieZamboni zamboni = null;
		TowerDefenseZombieBoss boss = null;
		TowerDefenseZombieBossDave daveBoss = null;
		try
		{
			_ = 4;
			try
			{
				Check(GodotObject.IsInstanceValid(instance), "TowerDefenseManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(instance))
				{
					goto end_IL_008b;
				}
				TowerDefenseZombie.UseBatch = true;
				garlicBird = Instantiate<TowerDefensePlantGarlicBird>("res://Asset/Anime/Character/Plant/Chapter8/GarlicBird/Scene/TowerDefensePlantGarlicBird.tscn");
				normalZombie = Instantiate<TowerDefenseZombieNormal>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn");
				zamboni = Instantiate<TowerDefenseZombieZamboni>("res://Asset/Anime/Character/Zombie/Chapter2/Zamboni/Scene/Normal/TowerDefenseZombieZamboni.tscn");
				boss = Instantiate<TowerDefenseZombieBoss>("res://Asset/Anime/Character/Zombie/Boss/Boss/Scene/TowerDefenseZombieBoss.tscn");
				daveBoss = Instantiate<TowerDefenseZombieBossDave>("res://Asset/Anime/Character/Zombie/Boss/BossDave/Scene/TowerDefenseZombieBossDave.tscn");
				Check(GodotObject.IsInstanceValid(garlicBird) && garlicBird.config?.name == "PlantGarlicBird", "The fixture must instantiate the real Garlic Bird scene.");
				Check(GodotObject.IsInstanceValid(normalZombie) && normalZombie.config?.name == "ZombieNormal", "The GroundSlot case must use a real ordinary zombie.");
				Check(GodotObject.IsInstanceValid(zamboni) && zamboni.config?.name == "ZombieZamboni", "The special-movement control must use the real Zamboni vehicle scene.");
				Check(GodotObject.IsInstanceValid(boss) && boss.config?.name == "ZombieBoss", "The boss immunity case must use the real Zomboss scene.");
				Check(GodotObject.IsInstanceValid(daveBoss) && daveBoss.config?.name == "ZombieBossDave", "The Dave boss immunity case must use the real Boss Dave scene.");
				if (!GodotObject.IsInstanceValid(garlicBird) || !GodotObject.IsInstanceValid(normalZombie) || !GodotObject.IsInstanceValid(zamboni) || !GodotObject.IsInstanceValid(boss) || !GodotObject.IsInstanceValid(daveBoss))
				{
					goto end_IL_008b;
				}
				garlicBird.inGame = false;
				normalZombie.inGame = true;
				normalZombie.editorPreviewMode = false;
				normalZombie.gridPos = new Vector2I(7, 1);
				zamboni.inGame = true;
				zamboni.editorPreviewMode = false;
				zamboni.gridPos = new Vector2I(7, 3);
				AddChild(normalZombie, forceReadableName: false, InternalMode.Disabled);
				AddChild(zamboni, forceReadableName: false, InternalMode.Disabled);
				AddChild(boss, forceReadableName: false, InternalMode.Disabled);
				AddChild(daveBoss, forceReadableName: false, InternalMode.Disabled);
				AddChild(garlicBird, forceReadableName: false, InternalMode.Disabled);
				normalZombie.GlobalPosition = new Vector2(1000f, 100f);
				zamboni.GlobalPosition = new Vector2(1000f, 300f);
				await WaitPhysicsFrames(4);
				GroundMoveComponent groundMoveComponent = normalZombie.groundMoveComponent;
				Check(groundMoveComponent != null && !groundMoveComponent.IsReleased && groundMoveComponent.HasMovementSource, "The ordinary zombie must use its authored GroundSlot movement source.");
				Check(zamboni.groundMoveComponent == null || zamboni.groundMoveComponent.IsReleased, "The Zamboni control must retain its dedicated vehicle movement instead of GroundMove.");
				Check((boss.instance.unUseBuffFlags & 0x80) != 0 && (daveBoss.instance.unUseBuffFlags & 0x80) != 0, "Both boss fixtures must advertise authored Garlic immunity before Garlic Bird is applied.");
				normalZombie.Walk();
				zamboni.Walk();
				await WaitPhysicsFrames(24);
				float normalForwardStart = normalZombie.GlobalPosition.X;
				float zamboniForwardStart = zamboni.GlobalPosition.X;
				await WaitPhysicsFrames(90);
				Check(normalZombie.GlobalPosition.X < normalForwardStart - 0.1f, $"Before Garlic Bird, the ordinary zombie must advance normally; delta={normalZombie.GlobalPosition.X - normalForwardStart:F3}.");
				Check(zamboni.GlobalPosition.X < zamboniForwardStart - 0.1f, $"Before Garlic Bird, the Zamboni must advance through its dedicated movement; delta={zamboni.GlobalPosition.X - zamboniForwardStart:F3}.");
				garlicBird.ApplySuanNiao(normalZombie);
				garlicBird.ApplySuanNiao(zamboni);
				garlicBird.ApplySuanNiao(boss);
				garlicBird.ApplySuanNiao(daveBoss);
				await WaitPhysicsFrames(2);
				Check(normalZombie.isGarlicBird && zamboni.isGarlicBird, "The real Garlic Bird deathrattle application path must mark both surviving zombies.");
				Check(!boss.isGarlicBird && !daveBoss.isGarlicBird, "Garlic Bird must respect Garlic immunity and leave both boss fixtures unmarked.");
				Check(normalZombie.sprite.playBack && zamboni.sprite.playBack, "Garlic Bird must put both real zombie sprites into reverse playback.");
				Check(!boss.sprite.playBack && !daveBoss.sprite.playBack, "Garlic Bird must not force boss root animations into reverse playback.");
				int normalReverseLoops = 0;
				normalZombie.sprite.OnAnimeCompleted += delegate
				{
					if (normalZombie.sprite.playBack)
					{
						normalReverseLoops++;
					}
				};
				float normalStartX = normalZombie.GlobalPosition.X;
				float zamboniStartX = zamboni.GlobalPosition.X;
				float previousNormalX = normalStartX;
				float previousZamboniX = zamboniStartX;
				float normalWorstForwardStep = 0f;
				float zamboniWorstForwardStep = 0f;
				float normalLargestRetreatStep = 0f;
				float zamboniLargestRetreatStep = 0f;
				int normalForwardTeleportCount = 0;
				int zamboniForwardTeleportCount = 0;
				for (int frame = 0; frame < 210; frame++)
				{
					await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
					float num = normalZombie.GlobalPosition.X - previousNormalX;
					float num2 = zamboni.GlobalPosition.X - previousZamboniX;
					normalWorstForwardStep = Mathf.Min(normalWorstForwardStep, num);
					zamboniWorstForwardStep = Mathf.Min(zamboniWorstForwardStep, num2);
					normalLargestRetreatStep = Mathf.Max(normalLargestRetreatStep, num);
					zamboniLargestRetreatStep = Mathf.Max(zamboniLargestRetreatStep, num2);
					if (num < -8f)
					{
						normalForwardTeleportCount++;
					}
					if (num2 < -8f)
					{
						zamboniForwardTeleportCount++;
					}
					previousNormalX = normalZombie.GlobalPosition.X;
					previousZamboniX = zamboni.GlobalPosition.X;
				}
				Check(normalZombie.GlobalPosition.X > normalStartX + 0.1f && normalLargestRetreatStep > 0.01f, $"Garlic Bird must make the ordinary zombie retreat continuously; delta={normalZombie.GlobalPosition.X - normalStartX:F3}, largestStep={normalLargestRetreatStep:F3}.");
				Check(normalReverseLoops >= 2, $"The ordinary-zombie sample must cross real reverse-loop boundaries; loops={normalReverseLoops}.");
				Check(normalForwardTeleportCount == 0, $"Garlic Bird reverse loops must not teleport the ordinary zombie toward the house; count={normalForwardTeleportCount}, worstStep={normalWorstForwardStep:F3}.");
				Check(zamboni.GlobalPosition.X > zamboniStartX + 0.1f && zamboniLargestRetreatStep > 0.01f, $"The Zamboni special-movement control must retreat continuously; delta={zamboni.GlobalPosition.X - zamboniStartX:F3}, largestStep={zamboniLargestRetreatStep:F3}.");
				Check(zamboniForwardTeleportCount == 0, $"Garlic Bird must not create a forward jump in the Zamboni control; count={zamboniForwardTeleportCount}, worstStep={zamboniWorstForwardStep:F3}.");
				goto end_IL_006c;
				end_IL_008b:;
			}
			catch (Exception value)
			{
				_failures.Add($"Unexpected runtime exception: {value}");
				goto end_IL_006c;
			}
			return;
			end_IL_006c:;
		}
		finally
		{
			TowerDefenseZombie.UseBatch = previousUseBatch;
			if (GodotObject.IsInstanceValid(garlicBird))
			{
				garlicBird.QueueFree();
			}
			if (GodotObject.IsInstanceValid(normalZombie))
			{
				normalZombie.QueueFree();
			}
			if (GodotObject.IsInstanceValid(zamboni))
			{
				zamboni.QueueFree();
			}
			if (GodotObject.IsInstanceValid(boss))
			{
				boss.QueueFree();
			}
			if (GodotObject.IsInstanceValid(daveBoss))
			{
				daveBoss.QueueFree();
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		Finish();
	}

	private static T Instantiate<T>(string path) where T : Node
	{
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Ignore);
		if (packedScene == null)
		{
			return null;
		}
		return packedScene.Instantiate<T>(PackedScene.GenEditState.Disabled);
	}

	private async Task WaitPhysicsFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures.Add(message);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PushError("[GarlicBirdDeathrattleMovement] " + failure);
		}
		bool flag = _failures.Count == 0 && _checks == 20;
		GD.Print($"GARLIC_BIRD_DEATHRATTLE_MOVEMENT_RESULT passed={flag} checks={_checks} failures={_failures.Count}");
		GetTree().Quit((!flag) ? 2 : 0);
	}
}
