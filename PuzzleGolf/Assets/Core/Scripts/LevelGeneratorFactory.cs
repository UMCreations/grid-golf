using UnityEngine;

public static class LevelGeneratorFactory
{
    public static ILevelGeneratorStrategy GetStrategy(bool useAdvanced)
    {
        if (useAdvanced)
        {
            return new AdvancedLevelGeneratorStrategy();
        }
        
        return new ClassicLevelGeneratorStrategy();
    }
}
