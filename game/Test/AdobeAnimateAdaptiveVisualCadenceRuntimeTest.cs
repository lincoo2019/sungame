using System;
using System.Collections.Generic;
using Godot;

public partial class AdobeAnimateAdaptiveVisualCadenceRuntimeTest : Node
{
	private readonly List<string> _failures = new List<string>();

	public override void _Ready()
	{
		Check(180, 0, 180.0);
		Check(180, 299, 180.0);
		Check(180, 300, 90.0);
		Check(180, 499, 90.0);
		Check(180, 500, 60.0);
		Check(180, 799, 60.0);
		Check(180, 800, 45.0);
		Check(180, 999, 45.0);
		Check(180, 1000, 30.0);
		Check(360, 0, 180.0);
		Check(144, 300, 90.0);
		Check(24, 1000, 24.0);
		bool flag = _failures.Count == 0;
		GD.Print($"ADOBE_ANIMATE_ADAPTIVE_CADENCE_RESULT passed={flag} checks=12 failures={_failures.Count}");
		for (int i = 0; i < _failures.Count; i++)
		{
			GD.PrintErr(_failures[i]);
		}
		GetTree().Quit((!flag) ? 1 : 0);
	}

	private void Check(int renderFrameRate, int characterCount, double expected)
	{
		double num = AdobeAnimateRuntimeManager.ResolveAdaptiveVisualTicksPerSecond(renderFrameRate, characterCount);
		if (!(Math.Abs(num - expected) <= 0.0001))
		{
			_failures.Add($"render={renderFrameRate} characters={characterCount} expected={expected:F2} actual={num:F2}");
		}
	}
}
