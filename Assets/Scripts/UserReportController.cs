using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

public class UserReportController
{
    private List<SceneData> scenes = new List<SceneData>();

    public void SceneInfo(SceneData currentScene)
    {
        scenes.Add(currentScene);
    }

    public void SaveIntoJson(string userId)
    {
        ScenesData levelsData = new ScenesData(scenes, userId);
        string data = JsonUtility.ToJson(levelsData);
        System.IO.File.WriteAllText(Application.persistentDataPath + "/user-" + userId + "-" + DateTimeOffset.Now.ToUnixTimeMilliseconds() + "-.json", data);
    }

}

[System.Serializable]
public class ScenesData
{
    public List<SceneData> scenes = new List<SceneData>();
    public string userId;

    public ScenesData(List<SceneData> scenes, string userId)
    {
        this.scenes = scenes;
        this.userId = userId;
    }
}

[System.Serializable]
public class SceneData
{
    public int isosurfaceCalculate;
    public int UIClick;
    public int leftHandButtonPress;
    public int rightHandButtonPress;
    public List<ParticleData> particlePositions;
    public List<InterestPointData> interestPointDuration;
    public float positiveParticleGrabTime;
    public float negativeParticleGrabTime;
    public List<Vector3> userPosition;
    public List<Quaternion> userRotation;
    public float sceneTime;

    public SceneData()
    {
        isosurfaceCalculate = 0;
        UIClick = 0;
        leftHandButtonPress = 0;
        rightHandButtonPress = 0;
        particlePositions = new List<ParticleData>();
        interestPointDuration = new List<InterestPointData>();
        positiveParticleGrabTime = 0.0f;
        negativeParticleGrabTime = 0.0f;
        userPosition = new List<Vector3>();
        userRotation = new List<Quaternion>();
        sceneTime = 0.0f;
    }

}

[System.Serializable]
public class ParticleData
{
    public bool isPositive;
    public List<Vector3> positions;

    public ParticleData(bool isPositive)
    {
        this.isPositive = isPositive;
        this.positions = new List<Vector3>();
    }
}


[System.Serializable]
public class InterestPointData
{
    public List<Vector3> positions;
    public float interactionTime;

    public InterestPointData(bool isPositive)
    {
        this.positions = new List<Vector3>();
        interactionTime = 0.0f;
    }
}


