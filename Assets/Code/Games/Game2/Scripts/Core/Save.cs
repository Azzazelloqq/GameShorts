using UnityEngine;

namespace Code.Core.ShortGamesCore.Game2
{
    public static class Save
    {
        const string BestKey = "BoxTower_BestScore";
        
        public static int BestScore
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
