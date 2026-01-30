using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TS;
using TMPro;

[System.Serializable]
public class Mission
{
    public Sprite levelImage;
    public string directory;
    public int levelNumber;
    [Space]
    public int time;
    public string missionName;
    public string levelName;
    [TextArea(2, 10)] public string description;
    [TextArea(2, 10)] public string startHelpText;
    public string artist;
    public int goldTime;
    public int ultimateTime;
}

public enum Type 
{
    none,
    beginner,
    intermediate,
    advanced,
    custom
}

public class PlayMissionManager : MonoBehaviour
{
    public List<Mission> missions = new List<Mission>();

    [Header("UI References")]
    public Image levelImage;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI levelDescriptionText;
    public TextMeshProUGUI bestTimesText;
    public TextMeshProUGUI currentLevelText;
    public TextMeshProUGUI timeToQualifyText;
    public GameObject notQualifiedText;
    public GameObject notQualifiedImage;
    public GameObject beginnerButton;
    public GameObject intermediateButton;
    public GameObject advancedButton;
    public GameObject customButton;
    public GameObject[] spaces;
    public Button prev;
    public Button next;
    public Button play;
    public Button home;
    [Space]
    public bool debug = false;

    int selectedLevelNum;
    public static Type currentlySelectedType = Type.none;

