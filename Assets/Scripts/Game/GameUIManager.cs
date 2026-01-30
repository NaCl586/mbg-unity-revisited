using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager instance;
    void Awake()
    {
        instance = this;
        UpdateHUDMaterial();
    }

    [SerializeField] Sprite[] numbers;
    [SerializeField] Image[] timerNumbers;
    [SerializeField] TextMeshProUGUI centerText;
    [SerializeField] TextMeshProUGUI bottomText;
    [SerializeField] Texture[] powerupIcon;
    [SerializeField] RawImage powerupHUD;
    [SerializeField] Image[] targetGem;
    [SerializeField] Image[] currentGem;
    [SerializeField] GameObject gemCountUI;
    [Space]
    [SerializeField] GameObject readyImage;
    [SerializeField] GameObject setImage;
    [SerializeField] GameObject goImage;
    [SerializeField] GameObject outOfBoundsImage;

    Tween centerTextFade;
    Tween bottomTextFade;

    public void UpdateHUDMaterial()
    {
        int targetLayer = LayerMask.NameToLayer("HUD");
        float smoothness01 = 1f;

        GameObject[] allObjects = FindObjectsOfType<GameObject>();

        for (int i = 0; i < allObjects.Length; i++)
        {
            if (allObjects[i].layer != targetLayer)
                continue;

            Renderer[] renderers = allObjects[i].GetComponentsInChildren<Renderer>(true);

            for (int r = 0; r < renderers.Length; r++)
            {
                // This creates per-renderer material instances
                Material[] mats = renderers[r].materials;

                for (int m = 0; m < mats.Length; m++)
                {
                    Material mat = mats[m];
                    if (mat == null) continue;

                    if (mat.HasProperty("_Smoothness"))
                        mat.SetFloat("_Smoothness", smoothness01);

                    if (mat.HasProperty("_Glossiness"))
                        mat.SetFloat("_Glossiness", smoothness01);
                }
            }
        }
    }

    public void ShowGemCountUI(bool _show)
    {
        gemCountUI.SetActive(_show);
    }

    public void SetTargetGem(int _count)
    {
        targetGem[0].sprite = numbers[_count / 10];
        targetGem[1].sprite = numbers[_count % 10];
    }

    public void SetCurrentGem(int _count)
    {
        currentGem[0].sprite = numbers[_count / 10];
        currentGem[1].sprite = numbers[_count % 10];
    }

    public void SetPowerupIcon(PowerupType _powerUp)
    {
        switch (_powerUp)
        {
            case PowerupType.None:
                powerupHUD.texture = powerupIcon[0];
                break;
            case PowerupType.SuperJump:
                powerupHUD.texture = powerupIcon[1];
                break;
            case PowerupType.SuperSpeed:
                powerupHUD.texture = powerupIcon[2];
                break;
            case PowerupType.SuperBounce:
                powerupHUD.texture = powerupIcon[3];
                break;
            case PowerupType.ShockAbsorber:
                powerupHUD.texture = powerupIcon[4];
                break;
            case PowerupType.Gyrocopter:
                powerupHUD.texture = powerupIcon[5];
                break;
            default:
                powerupHUD.texture = powerupIcon[0];
                break;
        }
    }

    public void SetCenterText(string _text)
    {
        centerTextFade?.Kill();

        _text = Utils.Resolve(_text).Replace("\\", "");

        centerText.color = Color.white;
        centerText.text = _text;
        centerTextFade = centerText.DOColor(Color.white, 3f).OnComplete(() => { centerText.DOColor(Color.clear, 0.25f); });
    }

    public void SetBottomText(string _text)
    {
        bottomTextFade?.Kill();

        _text = Utils.Resolve(_text).Replace("\\", "");

        bottomText.color = Color.yellow;
        bottomText.text = _text;
        bottomTextFade = bottomText.DOColor(Color.yellow, 3f).OnComplete(() => { bottomText.DOColor(Color.clear, 0.25f); });
    }

    public void SetTimerText(float _timeMs)
    {
        int decaminutes = (int)(_timeMs / (10 * 60 * 1000));
        int remainder = (int)(_timeMs % (10 * 60 * 1000));

        int minutes = remainder / (60 * 1000);
        remainder %= 60 * 1000;

        int decaseconds = remainder / (10 * 1000);
        remainder %= 10 * 1000;

        int seconds = remainder / 1000;
        remainder %= 1000;

        int deciseconds = remainder / 100;
        remainder %= 100;

        int centiseconds = remainder / 10;
        int milliseconds = remainder % 10;

        timerNumbers[0].sprite = numbers[decaminutes];
        timerNumbers[1].sprite = numbers[minutes];
        timerNumbers[2].sprite = numbers[decaseconds];
        timerNumbers[3].sprite = numbers[seconds];
        timerNumbers[4].sprite = numbers[deciseconds];
        timerNumbers[5].sprite = numbers[centiseconds];
        timerNumbers[6].sprite = numbers[milliseconds];
    }

    public void SetCenterImage(int index)
    {
        readyImage.SetActive(false);
        setImage.SetActive(false);
        goImage.SetActive(false);
        outOfBoundsImage.SetActive(false);

        switch (index)
        {
            case 0: readyImage.SetActive(true); break;
            case 1: setImage.SetActive(true); break;
            case 2: goImage.SetActive(true); break;
            case 3: outOfBoundsImage.SetActive(true); break;
        }
    }
}
