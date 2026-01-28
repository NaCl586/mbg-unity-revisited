using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public Button playButton;
    public Button helpButton;
    public Button optionsButton;
    public Button quitButton;

    public void Start()
    {
        playButton.onClick.AddListener(() => SceneManager.LoadScene("PlayMission"));
        helpButton.onClick.AddListener(() => SceneManager.LoadScene("HelpCredits"));
        optionsButton.onClick.AddListener(() => SceneManager.LoadScene("Options"));
        quitButton.onClick.AddListener(() => Application.Quit());
    }
}
