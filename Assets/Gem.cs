using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gem : MonoBehaviour
{
    [SerializeField] Texture[] gemColors;
    [SerializeField] SkinnedMeshRenderer smr;
    [SerializeField] AudioClip pickupSound;
    [SerializeField] AudioClip pickupLastGem;

    void Start()
    {
        smr.materials[0].mainTexture = gemColors[Random.Range(0, gemColors.Length)];
    }

    private void FixedUpdate()
    {
        var rot = transform.Find("Mesh").rotation;
        transform.Find("Mesh").rotation = Quaternion.AngleAxis(Time.fixedDeltaTime * 120f, rot * Vector3.up) * rot;
    }

    public void PickupItem()
    {
        GameManager.onCollectGem?.Invoke(GameManager.instance.currentGems + 1);

        if (GameManager.instance.CheckForAllGems())
            GameManager.instance.PlayAudioClip(pickupLastGem);
        else
            GameManager.instance.PlayAudioClip(pickupSound);

        gameObject.SetActive(false);
    }
}
