using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class BroadCastManager : Node
{
	public delegate void BroadCastOverEventHandler();

	private const string BroadcastFontPath = "res://Asset/Font/fzkt.ttf";

	[Export(PropertyHint.None, "")]
	public bool editorPreviewMode;

	private static PackedScene _broadCastFloatScene;

	public Control broad;

	public Label broadCastLabel;

	public Timer broadCastTimer;

	public Control floatBroaderNode;

	public List<BroadCastConfig> broadCastList = new List<BroadCastConfig>();

	public bool stay;

	public static BroadCastManager Instance { get; private set; }

	private static PackedScene BroadCastFloatScene => _broadCastFloatScene ?? (_broadCastFloatScene = GD.Load<PackedScene>("uid://c43r0mavkmqxl"));

	public event BroadCastOverEventHandler OnBroadCastOver;

	public override void _Ready()
	{
		if (!editorPreviewMode)
		{
			Instance = this;
		}
		broad = GetNode<Control>("%Broad");
		broadCastLabel = GetNode<Label>("%BroadCastLabel");
		broadCastTimer = GetNode<Timer>("%BroadCastTimer");
		floatBroaderNode = GetNode<Control>("%FloatBroaderNode");
		SetPhysicsProcess(enable: false);
		if (!editorPreviewMode && GodotObject.IsInstanceValid(SceneManager.Instance))
		{
			SceneManager.Instance.OnSceneChange += delegate
			{
				BraodCastClear();
			};
		}
		GetNode<Timer>("%BroadCastTimer").Timeout += BroadCastTimerTimeOut;
		LoadFontAfterFirstDraw();
	}

	private async void LoadFontAfterFirstDraw()
	{
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		if (IsInsideTree() && GodotObject.IsInstanceValid(broadCastLabel))
		{
			StartupLoadDiagnostics.Mark("deferred.broadcast_font.begin");
			FontFile fontFile = GD.Load<FontFile>("res://Asset/Font/fzkt.ttf");
			if (GodotObject.IsInstanceValid(fontFile))
			{
				broadCastLabel.AddThemeFontOverride("font", fontFile);
			}
			StartupLoadDiagnostics.Mark("deferred.broadcast_font.end");
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (broadCastList.Count > 0)
		{
			if (!stay && broadCastTimer.IsStopped())
			{
				broadCastLabel.Text = broadCastList[0].broadCastString;
				if (broadCastList[0].broadCastTime != -1.0)
				{
					broadCastTimer.Start(broadCastList[0].broadCastTime);
				}
				else
				{
					stay = true;
				}
				broad.Visible = broadCastList.Count > 0;
				broadCastList.RemoveAt(0);
			}
		}
		else
		{
			SetPhysicsProcess(enable: false);
		}
	}

	public void BroadCastAdd(BroadCastConfig config)
	{
		if (!broadCastList.Contains(config))
		{
			broadCastList.Add(config);
			SetPhysicsProcess(enable: true);
		}
	}

	public void BroadCastTimerTimeOut()
	{
		stay = false;
		this.OnBroadCastOver?.Invoke();
		if (broadCastList.Count == 0)
		{
			broadCastLabel.Text = "";
			broad.Visible = false;
		}
	}

	public void Next()
	{
		stay = false;
		broadCastTimer.Stop();
		if (broadCastList.Count > 0)
		{
			broadCastList.RemoveAt(0);
		}
	}

	public void BraodCastClear()
	{
		stay = false;
		broadCastLabel.Text = "";
		broadCastTimer.Stop();
		broadCastList.Clear();
		broad.Visible = false;
	}

	public void BroadCastFloatCreate(string text, Color color = default(Color))
	{
		if (color == default(Color))
		{
			color = Colors.White;
		}
		BroadCastFloat broadCastFloat = BroadCastFloatScene.Instantiate<BroadCastFloat>(PackedScene.GenEditState.Disabled);
		floatBroaderNode.AddChild(broadCastFloat, forceReadableName: false, InternalMode.Disabled);
		broadCastFloat.Init(text, color);
	}

	public BroadCastManager()
	{
		StartupLoadDiagnostics.Mark("autoload.constructed/BroadCastManager");
	}
}
