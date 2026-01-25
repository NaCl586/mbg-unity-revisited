using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Movement))]
public class Marble : MonoBehaviour
{
    public static Marble instance { get; private set; }

    [Header("Sound Effects")]
    AudioSource audioSource;
    [SerializeField] AudioClip jumpSfx;
    [SerializeField] AudioClip[] bounceSfx;
    [SerializeField] AudioSource rollingSound;
    [SerializeField] AudioSource slidingSound;
    [SerializeField] AudioSource useShockAbsorberSound;
    [SerializeField] AudioSource useSuperBounceSound;
    [SerializeField] AudioSource gyroSound;
    [SerializeField] AudioSource TTActiveSound;
    bool audioRollPlay = false;

    //things that stick to the marble
    public GameObject gyrocopterBlades;
    public GameObject glowBounce;

    private Movement movement;
    private Transform startPoint;
    public class OnRespawn : UnityEvent { };
    public static OnRespawn onRespawn = new OnRespawn();

    private void Awake()
    {
        // Enforce singleton
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        movement = GetComponent<Movement>();

        // Cursor setup
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        startPoint = GameObject.Find("StartPad").transform.Find("Spawn");

        onRespawn.AddListener(Respawn);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R) && !GameManager.gameFinish)
        {
            if (!GameManager.isPaused)
                onRespawn?.Invoke();
            else
                GameManager.instance.RestartLevel();
        }
    }

    public void Respawn()
    {
        if (startPoint == null)
            return;

        movement.SetPosition(startPoint.position);
    }

    public void PlaySound(PowerupType _powerup)
    {
        if (_powerup == PowerupType.ShockAbsorber)
            useShockAbsorberSound.Play();
        else if (_powerup == PowerupType.SuperBounce)
            useSuperBounceSound.Play();
        else if (_powerup == PowerupType.Gyrocopter)
            gyroSound.Play();
        else if (_powerup == PowerupType.TimeTravel)
            TTActiveSound.Play();
    }

    public void StopSound(PowerupType _powerup)
    {
        if (_powerup == PowerupType.ShockAbsorber)
            useShockAbsorberSound.Stop();
        else if (_powerup == PowerupType.SuperBounce)
            useSuperBounceSound.Stop();
        else if (_powerup == PowerupType.Gyrocopter)
            gyroSound.Stop();
        else if (_powerup == PowerupType.TimeTravel)
            TTActiveSound.Stop();
    }

    public void ToggleGlowBounce(bool _toggle)
    {
        glowBounce.SetActive(_toggle);
    }

    public void ToggleGyrocopterBlades(bool _toggle)
    {
        gyrocopterBlades.SetActive(_toggle);
    }

    public void RevertMaterial()
    {
        ToggleGlowBounce(false);

        //super bounce
        if (GameManager.instance.superBounceIsActive)
        {
            StopSound(PowerupType.SuperBounce);
            GameManager.instance.superBounceIsActive = false;
        }
        else if (GameManager.instance.shockAbsorberIsActive)
        {
            StopSound(PowerupType.ShockAbsorber);
            GameManager.instance.shockAbsorberIsActive = false;
        }
    }

    public void UseSuperBounce()
    {
        //cancel shock absorber immediately
        if (GameManager.instance.shockAbsorberIsActive)
            RevertMaterial();

        ToggleGlowBounce(true);

        if (!GameManager.instance.superBounceIsActive)
        {
            GameManager.instance.superBounceIsActive = true;

            //marble is changed into super bounce material
            FrictionManager.instance.RevertMaterial();
        }
    }

    //Shock Absorber Effects
    public void UseShockAbsorber()
    {
        //cancel super bounce immediately
        if (GameManager.instance.superBounceIsActive)
            RevertMaterial();

        ToggleGlowBounce(true);

        if (!GameManager.instance.shockAbsorberIsActive)
        {
            GameManager.instance.shockAbsorberIsActive = true;

            //marble is changed into super bounce material
            FrictionManager.instance.RevertMaterial();
        }
    }

    public void UseGyrocopter()
    {
        ToggleGyrocopterBlades(true);
        GameManager.instance.gyrocopterIsActive = true;
        PlaySound(PowerupType.Gyrocopter);
        Physics.gravity = Physics.gravity * 0.25f;
    }

    public void CancelGyrocopter()
    {
        ToggleGyrocopterBlades(false);
        GameManager.instance.gyrocopterIsActive = false;
        StopSound(PowerupType.Gyrocopter);
        Physics.gravity = Physics.gravity * 4;
    }

    public void ActivateTimeTravel(float _timeBonus)
    {
        if (!GameManager.instance.timeTravelActive)
        {
            GameManager.instance.timeTravelStartTime = Time.time;
            GameManager.instance.timeTravelActive = true;
        }
        GameManager.instance.timeTravelBonus += _timeBonus;
        PlaySound(PowerupType.TimeTravel);
    }

    public void InactivateTimeTravel()
    {
        GameManager.instance.timeTravelBonus = 0f;
        GameManager.instance.timeTravelActive = false;
        //StopSound(PowerupType.TimeTravel);
    }
}
