using UnityEngine;

public interface ILevelGeneratorStrategy
{
    LevelData GenerateLevel(Difficulty difficulty, int levelIndex, bool isTutorial = false);
}
