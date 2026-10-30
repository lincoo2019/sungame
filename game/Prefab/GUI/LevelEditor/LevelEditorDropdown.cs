using System.Collections.Generic;
using Godot;

public static class LevelEditorDropdown
{
	private sealed class TouchScroll
	{
		private readonly PopupMenu popup;

		private readonly ScrollContainer scroll;

		private readonly List<(int Index, int Id, string Text)> suspendedItems = new List<(int, int, string)>();

		private int finger = -1;

		private Vector2 start;

		private Vector2 previous;

		private bool dragging;

		private int gesture;

		public TouchScroll(PopupMenu popup)
		{
			this.popup = popup;
			scroll = FindScroll(popup);
			if (scroll != null)
			{
				popup.WindowInput += OnInput;
				popup.VisibilityChanged += OnVisibilityChanged;
			}
		}

		private static ScrollContainer FindScroll(Node node)
		{
			foreach (Node child in node.GetChildren(includeInternal: true))
			{
				if (child is ScrollContainer result)
				{
					return result;
				}
				ScrollContainer scrollContainer = FindScroll(child);
				if (scrollContainer != null)
				{
					return scrollContainer;
				}
			}
			return null;
		}

		private bool IsContent(Vector2 position)
		{
			if (!scroll.GetGlobalRect().HasPoint(position))
			{
				return false;
			}
			VScrollBar vScrollBar = scroll.GetVScrollBar();
			HScrollBar hScrollBar = scroll.GetHScrollBar();
			if (!vScrollBar.Visible || !vScrollBar.GetGlobalRect().HasPoint(position))
			{
				if (hScrollBar.Visible)
				{
					return !hScrollBar.GetGlobalRect().HasPoint(position);
				}
				return true;
			}
			return false;
		}

		private void OnInput(InputEvent input)
		{
			if (!popup.Visible)
			{
				return;
			}
			if (input is InputEventScreenTouch inputEventScreenTouch)
			{
				Vector2 position = inputEventScreenTouch.Position / popup.ContentScaleFactor;
				if (inputEventScreenTouch.Pressed && finger == -1 && IsContent(position))
				{
					Reset();
					finger = inputEventScreenTouch.Index;
					start = (previous = position);
				}
				else
				{
					if (inputEventScreenTouch.Pressed || inputEventScreenTouch.Index != finger)
					{
						return;
					}
					finger = -1;
					int completedGesture = gesture;
					Callable.From(delegate
					{
						if (GodotObject.IsInstanceValid(popup) && gesture == completedGesture)
						{
							Reset();
						}
					}).CallDeferred();
				}
			}
			else
			{
				if (!(input is InputEventScreenDrag inputEventScreenDrag) || inputEventScreenDrag.Index != finger)
				{
					return;
				}
				Vector2 vector = inputEventScreenDrag.Position / popup.ContentScaleFactor;
				if (!dragging)
				{
					if (vector.DistanceTo(start) < (float)Mathf.Max(8, scroll.ScrollDeadzone))
					{
						return;
					}
					dragging = true;
					for (int num = 0; num < popup.ItemCount; num++)
					{
						if (!popup.IsItemDisabled(num))
						{
							suspendedItems.Add((num, popup.GetItemId(num), popup.GetItemText(num)));
							popup.SetItemDisabled(num, disabled: true);
						}
					}
				}
				scroll.GetVScrollBar().Value -= vector.Y - previous.Y;
				previous = vector;
			}
		}

		private void Reset()
		{
			gesture++;
			finger = -1;
			dragging = false;
			foreach (var suspendedItem in suspendedItems)
			{
				if (suspendedItem.Index < popup.ItemCount && popup.GetItemId(suspendedItem.Index) == suspendedItem.Id && popup.GetItemText(suspendedItem.Index) == suspendedItem.Text)
				{
					popup.SetItemDisabled(suspendedItem.Index, disabled: false);
				}
			}
			suspendedItems.Clear();
		}

		private void OnVisibilityChanged()
		{
			if (!popup.Visible)
			{
				Reset();
			}
		}

		public void Disconnect()
		{
			popup.WindowInput -= OnInput;
			popup.VisibilityChanged -= OnVisibilityChanged;
			Reset();
		}
	}

	private const float MaxHeight = 450f;

	private const float EdgeMargin = 8f;

