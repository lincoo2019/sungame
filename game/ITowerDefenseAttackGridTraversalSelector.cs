public interface ITowerDefenseAttackGridTraversalSelector : ITowerDefenseCharacterRectSelector
{
	TowerDefenseCharacter SelectedMatch { get; }

	bool ShouldStopAfterCell { get; }

	void CompleteCell();
}
