using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseBattleFeatureBGM : TowerDefenseBattleFeature
{
	public AudioStreamPlayerMember backgroundAudio;

	public AudioStreamPlayerMember backgroundDrumsAudio;

	public TowerDefenseBackgroundMusicConfig backgroundMusicConfig;

	private double _drumsCheckTimer;

	private float _drumsTargetVolume;

	public override void Init(Dictionary _data)
	{
		base.Init(_data);
		string text = data.GetValueOrDefault("BackgroundMusic", data.GetValueOrDefault("BGMName", "")).AsString();
		if (text != "")
		{
			backgroundMusicConfig = TowerDefenseManager.Instance.GetbackgroundMusicConfig(text);
		}
	}

	public override void Process(double delta)
	{
		if (!GodotObject.IsInstanceValid(backgroundDrumsAudio))
		{
			return;
		}
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (GodotObject.IsInstanceValid(instance) && GodotObject.IsInstanceValid(instance.characterRegistry))
		{
			_drumsCheckTimer -= Math.Max(0.0, delta);
			if (_drumsCheckTimer <= 0.0)
			{
				int num = Math.Max(0, backgroundMusicConfig?.drumsZombieThreshold ?? 10);
				_drumsTargetVolume = ((instance.characterRegistry.GetZombieCount() >= num) ? 1f : 0f);
				_drumsCheckTimer = Mathf.Max(0.05f, backgroundMusicConfig?.drumsCheckInterval ?? 0.25f);
			}
			float num2 = (float)backgroundDrumsAudio.volumeScale;
			if (Mathf.Abs(num2 - _drumsTargetVolume) <= 0.001f)
			{
				backgroundDrumsAudio.volumeScale = _drumsTargetVolume;
				return;
			}
			float num3 = Mathf.Max(0.01f, backgroundMusicConfig?.drumsFadeSpeed ?? 1f);
			float weight = 1f - Mathf.Exp((0f - num3) * (float)Math.Max(0.0, delta));
			backgroundDrumsAudio.volumeScale = Mathf.Lerp(num2, _drumsTargetVolume, weight);
		}
	}

	public override Task GameEntry()
	{
		PlayEntryBGM();
		return Task.CompletedTask;
	}

	public override Task GameStart()
	{
		StopBGM();
		_drumsCheckTimer = 0.0;
		_drumsTargetVolume = 0f;
		if (GodotObject.IsInstanceValid(backgroundMusicConfig))
		{
			if (!string.IsNullOrWhiteSpace(backgroundMusicConfig.flag1))
			{
				backgroundAudio = AudioManager.Instance.AudioPlay(backgroundMusicConfig.flag1, AudioManagerEnum.TYPE.MUSIC, 0.0, once: false);
			}
			if (!string.IsNullOrWhiteSpace(backgroundMusicConfig.drums))
			{
				backgroundDrumsAudio = AudioManager.Instance.AudioPlay(backgroundMusicConfig.drums, AudioManagerEnum.TYPE.MUSIC, 0.0, once: false);
				if (GodotObject.IsInstanceValid(backgroundDrumsAudio))
				{
					backgroundDrumsAudio.volumeScale = 0.0;
				}
			}
		}
		return Task.CompletedTask;
	}

	public override Task GameReady()
	{
		StopBGM();
		return Task.CompletedTask;
	}

	public void PlayEntryBGM()
	{
		if (GodotObject.IsInstanceValid(backgroundMusicConfig))
		{
			if (GodotObject.IsInstanceValid(backgroundAudio))
			{
				backgroundAudio.Stop();
			}
			if (GodotObject.IsInstanceValid(backgroundDrumsAudio))
			{
				backgroundDrumsAudio.Stop();
			}
			if (!string.IsNullOrWhiteSpace(backgroundMusicConfig.entry))
			{
				backgroundAudio = AudioManager.Instance.AudioPlay(backgroundMusicConfig.entry, AudioManagerEnum.TYPE.MUSIC, 0.0, once: false);
			}
		}
	}

	public void StopBGM()
	{
		if (GodotObject.IsInstanceValid(backgroundAudio))
		{
			backgroundAudio.Stop();
		}
		if (GodotObject.IsInstanceValid(backgroundDrumsAudio))
		{
			backgroundDrumsAudio.Stop();
		}
	}

	public override void GameFail()
	{
		StopBGM();
	}

	public override void Destroy()
	{
		StopBGM();
		backgroundAudio = null;
		backgroundDrumsAudio = null;
		backgroundMusicConfig = null;
		_drumsCheckTimer = 0.0;
		_drumsTargetVolume = 0f;
		base.Destroy();
	}
}
