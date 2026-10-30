using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;

public partial class CellAttackTargetRuntimeTest : Node
{
	private readonly List<TowerDefenseCharacter> _characters = new List<TowerDefenseCharacter>();

	private int _checks;

	private const int GroundMask = 1;

	public override void _Ready()
	{
		try
		{
			VerifySelection();
			Benchmark(1);
			Benchmark(25);
			Benchmark(50);
			GD.Print($"CELL_ATTACK_TARGET_RESULT passed=True checks={_checks}");
			GetTree().Quit();
		}
		catch (Exception ex)
		{
			GD.PrintErr(ex);
			GetTree().Quit(1);
		}
		finally
		{
			foreach (TowerDefenseCharacter character in _characters)
			{
				if (GodotObject.IsInstanceValid(character))
				{
					character.Free();
				}
			}
		}
	}

	private TowerDefenseCharacter CreateCharacter()
	{
		TowerDefensePlant towerDefensePlant = new TowerDefensePlant
		{
			camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT,
			HitBoxDefinition = new CharacterHitBoxDefinition
			{
				Size = new Vector2(40f, 40f)
			}
		};
		towerDefensePlant.instance = new TowerDefenseCharacterInstance
		{
			character = towerDefensePlant,
			maskFlags = 1,
			collisionFlags = 1,
			canBeCollection = true
		};
		_characters.Add(towerDefensePlant);
		return towerDefensePlant;
	}

	private void VerifySelection()
	{
		using TowerDefenseCellInstance towerDefenseCellInstance = new TowerDefenseCellInstance();
		TowerDefenseCharacter towerDefenseCharacter = CreateCharacter();
		TowerDefenseCharacter towerDefenseCharacter2 = CreateCharacter();
		TowerDefenseCharacter towerDefenseCharacter3 = CreateCharacter();
		TowerDefenseCharacter towerDefenseCharacter4 = CreateCharacter();
		towerDefenseCellInstance.characterList.Add(towerDefenseCharacter);
		towerDefenseCellInstance.characterList.Add(towerDefenseCharacter2);
		Expect(towerDefenseCellInstance, towerDefenseCharacter, "普通植物保持加入顺序");
		towerDefenseCharacter.instance.invincible = true;
		Expect(towerDefenseCellInstance, towerDefenseCharacter2, "同帧无敌切换立即生效");
		Check(towerDefenseCellInstance.GetTarget(1, TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE, checkInvincible: false) == towerDefenseCharacter, "关闭无敌过滤保持原有语义");
		towerDefenseCharacter.instance.invincible = false;
		towerDefenseCharacter.instance.canBeCollection = false;
		Expect(towerDefenseCellInstance, towerDefenseCharacter2, "不可选取目标立即跳过");
		towerDefenseCharacter.instance.canBeCollection = true;
		towerDefenseCharacter.camp = TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE;
		Expect(towerDefenseCellInstance, towerDefenseCharacter2, "同阵营目标不能被选中");
		towerDefenseCharacter.camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT;
		towerDefenseCharacter.instance.maskFlags = 0;
		Expect(towerDefenseCellInstance, towerDefenseCharacter2, "碰撞掩码变化立即生效");
		towerDefenseCharacter.instance.maskFlags = 1;
		towerDefenseCellInstance.characterSurround = towerDefenseCharacter4;
		Expect(towerDefenseCellInstance, towerDefenseCharacter4, "保护层优先于普通植物");
		Check(towerDefenseCellInstance.GetTarget(1, TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE, checkInvincible: true, isCataplut: true) == towerDefenseCharacter, "投掷检测保持保护层后置");
		towerDefenseCellInstance.characterSurround = null;
		towerDefenseCellInstance.characterSlotDictionary[towerDefenseCharacter3] = towerDefenseCharacter;
		towerDefenseCellInstance.slot[TowerDefenseEnum.PLANTGRIDTYPE.GROUND] = towerDefenseCharacter;
		Expect(towerDefenseCellInstance, towerDefenseCharacter, "承载层保持优先级");
		Check(towerDefenseCellInstance.FindSlotParent(towerDefenseCharacter) == towerDefenseCharacter3, "承载关系可反查");
		towerDefenseCharacter3.instance.invincible = true;
		Expect(towerDefenseCellInstance, towerDefenseCharacter2, "承载父级无敌同时过滤槽位和普通列表");
		towerDefenseCharacter3.instance.invincible = false;
		towerDefenseCellInstance.characterSlotDictionary[towerDefenseCharacter3] = towerDefenseCharacter2;
		Check(towerDefenseCellInstance.FindSlotParent(towerDefenseCharacter) == null && towerDefenseCellInstance.FindSlotParent(towerDefenseCharacter2) == towerDefenseCharacter3, "同帧更换承载关系立即生效");
		Expect(towerDefenseCellInstance, towerDefenseCharacter2, "新承载目标立即获得优先级");
		towerDefenseCellInstance.slot[TowerDefenseEnum.PLANTGRIDTYPE.AIR] = towerDefenseCharacter4;
		Check(towerDefenseCellInstance.GetTarget(1, TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE, checkInvincible: true, isCataplut: true) == towerDefenseCharacter4, "投掷检测优先选空中槽位");
		towerDefenseCellInstance.characterSlotDictionary.Clear();
		towerDefenseCellInstance.slot.Clear();
		towerDefenseCellInstance.characterList.Remove(towerDefenseCharacter);
		Expect(towerDefenseCellInstance, towerDefenseCharacter2, "移除植物立即切换目标");
		towerDefenseCharacter2.Free();
		towerDefenseCellInstance.dirty = true;
		Expect(towerDefenseCellInstance, null, "已释放对象清理后返回空目标");
		Check(towerDefenseCellInstance.characterList.Count == 0, "脏格子仍执行原有清理");
	}

	private void Benchmark(int plantCount)
	{
		using TowerDefenseCellInstance towerDefenseCellInstance = new TowerDefenseCellInstance();
		towerDefenseCellInstance.slot[TowerDefenseEnum.PLANTGRIDTYPE.GROUND] = null;
		towerDefenseCellInstance.slot[TowerDefenseEnum.PLANTGRIDTYPE.AIR] = null;
		for (int i = 0; i < plantCount; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = CreateCharacter();
			towerDefenseCellInstance.characterList.Add(towerDefenseCharacter);
			towerDefenseCellInstance.characterSlotDictionary[towerDefenseCharacter] = null;
		}
		for (int j = 0; j < 4; j++)
		{
			long allocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();
			long timestamp = Stopwatch.GetTimestamp();
			int num = 0;
			for (int k = 0; k < 200000; k++)
			{
				if (towerDefenseCellInstance.GetTarget(1, TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE) == towerDefenseCellInstance.characterList[0])
				{
					num++;
				}
			}
			GD.Print($"CELL_ATTACK_BENCH plants={plantCount} pass={j} calls=200000 matches={num} ms={Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds:F3} allocated={GC.GetAllocatedBytesForCurrentThread() - allocatedBytesForCurrentThread}");
			Check(num == 200000, "压力测试始终选择正确目标");
		}
	}

	private void Expect(TowerDefenseCellInstance cell, TowerDefenseCharacter expected, string message)
	{
		Check(cell.GetTarget(1, TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE) == expected, message);
	}

	private void Check(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
		_checks++;
	}
}
