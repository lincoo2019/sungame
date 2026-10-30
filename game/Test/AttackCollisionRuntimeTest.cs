using System;
using Godot;
using Godot.Collections;

public partial class AttackCollisionRuntimeTest : Node
{
	private int _checks;

	private int _failures;

	public override void _Ready()
	{
		try
		{
			RunRectangleIsolationAndLifecycle();
			RunSegmentIsolation();
			RunCompoundUnionAndShapeDisable();
			RunHitBoxSourceFallbackAndRebind();
			RunHitBoxBoundsRevisionInvalidation();
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[AttackCollisionRuntimeTest] Unexpected exception: {value}");
		}
		bool flag = _failures == 0;
		GD.Print($"ATTACK_COLLISION_RUNTIME_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 1 : 0);
	}

	private void RunRectangleIsolationAndLifecycle()
	{
		RectangleShape2D rectangleShape2D = new RectangleShape2D
		{
			Size = new Vector2(20f, 10f)
		};
		AabbShape2DResource aabbShape2DResource = new AabbShape2DResource
		{
			Geometry = rectangleShape2D,
			LocalTransform = new Transform2D(0f, new Vector2(5f, 0f)),
			DebugDraw = false
		};
		AttackComponentDefinition definition = CreateDefinition("attack.runtime.rectangle", aabbShape2DResource);
		TowerDefenseCharacter towerDefenseCharacter = CreateOwner(new Vector2(100f, 200f));
		TowerDefenseCharacter towerDefenseCharacter2 = CreateOwner(new Vector2(-30f, 40f));
		ComponentManager manager = new ComponentManager();
		ComponentManager manager2 = new ComponentManager();
		AttackComponent attackComponent = Bind(definition, manager, towerDefenseCharacter);
		AttackComponent attackComponent2 = Bind(definition, manager2, towerDefenseCharacter2);
		Check(attackComponent.CheckAreaShapeCount == 1 && attackComponent2.CheckAreaShapeCount == 1, "Both runtimes must see the shared Definition shape.");
		Check(attackComponent.TryGetCheckAreaRectangleSize(0, out var size) && size == rectangleShape2D.Size, "Runtime A must read the configured rectangle size.");
		Check(attackComponent2.TryGetCheckAreaRectangleSize(0, out var size2) && size2 == rectangleShape2D.Size, "Runtime B must read the configured rectangle size.");
		Vector2 vector = new Vector2(40f, 30f);
		Check(attackComponent.SetCheckAreaRectangleSize(0, vector), "Runtime A must accept a rectangle override.");
		Check(attackComponent.SetCheckAreaShapeLocalOrigin(0, new Vector2(10f, 5f)), "Runtime A must accept a local-origin override.");
		Check(attackComponent.TryGetCheckAreaRectangleSize(0, out size) && size == vector, "Runtime A must expose its effective rectangle override.");
		Check(attackComponent2.TryGetCheckAreaRectangleSize(0, out size2) && size2 == new Vector2(20f, 10f), "Runtime A must not change Runtime B.");
		Check(rectangleShape2D.Size == new Vector2(20f, 10f) && aabbShape2DResource.LocalTransform.Origin == new Vector2(5f, 0f), "Runtime overrides must not mutate the shared Definition Resource.");
		Check(attackComponent.TryGetCheckAreaWorldRect(out var worldRect) && RectApprox(worldRect, new Rect2(90f, 190f, 40f, 30f)), "Rectangle world bounds must include owner and local transforms.");
		Check(attackComponent.SetCheckAreaShapeWorldOrigin(0, new Vector2(150f, 250f)), "Runtime A must accept a world-origin override.");
		Check(attackComponent.TryGetCheckAreaShapeLocalTransform(0, out var localTransform) && localTransform.Origin.IsEqualApprox(new Vector2(50f, 50f)), "World origin must be converted into owner-local coordinates.");
		Check(attackComponent.TryGetCheckAreaWorldRect(out worldRect) && RectApprox(worldRect, new Rect2(130f, 235f, 40f, 30f)), "World-origin override must update effective bounds.");
		Check(attackComponent.DrawCheckAreaCollisionPreview(towerDefenseCharacter), "Runtime preview must accept effective Attack geometry.");
		attackComponent.Detach(ComponentDetachReason.TemporaryTreeExit);
		attackComponent.Bind(manager, towerDefenseCharacter, definition);
		Check(attackComponent.TryGetCheckAreaRectangleSize(0, out size) && size == vector, "Temporary tree exit must retain per-character geometry overrides.");
		Check(attackComponent.SetCheckAreaEnabled(enabled: false) && !attackComponent.TryGetCheckAreaWorldRect(out var worldRect2), "Disabling an Attack area must prevent stale bounds from being returned.");
		Check(attackComponent2.TryGetCheckAreaWorldRect(out worldRect2), "Disabling Runtime A must not disable Runtime B.");
		Dictionary data = attackComponent.ExportComponentSave();
		TowerDefenseCharacter towerDefenseCharacter3 = CreateOwner(towerDefenseCharacter.Position);
		ComponentManager manager3 = new ComponentManager();
		AttackComponent attackComponent3 = Bind(definition, manager3, towerDefenseCharacter3);
		attackComponent3.ImportComponentSave(data, null);
		Check(!attackComponent3.TryGetCheckAreaWorldRect(out worldRect2) && attackComponent3.TryGetCheckAreaRectangleSize(0, out var size3) && size3 == vector, "Save import must restore area enabled state and geometry overrides.");
		attackComponent2.SyncDeserialize(attackComponent.SyncSerialize());
		Check(!attackComponent2.TryGetCheckAreaWorldRect(out worldRect2) && attackComponent2.TryGetCheckAreaRectangleSize(0, out var size4) && size4 == vector, "Network sync must restore area enabled state and geometry overrides.");
		attackComponent.Release();
		attackComponent2.Release();
		attackComponent3.Release();
		towerDefenseCharacter.Free();
		towerDefenseCharacter2.Free();
		towerDefenseCharacter3.Free();
	}

	private void RunSegmentIsolation()
	{
		SegmentShape2D segmentShape2D = new SegmentShape2D
		{
			A = new Vector2(0f, -2f),
			B = new Vector2(-10f, 3f)
		};
		AabbShape2DResource aabbShape2DResource = new AabbShape2DResource
		{
			Geometry = segmentShape2D,
			LocalTransform = Transform2D.Identity,
			DebugDraw = false
		};
		AttackComponentDefinition definition = CreateDefinition("attack.runtime.segment", aabbShape2DResource);
		TowerDefenseCharacter towerDefenseCharacter = CreateOwner(Vector2.Zero);
		TowerDefenseCharacter towerDefenseCharacter2 = CreateOwner(Vector2.Zero);
		ComponentManager manager = new ComponentManager();
		ComponentManager manager2 = new ComponentManager();
		AttackComponent attackComponent = Bind(definition, manager, towerDefenseCharacter);
		AttackComponent attackComponent2 = Bind(definition, manager2, towerDefenseCharacter2);
		Check(attackComponent.SetCheckAreaSegmentLengthX(0, 35f), "Segment length override must be accepted.");
		Check(attackComponent.TryGetCheckAreaSegmentEnd(0, out var endpoint) && endpoint.IsEqualApprox(new Vector2(-35f, 3f)), "Segment override must preserve configured direction and Y endpoint.");
		Check(attackComponent2.TryGetCheckAreaSegmentEnd(0, out var endpoint2) && endpoint2 == segmentShape2D.B, "Segment override must remain isolated per runtime.");
		Check(segmentShape2D.B == new Vector2(-10f, 3f), "Segment override must not mutate shared geometry.");
		attackComponent.Release();
		attackComponent2.Release();
		towerDefenseCharacter.Free();
		towerDefenseCharacter2.Free();
	}

	private void RunCompoundUnionAndShapeDisable()
	{
		AabbShape2DResource aabbShape2DResource = new AabbShape2DResource
		{
			Geometry = new RectangleShape2D
			{
				Size = new Vector2(10f, 10f)
			},
			LocalTransform = new Transform2D(0f, new Vector2(-10f, 0f))
		};
		AabbShape2DResource aabbShape2DResource2 = new AabbShape2DResource
		{
			Geometry = new RectangleShape2D
			{
				Size = new Vector2(10f, 20f)
			},
			LocalTransform = new Transform2D(0f, new Vector2(15f, 0f))
		};
		AttackComponentDefinition definition = CreateDefinition("attack.runtime.compound", aabbShape2DResource, aabbShape2DResource2);
		TowerDefenseCharacter towerDefenseCharacter = CreateOwner(Vector2.Zero);
		ComponentManager manager = new ComponentManager();
		AttackComponent attackComponent = Bind(definition, manager, towerDefenseCharacter);
		Check(attackComponent.TryGetCheckAreaWorldRect(out var worldRect) && RectApprox(worldRect, new Rect2(-15f, -10f, 35f, 20f)), "Multiple Definition shapes must union in order.");
		Check(attackComponent.SetCheckAreaShapeEnabled(1, enabled: false) && attackComponent.TryGetCheckAreaWorldRect(out var worldRect2) && RectApprox(worldRect2, new Rect2(-15f, -5f, 10f, 10f)), "Per-shape disable must remove that shape from effective bounds.");
		attackComponent.Release();
		towerDefenseCharacter.Free();
	}

	private void RunHitBoxSourceFallbackAndRebind()
	{
		AabbShape2DResource aabbShape2DResource = new AabbShape2DResource
		{
			Geometry = new RectangleShape2D
			{
				Size = new Vector2(10f, 10f)
			},
			DebugDraw = false
		};
		AttackComponentDefinition definition = CreateDefinition("attack.runtime.hitbox-source", aabbShape2DResource);
		TowerDefenseCharacter towerDefenseCharacter = CreateOwner(Vector2.Zero);
		TowerDefenseCharacter towerDefenseCharacter2 = CreateOwner(new Vector2(100f, 100f));
		towerDefenseCharacter2.HitBoxDefinition = new CharacterHitBoxDefinition
		{
			Size = new Vector2(30f, 40f)
		};
		ComponentManager manager = new ComponentManager();
		AttackComponent attackComponent = Bind(definition, manager, towerDefenseCharacter);
		attackComponent.SetCheckHitBoxSource(towerDefenseCharacter2);
		Check(attackComponent.TryGetCheckAreaWorldRect(out var worldRect) && RectApprox(worldRect, new Rect2(85f, 80f, 30f, 40f)), "A valid runtime HitBox source must replace Definition geometry.");
		attackComponent.Detach(ComponentDetachReason.TemporaryTreeExit);
		attackComponent.Bind(manager, towerDefenseCharacter, definition);
		Check(attackComponent.TryGetCheckAreaWorldRect(out worldRect) && RectApprox(worldRect, new Rect2(85f, 80f, 30f, 40f)), "Temporary tree exit must preserve the runtime HitBox source.");
		towerDefenseCharacter2.HitBoxDefinition = null;
		Check(attackComponent.TryGetCheckAreaWorldRect(out var worldRect2) && RectApprox(worldRect2, new Rect2(-5f, -5f, 10f, 10f)), "An invalid HitBox source must fall back to Definition geometry without a stale cache.");
		Check(attackComponent.SetCheckAreaEnabled(enabled: false) && !attackComponent.TryGetCheckAreaWorldRect(out var _), "Global area disable must also disable a runtime HitBox source.");
		attackComponent.Release();
		towerDefenseCharacter.Free();
		towerDefenseCharacter2.Free();
	}

	private void RunHitBoxBoundsRevisionInvalidation()
	{
		TowerDefenseCharacter towerDefenseCharacter = CreateOwner(Vector2.Zero);
		ulong hitBoxBoundsRevision = towerDefenseCharacter.HitBoxBoundsRevision;
		towerDefenseCharacter._Notification(2000);
		Check(towerDefenseCharacter.HitBoxBoundsRevision != hitBoxBoundsRevision, "Transform changes must invalidate cached Attack hit-box bounds.");
		ulong hitBoxBoundsRevision2 = towerDefenseCharacter.HitBoxBoundsRevision;
		towerDefenseCharacter.HitBoxDefinition = new CharacterHitBoxDefinition
		{
			Size = new Vector2(20f, 30f)
		};
		Check(towerDefenseCharacter.HitBoxBoundsRevision != hitBoxBoundsRevision2, "Replacing the hit-box definition must invalidate cached Attack bounds.");
		ulong hitBoxBoundsRevision3 = towerDefenseCharacter.HitBoxBoundsRevision;
		towerDefenseCharacter.SetHitBoxEnabled(enabled: false);
		Check(towerDefenseCharacter.HitBoxBoundsRevision != hitBoxBoundsRevision3, "Changing hit-box availability must invalidate cached Attack bounds.");
		ulong hitBoxBoundsRevision4 = towerDefenseCharacter.HitBoxBoundsRevision;
		_ = towerDefenseCharacter.WorldHitRect;
		Check(towerDefenseCharacter.HitBoxBoundsRevision == hitBoxBoundsRevision4, "Reading stable hit-box bounds must not invalidate the Attack range cache.");
		towerDefenseCharacter.Free();
	}

	private static AttackComponentDefinition CreateDefinition(string instanceId, params AabbShape2DResource[] shapes)
	{
		AttackComponentDefinition attackComponentDefinition = new AttackComponentDefinition
		{
			ComponentTypeId = "AttackComponent",
			DefinitionId = instanceId,
			InstanceId = instanceId,
			WireIndex = 0
		};
		foreach (AabbShape2DResource item in shapes)
		{
			attackComponentDefinition.checkShapeResources.Add(item);
		}
		return attackComponentDefinition;
	}

	private static TowerDefenseCharacter CreateOwner(Vector2 position)
	{
		return new TowerDefenseCharacter
		{
			Position = position
		};
	}

	private static AttackComponent Bind(AttackComponentDefinition definition, ComponentManager manager, TowerDefenseCharacter owner)
	{
		AttackComponent attackComponent = new AttackComponent();
		attackComponent.Bind(manager, owner, definition);
		return attackComponent;
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[AttackCollisionRuntimeTest] " + message);
		}
	}

	private static bool RectApprox(Rect2 actual, Rect2 expected)
	{
		if (actual.Position.IsEqualApprox(expected.Position))
		{
			return actual.Size.IsEqualApprox(expected.Size);
		}
		return false;
	}
}
