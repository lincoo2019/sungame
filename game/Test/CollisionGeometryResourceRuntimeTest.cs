using System;
using Godot;
using Godot.Collections;

public partial class CollisionGeometryResourceRuntimeTest : Node
{
	private int _checks;

	private int _failures;

	public override void _Ready()
	{
		RunShapeParity(new RectangleShape2D
		{
			Size = new Vector2(90f, 44f)
		}, "rectangle");
		RunShapeParity(new CircleShape2D
		{
			Radius = 37f
		}, "circle");
		RunShapeParity(new SegmentShape2D
		{
			A = new Vector2(-15f, 8f),
			B = new Vector2(170f, -12f)
		}, "segment");
		RunCompoundShapeUnion();
		RunAreaRuntimeSegmentOverride();
		RunAreaRuntimeShapeTransformOverride();
		RunAreaRuntimeRectangleSizeOverride();
		RunRayParity();
		RunMigratedSceneChecks();
		RunSingleRayBatchSceneChecks();
		RunRemainingSingleRaySceneChecks();
		RunStaticMultiRayBatchChecks();
		RunDynamicMultiRaySceneChecks();
		RunFogCircleShapeSceneChecks();
		RunContactRectangleShapeSceneChecks();
		RunChangeProjectileRectangleShapeSceneChecks();
		RunCompressionRectangleShapeSceneChecks();
		RunDirectAreaRectangleShapeSceneChecks();
		RunStaticSegmentShapeSceneChecks();
		RunFireAreaSegmentShapeSceneChecks();
		RunPopcornpultMultiSegmentSceneChecks();
		RunDynamicMultiSegmentFireAreaSceneChecks();
		RunAttackSegmentShapeSceneChecks();
		RunRectangleShapeSceneChecks();
		bool passed = _failures == 0;
		GD.Print($"COLLISION_RESOURCE_RESULT passed={passed} checks={_checks} failures={_failures}");
		Callable.From(delegate
		{
			GetTree().Quit((!passed) ? 2 : 0);
		}).CallDeferred();
	}

	private void RunShapeParity(Shape2D geometry, string label)
	{
		AabbArea2D aabbArea2D = new AabbArea2D
		{
			Position = new Vector2(113f, -47f),
			Rotation = 0.37f,
			Scale = new Vector2(-1.4f, 0.75f)
		};
		CollisionShape2D collisionShape2D = new CollisionShape2D
		{
			Shape = geometry,
			Position = new Vector2(9f, -13f),
			Rotation = -0.21f,
			Scale = new Vector2(0.8f, 1.25f)
		};
		AddChild(aabbArea2D, forceReadableName: false, InternalMode.Disabled);
		aabbArea2D.AddChild(collisionShape2D, forceReadableName: false, InternalMode.Disabled);
		AabbArea2D aabbArea2D2 = new AabbArea2D
		{
			Position = aabbArea2D.Position,
			Rotation = aabbArea2D.Rotation,
			Scale = aabbArea2D.Scale
		};
		AabbShape2DResource item = new AabbShape2DResource
		{
			Geometry = geometry,
			LocalTransform = collisionShape2D.Transform
		};
		aabbArea2D2.ShapeResources.Add(item);
		AddChild(aabbArea2D2, forceReadableName: false, InternalMode.Disabled);
		Rect2 rect = AabbShapeUtil.ComputeAreaWorldRect(aabbArea2D);
		Rect2 rect2 = AabbShapeUtil.ComputeAreaWorldRect(aabbArea2D2);
		Check(RectApprox(rect, rect2), $"{label} Node/Resource world bounds differ: {rect} vs {rect2}");
		Check(aabbArea2D2.GetChildCount() == 0, label + " Resource area materialized a child node");
		aabbArea2D.QueueFree();
		aabbArea2D2.QueueFree();
	}

	private void RunCompoundShapeUnion()
	{
		AabbArea2D aabbArea2D = new AabbArea2D
		{
			Position = new Vector2(-30f, 60f),
			Rotation = -0.18f
		};
		AabbShape2DResource aabbShape2DResource = new AabbShape2DResource
		{
			Geometry = new RectangleShape2D
			{
				Size = new Vector2(80f, 30f)
			},
			LocalTransform = new Transform2D(0f, new Vector2(-25f, 0f))
		};
		AabbShape2DResource aabbShape2DResource2 = new AabbShape2DResource
		{
			Geometry = new CircleShape2D
			{
				Radius = 22f
			},
			LocalTransform = new Transform2D(0f, new Vector2(65f, 5f))
		};
		aabbArea2D.ShapeResources.Add(aabbShape2DResource);
		aabbArea2D.ShapeResources.Add(aabbShape2DResource2);
		AddChild(aabbArea2D, forceReadableName: false, InternalMode.Disabled);
		aabbShape2DResource.TryGetWorldRect(aabbArea2D.GlobalTransform, out var rect);
		aabbShape2DResource2.TryGetWorldRect(aabbArea2D.GlobalTransform, out var rect2);
		Rect2 left = AabbShapeUtil.Union(rect, rect2);
		Check(RectApprox(left, aabbArea2D.WorldRect), "Compound Resource shapes did not union their world bounds");
		Check(aabbArea2D.GetChildCount() == 0, "Compound Resource area materialized collision nodes");
		aabbArea2D.QueueFree();
	}

	private void RunAreaRuntimeSegmentOverride()
	{
		SegmentShape2D segmentShape2D = new SegmentShape2D
		{
			A = new Vector2(10f, 4f),
			B = new Vector2(-100f, 4f)
		};
		AabbShape2DResource item = new AabbShape2DResource
		{
			Geometry = segmentShape2D,
			LocalTransform = new Transform2D(0f, new Vector2(7f, -3f))
		};
		AabbArea2D aabbArea2D = new AabbArea2D();
		AabbArea2D aabbArea2D2 = new AabbArea2D();
		aabbArea2D.ShapeResources.Add(item);
		aabbArea2D2.ShapeResources.Add(item);
		AddChild(aabbArea2D, forceReadableName: false, InternalMode.Disabled);
		AddChild(aabbArea2D2, forceReadableName: false, InternalMode.Disabled);
		int resourceGeometryHash = aabbArea2D.GetResourceGeometryHash();
		Check(aabbArea2D.SetRuntimeSegmentLengthX(0, 250f), "Runtime segment length override was rejected");
		Check(aabbArea2D.TryGetRuntimeSegmentEnd(0, out var endpoint) && endpoint.IsEqualApprox(new Vector2(-250f, 4f)), "Runtime segment override did not preserve direction and the orthogonal endpoint");
		Vector2 endpoint2 = segmentShape2D.B;
		Check(endpoint2.IsEqualApprox(new Vector2(-100f, 4f)), "Runtime segment override mutated the shared Shape Resource");
		Check(!aabbArea2D2.TryGetRuntimeSegmentEnd(0, out endpoint2) && aabbArea2D2.WorldRect.Size.X < aabbArea2D.WorldRect.Size.X, "Runtime segment override leaked into another Area instance");
		Check(aabbArea2D.GetResourceGeometryHash() != resourceGeometryHash, "Runtime segment override did not invalidate geometry-dependent caches");
		Check(aabbArea2D.ClearRuntimeSegmentEnd(0), "Runtime segment override could not be cleared");
		Check(!aabbArea2D.TryGetRuntimeSegmentEnd(0, out endpoint2) && RectApprox(aabbArea2D.WorldRect, aabbArea2D2.WorldRect), "Clearing the runtime segment override did not restore Resource geometry");
		aabbArea2D.QueueFree();
		aabbArea2D2.QueueFree();
	}

	private void RunAreaRuntimeShapeTransformOverride()
	{
		SegmentShape2D geometry = new SegmentShape2D
		{
			B = new Vector2(100f, 0f)
		};
		AabbShape2DResource aabbShape2DResource = new AabbShape2DResource
		{
			Geometry = geometry,
			LocalTransform = new Transform2D(0f, new Vector2(5f, -70f))
		};
		AabbArea2D aabbArea2D = new AabbArea2D();
		AabbArea2D aabbArea2D2 = new AabbArea2D();
		aabbArea2D.ShapeResources.Add(aabbShape2DResource);
		aabbArea2D2.ShapeResources.Add(aabbShape2DResource);
		AddChild(aabbArea2D, forceReadableName: false, InternalMode.Disabled);
		AddChild(aabbArea2D2, forceReadableName: false, InternalMode.Disabled);
		int resourceGeometryHash = aabbArea2D.GetResourceGeometryHash();
		Check(aabbArea2D.SetRuntimeShapeLocalOriginY(0, 80f), "Runtime shape origin override was rejected");
		Check(aabbArea2D.TryGetRuntimeShapeLocalTransform(0, out var localTransform) && localTransform.Origin.IsEqualApprox(new Vector2(5f, 80f)), "Runtime shape origin override did not preserve the orthogonal coordinate");
		Check(aabbShape2DResource.LocalTransform.Origin.IsEqualApprox(new Vector2(5f, -70f)), "Runtime shape transform override mutated the shared Shape Resource");
		Check(aabbArea2D2.TryGetRuntimeShapeLocalTransform(0, out var localTransform2) && localTransform2.Origin.IsEqualApprox(new Vector2(5f, -70f)), "Runtime shape transform override leaked into another Area instance");
		Check(aabbArea2D.GetResourceGeometryHash() != resourceGeometryHash, "Runtime shape transform did not invalidate geometry caches");
		Check(aabbArea2D.SetRuntimeSegmentLengthX(0, 250f), "Runtime shape transform did not compose with segment range");
		Check(aabbArea2D.TryGetRuntimeSegmentEnd(0, out var endpoint) && endpoint.IsEqualApprox(new Vector2(250f, 0f)) && aabbArea2D.WorldRect.Position.Y > aabbArea2D2.WorldRect.Position.Y, "Runtime shape transform/range overrides did not compose in world bounds");
		Check(aabbArea2D.ClearRuntimeShapeLocalTransform(0), "Runtime shape transform override could not be cleared");
		Check(aabbArea2D.TryGetRuntimeShapeLocalTransform(0, out localTransform) && localTransform.IsEqualApprox(aabbShape2DResource.LocalTransform), "Clearing runtime shape transform did not restore Resource configuration");
		aabbArea2D.QueueFree();
		aabbArea2D2.QueueFree();
	}

	private void RunAreaRuntimeRectangleSizeOverride()
	{
		RectangleShape2D rectangleShape2D = new RectangleShape2D
		{
			Size = new Vector2(73f, 33f)
		};
		AabbShape2DResource item = new AabbShape2DResource
		{
			Geometry = rectangleShape2D,
			LocalTransform = new Transform2D(0f, new Vector2(-1.5f, -0.5f))
		};
		AabbArea2D aabbArea2D = new AabbArea2D();
		AabbArea2D aabbArea2D2 = new AabbArea2D();
		aabbArea2D.ShapeResources.Add(item);
		aabbArea2D2.ShapeResources.Add(item);
		AddChild(aabbArea2D, forceReadableName: false, InternalMode.Disabled);
		AddChild(aabbArea2D2, forceReadableName: false, InternalMode.Disabled);
		int resourceGeometryHash = aabbArea2D.GetResourceGeometryHash();
		Check(aabbArea2D.SetRuntimeRectangleWidth(0, 120f), "Runtime rectangle width override was rejected");
		Check(aabbArea2D.TryGetRuntimeRectangleSize(0, out var size) && size.IsEqualApprox(new Vector2(120f, 33f)), "Runtime rectangle width override did not preserve configured height");
		Vector2 size2 = rectangleShape2D.Size;
		Check(size2.IsEqualApprox(new Vector2(73f, 33f)), "Runtime rectangle size override mutated the shared Shape Resource");
		Check(!aabbArea2D2.TryGetRuntimeRectangleSize(0, out size2) && aabbArea2D.WorldRect.Size.X > aabbArea2D2.WorldRect.Size.X, "Runtime rectangle size override leaked into another Area instance");
		Check(aabbArea2D.GetResourceGeometryHash() != resourceGeometryHash, "Runtime rectangle size did not invalidate geometry caches");
		Check(aabbArea2D.SetRuntimeRectangleSize(0, new Vector2(180f, 90f)) && aabbArea2D.TryGetRuntimeRectangleSize(0, out size) && size.IsEqualApprox(new Vector2(180f, 90f)), "Runtime rectangle full-size override was not stored");
		Check(aabbArea2D.ClearRuntimeRectangleSize(0), "Runtime rectangle size override could not be cleared");
		Check(!aabbArea2D.TryGetRuntimeRectangleSize(0, out size2) && RectApprox(aabbArea2D.WorldRect, aabbArea2D2.WorldRect), "Clearing runtime rectangle size did not restore Resource configuration");
		aabbArea2D.QueueFree();
		aabbArea2D2.QueueFree();
	}

	private void RunRayParity()
	{
		Node2D node2D = new Node2D
		{
			Position = new Vector2(240f, 90f),
			Rotation = 0.16f,
			Scale = new Vector2(-1.1f, 0.9f)
		};
		AabbRayProbe2D aabbRayProbe2D = new AabbRayProbe2D
		{
			Position = new Vector2(17f, -23f),
			Rotation = -0.42f,
			Scale = new Vector2(0.75f, 1.2f),
			targetPosition = new Vector2(2000f, 120f)
		};
		AddChild(node2D, forceReadableName: false, InternalMode.Disabled);
		node2D.AddChild(aabbRayProbe2D, forceReadableName: false, InternalMode.Disabled);
		AabbRay2DResource aabbRay2DResource = new AabbRay2DResource
		{
			LocalTransform = aabbRayProbe2D.Transform,
			TargetPosition = aabbRayProbe2D.targetPosition
		};
		aabbRay2DResource.TryGetWorldSegment(node2D.GlobalTransform, out var origin, out var target);
		Check(origin.IsEqualApprox(aabbRayProbe2D.GlobalPosition), "Resource ray origin differs from legacy ray node");
		Check(target.IsEqualApprox(aabbRayProbe2D.GlobalTransform * aabbRayProbe2D.targetPosition), "Resource ray target differs from legacy ray node");
		FireComponent fireComponent = new FireComponent
		{
			checkRayResources = new Array<AabbRay2DResource> { aabbRay2DResource }
		};
		fireComponent.RefreshConfiguration();
		Vector2 vector = new Vector2(0f, 83f);
		Check(fireComponent.SetCheckRayLocalOrigin(0, vector), "Runtime ray origin override was rejected");
		Check(fireComponent.TryGetCheckRayLocalTransform(0, out var localTransform) && localTransform.Origin.IsEqualApprox(vector), "Runtime ray origin override was not stored per FireComponent instance");
		Check(aabbRay2DResource.LocalTransform.Origin.IsEqualApprox(aabbRayProbe2D.Transform.Origin), "Runtime ray override mutated the shared Ray Resource");
		fireComponent.RefreshConfiguration();
		Check(fireComponent.TryGetCheckRayLocalTransform(0, out localTransform) && localTransform.Origin.IsEqualApprox(vector), "Runtime ray origin override was lost after configuration refresh");
		aabbRay2DResource.TryGetWorldSegment(node2D.GlobalTransform, localTransform, aabbRay2DResource.TargetPosition, out var origin2, out var _);
		Vector2 vector2 = node2D.GlobalTransform * vector;
		Check(origin2.IsEqualApprox(vector2), $"Ray Resource query ignored the per-character runtime transform: actual={origin2} expected={vector2}");
		node2D.QueueFree();
	}

