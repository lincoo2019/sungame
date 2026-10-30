using System;
using Godot;

public sealed class SlotComponent : CharacterComponentRuntime
{
	private sealed class OccupantBinding
	{
		public TowerDefenseCharacter Character;

		public bool ShadowWasVisible;

		public bool FollowHeightWasEnabled;

		public double GroundHeightBeforeBinding;

		public bool ConfigurationApplied;

		public bool AppliedHeightFollow;

		public bool AppliedHideShadow;
	}

	public Marker2D posMark;

	public bool heightFollow;

	public bool hideShadow = true;

	public int occupantRefreshInterval = 3;

	public bool restoreGroundHeightOnRelease = true;

	public TowerDefenseCharacter parent;

	public TowerDefenseCellInstance cell;

	public TowerDefenseCharacter slotCharacter;

	public TowerDefenseCharacter surroundCharacter;

	public bool slotCharacterShadow;

	public bool surroundCharacterShadow;

	private NodePath _posMarkPath = new NodePath();

	private OccupantBinding _slotBinding;

	private OccupantBinding _surroundBinding;

	private Vector2I _cellGridPosition;

	private int _refreshOffset;

	private bool _forceRefresh = true;

	private bool _configured;

	private SlotComponentDefinition Definition => base.ComponentDefinition as SlotComponentDefinition;

	internal override bool WantsPhysicsProcess => true;

	protected override void OnBound()
	{
		parent = base.Owner;
		ConfigureOnce();
		ResolveMarker();
		if (GodotObject.IsInstanceValid(parent))
		{
			int num = Math.Max(1, occupantRefreshInterval);
			_refreshOffset = (int)(Math.Abs((long)parent.randFreshIndex) % num);
			RefreshCell();
			RefreshOccupants();
		}
	}

	protected override void OnActivated()
	{
		_forceRefresh = true;
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		ReleaseAllOccupants();
		cell = null;
		posMark = null;
		parent = null;
	}

	protected override void OnReleased()
	{
		ReleaseAllOccupants();
		cell = null;
		posMark = null;
		parent = null;
	}

	protected override void OnAliveChanged(bool alive)
	{
		if (!alive)
		{
			ReleaseAllOccupants();
		}
		else
		{
			_forceRefresh = true;
		}
	}

	internal override void PhysicsProcess(double delta, ulong physicsFrame)
	{
		ProcessBindings(physicsFrame);
	}

	private void ConfigureOnce()
	{
		if (!_configured)
		{
			SlotComponentDefinition definition = Definition;
			_posMarkPath = definition?.posMarkPath ?? new NodePath();
			heightFollow = definition?.heightFollow ?? false;
			hideShadow = definition?.hideShadow ?? true;
			occupantRefreshInterval = Math.Clamp(definition?.occupantRefreshInterval ?? 3, 1, 30);
			restoreGroundHeightOnRelease = definition?.restoreGroundHeightOnRelease ?? true;
			_configured = true;
		}
	}

	private void ResolveMarker()
	{
		posMark = ((GodotObject.IsInstanceValid(parent) && !_posMarkPath.IsEmpty) ? parent.GetNodeOrNull<Marker2D>(_posMarkPath) : null);
	}

	private void ProcessBindings(ulong physicsFrame)
	{
		if (!base.Alive || base.Lifecycle != ComponentRuntimeLifecycle.Active || !GodotObject.IsInstanceValid(parent) || parent.die || !GodotObject.IsInstanceValid(posMark) || !GodotObject.IsInstanceValid(parent.transformPoint))
		{
			ReleaseAllOccupants();
			return;
		}
		if (!GodotObject.IsInstanceValid(cell) || _cellGridPosition != parent.gridPos)
		{
			ReleaseAllOccupants();
			RefreshCell();
			_forceRefresh = true;
		}
		if (GodotObject.IsInstanceValid(cell))
		{
			if (_forceRefresh || ShouldRefreshOccupants(physicsFrame))
			{
				RefreshOccupants();
			}
			ApplyBinding(ref _slotBinding, isSlot: true);
			ApplyBinding(ref _surroundBinding, isSlot: false);
		}
	}

	private bool ShouldRefreshOccupants(ulong physicsFrame)
	{
		int num = Math.Max(1, occupantRefreshInterval);
		if (num != 1)
		{
			return (ulong)((long)physicsFrame + (long)_refreshOffset) % (ulong)num == 0;
		}
		return true;
	}

	private void RefreshCell()
	{
		cell = ((TowerDefenseManager.GetMapFeature() != null && GodotObject.IsInstanceValid(parent)) ? TowerDefenseManager.GetMapCell(parent.gridPos) : null);
		if (GodotObject.IsInstanceValid(parent))
		{
			_cellGridPosition = parent.gridPos;
		}
	}

