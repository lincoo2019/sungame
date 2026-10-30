using System;
using System.Diagnostics;
using System.Reflection;
using Godot;

public partial class AnimationColorCacheRuntimeTest : Node
{
	private static readonly Func<CanvasItem, Color> ReadColor = typeof(AdobeAnimateSprite).GetMethod("GetCachedRenderFrameModulate", BindingFlags.Static | BindingFlags.NonPublic).CreateDelegate<Func<CanvasItem, Color>>();

	public override void _Ready()
	{
		Node2D[] array = new Node2D[1024];
		bool flag = false;
		try
		{
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = new Node2D
				{
					Modulate = new Color((float)i / 1024f, 0.5f, 0.25f, 0.75f)
				};
			}
			Color modulate = array[0].Modulate;
			AdobeAnimateSprite.BeginViewportWorldRectRenderFrame(23L);
			flag = true;
			if (ReadColor(array[0]) != modulate)
			{
				throw new InvalidOperationException("事务首次读取错误。");
			}
			array[0].Modulate = Colors.Red;
			ReadColor(array[1]);
			if (ReadColor(array[0]) != modulate || ReadColor(array[0]) != modulate)
			{
				throw new InvalidOperationException("字典命中或最近命中没有保持事务快照。");
			}
			AdobeAnimateSprite.EndViewportWorldRectRenderFrame();
			flag = false;
			if (ReadColor(array[0]) != Colors.Red)
			{
				throw new InvalidOperationException("事务外未读取即时颜色。");
			}
			AdobeAnimateSprite.BeginViewportWorldRectRenderFrame(23L);
			flag = true;
			if (ReadColor(array[0]) != Colors.Red)
			{
				throw new InvalidOperationException("重复帧号的新事务未刷新颜色。");
			}
			AdobeAnimateSprite.EndViewportWorldRectRenderFrame();
			flag = false;
			for (int j = 0; j < 4; j++)
			{
				long timestamp = Stopwatch.GetTimestamp();
				float num = 0f;
				for (int k = 0; k < 1000; k++)
				{
					AdobeAnimateSprite.BeginViewportWorldRectRenderFrame(k);
					flag = true;
					for (int l = 0; l < array.Length; l++)
					{
						num += ReadColor(array[l]).A;
					}
					AdobeAnimateSprite.EndViewportWorldRectRenderFrame();
					flag = false;
				}
				GD.Print($"ANIMATION_COLOR_BENCH pass={j} calls=1024000 ms={Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds:F3} alpha={num:F0}");
				if (num != 768250f)
				{
					throw new InvalidOperationException("压力循环累计颜色错误。");
				}
			}
			GD.Print("ANIMATION_COLOR_CACHE_RESULT passed=True");
			GetTree().Quit();
		}
		catch (Exception ex)
		{
			GD.PrintErr(ex);
			GetTree().Quit(1);
		}
		finally
		{
			if (flag)
			{
				AdobeAnimateSprite.EndViewportWorldRectRenderFrame();
			}
			Node2D[] array2 = array;
			foreach (Node2D node2D in array2)
			{
				if (GodotObject.IsInstanceValid(node2D))
				{
					node2D.Free();
				}
			}
		}
	}
}
