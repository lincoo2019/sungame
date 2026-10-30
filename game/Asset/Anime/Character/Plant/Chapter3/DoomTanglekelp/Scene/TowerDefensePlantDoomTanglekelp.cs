using System;
using Godot;

[Tool]
public partial class TowerDefensePlantDoomTanglekelp : TowerDefensePlant
{
	private TanglekelpComponent tanglekelpComponent;

	private ExplodeComponent explodeComponent;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			tanglekelpComponent = componentManager.GetRuntime<TanglekelpComponent>();
			if (tanglekelpComponent != null)
			{
				tanglekelpComponent.OnDrag += Drag;
			}
			explodeComponent = componentManager.GetRuntime<ExplodeComponent>();
			if (base.currentCustom.Contains("Custom0"))
			{
				tanglekelpComponent.grabFliterOpen = new string[2] { "skin4", "skin5" };
				tanglekelpComponent.grabFliterClose = new string[2] { "Layer 29", "Layer 32" };
			}
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		TanglekelpComponent obj = tanglekelpComponent;
		if (obj != null && !obj.IsReleased)
		{
			tanglekelpComponent.OnDrag -= Drag;
		}
	}

	public void Drag(TowerDefenseCharacter character, bool success)
	{
		ExplodeComponent obj = explodeComponent;
		if (obj != null && !obj.IsReleased)
		{
			explodeComponent.Explode();
		}
	}

	public override void OnCustomSwitched(string customKey)
	{
		if (customKey == "Custom0")
		{
			tanglekelpComponent.grabFliterOpen = new string[2] { "skin4", "skin5" };
			tanglekelpComponent.grabFliterClose = new string[2] { "Layer 29", "Layer 32" };
		}
		else
		{
			tanglekelpComponent.grabFliterOpen = Array.Empty<string>();
			tanglekelpComponent.grabFliterClose = Array.Empty<string>();
		}
	}
}
