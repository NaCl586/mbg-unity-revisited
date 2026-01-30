using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SuperJump : Powerups
{
    [SerializeField] float superJumpHeight = 20f;
    public class OnUseSuperJump : UnityEvent { };
    public static OnUseSuperJump onUseSuperJump = new OnUseSuperJump();
    public static bool alreadyListened = false;
    [Space]
    public GameObject particle;

    GameObject psObj;

    public void Start()
    {
        psObj = null;
        if (!alreadyListened)
        {
            alreadyListened = true;
            onUseSuperJump.AddListener(UsePowerup);
        }
    }

    public void OnDisable()
    {
        alreadyListened = false;
        onUseSuperJump.RemoveAllListeners();
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
        Destroy(psObj, psObj.GetComponent<ParticleSystem>().main.duration + 1);

        Movement.instance.marbleVelocity += -GravitySystem.GravityDir.normalized * superJumpHeight;
    }
}
