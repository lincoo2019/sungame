using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class PogoJacksonBattlefieldVisibilityRuntimeTest : Node
{
	private const string PacketName = "ZombiePogoJackson";

	private const string PacketPath = "res://Asset/Anime/Character/Zombie/Chapter5/PogoJackson/Packet/ZombiePogoJackson.tres";

	private const string ScenePath = "res://Asset/Anime/Character/Zombie/Chapter5/PogoJackson/Scene/TowerDefenseZombiePogoJackson.tscn";

	private static readonly Vector2I SpawnLineGrid = new Vector2I(-1, 3);

	private const float BattlefieldX = 720f;

	private const int MaximumJumpArcPhysicsFrames = 120;

	private int _checks;

	private int _failures;

	private Resource _previousPacket;

	private Resource _previousCharacter;

	private bool _packetWasMissing;

	private bool _characterWasMissing;

	private readonly List<Resource> _registeredResources = new List<Resource>();

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		PogoJacksonBattlefieldVisibilityControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		TowerDefensePacketConfig packet = null;
		TowerDefenseZombiePogoJackson pogoJackson = null;
		Image jumpGroundImage = null;
		Image jumpApexImage = null;
		try
		{
			_ = 7;
			try
			{
				Check(GodotObject.IsInstanceValid(manager) && GodotObject.IsInstanceValid(ResourceManager.Instance), "TowerDefenseManager and ResourceManager autoloads must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					throw new InvalidOperationException("Required battle autoloads are unavailable.");
				}
				RegisterRealFixtures();
				control = new PogoJacksonBattlefieldVisibilityControlStub
				{
					Name = "PogoJacksonBattlefieldVisibilityControl",
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
				manager.gridBeginPos = new Vector2(200f, 100f);
				manager.gridSize = new Vector2(80f, 76f);
				manager.gridNum = new Vector2I(9, 5);
				mapControl = new TowerDefenseMapControl
				{
					Name = "MapControl"
				};
				mapFeature = CreateMapFeature(mapControl, manager);
				mapFeature.control = control;
				control.featureDictionary[new StringName("Map")] = mapFeature;
				packet = TowerDefenseManager.GetPacketConfig("ZombiePogoJackson");
				Check(GodotObject.IsInstanceValid(packet) && packet.characterConfig?.name == "ZombiePogoJackson", "The production Pogo Jackson packet must load through ResourceManager.");
				if (!GodotObject.IsInstanceValid(packet))
				{
					throw new InvalidOperationException("Pogo Jackson packet registration failed.");
				}
				pogoJackson = packet.Spawn(SpawnLineGrid.Y) as TowerDefenseZombiePogoJackson;
				Check(GodotObject.IsInstanceValid(pogoJackson) && pogoJackson.GetParent() == control.characterNode, "The production wave spawn path must add Pogo Jackson to the battle character mount.");
				if (!GodotObject.IsInstanceValid(pogoJackson))
				{
					throw new InvalidOperationException("Pogo Jackson battle spawn failed.");
				}
				pogoJackson.invisible = false;
				if (GodotObject.IsInstanceValid(pogoJackson.sprite))
				{
					pogoJackson.sprite.pause = true;
				}
				await WaitFrames(8);
				Rect2 viewportRect = GetViewport().GetVisibleRect();
				Check(pogoJackson.GlobalPosition.X > viewportRect.End.X && pogoJackson.gridPos.Y == SpawnLineGrid.Y, $"The fixture must begin at the production off-lawn wave position; position={pogoJackson.GlobalPosition}, viewport={viewportRect}.");
				PogoJacksonBattlefieldVisibilityRuntimeTest pogoJacksonBattlefieldVisibilityRuntimeTest = this;
				AdobeAnimateSprite sprite = pogoJackson.sprite;
				pogoJacksonBattlefieldVisibilityRuntimeTest.Check(sprite != null && !sprite.forceLocalRender && pogoJackson.z > pogoJackson.groundHeight, $"Continuous Pogo jumping must stay on the dynamic-Z GPU Crowd path; forceLocal={pogoJackson.sprite?.forceLocalRender}, z={pogoJackson.z}, ground={pogoJackson.groundHeight}.");
				Check(Math.Abs(pogoJackson.gravity - 490.0) <= 0.001 && Math.Abs(pogoJackson.gravityScale - 1.5) <= 0.001, $"Pogo Jackson authored gravity changed; gravity={pogoJackson.gravity}, scale={pogoJackson.gravityScale}.");
				pogoJackson.GlobalPosition = new Vector2(720f, pogoJackson.GlobalPosition.Y);
				pogoJackson.gridPos = new Vector2I(7, SpawnLineGrid.Y);
				await WaitFrames(12);
				Check(viewportRect.HasPoint(pogoJackson.GlobalPosition) && pogoJackson.IsVisibleInTree() && (pogoJackson.sprite?.Visible ?? false) && !pogoJackson.sprite.invisible && !pogoJackson.invisible, $"Pogo Jackson must be logically visible after reaching the lawn; position={pogoJackson.GlobalPosition}, visible={pogoJackson.IsVisibleInTree()}, spriteVisible={pogoJackson.sprite?.Visible}, invisible={pogoJackson.invisible}.");
				int publishedInstances = CountPublishedInstances(GetTree().Root);
				Check(publishedInstances > 0, $"The battlefield render mount must publish Pogo Jackson instances; count={publishedInstances}.");
				await WaitForGroundLaunch(pogoJackson);
				float groundShadowY = pogoJackson.shadowSprite.GlobalPosition.Y;
				float groundSpriteGroupY = pogoJackson.spriteGroup.GlobalPosition.Y;
				jumpGroundImage = await CaptureFrame();
				float maximumHeight = (float)(pogoJackson.z - pogoJackson.groundHeight);
				float maximumVisualLift = groundSpriteGroupY - pogoJackson.spriteGroup.GlobalPosition.Y;
				bool shadowStayedGrounded = true;
				bool completedJumpArc = false;
				double previousYSpeed = pogoJackson.ySpeed;
				int minimumCrowdRoots = 2147483647;
				int maximumFallbackRoots = 0;
				for (int frame = 0; frame < 120; frame++)
				{
					await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
					float height = (float)(pogoJackson.z - pogoJackson.groundHeight);
					if (height > maximumHeight)
					{
						maximumHeight = height;
						jumpApexImage?.Dispose();
						jumpApexImage = await CaptureFrame();
					}
					maximumVisualLift = Math.Max(maximumVisualLift, groundSpriteGroupY - pogoJackson.spriteGroup.GlobalPosition.Y);
					shadowStayedGrounded &= Math.Abs(pogoJackson.shadowSprite.GlobalPosition.Y - groundShadowY) <= 0.05f;
					AdobeAnimateCrowdAggregateStats aggregateRenderStats = AdobeAnimateRenderManager.GetAggregateRenderStats();
					minimumCrowdRoots = Math.Min(minimumCrowdRoots, aggregateRenderStats.CrowdRoots);
					maximumFallbackRoots = Math.Max(maximumFallbackRoots, aggregateRenderStats.FallbackRoots);
					if (previousYSpeed > 0.0 && pogoJackson.ySpeed < 0.0 && height <= 1f)
					{
						completedJumpArc = true;
						break;
					}
					previousYSpeed = pogoJackson.ySpeed;
				}
				Check(completedJumpArc, $"Pogo Jackson must complete and relaunch one production physics arc; z={pogoJackson.z}, ground={pogoJackson.groundHeight}, speed={pogoJackson.ySpeed}.");
				Check(maximumHeight >= 50f && maximumHeight <= 70f && maximumVisualLift >= 50f, $"Pogo Jackson jump height left the authored physical range; height={maximumHeight:F3}, visualLift={maximumVisualLift:F3}.");
				Check(shadowStayedGrounded, $"Pogo Jackson root shadow must remain on the ground; expected={groundShadowY:F3}, actual={pogoJackson.shadowSprite.GlobalPosition.Y:F3}.");
				int jumpGroundPixels = CountForegroundPixels(jumpGroundImage, jumpGroundImage.GetPixel(0, 0));
				int jumpApexPixels = (GodotObject.IsInstanceValid(jumpApexImage) ? CountForegroundPixels(jumpApexImage, jumpApexImage.GetPixel(0, 0)) : 0);
				int jumpChangedPixels = (GodotObject.IsInstanceValid(jumpApexImage) ? CountChangedPixels(jumpGroundImage, jumpApexImage) : 0);
				Check(jumpGroundPixels >= 256 && jumpApexPixels >= 256 && jumpChangedPixels >= 256, $"Pogo Jackson jump must remain visibly animated at ground and apex; ground={jumpGroundPixels}, apex={jumpApexPixels}, changed={jumpChangedPixels}.");
				Check(minimumCrowdRoots >= 1 && maximumFallbackRoots == 0 && !pogoJackson.sprite.forceLocalRender, $"Pogo Jackson jump left Vulkan GPU Crowd; crowdMin={minimumCrowdRoots}, fallbackMax={maximumFallbackRoots}, forceLocal={pogoJackson.sprite.forceLocalRender}.");
				using Image visibleImage = await CaptureFrame();
				int visibleForeground = CountForegroundPixels(visibleImage, visibleImage.GetPixel(0, 0));
				pogoJackson.Visible = false;
				await WaitFrames(3);
				using Image hidden = GetViewport().GetTexture().GetImage();
				int num = CountChangedPixels(visibleImage, hidden);
				Check(visibleForeground >= 256, $"Pogo Jackson must draw visible body pixels while standing on the lawn; foreground={visibleForeground}.");
				Check(num >= 256, $"Hiding the battlefield Pogo Jackson must remove its rendered body; changed={num}.");
				GD.Print($"POGO_JACKSON_BATTLEFIELD_VISIBILITY_METRIC foreground={visibleForeground} changed={num} instances={publishedInstances} jumpHeight={maximumHeight:F3} visualLift={maximumVisualLift:F3} jumpGround={jumpGroundPixels} jumpApex={jumpApexPixels} jumpChanged={jumpChangedPixels} crowdMin={minimumCrowdRoots} fallbackMax={maximumFallbackRoots} forceLocal={pogoJackson.sprite?.forceLocalRender} position={pogoJackson.GlobalPosition} renderer={RenderingServer.GetCurrentRenderingMethod()}");
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[PogoJacksonBattlefieldVisibilityRuntimeTest] Unexpected exception: {value}");
			}
		}
		finally
		{
			jumpGroundImage?.Dispose();
			jumpApexImage?.Dispose();
			if (GodotObject.IsInstanceValid(pogoJackson))
			{
				pogoJackson.QueueFree();
			}
			await WaitFrames(5);
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
			}
			mapFeature?.Destroy();
			if (GodotObject.IsInstanceValid(mapControl))
			{
				mapControl.Free();
			}
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			RestoreRealFixtures();
			await WaitFrames(6);
			if (GodotObject.IsInstanceValid(ObjectManager.Instance))
			{
				ObjectManager.Instance.Clear();
			}
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			AdobeAnimateDefinitionCache.Clear();
			TowerDefenseGroundItemBase.ClearStaticBattleReferences();
			packet?.Dispose();
			foreach (Resource registeredResource in _registeredResources)
			{
				registeredResource?.Dispose();
			}
			_registeredResources.Clear();
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
		bool flag = _failures == 0 && _checks == 15;
		GD.Print($"POGO_JACKSON_BATTLEFIELD_VISIBILITY_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task WaitForGroundLaunch(TowerDefenseZombiePogoJackson pogoJackson)
	{
		for (int frame = 0; frame < 120; frame++)
		{
			if (pogoJackson.z - pogoJackson.groundHeight <= 1.0 && pogoJackson.ySpeed < 0.0)
			{
				return;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
		throw new InvalidOperationException("Pogo Jackson did not reach a production ground-launch state.");
	}

	private async Task<Image> CaptureFrame()
	{
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		return GetViewport().GetTexture().GetImage();
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, TowerDefenseManager manager)
	{
		TowerDefenseMapConfig towerDefenseMapConfig = new TowerDefenseMapConfig
		{
			gridNum = manager.gridNum,
			gridBeginPos = manager.gridBeginPos,
			gridSize = manager.gridSize,
			plantOffset = 50.0,
			edge = new Vector4(200f, 0f, 1100f, 600f)
		};
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = (mapControl.mapFeature = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = towerDefenseMapConfig,
			mapConfig = towerDefenseMapConfig
		});
		towerDefenseBattleFeatureMap.plantGrid.Resize(manager.gridNum.X + 1);
		for (int i = 0; i <= manager.gridNum.X; i++)
		{
			Godot.Collections.Array array = new Godot.Collections.Array();
			array.Resize(manager.gridNum.Y + 1);
			for (int j = 1; j <= manager.gridNum.Y; j++)
			{
				TowerDefenseCellInstance towerDefenseCellInstance = new TowerDefenseCellInstance
				{
					gridPos = new Vector2I(i, j)
				};
				towerDefenseCellInstance.Init(new TowerDefenseCellConfig());
				array[j] = towerDefenseCellInstance;
			}
			towerDefenseBattleFeatureMap.plantGrid[i] = array;
		}
		towerDefenseBattleFeatureMap.iceCapList.Resize(manager.gridNum.Y + 1);
		towerDefenseBattleFeatureMap.lineUse.Resize(manager.gridNum.Y + 1);
		for (int k = 1; k <= manager.gridNum.Y; k++)
		{
			towerDefenseBattleFeatureMap.lineUse[k] = true;
		}
		return towerDefenseBattleFeatureMap;
	}

	private void RegisterRealFixtures()
	{
		ResourceManager instance = ResourceManager.Instance;
		if (instance.TOWERDEFENSE_PACKETS.TryGetValue("ZombiePogoJackson", out var value))
		{
			_previousPacket = value;
		}
		else
		{
			_packetWasMissing = true;
		}
		if (instance.TOWERDEFENSE_CHARCATERS.TryGetValue("ZombiePogoJackson", out var value2))
		{
			_previousCharacter = value2;
		}
		else
		{
			_characterWasMissing = true;
		}
		TowerDefensePacketConfig towerDefensePacketConfig = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Zombie/Chapter5/PogoJackson/Packet/ZombiePogoJackson.tres", null, ResourceLoader.CacheMode.Ignore);
		PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter5/PogoJackson/Scene/TowerDefenseZombiePogoJackson.tscn", null, ResourceLoader.CacheMode.Ignore);
		_registeredResources.Add(towerDefensePacketConfig);
		_registeredResources.Add(packedScene);
		instance.TOWERDEFENSE_PACKETS["ZombiePogoJackson"] = towerDefensePacketConfig;
		instance.TOWERDEFENSE_CHARCATERS["ZombiePogoJackson"] = packedScene;
	}

	private void RestoreRealFixtures()
	{
		ResourceManager instance = ResourceManager.Instance;
		if (GodotObject.IsInstanceValid(instance))
		{
			if (_packetWasMissing)
			{
				instance.TOWERDEFENSE_PACKETS.Remove("ZombiePogoJackson");
			}
			else
			{
				instance.TOWERDEFENSE_PACKETS["ZombiePogoJackson"] = _previousPacket;
			}
			if (_characterWasMissing)
			{
				instance.TOWERDEFENSE_CHARCATERS.Remove("ZombiePogoJackson");
			}
			else
			{
				instance.TOWERDEFENSE_CHARCATERS["ZombiePogoJackson"] = _previousCharacter;
			}
		}
	}

	private static int CountPublishedInstances(Node node)
	{
		int num = ((node is MultiMeshInstance2D multiMeshInstance2D && GodotObject.IsInstanceValid(multiMeshInstance2D.Multimesh)) ? Math.Max(0, multiMeshInstance2D.Multimesh.VisibleInstanceCount) : 0);
		foreach (Node child in node.GetChildren())
		{
			num += CountPublishedInstances(child);
		}
		return num;
	}

	private static int CountForegroundPixels(Image image, Color background)
	{
		int num = 0;
		for (int i = 0; i < image.GetHeight(); i++)
		{
			for (int j = 0; j < image.GetWidth(); j++)
			{
				Color pixel = image.GetPixel(j, i);
				if (pixel.A > 0.05f && ColorDistance(pixel, background) > 0.05f)
				{
					num++;
				}
			}
		}
		return num;
	}

	private static int CountChangedPixels(Image visible, Image hidden)
	{
		int num = Math.Min(visible.GetWidth(), hidden.GetWidth());
		int num2 = Math.Min(visible.GetHeight(), hidden.GetHeight());
		int num3 = 0;
		for (int i = 0; i < num2; i++)
		{
			for (int j = 0; j < num; j++)
			{
				if (ColorDistance(visible.GetPixel(j, i), hidden.GetPixel(j, i)) > 0.08f)
				{
					num3++;
				}
			}
		}
		return num3;
	}

	private static float ColorDistance(Color left, Color right)
	{
		return Mathf.Max(Mathf.Max(Mathf.Abs(left.R - right.R), Mathf.Abs(left.G - right.G)), Mathf.Max(Mathf.Abs(left.B - right.B), Mathf.Abs(left.A - right.A)));
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
			GD.PushError("[PogoJacksonBattlefieldVisibilityRuntimeTest] " + message);
		}
	}
}