	private void RunMigratedSceneChecks()
	{
		Node node = GD.Load<PackedScene>("res://Asset/Anime/Character/Crater/CraterLava/Scene/TowerDefenseCraterLava.tscn")?.Instantiate(PackedScene.GenEditState.Disabled);
		PeriodicAreaEventComponentDefinition periodicAreaEventComponentDefinition = FindComponentDefinition<PeriodicAreaEventComponentDefinition>(node, "character.periodic_area_event");
		PeriodicAreaEventComponent periodicAreaEventComponent = FindComponentRuntime<PeriodicAreaEventComponent>(node, "character.periodic_area_event");
		Check(periodicAreaEventComponentDefinition != null && periodicAreaEventComponent != null && !periodicAreaEventComponent.IsReleased, "Migrated lava-crater Resource runtime failed to instantiate");
		Check(node?.GetNodeOrNull("ComponentManager/PeriodicAreaEventComponent/Area2D") == null, "Migrated lava-crater still materializes the legacy Area node");
		Check(periodicAreaEventComponentDefinition != null && periodicAreaEventComponentDefinition.CollisionPreviewShapeCount == 1 && periodicAreaEventComponent?.checkShape == periodicAreaEventComponentDefinition.GetCollisionPreviewShape(0), "Migrated lava-crater Shape Resource is missing from its runtime/definition");
		QueueSceneInstanceForCleanup(node);
		Node node2 = GD.Load<PackedScene>("res://Asset/Anime/Character/Plant/Cover/GatlingPea/Scene/TowerDefensePlantGatlingPea.tscn")?.Instantiate(PackedScene.GenEditState.Disabled);
		FireComponent fireComponent = FindFirstFireComponent(node2);
		Check(fireComponent != null && !fireComponent.IsReleased, "Migrated Gatling Pea FireComponent failed to instantiate");
		Check(!HasRayProbeDescendant(node2), "Migrated Gatling Pea still owns a ray node");
		Check(fireComponent != null && fireComponent.checkRayResources?.Count == 1, "Migrated Gatling Pea Ray Resource is missing");
		QueueSceneInstanceForCleanup(node2);
	}

	private void RunSingleRayBatchSceneChecks()
	{
		string[] array = new string[12]
		{
			"res://Asset/Anime/Character/Plant/Chapter0/Cactus/Scene/TowerDefensePlantCactus.tscn", "res://Asset/Anime/Character/Plant/Chapter0/PeaShooter/Scene/TowerDefensePlantPeaShooter.tscn", "res://Asset/Anime/Character/Plant/Chapter0/PeaShooterSingle/Scene/TowerDefensePlantPeaShooterSingle.tscn", "res://Asset/Anime/Character/Plant/Chapter0/PuffShroom/Scene/TowerDefensePlantPuffShroom.tscn", "res://Asset/Anime/Character/Plant/Chapter0/SeaShroom/Scene/TowerDefenseSeaShroom.tscn", "res://Asset/Anime/Character/Plant/Chapter0/SnowPea/Scene/TowerDefensePlantSnowPea.tscn", "res://Asset/Anime/Character/Plant/Chapter1/PeaPot/Scene/TowerDefensePlantPeaPot.tscn", "res://Asset/Anime/Character/Plant/Chapter1/PuffPot/Scene/TowerDefensePlantPuffPot.tscn", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/GatlingPea/TowerDefenseZombieNormalGatlingPea.tscn", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/PeaShooter/TowerDefenseZombieNormalPeaShooter.tscn",
			"res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/PeaShooterSingle/TowerDefenseZombieNormalPeaShooterSingle.tscn", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/SnowPea/TowerDefenseZombieNormalSnowPea.tscn"
		};
		foreach (string text in array)
		{
			Node node = GD.Load<PackedScene>(text)?.Instantiate(PackedScene.GenEditState.Disabled);
			FireComponent fireComponent = FindFirstFireComponent(node);
			Check(fireComponent != null && !fireComponent.IsReleased, "Single-ray scene FireComponent failed to instantiate: " + text);
			Check(fireComponent != null && fireComponent.checkRayResources?.Count == 1, "Single-ray scene Resource count changed: " + text);
			Check(!HasRayProbeDescendant(node), "Single-ray scene still materializes a ray node: " + text);
			QueueSceneInstanceForCleanup(node);
		}
	}

	private void RunRemainingSingleRaySceneChecks()
	{
		string[] array = new string[36]
		{
			"res://Asset/Anime/Character/Plant/Chapter0/ScaredyShroom/Scene/TowerDefensePlantScaredyShroom.tscn", "res://Asset/Anime/Character/Plant/Chapter1/ReCactus/Scene/TowerDefensePlantReCactus.tscn", "res://Asset/Anime/Character/Plant/Chapter1/SunflowerPea/Scene/TowerDefensePlantSunflowerPea.tscn", "res://Asset/Anime/Character/Plant/Chapter2/Icenut/Scene/TowerDefensePlantIcenut.tscn", "res://Asset/Anime/Character/Plant/Chapter2/PeaLilyPad/Scene/TowerDefensePlantPeaLilyPad.tscn", "res://Asset/Anime/Character/Plant/Chapter2/ScaredySunShroom/Scene/TowerDefensePlantScaredySunShroom.tscn", "res://Asset/Anime/Character/Plant/Chapter3/SeaNut/Scene/TowerDefensePlantSeaNut.tscn", "res://Asset/Anime/Character/Plant/Chapter4/IceSeaShroom/Scene/TowerDefenseIceSeaShroom.tscn", "res://Asset/Anime/Character/Plant/Chapter4/PumpkinSun/Scene/TowerDefensePlantPumpkinSun.tscn", "res://Asset/Anime/Character/Plant/Chapter5/Melonnut/Scene/TowerDefensePlantBowlingMelonnut.tscn",
			"res://Asset/Anime/Character/Plant/Chapter5/Melonnut/Scene/TowerDefensePlantMelonnut.tscn", "res://Asset/Anime/Character/Plant/Chapter5/SunPeashooter/Scene/TowerDefensePlantSunPeashooter.tscn", "res://Asset/Anime/Character/Plant/Chapter8/PeashooterZ/Scene/TowerDefensePlantPeashooterZ.tscn", "res://Asset/Anime/Character/Plant/Cover/GatlingPeaZ/Scene/TowerDefensePlantGatlingPeaZ.tscn", "res://Asset/Anime/Character/Plant/Cover/GatlingPot/Scene/TowerDefensePlantGatlingPot.tscn", "res://Asset/Anime/Character/Plant/Cover/IceSpearPea/Scene/TowerDefensePlantIceSpearPea.tscn", "res://Asset/Anime/Character/Plant/Cover/ThreeCactus/Scene/TowerDefensePlantThreeCactus.tscn", "res://Asset/Anime/Character/Plant/Gold/Hamburger/Scene/TowerDefensePlantHamburger.tscn", "res://Asset/Anime/Character/Plant/Gold/IceTallnut/Scene/TowerDefensePlantIceTallnut.tscn", "res://Asset/Anime/Character/Plant/Gold/SnowManPea/Scene/TowerDefensePlantSnowManPea.tscn",
			"res://Asset/Anime/Character/Plant/Gold/VIPNut/Scene/TowerDefensePlantBowlingVIPNut.tscn", "res://Asset/Anime/Character/Plant/Other/GatlingCabbage/Scene/TowerDefensePlantGatlingCabbage.tscn", "res://Asset/Anime/Character/Plant/Star/PeaVase/Scene/TowerDefensePlantPeaVase.tscn", "res://Asset/Anime/Character/Plant/Star/Robot/Scene/TowerDefensePlantRobot.tscn", "res://Asset/Anime/Character/Plant/Star/WallnutShooter/Scene/TowerDefensePlantWallnutShooter.tscn", "res://Asset/Anime/Character/Zombie/Chapter1/Imp/Scene/PeashooterSingle/TowerDefenseZombieImpPeashooterSingle.tscn", "res://Asset/Anime/Character/Zombie/Chapter1/Imp/Scene/PeashooterZ/TowerDefenseZombieImpPeashooterZ.tscn", "res://Asset/Anime/Character/Zombie/Chapter1/Imp/Scene/PuffShroom/TowerDefenseZombieImpPuffShroom.tscn", "res://Asset/Anime/Character/Zombie/Chapter1/Imp/Scene/SeaShroom/TowerDefenseZombieImpSeaShroom.tscn", "res://Asset/Anime/Character/Zombie/Chapter1/Imp/Scene/Sunpult/TowerDefenseZombieImpSunpult.tscn",
			"res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Icenut/TowerDefenseZombieNormalIcenut.tscn", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/IceTallnut/TowerDefenseZombieNormalIceTallnut.tscn", "res://Asset/Anime/Character/Zombie/Chapter2/Digger/Scene/GatlingPea/TowerDefenseZombieDiggerGatlingPea.tscn", "res://Asset/Anime/Character/Zombie/Chapter2/Zamboni/Scene/GatlingPea/TowerDefenseZombieZamboniGatlingPea.tscn", "res://Asset/Anime/Character/Zombie/Chapter3/Dolphinrider/Scene/TowerDefenseZombieDolphinriderGatlingPea.tscn", "res://Asset/Anime/Character/Zombie/Chapter7/FatGold/Scene/TowerDefenseZombieFatGold.tscn"
		};
		foreach (string text in array)
		{
			Node node = GD.Load<PackedScene>(text)?.Instantiate(PackedScene.GenEditState.Disabled);
			FireComponent fireComponent = FindFirstFireComponent(node);
			Check(fireComponent != null && !fireComponent.IsReleased, "Remaining single-ray scene FireComponent failed to instantiate: " + text);
			Check(fireComponent != null && fireComponent.checkRayResources?.Count == 1, "Remaining single-ray scene Resource count changed: " + text);
			Check(!HasRayProbeDescendant(node), "Remaining single-ray scene still materializes a ray node: " + text);
			AabbRay2DResource aabbRay2DResource = ((fireComponent != null && fireComponent.checkRayResources?.Count == 1) ? fireComponent.checkRayResources[0] : null);
			Vector2 other = (text.Contains("/Zombie/") ? new Vector2(-2000f, 0f) : new Vector2(2000f, 0f));
			Check(GodotObject.IsInstanceValid(aabbRay2DResource) && aabbRay2DResource.TargetPosition.IsEqualApprox(other), "Remaining single-ray direction changed: " + text);
			Vector2 zero = Vector2.Zero;
			Check(GodotObject.IsInstanceValid(aabbRay2DResource) && aabbRay2DResource.LocalTransform.Origin.IsEqualApprox(zero) && aabbRay2DResource.LocalTransform.X.IsEqualApprox(Vector2.Right) && aabbRay2DResource.LocalTransform.Y.IsEqualApprox(Vector2.Down), "Remaining single-ray local transform changed: " + text);
			QueueSceneInstanceForCleanup(node);
		}
	}

