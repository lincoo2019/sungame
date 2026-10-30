using System;
using System.Threading.Tasks;
using Godot;

public partial class ShadowComponentRuntimeTest : Node2D
{
	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		bool previousShadowMultiMesh = ShadowComponent.UseMultiMesh;
		bool previousRendererEnabled = TowerDefenseShadowMultiMeshRenderer.Enabled;
		try
		{
			if (OS.GetEnvironment("PVZHE_SHADOW_SCREEN_ENTRY_ONLY") == "1")
			{
				RunFirstSubmissionHandoff();
			}
			else
			{
				RunShadowVisualDataizationContracts();
				await RunCarrierShadowDataizationContracts();
				RunFirstSubmissionHandoff();
				await RunRegisteredDataFastPathHeightScale();
				RunTransformPointScaleNotification();
				await RunRapidCellMovementRetargeting();
			}
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[ShadowComponentRuntimeTest] Unexpected exception: {value}");
		}
		finally
		{
			ShadowComponent.UseMultiMesh = previousShadowMultiMesh;
			TowerDefenseShadowMultiMeshRenderer.Enabled = previousRendererEnabled;
		}
		bool flag = _failures == 0;
		GD.Print($"SHADOW_COMPONENT_RUNTIME_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 1 : 0);
	}

	private async Task RunCarrierShadowDataizationContracts()
	{
		PackedScene damagePartScene = GD.Load<PackedScene>("res://Prefab/TowerDefense/DamagePart/DamagePartDrop.tscn");
		DamagePartDrop damagePart = damagePartScene?.Instantiate<DamagePartDrop>(PackedScene.GenEditState.Disabled);
		Check(GodotObject.IsInstanceValid(damagePart), "The production DamagePartDrop scene must instantiate.");
		if (GodotObject.IsInstanceValid(damagePart))
		{
			AddChild(damagePart, forceReadableName: false, InternalMode.Disabled);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			Check(damagePart.GetNodeOrNull<Sprite2D>("%ShadowSprite") == null, "DamagePartDrop must remove its authored ShadowSprite after entering the tree.");
			damagePart.Refresh();
			damagePart._Process(0.0);
			Check(FindChild("TowerDefenseShadowMultiMeshRenderer", recursive: true, owned: false) is TowerDefenseShadowMultiMeshRenderer, "DamagePart shadow data must submit through the shared MultiMesh renderer.");
			damagePart.Free();
		}
		DamagePartDrop customModDamagePart = damagePartScene?.Instantiate<DamagePartDrop>(PackedScene.GenEditState.Disabled);
		Check(GodotObject.IsInstanceValid(customModDamagePart), "A custom Mod DamagePart fixture must instantiate from the production carrier.");
		if (GodotObject.IsInstanceValid(customModDamagePart))
		{
			Sprite2D customModShadow = customModDamagePart.GetNodeOrNull<Sprite2D>("%ShadowSprite");
			customModShadow.VisibilityLayer = 7u;
			customModShadow.AddChild(new Node2D
			{
				Name = "CustomModShadowChild"
			}, forceReadableName: false, InternalMode.Disabled);
			AddChild(customModDamagePart, forceReadableName: false, InternalMode.Disabled);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			Check(customModDamagePart.GetNodeOrNull<Sprite2D>("%ShadowSprite") == customModShadow, "A Mod DamagePart shadow with custom topology must retain its native node.");
			customModDamagePart.Refresh();
			customModDamagePart._Process(0.0);
			Check(customModShadow.VisibilityLayer == 7 && customModShadow.GetChildCount() == 1, "A custom Mod DamagePart shadow must not be flattened or hidden by the shared MultiMesh path.");
			customModDamagePart.Free();
		}
		TowerDefenseProjectile projectile = GD.Load<PackedScene>("res://Prefab/TowerDefense/Projectile/TowerDefenseProjectile.tscn")?.Instantiate<TowerDefenseProjectile>(PackedScene.GenEditState.Disabled);
		Check(GodotObject.IsInstanceValid(projectile), "The legacy-compatible projectile scene must instantiate.");
		if (GodotObject.IsInstanceValid(projectile))
		{
			AddChild(projectile, forceReadableName: false, InternalMode.Disabled);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			Check(projectile.GetNodeOrNull<Sprite2D>("%ShadowSprite") == null, "A legacy-compatible projectile must remove its authored ShadowSprite after entering the tree.");
			projectile.Free();
		}
	}

