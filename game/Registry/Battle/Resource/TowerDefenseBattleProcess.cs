using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseBattleProcess : TowerDefenseBattleComponentBase
{
	public virtual void InitManager()
	{
	}

	public virtual bool CheckFinal()
	{
		return false;
	}

	public virtual bool CheckFail()
	{
		return false;
	}

	public virtual void Finish()
	{
	}

	public virtual bool CanFinish()
	{
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return false;
		}
		if (GodotObject.IsInstanceValid(control))
		{
			return !control.HasPendingBattleOperations;
		}
		return false;
	}

	public bool TryFinish()
	{
		if (!CanFinish())
		{
			return false;
		}
		Finish();
		return true;
	}

	public virtual void PhysicsProcess(double delta)
	{
	}

	public virtual void InputProcess(InputEvent event_)
	{
	}

	public virtual void ViewMap()
	{
	}

	public virtual void GameFail(TowerDefenseCharacter enterCharacter)
	{
	}

	public virtual Dictionary SaveProcess()
	{
		return new Dictionary();
	}

	public virtual void LoadProcess(Dictionary _data, TowerDefenseLevelSaveConfigCSharp _owner)
	{
	}
}