	public static void Configure(OptionButton option)
	{
		PopupMenu popup = option.GetPopup();
		Viewport viewport = option.GetViewport();
		TouchScroll touchScroll = new TouchScroll(popup);
		popup.Transparent = false;
		popup.TransparentBg = false;
		StyleBox originalPanel = popup.GetThemeStylebox("panel");
		StyleBoxFlat embeddedPanel = ((originalPanel is StyleBoxFlat styleBoxFlat) ? ((StyleBoxFlat)styleBoxFlat.Duplicate()) : new StyleBoxFlat
		{
			BgColor = new Color(0.15f, 0.15f, 0.15f)
		});
		embeddedPanel.BgColor = new Color(embeddedPanel.BgColor);
		embeddedPanel.BorderColor = new Color(embeddedPanel.BorderColor);
		embeddedPanel.DrawCenter = true;
		embeddedPanel.ShadowSize = 0;
		bool hasPanelOverride = popup.HasThemeStyleboxOverride("panel");
		bool usingEmbeddedPanel = false;
		popup.AboutToPopup += UpdateLimits;
		popup.VisibilityChanged += PlacePopup;
		viewport.SizeChanged += OnViewportSizeChanged;
		option.TreeExiting += Disconnect;
		void Disconnect()
		{
			touchScroll.Disconnect();
			popup.AboutToPopup -= UpdateLimits;
			popup.VisibilityChanged -= PlacePopup;
			viewport.SizeChanged -= OnViewportSizeChanged;
			option.TreeExiting -= Disconnect;
		}
		Rect2 GetBounds()
		{
			Rect2 rect = option.GetViewportRect();
			if (!popup.IsEmbedded())
			{
				rect = option.GetScreenTransform() * option.GetGlobalTransformWithCanvas().AffineInverse() * rect;
				Rect2 b = DisplayServer.ScreenGetUsableRect(option.GetWindow().CurrentScreen);
				if (rect.Intersects(b))
				{
					rect = rect.Intersection(b);
				}
			}
			float num = Mathf.Min(8f, Mathf.Min(rect.Size.X, rect.Size.Y) / 4f);
			return rect.Grow(0f - num);
		}
		void OnViewportSizeChanged()
		{
			if (popup.Visible)
			{
				UpdateLimits();
				PlacePopup();
			}
		}
		void PlacePopup()
		{
			if (popup.Visible)
			{
				Rect2 rect = GetBounds();
				Rect2 rect2 = option.GetScreenTransform() * new Rect2(Vector2.Zero, option.Size);
				float num = rect2.End.Y;
				if (num + (float)popup.Size.Y > rect.End.Y)
				{
					num = rect2.Position.Y - (float)popup.Size.Y;
				}
				popup.Position = new Vector2I(Mathf.RoundToInt(Mathf.Clamp(rect2.Position.X, rect.Position.X, Mathf.Max(rect.Position.X, rect.End.X - (float)popup.Size.X))), Mathf.RoundToInt(Mathf.Clamp(num, rect.Position.Y, Mathf.Max(rect.Position.Y, rect.End.Y - (float)popup.Size.Y))));
			}
		}
		void UpdateBackground()
		{
			popup.Transparent = false;
			popup.TransparentBg = false;
			if (popup.IsEmbedded() && !usingEmbeddedPanel)
			{
				popup.AddThemeStyleboxOverride("panel", embeddedPanel);
				usingEmbeddedPanel = true;
			}
			else if (!popup.IsEmbedded() && usingEmbeddedPanel)
			{
				if (hasPanelOverride)
				{
					popup.AddThemeStyleboxOverride("panel", originalPanel);
				}
				else
				{
					popup.RemoveThemeStyleboxOverride("panel");
				}
				usingEmbeddedPanel = false;
			}
		}
		void UpdateLimits()
		{
			UpdateBackground();
			Rect2 rect = GetBounds();
			Vector2 vector = option.GetScreenTransform().Scale.Abs();
			float num = Mathf.Max(0.01f, Mathf.Min(vector.X, vector.Y));
			popup.MinSize = Vector2I.Zero;
			popup.MaxSize = new Vector2I(Mathf.Max(1, Mathf.FloorToInt(rect.Size.X)), Mathf.Max(1, Mathf.FloorToInt(Mathf.Min(450f * num, rect.Size.Y))));
			popup.ContentScaleFactor = num;
		}
	}
}
