using System;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewPortalProjectileTransportRuntimeTest : Node
{
	private const string ProjectileScenePath = "res://Prefab/TowerDefense/Projectile/TowerDefenseProjectile.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		_ = 1;
		try
		{
			await VerifyPortalTallnutNodeProjectile();
			await VerifyFeaturePortalNodeProjectile();
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[PortalProjectileTransport] Unexpected exception: {value}");
		}
		bool flag = _failures == 0 && _checks == 17;
		GD.Print($"PORTAL_PROJECTILE_TRANSPORT_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task VerifyPortalTallnutNodeProjectile()
	{
		PortalProjectileTransportPorTallnutStub source = new PortalProjectileTransportPorTallnutStub
		{
			Name = "PortalTallnutSource",
			gridPos = new Vector2I(2, 2),
			Position = new Vector2(200f, 152f),
			camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT
		};
		PortalProjectileTransportPorTallnutStub destination = new PortalProjectileTransportPorTallnutStub
		{
			Name = "PortalTallnutDestination",
			gridPos = new Vector2I(6, 4),
			Position = new Vector2(600f, 304f),
			camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT
		};
		AddChild(source, forceReadableName: false, InternalMode.Disabled);
		AddChild(destination, forceReadableName: false, InternalMode.Disabled);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		Check(GetTree().GetNodeCountInGroup("PorTallnut") == 2, "The runtime fixture must expose one real portal-Tallnut pair through the production group lookup.");
		TowerDefenseProjectile towerDefenseProjectile = CreateProjectile("PortalTallnutProjectile", new Vector2(205f, 152f), new Vector2I(2, 2), new Vector2(-300f, 0f), TowerDefenseEnum.CHARACTER_CAMP.PLANT);
		source.OnProjectileIntersect(towerDefenseProjectile);
		Check(towerDefenseProjectile.GlobalPosition.IsEqualApprox(destination.GlobalPosition - new Vector2(11f, 0f)), $"Portal Tall Nut must transport a Node projectile to its partner; got {towerDefenseProjectile.GlobalPosition}.");
		Check(towerDefenseProjectile.gridPos == destination.gridPos, $"Portal Tall Nut must update the Node projectile destination cell; got {towerDefenseProjectile.gridPos}.");
		Vector2 other = (towerDefenseProjectile.GlobalPosition = new Vector2(205f, 152f));
		towerDefenseProjectile.gridPos = source.gridPos;
		towerDefenseProjectile.camp = TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE;
		source.OnProjectileIntersect(towerDefenseProjectile);
		Check(towerDefenseProjectile.GlobalPosition.IsEqualApprox(other) && towerDefenseProjectile.gridPos == source.gridPos, "Portal Tall Nut must reject a projectile from the opposite camp.");
		towerDefenseProjectile.camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT;
		towerDefenseProjectile.velocity = new Vector2(300f, 0f);
		source.OnProjectileIntersect(towerDefenseProjectile);
		Check(towerDefenseProjectile.GlobalPosition.IsEqualApprox(other) && towerDefenseProjectile.gridPos == source.gridPos, "Portal Tall Nut must reject a projectile travelling away from its entrance.");
	}

	private async Task VerifyFeaturePortalNodeProjectile()
	{
		PortalProjectileTransportFeaturePortalStub portal = new PortalProjectileTransportFeaturePortalStub
		{
			Name = "FeaturePortal",
			gridPos1 = new Vector2I(1, 1),
			gridPos2 = new Vector2I(6, 4),
			gridSize = new Vector2(100f, 76f)
		};
		portal.protalNode1 = new Node2D
		{
			Name = "PortalEndpoint1",
			Position = new Vector2(150f, 76f)
		};
		portal.protalNode2 = new Node2D
		{
			Name = "PortalEndpoint2",
			Position = new Vector2(650f, 304f)
		};
		portal.AddChild(portal.protalNode1, forceReadableName: false, InternalMode.Disabled);
		portal.AddChild(portal.protalNode2, forceReadableName: false, InternalMode.Disabled);
		portal.hitBox1 = CreatePortalHitBox("PortalHitBox1", portal.protalNode1);
		portal.hitBox2 = CreatePortalHitBox("PortalHitBox2", portal.protalNode2);
		AddChild(portal, forceReadableName: false, InternalMode.Disabled);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		Check(GodotObject.IsInstanceValid(portal.protalNode1) && GodotObject.IsInstanceValid(portal.protalNode2), "The level-feature portal fixture must expose both real transport endpoints.");
		Vector2 position = new Vector2(155f, 86f);
		Vector2 other = new Vector2(150f, 86f);
		Vector2 vector = new Vector2(650f, 314f);
		TowerDefenseProjectile towerDefenseProjectile = CreateProjectile("FeaturePortalProjectile", position, portal.gridPos1, new Vector2(300f, 0f), TowerDefenseEnum.CHARACTER_CAMP.PLANT);
		portal.TeleportProjectile(towerDefenseProjectile, fromPortal1: true);
		Check(towerDefenseProjectile.GlobalPosition.IsEqualApprox(vector), $"The level-feature portal must transport a Node projectile to endpoint 2; got {towerDefenseProjectile.GlobalPosition}.");
		Check(towerDefenseProjectile.gridPos == portal.gridPos2, $"The level-feature portal must update the destination row; got {towerDefenseProjectile.gridPos}.");
		Check(portal.exclude.Contains(towerDefenseProjectile), "The transported Node projectile must enter the anti-bounce exclusion set.");
		portal.TeleportProjectile(towerDefenseProjectile, fromPortal1: false);
		Check(towerDefenseProjectile.GlobalPosition.IsEqualApprox(vector) && towerDefenseProjectile.gridPos == portal.gridPos2, "The destination endpoint must not immediately bounce the projectile back.");
		Check(portal.exclude.Contains(towerDefenseProjectile), "The anti-bounce guard must remain armed while the projectile still overlaps a portal.");
		for (int i = 0; i < 3; i++)
		{
			portal._PhysicsProcess(0.0);
			portal.TeleportProjectile(towerDefenseProjectile, fromPortal1: false);
		}
		Check(towerDefenseProjectile.GlobalPosition.IsEqualApprox(vector) && towerDefenseProjectile.gridPos == portal.gridPos2 && portal.exclude.Contains(towerDefenseProjectile), "A flat projectile overlapping the exit across physics frames must not bounce or stick in the portal pair.");
		towerDefenseProjectile.GlobalPosition = vector + new Vector2(32f, 0f);
		portal._PhysicsProcess(0.0);
		Check(!portal.exclude.Contains(towerDefenseProjectile), "Leaving both endpoints must clear the anti-bounce guard.");
		towerDefenseProjectile.GlobalPosition = vector;
		towerDefenseProjectile.gridPos = portal.gridPos2;
		portal.TeleportProjectile(towerDefenseProjectile, fromPortal1: false);
		Check(towerDefenseProjectile.GlobalPosition.IsEqualApprox(other) && towerDefenseProjectile.gridPos == portal.gridPos1, $"A later endpoint-2 crossing must transport the projectile back to endpoint 1; position={towerDefenseProjectile.GlobalPosition}, grid={towerDefenseProjectile.gridPos}.");
		Check(portal.exclude.Contains(towerDefenseProjectile), "Reverse transport must arm the same anti-bounce guard.");
	}

	private static AabbArea2D CreatePortalHitBox(string name, Node2D endpoint)
	{
		AabbArea2D aabbArea2D = new AabbArea2D
		{
			Name = name
		};
		aabbArea2D.ShapeResources.Add(new AabbShape2DResource
		{
			Geometry = new RectangleShape2D
			{
				Size = new Vector2(12f, 33f)
			}
		});
		endpoint.AddChild(aabbArea2D, forceReadableName: false, InternalMode.Disabled);
		return aabbArea2D;
	}

	private TowerDefenseProjectile CreateProjectile(string name, Vector2 position, Vector2I gridPos, Vector2 velocity, TowerDefenseEnum.CHARACTER_CAMP camp)
	{
		TowerDefenseProjectile towerDefenseProjectile = ResourceLoader.Load<PackedScene>("res://Prefab/TowerDefense/Projectile/TowerDefenseProjectile.tscn", null, ResourceLoader.CacheMode.Ignore)?.Instantiate<TowerDefenseProjectile>(PackedScene.GenEditState.Disabled);
		Check(GodotObject.IsInstanceValid(towerDefenseProjectile), "The real projectile scene must instantiate for " + name + ".");
		if (!GodotObject.IsInstanceValid(towerDefenseProjectile))
		{
			throw new InvalidOperationException("Could not instantiate res://Prefab/TowerDefense/Projectile/TowerDefenseProjectile.tscn.");
		}
		towerDefenseProjectile.Name = name;
		towerDefenseProjectile.Position = position;
		towerDefenseProjectile.gridPos = gridPos;
		towerDefenseProjectile.velocity = velocity;
		towerDefenseProjectile.camp = camp;
		AddChild(towerDefenseProjectile, forceReadableName: false, InternalMode.Disabled);
		return towerDefenseProjectile;
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[PortalProjectileTransport] " + message);
		}
	}
}
