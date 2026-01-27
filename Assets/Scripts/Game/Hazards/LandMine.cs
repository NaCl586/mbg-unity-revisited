using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LandMine : MonoBehaviour
{
    public Transform explosionParticle;
    public float respawnTime = 7f;
    [Space]
    [HideInInspector] public bool isActive = true;
    float timeDeactivated;

    MeshRenderer mr;
    MeshCollider mc;

    AudioSource audioSource;
    public AudioClip explodeSfx;

    void Start()
    {
        mr = GetComponent<MeshRenderer>();
        mc = GetComponent<MeshCollider>();
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (Time.time - timeDeactivated >= respawnTime)
        {
            ActivateLandMine();
        }
    }

    public void CollisionEnter()
    {
        // Positions
        Vector3 vec = Marble.instance.transform.position - transform.position;

        // Explosion force
        float distance = vec.magnitude;
        float explosionStrength = ComputeExplosionStrength(distance);

        if (distance > 0.0001f)
        {
            Vector3 dir = vec.normalized;
            Movement.instance.marbleVelocity += dir * explosionStrength;
        }

        var effect = Instantiate(explosionParticle);
        effect.transform.position = transform.position;
        effect.transform.localScale = Vector3.one * 8f;
        Destroy(effect.gameObject, 3f);

        Vector3 pos = transform.position;

        Vector3 inheritedVel = Movement.instance != null
            ? Movement.instance.marbleVelocity
            : Vector3.zero;

        audioSource.PlayOneShot(explodeSfx);

        DeactivateLandMine();
    }

    float ComputeExplosionStrength(float r)
    {
        // Figured out through testing by RandomityGuy
        if (r >= 10.25f)
            return 0f;

        if (r >= 10f)
            return Mathf.Lerp(30.0087f, 30.7555f, r - 10f);

        // The explosion first becomes stronger the further you are away from it,
        // then becomes weaker again (parabolic).
        float a = 0.071436222f;
        float v = ((r - 5f) * (r - 5f)) / (-4f * a) + 87.5f;

        return v;
    }


    public void DeactivateLandMine()
    {
        timeDeactivated = Time.time;
        isActive = false;

        mr.enabled = false;
        mc.enabled = false;
    }

    public void ActivateLandMine()
    {
        isActive = true;

        mr.enabled = true;
        mc.enabled = true;
    }
}