    public void FixedUpdate()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow)) PrevButton();
        if (Input.GetKeyDown(KeyCode.RightArrow)) NextButton();
        if (Input.GetKeyDown(KeyCode.Escape)) SceneManager.LoadScene("MainMenu");
    }

    public void Start()
    {
        StartCoroutine(WaitUntilFinishLoading());
    }

    IEnumerator WaitUntilFinishLoading()
    {
        while (MissionInfo.instance.missionsBeginner == null || MissionInfo.instance.missionsBeginner.Count == 0)
            yield return null;

        Time.timeScale = 1;

        beginnerButton.GetComponent<Button>().onClick.AddListener(() => LoadMissions(Type.beginner));
        intermediateButton.GetComponent<Button>().onClick.AddListener(() => LoadMissions(Type.intermediate));
        advancedButton.GetComponent<Button>().onClick.AddListener(() => LoadMissions(Type.advanced));
        customButton.GetComponent<Button>().onClick.AddListener(() => LoadMissions(Type.custom));

        home.onClick.AddListener(() => SceneManager.LoadScene("MainMenu"));

        prev.onClick.AddListener(PrevButton);
        next.onClick.AddListener(NextButton);
        play.onClick.AddListener(() => SceneManager.LoadScene("Loading"));

        if (currentlySelectedType == Type.none)
            currentlySelectedType = Type.beginner;

        LoadMissions(currentlySelectedType);
    }

    void LoadMissions(Type difficulty)
    {
        beginnerButton.transform.SetAsFirstSibling();
        intermediateButton.transform.SetAsFirstSibling();
        advancedButton.transform.SetAsFirstSibling();
        customButton.transform.SetAsFirstSibling();

        if (difficulty == Type.beginner)
        {
            beginnerButton.transform.SetAsLastSibling();
            missions = MissionInfo.instance.missionsBeginner;
        }
        else if (difficulty == Type.intermediate)
        {
            intermediateButton.transform.SetAsLastSibling();
            missions = MissionInfo.instance.missionsIntermediate;
        }
        else if (difficulty == Type.advanced)
        {
            advancedButton.transform.SetAsLastSibling();
            missions = MissionInfo.instance.missionsAdvanced;
        }
        else if (difficulty == Type.custom)
        {
            customButton.transform.SetAsLastSibling();
            missions = MissionInfo.instance.missionsCustom;
        }

        currentlySelectedType = difficulty;

        int qualifiedLevel = PlayerPrefs.GetInt("QualifiedLevel" + CapitalizeFirst(currentlySelectedType.ToString()), 0);
        selectedLevelNum = PlayerPrefs.GetInt("SelectedLevel" + CapitalizeFirst(currentlySelectedType.ToString()), qualifiedLevel);
        if (selectedLevelNum < 0) selectedLevelNum = 0;
        if (selectedLevelNum >= missions.Count) selectedLevelNum = missions.Count - 1;

        SetLevelInfo(selectedLevelNum);
    }

    public void PrevButton()
    {
        next.interactable = true;
        selectedLevelNum--;
        if(selectedLevelNum <= 0)
        {
            selectedLevelNum = 0;
            prev.interactable = false;
        }
        SetLevelInfo(selectedLevelNum);
    }

    public void NextButton()
    {
        prev.interactable = true;
        selectedLevelNum++;
        if (selectedLevelNum >= missions.Count - 1)
        {
            selectedLevelNum = missions.Count - 1;
            next.interactable = false;
        }
        SetLevelInfo(selectedLevelNum);
    }

    public void SetLevelInfo(int number)
    {
        foreach (GameObject g in spaces)
            g.SetActive(missions.Count != 0);

        if(missions.Count == 0)
        {
            levelText.gameObject.SetActive(false);
            levelDescriptionText.gameObject.SetActive(false);

            levelImage.color = Color.clear;
            currentLevelText.text = "Level 0";

            notQualifiedImage.SetActive(true);
            notQualifiedText.SetActive(true);

            prev.interactable = false;
            next.interactable = false;
            play.interactable = false;

            bestTimesText.text = string.Empty;
            for (int i = 0; i < 3; i++)
            {
                string _name = "Nardo Polo";
                float _time = -1;
                bestTimesText.text += (i + 1) + ". " + _name;
                bestTimesText.text += "\t" + Utils.FormatTime(_time) + "\n";
            }

            return;
        }

        int qualifiedLevel = debug ? 9999 : PlayerPrefs.GetInt("QualifiedLevel" + CapitalizeFirst(currentlySelectedType.ToString()), 0);

        if (currentlySelectedType.ToString().ToLower().Equals("custom"))
            qualifiedLevel = 9999;

        play.interactable = (qualifiedLevel >= number);
        prev.interactable = true;
        next.interactable = true;

        int lastQualifiedLevel = number > qualifiedLevel ? qualifiedLevel : number;
        PlayerPrefs.SetInt("SelectedLevel" + CapitalizeFirst(currentlySelectedType.ToString()), lastQualifiedLevel);

        if (number >= missions.Count - 1)
            next.interactable = false;

        if (number <= 0)
            prev.interactable = false;

        levelText.gameObject.SetActive(true);
        levelDescriptionText.gameObject.SetActive(true);

        levelText.text = missions[number].levelName;
        levelDescriptionText.text = missions[number].description;

        if (missions[number].time != -1)
            timeToQualifyText.text = "Time to Qualify: " + Utils.FormatTime(missions[number].time);
        else
            timeToQualifyText.text = string.Empty;

        RefreshTMPLayout(levelText);
        RefreshTMPLayout(levelDescriptionText);
        RefreshTMPLayout(timeToQualifyText);

        levelImage.color = Color.white;

        if (missions[number].levelImage)
        {
            levelImage.sprite = missions[number].levelImage;
            levelImage.color = Color.white;
        }
        else
        {
            levelImage.sprite = null;
            levelImage.color = Color.clear;
        }

        currentLevelText.text = CapitalizeFirst(currentlySelectedType.ToString()) + " Level " + (number + 1);

        notQualifiedImage.SetActive(qualifiedLevel < number);
        notQualifiedText.SetActive(qualifiedLevel < number);

        MissionInfo.instance.MissionPath = missions[number].directory;
        MissionInfo.instance.missionName = missions[number].missionName;
        MissionInfo.instance.time = missions[number].time;
        MissionInfo.instance.levelName = missions[number].levelName;
        MissionInfo.instance.description = missions[number].description;
        MissionInfo.instance.startHelpText = missions[number].startHelpText;
        MissionInfo.instance.level = missions[number].levelNumber;
        MissionInfo.instance.artist = missions[number].artist;
        MissionInfo.instance.goldTime = missions[number].goldTime;
        MissionInfo.instance.ultimateTime = missions[number].ultimateTime;

        bestTimesText.text = string.Empty;
        for (int i = 0; i < 3; i++)
        {
            string _name = PlayerPrefs.GetString(MissionInfo.instance.levelName + "_Name_" + i, "Nardo Polo");
            float _time = PlayerPrefs.GetFloat(MissionInfo.instance.levelName + "_Time_" + i, -1);
            bestTimesText.text += (i+1) + ". " + _name;

            if (_time < MissionInfo.instance.goldTime && _time != -1)
                bestTimesText.text += "\t" + Utils.FormatTime(_time) + "<sprite name=\"gold\">\n";
            else
                bestTimesText.text += "\t" + Utils.FormatTime(_time) + "\n";
        }
    }

    public void RefreshTMPLayout(TextMeshProUGUI tmp)
    {
        tmp.ForceMeshUpdate();
        LayoutRebuilder.ForceRebuildLayoutImmediate(
            tmp.rectTransform
        );
    }

    public static string CapitalizeFirst(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return char.ToUpper(input[0]) + input.Substring(1);
    }
}
