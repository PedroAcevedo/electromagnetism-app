using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RaycastingHand : MonoBehaviour
{
    public bool LeftHander = false;

    public void GoToSimulation()
    {
        int left = 0;

        if (LeftHander)
        {
            left = 1;
        }

        PlayerPrefs.SetInt("LeftHander", left);
        SceneManager.LoadScene("GameScene");
    }
}
