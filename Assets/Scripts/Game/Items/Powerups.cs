using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;

public enum PowerupType
{
    None,
    SuperJump,
    SuperSpeed,
    SuperBounce,
    ShockAbsorber,
    MegaMarble,
    MiniMarble,
    TimeTravel,
    AntiGravity,
    Gyrocopter
}

public class Powerups : MonoBehaviour
{
    [SerializeField] PowerupType powerupType;
    [SerializeField] string powerupName;
    [SerializeField] bool autoUse;
    [SerializeField] float respawnTime = 7f;
    [SerializeField] protected AudioClip pickupSound;
    [SerializeField] protected AudioClip useSound;

    [Space]
    [SerializeField] MeshRenderer meshRenderer;
    [SerializeField] SkinnedMeshRenderer skinnedMeshRenderer;

    [Space]
    [SerializeField] protected Animator anim;

    [HideInInspector] public bool isActive = true;
    private float timeDeactivated;
    protected string bottomTextMsg = string.Empty;

    [HideInInspector] public bool rotateMesh = true;

    public bool showHelpOnPickup = false;

    // Start is called before the first frame update
    void Start()
    {
        isActive = true;
    }

    public void PickupItem()
    {
        if (isActive)
        {
            Deactivate();

            if(showHelpOnPickup)
                GameUIManager.instance.SetCenterText("Press the Left Mouse Button to use the " + powerupName);

            if (autoUse)
            {
                UsePowerup();
            }
            else
            {
                GameManager.instance.activePowerup = powerupType;
                GameUIManager.instance.SetPowerupIcon(powerupType);
            }
        }
    }

    protected void FixedUpdate()
    {
        
        if (rotateMesh)
        {
            var rot = transform.Find("Mesh").rotation;
            transform.Find("Mesh").rotation = Quaternion.AngleAxis(Time.fixedDeltaTime * 120f, rot * Vector3.up) * rot;
        }
        else
        {
            transform.rotation = Quaternion.AngleAxis(Time.fixedDeltaTime * 120f, transform.rotation * Vector3.up) * transform.rotation;
        }

        if (Time.time - timeDeactivated >= respawnTime && !this.isActive)
            Activate(true);
    }

    public void Activate(bool fade)
    {
        isActive = true;

        foreach (Transform child in transform)
            child.gameObject.SetActive(true);

        if (fade)
        {
            if (meshRenderer)
            {
                foreach (Material mat in meshRenderer.materials)
                    mat.color = Color.clear;
                foreach (Material mat in meshRenderer.materials)
                    mat.DOColor(Color.white, 3f);
            }
            else if (skinnedMeshRenderer) 
            {
                foreach (Material mat in skinnedMeshRenderer.materials)
                    mat.color = Color.clear;
                foreach (Material mat in skinnedMeshRenderer.materials)
                    mat.DOColor(Color.white, 3f);
            }
        }
    }

    public void Deactivate()
    {
        timeDeactivated = Time.time;
        isActive = false;

        GameManager.instance.PlayAudioClip(pickupSound);
        if (powerupType != PowerupType.TimeTravel)
            bottomTextMsg = "You picked up a " + powerupName;

        GameUIManager.instance.SetBottomText(bottomTextMsg);

        foreach (Transform child in transform)
            child.gameObject.SetActive(false);
    }

    protected virtual void UsePowerup()
    {
        //to be overriden
    }
}
