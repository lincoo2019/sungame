using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseCharacterEventLuckyDraw : TowerDefenseCharacterEventBase
{
	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventLuckyDrawItem> eventList = new Array<TowerDefenseCharacterEventLuckyDrawItem>();

	public override void Execute(Vector2 pos, TowerDefenseCharacter target)
	{
		Run(pos, target, eventList, null);
	}

	public override void ExecuteDps(Vector2 pos, TowerDefenseCharacter target, double delta)
	{
		Run(pos, target, eventList, null);
	}

	public override void ExecuteProject(TowerDefenseProjectile projectile, TowerDefenseCharacter target)
	{
		Run(projectile.GlobalPosition, target, eventList, projectile);
	}

	public static void Run(Vector2 _pos, TowerDefenseCharacter _target, Array<TowerDefenseCharacterEventLuckyDrawItem> _eventList, TowerDefenseProjectile _projectile)
	{
		if (_eventList == null || _eventList.Count == 0)
		{
			return;
		}
		double num = 0.0;
		foreach (TowerDefenseCharacterEventLuckyDrawItem _event in _eventList)
		{
			num += _event.weight;
		}
		if (!(num > 0.0))
		{
			return;
		}
		double num2 = (double)GD.Randf() * num;
		foreach (TowerDefenseCharacterEventLuckyDrawItem _event2 in _eventList)
		{
			if (_event2._event == null)
			{
				continue;
			}
			num2 -= _event2.weight;
			if (num2 <= 0.0)
			{
				if (GodotObject.IsInstanceValid(_projectile))
				{
					_event2._event.ExecuteProject(_projectile, _target);
				}
				else
				{
					_event2._event.Execute(_pos, _target);
				}
				break;
			}
		}
	}
}
