using Godot;

public sealed class LightDetectionComponent : CharacterComponentRuntime
{
	public TowerDefenseCharacter parent;

	public int detectionRadius = 1;

	public bool includeCenter = true;

	private bool _configured;

	private LightDetectionComponentDefinition Definition => base.ComponentDefinition as LightDetectionComponentDefinition;

	protected override void OnBound()
	{
		parent = base.Owner;
		if (!_configured)
		{
			detectionRadius = Definition?.detectionRadius ?? 1;
			includeCenter = Definition?.includeCenter ?? true;
			_configured = true;
		}
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		parent = null;
	}

	protected override void OnReleased()
	{
		parent = null;
	}

	public bool CheckShow()
	{
		if (!base.Alive || base.Lifecycle != ComponentRuntimeLifecycle.Active || !GodotObject.IsInstanceValid(parent))
		{
			return false;
		}
		int num = Mathf.Clamp(detectionRadius, 0, 8);
		Vector2I gridPos = parent.gridPos;
		for (int i = -num; i <= num; i++)
		{
			for (int j = -num; j <= num; j++)
			{
				if (includeCenter || j != 0 || i != 0)
				{
					TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(gridPos + new Vector2I(j, i));
					if (GodotObject.IsInstanceValid(mapCell) && mapCell.HasLight())
					{
						return true;
					}
				}
			}
		}
		return false;
	}
}
