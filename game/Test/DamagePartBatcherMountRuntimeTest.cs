using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class DamagePartBatcherMountRuntimeTest : Node
{
	private const string ResultMarker = "DAMAGE_PART_BATCHER_MOUNT_RESULT";

	private const string ConeTexturePath = "res://Asset/AtlasSource/Armor/Texture/Character/Armor/Cone/ZombieCone1.png";

	private const string BlackHelmetReplacementPath = "res://Asset/AtlasSource/Armor/Texture/Character/Armor/BlackHelmet/ZombieBlackHelmet1.png";

	private const string FootballImpAnimePath = "res://Asset/Anime/Character/Zombie/Challenge/FootballImp/ZombieFootballImp.tres";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBeginPosition = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridCount = manager?.gridNum ?? Vector2I.Zero;
		DamagePartBatcherMountRuntimeControlStub control = null;
		try
		{
			_ = 1;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager 自动加载必须可用。");
				Require(GodotObject.IsInstanceValid(manager), "TowerDefenseManager 自动加载不可用。");
				control = new DamagePartBatcherMountRuntimeControlStub
				{
					Name = "DamagePartBatcherMountControl",
					isGameRunning = true,
					isInit = true
				};
				AddChild(control, forceReadableName: false, InternalMode.Disabled);
				CanvasLayer canvasLayer = new CanvasLayer
				{
					Name = "OldCharacterLayer",
					Layer = 0
				};
				Node2D node2D = new Node2D
				{
					Name = "OldCharacterNode"
				};
				control.AddChild(canvasLayer, forceReadableName: false, InternalMode.Disabled);
				canvasLayer.AddChild(node2D, forceReadableName: false, InternalMode.Disabled);
				control.characterNode = node2D;
				manager.currentControl = control;
				manager.gridBeginPos = Vector2.Zero;
				manager.gridSize = new Vector2(100f, 100f);
				manager.gridNum = new Vector2I(9, 5);
				DamagePartBatcher oldBatcher = DamagePartBatcher.GetOrCreate();
				Check(GodotObject.IsInstanceValid(oldBatcher) && oldBatcher.GetParent() == node2D && ResolveCanvasLayer(oldBatcher) == 0, "首个批处理器必须挂在旧角色画布。");
				CanvasLayer canvasLayer2 = new CanvasLayer
				{
					Name = "CurrentCharacterLayer",
					Layer = 1
				};
				Node2D node2D2 = new Node2D
				{
					Name = "CurrentCharacterNode"
				};
				control.AddChild(canvasLayer2, forceReadableName: false, InternalMode.Disabled);
				canvasLayer2.AddChild(node2D2, forceReadableName: false, InternalMode.Disabled);
				control.characterNode = node2D2;
				DamagePartBatcher currentBatcher = DamagePartBatcher.GetOrCreate();
				Check(GodotObject.IsInstanceValid(currentBatcher) && currentBatcher != oldBatcher && currentBatcher.GetParent() == node2D2, "角色挂点变化后必须在当前 CharacterNode 下重建批处理器。");
				Check(GodotObject.IsInstanceValid(oldBatcher) && oldBatcher.IsQueuedForDeletion(), "旧画布中的批处理器必须进入释放队列。");
				Check(ResolveCanvasLayer(currentBatcher) == 1 && currentBatcher.GetViewport() == node2D2.GetViewport(), "新批处理器必须继承当前角色 CanvasLayer 和 Viewport。");
				Check(DamagePartBatcher.GetOrCreate() == currentBatcher, "相同角色挂点内必须继续复用当前批处理器。");
				bool flag = currentBatcher.TrySpawnExternalAtlas("res://Asset/AtlasSource/Armor/Texture/Character/Armor/Cone/ZombieCone1.png", centered: true, Transform2D.Identity, Colors.White, new Transform2D(0f, new Vector2(320f, 220f)), 80.0, new Vector2(60f, -300f), new Vector2I(-1, -1));
				Check(flag && currentBatcher.ActiveCount == 1 && currentBatcher.VisualRidCountForTest == 1, "重建后的批处理器必须仍能提交可见路障 RID。");
				int num = 52;
				int activeRenderZIndexForTest = currentBatcher.GetActiveRenderZIndexForTest(0);
				Check(activeRenderZIndexForTest == num + 1 && activeRenderZIndexForTest == 53, "无效角色格坐标必须按落地点补算第 3 行，并把路障放入高于僵尸一档的 DAMAGEPART Z 分桶。");
				int num2 = DamagePartBatcher.ResolveRenderZIndexForTest(new Vector2I(-2147483648, -2147483648));
				Check(num2 == -4095 && num2 > -4096, "无效地图行被夹取时，路障必须保留在最低僵尸层 -4096 之上的 -4095 层。");
				AdobeAnimateData adobeAnimateData = ResourceLoader.Load<AdobeAnimateData>("res://Asset/Anime/Character/Zombie/Challenge/FootballImp/ZombieFootballImp.tres", null, ResourceLoader.CacheMode.Reuse);
				AdobeAnimateExternalTextureAtlasAllocation blackHelmetAllocation;
				bool flag2 = AdobeAnimateGlobalAtlasCache.TryGetReplaceTextureAllocation("res://Asset/AtlasSource/Armor/Texture/Character/Armor/BlackHelmet/ZombieBlackHelmet1.png", out blackHelmetAllocation);
				Check(GodotObject.IsInstanceValid(adobeAnimateData) && flag2 && blackHelmetAllocation.UsesTextureArray && blackHelmetAllocation.TextureArrayRid.IsValid, "黑橄榄小鬼头盔替换图必须存在于共享纹理数组分配中。");
				Array<Texture2D> array = new Array<Texture2D>();
				array.Resize(2);
				Array<string> array2 = new Array<string>();
				array2.Resize(2);
				array2[1] = "res://Asset/AtlasSource/Armor/Texture/Character/Armor/BlackHelmet/ZombieBlackHelmet1.png";
				bool flag3 = currentBatcher.TrySpawnAdobeMedia(adobeAnimateData, 1, Transform2D.Identity, Colors.White, array, array2, new Transform2D(0f, new Vector2(420f, 220f)), 80.0, new Vector2(60f, -300f), new Vector2I(0, 3));
				Check(flag3 && currentBatcher.ActiveCount == 2 && currentBatcher.VisualRidCountForTest == 2, "黑橄榄小鬼头盔替换图必须作为共享图集绘制 RID 提交，不能退化成白块纹理。");
				await WaitFrames(2);
				Check(!GodotObject.IsInstanceValid(oldBatcher), "旧画布批处理器必须在帧结束后完成释放。");
				bool flag4 = await WaitForDamagePartSettled(currentBatcher, 1, 120);
				Check(currentBatcher.ActiveCount == 2, "黑橄榄小鬼头盔进入落地停留阶段时必须仍处于活动批次中。");
				Check(flag4 && currentBatcher.IsActiveSettledForTest(1), "黑橄榄小鬼头盔必须真实进入落地停留状态后再检查画面。");
				Check(currentBatcher.HasArrayTextureBindingForTest(blackHelmetAllocation.TextureArray), "黑橄榄小鬼头盔落地停留后必须继续持有共享纹理数组材质，不能在落地时变成白块。");
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[{"DamagePartBatcherMountRuntimeTest"}] 意外异常：{value}");
			}
		}
		finally
		{
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBeginPosition;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridCount;
			}
			if (GodotObject.IsInstanceValid(control) && !control.IsQueuedForDeletion())
			{
				control.QueueFree();
			}
			await WaitFrames(3);
		}
		bool flag5 = _checks == 15 && _failures == 0;
		GD.Print($"{"DAMAGE_PART_BATCHER_MOUNT_RESULT"} passed={flag5} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag5) ? 2 : 0);
	}

	private static int ResolveCanvasLayer(Node node)
	{
		Node node2 = node;
		while (GodotObject.IsInstanceValid(node2))
		{
			if (node2 is CanvasLayer canvasLayer)
			{
				return canvasLayer.Layer;
			}
			node2 = node2.GetParent();
		}
		return -2147483648;
	}

	private async Task WaitFrames(int count)
	{
		for (int index = 0; index < count; index++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private async Task<bool> WaitForDamagePartSettled(DamagePartBatcher batcher, int index, int maximumPhysicsFrames)
	{
		for (int frame = 0; frame < maximumPhysicsFrames; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			if (GodotObject.IsInstanceValid(batcher) && batcher.IsActiveSettledForTest(index))
			{
				return true;
			}
		}
		return false;
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[DamagePartBatcherMountRuntimeTest] " + message);
		}
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
