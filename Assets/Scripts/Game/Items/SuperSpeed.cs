using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SuperSpeed : Powerups
{
    [SerializeField] float superSpeedMultiplier = 25f;
    public class OnUseSuperSpeed : UnityEvent { };
    public static OnUseSuperSpeed onUseSuperSpeed = new OnUseSuperSpeed();
    public static bool alreadyListened = false;

    public GameObject particle;

    GameObject psObj;

    public void Start()
    {
        psObj = null;
        if (!alreadyListened)
        {
            alreadyListened = true;
            onUseSuperSpeed.AddListener(UsePowerup);
        }
    }

    public void OnDisable()
    {
        alreadyListened = false;
        onUseSuperSpeed.RemoveAllListeners();
    }

    void FixedUpdate()
    {
        base.FixedUpdate();

        if (psObj != null)
            psObj.transform.position = Marble.instance.transform.position;
    }

    protected override void UsePowerup()
    {
        GameManager.instance.PlayAudioClip(useSound);

        psObj = Instantiate(particle);
        psObj.GetComponent<ParticleSystem>().Play();
        Destroy(psObj, psObj.GetComponent<ParticleSystem>().main.duration + 5f);

        Movement.instance.ApplySurfaceBoost(superSpeedMultiplier);
    }
}
