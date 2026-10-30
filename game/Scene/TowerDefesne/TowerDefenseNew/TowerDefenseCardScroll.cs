using System;
using Godot;

public partial class TowerDefenseCardScroll : ScrollContainer
{
	private int _touchIndex = -1;

	private Vector2 _touchStart;

	private float _touchScroll;

	private bool _touchDragging;

	public bool ScrollingEnabled { get; private set; }

	public bool Vertical { get; private set; }

	public void ConfigureScrolling(bool enabled, bool vertical)
	{
		if (Vertical != vertical || !enabled)
		{
			int scrollHorizontal = (base.ScrollVertical = 0);
			base.ScrollHorizontal = scrollHorizontal;
			_touchIndex = -1;
			_touchDragging = false;
		}
		ScrollingEnabled = enabled;
		Vertical = vertical;
		base.HorizontalScrollMode = (ScrollMode)((enabled && !vertical) ? 3 : 0);
		base.VerticalScrollMode = (ScrollMode)((enabled && vertical) ? 3 : 0);
	}

	private bool CardsOverflow()
	{
		Control childOrNull = GetChildOrNull<Control>(0);
		if (childOrNull == null)
		{
			return false;
		}
		if (!Vertical)
		{
			return childOrNull.Size.X > base.Size.X;
		}
		return childOrNull.Size.Y > base.Size.Y;
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (!ScrollingEnabled || !CardsOverflow() || !IsVisibleInTree())
		{
			return;
		}
		if (inputEvent is InputEventScreenTouch inputEventScreenTouch)
		{
			if (inputEventScreenTouch.Pressed && _touchIndex < 0 && GetGlobalRect().HasPoint(inputEventScreenTouch.Position))
			{
				_touchIndex = inputEventScreenTouch.Index;
				_touchStart = inputEventScreenTouch.Position;
				_touchScroll = (Vertical ? base.ScrollVertical : base.ScrollHorizontal);
				_touchDragging = false;
			}
			else if (!inputEventScreenTouch.Pressed && inputEventScreenTouch.Index == _touchIndex)
			{
				if (_touchDragging)
				{
					TowerDefenseManager.Instance.GetPacketPickControl()?.Release();
				}
				_touchIndex = -1;
				_touchDragging = false;
			}
		}
		else if (inputEvent is InputEventScreenDrag inputEventScreenDrag && inputEventScreenDrag.Index == _touchIndex)
		{
			Vector2 vector = inputEventScreenDrag.Position - _touchStart;
			if (_touchDragging || !(vector.Length() < 16f))
			{
				_touchDragging = true;
				TowerDefenseManager.Instance.GetPacketPickControl()?.Release();
				if (Vertical)
				{
					base.ScrollVertical = (int)(_touchScroll - vector.Y);
				}
				else
				{
					base.ScrollHorizontal = (int)(_touchScroll - vector.X);
				}
				GetViewport().SetInputAsHandled();
			}
		}
		else
		{
			if (!(inputEvent is InputEventMouseButton { Pressed: not false } inputEventMouseButton) || !GetGlobalRect().HasPoint(inputEventMouseButton.Position))
			{
				return;
			}
			MouseButton buttonIndex = inputEventMouseButton.ButtonIndex;
			int num;
			if ((buttonIndex == MouseButton.WheelDown || buttonIndex == MouseButton.WheelRight) ? true : false)
			{
				num = 1;
			}
			else
			{
				MouseButton buttonIndex2 = inputEventMouseButton.ButtonIndex;
				bool flag = ((buttonIndex2 == MouseButton.WheelUp || buttonIndex2 == MouseButton.WheelLeft) ? true : false);
				num = (flag ? (-1) : 0);
			}
			int num2 = num;
			if (num2 != 0)
			{
				int num3 = (int)((float)(Vertical ? 62 : 51) * Math.Max(1f, Math.Abs(inputEventMouseButton.Factor)));
				if (Vertical)
				{
					base.ScrollVertical += num2 * num3;
				}
				else
				{
					base.ScrollHorizontal += num2 * num3;
				}
				GetViewport().SetInputAsHandled();
			}
		}
	}
}