	private void RunShadowVisualDataizationContracts()
	{
		Image image = Image.CreateEmpty(2, 2, useMipmaps: false, Image.Format.Rgba8);
		image.Fill(Colors.White);
		ImageTexture imageTexture = ImageTexture.CreateFromImage(image);
		Node2D node2D = new Node2D
		{
			Name = "InheritedModOwner"
		};
		AddChild(node2D, forceReadableName: false, InternalMode.Disabled);
		TowerDefenseCharacterShadowSprite towerDefenseCharacterShadowSprite = new TowerDefenseCharacterShadowSprite
		{
			Name = "ShadowSprite",
			Texture = imageTexture,
			Position = new Vector2(12f, 36f),
			Scale = new Vector2(1.2f, 1.2f)
		};
		node2D.AddChild(towerDefenseCharacterShadowSprite, forceReadableName: false, InternalMode.Disabled);
		TowerDefenseShadowVisual towerDefenseShadowVisual = TowerDefenseShadowVisual.Capture(node2D, towerDefenseCharacterShadowSprite);
		Check(GodotObject.IsInstanceValid(towerDefenseShadowVisual) && !towerDefenseShadowVisual.UsesLegacyNode && node2D.GetChildCount() == 0, "A property-only Mod shadow inherited from the built-in scene must use the same node-free data path.");
		Check(towerDefenseShadowVisual.Texture == imageTexture && towerDefenseShadowVisual.Position.IsEqualApprox(new Vector2(12f, 36f)) && towerDefenseShadowVisual.Scale.IsEqualApprox(new Vector2(1.2f, 1.2f)), "The Mod-compatible data path must preserve the authored shadow texture and transform.");
		node2D.Free();
		Node2D node2D2 = new Node2D
		{
			Name = "PlainModOwner"
		};
		AddChild(node2D2, forceReadableName: false, InternalMode.Disabled);
		Sprite2D sprite2D = new Sprite2D
		{
			Name = "ShadowSprite",
			Texture = imageTexture,
			Visible = true
		};
		node2D2.AddChild(sprite2D, forceReadableName: false, InternalMode.Disabled);
		TowerDefenseShadowVisual towerDefenseShadowVisual2 = TowerDefenseShadowVisual.Capture(node2D2, sprite2D);
		Check(GodotObject.IsInstanceValid(towerDefenseShadowVisual2) && !towerDefenseShadowVisual2.UsesLegacyNode && node2D2.GetChildCount() == 0, "An unscripted Mod ShadowSprite leaf must be captured without retaining a Godot node.");
		node2D2.Free();
		Node2D node2D3 = new Node2D
		{
			Name = "CustomModOwner"
		};
		AddChild(node2D3, forceReadableName: false, InternalMode.Disabled);
		Sprite2D sprite2D2 = new Sprite2D
		{
			Name = "ShadowSprite",
			Texture = imageTexture
		};
		sprite2D2.AddChild(new Node2D
		{
			Name = "CustomShadowChild"
		}, forceReadableName: false, InternalMode.Disabled);
		node2D3.AddChild(sprite2D2, forceReadableName: false, InternalMode.Disabled);
		TowerDefenseShadowVisual towerDefenseShadowVisual3 = TowerDefenseShadowVisual.Capture(node2D3, sprite2D2);
		Check(GodotObject.IsInstanceValid(towerDefenseShadowVisual3) && towerDefenseShadowVisual3.UsesLegacyNode && towerDefenseShadowVisual3.LegacySprite == sprite2D2 && node2D3.GetChildCount() == 1, "A Mod shadow with custom child topology must retain its legacy node compatibility path.");
		towerDefenseShadowVisual3.Visible = false;
		Check(!sprite2D2.Visible, "The compatibility facade must proxy state changes to a retained custom Mod shadow node.");
		towerDefenseShadowVisual3.Visible = true;
		Check(!TowerDefenseShadowMultiMeshRenderer.SubmitVisual(towerDefenseShadowVisual3, 0), "A custom Mod shadow must stay on its complete native-node rendering path instead of flattening only its parent Sprite into MultiMesh.");
		node2D3.Free();
		Node2D node2D4 = new Node2D
		{
			Name = "TopLevelModOwner"
		};
		AddChild(node2D4, forceReadableName: false, InternalMode.Disabled);
		Sprite2D sprite2D3 = new Sprite2D
		{
			Name = "ShadowSprite",
			Texture = imageTexture,
			TopLevel = true
		};
		node2D4.AddChild(sprite2D3, forceReadableName: false, InternalMode.Disabled);
		TowerDefenseShadowVisual towerDefenseShadowVisual4 = TowerDefenseShadowVisual.Capture(node2D4, sprite2D3);
		Check(GodotObject.IsInstanceValid(towerDefenseShadowVisual4) && towerDefenseShadowVisual4.UsesLegacyNode && towerDefenseShadowVisual4.LegacySprite == sprite2D3, "A top-level Mod shadow must retain its native world-relative transform path.");
		node2D4.Free();
	}

