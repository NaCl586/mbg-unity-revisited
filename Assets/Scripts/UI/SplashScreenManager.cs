using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class SplashScreenManager : MonoBehaviour
{
    public Image image;
    public Sprite production;
    public Sprite presents;
    public Sprite title;

    private void Update()
    {
        if(Input.GetKey(KeyCode.Mouse0))
            SceneManager.LoadScene("MainMenu");
    }

    public void Start()
    {
        image.sprite = presents;
        image.color = Color.clear;
        image.DOColor(Color.white, 0.5f).OnComplete(() => {
            DOVirtual.DelayedCall(2f, () =>
            {
                image.DOColor(Color.clear, 0.5f).OnComplete(() => {
                    image.sprite = production;
                    image.DOColor(Color.white, 0.5f).OnComplete(() => {
                        DOVirtual.DelayedCall(2f, () =>
                        {
                            image.DOColor(Color.clear, 0.5f).OnComplete(() => {
                                image.sprite = title;
                                image.DOColor(Color.white, 0.5f).OnComplete(() => {
                                    DOVirtual.DelayedCall(2f, () =>
                                    {
                                        image.DOColor(Color.clear, 0.5f).OnComplete(() => {
                                            SceneManager.LoadScene("MainMenu");
                                        });
                                    });
                                });
                            });
                        });
                    });
                });
            });
        });
    }
}
