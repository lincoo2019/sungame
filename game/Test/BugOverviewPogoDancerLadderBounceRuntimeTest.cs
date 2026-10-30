using System;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewPogoDancerLadderBounceRuntimeTest : Node
{
	private const string PogoDancerScenePath = "res://Asset/Anime/Character/Zombie/Chapter5/PogoDancer/Scene/TowerDefenseZombiePogoDancer.tscn";

	private const float PhysicsDelta = 1f / 60f;

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseZombiePogoDancer pogoDancer = null;
		TowerDefenseCharacter ladder = null;
		PackedScene scene = null;
		try
		{
			try
			{
				scene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter5/PogoDancer/Scene/TowerDefenseZombiePogoDancer.tscn", null, ResourceLoader.CacheMode.Ignore);
				Check(GodotObject.IsInstanceValid(scene), "The real Pogo Dancer scene must load.");
				pogoDancer = scene?.Instantiate<TowerDefenseZombiePogoDancer>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(pogoDancer), "The real Pogo Dancer scene must instantiate its production script.");
				if (!GodotObject.IsInstanceValid(pogoDancer))
				{
					goto end_IL_0045;
				}
				pogoDancer.editorPreviewMode = true;
				pogoDancer.inGame = false;
				AddChild(pogoDancer, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(4);
				pogoDancer.ProcessMode = ProcessModeEnum.Disabled;
				BugOverviewPogoDancerLadderBounceRuntimeTest bugOverviewPogoDancerLadderBounceRuntimeTest = this;
				int condition;
				if (pogoDancer.IsNodeReady())
				{
					GroundHeightComponent groundHeightComponent = pogoDancer.groundHeightComponent;
					condition = ((groundHeightComponent != null && groundHeightComponent.Lifecycle == ComponentRuntimeLifecycle.Active) ? 1 : 0);
				}
				else
				{
					condition = 0;
				}
				bugOverviewPogoDancerLadderBounceRuntimeTest.Check((byte)condition != 0, "The real Pogo Dancer ground-height runtime must be active.");
				Check(pogoDancer.hasPogo && pogoDancer.ySpeed < 0.0, "The authored Pogo Dancer must begin with an active pogo bounce.");
				ladder = new TowerDefenseCharacter();
				TowerDefenseCellInstance towerDefenseCellInstance = new TowerDefenseCellInstance
				{
					gridPos = new Vector2I(4, 2),
					characterLadder = ladder
				};
				towerDefenseCellInstance.Init(new TowerDefenseCellConfig());
				towerDefenseCellInstance.characterLadder = ladder;
				pogoDancer.cell = towerDefenseCellInstance;
				pogoDancer.cellPercentage = 0.75;
				PrepareDescendingBounce(pogoDancer);
				pogoDancer.groundHeightComponent.DetectEnvironment();
				Check(pogoDancer.groundHeightComponent.onLadder, "The production ground-height runtime must detect the ladder.");
				pogoDancer.groundHeightComponent.BatchUpdate(0.5);
				Check(Mathf.IsEqualApprox((float)pogoDancer.groundHeight, pogoDancer.groundHeightComponent.ladderHeight) && pogoDancer.z < pogoDancer.groundHeight, "The ladder must rise through the descending Pogo Dancer before landing resolution.");
				pogoDancer.PhysiceUpdate(1f / 60f);
				Check(!pogoDancer.isGround && pogoDancer.ySpeed < 0.0 && Mathf.IsEqualApprox((float)pogoDancer.z, (float)pogoDancer.groundHeight), "A ladder-raised landing must invoke Pogo Dancer's landing callback and relaunch it.");
				pogoDancer.PhysiceUpdate(1f / 60f);
				Check(pogoDancer.z > pogoDancer.groundHeight, "The relaunched Pogo Dancer must visibly leave the ladder height.");
				towerDefenseCellInstance.characterLadder = null;
				pogoDancer.groundHeightComponent.DetectEnvironment();
				pogoDancer.groundHeightComponent.BatchUpdate(0.5);
				Check(!pogoDancer.groundHeightComponent.onLadder && Mathf.IsEqualApprox((float)pogoDancer.groundHeight, 0f), "The fixture must leave the ladder before testing repeated reuse.");
				towerDefenseCellInstance.characterLadder = ladder;
				PrepareDescendingBounce(pogoDancer);
				pogoDancer.groundHeightComponent.DetectEnvironment();
				pogoDancer.groundHeightComponent.BatchUpdate(0.5);
				pogoDancer.PhysiceUpdate(1f / 60f);
				Check(pogoDancer.groundHeightComponent.onLadder && !pogoDancer.isGround && pogoDancer.ySpeed < 0.0, "A second ladder ascent must relaunch the same Pogo Dancer again.");
				pogoDancer.hasPogo = false;
				towerDefenseCellInstance.characterLadder = null;
				pogoDancer.groundHeightComponent.DetectEnvironment();
				pogoDancer.groundHeightComponent.BatchUpdate(0.5);
				towerDefenseCellInstance.characterLadder = ladder;
				PrepareDescendingBounce(pogoDancer);
				pogoDancer.groundHeightComponent.DetectEnvironment();
				pogoDancer.groundHeightComponent.BatchUpdate(0.5);
				pogoDancer.PhysiceUpdate(1f / 60f);
				Check(pogoDancer.isGround && Mathf.IsEqualApprox((float)pogoDancer.ySpeed, 0f), "A Pogo Dancer without its pogo must settle on the ladder instead of relaunching.");
				goto end_IL_003c;
				end_IL_0045:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewPogoDancerLadderBounceRuntimeTest] Unexpected exception: {value}");
				goto end_IL_003c;
			}
			return;
			end_IL_003c:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(pogoDancer) && !pogoDancer.IsQueuedForDeletion())
			{
				pogoDancer.QueueFree();
			}
			if (GodotObject.IsInstanceValid(ladder))
			{
				ladder.Free();
			}
			await WaitFrames(4);
			scene?.Dispose();
			TowerDefenseGroundItemBase.ClearStaticBattleReferences();
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
		bool flag = _failures == 0 && _checks == 11;
		GD.Print($"POGO_DANCER_LADDER_BOUNCE_RESULT version=1 passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static void PrepareDescendingBounce(TowerDefenseZombiePogoDancer pogoDancer)
	{
		pogoDancer.groundHeight = 0.0;
		pogoDancer.z = 20.0;
		pogoDancer.isGround = false;
		pogoDancer.ySpeed = 120.0;
		pogoDancer.gravityUse = true;
		pogoDancer.gravity = 490.0;
		pogoDancer.gravityScale = 1.5;
		pogoDancer.pogoPlant = false;
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BugOverviewPogoDancerLadderBounceRuntimeTest] " + message);
		}
	}
}
