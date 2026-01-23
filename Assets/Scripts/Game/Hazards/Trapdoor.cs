using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trapdoor : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip trapdoorSfx;
    public float openTime = 3f;
    public float timeBeforeOpen = 0.5f;

    public Animator anim;

    bool isActive = false;

    private Marble marble;
    private float initXRot;

    public void OnCollisionWithMarble()
    {
        if (!isActive)
        {
            StartCoroutine(TrapdoorOpenClose());
        }
    }

    IEnumerator TrapdoorOpenClose()
    {
        isActive = true;

        yield return new WaitForSeconds(timeBeforeOpen);

        anim.SetTrigger("Open");
        audioSource.PlayOneShot(trapdoorSfx);

        yield return new WaitForSeconds(openTime + (50/30));

        anim.SetTrigger("Close");
        audioSource.PlayOneShot(trapdoorSfx);

        yield return new WaitForSeconds((50 / 30));
        isActive = false;
    }
}
