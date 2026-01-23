using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;

public class GravityModifier : Powerups
{
    private static bool isRotating;

    [SerializeField] private GameObject upVectorFrom;
    [SerializeField] private GameObject upVectorTo;
    private Vector3 upVector;

    [Header("Gravity Settings")]
    public float transitionTime = 0.5f;

    private bool triggered;
    public class OnResetGravity : UnityEvent { }
    public static OnResetGravity onResetGravity = new OnResetGravity();
    public class OnGravityChangedEvent : UnityEvent<Vector3, Vector3> { }
    public static OnGravityChangedEvent onGravityChanged = new OnGravityChangedEvent();

    private void Start()
    {
        onResetGravity.AddListener(ResetGravity);

        upVector = upVectorTo.transform.position - upVectorFrom.transform.position;

        isRotating = false;
    }

    protected override void UsePowerup()
    {
        if (triggered)
            return;

        ApplyGravity(upVector);

        triggered = true;
    }

    void ApplyGravity(Vector3 targetDir)
    {
        StopCoroutine("ApplyGravityCoroutine"); // cancel any ongoing gravity tween
        StartCoroutine(ApplyGravityCoroutine(targetDir));
    }

    IEnumerator ApplyGravityCoroutine(Vector3 targetDir)
    {
        if (targetDir.sqrMagnitude < 0.001f)
            yield break;

        targetDir.Normalize();

        Vector3 startGravity = GravitySystem.GravityDir;
        float elapsed = 0f;

        triggered = true;

        while (elapsed < transitionTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionTime);

            Vector3 newGravity = SafeLerp(startGravity, targetDir, t).normalized;
            GravitySystem.GravityDir = newGravity;

            // Fire event each frame with current gravity
            onGravityChanged.Invoke(startGravity, newGravity);

            yield return null;
        }

        GravitySystem.GravityDir = targetDir;
        onGravityChanged.Invoke(startGravity, targetDir); // final exact value

        triggered = false;
    }


    Vector3 SafeLerp(Vector3 start, Vector3 target, float t)
    {
        start.Normalize();
        target.Normalize();

        float dot = Vector3.Dot(start, target);

        if (dot < -0.9999f) // nearly opposite
        {
            // Use camera forward as reference, but project it onto plane perpendicular to 'start'
            Vector3 lookVector = Camera.main.transform.forward;
            Vector3 projected = lookVector - Vector3.Dot(lookVector, start) * start;

            Vector3 intermediate = projected.normalized;

            // Fallback if projection degenerates (camera forward ≈ start)
            if (intermediate.sqrMagnitude < 1e-6f)
                intermediate = Vector3.Cross(start, Vector3.right).normalized;

            // Two-phase lerp: start→intermediate, then intermediate→target
            if (t < 0.5f)
                return Vector3.Lerp(start, intermediate, t * 2f).normalized;
            else
                return Vector3.Lerp(intermediate, target, (t - 0.5f) * 2f).normalized;
        }
        else
        {
            return Vector3.Lerp(start, target, t).normalized;
        }
    }



    private void ResetGravity()
    {
        GravitySystem.GravityDir = Vector3.down;
        onGravityChanged.Invoke(Vector3.down, Vector3.down);
    }
}
