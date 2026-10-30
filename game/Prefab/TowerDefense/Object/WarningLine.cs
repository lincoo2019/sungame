using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class WarningLine : Node2D
{
	private Sprite2D _sprite;

	private AabbArea2D _frontArea;

	private AabbArea2D _backArea;

	private readonly HashSet<TowerDefenseCharacter> _frontOverlaps = new HashSet<TowerDefenseCharacter>();

	private readonly HashSet<TowerDefenseCharacter> _backOverlaps = new HashSet<TowerDefenseCharacter>();

	private readonly HashSet<TowerDefenseCharacter> _scratchOverlaps = new HashSet<TowerDefenseCharacter>();

	public TowerDefenseBattleFeatureWarningLine feature;

	public int column;

	public int row
	{
		get
		{
			return column;
		}
		set
		{
			column = value;
		}
	}

	public override void _Ready()
	{
		_sprite = GetNodeOrNull<Sprite2D>("%Sprite");
		_frontArea = GetNode<AabbArea2D>("Sprite/Area2D");
		_backArea = GetNode<AabbArea2D>("Sprite/Area2D2");
		SetPhysicsProcess(enable: false);
	}

	public void UpdateOverlaps()
	{
		ProcessArea(_frontArea, _frontOverlaps, backArea: false);
		ProcessArea(_backArea, _backOverlaps, backArea: true);
	}

	public void CheckFrontArea(AabbArea2D area)
	{
		if (area.GetParent() is TowerDefenseCharacter tdChar)
		{
			CheckCharacter(tdChar, backArea: false);
		}
	}

	public void CheckBackArea(AabbArea2D area)
	{
		if (area.GetParent() is TowerDefenseCharacter tdChar)
		{
			CheckCharacter(tdChar, backArea: true);
		}
	}

	private void ProcessArea(AabbArea2D area, HashSet<TowerDefenseCharacter> currentOverlaps, bool backArea)
	{
		if (!GodotObject.IsInstanceValid(area) || area.ProcessMode == ProcessModeEnum.Disabled)
		{
			currentOverlaps.Clear();
		}
		else
		{
			if (TowerDefenseManager.Instance == null || TowerDefenseManager.Instance.characterRegistry == null)
			{
				return;
			}
			Rect2 checkRect = AabbShapeUtil.ComputeAreaWorldRect(area);
			List<TowerDefenseCharacter> charactersIntersectingRectList = TowerDefenseManager.Instance.characterRegistry.GetCharactersIntersectingRectList(checkRect);
			_scratchOverlaps.Clear();
			for (int i = 0; i < charactersIntersectingRectList.Count; i++)
			{
				TowerDefenseCharacter towerDefenseCharacter = charactersIntersectingRectList[i];
				if (GodotObject.IsInstanceValid(towerDefenseCharacter))
				{
					_scratchOverlaps.Add(towerDefenseCharacter);
					if (!currentOverlaps.Contains(towerDefenseCharacter))
					{
						CheckCharacter(towerDefenseCharacter, backArea);
					}
				}
			}
			currentOverlaps.RemoveWhere((TowerDefenseCharacter ch) => !GodotObject.IsInstanceValid(ch) || !_scratchOverlaps.Contains(ch));
			foreach (TowerDefenseCharacter scratchOverlap in _scratchOverlaps)
			{
				currentOverlaps.Add(scratchOverlap);
			}
		}
	}

	private void CheckCharacter(TowerDefenseCharacter tdChar, bool backArea)
	{
		if (GodotObject.IsInstanceValid(tdChar) && !tdChar.config.warnningLineFliter && (backArea || !(tdChar.Scale.X < 0f)) && (!backArea || !(tdChar.Scale.X > 0f)) && tdChar.camp != TowerDefenseEnum.CHARACTER_CAMP.PLANT && (tdChar.instance.maskFlags & 0x12) == 0 && tdChar.instance.maskFlags != 0 && !(tdChar is TowerDefensePlant) && !(tdChar is TowerDefenseGravestone) && (!(tdChar is TowerDefenseZombie) || (!tdChar.instance.die && !tdChar.instance.nearDie && (backArea || tdChar.instance.zombiePhysique != TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS))) && feature != null)
		{
			feature.OnWarningLineTriggered();
		}
	}
}
