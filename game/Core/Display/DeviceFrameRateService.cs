using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class DeviceFrameRateService : Node
{
	private const double DesktopRefreshPollSeconds = 1.0;

	private readonly List<DisplayRefreshMode> _androidModes = new List<DisplayRefreshMode>();

	private double _desktopPollElapsed;

	private int _lastScreen = -2147483648;

	private bool _hasApplied;

	private bool _androidFailureReported;

	private int _androidWidth;

	private int _androidHeight;

	public int PreferredFrameRate { get; private set; } = 60;

	public int DeviceFrameRateLimit { get; private set; } = 60;

	public int EffectiveFrameRate { get; private set; } = 60;

	public int[] FrameRateOptions { get; private set; } = FrameRatePolicy.BuildOptions(60);

	private bool IsAndroid => OS.GetName() == "Android";

	public event Action FrameRateOptionsChanged;

	public event Action<int> EffectiveFrameRateChanged;

	public void Initialize(double preferredFrameRate)
	{
		PreferredFrameRate = FrameRatePolicy.NormalizeRefreshRate(preferredFrameRate);
	}

	public override void _Ready()
	{
		if (IsAndroid)
		{
			RefreshAndroidCapabilities();
			SetProcess(enable: false);
		}
		else
		{
			RefreshDesktopCapabilities(force: true);
			SetProcess(enable: true);
		}
	}

	public override void _Process(double delta)
	{
		_desktopPollElapsed += delta;
		if (!(_desktopPollElapsed < 1.0))
		{
			_desktopPollElapsed = 0.0;
			RefreshDesktopCapabilities(force: false);
		}
	}

	public override void _Notification(int what)
	{
		if ((long)what != 2014)
		{
			return;
		}
		Callable.From(delegate
		{
			if (IsAndroid)
			{
				RefreshAndroidCapabilities();
			}
			else
			{
				RefreshDesktopCapabilities(force: true);
			}
		}).CallDeferred();
	}

	public int[] GetFrameRateOptions()
	{
		return (int[])FrameRateOptions.Clone();
	}

	public void SetPreferredFrameRate(double value)
	{
		PreferredFrameRate = FrameRatePolicy.NormalizeRefreshRate(value);
		ApplyPreferredFrameRate();
	}

	private void RefreshDesktopCapabilities(bool force)
	{
		int num = DisplayServer.WindowGetCurrentScreen();
		double num2 = DisplayServer.ScreenGetRefreshRate(num);
		int num3 = ((num2 >= 1.0) ? FrameRatePolicy.NormalizeRefreshRate(num2) : 60);
		if (force || num != _lastScreen || num3 != DeviceFrameRateLimit)
		{
			_lastScreen = num;
			SetDeviceLimit(num3);
		}
	}

	private void RefreshAndroidCapabilities()
	{
		try
		{
			if (!TryGetAndroidContext(out var _, out var activity))
			{
				ApplyAndroidFallback("AndroidRuntime or Activity is unavailable.");
				return;
			}
			JavaObject androidDisplay = GetAndroidDisplay(activity);
			JavaObject javaObject = ((androidDisplay == null) ? null : (androidDisplay.Call("getMode").AsGodotObject() as JavaObject));
			JavaObject[] array = ((androidDisplay == null) ? Array.Empty<JavaObject>() : androidDisplay.Call("getSupportedModes").AsGodotObjectArray<JavaObject>());
			if (javaObject == null || array.Length == 0)
			{
				ApplyAndroidFallback("Android returned no supported display modes.");
				return;
			}
			_androidWidth = javaObject.Call("getPhysicalWidth").AsInt32();
			_androidHeight = javaObject.Call("getPhysicalHeight").AsInt32();
			_androidModes.Clear();
			JavaObject[] array2 = array;
			foreach (JavaObject javaObject2 in array2)
			{
				if (javaObject2 != null)
				{
					_androidModes.Add(new DisplayRefreshMode(javaObject2.Call("getModeId").AsInt32(), javaObject2.Call("getPhysicalWidth").AsInt32(), javaObject2.Call("getPhysicalHeight").AsInt32(), FrameRatePolicy.NormalizeRefreshRate(javaObject2.Call("getRefreshRate").AsDouble())));
				}
			}
			ThrowIfJavaException("query supported display modes");
			if (_androidModes.Count == 0)
			{
				ApplyAndroidFallback("Android returned only invalid display modes.");
				return;
			}
			_androidFailureReported = false;
			DisplayRefreshMode[] array3 = _androidModes.Where((DisplayRefreshMode mode) => mode.Width == _androidWidth && mode.Height == _androidHeight).ToArray();
			DisplayRefreshMode[] source = ((array3.Length != 0) ? array3 : _androidModes.ToArray());
			SetDeviceLimit(source.Max((DisplayRefreshMode mode) => mode.RefreshRate));
		}
		catch (Exception ex)
		{
			ApplyAndroidFallback("Android refresh-rate discovery failed: " + ex.Message);
		}
	}

	private static bool TryGetAndroidContext(out GodotObject runtime, out JavaObject activity)
	{
		runtime = null;
		activity = null;
		if (!Engine.HasSingleton("AndroidRuntime"))
		{
			return false;
		}
		runtime = Engine.GetSingleton("AndroidRuntime");
		activity = runtime.Call("getActivity").AsGodotObject() as JavaObject;
		return activity != null;
	}

	private static JavaObject GetAndroidDisplay(JavaObject activity)
	{
		if (activity.Call("getWindowManager").AsGodotObject() is JavaObject javaObject)
		{
			return javaObject.Call("getDefaultDisplay").AsGodotObject() as JavaObject;
		}
		return null;
	}

	private void SetDeviceLimit(int limit)
	{
		DeviceFrameRateLimit = Math.Max(1, limit);
		int[] array = FrameRatePolicy.BuildOptions(DeviceFrameRateLimit);
		bool num = !FrameRateOptions.SequenceEqual(array);
		if (num)
		{
			FrameRateOptions = array;
		}
		ApplyPreferredFrameRate();
		if (num)
		{
			this.FrameRateOptionsChanged?.Invoke();
		}
	}

	private void ApplyPreferredFrameRate()
	{
		int num = FrameRatePolicy.ResolveEffective(PreferredFrameRate, DeviceFrameRateLimit);
		if (!_hasApplied || num != EffectiveFrameRate)
		{
			if (IsAndroid && _androidModes.Count > 0)
			{
				RequestAndroidRefreshRate(num);
			}
			_hasApplied = true;
			EffectiveFrameRate = num;
			this.EffectiveFrameRateChanged?.Invoke(num);
		}
	}

	private void RequestAndroidRefreshRate(int targetFrameRate)
	{
		DisplayRefreshMode selected = FrameRatePolicy.SelectAndroidMode(_androidModes.ToArray(), targetFrameRate, _androidWidth, _androidHeight);
		if (selected == null || !TryGetAndroidContext(out var runtime, out var activity))
		{
			return;
		}
		Callable callable = Callable.From(delegate
		{
			JavaObject javaObject2 = activity.Call("getWindow").AsGodotObject() as JavaObject;
			JavaObject javaObject3 = ((javaObject2 == null) ? null : (javaObject2.Call("getAttributes").AsGodotObject() as JavaObject));
			if (javaObject2 != null && javaObject3 != null)
			{
				javaObject3.Set("preferredDisplayModeId", 0);
				javaObject3.Set("preferredRefreshRate", (float)selected.RefreshRate);
				javaObject2.Call("setAttributes", javaObject3);
				JavaObject exception = JavaClassWrapper.Singleton.GetException();
				if (exception != null && !_androidFailureReported)
				{
					_androidFailureReported = true;
					GD.PushWarning($"Android display-mode request failed: {exception}");
				}
			}
		});
		if (runtime.Call("createRunnableFromGodotCallable", callable).AsGodotObject() is JavaObject javaObject)
		{
			activity.Call("runOnUiThread", javaObject);
		}
	}

	private static void ThrowIfJavaException(string operation)
	{
		JavaObject exception = JavaClassWrapper.Singleton.GetException();
		if (exception != null)
		{
			throw new InvalidOperationException($"Java exception during {operation}: {exception}");
		}
	}

	private void ApplyAndroidFallback(string message)
	{
		_androidModes.Clear();
		if (!_androidFailureReported)
		{
			_androidFailureReported = true;
			GD.PushWarning(message);
		}
		double num = DisplayServer.ScreenGetRefreshRate(DisplayServer.WindowGetCurrentScreen());
		SetDeviceLimit((num >= 1.0) ? FrameRatePolicy.NormalizeRefreshRate(num) : 60);
	}
}
