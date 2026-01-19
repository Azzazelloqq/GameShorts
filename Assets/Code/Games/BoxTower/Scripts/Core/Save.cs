using UnityEngine;

namespace Code.Core.ShortGamesCore.Game2
{
internal static class Save
{
    private const string BestKey = "BoxTower_BestScore";

    internal static int BestScore
    {
        get => PlayerPrefs.GetInt(BestKey, 0);
        set
        {
            if (value > BestScore)
            {
                PlayerPrefs.SetInt(BestKey, value);
                PlayerPrefs.Save();
            }
        }
    }
}
}