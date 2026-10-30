using System;
using System.Reflection;
using System.Threading.Tasks;
using Godot;

public partial class BungiHypnotistLandingTargetabilityRuntimeTest : Node
{
	private const string BungiScenePath = "res://Asset/Anime/Character/Zombie/Chapter5/Bungi/Scene/TowerDefenseZombieBungi.tscn";

	private const string HypnotistScenePath = "res://Asset/Anime/Character/Zombie/Chapter5/Hypnotist/Scene/TowerDefenseZombieHypnotist.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		BungiHypnotistLandingTargetabilityRuntimeControlStub control = null;
		TowerDefenseZombieBungi bungi = null;
		TowerDefenseZombieHypnotist hypnotist = null;
		try
		{
			_ = 2;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager))
				{
					goto end_IL_0087;
				}
				control = new BungiHypnotistLandingTargetabilityRuntimeControlStub
				{
					Name = "BungiHypnotistLandingTargetabilityRuntimeControl",
					isGameRunning = true,
					isInit = true,
					levelConfig = new TowerDefenseLevelConfig()
				};
				AddChild(control, forceReadableName: false, InternalMode.Disabled);
				control.characterNode = new Node2D
				{
					Name = "CharacterNode"
				};
				control.AddChild(control.characterNode, forceReadableName: false, InternalMode.Disabled);
				manager.currentControl = control;
				bungi = Instantiate<TowerDefenseZombieBungi>("res://Asset/Anime/Character/Zombie/Chapter5/Bungi/Scene/TowerDefenseZombieBungi.tscn");
				hypnotist = Instantiate<TowerDefenseZombieHypnotist>("res://Asset/Anime/Character/Zombie/Chapter5/Hypnotist/Scene/TowerDefenseZombieHypnotist.tscn");
				Check(GodotObject.IsInstanceValid(bungi) && bungi.config?.name == "ZombieBungi" && GodotObject.IsInstanceValid(hypnotist) && hypnotist.config?.name == "ZombieHypnotist", "The fixture must instantiate the real Bungee and Hypnotist scenes.");
				if (!GodotObject.IsInstanceValid(bungi) || !GodotObject.IsInstanceValid(hypnotist))
				{
					goto end_IL_0087;
				}
				PrepareCharacter(bungi, new Vector2I(3, 2));
				PrepareCharacter(hypnotist, new Vector2I(5, 2));
				control.characterNode.AddChild(bungi, forceReadableName: false, InternalMode.Disabled);
				control.characterNode.AddChild(hypnotist, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(4);
				bungi.ProcessMode = ProcessModeEnum.Disabled;
				hypnotist.ProcessMode = ProcessModeEnum.Disabled;
				VerifyUntargetable(bungi, "An ordinary Bungee must begin outside targeting before landing.");
				VerifyUntargetable(hypnotist, "A Hypnotist must begin outside targeting before landing.");
				bungi.isGround = true;
				InvokeTargetabilityTransition(bungi, "ApplyLandedTargetability");
				VerifyGroundTargetable(bungi, "An ordinary Bungee must enter ground targeting after landing.");
				bungi.skipBungeeTarget = true;
				bungi.instance.hypnoses = true;
				InvokeTargetabilityTransition(bungi, "ApplyLandedTargetability");
				VerifyUntargetable(bungi, "The protected hypnotized Bungee must remain untargetable after landing.");
				TowerDefenseCellInstance towerDefenseCellInstance = new TowerDefenseCellInstance
				{
					gridPos = hypnotist.gridPos
				};
				towerDefenseCellInstance.Init(new TowerDefenseCellConfig());
				towerDefenseCellInstance.characterList.Add(hypnotist);
				hypnotist.cell = towerDefenseCellInstance;
				hypnotist.ProcessMode = ProcessModeEnum.Always;
				hypnotist.Walk();
				await WaitFrames(2);
				VerifyUntargetable(hypnotist, "A descending Hypnotist must remain outside every target channel.");
				Check(await WaitUntil(() => GodotObject.IsInstanceValid(hypnotist) && hypnotist.waitGrab, 300), "The real Hypnotist drop tween must reach its landed wait state.");
				VerifyGroundTargetable(hypnotist, "A Hypnotist must enter ground targeting only after its real drop tween lands.");
				goto end_IL_0070;
				end_IL_0087:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BungiHypnotistLandingTargetabilityRuntimeTest] Unexpected exception: {value}");
				goto end_IL_0070;
			}
			return;
			end_IL_0070:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
			}
			if (GodotObject.IsInstanceValid(hypnotist))
			{
				hypnotist.QueueFree();
			}
			if (GodotObject.IsInstanceValid(bungi))
			{
				bungi.QueueFree();
			}
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			await WaitFrames(4);
		}
		bool flag = _failures == 0 && _checks >= 8;
		GD.Print($"BUNGI_HYPNOTIST_TARGETABILITY_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
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

	private static void PrepareCharacter(TowerDefenseCharacter character, Vector2I grid)
	{
		character.editorPreviewMode = false;
		character.inGame = true;
		character.gridPos = grid;
		character.SetLogicalGlobalPosition(new Vector2((float)grid.X * 100f, (float)grid.Y * 76f));
	}

	private static void InvokeTargetabilityTransition(TowerDefenseCharacter character, string methodName)
	{
		MethodInfo? method = character.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
		if (method == null)
		{
			throw new MissingMethodException(character.GetType().Name, methodName);
		}
		method.Invoke(character, null);
	}

	private void VerifyUntargetable(TowerDefenseCharacter character, string message)
	{
		Check(character.HasHitBox && !character.IsHitBoxEnabled && !character.IsHitBoxMonitorable && character.instance.invincible && !character.instance.canBeCollection && !character.targetRegistrationComponent.canProjectileCheck && character.instance.maskFlags == 0, $"{message} enabled={character.IsHitBoxEnabled}, monitorable={character.IsHitBoxMonitorable}, invincible={character.instance.invincible}, collection={character.instance.canBeCollection}, projectile={character.targetRegistrationComponent.canProjectileCheck}, mask={character.instance.maskFlags}.");
	}

	private void VerifyGroundTargetable(TowerDefenseCharacter character, string message)
	{
		int num = 1;
		Check(character.HasHitBox && character.IsHitBoxEnabled && character.IsHitBoxMonitorable && !character.instance.invincible && character.instance.canBeCollection && character.targetRegistrationComponent.canProjectileCheck && character.instance.maskFlags == num, $"{message} enabled={character.IsHitBoxEnabled}, monitorable={character.IsHitBoxMonitorable}, invincible={character.instance.invincible}, collection={character.instance.canBeCollection}, projectile={character.targetRegistrationComponent.canProjectileCheck}, mask={character.instance.maskFlags}, expected={num}.");
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private async Task<bool> WaitUntil(Func<bool> predicate, int maximumFrames)
	{
		for (int frame = 0; frame < maximumFrames; frame++)
		{
			if (predicate())
			{
				return true;
			}
			await WaitFrames(1);
		}
		return predicate();
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BungiHypnotistLandingTargetabilityRuntimeTest] " + message);
		}
	}
}
