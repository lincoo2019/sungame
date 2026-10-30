using System;
using System.Reflection;
using Godot;

public partial class AdobeAnimateRenderColorTransactionRuntimeTest : Node
{
	private const string ResultMarker = "ADOBE_ANIMATE_RENDER_COLOR_TRANSACTION_RESULT";

	private static readonly MethodInfo EffectiveModulateMethod = typeof(AdobeAnimateSprite).GetMethod("GetEffectiveRenderModulate", BindingFlags.Instance | BindingFlags.NonPublic);

	public override void _Ready()
	{
		int exitCode = 2;
		Node2D node2D = null;
		bool flag = false;
		try
		{
			if (EffectiveModulateMethod == null)
			{
				throw new InvalidOperationException("GetEffectiveRenderModulate was not found.");
			}
			node2D = new Node2D
			{
				Modulate = new Color(0.11f, 0.13f, 0.17f, 0.19f)
			};
			Node2D node2D2 = new Node2D
			{
				Modulate = new Color(0.9f, 0.8f, 0.7f, 0.6f),
				SelfModulate = new Color(0.2f, 0.3f, 0.4f, 0.5f)
			};
			AdobeAnimateSprite adobeAnimateSprite = new AdobeAnimateSprite
			{
				Modulate = new Color(0.8f, 0.7f, 0.6f, 0.9f),
				SelfModulate = new Color(0.5f, 0.6f, 0.7f, 0.8f)
			};
			node2D.AddChild(node2D2, forceReadableName: false, InternalMode.Disabled);
			node2D2.AddChild(adobeAnimateSprite, forceReadableName: false, InternalMode.Disabled);
			Color expected = Multiply(Multiply(adobeAnimateSprite.Modulate, adobeAnimateSprite.SelfModulate), node2D2.Modulate);
			Color actual = ReadEffectiveModulate(adobeAnimateSprite, node2D);
			CheckColor(in actual, in expected, "initial direct probe");
			adobeAnimateSprite.Modulate = new Color(0.6f, 0.5f, 0.4f, 0.7f);
			adobeAnimateSprite.SelfModulate = new Color(0.7f, 0.8f, 0.9f, 0.6f);
			node2D2.Modulate = new Color(0.8f, 0.7f, 0.6f, 0.5f);
			Color expected2 = Multiply(Multiply(adobeAnimateSprite.Modulate, adobeAnimateSprite.SelfModulate), node2D2.Modulate);
			CheckColor(ReadEffectiveModulate(adobeAnimateSprite, node2D), in expected2, "updated direct probe");
			AdobeAnimateSprite.BeginViewportWorldRectRenderFrame(17L);
			flag = true;
			Color actual2 = ReadEffectiveModulate(adobeAnimateSprite, node2D);
			CheckColor(in actual2, in expected2, "first transaction sample");
			adobeAnimateSprite.Modulate = new Color(0.4f, 0.3f, 0.2f, 0.5f);
			adobeAnimateSprite.SelfModulate = new Color(0.9f, 0.7f, 0.5f, 0.4f);
			node2D2.Modulate = new Color(0.7f, 0.6f, 0.5f, 0.4f);
			CheckColor(ReadEffectiveModulate(adobeAnimateSprite, node2D), in actual2, "same transaction snapshot");
			AdobeAnimateSprite.EndViewportWorldRectRenderFrame();
			flag = false;
			Color expected3 = Multiply(Multiply(adobeAnimateSprite.Modulate, adobeAnimateSprite.SelfModulate), node2D2.Modulate);
			AdobeAnimateSprite.BeginViewportWorldRectRenderFrame(17L);
			flag = true;
			CheckColor(ReadEffectiveModulate(adobeAnimateSprite, node2D), in expected3, "next transaction with repeated frame version");
			AdobeAnimateSprite.EndViewportWorldRectRenderFrame();
			flag = false;
			adobeAnimateSprite.Modulate = new Color(0.3f, 0.4f, 0.5f, 0.6f);
			adobeAnimateSprite.SelfModulate = new Color(0.8f, 0.7f, 0.6f, 0.5f);
			node2D2.Modulate = new Color(0.6f, 0.5f, 0.4f, 0.3f);
			Color expected4 = Multiply(Multiply(adobeAnimateSprite.Modulate, adobeAnimateSprite.SelfModulate), node2D2.Modulate);
			CheckColor(ReadEffectiveModulate(adobeAnimateSprite, node2D), in expected4, "final direct probe");
			adobeAnimateSprite.SetRenderGrayscale(enabled: true);
			Color color = ReadEffectiveModulate(adobeAnimateSprite, node2D);
			Check(color.A < 0f && Mathf.IsEqualApprox(Mathf.Abs(color.A), expected4.A), "grayscale alpha sign encoding changed");
			adobeAnimateSprite.SetRenderGrayscale(enabled: false);
			GD.Print($"{"ADOBE_ANIMATE_RENDER_COLOR_TRANSACTION_RESULT"} passed=True initial={Format(in actual)} transaction={Format(in actual2)} final={Format(in expected4)}");
			exitCode = 0;
		}
		catch (Exception value)
		{
			GD.PrintErr($"{"ADOBE_ANIMATE_RENDER_COLOR_TRANSACTION_RESULT"} passed=False exception={value}");
		}
		finally
		{
			if (flag)
			{
				AdobeAnimateSprite.EndViewportWorldRectRenderFrame();
			}
			if (GodotObject.IsInstanceValid(node2D))
			{
				node2D.Free();
			}
		}
		GetTree().Quit(exitCode);
	}

	private static Color ReadEffectiveModulate(AdobeAnimateSprite sprite, Node renderMount)
	{
		try
		{
			return (Color)EffectiveModulateMethod.Invoke(sprite, new object[1] { renderMount });
		}
		catch (TargetInvocationException ex) when (ex.InnerException != null)
		{
			throw ex.InnerException;
		}
	}

	private static Color Multiply(in Color left, in Color right)
	{
		return new Color(left.R * right.R, left.G * right.G, left.B * right.B, left.A * right.A);
	}

	private static void CheckColor(in Color actual, in Color expected, string label)
	{
		Check(Mathf.IsEqualApprox(actual.R, expected.R) && Mathf.IsEqualApprox(actual.G, expected.G) && Mathf.IsEqualApprox(actual.B, expected.B) && Mathf.IsEqualApprox(actual.A, expected.A), $"{label}: expected {Format(in expected)}, got {Format(in actual)}");
	}

	private static void Check(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}

	private static string Format(in Color color)
	{
		return $"({color.R:F5},{color.G:F5},{color.B:F5},{color.A:F5})";
	}
}
