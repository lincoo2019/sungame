using System;
using System.Threading.Tasks;
using Godot;

public partial class DismemberNodeFreeRuntimeTest : Node
{
	private const string ZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/PeaShooterSingle/TowerDefenseZombieNormalPeaShooterSingle.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		DismemberNodeFreeRuntimeControlStub control = null;
		TowerDefenseZombieNormalPeaShooterSingle zombie = null;
		try
		{
			_ = 2;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ObjectManager.Instance), "ObjectManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ObjectManager.Instance))
				{
					goto end_IL_00d3;
				}
				control = new DismemberNodeFreeRuntimeControlStub
				{
					Name = "DismemberNodeFreeRuntimeControl",
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
				manager.gridBeginPos = Vector2.Zero;
				manager.gridSize = new Vector2(100f, 76f);
				manager.gridNum = new Vector2I(9, 5);
				zombie = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/PeaShooterSingle/TowerDefenseZombieNormalPeaShooterSingle.tscn", null, ResourceLoader.CacheMode.Ignore)?.Instantiate<TowerDefenseZombieNormalPeaShooterSingle>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(zombie), "The production zombie scene must instantiate.");
				if (!GodotObject.IsInstanceValid(zombie))
				{
					goto end_IL_00d3;
				}
				zombie.editorPreviewMode = false;
				zombie.inGame = true;
				zombie.gridPos = new Vector2I(5, 3);
				zombie.GlobalPosition = new Vector2(500f, 228f);
				control.characterNode.AddChild(zombie, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(8);
				zombie.Walk();
				await WaitFrames(4);
				Check(GodotObject.IsInstanceValid(zombie.sprite) && zombie.sprite.Visible, "The production zombie animation must be live before dismembering.");
				DismemberComponent.InjectToCharacter(zombie);
				DismemberComponent dismember = zombie.dismemberComponent;
				Check(dismember != null && !dismember.IsReleased && zombie.GetNodeOrNull<Node>("ComponentManager/DismemberComponent") == null, "Dismember must be a node-free component runtime.");
				if (dismember == null || dismember.IsReleased)
				{
					goto end_IL_00d3;
				}
				DamagePartBatcher batcher = DamagePartBatcher.GetOrCreate();
				Check(GodotObject.IsInstanceValid(batcher), "The shared retained-RID DamagePart batcher must be available.");
				if (!GodotObject.IsInstanceValid(batcher))
				{
					goto end_IL_00d3;
				}
				int activeBefore = batcher.ActiveCount;
				int nodesBefore = GetTree().GetNodeCount();
				dismember.fadeDuration = 2f;
				dismember.Dismember();
				await WaitFrames(3);
				Check(dismember._dismembered && batcher.ActiveCount > activeBefore && batcher.VisualRidCountForTest > 0, "Dismember must publish visible body layers directly into retained RIDs.");
				Check(GetTree().GetNodeCount() <= nodesBefore && CountLegacyDamagePartDrops(control.characterNode) == 0, "Dismember must add zero per-layer Nodes to the SceneTree.");
				Check(!zombie.sprite.Visible, $"The original body must remain hidden after the RID fragments publish; visible={zombie.sprite.Visible}.");
				Check(batcher.ActiveLandingOffsetsWithinForTest(2000f), "Every retained dismember fragment must keep a finite battlefield landing plane.");
				goto end_IL_00bc;
				end_IL_00d3:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[DismemberNodeFreeRuntimeTest] Unexpected exception: {value}");
				goto end_IL_00bc;
			}
			return;
			end_IL_00bc:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(zombie))
			{
				zombie.QueueFree();
			}
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
			}
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			await WaitFrames(3);
			if (GodotObject.IsInstanceValid(ObjectManager.Instance))
			{
				ObjectManager.Instance.Clear();
			}
			await WaitFrames(2);
		}
		bool flag = _failures == 0 && _checks == 10;
		GD.Print($"DISMEMBER_NODE_FREE_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static int CountLegacyDamagePartDrops(Node parent)
	{
		if (!GodotObject.IsInstanceValid(parent))
		{
			return 0;
		}
		int num = 0;
		foreach (Node child in parent.GetChildren())
		{
			if (child is DamagePartDrop)
			{
				num++;
			}
		}
		return num;
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
			GD.PushError("[DismemberNodeFreeRuntimeTest] " + message);
		}
	}
}
