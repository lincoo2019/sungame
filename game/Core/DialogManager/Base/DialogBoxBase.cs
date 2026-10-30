using System.Threading.Tasks;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class DialogBoxBase : Control
{
	public delegate void CloseEventHandler();

	[Export(PropertyHint.None, "")]
	public bool pasue;

	[Export(PropertyHint.None, "")]
	public ProcessModeEnum aliveProcessMode = ProcessModeEnum.Pausable;

	[Export(PropertyHint.None, "")]
	public Control dragControl;

	public bool isRoot;

	public bool savePauseState;

	public double saveTimeScale = 1.0;

	public bool dragStart;

	public bool isInDrag;

	public Vector2 savePos = Vector2.Zero;

	public Vector2 saveMousePos = Vector2.Zero;

	public event CloseEventHandler OnClose;

	public async Task WaitForClose()
	{
		TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
		OnClose += Handler;
		try
		{
			await tcs.Task;
		}
		finally
		{
			OnClose -= Handler;
		}
		void Handler()
		{
			tcs.TrySetResult(result: true);
		}
	}

	public virtual void Init(Dictionary data)
	{
	}

	public override void _Ready()
	{
		saveTimeScale = Engine.TimeScale;
		Engine.TimeScale = 1.0;
		if (pasue)
		{
			savePauseState = GetTree().Paused;
			base.ProcessMode = ProcessModeEnum.Always;
			GetTree().Paused = true;
		}
		if (dragControl != null)
		{
			dragControl.GuiInput += DragInput;
			dragControl.MouseEntered += delegate
			{
				isInDrag = true;
			};
			dragControl.MouseExited += delegate
			{
				isInDrag = false;
			};
		}
	}

	public void DragInput(InputEvent _event)
	{
		if (isInDrag)
		{
			if (Input.IsActionJustPressed("Press"))
			{
				dragStart = true;
				savePos = dragControl.GlobalPosition;
				saveMousePos = GetGlobalMousePosition();
			}
			if (Input.IsActionJustReleased("Press"))
			{
				dragStart = false;
			}
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (dragStart)
		{
			dragControl.GlobalPosition = savePos + (GetGlobalMousePosition() - saveMousePos);
		}
	}

	public void ChildClose()
	{
		base.ProcessMode = aliveProcessMode;
	}

	public void CloseDialog()
	{
		Engine.TimeScale = saveTimeScale;
		if (pasue)
		{
			GetTree().Paused = savePauseState;
		}
		this.OnClose?.Invoke();
		QueueFree();
	}

	public DialogBoxBase DialogCreate(string dialogName, Dictionary data = null)
	{
		if (data == null)
		{
			data = new Dictionary();
		}
		PackedScene dialogScene = DialogManager.GetDialogScene(dialogName);
		if (dialogScene == null)
		{
			return null;
		}
		DialogBoxBase dialogBoxBase = (DialogBoxBase)dialogScene.Instantiate(PackedScene.GenEditState.Disabled);
		dialogBoxBase.OnClose += ChildClose;
		dialogBoxBase.Init(data);
		AddChild(dialogBoxBase, forceReadableName: false, InternalMode.Disabled);
		dialogBoxBase.GlobalPosition = Vector2.Zero;
		return dialogBoxBase;
	}
}
