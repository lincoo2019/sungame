using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Godot;

public partial class AnimationDynamicStateRuntimeTest : Node
{
	private const string ExpectedDigest = "F2B377E4FDCCE88CC94D61D2CBEAD4F8F6B5298AC7EEFC772A32EDB93654E1F8";

	public override void _Ready()
	{
		try
		{
			using IncrementalHash incrementalHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
			float[] array = new float[256];
			for (int i = 0; i < 1024; i++)
			{
				Array.Fill(array, -1234.5f);
				int num = i % 4;
				int num2 = i / 4 % 3;
				AdobeAnimateGpuGraphOwnerState ownerState = CreateState(i);
				if (!AdobeAnimateGpuGraphStateWriter.TryPatchOwnerDynamicState(array, num, num2, ownerState, (i & 0x10) != 0))
				{
					throw new InvalidOperationException("有效动态状态被拒绝。");
				}
				int num3 = (num + 4 + num2 * 10) * 4;
				if (array[num3 + 8] != -1234.5f || array[num3 + 14] != -1234.5f || array[num3 - 1] != -1234.5f || array[num3 + 40] != -1234.5f)
				{
					throw new InvalidOperationException("动态补丁覆盖了静态字段或相邻数据。");
				}
				incrementalHash.AppendData(MemoryMarshal.AsBytes(array.AsSpan()));
			}
			string text = Convert.ToHexString(incrementalHash.GetHashAndReset());
			GD.Print("ANIMATION_DYNAMIC_DIGEST " + text);
			if (text != "F2B377E4FDCCE88CC94D61D2CBEAD4F8F6B5298AC7EEFC772A32EDB93654E1F8")
			{
				throw new InvalidOperationException("动态编码与优化前逐位输出不一致。");
			}
			AdobeAnimateGpuGraphOwnerState ownerState2 = CreateState(27);
			if (AdobeAnimateGpuGraphStateWriter.TryPatchOwnerDynamicState(new float[55], 0, 0, ownerState2, useAbsoluteTransform: true) || AdobeAnimateGpuGraphStateWriter.TryPatchOwnerDynamicState(array, -1, 0, ownerState2, useAbsoluteTransform: true) || AdobeAnimateGpuGraphStateWriter.TryPatchOwnerDynamicState(array, 0, -1, ownerState2, useAbsoluteTransform: true) || AdobeAnimateGpuGraphStateWriter.TryPatchOwnerDynamicState(array, 2147483647, 0, ownerState2, useAbsoluteTransform: true) || AdobeAnimateGpuGraphStateWriter.TryPatchOwnerDynamicState(null, 0, 0, ownerState2, useAbsoluteTransform: true) || AdobeAnimateGpuGraphStateWriter.TryPatchOwnerDynamicState(array, 0, 0, null, useAbsoluteTransform: true))
			{
				throw new InvalidOperationException("无效缓冲或参数没有被拒绝。");
			}
			for (int j = 0; j < 4; j++)
			{
				long allocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();
				long timestamp = Stopwatch.GetTimestamp();
				for (int k = 0; k < 1000000; k++)
				{
					if (!AdobeAnimateGpuGraphStateWriter.TryPatchOwnerDynamicState(array, k & 3, (k >> 2) & 1, ownerState2, useAbsoluteTransform: true))
					{
						throw new InvalidOperationException("动态编码压力循环失败。");
					}
				}
				double totalMilliseconds = Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds;
				long value = GC.GetAllocatedBytesForCurrentThread() - allocatedBytesForCurrentThread;
				GD.Print($"ANIMATION_DYNAMIC_BENCH pass={j} calls=1000000 ms={totalMilliseconds:F3} allocated={value}");
			}
			GD.Print("ANIMATION_DYNAMIC_RESULT passed=True cases=1024 invalid=6");
			GetTree().Quit();
		}
		catch (Exception ex)
		{
			GD.PrintErr(ex);
			GetTree().Quit(1);
		}
	}

	private static AdobeAnimateGpuGraphOwnerState CreateState(int index)
	{
		float num = (float)index / 16f;
		return new AdobeAnimateGpuGraphOwnerState(null, new Transform2D(new Vector2(1f + num, 0f - num), new Vector2(num, -1f - num), new Vector2(num * 2f, 0f - num)), new Color(num, 0.25f, 0.75f, (float)(index % 5) / 4f), index % 100, (float)(index % 9) / 8f, (index & 1) != 0, new AdobeAnimateClipBlendState((index & 2) != 0, num - 2.5f, 0.75f), new AdobeAnimateGpuClockState((index & 4) != 0, num, 2f, 24f, 2f, 99f, (index & 8) != 0), new AdobeAnimateGpuHitFlashState(num, ((index & 0x20) != 0) ? 0.7f : 0f, 0.3f), new AdobeAnimateGpuHitFlashState(num + 1f, ((index & 0x40) != 0) ? 0.9f : 0f, 0.5f));
	}
}
