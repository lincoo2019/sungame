using System;
using Godot;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

internal sealed partial class XWAttackContactOverlay : Node2D
{
	private static readonly Vector2[] DummyPositions = new Vector2[6]
	{
		new Vector2(82f, -58f),
		new Vector2(114f, -28f),
		new Vector2(146f, 0f),
		new Vector2(178f, 28f),
		new Vector2(210f, 58f),
		new Vector2(242f, 88f)
	};

	private AttackComponentDefinition _definition;

	private AttackComponent _runtime;

	private Node _owner;

	public int RedrawRevision { get; private set; }

	public int TargetDummyCount => DummyPositions.Length;

	public event Action<int> GeometryProfileRequested;

	public XWAttackContactOverlay()
	{
		SetProcess(enable: false);
		SetPhysicsProcess(enable: false);
		SetProcessInput(enable: false);
	}

	public void Bind(AttackComponentDefinition definition, Node owner, AttackComponent runtime)
	{
		_definition = definition;
		_owner = ((GodotObject.IsInstanceValid(owner) && owner.IsInsideTree()) ? owner : null);
		_runtime = ((runtime != null && !runtime.IsReleased) ? runtime : null);
		RedrawRevision++;
		QueueRedraw();
	}

	public void RefreshDefinition(AttackComponentDefinition definition)
	{
		_definition = definition;
		RedrawRevision++;
		QueueRedraw();
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (!base.Visible || !IsProcessingInput() || !(inputEvent is InputEventMouseButton inputEventMouseButton) || inputEventMouseButton.ButtonIndex != MouseButton.Left || !inputEventMouseButton.Pressed)
		{
			return;
		}
		Vector2 vector = ToLocal(inputEventMouseButton.Position);
		for (int i = 0; i < DummyPositions.Length; i++)
		{
			if (!(vector.DistanceTo(DummyPositions[i]) > 25f))
			{
				this.GeometryProfileRequested?.Invoke(i);
				GetViewport()?.SetInputAsHandled();
				break;
			}
		}
	}

	public override void _Draw()
	{
		if (GodotObject.IsInstanceValid(_owner))
		{
			AttackComponent runtime = _runtime;
			if (runtime != null && !runtime.IsReleased)
			{
				_runtime.DrawCheckAreaCollisionPreview(this);
			}
		}
		if (GodotObject.IsInstanceValid(_definition))
		{
			for (int i = -1; i <= 1; i++)
			{
				float y = (float)i * 42f;
				DrawLine(new Vector2(-42f, y), new Vector2(232f, y), new Color("567082", (i == 0) ? 0.54f : 0.26f), (i != 0) ? 1 : 2);
			}
			int geometryProfile = GetGeometryProfile();
			for (int j = 0; j < DummyPositions.Length; j++)
			{
				Vector2 vector = DummyPositions[j];
				bool flag = j == geometryProfile;
				Color color = (flag ? new Color("b04c3c", 0.88f) : new Color("334652", 0.72f));
				Color color2 = (flag ? new Color("ffb05e") : new Color("83a9ba"));
				DrawCircle(vector + new Vector2(0f, -12f), 10f, color);
				DrawRect(new Rect2(vector + new Vector2(-13f, 0f), new Vector2(26f, 30f)), color);
				DrawCircle(vector + new Vector2(0f, -12f), 10f, color2, filled: false, flag ? 3f : 1.5f);
				DrawLine(new Vector2(vector.X - 13f, vector.Y), new Vector2(vector.X - 13f, vector.Y + 30f), color2, flag ? 3f : 1.5f);
			}
			Vector2 vector2 = DummyPositions[geometryProfile];
			DrawLine(new Vector2(16f, 0f), vector2 - new Vector2(18f, 0f), new Color("ff705a"), 3f);
			DrawCircle(vector2 - new Vector2(18f, 0f), 5f, new Color("ffe0a3"));
			Font fallbackFont = ThemeDB.FallbackFont;
			DrawString(fallbackFont, new Vector2(80f, -72f), _definition.checkAll ? "全体处理 / 无需持续目标" : "点击假人切换目标空间组合", HorizontalAlignment.Left, -1f, 12, new Color("ffd4a1"), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
		}
	}

	private int GetGeometryProfile()
	{
		if (!_definition.checkGrid)
		{
			return _definition.checkLine ? 1 : 0;
		}
		if (!_definition.checkLine)
		{
			if (!_definition.useCheckAreaGridColumn)
			{
				return 2;
			}
			return 3;
		}
		if (!_definition.useCheckAreaGridColumn)
		{
			return 4;
		}
		return 5;
	}
}
