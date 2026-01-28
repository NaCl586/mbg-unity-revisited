using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LoadingManager : MonoBehaviour
{
    [Header("Loading")]
    public Slider loadingSlider;
    public TextMeshProUGUI loadingText;

    AsyncOperation op;
    bool cancelRequested;

    void Start()
    {
        loadingText.text = MissionInfo.instance.levelName;
        StartCoroutine(LoadAsync());
    }

    IEnumerator LoadAsync()
    {
        op = SceneManager.LoadSceneAsync("Game");
        op.allowSceneActivation = false;

        while (!op.isDone)
        {
            float progress = Mathf.Clamp01(op.progress / 0.9f);
            loadingSlider.value = progress;

            if (op.progress >= 0.9f)
            {
                loadingSlider.value = 1f;
                op.allowSceneActivation = true;
            }

            yield return null;
        }

    }

    public void CancelLoading()
    {
        SceneManager.LoadScene("PlayMission");
    }
}
