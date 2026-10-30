using System;
using Godot;

public partial class PortalProjectileTransportPorTallnutStub : TowerDefensePlantPorTallnut
{
	private const string PortalSpriteScenePath = "res://Asset/Anime/Character/Plant/Star/PorTallnut/PorTallnut.tscn";

	public override void _Ready()
	{
		base.ProcessMode = ProcessModeEnum.Disabled;
		instance = new TowerDefenseCharacterInstance
		{
			canBeCollection = false,
			hypnoses = false
		};
		open = true;
		AddToGroup("PorTallnut");
		base.sprite = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Star/PorTallnut/PorTallnut.tscn", null, ResourceLoader.CacheMode.Ignore)?.Instantiate<AdobeAnimateSprite>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(base.sprite))
		{
			throw new InvalidOperationException("Could not instantiate res://Asset/Anime/Character/Plant/Star/PorTallnut/PorTallnut.tscn.");
		}
		AddChild(base.sprite, forceReadableName: false, InternalMode.Disabled);
		OpenIdleEntered();
	}
}
