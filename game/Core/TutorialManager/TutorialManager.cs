using System.Threading.Tasks;
using Godot;

[GlobalClass]
public partial class TutorialManager : Node
{
	public delegate void TutorialFinishEventHandler();

	public TutorialConfig currentTutoroal;

	public int currentStep;

	public bool stepExe;

	private bool _isProcessing;

	public static TutorialManager Instance { get; private set; }

	public event TutorialFinishEventHandler OnTutorialFinish;

	public async Task WaitForFinish()
	{
		TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
		OnTutorialFinish += Handler;
		try
		{
			await tcs.Task;
		}
		finally
		{
			OnTutorialFinish -= Handler;
		}
		void Handler()
		{
			tcs.TrySetResult(result: true);
		}
	}

	public override void _Ready()
	{
		Instance = this;
		SceneManager.Instance.OnSceneChange += delegate
		{
			TutorialClear();
		};
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!stepExe)
		{
			return;
		}
		TutorialStepConfig tutorialStep = currentTutoroal.GetTutorialStep(currentStep);
		if (!tutorialStep.Step())
		{
			return;
		}
		stepExe = false;
		tutorialStep.Exit();
		if (tutorialStep.broadCastUse)
		{
			if (tutorialStep.broadCastConfig.broadCastTime == -1.0)
			{
				BroadCastManager.Instance.Next();
				currentStep++;
				TutorialExecute();
			}
			else if (!_isProcessing)
			{
				_isProcessing = true;
				ProcessBroadCastAsync();
			}
		}
		else
		{
			currentStep++;
			TutorialExecute();
		}
	}

	private async void ProcessBroadCastAsync()
	{
		TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
		BroadCastManager.Instance.OnBroadCastOver += OnBroadCastOverHandler;
		try
		{
			await tcs.Task;
			if (GodotObject.IsInstanceValid(this))
			{
				currentStep++;
				TutorialExecute();
			}
		}
		finally
		{
			BroadCastManager.Instance.OnBroadCastOver -= OnBroadCastOverHandler;
			_isProcessing = false;
		}
		void OnBroadCastOverHandler()
		{
			tcs.TrySetResult(result: true);
		}
	}

	public void StartTutorial(TutorialConfig tutorial)
	{
		currentTutoroal = tutorial;
		currentStep = 0;
		TutorialExecute();
	}

	public void TutorialExecute()
	{
		if (!GodotObject.IsInstanceValid(currentTutoroal))
		{
			return;
		}
		if (currentStep >= currentTutoroal.GetStepNum())
		{
			currentTutoroal = null;
			BroadCastManager.Instance.BraodCastClear();
			this.OnTutorialFinish?.Invoke();
			return;
		}
		TutorialStepConfig tutorialStep = currentTutoroal.GetTutorialStep(currentStep);
		if (tutorialStep.broadCastUse)
		{
			BroadCastManager.Instance.BroadCastAdd(tutorialStep.broadCastConfig);
		}
		tutorialStep.Enter();
		stepExe = true;
	}

	public void TutorialClear()
	{
		currentTutoroal = null;
		currentStep = 0;
		stepExe = false;
		BroadCastManager.Instance.BraodCastClear();
	}

	public TutorialManager()
	{
		StartupLoadDiagnostics.Mark("autoload.constructed/TutorialManager");
	}
}
