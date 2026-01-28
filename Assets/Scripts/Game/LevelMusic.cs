using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class LevelMusic : MonoBehaviour
{
    public static LevelMusic instance;

    private void Awake()
    {
        instance = this;
    }
    public List<AudioClip> musics;

    public void Start()
    {
        musics = musics
        .OrderByDescending(clip => clip.name)
        .ToList();
    }

    public void SetMusic(int levelNumber)
    {
        GetComponent<AudioSource>().clip = musics[(levelNumber - 1) % musics.Count];
    }
}
