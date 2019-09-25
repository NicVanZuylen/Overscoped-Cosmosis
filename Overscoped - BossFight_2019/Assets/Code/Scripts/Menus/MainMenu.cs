using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    private void Awake()
    {
        
    }

    private void Update()
    {
        
    }

    public void PlayButtonCallback()
    {
        SceneManager.LoadScene(1); // Scene at build index 1 should be the game scene.
    }

    public void QuitButtonCallback()
    {
        Application.Quit();
    }
}
