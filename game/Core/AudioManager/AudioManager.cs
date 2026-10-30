using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class AudioManager : Node
{
	public struct AudioRequest
	{
		public string Stream;

		public AudioManagerEnum.TYPE Type;

		public double Pos;

		public bool PauseAlive;
	}

	private const ulong SfxBatchPhysicsInterval = 4uL;

	public static AudioManager Instance;

	public Dictionary<AudioManagerEnum.TYPE, Dictionary<string, AudioStreamPlayerMember>> audioMemberDictionary = new Dictionary<AudioManagerEnum.TYPE, Dictionary<string, AudioStreamPlayerMember>>();

	public List<AudioRequest> audioStack = new List<AudioRequest>();

	private HashSet<string> _queuedSfxStreams = new HashSet<string>();

	private HashSet<string> _sfxPlayedList = new HashSet<string>();

	public ulong DuplicateSfxRejectCountForTest { get; private set; }

	public ulong AudioResourceLookupCountForTest { get; private set; }

	public override void _Ready()
	{
		Instance = this;
		audioMemberDictionary[AudioManagerEnum.TYPE.MUSIC] = new Dictionary<string, AudioStreamPlayerMember>();
		audioMemberDictionary[AudioManagerEnum.TYPE.SFX] = new Dictionary<string, AudioStreamPlayerMember>();
		SetupBuses();
		GameSaveManager instance = GameSaveManager.Instance;
		if (instance != null)
		{
			VolumSet(AudioManagerEnum.TYPE.MUSIC, (double)instance.GetConfigValue("MusicVolum"));
			VolumSet(AudioManagerEnum.TYPE.SFX, (double)instance.GetConfigValue("SfxVolum"));
		}
	}

	public override void _ExitTree()
	{
		ReleaseAudioPlayers(queueFree: false);
		if (Instance == this)
		{
			Instance = null;
		}
	}

	private void SetupBuses()
	{
		if (AudioServer.GetBusIndex("Music") == -1)
		{
			AudioServer.AddBus();
			AudioServer.SetBusName(AudioServer.BusCount - 1, "Music");
			AudioServer.SetBusSend(AudioServer.BusCount - 1, "Master");
		}
		if (AudioServer.GetBusIndex("SFX") == -1)
		{
			AudioServer.AddBus();
			AudioServer.SetBusName(AudioServer.BusCount - 1, "SFX");
			AudioServer.SetBusSend(AudioServer.BusCount - 1, "Master");
		}
	}

	private string _GetBusName(AudioManagerEnum.TYPE type)
	{
		return type switch
		{
			AudioManagerEnum.TYPE.MUSIC => "Music", 
			AudioManagerEnum.TYPE.SFX => "SFX", 
			_ => "Master", 
		};
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Engine.GetPhysicsFrames() % 4 != 0L || audioStack.Count <= 0)
		{
			return;
		}
		_sfxPlayedList.Clear();
		foreach (AudioRequest item in audioStack)
		{
			if (!_sfxPlayedList.Add(item.Stream))
			{
				continue;
			}
			AudioStreamPlayerMember audioStreamPlayerMember = MemberFind(item.Stream, item.Type);
			if (GodotObject.IsInstanceValid(audioStreamPlayerMember))
			{
				audioStreamPlayerMember.Play((float)item.Pos);
				if (item.PauseAlive)
				{
					audioStreamPlayerMember.ProcessMode = ProcessModeEnum.Always;
				}
				else
				{
					audioStreamPlayerMember.ProcessMode = ProcessModeEnum.Pausable;
				}
			}
		}
		audioStack.Clear();
		_queuedSfxStreams.Clear();
	}

	public AudioStreamPlayerMember MemberFind(string stream, AudioManagerEnum.TYPE type = AudioManagerEnum.TYPE.SFX)
	{
		if (string.IsNullOrEmpty(stream))
		{
			return null;
		}
		AudioStream audioStream = null;
		Dictionary<string, AudioStreamPlayerMember> dictionary = audioMemberDictionary[type];
		if (dictionary.TryGetValue(stream, out var value) && GodotObject.IsInstanceValid(value))
		{
			return value;
		}
		if (ResourceManager.Instance != null && ResourceManager.Instance.AUDIOS.ContainsKey(stream))
		{
			audioStream = (AudioStream)ResourceManager.Instance.AUDIOS[stream];
		}
		if (GodotObject.IsInstanceValid(audioStream))
		{
			AudioStreamPlayerMember audioStreamPlayerMember = new AudioStreamPlayerMember();
			audioStreamPlayerMember.type = type;
			audioStreamPlayerMember.Bus = _GetBusName(type);
			audioStreamPlayerMember.Stream = audioStream;
			audioStreamPlayerMember.ProcessMode = ProcessModeEnum.Pausable;
			if (type == AudioManagerEnum.TYPE.SFX)
			{
				audioStreamPlayerMember.MaxPolyphony = 10;
			}
			AddChild(audioStreamPlayerMember, forceReadableName: false, InternalMode.Disabled);
			dictionary[stream] = audioStreamPlayerMember;
			return audioStreamPlayerMember;
		}
		return null;
	}

	public void VolumSet(AudioManagerEnum.TYPE type = AudioManagerEnum.TYPE.SFX, double valum = 1.0)
	{
		int busIndex = AudioServer.GetBusIndex(_GetBusName(type));
		if (busIndex != -1)
		{
			AudioServer.SetBusVolumeDb(busIndex, (float)Mathf.LinearToDb(valum));
			AudioServer.SetBusMute(busIndex, valum <= 0.0);
		}
	}

	public double VolumGet(AudioManagerEnum.TYPE type = AudioManagerEnum.TYPE.SFX)
	{
		int busIndex = AudioServer.GetBusIndex(_GetBusName(type));
		if (busIndex != -1)
		{
			return Mathf.DbToLinear(AudioServer.GetBusVolumeDb(busIndex));
		}
		return 1.0;
	}

	public void AudioStopAll()
	{
		ReleaseAudioPlayers(queueFree: true);
	}

	private void ReleaseAudioPlayers(bool queueFree)
	{
		foreach (Node child in GetChildren())
		{
			if (child is AudioStreamPlayer audioStreamPlayer)
			{
				audioStreamPlayer.Stop();
				audioStreamPlayer.Stream = null;
			}
			if (queueFree && GodotObject.IsInstanceValid(child))
			{
				child.QueueFree();
			}
		}
		foreach (Dictionary<string, AudioStreamPlayerMember> value in audioMemberDictionary.Values)
		{
			value.Clear();
		}
		audioStack.Clear();
		_queuedSfxStreams.Clear();
		_sfxPlayedList.Clear();
	}

	public AudioStreamPlayerMember AudioPlay(string stream, AudioManagerEnum.TYPE type = AudioManagerEnum.TYPE.SFX, double pos = 0.0, bool once = true, bool pauseAlive = false)
	{
		if (string.IsNullOrEmpty(stream))
		{
			return null;
		}
		bool flag = false;
		if (type == AudioManagerEnum.TYPE.SFX)
		{
			if (!_queuedSfxStreams.Add(stream))
			{
				DuplicateSfxRejectCountForTest++;
				return null;
			}
			flag = true;
		}
		AudioResourceLookupCountForTest++;
		if (ResourceManager.Instance == null || !ResourceManager.Instance.AUDIOS.ContainsKey(stream))
		{
			if (flag)
			{
				_queuedSfxStreams.Remove(stream);
			}
			return null;
		}
		AudioStreamPlayerMember audioStreamPlayerMember = null;
		switch (type)
		{
		case AudioManagerEnum.TYPE.SFX:
			audioStack.Add(new AudioRequest
			{
				Stream = stream,
				Type = type,
				Pos = pos,
				PauseAlive = pauseAlive
			});
			break;
		case AudioManagerEnum.TYPE.MUSIC:
			audioStreamPlayerMember = MemberFind(stream, type);
			if (pauseAlive)
			{
				audioStreamPlayerMember.ProcessMode = ProcessModeEnum.Always;
			}
			else
			{
				audioStreamPlayerMember.ProcessMode = ProcessModeEnum.Pausable;
			}
			audioStreamPlayerMember.Play((float)pos);
			break;
		}
		return audioStreamPlayerMember;
	}

	public static AudioStreamPlayerMember _AudioPlay(string stream, AudioManagerEnum.TYPE type = AudioManagerEnum.TYPE.SFX, double pos = 0.0, bool once = true, bool pauseAlive = false)
	{
		if (Instance == null)
		{
			return null;
		}
		return Instance.AudioPlay(stream, type, pos, once, pauseAlive);
	}

	public AudioManager()
	{
		StartupLoadDiagnostics.Mark("autoload.constructed/AudioManager");
	}
}
