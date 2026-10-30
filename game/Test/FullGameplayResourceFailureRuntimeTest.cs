using System;
using System.Reflection;
using Godot;

public partial class FullGameplayResourceFailureRuntimeTest : Node
{
	private const string FixtureError = "category=CharacterScene, key=BrokenCharacterFixture, path=res://Test/Fixture/MissingCharacterScene.tscn";

	private const string ResultMarker = "FULL_GAMEPLAY_RESOURCE_FAILURE_RESULT";

	private int _failureEventCount;

	private int _loadOverEventCount;

	private int _percentageEventCount;

	private int _failureCount;

	public override async void _Ready()
	{
		ResourceManager resourceManager = ResourceManager.Instance;
		if (!GodotObject.IsInstanceValid(resourceManager))
		{
			Fail("ResourceManager is unavailable");
			Finish();
			return;
		}
		resourceManager.OnLoadFailed += ThrowingLoadFailedSubscriber;
		resourceManager.OnLoadFailed += OnLoadFailed;
		resourceManager.OnLoadOver += ThrowingLoadOverSubscriber;
		resourceManager.OnLoadOver += OnLoadOver;
		resourceManager.OnLoadPercentage += ThrowingLoadPercentageSubscriber;
		resourceManager.OnLoadPercentage += OnLoadPercentage;
		MethodInfo method = typeof(ResourceManager).GetMethod("FailFullGameplayLoad", BindingFlags.Instance | BindingFlags.NonPublic);
		MethodInfo method2 = typeof(ResourceManager).GetMethod("LoadBuiltInRoot", BindingFlags.Instance | BindingFlags.NonPublic);
		MethodInfo method3 = typeof(ResourceManager).GetMethod("NotifyLoadOverSubscribers", BindingFlags.Instance | BindingFlags.NonPublic);
		MethodInfo method4 = typeof(ResourceManager).GetMethod("EmitLoadPercentage", BindingFlags.Instance | BindingFlags.NonPublic);
		Check(method != null, "ResourceManager failure transition method was not found");
		Check(method2 != null, "ResourceManager strict root loading method was not found");
		Check(method3 != null, "ResourceManager safe success notification method was not found");
		Check(method4 != null, "ResourceManager safe progress notification method was not found");
		if (method == null || method2 == null || method3 == null || method4 == null)
		{
			Finish();
			return;
		}
		Exception ex = null;
		try
		{
			object[] parameters = new object[4] { "res://Test/Fixture/MissingCharacterScene.tscn", "CharacterScene", "BrokenCharacterFixture", 0.0 };
			method2.Invoke(resourceManager, parameters);
		}
		catch (TargetInvocationException ex2)
		{
			ex = ex2.InnerException;
		}
		Check(ex != null, "Missing CharacterScene root did not produce a strict load failure");
		Check(ex?.Message.Contains("category=CharacterScene, key=BrokenCharacterFixture, path=res://Test/Fixture/MissingCharacterScene.tscn", StringComparison.Ordinal) ?? false, "Strict root failure omitted category, key, or path: " + ex?.Message);
		method3.Invoke(resourceManager, null);
		Check(_loadOverEventCount == 1, $"Throwing success subscriber prevented later subscribers: events={_loadOverEventCount}");
		Check(resourceManager.CurrentGameplayResourceLoadState == GameplayResourceLoadState.NotStarted, "Success subscriber exception changed the pre-load resource state");
		method.Invoke(resourceManager, new object[1] { ex ?? new InvalidOperationException("category=CharacterScene, key=BrokenCharacterFixture, path=res://Test/Fixture/MissingCharacterScene.tscn") });
		Check(resourceManager.CurrentGameplayResourceLoadState == GameplayResourceLoadState.Failed, $"ResourceManager did not enter Failed: {resourceManager.CurrentGameplayResourceLoadState}");
		Check(resourceManager.FullGameplayResourceLoadError.Contains("category=CharacterScene, key=BrokenCharacterFixture, path=res://Test/Fixture/MissingCharacterScene.tscn", StringComparison.Ordinal), "ResourceManager failure omitted fixture path: " + resourceManager.FullGameplayResourceLoadError);
		method4.Invoke(resourceManager, new object[5] { 0.5, "FAILURE_FIXTURE", "BrokenCharacterFixture", 1, 1 });
		Check(_percentageEventCount == 1, $"Throwing progress subscriber prevented later subscribers: events={_percentageEventCount}");
		Check(resourceManager.CurrentGameplayResourceLoadState == GameplayResourceLoadState.Failed, "Progress subscriber exception changed the terminal resource state");
		resourceManager.BeginLoad();
		Check(resourceManager.CurrentGameplayResourceLoadState == GameplayResourceLoadState.Failed, "BeginLoad retried a terminal failure");
		PackedScene packedScene = GD.Load<PackedScene>("res://Scene/Loading/Loading.tscn");
		Check(GodotObject.IsInstanceValid(packedScene), "Loading scene is unavailable");
		if (!GodotObject.IsInstanceValid(packedScene))
		{
			Finish();
			return;
		}
		Loading loading = packedScene.Instantiate<Loading>(PackedScene.GenEditState.Disabled);
		AddChild(loading, forceReadableName: false, InternalMode.Disabled);
		for (int frame = 0; frame < 12; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		Button node = loading.GetNode<Button>("%StartButton");
		Label node2 = loading.GetNode<Label>("%LoadLabel");
		Check(node.Disabled, "Loading enabled StartButton after a terminal resource failure");
		Check(node2.Visible, "Loading hid the terminal resource failure label");
		Check(node2.Text.Contains("CharacterScene", StringComparison.Ordinal) && node2.Text.Contains("BrokenCharacterFixture", StringComparison.Ordinal) && node2.Text.Contains("res://Test/Fixture/MissingCharacterScene.tscn", StringComparison.Ordinal), "Loading failure label omitted category, key, or path: " + node2.Text);
		Check(!resourceManager.AreFullGameplayResourcesReady, "Failed ResourceManager reported Ready");
		Check(_failureEventCount >= 3, $"Terminal failure was not replayed to later callers: events={_failureEventCount}");
		Check(_loadOverEventCount == 1, $"Terminal failure fired an additional OnLoadOver event: events={_loadOverEventCount}");
		loading.QueueFree();
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		resourceManager.OnLoadFailed -= OnLoadFailed;
		resourceManager.OnLoadFailed -= ThrowingLoadFailedSubscriber;
		resourceManager.OnLoadOver -= OnLoadOver;
		resourceManager.OnLoadOver -= ThrowingLoadOverSubscriber;
		resourceManager.OnLoadPercentage -= OnLoadPercentage;
		resourceManager.OnLoadPercentage -= ThrowingLoadPercentageSubscriber;
		Finish();
	}

	private void ThrowingLoadFailedSubscriber(string error)
	{
		throw new InvalidOperationException("Failure subscriber fixture exception.");
	}

	private void ThrowingLoadOverSubscriber()
	{
		throw new InvalidOperationException("Success subscriber fixture exception.");
	}

	private void ThrowingLoadPercentageSubscriber(double percentage, string stepName, string resourceName, int resourceIndex, int resourceTotal)
	{
		throw new InvalidOperationException("Progress subscriber fixture exception.");
	}

	private void OnLoadFailed(string error)
	{
		_failureEventCount++;
	}

	private void OnLoadOver()
	{
		_loadOverEventCount++;
	}

	private void OnLoadPercentage(double percentage, string stepName, string resourceName, int resourceIndex, int resourceTotal)
	{
		_percentageEventCount++;
	}

	private void Check(bool condition, string message)
	{
		if (!condition)
		{
			Fail(message);
		}
	}

	private void Fail(string message)
	{
		_failureCount++;
		GD.PrintErr("FULL_GAMEPLAY_RESOURCE_FAILURE_CHECK_FAILED " + message);
	}

	private void Finish()
	{
		GD.Print($"{"FULL_GAMEPLAY_RESOURCE_FAILURE_RESULT"} passed={_failureCount == 0} state={ResourceManager.Instance?.CurrentGameplayResourceLoadState} failureEvents={_failureEventCount} loadOverEvents={_loadOverEventCount} percentageEvents={_percentageEventCount} failures={_failureCount}");
		GetTree().Quit((_failureCount != 0) ? 1 : 0);
	}
}
