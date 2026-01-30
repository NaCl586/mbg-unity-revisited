using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrictionManager : MonoBehaviour
{
    public static FrictionManager instance;
    void Awake()
    {
        instance = this;
    }

    [Header("Surface Definitions")]
    [SerializeField] private FrictionSO[] frictions;
    [SerializeField] private PhysicMaterial defaultPhysicMaterial;

    // DEFAULT VALUES (MARBLE)
    private float m_staticFriction;
    private float m_kineticFriction;
    [HideInInspector] public float m_restitution;
    private float m_bounce;

    // Movement and collision reference
    private Movement movement;
    private CheckCollision checkCollision;

    // Cache of currently applied friction
    private FrictionSO currentFriction;

    public void Start()
    {   
        movement = GetComponent<Movement>();
        checkCollision = GetComponent<CheckCollision>();

        m_staticFriction = 1.1f;
        m_kineticFriction = 0.7f;
        m_restitution = 0.5f;
        m_bounce = 0f;

        currentFriction = null; // start with defaults

        defaultPhysicMaterial.staticFriction = 1.1f;
        defaultPhysicMaterial.dynamicFriction = 0.7f;
        defaultPhysicMaterial.bounciness = 1;
    }

    public void RevertMaterial()
    {
        // Only revert if something was applied before
        if (currentFriction == null) return;

        currentFriction = null;

        movement.staticFriction = m_staticFriction;
        movement.kineticFriction = m_kineticFriction;
        movement.bounce = m_bounce;

        if (GameManager.instance.shockAbsorberIsActive)
            movement.bounceRestitution = 0f;
        else if (GameManager.instance.superBounceIsActive)
            movement.bounceRestitution = 1f;
        else
            movement.bounceRestitution = m_restitution;

        defaultPhysicMaterial.staticFriction = 1.1f;
        defaultPhysicMaterial.dynamicFriction = 0.7f;
        defaultPhysicMaterial.bounciness = 1;
    }

    public void ApplyMaterial(FrictionSO frictionSO)
    {
        if (frictionSO == null) return;

        // Only apply if it's different from the current one
        if (currentFriction == frictionSO) return;

        currentFriction = frictionSO;

        if (frictionSO.friction != -1)
        {
            movement.staticFriction = frictionSO.friction * 1.1f;
            movement.kineticFriction = frictionSO.friction * 0.7f;

            defaultPhysicMaterial.staticFriction = frictionSO.friction * 1.1f;
            defaultPhysicMaterial.dynamicFriction = frictionSO.friction * 0.7f;
        }

        if (frictionSO.restitution != -1)
        {
            movement.bounceRestitution = frictionSO.restitution * 0.5f;
        }

        if (frictionSO.bounce != -1)
        {
            movement.bounce = frictionSO.bounce;
        } 
    }

    public FrictionSO SearchFriction(string _name)
    {
        if (string.IsNullOrEmpty(_name)) return null;

        foreach (var frictionSO in frictions)
        {
            if (frictionSO == null || frictionSO.textures == null) continue;

            foreach (var tex in frictionSO.textures)
            {
                if (tex != null && tex.name == _name)
                {
                    return frictionSO;
                }
            }
        }
        return null; // No match found
    }
}
