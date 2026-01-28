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
    public AudioSource rollingSound;
    public AudioSource slidingSound;
    [SerializeField] AudioSource useShockAbsorberSound;
    [SerializeField] AudioSource useSuperBounceSound;
    [SerializeField] AudioSource gyroSound;
    [SerializeField] AudioSource TTActiveSound;
    bool audioRollPlay = false;

    //things that stick to the marble
    public GameObject gyrocopterBlades;
    public GameObject glowBounce;

    public GameObject bounceParticle;

    private Movement movement;
    private Transform startPoint;
    public class OnRespawn : UnityEvent { };
    public static OnRespawn onRespawn = new OnRespawn();

    public void Awake()
    {
        // Enforce singleton
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        onRespawn.AddListener(Respawn);
    }

    private void Start()
    {
        movement = GetComponent<Movement>();
        audioSource = GetComponent<AudioSource>();

        startPoint = GameManager.instance.startPad.transform.Find("Spawn");
    }

    private void FixedUpdate()
    {
        if (Input.GetKeyDown(KeyCode.R) && !GameManager.gameFinish)
        {
            if (!GameManager.isPaused)
                onRespawn?.Invoke();
            else
                GameManager.instance.RestartLevel();
        }

        if (Input.GetKey(ControlBinding.instance.usePowerup) && !GameManager.isPaused && !GameManager.gameFinish)
            UsePowerup();
    }

    public void LateUpdate()
    {
        gyrocopterBlades.transform.position = transform.position;
    }

    void UsePowerup()
    {
        PowerupType powerUp = GameManager.instance.ConsumePowerup();
        if (powerUp == PowerupType.SuperJump)
            SuperJump.onUseSuperJump?.Invoke();
        if (powerUp == PowerupType.SuperSpeed)
            SuperSpeed.onUseSuperSpeed?.Invoke();
        if (powerUp == PowerupType.ShockAbsorber)
            ShockAbsorber.onUseShockAbsorber?.Invoke();
        if (powerUp == PowerupType.SuperBounce)
            SuperBounce.onUseSuperBounce?.Invoke();
        if (powerUp == PowerupType.Gyrocopter)
            Gyrocopter.onUseGyrocopter?.Invoke();
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

    public void PlayBounceSound(float volume)
    {
        audioSource.volume = volume;
        audioSource.PlayOneShot(bounceSfx[Random.Range(0, bounceSfx.Length)]);
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
        }
        else if (GameManager.instance.shockAbsorberIsActive)
        {
            StopSound(PowerupType.ShockAbsorber);
        }

        GameManager.instance.superBounceIsActive = false;
        GameManager.instance.shockAbsorberIsActive = false;

        if (GameManager.instance.shockAbsorberIsActive)
            movement.bounceRestitution = 0;
        else if (GameManager.instance.superBounceIsActive)
            movement.bounceRestitution = 1;
        else
            movement.bounceRestitution = movement.GetComponent<FrictionManager>().m_restitution;
    }

    public void UseSuperBounce()
    {
        //cancel shock absorber immediately
        if (GameManager.instance.shockAbsorberIsActive)
            GameManager.instance.shockAbsorberIsActive = false;

        ToggleGlowBounce(true);

        if (!GameManager.instance.superBounceIsActive)
        {
            GameManager.instance.superBounceIsActive = true;

            //marble is changed into super bounce material
            FrictionManager.instance.RevertMaterial();
            movement.bounceRestitution = 1f;
        }
    }

    //Shock Absorber Effects
    public void UseShockAbsorber()
    {
        //cancel super bounce immediately
        if (GameManager.instance.superBounceIsActive)
            GameManager.instance.superBounceIsActive = false;

        ToggleGlowBounce(true);

        if (!GameManager.instance.shockAbsorberIsActive)
        {
            GameManager.instance.shockAbsorberIsActive = true;

            //marble is changed into super bounce material
            FrictionManager.instance.RevertMaterial();
            movement.bounceRestitution = 0f;
        }
    }

    public void UseGyrocopter()
    {
        ToggleGyrocopterBlades(true);
        GameManager.instance.gyrocopterIsActive = true;
        PlaySound(PowerupType.Gyrocopter);
        movement.gravity = movement.gravity * 0.25f;
    }

    public void CancelGyrocopter()
    {
        ToggleGyrocopterBlades(false);
        GameManager.instance.gyrocopterIsActive = false;
        StopSound(PowerupType.Gyrocopter);
        movement.gravity = movement.gravity * 4;
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
        StopSound(PowerupType.TimeTravel);
    }

    public void BounceEmitter(float _speed, CollisionInfo _collisionInfo)
    {
        if (_speed > 3)
        {
            var effect = Instantiate(bounceParticle);
            effect.transform.position = transform.position;
            effect.transform.up = _collisionInfo.normal.normalized;

            effect.transform.parent = _collisionInfo.collider.transform;

            effect.transform.localScale = Vector3.one;

            Destroy(effect.gameObject, effect.GetComponent<ParticleSystem>().main.duration + 1f);
        }
    }
}
