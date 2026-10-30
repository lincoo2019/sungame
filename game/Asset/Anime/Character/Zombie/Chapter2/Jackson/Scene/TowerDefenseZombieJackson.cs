using System;
using Godot;

[Tool]
[GlobalClass]
public partial class TowerDefenseZombieJackson : TowerDefenseZombie, IJackson, INetworkDancerOwner
{
	public DancingComponent dancingComponent;

	private readonly TowerDefenseCharacter[] _pendingNetworkDancers = new TowerDefenseCharacter[4];

	private string _dancerPacketName = "";

	private bool HasDancingRuntime
	{
		get
		{
			DancingComponent obj = dancingComponent;
			if (obj == null)
			{
				return false;
			}
			return !obj.IsReleased;
		}
	}

	private bool HasDancingComponent
	{
		get
		{
			if (HasDancingRuntime)
			{
				return dancingComponent.Lifecycle == ComponentRuntimeLifecycle.Active;
			}
			return false;
		}
	}

	[Export(PropertyHint.None, "")]
	public string dancerPacketName
	{
		get
		{
			return _dancerPacketName;
		}
		set
		{
			_dancerPacketName = value ?? "";
			if (!string.IsNullOrEmpty(_dancerPacketName) && HasDancingComponent)
			{
				dancingComponent.dancerPacketName = _dancerPacketName;
			}
		}
	}

	public void RemoveDancer(TowerDefenseCharacter dancer)
	{
		if (HasDancingRuntime)
		{
			dancingComponent.RemoveDancer(dancer);
		}
		for (int i = 0; i < 4; i++)
		{
			if (IsSameDancer(_pendingNetworkDancers[i], dancer))
			{
				_pendingNetworkDancers[i] = null;
			}
		}
	}

	private static bool IsSameDancer(TowerDefenseCharacter candidate, TowerDefenseCharacter dancer)
	{
		if (candidate != dancer)
		{
			if (GodotObject.IsInstanceValid(candidate) && GodotObject.IsInstanceValid(dancer) && candidate.syncId >= 0)
			{
				return candidate.syncId == dancer.syncId;
			}
			return false;
		}
		return true;
	}

	public void SetNetworkDancer(int slot, TowerDefenseCharacter dancer)
	{
		if (slot >= 0 && slot < 4)
		{
			if (!HasDancingRuntime)
			{
				_pendingNetworkDancers[slot] = dancer;
				return;
			}
			dancingComponent.SetNetworkDancer(slot, dancer);
			_pendingNetworkDancers[slot] = null;
		}
	}

	private void ApplyPendingNetworkDancers()
	{
		if (!HasDancingRuntime)
		{
			return;
		}
		for (int i = 0; i < 4; i++)
		{
			TowerDefenseCharacter dancer = _pendingNetworkDancers[i];
			if (GodotObject.IsInstanceValid(dancer))
			{
				SetNetworkDancer(i, dancer);
			}
			else
			{
				_pendingNetworkDancers[i] = null;
			}
		}
	}

	private void ReleaseDancerOwnership()
	{
		if (HasDancingRuntime)
		{
			for (int i = 0; i < 4; i++)
			{
				dancingComponent.SetNetworkDancer(i, null);
			}
		}
		Array.Fill(_pendingNetworkDancers, null);
	}

	public override void _ExitTree()
	{
		if (IsQueuedForDeletion())
		{
			ReleaseDancerOwnership();
		}
		base._ExitTree();
	}

	public override void _Ready()
	{
		base._Ready();
		if (Engine.IsEditorHint())
		{
			return;
		}
		dancingComponent = componentManager.GetRuntime<DancingComponent>();
		if (HasDancingComponent)
		{
			if (!string.IsNullOrEmpty(dancerPacketName))
			{
				dancingComponent.dancerPacketName = dancerPacketName;
			}
			else
			{
				foreach (TowerDefenseArmorInstance armor in instance.armorList)
				{
					if (armor.slotConfig.armorName == "BlackHelmet" || armor.slotConfig.armorName == "SpecialHelmet")
					{
						dancingComponent.dancerPacketName = "ZombieDancerCone";
						break;
					}
				}
			}
		}
		ApplyPendingNetworkDancers();
	}

	public override void WalkEntered()
	{
		base.WalkEntered();
		if (HasDancingComponent)
		{
			dancingComponent.OnWalkEntered();
		}
	}

	public override void WalkProcessing(double delta)
	{
		if (HasDancingComponent)
		{
			groundMoveComponent.SetAlive(dancingComponent.CanWalk());
		}
		base.WalkProcessing(delta);
	}

	public override void AttackProcessing(double delta)
	{
		base.AttackProcessing(delta);
		if (HasDancingComponent)
		{
			dancingComponent.OnAttackProcessing(delta);
		}
	}

	public override void Walk()
	{
		if (die)
		{
			SendStateEvent("ToDie");
		}
		else if (!HasDancingComponent || !dancingComponent.OnWalk())
		{
			SendStateEvent("ToWalk");
		}
	}

	public override void DieProcessing(double delta)
	{
		base.DieProcessing(delta);
		if (HasDancingComponent)
		{
			dancingComponent.OnDieProcessing();
		}
	}

	public override void Hypnoses(double time = -1.0, bool canFliter = true, TowerDefenseCharacterBuffHypnoses hypnosesConfig = null)
	{
		base.Hypnoses(time, canFliter, hypnosesConfig);
		ReleaseDancerOwnership();
	}
}