	private async Task RunRapidCellMovementRetargeting()
	{
		ShadowComponent.UseMultiMesh = false;
		Image image = Image.CreateEmpty(2, 2, useMipmaps: false, Image.Format.Rgba8);
		image.Fill(Colors.White);
		Sprite2D sprite2D = new Sprite2D
		{
			Texture = ImageTexture.CreateFromImage(image),
			Position = new Vector2(7f, 19f)
		};
		TowerDefenseCharacterTransformPoint towerDefenseCharacterTransformPoint = new TowerDefenseCharacterTransformPoint();
		ShadowCellMoveCharacterStub owner = new ShadowCellMoveCharacterStub
		{
			shadowSprite = sprite2D,
			transformPoint = towerDefenseCharacterTransformPoint,
			Position = new Vector2(100f, 100f)
		};
		owner.AddChild(sprite2D, forceReadableName: false, InternalMode.Disabled);
		owner.AddChild(towerDefenseCharacterTransformPoint, forceReadableName: false, InternalMode.Disabled);
		AddChild(owner, forceReadableName: false, InternalMode.Disabled);
		owner.SetPhysicsProcess(enable: false);
		ComponentManager manager = new ComponentManager();
		ShadowComponentDefinition definition = new ShadowComponentDefinition
		{
			ComponentTypeId = "ShadowComponent",
			DefinitionId = "test.component.shadow.cell_move",
			InstanceId = "test.shadow.cell_move",
			WireIndex = 0,
			preferMultiMesh = false
		};
		ShadowComponent runtime = new ShadowComponent();
		runtime.Bind(manager, owner, definition);
		owner.shadowComponent = runtime;
		runtime.Activate();
		Vector2 initialOffset = runtime.saveShadowPosition - owner.GetLogicalGlobalPosition();
		Vector2 targetPosition = new Vector2(220f, 100f);
		Vector2 secondTarget = new Vector2(220f, 240f);
		Vector2 finalTarget = new Vector2(80f, 240f);
		TowerDefenseCellInstance.CreateCharacterCellMoveTween(owner, targetPosition);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		TowerDefenseCellInstance.CreateCharacterCellMoveTween(owner, secondTarget);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		TowerDefenseCellInstance.CreateCharacterCellMoveTween(owner, finalTarget);
		for (int i = 0; i < 40; i++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
		Check(owner.GetLogicalGlobalPosition().IsEqualApprox(finalTarget), "Rapid cell movement must finish at the latest requested cell.");
		Check((runtime.saveShadowPosition - owner.GetLogicalGlobalPosition()).IsEqualApprox(initialOffset), "Rapid cell movement must preserve the shadow's initial offset without cumulative drift.");
		runtime.Release();
		owner.Free();
	}

	private void RunTransformPointScaleNotification()
	{
		ShadowComponent.UseMultiMesh = false;
		Image image = Image.CreateEmpty(2, 2, useMipmaps: false, Image.Format.Rgba8);
		image.Fill(Colors.White);
		Sprite2D sprite2D = new Sprite2D
		{
			Texture = ImageTexture.CreateFromImage(image),
			Scale = new Vector2(1.2f, 0.8f),
			Visible = true
		};
		TowerDefenseCharacterTransformPoint towerDefenseCharacterTransformPoint = new TowerDefenseCharacterTransformPoint();
		AddChild(sprite2D, forceReadableName: false, InternalMode.Disabled);
		AddChild(towerDefenseCharacterTransformPoint, forceReadableName: false, InternalMode.Disabled);
		TowerDefenseCharacter towerDefenseCharacter = new TowerDefenseCharacter
		{
			shadowSprite = sprite2D,
			transformPoint = towerDefenseCharacterTransformPoint
		};
		ComponentManager manager = new ComponentManager();
		ShadowComponentDefinition definition = new ShadowComponentDefinition
		{
			ComponentTypeId = "ShadowComponent",
			DefinitionId = "test.component.shadow.scale",
			InstanceId = "test.shadow.scale",
			WireIndex = 0,
			preferMultiMesh = false
		};
		ShadowComponent shadowComponent = new ShadowComponent();
		shadowComponent.Bind(manager, towerDefenseCharacter, definition);
		shadowComponent.Activate();
		shadowComponent.PhysicsProcess(0.0, Engine.GetPhysicsFrames());
		Vector2 scale = sprite2D.Scale;
		ulong scaleRevision = towerDefenseCharacterTransformPoint.ScaleRevision;
		towerDefenseCharacterTransformPoint.Scale = new Vector2(0.5f, 0.75f);
		Check(towerDefenseCharacterTransformPoint.ScaleRevision != scaleRevision && towerDefenseCharacterTransformPoint.CachedScale.IsEqualApprox(towerDefenseCharacterTransformPoint.Scale), "Direct or Mod TransformPoint scaling must update the managed scale revision immediately.");
		shadowComponent.PhysicsProcess(0.0, Engine.GetPhysicsFrames() + 1);
		Vector2 other = scale * towerDefenseCharacterTransformPoint.Scale;
		Check(sprite2D.Scale.IsEqualApprox(other), "Shadow scaling must follow a notification-backed TransformPoint scale change.");
		shadowComponent.Release();
		sprite2D.Free();
		towerDefenseCharacterTransformPoint.Free();
		towerDefenseCharacter.Free();
	}

	private void RunFirstSubmissionHandoff()
	{
		ShadowComponent.UseMultiMesh = true;
		TowerDefenseShadowMultiMeshRenderer.Enabled = true;
		Image image = Image.CreateEmpty(2, 2, useMipmaps: false, Image.Format.Rgba8);
		image.Fill(Colors.White);
		Sprite2D sprite2D = new Sprite2D
		{
			Texture = ImageTexture.CreateFromImage(image),
			VisibilityLayer = 5u,
			Visible = true
		};
		Marker2D marker2D = new Marker2D();
		AddChild(sprite2D, forceReadableName: false, InternalMode.Disabled);
		AddChild(marker2D, forceReadableName: false, InternalMode.Disabled);
		TowerDefenseCharacter towerDefenseCharacter = new TowerDefenseCharacter
		{
			shadowSprite = sprite2D,
			transformPoint = marker2D
		};
		ComponentManager manager = new ComponentManager();
		ShadowComponentDefinition definition = new ShadowComponentDefinition
		{
			ComponentTypeId = "ShadowComponent",
			DefinitionId = "test.component.shadow",
			InstanceId = "test.shadow",
			WireIndex = 0,
			preferMultiMesh = true
		};
		ShadowComponent shadowComponent = new ShadowComponent();
		shadowComponent.Bind(manager, towerDefenseCharacter, definition);
		Check(sprite2D.VisibilityLayer == 5, "Binding must keep the native shadow visible before the first renderer submission.");
		shadowComponent.Activate();
		Check(sprite2D.VisibilityLayer == 5, "Activation must not hide the native shadow before physics dispatch.");
		Check(shadowComponent.CanDispatchPhysicsWorkForOwnerState(ownerInsideComponentBattlefield: false), "A screen-visible shadow must publish before its owner enters the component battlefield bounds.");
		TowerDefenseShadowMultiMeshRenderer.Enabled = false;
		shadowComponent.PhysicsProcess(0.0, Engine.GetPhysicsFrames());
		Check(sprite2D.VisibilityLayer == 5, "A rejected MultiMesh submission must preserve the native shadow fallback.");
		TowerDefenseShadowMultiMeshRenderer.Enabled = true;
		shadowComponent.PhysicsProcess(0.0, Engine.GetPhysicsFrames());
		Check(sprite2D.VisibilityLayer == 0, "The native shadow may be hidden after the renderer accepts its submission.");
		shadowComponent.Detach(ComponentDetachReason.TemporaryTreeExit);
		Check(sprite2D.VisibilityLayer == 5, "Temporary detachment must restore the original shadow visibility layer.");
		shadowComponent.Release();
		towerDefenseCharacter.Free();
	}

	private async Task RunRegisteredDataFastPathHeightScale()
	{
		ShadowComponent.UseMultiMesh = true;
		TowerDefenseShadowMultiMeshRenderer.Enabled = true;
		Image image = Image.CreateEmpty(2, 2, useMipmaps: false, Image.Format.Rgba8);
		image.Fill(Colors.White);
		TowerDefenseCharacterShadowSprite shadowNode = new TowerDefenseCharacterShadowSprite
		{
			Name = "ShadowSprite",
			Texture = ImageTexture.CreateFromImage(image),
			Scale = Vector2.One,
			VisibilityLayer = 5u
		};
		TowerDefenseCharacterTransformPoint transformPoint = new TowerDefenseCharacterTransformPoint
		{
			Position = new Vector2(0f, 12f),
			Scale = Vector2.One
		};
		ShadowCellMoveCharacterStub owner = new ShadowCellMoveCharacterStub
		{
			Position = new Vector2(40f, 100f),
			transformPoint = transformPoint,
			isGround = false
		};
		owner.AddChild(shadowNode, forceReadableName: false, InternalMode.Disabled);
		owner.AddChild(transformPoint, forceReadableName: false, InternalMode.Disabled);
		owner.shadowSprite = TowerDefenseShadowVisual.Capture(owner, shadowNode, detachCompatibleNode: false);
		AddChild(owner, forceReadableName: false, InternalMode.Disabled);
		owner.SetPhysicsProcess(enable: false);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		ComponentManager manager = new ComponentManager();
		ShadowComponentDefinition definition = new ShadowComponentDefinition
		{
			ComponentTypeId = "ShadowComponent",
			DefinitionId = "test.component.shadow.data_fast_path",
			InstanceId = "test.shadow.data_fast_path",
			WireIndex = 0,
			preferMultiMesh = true,
			followHeight = true,
			heightScaleFactor = 100f,
			minimumHeightScale = 0.25f
		};
		ShadowComponent runtime = new ShadowComponent();
		runtime.Bind(manager, owner, definition);
		owner.shadowComponent = runtime;
		runtime.Activate();
		ulong frame = Engine.GetPhysicsFrames();
		runtime.PhysicsProcess(0.0, frame);
		Check(shadowNode.VisibilityLayer == 0, "The registered data fixture must complete its initial MultiMesh handoff.");
		owner.groundHeight = 20.0;
		owner.z = 50.0;
		transformPoint.Scale = new Vector2(2f, 0.5f);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		bool condition = runtime.TrySubmitRegisteredDataFastPath(frame + 1, Transform2D.Identity);
		Check(condition, "Special height and TransformPoint scale changes must remain on the registered data fast path.");
		Check(runtime.ComputedShadowScale.IsEqualApprox(new Vector2(1f, 0.25f)), $"The data fast path must combine height scaling with the live TransformPoint scale (actual={runtime.ComputedShadowScale}).");
		Check(Mathf.IsEqualApprox(runtime.ComputedShadowGlobalY, 102f), $"The data fast path must combine follow-height and scaled ground height without a TransformPoint global getter (actual={runtime.ComputedShadowGlobalY}).");
		Check(shadowNode.Scale.IsEqualApprox(Vector2.One), "The registered data fast path must not write the hidden native shadow transform.");
		runtime.Release();
		owner.Free();
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[ShadowComponentRuntimeTest] " + message);
		}
	}
}
