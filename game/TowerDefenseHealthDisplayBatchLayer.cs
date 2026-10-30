using System.Collections.Generic;
using Godot;

public sealed partial class TowerDefenseHealthDisplayBatchLayer : Node2D
{
	private const int OutlineSize = 5;

	private readonly List<ShowHealthComponent> _displays = new List<ShowHealthComponent>(64);

	private readonly List<HealthDisplayDrawData> _drawData = new List<HealthDisplayDrawData>(64);

	private bool _redrawRequested;

	internal Font Font { get; set; }

	internal HealthTextLayoutCache TextLayoutCache { get; set; }

	internal TowerDefenseHealthDisplayBatch OwnerBatch { get; set; }

	internal int DisplayCount => _displays.Count;

	internal void Add(ShowHealthComponent display)
	{
		_displays.Add(display);
		RequestRedraw();
	}

	internal void Remove(ShowHealthComponent display)
	{
		int num = _displays.IndexOf(display);
		if (num >= 0)
		{
			_displays.RemoveAt(num);
			RequestRedraw();
		}
	}

	internal void RequestRedraw()
	{
		if (!_redrawRequested)
		{
			_redrawRequested = true;
			QueueRedraw();
		}
	}

	public override void _Ready()
	{
		base.TopLevel = true;
		base.GlobalTransform = Transform2D.Identity;
		SetProcess(enable: false);
	}

	public override void _Draw()
	{
		_redrawRequested = false;
		if (GodotObject.IsInstanceValid(Font))
		{
			CollectDrawData();
			float height = Font.GetHeight(14);
			Rid canvasItem = GetCanvasItem();
			DrawPass(outline: true, height, canvasItem);
			DrawPass(outline: false, height, canvasItem);
			OwnerBatch?.ReportDrawRebuild();
		}
	}

	private void CollectDrawData()
	{
		_drawData.Clear();
		for (int i = 0; i < _displays.Count; i++)
		{
			ShowHealthComponent showHealthComponent = _displays[i];
			if (showHealthComponent != null && showHealthComponent.TryGetSharedDrawData(out var data))
			{
				_drawData.Add(data);
			}
		}
	}

	private void DrawPass(bool outline, float lineHeight, Rid canvasItem)
	{
		for (int i = 0; i < _drawData.Count; i++)
		{
			DrawDisplay(_drawData[i], outline, lineHeight, canvasItem);
		}
	}

	private void DrawDisplay(in HealthDisplayDrawData data, bool outline, float lineHeight, Rid canvasItem)
	{
		int num = 0;
		if (!string.IsNullOrEmpty(data.ShieldText))
		{
			num++;
		}
		if (!string.IsNullOrEmpty(data.HelmetText))
		{
			num++;
		}
		if (!string.IsNullOrEmpty(data.BodyText))
		{
			num++;
		}
		if (num != 0)
		{
			float num2 = data.Center.Y - lineHeight * (float)num * 0.5f;
			float x = data.Center.X - 56f;
			if (!string.IsNullOrEmpty(data.ShieldText))
			{
				DrawHealthLine(canvasItem, new Vector2(x, num2), data.ShieldText, data.ShieldColor, outline);
				num2 += lineHeight;
			}
			if (!string.IsNullOrEmpty(data.HelmetText))
			{
				DrawHealthLine(canvasItem, new Vector2(x, num2), data.HelmetText, data.HelmetColor, outline);
				num2 += lineHeight;
			}
			if (!string.IsNullOrEmpty(data.BodyText))
			{
				DrawHealthLine(canvasItem, new Vector2(x, num2), data.BodyText, data.BodyColor, outline);
			}
		}
	}

	private void DrawHealthLine(Rid canvasItem, Vector2 topLeft, string text, Color color, bool outline)
	{
		CachedTextLine cachedTextLine = TextLayoutCache?.GetOrCreate(text);
		if (cachedTextLine != null)
		{
			if (outline)
			{
				cachedTextLine.Line.DrawOutline(canvasItem, topLeft, 5, Colors.Black);
			}
			else
			{
				cachedTextLine.Line.Draw(canvasItem, topLeft, color);
			}
		}
	}
}
