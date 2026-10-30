public interface ITowerDefenseCharacterRectSelector
{
	bool CanConsider(TowerDefenseCharacter character);

	bool Visit(TowerDefenseCharacter character, ref TowerDefenseCharacter match);
}
