using System.Threading.Tasks;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseBattleProcessEmpty : TowerDefenseBattleProcess
{
	public TowerDefenseBattleFeatureSeedBank seedBankFeature;

	public override void Init(Dictionary _data)
	{
		base.Init(_data);
	}

	private void ResolveDependencies()
	{
		seedBankFeature = GetFeature<TowerDefenseBattleFeatureSeedBank>("SeedBank");
	}

	public override Task GameInit()
	{
		ResolveDependencies();
		return Task.CompletedTask;
	}

	public override Task GameInitFromProgress()
	{
		ResolveDependencies();
		return Task.CompletedTask;
	}

	public override Task GameEntry()
	{
		if (!base.IsLifetimeActive || !GodotObject.IsInstanceValid(control))
		{
			return Task.CompletedTask;
		}
		if (GodotObject.IsInstanceValid(seedBankFeature?.seedBank))
		{
			seedBankFeature.seedBank.packetSlotContainer.Visible = true;
		}
		if (GameSaveManager.Instance.GetConfigValue("MobilePreset").AsBool())
		{
			control.uiTopAnimationPlayer.Play("MobileEnter");
		}
		else
		{
			control.uiTopAnimationPlayer.Play("Enter");
		}
		return Task.CompletedTask;
	}

	public override Task GameReady()
	{
		if (GodotObject.IsInstanceValid(seedBankFeature?.seedBank))
		{
			seedBankFeature.seedBank.ReadyPackets();
		}
		return Task.CompletedTask;
	}

	public override Task GameStart()
	{
		if (GodotObject.IsInstanceValid(seedBankFeature?.seedBank))
		{
			seedBankFeature.seedBank.StartFromProgress();
		}
		return Task.CompletedTask;
	}

	public override bool CheckFinal()
	{
		return false;
	}

	public override bool CheckFail()
	{
		return false;
	}

	public override void ZombieEnterHouse(TowerDefenseCharacter character)
	{
		if (GodotObject.IsInstanceValid(character))
		{
			character.Destroy();
		}
	}

	public override void Destroy()
	{
		base.Destroy();
		seedBankFeature = null;
	}
}
