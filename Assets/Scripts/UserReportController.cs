using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class UserReportController
{
    private List<SceneData> scenes = new List<SceneData>();

    public void levelInfo(int level, int collectedCoins, int curiosityRate, List<Vector3> coins)
    {
        scenes.Add(new SceneData(level, collectedCoins, curiosityRate, new List<Vector3>(coins)));
    }

    public void SaveIntoJson()
    {
        ScenesData levelsData = new ScenesData(scenes, 0);
        string data = JsonUtility.ToJson(levelsData);
        System.IO.File.WriteAllText(Application.dataPath + "/Resources/GameLevelData.json", data);
    }

}

[System.Serializable]
public class ScenesData
{
    public List<SceneData> levels = new List<SceneData>();
    public int userId;

    public ScenesData(List<SceneData> levels, int userId)
    {
        this.levels = levels;
        this.userId = userId;
    }
}

[System.Serializable]
public class SceneData
{
    public int level;
    public int collectedCoins;
    public int curiosityRate;
    public List<Vector3> coins = new List<Vector3>();
    
    public SceneData(int level, int collectedCoins, int curiosityRate, List<Vector3> coins)
    {
        this.level = level;
        this.collectedCoins = collectedCoins;
        this.curiosityRate = curiosityRate;
        this.coins = coins;
    }
}
