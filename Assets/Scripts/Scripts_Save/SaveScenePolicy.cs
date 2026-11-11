using System;

public static class SaveScenePolicy
{
    public static bool IsAllowed(string sceneName, string officeSceneName, bool isEditor, bool allowTestScene)
    {
        if (!string.IsNullOrWhiteSpace(officeSceneName) &&
            string.Equals(sceneName, officeSceneName, StringComparison.Ordinal)) return true;
        return isEditor && allowTestScene &&
            string.Equals(sceneName, "PlayerMonsterTest", StringComparison.Ordinal);
    }
}
