using System;
using Godot;

namespace PVZHE.ModEditor.SceneEditor;

public partial class XW2DSpriteAtlasCanvas : Control
{
	[Signal]
	public delegate void FramePickedEventHandler(int frame);

	private const int MaximumGuideLinesPerAxis = 128;

	private Texture2D _texture;

	private int _columns = 1;

	private int _rows = 1;

	private int _frame;

	private bool _regionEnabled;

	private Rect2 _regionRect;

	private Rect2 _drawRect;

	public int Columns => _columns;

	public int Rows => _rows;

	public int SelectedFrame => _frame;

	public Rect2 DrawnAtlasRect
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_texture))
			{
				return default(Rect2);
			}
			Vector2 size = _texture.GetSize();
			_drawRect = CalculateTextureDrawRect(size);
			return PixelRectToDraw(ResolveSourcePixels(size), size);
		}
	}

	public bool IsGuideDensityLimited
	{
		get
		{
			if (_columns <= 128)
			{
				return _rows > 128;
			}
			return true;
		}
	}

	public override void _Ready()
	{
		base.CustomMinimumSize = new Vector2(360f, 260f);
		base.MouseDefaultCursorShape = CursorShape.PointingHand;
		base.MouseFilter = MouseFilterEnum.Stop;
		SetProcess(enable: false);
		base.Resized += base.QueueRedraw;
		base.GuiInput += OnGuiInput;
	}

	public override void _ExitTree()
	{
		base.Resized -= base.QueueRedraw;
		base.GuiInput -= OnGuiInput;
	}

	public void Bind(Texture2D texture, int columns, int rows, int frame, bool regionEnabled, Rect2 regionRect)
	{
		_texture = texture;
		_columns = Math.Clamp(columns, 1, 256);
		_rows = Math.Clamp(rows, 1, 256);
		_frame = Math.Clamp(frame, 0, Math.Max(0, _columns * _rows - 1));
		_regionEnabled = regionEnabled;
		_regionRect = regionRect;
		QueueRedraw();
	}

	public override void _Draw()
	{
		DrawRect(new Rect2(Vector2.Zero, base.Size), new Color("#0b1119"));
		DrawCheckerboard();
		if (!GodotObject.IsInstanceValid(_texture))
		{
			DrawString(ThemeDB.FallbackFont, base.Size * 0.5f - new Vector2(78f, 0f), "先给 Sprite2D 选择贴图", HorizontalAlignment.Left, -1f, 16, new Color("#8fa5b8"), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
			_drawRect = default(Rect2);
			return;
		}
		Vector2 size = _texture.GetSize();
		if (!(size.X <= 0f) && !(size.Y <= 0f))
		{
			_drawRect = CalculateTextureDrawRect(size);
			DrawTextureRect(_texture, _drawRect, tile: false);
			DrawRect(_drawRect, new Color("#6e8ca5"), filled: false, 1f);
			Rect2 pixels = ResolveSourcePixels(size);
			Rect2 rect = PixelRectToDraw(pixels, size);
			if (_regionEnabled)
			{
				DrawRegionMask(rect);
				DrawRect(rect, new Color("#5ae6a8"), filled: false, 2f);
			}
			DrawGrid(rect);
			DrawSelection(rect);
		}
	}

	private void DrawCheckerboard()
	{
		Color color = new Color("#111c28");
		Color color2 = new Color("#172634");
		int num = Mathf.CeilToInt(base.Size.X / 20f);
		int num2 = Mathf.CeilToInt(base.Size.Y / 20f);
		for (int i = 0; i < num2; i++)
		{
			for (int j = 0; j < num; j++)
			{
				DrawRect(new Rect2((float)j * 20f, (float)i * 20f, 20f, 20f), (((j + i) & 1) == 0) ? color : color2);
			}
		}
	}

	private Rect2 ResolveSourcePixels(Vector2 textureSize)
	{
		if (!_regionEnabled || _regionRect.Size.X <= 0f || _regionRect.Size.Y <= 0f)
		{
			return new Rect2(Vector2.Zero, textureSize);
		}
		Rect2 b = new Rect2(Vector2.Zero, textureSize);
		return _regionRect.Intersection(b);
	}

	private Rect2 PixelRectToDraw(Rect2 pixels, Vector2 textureSize)
	{
		Vector2 vector = _drawRect.Size / textureSize;
		return new Rect2(_drawRect.Position + pixels.Position * vector, pixels.Size * vector);
	}

	private Rect2 CalculateTextureDrawRect(Vector2 textureSize)
	{
		if (textureSize.X <= 0f || textureSize.Y <= 0f)
		{
			return default(Rect2);
		}
		Rect2 rect = new Rect2(new Vector2(18f, 18f), new Vector2(Mathf.Max(1f, base.Size.X - 36f), Mathf.Max(1f, base.Size.Y - 36f)));
		float num = Mathf.Min(rect.Size.X / textureSize.X, rect.Size.Y / textureSize.Y);
		Vector2 vector = textureSize * num;
		return new Rect2(rect.Position + (rect.Size - vector) * 0.5f, vector);
	}

	private void DrawRegionMask(Rect2 active)
	{
		Color color = new Color(0.02f, 0.04f, 0.07f, 0.7f);
		if (active.Position.Y > _drawRect.Position.Y)
		{
			DrawRect(new Rect2(_drawRect.Position, new Vector2(_drawRect.Size.X, active.Position.Y - _drawRect.Position.Y)), color);
		}
		if (active.End.Y < _drawRect.End.Y)
		{
			DrawRect(new Rect2(new Vector2(_drawRect.Position.X, active.End.Y), new Vector2(_drawRect.Size.X, _drawRect.End.Y - active.End.Y)), color);
		}
		if (active.Position.X > _drawRect.Position.X)
		{
			DrawRect(new Rect2(new Vector2(_drawRect.Position.X, active.Position.Y), new Vector2(active.Position.X - _drawRect.Position.X, active.Size.Y)), color);
		}
		if (active.End.X < _drawRect.End.X)
		{
			DrawRect(new Rect2(new Vector2(active.End.X, active.Position.Y), new Vector2(_drawRect.End.X - active.End.X, active.Size.Y)), color);
		}
	}

	private void DrawGrid(Rect2 sourceDraw)
	{
		Color color = new Color(0.64f, 0.83f, 0.94f, 0.6f);
		int num = Math.Max(1, Mathf.CeilToInt((float)_columns / 128f));
		int num2 = Math.Max(1, Mathf.CeilToInt((float)_rows / 128f));
		for (int i = 0; i <= _columns; i += num)
		{
			float x = sourceDraw.Position.X + sourceDraw.Size.X * (float)i / (float)_columns;
			DrawLine(new Vector2(x, sourceDraw.Position.Y), new Vector2(x, sourceDraw.End.Y), color, 1f);
		}
		if (_columns % num != 0)
		{
			DrawLine(new Vector2(sourceDraw.End.X, sourceDraw.Position.Y), sourceDraw.End, color, 1f);
		}
		for (int j = 0; j <= _rows; j += num2)
		{
			float y = sourceDraw.Position.Y + sourceDraw.Size.Y * (float)j / (float)_rows;
			DrawLine(new Vector2(sourceDraw.Position.X, y), new Vector2(sourceDraw.End.X, y), color, 1f);
		}
		if (_rows % num2 != 0)
		{
			DrawLine(new Vector2(sourceDraw.Position.X, sourceDraw.End.Y), sourceDraw.End, color, 1f);
		}
	}

	private void DrawSelection(Rect2 sourceDraw)
	{
		int num = _frame % _columns;
		int num2 = _frame / _columns;
		Vector2 vector = sourceDraw.Size / new Vector2(_columns, _rows);
		Rect2 rect = new Rect2(sourceDraw.Position + new Vector2(num, num2) * vector, vector);
		DrawRect(rect, new Color(0.98f, 0.72f, 0.23f, 0.22f));
		DrawRect(rect.Grow(-1f), new Color("#ffd166"), filled: false, 3f);
		string text = $"{_frame}";
		DrawString(ThemeDB.FallbackFont, rect.Position + new Vector2(5f, 17f), text, HorizontalAlignment.Left, -1f, 14, new Color("#fff2be"), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
	}

	private void OnGuiInput(InputEvent inputEvent)
	{
		if (inputEvent is InputEventMouseButton inputEventMouseButton && inputEventMouseButton.ButtonIndex == MouseButton.Left && inputEventMouseButton.Pressed && GodotObject.IsInstanceValid(_texture))
		{
			Vector2 size = _texture.GetSize();
			_drawRect = CalculateTextureDrawRect(size);
			Rect2 rect = PixelRectToDraw(ResolveSourcePixels(size), size);
			if (rect.HasPoint(inputEventMouseButton.Position) && !(rect.Size.X <= 0f) && !(rect.Size.Y <= 0f))
			{
				Vector2 vector = (inputEventMouseButton.Position - rect.Position) / rect.Size;
				int num = Math.Clamp(Mathf.FloorToInt(vector.X * (float)_columns), 0, _columns - 1);
				int num2 = Math.Clamp(Mathf.FloorToInt(vector.Y * (float)_rows), 0, _rows - 1);
				EmitSignal(SignalName.FramePicked, num2 * _columns + num);
				AcceptEvent();
			}
		}
	}
}
