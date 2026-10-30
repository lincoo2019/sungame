using Godot;

[GlobalClass]
public partial class BattleEventBus : Node
{
	public delegate void CharacterDestroyEventHandler(TowerDefensePacketConfig packet, Vector2 pos, Vector2 gridPos, TowerDefenseEnum.CHARACTER_CAMP camp, double scale, double hitpointScale);

	public delegate void CharacterSpawnedEventHandler(TowerDefenseCharacter character);

	public delegate void CharacterHurtEventHandler(TowerDefenseCharacter character, int damage, Node source);

	public delegate void ColdEffectEmitEventHandler();

	public delegate void BlowAllEffectEmitEventHandler();

	public delegate void BlowLineEffectEmitEventHandler(int line);

	public delegate void JalaLineEffectEmitEventHandler(int line);

	public delegate void JalaRowEffectEmitEventHandler(int row);

	public delegate void JalaGridEffectEmitEventHandler(Vector2I gridPos);

	public delegate void GameStartedEventHandler();

	public delegate void GameFailedEventHandler();

	public delegate void GameVictoryEventHandler();

	public delegate void GamePausedEventHandler(bool paused);

	public delegate void WaveStartedEventHandler(int waveIndex);

	public delegate void UiSwitchedEventHandler(bool shown);

	public delegate void CharacterSkinSwitchedEventHandler(string packetSaveKey, string customKey);

	public delegate void PacketUIFrontEventHandler(bool open);

	public delegate void ShowPlantHealthEventHandler(bool show);

	public delegate void ShowZombieHealthEventHandler(bool show);

	public delegate void ShowBossHealthBarEventHandler(bool show);

	public delegate void ScreenTransformChangedEventHandler();

	public static BattleEventBus Instance;

	public bool HasCharacterHurtSubscribers => this.OnCharacterHurt != null;

	public event CharacterDestroyEventHandler OnCharacterDestroy;

	public event CharacterSpawnedEventHandler OnCharacterSpawned;

	public event CharacterHurtEventHandler OnCharacterHurt;

	public event ColdEffectEmitEventHandler OnColdEffectEmit;

	public event BlowAllEffectEmitEventHandler OnBlowAllEffectEmit;

	public event BlowLineEffectEmitEventHandler OnBlowLineEffectEmit;

	public event JalaLineEffectEmitEventHandler OnJalaLineEffectEmit;

	public event JalaRowEffectEmitEventHandler OnJalaRowEffectEmit;

	public event JalaGridEffectEmitEventHandler OnJalaGridEffectEmit;

	public event GameStartedEventHandler OnGameStarted;

	public event GameFailedEventHandler OnGameFailed;

	public event GameVictoryEventHandler OnGameVictory;

	public event GamePausedEventHandler OnGamePaused;

	public event WaveStartedEventHandler OnWaveStarted;

	public event UiSwitchedEventHandler OnUiSwitched;

	public event CharacterSkinSwitchedEventHandler OnCharacterSkinSwitched;

	public event PacketUIFrontEventHandler OnPacketUIFront;

	public event ShowPlantHealthEventHandler OnShowPlantHealth;

	public event ShowZombieHealthEventHandler OnShowZombieHealth;

	public event ShowBossHealthBarEventHandler OnShowBossHealthBar;

	public event ScreenTransformChangedEventHandler OnScreenTransformChanged;

	public void EmitCharacterHurt(TowerDefenseCharacter character, int damage, Node source)
	{
		this.OnCharacterHurt?.Invoke(character, damage, source);
	}

	public void EmitCharacterDestroy(TowerDefensePacketConfig packet, Vector2 pos, Vector2 gridPos, TowerDefenseEnum.CHARACTER_CAMP camp, double scale, double hitpointScale)
	{
		this.OnCharacterDestroy?.Invoke(packet, pos, gridPos, camp, scale, hitpointScale);
	}

	public void EmitCharacterSpawned(TowerDefenseCharacter character)
	{
		this.OnCharacterSpawned?.Invoke(character);
	}

	public override void _Ready()
	{
		Instance = this;
		GetViewport().SizeChanged += OnViewportSizeChanged;
	}

	private void OnViewportSizeChanged()
	{
		this.OnScreenTransformChanged?.Invoke();
	}

	public void EmitColdEffectEmit()
	{
		this.OnColdEffectEmit?.Invoke();
	}

	public void EmitBlowAllEffectEmit()
	{
		this.OnBlowAllEffectEmit?.Invoke();
	}

	public void EmitBlowLineEffectEmit(int line)
	{
		this.OnBlowLineEffectEmit?.Invoke(line);
	}

	public void EmitJalaLineEffectEmit(int line)
	{
		this.OnJalaLineEffectEmit?.Invoke(line);
	}

	public void EmitJalaRowEffectEmit(int row)
	{
		this.OnJalaRowEffectEmit?.Invoke(row);
	}

	public void EmitJalaGridEffectEmit(Vector2I gridPos)
	{
		this.OnJalaGridEffectEmit?.Invoke(gridPos);
	}

	public void EmitGameStarted()
	{
		this.OnGameStarted?.Invoke();
	}

	public void EmitGameFailed()
	{
		this.OnGameFailed?.Invoke();
	}

	public void EmitGameVictory()
	{
		this.OnGameVictory?.Invoke();
	}

	public void EmitGamePaused(bool paused)
	{
		this.OnGamePaused?.Invoke(paused);
	}

	public void EmitWaveStarted(int waveIndex)
	{
		this.OnWaveStarted?.Invoke(waveIndex);
	}

	public void EmitUiSwitched(bool shown)
	{
		this.OnUiSwitched?.Invoke(shown);
	}

	public void EmitCharacterSkinSwitched(string packetSaveKey, string customKey)
	{
		this.OnCharacterSkinSwitched?.Invoke(packetSaveKey, customKey);
	}

	public void EmitPacketUIFront(bool open)
	{
		this.OnPacketUIFront?.Invoke(open);
	}

	public void EmitShowPlantHealth(bool show)
	{
		this.OnShowPlantHealth?.Invoke(show);
	}

	public void EmitShowZombieHealth(bool show)
	{
		this.OnShowZombieHealth?.Invoke(show);
	}

	public void EmitShowBossHealthBar(bool show)
	{
		this.OnShowBossHealthBar?.Invoke(show);
	}

	public void EmitScreenTransformChanged()
	{
		this.OnScreenTransformChanged?.Invoke();
	}

	public BattleEventBus()
	{
		StartupLoadDiagnostics.Mark("autoload.constructed/BattleEventBus");
	}
}
