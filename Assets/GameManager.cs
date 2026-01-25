using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public void Awake()
    {
        instance = this;

        onFinish.AddListener(Finish);
        onOutOfBounds.AddListener(OutOfBounds);
        onCollectGem.AddListener(UpdateGem);

        Marble.onRespawn.AddListener(Respawn);
    }

    [Header("Mission Info")]
    public int time;
    public string missionName;
    public string levelName;
    [TextArea(2, 10)] public string description;
    [TextArea(2, 10)] public string startHelpText;
    public int level;
    public string artist;
    public int goldTime;
    public int ultimateTime;

    [Header("Level Objects")]
    public GameObject startPad;
    public GameObject finishPad;

    [Space]
    [Header("Audio Clips")]
    [SerializeField] AudioClip jump;
    [SerializeField] AudioClip puSpawn;
    [SerializeField] AudioClip puReady;
    [SerializeField] AudioClip puSet;
    [SerializeField] AudioClip puGo;
    [SerializeField] AudioClip puFinish;
    [SerializeField] AudioClip puOutOfBounds;
    [SerializeField] AudioClip puHelp;
    [SerializeField] AudioClip puMissingGems;

    public void PlayJumpAudio() => audioSource.PlayOneShot(jump);
    public void PlaySpawnAudio() => audioSource.PlayOneShot(puSpawn);
    public void PlayReadyAudio() => audioSource.PlayOneShot(puReady);
    public void PlaySetAudio() => audioSource.PlayOneShot(puSet);
    public void PlayGoAudio() => audioSource.PlayOneShot(puGo);
    public void PlayFinishAudio() => audioSource.PlayOneShot(puFinish);
    public void PlayOutOfBoundsAudio() => audioSource.PlayOneShot(puOutOfBounds);
    public void PlayHelpAudio() => audioSource.PlayOneShot(puHelp);
    public void PlayMissingGemAudio() => audioSource.PlayOneShot(puMissingGems);
    public void PlayAudioClip(AudioClip _ac) => audioSource.PlayOneShot(_ac);

    [Space]
    [Header("UI Menu")]
    [SerializeField] GameObject pauseMenu;
    [SerializeField] GameObject finishMenu;
    [SerializeField] GameObject enterNameMenu;
    [SerializeField] TextMeshProUGUI finalTime;
    [SerializeField] TextMeshProUGUI finishCaption;
    [SerializeField] TextMeshProUGUI rightCaption;
    [SerializeField] TextMeshProUGUI namesCaption;
    [SerializeField] TextMeshProUGUI timesCaption;
    [SerializeField] TextMeshProUGUI enterNameCaption;
    [SerializeField] Button replayButton;
    [SerializeField] Button continueButton;
    [SerializeField] Button noButton;
    [SerializeField] Button yesButton;
    [SerializeField] Button restartButton;
    [SerializeField] Button okayButton;
    [SerializeField] TMP_InputField nameInputField;

    [HideInInspector] public GameObject activeCheckpoint;
    bool useCheckpoint;

    [Space]
    [SerializeField] AudioSource audioSource;

    bool startTimer;
    [HideInInspector] public bool timeTravelActive;
    float elapsedTime;
    float bonusTime;
    string bestTimeName = string.Empty;

    int totalGems;
    [HideInInspector] public int currentGems;
    Gem[] gems;

    [HideInInspector] public PowerupType activePowerup;
    [HideInInspector] public bool superBounceIsActive = false;
    [HideInInspector] public bool shockAbsorberIsActive = false;
    [HideInInspector] public bool gyrocopterIsActive = false;
    [HideInInspector] public float sbsaActiveTime;
    [HideInInspector] public float gyroActiveTime;
    [HideInInspector] public float timeTravelStartTime;
    [HideInInspector] public float timeTravelBonus;

    [Header("Particles")]
    public GameObject finishParticles;
    GameObject finishParticleInstance;

    //checkpoint properties
    PowerupType tempPowerup;

    //game state
    [Space]
    [HideInInspector] public static bool gameFinish = false;
    [HideInInspector] public static bool gameStart = false;
    [HideInInspector] public static bool isPaused = false;
    [HideInInspector] public static bool cannotReset = false;
    [HideInInspector] public static bool outOfBoundsState = false;

    //events
    public class OnFinish : UnityEvent { };
    public static OnFinish onFinish = new OnFinish();
    public class OnOutOfBounds : UnityEvent { };
    public static OnOutOfBounds onOutOfBounds = new OnOutOfBounds();
    public class OnCollectGem : UnityEvent<int> { };
    public static OnCollectGem onCollectGem = new OnCollectGem();

    void Start()
    {
        startTimer = false;
        timeTravelActive = false;
        activePowerup = PowerupType.None;

        //disable cursor visibility
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        //disable UI
        finishMenu.SetActive(false);
        pauseMenu.SetActive(false);

        okayButton.onClick.AddListener(CloseEnterNameWindow);
        replayButton.onClick.AddListener(ReplayLevel);
        continueButton.onClick.AddListener(ReturnToMenu);
        noButton.onClick.AddListener(TogglePause);
        yesButton.onClick.AddListener(ReturnToMenu);
        restartButton.onClick.AddListener(RestartLevel);

        nameInputField.onEndEdit.AddListener(UpdateName);
    }

    public void InitGemCount()
    {
        gems = FindObjectsOfType<Gem>();

        totalGems = gems.Length;
        if (totalGems != 0)
            GameUIManager.instance.SetTargetGem(totalGems);
        else
            GameUIManager.instance.ShowGemCountUI(false);
    }

    #region Game
    public PowerupType ConsumePowerup()
    {
        PowerupType powerup = activePowerup;
        activePowerup = PowerupType.None;

        GameUIManager.instance.SetPowerupIcon(activePowerup);

        return powerup;
    }

    private void Update()
    {
        //Handle Timer
        if (startTimer && !timeTravelActive)
        {
            elapsedTime += Time.deltaTime * 1000f;
            elapsedTime = Mathf.RoundToInt(elapsedTime);
            GameUIManager.instance.SetTimerText(elapsedTime);
        }

        //Handle Shock Absorber and Super Bounce timer
        if (shockAbsorberIsActive || superBounceIsActive)
        {
            if (Time.time - sbsaActiveTime > 5f)
                Marble.instance.RevertMaterial();
        }

        //Handle Gyrocopter Timer
        if (gyrocopterIsActive)
        {
            if (Time.time - gyroActiveTime > 5f)
                Marble.instance.CancelGyrocopter();
        }

        //Handle Time travel timer
        if (timeTravelActive)
        {
            bonusTime += Time.deltaTime * 1000f;
            if (Time.time - timeTravelStartTime >= timeTravelBonus)
                Marble.instance.InactivateTimeTravel();
        }

        //pause
        if (Input.GetKeyDown(KeyCode.Escape) && !gameFinish)
            TogglePause();
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        if (isPaused)
        {
            Time.timeScale = 0f;
            pauseMenu.SetActive(true);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Time.timeScale = 1f;
            pauseMenu.SetActive(false);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public bool CheckForAllGems() => (totalGems == currentGems);

    void UpdateGem(int _count)
    {
        if (totalGems == 0) return;

        //negative symbol means no center text message
        currentGems = Mathf.Abs(_count);

        GameUIManager.instance.SetCurrentGem(currentGems);

        string remainingGemMsg;

        if (currentGems + 1 == totalGems) remainingGemMsg = "You picked up a gem! Only one more gem to go!";
        else if (currentGems == totalGems) remainingGemMsg = "You picked up all gems! Head for the finish!";
        else remainingGemMsg = "You picked up a gem! " + (totalGems - currentGems) + " gems to go!";

        if (_count > 0)
            GameUIManager.instance.SetBottomText(remainingGemMsg);
    }

    void OutOfBounds()
    {
        GameUIManager.instance.SetCenterImage(3);
        PlayOutOfBoundsAudio();
        CameraController.instance.LockCamera(false);

        CancelInvoke();
        Invoke(nameof(InvokeRespawn), 2f);
    }

    public void InvokeRespawn() => Marble.onRespawn?.Invoke();

    public void RestartLevel()
    {
        TogglePause();
        Marble.onRespawn?.Invoke();
    }

    public void ReplayLevel()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        finishMenu.SetActive(false);
        Marble.onRespawn?.Invoke();
    }

    public void Respawn()
    {
        PlaySpawnAudio();

        CancelInvoke();
        gameFinish = false;
        Movement.instance.freezeMovement = false;

        GravityModifier.onResetGravity?.Invoke();
        CameraController.instance?.ResetCam();

        CameraController.instance.LockCamera(true);

        if (!useCheckpoint)
        {
            Movement.instance.StopAllMovement();
            Movement.instance.StopAllbutJumping();
            GameStateStart();
        }
        else
        {
            Movement.instance.StopAllMovement();
            Movement.instance.StartMoving();
        }

        /*Marble.instance.RevertMaterial();
        Marble.instance.ToggleGyrocopterBlades(false);
        if (gyrocopterIsActive)
            Marble.instance.CancelGyrocopter();
        Marble.instance.InactivateTimeTravel();*/
    }


    void GameStateStart()
    {
        startTimer = false;
        UpdateGem(0);
        elapsedTime = bonusTime = 0;

        foreach (Gem gem in gems)
            gem.gameObject.SetActive(true);

        ConsumePowerup();

        //reset powerups
        foreach (Powerups po in FindObjectsOfType<Powerups>())
            po.Activate(false);

        //reset moving platforms
        foreach (MovingPlatform mp in FindObjectsOfType<MovingPlatform>())
            mp.ResetMP();

        GameUIManager.instance.SetTimerText(elapsedTime);
        GameUIManager.instance.SetCenterText(startHelpText);

        if (finishParticleInstance)
            Destroy(finishParticleInstance);

        gameStart = false;
        GameUIManager.instance.SetCenterImage(-1);
        Invoke(nameof(GameStateReady), 0.5f);
    }

    void GameStateReady()
    {
        PlayReadyAudio();
        GameUIManager.instance.SetCenterImage(0);
        Invoke(nameof(GameStateSet), 1.5f);
    }
    void GameStateSet()
    {
        PlaySetAudio();
        GameUIManager.instance.SetCenterImage(1);
        Invoke(nameof(GameStateGo), 1.5f);
    }

    void GameStateGo()
    {
        PlayGoAudio();

        startTimer = true;
        gameStart = true;

        GameUIManager.instance.SetCenterImage(2);
        Movement.instance.StartMoving();
        Invoke(nameof(ClearCenterImage), 2f);
    }

    void ClearCenterImage()
    {
        GameUIManager.instance.SetCenterImage(-1);
    }

    void Finish()
    {
        //Missing gems
        if (totalGems != 0 && totalGems != currentGems)
        {
            GameUIManager.instance.SetBottomText("You can't finish without all gems!");

            PlayMissingGemAudio();
        }
        //Finish
        else
        {
            PlayFinishAudio();

            startTimer = false;
            GameUIManager.instance.SetBottomText("Congratulations! You've finished!");

            finishParticleInstance = Instantiate(finishParticles, finishPad.transform.GetChild(0).position, Quaternion.Euler(-90, 0, 0));

            Marble.instance.InactivateTimeTravel();

            gameFinish = true;
            CameraController.onCameraFinish?.Invoke();
            Invoke(nameof(StopMarbleMovement), 0.125f);
            Invoke(nameof(ShowFinishUI), 2f);
        }

    }
    void StopMarbleMovement()
    {
        Movement.instance.freezeMovement = true;
        Movement.instance.StopMoving();
    }
#endregion

#region UI
    public void ReturnToMenu()
    {

    }

    public void ShowFinishUI()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        finishMenu.SetActive(true);
        GenerateFinishUIText();
    }

    public void UpdateName(string s)
    {
        bestTimeName = s;
    }

    public void CloseEnterNameWindow()
    {
        enterNameMenu.SetActive(false);
        replayButton.interactable = true;
        continueButton.interactable = true;

        InsertBestTime(bestTimeName, elapsedTime);
        UpdateBestTimes();
    }

    public void GenerateFinishUIText()
    {
        replayButton.interactable = true;
        continueButton.interactable = true;


        bool gold = elapsedTime < goldTime;
        bool qualify = !(time != -1 && elapsedTime >= time);
        finalTime.text = Utils.FormatTime(elapsedTime);

        int pos = DeterminePosition(elapsedTime);
        if (pos != -1 && qualify)
        {
            replayButton.interactable = false;
            continueButton.interactable = false;
            enterNameMenu.SetActive(true);
            if (pos == 0)
                enterNameCaption.text = "You got the best time!";
            else if (pos == 1)
                enterNameCaption.text = "You got the 2nd best time!";
            else if (pos == 2)
                enterNameCaption.text = "You got the 3rd best time!";
        }

        if (gold && qualify)
            finishCaption.text = "You beat the <color=yellow>GOLD</color> time!";
        else if (qualify)
            finishCaption.text = "You've qualified!";
        else
            finishCaption.text = "<color=red>You failed to qualify!</color>";

        string _qualifyTime, _goldTime;
        if (!qualify)
            _qualifyTime = "<color=red>" + Utils.FormatTime(time) + "</color>";
        else
            _qualifyTime = Utils.FormatTime(time);

        _goldTime = "<color=yellow>" + Utils.FormatTime(goldTime) + "</color>";

        rightCaption.text = _qualifyTime + "\n" +
                            _goldTime + "\n" +
                            Utils.FormatTime(elapsedTime + bonusTime) + "\n" +
                            Utils.FormatTime(bonusTime);

        UpdateBestTimes();
    }

    void UpdateBestTimes()
    {
        namesCaption.text = string.Empty;
        timesCaption.text = string.Empty;

        for (int i = 0; i < 3; i++)
        {
            string _name = PlayerPrefs.GetString(levelName + "_Name_" + i, "Nardo Polo");
            float _time = PlayerPrefs.GetFloat(levelName + "_Time_" + i, -1);
            namesCaption.text += _name + "\n";

            if (_time < goldTime && _time != -1)
                timesCaption.text += "<color=yellow>" + Utils.FormatTime(_time) + "</color>" + "\n";
            else
                timesCaption.text += Utils.FormatTime(_time) + "\n";
        }
    }

    int DeterminePosition(float time)
    {
        float[] times = new float[3];
        for (int i = 0; i < 3; i++)
            times[i] = PlayerPrefs.GetFloat(levelName + "_Time_" + i, -1);

        if (times[0] == -1 || time < times[0]) return 0;
        else if (times[1] == -1 || (time < times[1] && time >= times[0])) return 1;
        else if (times[2] == -1 || (time < times[2] && time >= times[1])) return 2;
        else return -1;
    }

    void InsertBestTime(string _name, float _time)
    {
        int pos = DeterminePosition(_time);
        if (pos == -1) return;

        for (int i = 1; i >= pos; i--)
        {
            string playerName = PlayerPrefs.GetString(levelName + "_Name_" + i, "Nardo Polo");
            float playerTime = PlayerPrefs.GetFloat(levelName + "_Time_" + i, -1);
            PlayerPrefs.SetString(levelName + "_Name_" + (i + 1), playerName);
            PlayerPrefs.SetFloat(levelName + "_Time_" + (i + 1), playerTime);
        }

        PlayerPrefs.SetString(levelName + "_Name_" + pos, _name);
        PlayerPrefs.SetFloat(levelName + "_Time_" + pos, _time);
    }
    #endregion
}