	private void RefreshOccupants()
	{
		_forceRefresh = false;
		if (GodotObject.IsInstanceValid(cell) && GodotObject.IsInstanceValid(parent))
		{
			TowerDefenseCharacter towerDefenseCharacter = cell.GetSlot(parent);
			TowerDefenseCharacter towerDefenseCharacter2 = cell.GetSurround();
			if (towerDefenseCharacter == parent)
			{
				towerDefenseCharacter = null;
			}
			if (towerDefenseCharacter2 == parent || towerDefenseCharacter2 == towerDefenseCharacter)
			{
				towerDefenseCharacter2 = null;
			}
			BindOccupant(ref _slotBinding, towerDefenseCharacter, isSlot: true);
			BindOccupant(ref _surroundBinding, towerDefenseCharacter2, isSlot: false);
		}
	}

	private void BindOccupant(ref OccupantBinding binding, TowerDefenseCharacter character, bool isSlot)
	{
		if (binding == null || binding.Character != character || !GodotObject.IsInstanceValid(character))
		{
			ReleaseOccupant(ref binding, isSlot);
			if (GodotObject.IsInstanceValid(character))
			{
				OccupantBinding obj = new OccupantBinding
				{
					Character = character,
					ShadowWasVisible = (GodotObject.IsInstanceValid(character.shadowSprite) && character.shadowSprite.Visible)
				};
				ShadowComponent shadowComponent = character.shadowComponent;
				obj.FollowHeightWasEnabled = shadowComponent != null && !shadowComponent.IsReleased && character.shadowComponent.followHeight;
				obj.GroundHeightBeforeBinding = character.groundHeight;
				binding = obj;
				UpdatePublicBinding(binding, isSlot);
				ApplyBinding(ref binding, isSlot);
			}
		}
	}

	private void ApplyBinding(ref OccupantBinding binding, bool isSlot)
	{
		if (binding == null)
		{
			return;
		}
		TowerDefenseCharacter character = binding.Character;
		if (!GodotObject.IsInstanceValid(character) || character.die)
		{
			ReleaseOccupant(ref binding, isSlot);
			return;
		}
		if (!binding.ConfigurationApplied || binding.AppliedHeightFollow != heightFollow || binding.AppliedHideShadow != hideShadow)
		{
			ShadowComponent shadowComponent = character.shadowComponent;
			if (shadowComponent != null && !shadowComponent.IsReleased)
			{
				character.shadowComponent.followHeight = heightFollow;
				character.shadowComponent.MarkDirty();
				character.shadowComponent.SetShadowVisible(binding.ShadowWasVisible && !hideShadow);
			}
			binding.ConfigurationApplied = true;
			binding.AppliedHeightFollow = heightFollow;
			binding.AppliedHideShadow = hideShadow;
		}
		float y = parent.transformPoint.GlobalScale.Y;
		if (Mathf.Abs(y) <= 0.001f)
		{
			return;
		}
		double num = (parent.GetLogicalGlobalPosition().Y - parent.GetLogicalGlobalPosition(posMark).Y) / y;
		if (!Mathf.IsEqualApprox((float)character.groundHeight, (float)num))
		{
			character.groundHeight = num;
			ShadowComponent shadowComponent2 = character.shadowComponent;
			if (shadowComponent2 != null && !shadowComponent2.IsReleased)
			{
				character.shadowComponent.MarkDirty();
			}
		}
	}

	private void ReleaseAllOccupants()
	{
		ReleaseOccupant(ref _slotBinding, isSlot: true);
		ReleaseOccupant(ref _surroundBinding, isSlot: false);
	}

	private void ReleaseOccupant(ref OccupantBinding binding, bool isSlot)
	{
		if (binding != null && GodotObject.IsInstanceValid(binding.Character))
		{
			TowerDefenseCharacter character = binding.Character;
			if (restoreGroundHeightOnRelease)
			{
				character.groundHeight = binding.GroundHeightBeforeBinding;
			}
			ShadowComponent shadowComponent = character.shadowComponent;
			if (shadowComponent != null && !shadowComponent.IsReleased)
			{
				character.shadowComponent.followHeight = binding.FollowHeightWasEnabled;
				character.shadowComponent.MarkDirty();
				character.shadowComponent.SetShadowVisible(binding.ShadowWasVisible);
			}
		}
		binding = null;
		UpdatePublicBinding(null, isSlot);
	}

	private void UpdatePublicBinding(OccupantBinding binding, bool isSlot)
	{
		if (isSlot)
		{
			slotCharacter = binding?.Character;
			slotCharacterShadow = binding?.ShadowWasVisible ?? false;
		}
		else
		{
			surroundCharacter = binding?.Character;
			surroundCharacterShadow = binding?.ShadowWasVisible ?? false;
		}
	}
}