	private void RunStaticMultiRayBatchChecks()
	{
		Vector2 one = Vector2.One;
		Vector2 item = new Vector2((float)Math.PI * 113f / 355f, (float)Math.PI * 113f / 355f);
		Vector2 item2 = new Vector2(2000f, 0f);
		Vector2 item3 = new Vector2(-2000f, 0f);
		(Vector2, float, Vector2, Vector2)[] array = new(Vector2, float, Vector2, Vector2)[5]
		{
			(Vector2.Zero, -(float)Math.PI / 6f, one, item2),
			(Vector2.Zero, -(float)Math.PI / 2f, one, item2),
			(Vector2.Zero, (float)Math.PI, one, item2),
			(Vector2.Zero, (float)Math.PI / 2f, one, item2),
			(Vector2.Zero, (float)Math.PI / 6f, one, item2)
		};
		(Vector2, float, Vector2, Vector2)[] expected = new(Vector2, float, Vector2, Vector2)[2]
		{
			(Vector2.Zero, 0f, one, item2),
			(Vector2.Zero, 0f, one, item3)
		};
		(Vector2, float, Vector2, Vector2)[] expected2 = new(Vector2, float, Vector2, Vector2)[2]
		{
			(new Vector2(43f, 0f), 0f, one, item2),
			(new Vector2(43f, 0f), 0f, one, item3)
		};
		CheckStaticMultiRayScene("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/WildPea/TowerDefenseZombieNormalWildPea.tscn", (Vector2.Zero, 0f, one, item3), (Vector2.Zero, (float)Math.PI / 24f, one, item3), (Vector2.Zero, -(float)Math.PI / 24f, one, item3), (Vector2.Zero, (float)Math.PI / 12f, one, item3), (Vector2.Zero, -(float)Math.PI / 12f, one, item3));
		CheckStaticMultiRayScene("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Starfruit/TowerDefenseZombieNormalStarfruit.tscn", (Vector2.Zero, 2.6179938f, one, item2), (Vector2.Zero, (float)Math.PI / 2f, one, item2), (Vector2.Zero, 0f, one, item2), (Vector2.Zero, 4.712389f, one, item2), (new Vector2(0f, 3.8E-06f), 3.6651917f, one, item2));
		CheckStaticMultiRayScene("res://Asset/Anime/Character/Plant/Gold/WildPea/Scene/TowerDefensePlantWildPea.tscn", (Vector2.Zero, 0f, item, item2), (Vector2.Zero, -(float)Math.PI / 24f, item, item2), (Vector2.Zero, (float)Math.PI / 24f, one, item2), (Vector2.Zero, -(float)Math.PI / 12f, one, item2), (Vector2.Zero, (float)Math.PI / 12f, one, item2));
		CheckStaticMultiRayScene("res://Asset/Anime/Character/Plant/Colour/GoldPea/Scene/TowerDefensePlantGoldPea.tscn", (Vector2.Zero, 0f, item, item2), (Vector2.Zero, -(float)Math.PI / 12f, one, item2), (Vector2.Zero, (float)Math.PI / 12f, one, item2));
		CheckStaticMultiRayScene("res://Asset/Anime/Character/Plant/Gold/CatGatlingPea/Scene/TowerDefensePlantCatGatlingPea.tscn", (Vector2.Zero, 0f, one, item2), array[0], array[1], array[2], array[3], array[4]);
		CheckStaticMultiRayScene("res://Asset/Anime/Character/Plant/Chapter2/FireSnowPea/Scene/TowerDefensePlantFireSnowPea.tscn", expected);
		CheckStaticMultiRayScene("res://Asset/Anime/Character/Plant/Gold/HotDog/Scene/TowerDefensePlantHotDog.tscn", expected2);
		CheckStaticMultiRayScene("res://Asset/Anime/Character/Plant/Chapter7/PumpkinPea/Scene/TowerDefensePlantPumpkinPea.tscn", expected);
		CheckStaticMultiRayScene("res://Asset/Anime/Character/Plant/Chapter0/SplitPea/Scene/TowerDefensePlantSplitPea.tscn", expected);
		CheckStaticMultiRayScene("res://Asset/Anime/Character/Plant/Other/StarCaltrop/Scene/TowerDefensePlantStarCaltrop.tscn", array);
		CheckStaticMultiRayScene("res://Asset/Anime/Character/Plant/Other/PotatoStar/Scene/TowerDefensePlantPotatoStar.tscn", array);
		CheckStaticMultiRayScene("res://Asset/Anime/Character/Plant/Chapter7/FeverStar/Scene/TowerDefensePlantFeverStar.tscn", array);
		CheckStaticMultiRayScene("res://Asset/Anime/Character/Plant/Chapter0/Starfruit/Scene/TowerDefensePlantStarfruit.tscn", array);
		CheckStaticMultiRayScene("res://Asset/Anime/Character/Plant/Chapter4/SeaStar/Scene/TowerDefensePlantSeaStar.tscn", array);
	}

	private void CheckStaticMultiRayScene(string scenePath, params (Vector2 Origin, float Rotation, Vector2 Scale, Vector2 Target)[] expected)
	{
		Node node = GD.Load<PackedScene>(scenePath)?.Instantiate(PackedScene.GenEditState.Disabled);
		FireComponent fireComponent = FindFirstFireComponent(node);
		Check(fireComponent != null && !fireComponent.IsReleased, "Static multi-ray FireComponent failed to instantiate: " + scenePath);
		Check(fireComponent?.checkRayResources?.Count == expected.Length, "Static multi-ray Resource count changed: " + scenePath);
		Check(!HasRayProbeDescendant(node), "Static multi-ray scene still materializes ray nodes: " + scenePath);
		for (int i = 0; i < expected.Length; i++)
		{
			AabbRay2DResource aabbRay2DResource = ((fireComponent?.checkRayResources != null && i < fireComponent.checkRayResources.Count) ? fireComponent.checkRayResources[i] : null);
			(Vector2, float, Vector2, Vector2) tuple = expected[i];
			float num = Mathf.Cos(tuple.Item2);
			float num2 = Mathf.Sin(tuple.Item2);
			Vector2 other = new Vector2(num * tuple.Item3.X, num2 * tuple.Item3.X);
			Vector2 other2 = new Vector2((0f - num2) * tuple.Item3.Y, num * tuple.Item3.Y);
			Check(GodotObject.IsInstanceValid(aabbRay2DResource) && aabbRay2DResource.LocalTransform.Origin.IsEqualApprox(tuple.Item1) && aabbRay2DResource.LocalTransform.X.IsEqualApprox(other) && aabbRay2DResource.LocalTransform.Y.IsEqualApprox(other2) && aabbRay2DResource.TargetPosition.IsEqualApprox(tuple.Item4), $"Static multi-ray order or transform changed at index {i}: {scenePath}");
		}
		QueueSceneInstanceForCleanup(node);
	}

