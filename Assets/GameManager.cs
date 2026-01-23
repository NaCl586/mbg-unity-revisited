using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public void Awake()
    {
        instance = this;
    }

    [Header("Level Objects")]
    public GameObject startPad;
    public GameObject finishPad;

    [Space]
    [Header("Audio Clips")]
    [SerializeField] AudioSource audioSource;
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
}