	private void RunDynamicMultiRaySceneChecks()
	{
		string[] array = new string[10] { "res://Asset/Anime/Character/Plant/Chapter0/ThreePeater/Scene/TowerDefensePlantThreePeater.tscn", "res://Asset/Anime/Character/Plant/Chapter5/IceCactus/Scene/TowerDefensePlantIceCactus.tscn", "res://Asset/Anime/Character/Plant/Chapter5/ThreePeaterG/Scene/TowerDefensePlantThreePeaterG.tscn", "res://Asset/Anime/Character/Plant/Chapter7/PiratePeater/Scene/TowerDefensePlantPiratePeater.tscn", "res://Asset/Anime/Character/Plant/Chapter8/ThreePeaterZ/Scene/TowerDefensePlantThreePeaterZ.tscn", "res://Asset/Anime/Character/Plant/Gold/SunCactus/Scene/TowerDefensePlantSunCactus.tscn", "res://Asset/Anime/Character/Plant/Gold/ThreePeaterU/Scene/TowerDefensePlantThreePeaterU.tscn", "res://Asset/Anime/Character/Plant/Other/StarPeater/Scene/TowerDefensePlantStarPeater.tscn", "res://Asset/Anime/Character/Plant/Other/ThreeHybirdPeater/Scene/TowerDefensePlantThreeHybirdPeater.tscn", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/ThreePeater/TowerDefenseZombieNormalThreePeater.tscn" };
		foreach (string text in array)
		{
			Node node = GD.Load<PackedScene>(text)?.Instantiate(PackedScene.GenEditState.Disabled);
			FireComponent fireComponent = FindFirstFireComponent(node);
			Check(fireComponent != null && !fireComponent.IsReleased, "Dynamic multi-ray FireComponent failed to instantiate: " + text);
			Check(fireComponent != null && fireComponent.checkRayResources?.Count == 3, "Dynamic multi-ray Resource count changed: " + text);
			Check(!HasRayProbeDescendant(node), "Dynamic multi-ray scene still materializes ray nodes: " + text);
			Vector2 other = (text.Contains("/Zombie/") ? new Vector2(-2000f, 0f) : new Vector2(2000f, 0f));
			Vector2[] array2 = ((!text.Contains("/IceCactus/") && !text.Contains("/SunCactus/")) ? new Vector2[3]
			{
				Vector2.Zero,
				new Vector2(0f, -76f),
				new Vector2(0f, 76f)
			} : new Vector2[3]
			{
				new Vector2(0f, -76f),
				Vector2.Zero,
				new Vector2(0f, 76f)
			});
			for (int j = 0; j < array2.Length; j++)
			{
				AabbRay2DResource aabbRay2DResource = ((fireComponent?.checkRayResources != null && j < fireComponent.checkRayResources.Count) ? fireComponent.checkRayResources[j] : null);
				Check(GodotObject.IsInstanceValid(aabbRay2DResource) && aabbRay2DResource.LocalTransform.Origin.IsEqualApprox(array2[j]) && aabbRay2DResource.LocalTransform.X.IsEqualApprox(Vector2.Right) && aabbRay2DResource.LocalTransform.Y.IsEqualApprox(Vector2.Down) && aabbRay2DResource.TargetPosition.IsEqualApprox(other), $"Dynamic multi-ray initial Resource transform changed at index {j}: {text}");
			}
			QueueSceneInstanceForCleanup(node);
		}
	}

	private void RunFogCircleShapeSceneChecks()
	{
		(string, string, float)[] array = new(string, string, float)[24]
		{
			("res://Asset/Anime/Character/Plant/Chapter0/Plantern/Scene/TowerDefensePlantern.tscn", "SpriteGroup/FogArea", 200f),
			("res://Asset/Anime/Character/Plant/Chapter3/PlanternSix/Scene/TowerDefensePlanternSix.tscn", "SpriteGroup/FogArea", 200f),
			("res://Asset/Anime/Character/Plant/Chapter8/PumpLantern/Scene/TowerDefensePumpLantern.tscn", "SpriteGroup/FogArea", 200f),
			("res://Resource/TowerDefense/Character/Buff/LightArea/LightArea.tscn", ".", 200f),
			("res://Asset/Anime/Character/Plant/Chapter4/PlanternTanglekelp/Scene/TowerDefensePlantPlanternTanglekelp.tscn", "SpriteGroup/FogArea", 200f),
			("res://Asset/Anime/Character/Plant/Other/MedicPlantern/Scene/TowerDefenseMedicPlantern.tscn", "SpriteGroup/FogArea", 200f),
			("res://Asset/Anime/Character/Item/MegaFire/Scene/TowerDefenseItemMegaFire.tscn", "SpriteGroup/FogArea", 74.953316f),
			("res://Asset/Anime/Character/Plant/Chapter0/Torchwood/Scene/TowerDefensePlantTorchwood.tscn", "SpriteGroup/FogArea", 74.953316f),
			("res://Asset/Anime/Character/Plant/Chapter1/Firenut/Scene/TowerDefensePlantBowlingFirenut.tscn", "SpriteGroup/FogArea", 74.953316f),
			("res://Asset/Anime/Character/Plant/Chapter1/Firenut/Scene/TowerDefensePlantFirenut.tscn", "SpriteGroup/FogArea", 74.953316f),
			("res://Asset/Anime/Character/Plant/Chapter2/FireSnowPea/Scene/TowerDefensePlantFireSnowPea.tscn", "SpriteGroup/FogArea", 74.953316f),
			("res://Asset/Anime/Character/Plant/Chapter3/PumpkinFire/Scene/TowerDefensePlantPumpkinFire.tscn", "SpriteGroup/FogArea", 74.953316f),
			("res://Asset/Anime/Character/Plant/Chapter3/Snowwood/Scene/TowerDefensePlantSnowwood.tscn", "SpriteGroup/FogArea", 74.953316f),
			("res://Asset/Anime/Character/Plant/Chapter3/SunPad/Scene/TowerDefensePlantSunPad.tscn", "SpriteGroup/FogArea", 74.953316f),
			("res://Asset/Anime/Character/Plant/Chapter5/SunPeashooter/Scene/TowerDefensePlantSunPeashooter.tscn", "SpriteGroup/TransformPoint/SunPeashooter/FogArea", 74.953316f),
			("res://Asset/Anime/Character/Plant/Chapter7/Richwood/Scene/TowerDefensePlantRichwood.tscn", "SpriteGroup/FogArea", 74.953316f),
			("res://Asset/Anime/Character/Plant/Chapter8/TorchwoodZ/Scene/TowerDefensePlantTorchwoodZ.tscn", "SpriteGroup/FogArea", 74.953316f),
			("res://Asset/Anime/Character/Plant/Cover/PumpkinFireIce/Scene/TowerDefensePlantPumpkinFireIce.tscn", "SpriteGroup/FogArea", 74.953316f),
			("res://Asset/Anime/Character/Plant/Other/TorchwoodStar/Scene/TowerDefensePlantTorchwoodStar.tscn", "SpriteGroup/FogArea", 74.953316f),
			("res://Asset/Anime/Character/Zombie/Challenge/DancerFire/Scene/TowerDefenseZombieDancerFire.tscn", "SpriteGroup/FogArea", 74.953316f),
			("res://Asset/Anime/Character/Zombie/Challenge/DiscoFire/Scene/TowerDefenseZombieDiscoFire.tscn", "SpriteGroup/FogArea", 74.953316f),
			("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Firenut/TowerDefenseZombieNormalFireNut.tscn", "SpriteGroup/TransformPoint/FogArea", 74.953316f),
			("res://Asset/Anime/Character/Plant/Cover/FireTallnut/Scene/TowerDefensePlantFireTallnut.tscn", "SpriteGroup/FogArea", 74.953316f),
			("res://Asset/Anime/Character/Plant/Gold/QueenSunFlower/Scene/TowerDefensePlantQueenSunFlower.tscn", "SpriteGroup/FogArea", 150f)
		};
		for (int i = 0; i < array.Length; i++)
		{
			(string, string, float) tuple = array[i];
			Node node = GD.Load<PackedScene>(tuple.Item1)?.Instantiate(PackedScene.GenEditState.Disabled);
			AabbArea2D aabbArea2D = node?.GetNodeOrNull<AabbArea2D>(tuple.Item2);
			CharacterAabbAreaComponentDefinition characterAabbAreaComponentDefinition = null;
			CharacterAabbAreaComponent characterAabbAreaComponent = null;
			AabbShape2DResource aabbShape2DResource;
			if (GodotObject.IsInstanceValid(aabbArea2D))
			{
				Array<AabbShape2DResource> shapeResources = aabbArea2D.ShapeResources;
				Check(shapeResources != null && shapeResources.Count == 1, "Fog/light legacy Shape Resource count changed: " + tuple.Item1);
				Check(!HasDirectCollisionShape(aabbArea2D), "Fog/light legacy area still materializes CollisionShape2D: " + tuple.Item1);
				Array<AabbShape2DResource> shapeResources2 = aabbArea2D.ShapeResources;
				aabbShape2DResource = ((shapeResources2 != null && shapeResources2.Count == 1) ? aabbArea2D.ShapeResources[0] : null);
			}
			else
			{
				characterAabbAreaComponentDefinition = FindComponentDefinitionByLegacyNodeName<CharacterAabbAreaComponentDefinition>(node, "FogArea");
				characterAabbAreaComponent = FindComponentRuntime<CharacterAabbAreaComponent>(node, characterAabbAreaComponentDefinition?.InstanceId);
				Check(characterAabbAreaComponentDefinition != null && characterAabbAreaComponent != null && !characterAabbAreaComponent.IsReleased, "Fog/light Resource runtime failed to instantiate: " + tuple.Item1);
				Check(characterAabbAreaComponentDefinition != null && characterAabbAreaComponentDefinition.CollisionPreviewShapeCount == 1 && characterAabbAreaComponent != null && characterAabbAreaComponent.ShapeCount == 1, "Fog/light runtime/definition Shape Resource count changed: " + tuple.Item1);
				Check(node?.GetNodeOrNull(tuple.Item2) == null, "Fog/light scene still materializes the legacy Area node: " + tuple.Item1);
				aabbShape2DResource = characterAabbAreaComponentDefinition?.GetCollisionPreviewShape(0);
				Check(characterAabbAreaComponent?.GetShapeResource(0) == aabbShape2DResource, "Fog/light runtime is not bound to its immutable Shape Resource: " + tuple.Item1);
			}
			CircleShape2D circleShape2D = (GodotObject.IsInstanceValid(aabbShape2DResource) ? (aabbShape2DResource.Geometry as CircleShape2D) : null);
			Check(GodotObject.IsInstanceValid(circleShape2D) && Mathf.IsEqualApprox(circleShape2D.Radius, tuple.Item3), "Fog/light circle geometry changed: " + tuple.Item1);
			Vector2 vector = new Vector2((float)Math.PI * 113f / 355f, (float)Math.PI * 113f / 355f);
			Check(GodotObject.IsInstanceValid(aabbShape2DResource) && aabbShape2DResource.LocalTransform.Origin.IsEqualApprox(Vector2.Zero) && aabbShape2DResource.LocalTransform.X.IsEqualApprox(new Vector2(vector.X, 0f)) && aabbShape2DResource.LocalTransform.Y.IsEqualApprox(new Vector2(0f, vector.Y)), "Fog/light circle local transform changed: " + tuple.Item1);
			if (characterAabbAreaComponent != null && characterAabbAreaComponentDefinition != null)
			{
				Transform2D localTransform = aabbShape2DResource.LocalTransform;
				Transform2D transform2D = localTransform;
				transform2D.Origin += new Vector2(11f, -7f);
				Check(characterAabbAreaComponent.SetRuntimeShapeLocalTransform(0, transform2D) && characterAabbAreaComponent.TryGetRuntimeShapeLocalTransform(0, out var localTransform2) && localTransform2.IsEqualApprox(transform2D) && aabbShape2DResource.LocalTransform.IsEqualApprox(localTransform), "Fog/light runtime transform override polluted shared geometry: " + tuple.Item1);
				Node2D node2D = ((characterAabbAreaComponentDefinition.anchorPath == null || characterAabbAreaComponentDefinition.anchorPath.IsEmpty) ? (node as Node2D) : node?.GetNodeOrNull<Node2D>(characterAabbAreaComponentDefinition.anchorPath));
				Rect2 rect = default(Rect2);
				bool flag = GodotObject.IsInstanceValid(node2D) && AabbShapeUtil.TryComputeShapeWorldRect(aabbShape2DResource.Geometry, node2D.GlobalTransform * characterAabbAreaComponentDefinition.localTransform * transform2D, out rect);
				Check(flag && characterAabbAreaComponent.TryGetWorldRect(out var rect2) && RectApprox(rect2, rect), "Fog/light runtime world transform changed: " + tuple.Item1);
				Check(characterAabbAreaComponent.ClearRuntimeShapeLocalTransform(0) && characterAabbAreaComponent.TryGetRuntimeShapeLocalTransform(0, out var localTransform3) && localTransform3.IsEqualApprox(localTransform), "Fog/light runtime transform override could not be restored: " + tuple.Item1);
			}
			QueueSceneInstanceForCleanup(node);
		}
	}

	private static bool HasDirectCollisionShape(Node root)
	{
		foreach (Node child in root.GetChildren())
		{
			if (child is CollisionShape2D)
			{
				return true;
			}
		}
		return false;
	}

	private void RunContactRectangleShapeSceneChecks()
	{
		(string, string, Vector2, Vector2)[] array = new(string, string, Vector2, Vector2)[30]
		{
			("res://Asset/Anime/Character/Item/Rake/Scene/TowerDefenseItemRake.tscn", "character.attack.0", new Vector2(33f, 33f), new Vector2(-4.5f, -6.5f)),
			("res://Asset/Anime/Character/Item/RakeSun/Scene/TowerDefenseItemRakeSun.tscn", "character.attack.0", new Vector2(33f, 33f), new Vector2(-4.5f, -6.5f)),
			("res://Asset/Anime/Character/Plant/Chapter0/PotatoMine/Scene/TowerDefensePlantPotatoMine.tscn", "character.attack.0", new Vector2(70f, 49.5f), new Vector2(-1f, 0.75f)),
			("res://Asset/Anime/Character/Plant/Chapter1/SunMine/Scene/TowerDefensePlantSunMine.tscn", "character.attack.0", new Vector2(70f, 49.5f), new Vector2(-1f, 0.75f)),
			("res://Asset/Anime/Character/Plant/Chapter2/MagnetMine/Scene/TowerDefensePlantMagnetMine.tscn", "character.attack.0", new Vector2(70f, 49.5f), new Vector2(-1f, 0.75f)),
			("res://Asset/Anime/Character/Plant/Cover/IceSunMine/Scene/TowerDefensePlantIceSunMine.tscn", "character.attack.0", new Vector2(70f, 49.5f), new Vector2(-1f, 0.75f)),
			("res://Asset/Anime/Character/Plant/Other/CherryMine/Scene/TowerDefensePlantCherryMine.tscn", "character.attack.0", new Vector2(70f, 49.5f), new Vector2(-1f, 0.75f)),
			("res://Asset/Anime/Character/Plant/Chapter0/Caltrop/Scene/TowerDefensePlantCaltrop.tscn", "character.attack.0", new Vector2(73f, 33f), new Vector2(-1.5f, -0.5f)),
			("res://Asset/Anime/Character/Plant/Chapter1/Corntrop/Scene/TowerDefensePlantCorntrop.tscn", "character.attack.0", new Vector2(73f, 33f), new Vector2(-1.5f, -0.5f)),
			("res://Asset/Anime/Character/Plant/Chapter3/CaltropMelon/Scene/TowerDefensePlantCaltropMelon.tscn", "character.attack.0", new Vector2(73f, 33f), new Vector2(-1.5f, -0.5f)),
			("res://Asset/Anime/Character/Plant/Chapter5/CaltropSnow/Scene/TowerDefensePlantCaltropSnow.tscn", "character.attack.0", new Vector2(73f, 33f), new Vector2(-1.5f, -0.5f)),
			("res://Asset/Anime/Character/Plant/Chapter5/PotSpike/Scene/TowerDefensePlantPotSpike.tscn", "character.attack.0", new Vector2(73f, 33f), new Vector2(-1.5f, -0.5f)),
			("res://Asset/Anime/Character/Plant/Chapter8/CaltropZ/Scene/TowerDefensePlantCaltropZ.tscn", "character.attack.0", new Vector2(73f, 33f), new Vector2(-1.5f, -0.5f)),
			("res://Asset/Anime/Character/Plant/Other/StarCaltrop/Scene/TowerDefensePlantStarCaltrop.tscn", "character.attack.0", new Vector2(73f, 33f), new Vector2(-1.5f, -0.5f)),
			("res://Asset/Anime/Character/Zombie/Challenge/Pogoball/Scene/TowerDefenseZombiePogoball.tscn", "character.attack.1", new Vector2(44f, 33f), new Vector2(4f, -1.5f)),
			("res://Asset/Anime/Character/Zombie/Chapter4/Pogo/Scene/TowerDefenseZombiePogo.tscn", "character.attack.1", new Vector2(44f, 33f), new Vector2(4f, -1.5f)),
			("res://Asset/Anime/Character/Zombie/Chapter5/Chess/Scene/TowerDefenseZombieChess.tscn", "character.attack.1", new Vector2(44f, 33f), new Vector2(4f, -1.5f)),
			("res://Asset/Anime/Character/Zombie/Chapter5/PogoDancer/Scene/TowerDefenseZombiePogoDancer.tscn", "character.attack.1", new Vector2(44f, 33f), new Vector2(4f, -1.5f)),
			("res://Asset/Anime/Character/Zombie/Chapter5/PogoJackson/Scene/TowerDefenseZombiePogoJackson.tscn", "character.attack.1", new Vector2(44f, 33f), new Vector2(4f, -1.5f)),
			("res://Asset/Anime/Character/Plant/Chapter0/Tanglekelp/Scene/TowerDefensePlantTanglekelp.tscn", "character.attack.0", new Vector2(84f, 36f), Vector2.Zero),
			("res://Asset/Anime/Character/Plant/Chapter3/DoomTanglekelp/Scene/TowerDefensePlantDoomTanglekelp.tscn", "character.attack.0", new Vector2(84f, 36f), Vector2.Zero),
			("res://Asset/Anime/Character/Plant/Chapter3/TanglekelpH/Scene/TowerDefensePlantTanglekelpH.tscn", "character.attack.0", new Vector2(84f, 36f), Vector2.Zero),
			("res://Asset/Anime/Character/Plant/Star/InsaniKelp/Scene/TowerDefensePlantInsaniKelp.tscn", "character.attack.0", new Vector2(84f, 36f), Vector2.Zero),
			("res://Asset/Anime/Character/Zombie/Chapter3/Dolphinrider/Scene/TowerDefenseZombieDolphinrider.tscn", "character.attack.1", new Vector2(85f, 76f), new Vector2(-18.5f, -1f)),
			("res://Asset/Anime/Character/Zombie/Chapter4/DolphinDC/Scene/TowerDefenseZombieDolphinDC.tscn", "character.attack.1", new Vector2(85f, 76f), new Vector2(-18.5f, -1f)),
			("res://Asset/Anime/Character/Zombie/Chapter4/DolphinMJ/Scene/TowerDefenseZombieDolphinMJ.tscn", "character.attack.1", new Vector2(85f, 76f), new Vector2(-18.5f, -1f)),
			("res://Asset/Anime/Character/Zombie/Chapter3/Dolphinrider/Scene/TowerDefenseZombieDolphinriderGatlingPea.tscn", "character.attack.1", new Vector2(85f, 76f), new Vector2(-20f, 0f)),
			("res://Asset/Anime/Character/Zombie/Chapter3/DolphiSN/Scene/TowerDefenseZombieDolphiSN.tscn", "character.attack.1", new Vector2(85f, 76f), new Vector2(-20f, 0f)),
			("res://Asset/Anime/Character/Zombie/Chapter7/Captain/Scene/TowerDefenseZombieCaptain.tscn", "character.attack.0", new Vector2(87f, 33f), new Vector2(-17.5f, -2f)),
			("res://Asset/Anime/Character/Zombie/Chapter7/Crew/Scene/TowerDefenseZombieCrew.tscn", "character.attack.0", new Vector2(87f, 33f), new Vector2(-17.5f, -2f))
		};
		for (int i = 0; i < array.Length; i++)
		{
			(string, string, Vector2, Vector2) tuple = array[i];
			CheckAttackRectangleShapeScene(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, "Contact rectangle");
		}
	}

	private void RunChangeProjectileRectangleShapeSceneChecks()
	{
		(string, Vector2)[] array = new(string, Vector2)[15]
		{
			("res://Asset/Anime/Character/Item/MegaFire/Scene/TowerDefenseItemMegaFire.tscn", Vector2.Zero),
			("res://Asset/Anime/Character/Plant/Chapter3/PumpkinFire/Scene/TowerDefensePlantPumpkinFire.tscn", Vector2.Zero),
			("res://Asset/Anime/Character/Plant/Cover/HypnoShroomIce/Scene/TowerDefensePlantHypnoShroomIce.tscn", Vector2.Zero),
			("res://Asset/Anime/Character/Plant/Cover/PumpkinFireIce/Scene/TowerDefensePlantPumpkinFireIce.tscn", new Vector2(-0.5f, 0f)),
			("res://Asset/Anime/Character/Plant/Chapter0/Torchwood/Scene/TowerDefensePlantTorchwood.tscn", new Vector2(3f, 0f)),
			("res://Asset/Anime/Character/Plant/Chapter1/Firenut/Scene/TowerDefensePlantFirenut.tscn", new Vector2(3f, 0f)),
			("res://Asset/Anime/Character/Plant/Chapter3/Snowwood/Scene/TowerDefensePlantSnowwood.tscn", new Vector2(3f, 0f)),
			("res://Asset/Anime/Character/Plant/Chapter8/TorchwoodZ/Scene/TowerDefensePlantTorchwoodZ.tscn", new Vector2(3f, 0f)),
			("res://Asset/Anime/Character/Plant/Other/TorchwoodStar/Scene/TowerDefensePlantTorchwoodStar.tscn", new Vector2(3f, 0f)),
			("res://Asset/Anime/Character/Zombie/Challenge/DancerFire/Scene/TowerDefenseZombieDancerFire.tscn", new Vector2(3f, 0f)),
			("res://Asset/Anime/Character/Zombie/Challenge/DiscoFire/Scene/TowerDefenseZombieDiscoFire.tscn", new Vector2(3f, 0f)),
			("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Firenut/TowerDefenseZombieNormalFireNut.tscn", new Vector2(3f, 0f)),
			("res://Asset/Anime/Character/Plant/Chapter7/UmbrellaTorch/Scene/TowerDefensePlantUmbrellaTorch.tscn", new Vector2(3f, 0f)),
			("res://Asset/Anime/Character/Plant/Cover/FireTallnut/Scene/TowerDefensePlantFireTallnut.tscn", new Vector2(3f, 0f)),
			("res://Asset/Anime/Character/Plant/Gold/QueenSunFlower/Scene/TowerDefensePlantQueenSunFlower.tscn", new Vector2(3f, 0f))
		};
		for (int i = 0; i < array.Length; i++)
		{
			(string, Vector2) tuple = array[i];
			CheckRectangleShapeScene(tuple.Item1, "ComponentManager/ChangeProjectileComponent/Area2D", new Vector2(100f, 98f), tuple.Item2, "Change-projectile rectangle");
		}
	}

	private void CheckRectangleShapeScene(string scenePath, string areaPath, Vector2 expectedSize, Vector2 expectedOrigin, string label)
	{
		Node node = GD.Load<PackedScene>(scenePath)?.Instantiate(PackedScene.GenEditState.Disabled);
		AabbArea2D aabbArea2D = node?.GetNodeOrNull<AabbArea2D>(areaPath);
		CharacterComponentRuntime characterComponentRuntime = null;
		ICharacterComponentCollisionPreviewProvider characterComponentCollisionPreviewProvider = null;
		int num = (areaPath.EndsWith("Area2D2", StringComparison.Ordinal) ? 1 : 0);
		AabbShape2DResource aabbShape2DResource;
		AabbShape2DResource aabbShape2DResource2;
		if (GodotObject.IsInstanceValid(aabbArea2D))
		{
			Array<AabbShape2DResource> shapeResources = aabbArea2D.ShapeResources;
			Check(shapeResources != null && shapeResources.Count == 1, label + " legacy Shape Resource count changed: " + scenePath);
			Check(!HasDirectCollisionShape(aabbArea2D), label + " legacy area still materializes CollisionShape2D: " + scenePath);
			Array<AabbShape2DResource> shapeResources2 = aabbArea2D.ShapeResources;
			aabbShape2DResource = ((shapeResources2 != null && shapeResources2.Count == 1) ? aabbArea2D.ShapeResources[0] : null);
			aabbShape2DResource2 = aabbShape2DResource;
		}
		else
		{
			characterComponentRuntime = FindComponentRuntimeByLegacyPath(node, areaPath);
			characterComponentCollisionPreviewProvider = characterComponentRuntime?.ComponentDefinition as ICharacterComponentCollisionPreviewProvider;
			Check(characterComponentRuntime != null && !characterComponentRuntime.IsReleased && characterComponentCollisionPreviewProvider != null, label + " Resource runtime failed to instantiate: " + scenePath);
			Check(characterComponentCollisionPreviewProvider != null && characterComponentCollisionPreviewProvider.CollisionPreviewShapeCount > num, label + " runtime/definition Shape Resource count changed: " + scenePath);
			Check(node?.GetNodeOrNull(areaPath) == null, label + " still materializes the legacy Area node: " + scenePath);
			aabbShape2DResource = characterComponentCollisionPreviewProvider?.GetCollisionPreviewShape(num);
			aabbShape2DResource2 = GetRuntimeShapeResource(characterComponentRuntime, num, aabbShape2DResource);
			Check(GodotObject.IsInstanceValid(aabbShape2DResource2) && aabbShape2DResource2.Geometry?.GetType() == aabbShape2DResource?.Geometry?.GetType(), label + " runtime geometry is not bound to its definition: " + scenePath);
		}
		AabbShape2DResource aabbShape2DResource3 = aabbShape2DResource;
		RectangleShape2D rectangleShape2D = (GodotObject.IsInstanceValid(aabbShape2DResource3) ? (aabbShape2DResource3.Geometry as RectangleShape2D) : null);
		Check(GodotObject.IsInstanceValid(rectangleShape2D) && rectangleShape2D.Size.IsEqualApprox(expectedSize), label + " geometry changed: " + scenePath);
		bool flag = characterComponentRuntime is BlockComponent;
		Check(GodotObject.IsInstanceValid(aabbShape2DResource3) && GodotObject.IsInstanceValid(aabbShape2DResource2) && aabbShape2DResource2.LocalTransform.IsEqualApprox(aabbShape2DResource3.LocalTransform) && (flag || aabbShape2DResource3.LocalTransform.Origin.IsEqualApprox(expectedOrigin)) && aabbShape2DResource3.LocalTransform.X.IsEqualApprox(Vector2.Right) && aabbShape2DResource3.LocalTransform.Y.IsEqualApprox(Vector2.Down), label + " runtime/definition local transform changed: " + scenePath);
		if (characterComponentRuntime != null && rectangleShape2D != null)
		{
			RectangleShape2D rectangleShape2D2 = aabbShape2DResource2?.Geometry as RectangleShape2D;
			if (characterComponentRuntime is BlockComponent || characterComponentRuntime is ChangeProjectileStateComponent)
			{
				Check(rectangleShape2D2 != rectangleShape2D, label + " mutable runtime rectangle still aliases shared geometry: " + scenePath);
			}
			else if (characterComponentRuntime is CharacterAabbAreaComponent)
			{
				Check(rectangleShape2D2 == rectangleShape2D, label + " node-free runtime did not retain its immutable Shape Resource: " + scenePath);
			}
			Vector2 vector = expectedSize + new Vector2(13f, 7f);
			if (characterComponentRuntime is CharacterAabbAreaComponent || (characterComponentRuntime is BlockComponent && num == 0) || characterComponentRuntime is ChangeProjectileStateComponent)
			{
				Check(TrySetRuntimeRectangleSize(characterComponentRuntime, num, vector, out var actualSize) && actualSize.IsEqualApprox(vector) && rectangleShape2D.Size.IsEqualApprox(expectedSize), label + " runtime rectangle override polluted shared geometry: " + scenePath);
			}
			else
			{
				TowerDefenseCharacter towerDefenseCharacter = node as TowerDefenseCharacter;
				Check(GodotObject.IsInstanceValid(towerDefenseCharacter) && GodotObject.IsInstanceValid(aabbShape2DResource2) && aabbShape2DResource2.TryGetWorldRect(towerDefenseCharacter.GlobalTransform, out var rect) && rect.Size != Vector2.Zero && rectangleShape2D.Size.IsEqualApprox(expectedSize), label + " Resource runtime world geometry changed: " + scenePath);
			}
		}
		QueueSceneInstanceForCleanup(node);
	}

	private void CheckDefinitionRectangleShape<TDefinition>(string scenePath, string instanceId, Vector2 expectedSize, Vector2 expectedOrigin, string label) where TDefinition : CharacterComponentDefinition, ICharacterComponentCollisionPreviewProvider
	{
		Node node = GD.Load<PackedScene>(scenePath)?.Instantiate(PackedScene.GenEditState.Disabled);
		TDefinition val = FindComponentDefinition<TDefinition>(node, instanceId);
		CharacterComponentRuntime characterComponentRuntime = FindComponentRuntimeByInstanceId(node, instanceId);
		Check(val != null, label + " Definition is missing from the role-local ComponentSet: " + scenePath);
		Check(characterComponentRuntime != null && !characterComponentRuntime.IsReleased && characterComponentRuntime.ComponentDefinition == val, label + " Resource runtime is not bound to its definition: " + scenePath);
		Check(val != null && val.CollisionPreviewShapeCount == 1, label + " Shape Resource count changed: " + scenePath);
		AabbShape2DResource aabbShape2DResource = val?.GetCollisionPreviewShape(0);
		RectangleShape2D rectangleShape2D = (GodotObject.IsInstanceValid(aabbShape2DResource) ? (aabbShape2DResource.Geometry as RectangleShape2D) : null);
		Check(GodotObject.IsInstanceValid(rectangleShape2D) && rectangleShape2D.Size.IsEqualApprox(expectedSize), label + " Definition geometry changed: " + scenePath);
		Check(GodotObject.IsInstanceValid(aabbShape2DResource) && aabbShape2DResource.LocalTransform.Origin.IsEqualApprox(expectedOrigin) && aabbShape2DResource.LocalTransform.X.IsEqualApprox(Vector2.Right) && aabbShape2DResource.LocalTransform.Y.IsEqualApprox(Vector2.Down), label + " Definition local transform changed: " + scenePath);
		QueueSceneInstanceForCleanup(node);
	}

	private AttackComponent CreateAttackRuntime(Node instance, string instanceId, out AttackComponentDefinition definition)
	{
		definition = FindComponentDefinition<AttackComponentDefinition>(instance, instanceId);
		return FindComponentRuntime<AttackComponent>(instance, instanceId);
	}

	private void CheckAttackRectangleShapeScene(string scenePath, string instanceId, Vector2 expectedSize, Vector2 expectedOrigin, string label)
	{
		Node node = GD.Load<PackedScene>(scenePath)?.Instantiate(PackedScene.GenEditState.Disabled);
		AttackComponentDefinition definition;
		AttackComponent attackComponent = CreateAttackRuntime(node, instanceId, out definition);
		Check(definition != null, $"{label} Attack Definition is missing: {scenePath} instance={instanceId}");
		Check(definition != null && definition.CollisionPreviewShapeCount == 1, $"{label} Definition Shape Resource count changed: {scenePath} instance={instanceId}");
		Check(attackComponent != null && attackComponent.CheckAreaShapeCount == 1, $"{label} runtime Shape Resource count changed: {scenePath} instance={instanceId}");
		AabbShape2DResource aabbShape2DResource = definition?.GetCollisionPreviewShape(0);
		RectangleShape2D rectangleShape2D = (GodotObject.IsInstanceValid(aabbShape2DResource) ? (aabbShape2DResource.Geometry as RectangleShape2D) : null);
		Check(GodotObject.IsInstanceValid(rectangleShape2D) && rectangleShape2D.Size.IsEqualApprox(expectedSize), $"{label} Definition geometry changed: {scenePath} instance={instanceId}");
		Check(GodotObject.IsInstanceValid(aabbShape2DResource) && aabbShape2DResource.LocalTransform.Origin.IsEqualApprox(expectedOrigin) && aabbShape2DResource.LocalTransform.X.IsEqualApprox(Vector2.Right) && aabbShape2DResource.LocalTransform.Y.IsEqualApprox(Vector2.Down), $"{label} Definition local transform changed: {scenePath} instance={instanceId}");
		Vector2 size = default(Vector2);
		bool flag = attackComponent?.TryGetCheckAreaRectangleSize(0, out size) ?? false;
		Check(flag && size.IsFinite() && size.X > 0f && size.Y > 0f, $"{label} runtime effective size is invalid: {scenePath} instance={instanceId}");
		Transform2D localTransform = Transform2D.Identity;
		bool flag2 = attackComponent?.TryGetCheckAreaShapeLocalTransform(0, out localTransform) ?? false;
		Check(flag2 && localTransform.IsFinite() && localTransform.X.IsEqualApprox(aabbShape2DResource?.LocalTransform.X ?? Vector2.Zero) && localTransform.Y.IsEqualApprox(aabbShape2DResource?.LocalTransform.Y ?? Vector2.Zero), $"{label} runtime effective local transform is invalid: {scenePath} instance={instanceId}");
		Vector2 vector = size + new Vector2(17f, 9f);
		Check(attackComponent?.SetCheckAreaRectangleSize(0, vector) ?? false, $"{label} runtime rejected rectangle override: {scenePath} instance={instanceId}");
		Check(attackComponent != null && attackComponent.TryGetCheckAreaRectangleSize(0, out var size2) && size2.IsEqualApprox(vector) && attackComponent.TryGetCheckAreaShapeLocalTransform(0, out var localTransform2) && localTransform2.IsEqualApprox(localTransform) && (rectangleShape2D?.Size.IsEqualApprox(expectedSize) ?? false), $"{label} runtime rectangle override mutated shared geometry: {scenePath} instance={instanceId}");
		TowerDefenseCharacter towerDefenseCharacter = node as TowerDefenseCharacter;
		Rect2 right = ((GodotObject.IsInstanceValid(towerDefenseCharacter) && flag2) ? AabbShapeUtil.ComputeRectangleWorldRect(towerDefenseCharacter.GlobalTransform * localTransform, vector) : default(Rect2));
		Check(attackComponent != null && attackComponent.TryGetCheckAreaWorldRect(out var worldRect) && RectApprox(worldRect, right), $"{label} runtime world rectangle changed: {scenePath} instance={instanceId}");
		attackComponent?.Release();
		QueueSceneInstanceForCleanup(node);
	}

	private void RunCompressionRectangleShapeSceneChecks()
	{
		Vector2 expectedSize = new Vector2(224f, 34f);
		CheckAttackRectangleShapeScene("res://Asset/Anime/Character/Plant/Chapter0/Squash/Scene/TowerDefensePlantSquash.tscn", "character.attack.0", expectedSize, Vector2.Zero, "Compression rectangle");
		CheckAttackRectangleShapeScene("res://Asset/Anime/Character/Plant/Chapter4/IceSquash/Scene/TowerDefensePlantIceSquash.tscn", "character.attack.0", expectedSize, Vector2.Zero, "Compression rectangle");
		CheckAttackRectangleShapeScene("res://Asset/Anime/Character/Plant/Chapter6/SquashKing/Scene/TowerDefensePlantSquashKing.tscn", "character.attack.0", expectedSize, Vector2.Zero, "Compression rectangle");
		CheckAttackRectangleShapeScene("res://Asset/Anime/Character/Plant/Chapter6/WallnutSquash/Scene/TowerDefensePlantWallnutSquash.tscn", "character.attack.0", expectedSize, Vector2.Zero, "Compression rectangle");
		CheckAttackRectangleShapeScene("res://Asset/Anime/Character/Plant/Star/SquashCandy/Scene/TowerDefensePlantSquashCandy.tscn", "character.attack.0", expectedSize, Vector2.Zero, "Compression rectangle");
		CheckAttackRectangleShapeScene("res://Asset/Anime/Character/Plant/Star/SquashVase/Scene/TowerDefensePlantSquashVase.tscn", "character.attack.0", expectedSize, Vector2.Zero, "Compression rectangle");
		CheckDefinitionRectangleShape<SquashComponentDefinition>("res://Asset/Anime/Character/Plant/Star/SquashVase/Scene/TowerDefensePlantSquashVase.tscn", "character.squash", new Vector2(80f, 33f), Vector2.Zero, "Squash-vase check rectangle");
		CheckAttackRectangleShapeScene("res://Asset/Anime/Character/Plant/Chapter6/GloomSquash/Scene/TowerDefensePlantGloomSquash.tscn", "character.attack.1", expectedSize, Vector2.Zero, "Compression rectangle");
		CheckAttackRectangleShapeScene("res://Asset/Anime/Character/Plant/Chapter1/Firenut/Scene/TowerDefensePlantFirenut.tscn", "character.attack.0", new Vector2(67f, 82f), new Vector2(1.5f, -5f), "Attack rectangle");
		CheckAttackRectangleShapeScene("res://Asset/Anime/Character/Zombie/Puzzle/Gardener/Scene/TowerDefenseZombieGardener.tscn", "character.attack.1", new Vector2(100f, 33f), new Vector2(-26f, 0.5f), "Gardener check rectangle");
		CheckAttackRectangleShapeScene("res://Asset/Anime/Character/Zombie/Challenge/Soccer/Scene/TowerDefenseZombieSoccer.tscn", "character.attack.1", new Vector2(100f, 76f), new Vector2(-26f, -1f), "Soccer kick rectangle");
		CheckAttackRectangleShapeScene("res://Asset/Anime/Character/Zombie/Chapter4/ZombieShark/Scene/TowerDefenseZombieShark.tscn", "character.attack.1", new Vector2(49f, 56f), new Vector2(-0.5f, -1f), "Shark check rectangle");
		CheckAttackRectangleShapeScene("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Squash/TowerDefenseZombieNormalSquash.tscn", "character.attack.1", new Vector2(75f, 34f), new Vector2(-28.5f, 0f), "Zombie squash rectangle");
		CheckRectangleShapeScene("res://Asset/Anime/Character/Plant/Chapter7/WallnutHole/Scene/TowerDefensePlantWallnutHole.tscn", "ComponentManager/BlockDiggerComponent/Area2D", new Vector2(76f, 50f), Vector2.Zero, "Digger block rectangle");
		CheckAttackRectangleShapeScene("res://Asset/Anime/Character/Zombie/Chapter2/Digger/Scene/PotatoMine/TowerDefenseZombieDiggerPotatoMine.tscn", "character.attack.1", new Vector2(80f, 49.5f), new Vector2(-8f, 2f), "Digger mine rectangle");
	}

	private void RunDirectAreaRectangleShapeSceneChecks()
	{
		Vector2 expectedSize = new Vector2(100f, 76f);
		Vector2 expectedOrigin = new Vector2(-26f, -1f);
		CheckAttackRectangleShapeScene("res://Asset/Anime/Character/Zombie/Chapter1/Polevaulter/Scene/TowerDefenseZombiePolevaulter.tscn", "character.attack.1", expectedSize, expectedOrigin, "Pole-vault check rectangle");
		CheckAttackRectangleShapeScene("res://Asset/Anime/Character/Zombie/Chapter2/Polestriker/Scene/TowerDefenseZombiePolestriker.tscn", "character.attack.1", expectedSize, expectedOrigin, "Pole-strike check rectangle");
		CheckRectangleShapeScene("res://Asset/Anime/Character/Zombie/Challenge/Portal/Scene/TowerDefenseZombiePortal.tscn", "Node2D/Area2D", new Vector2(72.5f, 77f), new Vector2(-6.25f, 0.5f), "Zombie portal rectangle");
		CheckRectangleShapeScene("res://Registry/Battle/Feature/Portal/Object/TowerDefensePortal.tscn", "ProtalNode1/HitBox1", new Vector2(12f, 33f), Vector2.Zero, "Portal endpoint rectangle");
		CheckRectangleShapeScene("res://Registry/Battle/Feature/Portal/Object/TowerDefensePortal.tscn", "ProtalNode2/HitBox2", new Vector2(12f, 33f), Vector2.Zero, "Portal endpoint rectangle");
		Vector2 expectedSize2 = new Vector2(19.5f, 505.5f);
		Vector2 expectedOrigin2 = new Vector2(0f, 253f);
		CheckRectangleShapeScene("res://Prefab/TowerDefense/Object/WarningLine.tscn", "Sprite/Area2D", expectedSize2, expectedOrigin2, "Warning-line rectangle");
		CheckRectangleShapeScene("res://Prefab/TowerDefense/Object/WarningLine.tscn", "Sprite/Area2D2", expectedSize2, expectedOrigin2, "Warning-line rectangle");
		Vector2 expectedSize3 = new Vector2(341f, 1997f);
		Vector2 expectedOrigin3 = new Vector2(-13.5f, 1001.5f);
		CheckRectangleShapeScene("res://Scene/TowerDefesne/TowerDefenseNew/TowerDefenseControlNew.tscn", "CharacterLayer/ZombieCheckArea", expectedSize3, expectedOrigin3, "New-board zombie check rectangle");
		CheckRectangleShapeScene("res://Scene/TowerDefesne/TowerDefenseOld/TowerDefenseControlOld.tscn", "MapLayer/ZombieWonArea", expectedSize3, expectedOrigin3, "Old-board zombie win rectangle");
	}

	private void RunStaticSegmentShapeSceneChecks()
	{
		Node node = GD.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter3/ChomperBlow/Scene/TowerDefensePlantChomperBlow.tscn")?.Instantiate(PackedScene.GenEditState.Disabled);
		AttackComponentDefinition definition;
		AttackComponent attackComponent = CreateAttackRuntime(node, "character.attack.0", out definition);
		Check(attackComponent != null && !attackComponent.IsReleased, "Static segment Attack runtime failed to instantiate: res://Asset/Anime/Character/Plant/Chapter3/ChomperBlow/Scene/TowerDefensePlantChomperBlow.tscn");
		Check(definition != null && definition.CollisionPreviewShapeCount == 1 && attackComponent != null && attackComponent.CheckAreaShapeCount == 1, "Static segment Shape Resource count changed: res://Asset/Anime/Character/Plant/Chapter3/ChomperBlow/Scene/TowerDefensePlantChomperBlow.tscn");
		Check(node?.GetNodeOrNull("ComponentManager/ChomperComponent/CheckArea") == null, "Static segment still materializes the legacy Area node: res://Asset/Anime/Character/Plant/Chapter3/ChomperBlow/Scene/TowerDefensePlantChomperBlow.tscn");
		AabbShape2DResource aabbShape2DResource = definition?.GetCollisionPreviewShape(0);
		SegmentShape2D segmentShape2D = (GodotObject.IsInstanceValid(aabbShape2DResource) ? (aabbShape2DResource.Geometry as SegmentShape2D) : null);
		Check(GodotObject.IsInstanceValid(segmentShape2D) && segmentShape2D.A.IsEqualApprox(Vector2.Zero) && segmentShape2D.B.IsEqualApprox(new Vector2(100f, 0f)), "Static segment geometry changed: res://Asset/Anime/Character/Plant/Chapter3/ChomperBlow/Scene/TowerDefensePlantChomperBlow.tscn");
		Check(attackComponent != null && attackComponent.TryGetCheckAreaShapeLocalTransform(0, out var localTransform) && localTransform.IsEqualApprox(Transform2D.Identity) && GodotObject.IsInstanceValid(aabbShape2DResource) && aabbShape2DResource.LocalTransform.IsEqualApprox(Transform2D.Identity), "Static segment local transform changed: res://Asset/Anime/Character/Plant/Chapter3/ChomperBlow/Scene/TowerDefensePlantChomperBlow.tscn");
		QueueSceneInstanceForCleanup(node);
	}

	private void RunFireAreaSegmentShapeSceneChecks()
	{
		(string, float, int)[] array = new(string, float, int)[25]
		{
			("res://Asset/Anime/Character/Plant/Chapter0/Cabbagepult/Scene/TowerDefensePlantCabbagepult.tscn", 1f, 1),
			("res://Asset/Anime/Character/Plant/Chapter0/Cornpult/Scene/TowerDefensePlantCornpult.tscn", 1f, 1),
			("res://Asset/Anime/Character/Plant/Chapter0/Melonpult/Scene/TowerDefensePlantMelonpult.tscn", 1f, 1),
			("res://Asset/Anime/Character/Plant/Chapter2/Gloompult/Scene/TowerDefensePlantGloompult.tscn", 1f, 1),
			("res://Asset/Anime/Character/Plant/Chapter3/MelonpultSpike/Scene/TowerDefensePlantMelonpultSpike.tscn", 1f, 1),
			("res://Asset/Anime/Character/Plant/Chapter4/CornpultM/Scene/TowerDefensePlantCornpultM.tscn", 1f, 1),
			("res://Asset/Anime/Character/Plant/Chapter5/CabbageCob/Scene/TowerDefensePlantCabbageCob.tscn", 1f, 1),
			("res://Asset/Anime/Character/Plant/Chapter5/Sunpult/Scene/TowerDefensePlantSunpult.tscn", 1f, 1),
			("res://Asset/Anime/Character/Plant/Chapter6/Jalapult/Scene/TowerDefensePlantJalapult.tscn", 1f, 1),
			("res://Asset/Anime/Character/Plant/Chapter6/WinterMelonX/Scene/TowerDefensePlantWinterMelonX.tscn", 1f, 1),
			("res://Asset/Anime/Character/Plant/Chapter7/Nutpult/Scene/TowerDefensePlantNutpult.tscn", 1f, 1),
			("res://Asset/Anime/Character/Plant/Cover/WinterMelon/Scene/TowerDefensePlantWinterMelon.tscn", 1f, 1),
			("res://Asset/Anime/Character/Plant/Gold/GoldMelonpult/Scene/TowerDefensePlantGoldMelonpult.tscn", 1f, 1),
			("res://Asset/Anime/Character/Plant/Other/CabbagepultInside/Scene/TowerDefensePlantCabbagepultInside.tscn", 1f, 1),
			("res://Asset/Anime/Character/Plant/Other/PumpkinPult/Scene/TowerDefensePlantPumpkinPult.tscn", 1f, 1),
			("res://Asset/Anime/Character/Plant/Other/SnowCornpult/Scene/TowerDefensePlantSnowCornpult.tscn", 1f, 1),
			("res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Scene/Gloompult/TowerDefenseZombieGargantuarGloompult.tscn", -1f, 1),
			("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Cabbagepult/TowerDefenseZombieNormalCabbagepult.tscn", -1f, 1),
			("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Cornpult/TowerDefenseZombieNormalCornpult.tscn", -1f, 1),
			("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Melonpult/TowerDefenseZombieNormalMelonpult.tscn", -1f, 1),
			("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/WinterMelon/TowerDefenseZombieNormalWinterMelon.tscn", -1f, 1),
			("res://Asset/Anime/Character/Zombie/Chapter5/Balloonpult/Scene/TowerDefenseZombieBalloonpult.tscn", -1f, 1),
			("res://Asset/Anime/Character/Zombie/Chapter5/Catapult/Scene/TowerDefenseZombieCatapult.tscn", -1f, 1),
			("res://Asset/Anime/Character/Zombie/Chapter5/Imppult/Scene/TowerDefenseZombieImppult.tscn", -1f, 1),
			("res://Asset/Anime/Character/Zombie/Chapter6/Zambonipult/Scene/TowerDefenseZombieZambonipult.tscn", -1f, 1)
		};
		for (int i = 0; i < array.Length; i++)
		{
			(string, float, int) tuple = array[i];
			Node node = GD.Load<PackedScene>(tuple.Item1)?.Instantiate(PackedScene.GenEditState.Disabled);
			FireComponent fireComponent = FindFirstFireComponent(node);
			Array<AabbShape2DResource> array2 = fireComponent?.checkShapeResources;
			Check(fireComponent != null && !fireComponent.IsReleased, "Fire runtime failed to instantiate: " + tuple.Item1);
			Check(array2?.Count == tuple.Item3, "Fire Shape Resource count changed: " + tuple.Item1);
			Check(!HasLegacyFireComponentNode(node), "Fire scene still materializes component/helper nodes: " + tuple.Item1);
			int num = 0;
			while (array2 != null && num < array2.Count)
			{
				AabbShape2DResource aabbShape2DResource = array2[num];
				SegmentShape2D segmentShape2D = (GodotObject.IsInstanceValid(aabbShape2DResource) ? (aabbShape2DResource.Geometry as SegmentShape2D) : null);
				Check(GodotObject.IsInstanceValid(segmentShape2D) && segmentShape2D.A.IsEqualApprox(Vector2.Zero) && segmentShape2D.B.IsEqualApprox(new Vector2(tuple.Item2 * 2000f, 0f)), $"Fire Area segment geometry changed: {tuple.Item1} index={num}");
				Vector2 other = ((tuple.Item3 == 3) ? new Vector2(0f, (float)(num - 1) * 70f) : Vector2.Zero);
				Check(GodotObject.IsInstanceValid(aabbShape2DResource) && aabbShape2DResource.LocalTransform.Origin.IsEqualApprox(other), $"Fire Area segment transform changed: {tuple.Item1} index={num}");
				num++;
			}
			FireComponentDefinition obj = fireComponent?.ComponentDefinition as FireComponentDefinition;
			SegmentShape2D segmentShape2D2 = ((obj != null && obj.checkShapeResources?.Count > 0) ? ((fireComponent.ComponentDefinition as FireComponentDefinition).checkShapeResources[0].Geometry as SegmentShape2D) : null);
			Check(fireComponent?.SetCheckAreaSegmentLengthX(0, 375f) ?? false, "Fire runtime rejected range: " + tuple.Item1);
			Check(GodotObject.IsInstanceValid(segmentShape2D2) && segmentShape2D2.B.IsEqualApprox(new Vector2(tuple.Item2 * 2000f, 0f)) && fireComponent.TryGetCheckAreaSegmentEnd(0, out var end) && end.IsEqualApprox(new Vector2(tuple.Item2 * 375f, 0f)), "Fire runtime range mutated Definition geometry: " + tuple.Item1);
			QueueSceneInstanceForCleanup(node);
		}
	}

	private void RunPopcornpultMultiSegmentSceneChecks()
	{
		Node node = GD.Load<PackedScene>("res://Asset/Anime/Character/Plant/Cover/Popcornpult/Scene/TowerDefensePlantPopcornpult.tscn")?.Instantiate(PackedScene.GenEditState.Disabled);
		FireComponent fireComponent = FindFirstFireComponent(node);
		Array<AabbShape2DResource> array = fireComponent?.checkShapeResources;
		Check(fireComponent != null && !fireComponent.IsReleased, "Popcornpult Fire runtime failed to instantiate");
		Check(array != null && array.Count == 3, "Popcornpult Fire runtime must retain three segments");
		Check(!HasLegacyFireComponentNode(node), "Popcornpult still materializes Fire component/helper nodes");
		Vector2[] array2 = new Vector2[3]
		{
			new Vector2(0f, -70f),
			Vector2.Zero,
			new Vector2(0f, 70f)
		};
		int num = 0;
		while (array != null && num < array.Count)
		{
			AabbShape2DResource aabbShape2DResource = array[num];
			SegmentShape2D segmentShape2D = (GodotObject.IsInstanceValid(aabbShape2DResource) ? (aabbShape2DResource.Geometry as SegmentShape2D) : null);
			Check(GodotObject.IsInstanceValid(segmentShape2D) && segmentShape2D.A.IsEqualApprox(Vector2.Zero) && segmentShape2D.B.IsEqualApprox(new Vector2(2000f, 0f)), $"Popcornpult segment geometry changed at index {num}");
			Check(GodotObject.IsInstanceValid(aabbShape2DResource) && aabbShape2DResource.LocalTransform.Origin.IsEqualApprox(array2[num]), $"Popcornpult segment ordering/transform changed at index {num}");
			num++;
		}
		Check(fireComponent?.SetCheckAreaSegmentLengthX(0, 420f) ?? false, "Popcornpult upper segment rejected runtime range");
		Check(fireComponent != null && fireComponent.TryGetCheckAreaSegmentEnd(0, out var end) && end.IsEqualApprox(new Vector2(420f, 0f)) && fireComponent.TryGetCheckAreaSegmentEnd(1, out var end2) && end2.IsEqualApprox(new Vector2(2000f, 0f)) && fireComponent.TryGetCheckAreaSegmentEnd(2, out var end3) && end3.IsEqualApprox(new Vector2(2000f, 0f)), "Popcornpult runtime range must preserve the legacy first-child-only behavior");
		QueueSceneInstanceForCleanup(node);
	}

	private void RunDynamicMultiSegmentFireAreaSceneChecks()
	{
		(string, string)[] array = new(string, string)[2]
		{
			("res://Asset/Anime/Character/Plant/Other/ThreeCornpult/Scene/TowerDefensePlantThreeCornpult.tscn", "character.fire"),
			("res://Asset/Anime/Character/Plant/Star/Robot/Scene/TowerDefensePlantRobot.tscn", "character.fire.1")
		};
		for (int i = 0; i < array.Length; i++)
		{
			(string, string) tuple = array[i];
			Node node = GD.Load<PackedScene>(tuple.Item1)?.Instantiate(PackedScene.GenEditState.Disabled);
			FireComponent fireComponent = FindFirstFireComponent(node, tuple.Item2);
			Array<AabbShape2DResource> array2 = fireComponent?.checkShapeResources;
			Array<AabbShape2DResource> array3 = (fireComponent?.ComponentDefinition as FireComponentDefinition)?.checkShapeResources;
			Check(fireComponent != null && !fireComponent.IsReleased, "Dynamic multi-line Fire runtime failed to instantiate: " + tuple.Item1);
			Check(array2 != null && array2.Count == 3, "Dynamic multi-line runtime must retain three segments: " + tuple.Item1);
			Check(!HasLegacyFireComponentNode(node), "Dynamic multi-line scene still materializes Fire component/helper nodes: " + tuple.Item1);
			Vector2[] array4 = new Vector2[3]
			{
				new Vector2(0f, -70f),
				Vector2.Zero,
				new Vector2(0f, 70f)
			};
			int num = 0;
			while (array3 != null && num < array3.Count)
			{
				AabbShape2DResource aabbShape2DResource = array3[num];
				SegmentShape2D segmentShape2D = (GodotObject.IsInstanceValid(aabbShape2DResource) ? (aabbShape2DResource.Geometry as SegmentShape2D) : null);
				Check(GodotObject.IsInstanceValid(segmentShape2D) && segmentShape2D.B.IsEqualApprox(new Vector2(2000f, 0f)), $"Dynamic multi-line segment geometry changed: {tuple.Item1} index={num}");
				Check(GodotObject.IsInstanceValid(aabbShape2DResource) && aabbShape2DResource.LocalTransform.Origin.IsEqualApprox(array4[num]), $"Dynamic multi-line configured transform changed: {tuple.Item1} index={num}");
				num++;
			}
			Check(fireComponent != null && fireComponent.SetCheckAreaShapeLocalOriginY(0, 80f) && fireComponent.SetCheckAreaShapeLocalOriginY(2, -80f), "Dynamic multi-line FireComponent rejected row offsets: " + tuple.Item1);
			Check(fireComponent != null && fireComponent.TryGetCheckAreaShapeLocalTransform(0, out var localTransform) && localTransform.Origin.IsEqualApprox(new Vector2(0f, 80f)) && fireComponent.TryGetCheckAreaShapeLocalTransform(1, out var localTransform2) && localTransform2.Origin.IsEqualApprox(Vector2.Zero) && fireComponent.TryGetCheckAreaShapeLocalTransform(2, out var localTransform3) && localTransform3.Origin.IsEqualApprox(new Vector2(0f, -80f)), "Dynamic multi-line runtime row offsets changed: " + tuple.Item1);
			FireComponentDefinition obj = fireComponent?.ComponentDefinition as FireComponentDefinition;
			Check(obj != null && obj.checkShapeResources?[0].LocalTransform.Origin.IsEqualApprox(array4[0]) == true && (fireComponent.ComponentDefinition as FireComponentDefinition).checkShapeResources[2].LocalTransform.Origin.IsEqualApprox(array4[2]), "Dynamic multi-line row offsets mutated shared Shape Resources: " + tuple.Item1);
			QueueSceneInstanceForCleanup(node);
		}
	}

	private void RunAttackSegmentShapeSceneChecks()
	{
		(string, string, float, float, int)[] array = new(string, string, float, float, int)[15]
		{
			("res://Asset/Anime/Character/Plant/Chapter0/Chomper/Scene/TowerDefensePlantChomper.tscn", "character.attack.0", 1f, 100f, 1),
			("res://Asset/Anime/Character/Plant/Chapter0/FumeShroom/Scene/TowerDefensePlantFumeShroom.tscn", "character.attack.0", 1f, 100f, 1),
			("res://Asset/Anime/Character/Plant/Chapter2/WinterMelonShroom/Scene/TowerDefensePlantWinterMelonShroom.tscn", "character.attack.0", 1f, 100f, 1),
			("res://Asset/Anime/Character/Plant/Chapter3/ChomperBlow/Scene/TowerDefensePlantChomperBlow.tscn", "character.attack.0", 1f, 100f, 1),
			("res://Asset/Anime/Character/Plant/Chapter5/GarlicChomper/Scene/TowerDefensePlantGarlicChomper.tscn", "character.attack.0", 1f, 100f, 1),
			("res://Asset/Anime/Character/Plant/Chapter7/PowShroom/Scene/TowerDefensePlantPowShroom.tscn", "character.attack.0", 1f, 100f, 1),
			("res://Asset/Anime/Character/Plant/Chapter7/StressShroom/Scene/TowerDefensePlantStressShroom.tscn", "character.attack.0", 1f, 100f, 6),
			("res://Asset/Anime/Character/Plant/Chapter8/ChomperZ/Scene/TowerDefensePlantChomperZ.tscn", "character.attack.0", 1f, 100f, 1),
			("res://Asset/Anime/Character/Plant/Gold/SpikeChomper/Scene/TowerDefensePlantSpikeChomper.tscn", "character.attack.0", 1f, 100f, 1),
			("res://Asset/Anime/Character/Plant/Gold/SpikeShroom/Scene/TowerDefensePlantSpikeShroom.tscn", "character.attack.0", 1f, 2000f, 1),
			("res://Asset/Anime/Character/Plant/Other/FumeSeaShroom/Scene/TowerDefensePlantFumeSeaShroom.tscn", "character.attack.0", 1f, 100f, 1),
			("res://Asset/Anime/Character/Plant/Other/FumeShroomC/Scene/TowerDefensePlantFumeShroomC.tscn", "character.attack.0", 1f, 100f, 1),
			("res://Asset/Anime/Character/Zombie/Chapter1/Football/Scene/BlackFumeShroom/TowerDefenseZombieFootballBlackFumeShroom.tscn", "character.attack.1", -1f, 100f, 1),
			("res://Asset/Anime/Character/Zombie/Chapter1/Football/Scene/FumeShroom/TowerDefenseZombieFootballFumeShroom.tscn", "character.attack.1", -1f, 100f, 1),
			("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Chomper/TowerDefenseZombieNormalChomper.tscn", "character.attack.1", -1f, 100f, 1)
		};
		for (int i = 0; i < array.Length; i++)
		{
			(string, string, float, float, int) tuple = array[i];
			Node instance = GD.Load<PackedScene>(tuple.Item1)?.Instantiate(PackedScene.GenEditState.Disabled);
			AttackComponentDefinition definition;
			AttackComponent attackComponent = CreateAttackRuntime(instance, tuple.Item2, out definition);
			Check(definition != null, "Attack segment Definition is missing: " + tuple.Item1 + " instance=" + tuple.Item2);
			Check(definition?.CollisionPreviewShapeCount == tuple.Item5, "Attack segment Definition Resource count changed: " + tuple.Item1);
			Check(attackComponent?.CheckAreaShapeCount == tuple.Item5, "Attack segment runtime Resource count changed: " + tuple.Item1);
			int num = 0;
			while (definition != null && num < definition.CollisionPreviewShapeCount)
			{
				AabbShape2DResource collisionPreviewShape = definition.GetCollisionPreviewShape(num);
				SegmentShape2D segmentShape2D = (GodotObject.IsInstanceValid(collisionPreviewShape) ? (collisionPreviewShape.Geometry as SegmentShape2D) : null);
				Check(GodotObject.IsInstanceValid(segmentShape2D) && segmentShape2D.A.IsEqualApprox(Vector2.Zero) && segmentShape2D.B.IsEqualApprox(new Vector2(tuple.Item3 * tuple.Item4, 0f)), $"Attack segment base geometry changed: {tuple.Item1} index={num}");
				Check(attackComponent?.SetCheckAreaSegmentLengthX(num, 333f) ?? false, $"Attack segment rejected runtime length: {tuple.Item1} index={num}");
				Check(attackComponent != null && attackComponent.TryGetCheckAreaSegmentEnd(num, out var endpoint) && endpoint.IsEqualApprox(new Vector2(tuple.Item3 * 333f, 0f)) && segmentShape2D.B.IsEqualApprox(new Vector2(tuple.Item3 * tuple.Item4, 0f)), $"Attack segment runtime length mutated shared geometry: {tuple.Item1} index={num}");
				num++;
			}
			if (tuple.Item5 == 6)
			{
				Transform2D[] array2 = new Transform2D[6]
				{
					Transform2D.Identity,
					new Transform2D(-(float)Math.PI / 2f, new Vector2(-8f, 0f)),
					new Transform2D(-(float)Math.PI / 2f, new Vector2(9f, 0f)),
					new Transform2D((float)Math.PI, new Vector2(0f, 1f)),
					new Transform2D((float)Math.PI / 2f, new Vector2(-8f, 1f)),
					new Transform2D((float)Math.PI / 2f, new Vector2(9f, 1f))
				};
				for (int j = 0; j < array2.Length; j++)
				{
					Check(attackComponent != null && attackComponent.TryGetCheckAreaShapeLocalTransform(j, out var localTransform) && localTransform.IsEqualApprox(array2[j]) && definition.GetCollisionPreviewShape(j).LocalTransform.IsEqualApprox(array2[j]), $"StressShroom segment ordering/transform changed at index {j}");
				}
			}
			attackComponent?.Release();
			QueueSceneInstanceForCleanup(instance);
		}
	}

	private void RunRectangleShapeSceneChecks()
	{
		(string, string, Vector2, Vector2)[] array = new(string, string, Vector2, Vector2)[40]
		{
			("res://Asset/Anime/Character/GraveStone/TargetHealth/Scene/TowerDefenseGraveStoneTargetHealth.tscn", "Node2D/CheckArea", new Vector2(73f, 73f), new Vector2(-1.5f, -0.5f)),
			("res://Asset/Anime/Character/GraveStone/TargetMagnet/Scene/TowerDefenseGraveStoneTargetMagnet.tscn", "ComponentManager/ChangeProjectileStateComponent/ChangeCheckArea", new Vector2(368f, 320f), Vector2.Zero),
			("res://Asset/Anime/Character/Item/Spikeball/Scene/TowerDefenseItemSpikeball.tscn", "character.attack.0", new Vector2(73f, 33f), new Vector2(-1.5f, -0.5f)),
			("res://Asset/Anime/Character/Plant/Chapter0/ScaredyShroom/Scene/TowerDefensePlantScaredyShroom.tscn", "character.scared", new Vector2(134f, 144f), Vector2.Zero),
			("res://Asset/Anime/Character/Plant/Chapter0/Umbrellaleaf/Scene/TowerDefensePlantUmbrellaleaf.tscn", "ComponentManager/BlockComponent/Area2D", new Vector2(73f, 33f), Vector2.Zero),
			("res://Asset/Anime/Character/Plant/Chapter2/Gloompult/Scene/TowerDefensePlantGloompult.tscn", "character.attack.0", new Vector2(73f, 33f), new Vector2(-1.5f, -0.5f)),
			("res://Asset/Anime/Character/Plant/Chapter2/ScaredySunShroom/Scene/TowerDefensePlantScaredySunShroom.tscn", "character.scared", new Vector2(134f, 144f), Vector2.Zero),
			("res://Asset/Anime/Character/Plant/Chapter4/PlanternTanglekelp/Scene/TowerDefensePlantPlanternTanglekelp.tscn", "character.attack.0", new Vector2(84f, 33f), new Vector2(0f, -0.5f)),
			("res://Asset/Anime/Character/Plant/Chapter4/PlanternTanglekelp/Scene/TowerDefensePlantPlanternTanglekelp.tscn", "character.attack.1", new Vector2(73f, 73f), new Vector2(-1.5f, -0.5f)),
			("res://Asset/Anime/Character/Plant/Chapter5/Coffeeleaf/Scene/TowerDefensePlantCoffeeleaf.tscn", "ComponentManager/BlockComponent/Area2D", new Vector2(73f, 33f), Vector2.Zero),
			("res://Asset/Anime/Character/Plant/Chapter5/Nutleaf/Scene/TowerDefensePlantNutleaf.tscn", "ComponentManager/BlockComponent/Area2D", new Vector2(73f, 33f), new Vector2(-1.5f, -0.5f)),
			("res://Asset/Anime/Character/Plant/Chapter6/ChomperPot/Scene/TowerDefensePlantChomperPot.tscn", "character.attack.0", new Vector2(92f, 46f), Vector2.Zero),
			("res://Asset/Anime/Character/Plant/Chapter6/GloomSquash/Scene/TowerDefensePlantGloomSquash.tscn", "character.attack.0", new Vector2(73f, 33f), new Vector2(-1.5f, -0.5f)),
			("res://Asset/Anime/Character/Plant/Chapter6/SpringFumeShroom/Scene/TowerDefensePlantSpringFumeShroom.tscn", "ComponentManager/BlockComponent/Area2D", new Vector2(73f, 33f), new Vector2(-1.5f, -0.5f)),
			("res://Asset/Anime/Character/Plant/Chapter6/Starleaf/Scene/TowerDefensePlantStarleaf.tscn", "ComponentManager/BlockComponent/Area2D", new Vector2(73f, 33f), new Vector2(-1.5f, -0.5f)),
			("res://Asset/Anime/Character/Plant/Chapter7/SquidGloomShroom/Scene/TowerDefensePlantSquidGloomShroom.tscn", "character.attack.0", new Vector2(73f, 33f), new Vector2(-1.5f, -0.5f)),
			("res://Asset/Anime/Character/Plant/Chapter7/UmbrellaTorch/Scene/TowerDefensePlantUmbrellaTorch.tscn", "ComponentManager/BlockComponent/Area2D", new Vector2(73f, 33f), Vector2.Zero),
			("res://Asset/Anime/Character/Plant/Chapter7/UmbrellaTorch/Scene/TowerDefensePlantUmbrellaTorch.tscn", "ComponentManager/BlockComponent/Area2D2", new Vector2(34f, 33f), Vector2.Zero),
			("res://Asset/Anime/Character/Plant/Cover/FireTallnut/Scene/TowerDefensePlantFireTallnut.tscn", "character.attack.0", new Vector2(73f, 47f), new Vector2(-1.5f, -0.5f)),
			("res://Asset/Anime/Character/Plant/Cover/GloomShroom/Scene/TowerDefensePlantGloomShroom.tscn", "character.attack.0", new Vector2(73f, 33f), new Vector2(-1.5f, -0.5f)),
			("res://Asset/Anime/Character/Plant/Cover/SpringFumeShroomIceCold/Scene/TowerDefensePlantSpringFumeShroomIceCold.tscn", "ComponentManager/BlockComponent/Area2D", new Vector2(73f, 33f), new Vector2(-1.5f, -0.5f)),
			("res://Asset/Anime/Character/Plant/Diamond/Pinata/Scene/TowerDefensePlantPinata.tscn", "character.attack.0", new Vector2(73f, 33f), new Vector2(-1.5f, -0.5f)),
			("res://Asset/Anime/Character/Plant/Gold/ButterGloomShroom/Scene/TowerDefensePlantButterGloomShroom.tscn", "character.attack.0", new Vector2(73f, 33f), new Vector2(-1.5f, -0.5f)),
			("res://Asset/Anime/Character/Plant/Gold/QueenSunFlower/Scene/TowerDefensePlantQueenSunFlower.tscn", "character.attack.0", new Vector2(73f, 73f), new Vector2(-1.5f, -0.5f)),
			("res://Asset/Anime/Character/Plant/Other/MedicPlantern/Scene/TowerDefenseMedicPlantern.tscn", "Node2D/CheckArea", new Vector2(73f, 73f), new Vector2(-1.5f, -0.5f)),
			("res://Asset/Anime/Character/Plant/Other/PotatoSquash/Scene/TowerDefensePlantPotatoSquash.tscn", "character.attack.0", new Vector2(368f, 320f), Vector2.Zero),
			("res://Asset/Anime/Character/Plant/Other/TallnutUmbrella/Scene/TowerDefensePlantTallnutUmbrella.tscn", "ComponentManager/BlockComponent/Area2D", new Vector2(73f, 33f), Vector2.Zero),
			("res://Asset/Anime/Character/Plant/Star/JewelBomb/Scene/TowerDefensePlantJewelBomb.tscn", "character.attack.0", new Vector2(73f, 33f), new Vector2(-1.5f, -0.5f)),
			("res://Asset/Anime/Character/Plant/Star/LilyLeaf/Scene/TowerDefensePlantLilyLeaf.tscn", "ComponentManager/BlockComponent/Area2D", new Vector2(73f, 33f), Vector2.Zero),
			("res://Asset/Anime/Character/Plant/Star/MiniPinata/Scene/TowerDefensePlantMiniPinata.tscn", "character.attack.0", new Vector2(73f, 33f), new Vector2(-1.5f, -0.5f)),
			("res://Asset/Anime/Character/Zombie/Boss/Boss/Scene/TowerDefenseZombieBoss.tscn", "character.attack.0", new Vector2(299f, 141f), Vector2.Zero),
			("res://Asset/Anime/Character/Zombie/Boss/Boss/Scene/TowerDefenseZombieBoss.tscn", "character.attack.1", new Vector2(299f, 141f), Vector2.Zero),
			("res://Asset/Anime/Character/Zombie/Boss/Boss/Scene/TowerDefenseZombieBoss.tscn", "character.attack.2", new Vector2(299f, 141f), Vector2.Zero),
			("res://Asset/Anime/Character/Zombie/Boss/Boss/Scene/TowerDefenseZombieBoss.tscn", "character.attack.3", new Vector2(299f, 141f), Vector2.Zero),
			("res://Asset/Anime/Character/Zombie/Boss/Boss/Scene/TowerDefenseZombieBoss.tscn", "character.attack.4", new Vector2(299f, 141f), Vector2.Zero),
			("res://Asset/Anime/Character/Zombie/Chapter1/Football/Scene/BlackGloomShroom/TowerDefenseZombieFootballBlackGloomShroom.tscn", "character.attack.1", new Vector2(73f, 33f), new Vector2(-0.5f, -2.5f)),
			("res://Asset/Anime/Character/Zombie/Chapter1/Football/Scene/GloomShroom/TowerDefenseZombieFootballGloomShroom.tscn", "character.attack.1", new Vector2(73f, 33f), new Vector2(10.5f, -1.5f)),
			("res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Scene/Gloompult/TowerDefenseZombieGargantuarGloompult.tscn", "character.attack.1", new Vector2(73f, 33f), new Vector2(3.5f, 13.5f)),
			("res://Asset/Anime/Character/Zombie/Chapter4/ZombieMagnetic/Scene/TowerDefenseZombieMagnetic.tscn", "ComponentManager/ChangeProjectileStateComponent/ChangeCheckArea", new Vector2(368f, 320f), Vector2.Zero),
			("res://Asset/Anime/Character/Zombie/Chapter8/NecromancerG/Scene/TowerDefenseZombieNecromancerG.tscn", "Node2D/Area2D", new Vector2(73f, 33f), new Vector2(10.5f, -1.5f))
		};
		for (int i = 0; i < array.Length; i++)
		{
			(string, string, Vector2, Vector2) tuple = array[i];
			if (tuple.Item2.StartsWith("character.attack.", StringComparison.Ordinal))
			{
				CheckAttackRectangleShapeScene(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, "Attack rectangle");
			}
			else if (tuple.Item2 == "character.scared")
			{
				CheckDefinitionRectangleShape<ScaredComponentDefinition>(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, "Scared rectangle");
			}
			else
			{
				CheckRectangleShapeScene(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, "Rectangle");
			}
		}
	}

	private FireComponent FindFirstFireComponent(Node root, string instanceId = null)
	{
		return FindComponentRuntime<FireComponent>(root, instanceId);
	}

	private T FindComponentRuntime<T>(Node root, string instanceId = null) where T : CharacterComponentRuntime
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return null;
		}
		if (root.GetParent() == null)
		{
			AddChild(root, forceReadableName: false, InternalMode.Disabled);
		}
		ComponentManager componentManager = (root as TowerDefenseCharacter)?.componentManager;
		if (!string.IsNullOrEmpty(instanceId))
		{
			if (componentManager == null)
			{
				return null;
			}
			return componentManager.GetRuntime<T>(instanceId);
		}
		if (componentManager == null)
		{
			return null;
		}
		return componentManager.GetRuntime<T>();
	}

	private CharacterComponentRuntime FindComponentRuntimeByLegacyPath(Node root, string legacyPath)
	{
		if (!GodotObject.IsInstanceValid(root) || string.IsNullOrEmpty(legacyPath))
		{
			return null;
		}
		if (root.GetParent() == null)
		{
			AddChild(root, forceReadableName: false, InternalMode.Disabled);
		}
		ComponentManager componentManager = (root as TowerDefenseCharacter)?.componentManager;
		if (!GodotObject.IsInstanceValid(componentManager))
		{
			return null;
		}
		string[] array = legacyPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
		for (int i = Math.Max(0, array.Length - 2); i < array.Length; i++)
		{
			if (componentManager.TryGetRuntimeByLegacyNodeName(array[i], out var runtime))
			{
				return runtime;
			}
		}
		return null;
	}

	private CharacterComponentRuntime FindComponentRuntimeByInstanceId(Node root, string instanceId)
	{
		if (!GodotObject.IsInstanceValid(root) || string.IsNullOrEmpty(instanceId))
		{
			return null;
		}
		if (root.GetParent() == null)
		{
			AddChild(root, forceReadableName: false, InternalMode.Disabled);
		}
		ComponentManager componentManager = (root as TowerDefenseCharacter)?.componentManager;
		if (!GodotObject.IsInstanceValid(componentManager) || !componentManager.TryGetRuntimeByInstanceId(instanceId, out var runtime))
		{
			return null;
		}
		return runtime;
	}

	private static AabbShape2DResource GetRuntimeShapeResource(CharacterComponentRuntime runtime, int shapeIndex, AabbShape2DResource definitionShape)
	{
		if (!(runtime is CharacterAabbAreaComponent characterAabbAreaComponent))
		{
			if (!(runtime is BlockComponent blockComponent))
			{
				if (!(runtime is ChangeProjectileComponent changeProjectileComponent))
				{
					if (!(runtime is ChangeProjectileStateComponent changeProjectileStateComponent))
					{
						if (runtime is PeriodicAreaEventComponent periodicAreaEventComponent && shapeIndex == 0)
						{
							return periodicAreaEventComponent.checkShape;
						}
					}
					else if (shapeIndex == 0)
					{
						return changeProjectileStateComponent.checkShape;
					}
				}
				else if (shapeIndex == 0)
				{
					return changeProjectileComponent.checkShape;
				}
			}
			else
			{
				if (shapeIndex == 0)
				{
					return blockComponent.checkShape;
				}
				BlockComponent blockComponent2 = blockComponent;
				if (shapeIndex == 1)
				{
					return blockComponent2.reboundProjectileShape;
				}
			}
			return definitionShape;
		}
		return characterAabbAreaComponent.GetShapeResource(shapeIndex);
	}

	private static bool TrySetRuntimeRectangleSize(CharacterComponentRuntime runtime, int shapeIndex, Vector2 size, out Vector2 actualSize)
	{
		actualSize = default(Vector2);
		if (runtime is CharacterAabbAreaComponent characterAabbAreaComponent)
		{
			if (characterAabbAreaComponent.SetRuntimeRectangleSize(shapeIndex, size))
			{
				return characterAabbAreaComponent.TryGetRuntimeRectangleSize(shapeIndex, out actualSize);
			}
			return false;
		}
		if (runtime is BlockComponent blockComponent && shapeIndex == 0)
		{
			if (!blockComponent.SetCheckRectangleSize(size) || !(blockComponent.checkShape?.Geometry is RectangleShape2D rectangleShape2D))
			{
				return false;
			}
			actualSize = rectangleShape2D.Size;
			return true;
		}
		if (runtime is ChangeProjectileStateComponent changeProjectileStateComponent && shapeIndex == 0 && changeProjectileStateComponent.checkShape?.Geometry is RectangleShape2D rectangleShape2D2)
		{
			rectangleShape2D2.Size = size;
			actualSize = rectangleShape2D2.Size;
			return true;
		}
		return false;
	}

	private static TDefinition FindComponentDefinition<TDefinition>(Node root, string instanceId = null) where TDefinition : CharacterComponentDefinition
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return null;
		}
		TowerDefenseCharacter towerDefenseCharacter = root as TowerDefenseCharacter;
		CharacterComponentSet characterComponentSet = towerDefenseCharacter?.ComponentSet ?? towerDefenseCharacter?.componentManager?.ComponentSet;
		if (!GodotObject.IsInstanceValid(characterComponentSet))
		{
			return null;
		}
		foreach (CharacterComponentDefinition flattenedDefinition in characterComponentSet.GetFlattenedDefinitions())
		{
			if (flattenedDefinition is TDefinition result && (string.IsNullOrEmpty(instanceId) || string.Equals(flattenedDefinition.InstanceId, instanceId, StringComparison.Ordinal)))
			{
				return result;
			}
		}
		return null;
	}

	private static TDefinition FindComponentDefinitionByLegacyNodeName<TDefinition>(Node root, string legacyNodeName) where TDefinition : CharacterComponentDefinition
	{
		if (!GodotObject.IsInstanceValid(root) || string.IsNullOrEmpty(legacyNodeName))
		{
			return null;
		}
		TowerDefenseCharacter towerDefenseCharacter = root as TowerDefenseCharacter;
		CharacterComponentSet characterComponentSet = towerDefenseCharacter?.ComponentSet ?? towerDefenseCharacter?.componentManager?.ComponentSet;
		if (!GodotObject.IsInstanceValid(characterComponentSet))
		{
			return null;
		}
		foreach (CharacterComponentDefinition flattenedDefinition in characterComponentSet.GetFlattenedDefinitions())
		{
			if (!(flattenedDefinition is TDefinition result) || flattenedDefinition.LegacyNodeNames == null)
			{
				continue;
			}
			for (int i = 0; i < flattenedDefinition.LegacyNodeNames.Count; i++)
			{
				if (string.Equals(flattenedDefinition.LegacyNodeNames[i].ToString(), legacyNodeName, StringComparison.Ordinal))
				{
					return result;
				}
			}
		}
		return null;
	}

	private static bool HasRayProbeDescendant(Node root)
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return false;
		}
		foreach (Node child in root.GetChildren())
		{
			if (child is AabbRayProbe2D || HasRayProbeDescendant(child))
			{
				return true;
			}
		}
		return false;
	}

	private static bool HasLegacyFireComponentNode(Node root)
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return false;
		}
		Node nodeOrNull = root.GetNodeOrNull("ComponentManager");
		if (!GodotObject.IsInstanceValid(nodeOrNull))
		{
			return false;
		}
		foreach (Node child in nodeOrNull.GetChildren())
		{
			if (child.Name.ToString().StartsWith("FireComponent", StringComparison.Ordinal))
			{
				return true;
			}
		}
		return false;
	}

	private static void QueueSceneInstanceForCleanup(Node instance)
	{
		if (GodotObject.IsInstanceValid(instance))
		{
			instance.QueueFree();
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[CollisionResourceTest] " + message);
		}
	}

	private static bool RectApprox(Rect2 left, Rect2 right)
	{
		if (left.Position.IsEqualApprox(right.Position))
		{
			return left.Size.IsEqualApprox(right.Size);
		}
		return false;
	}
}
