using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerGoToTarget : MonoBehaviour
{
    public MovingPlatform movingPlatform;
    public float targetTime;
    public bool instantReturn = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Marble"))
        {
            if (!instantReturn)
                movingPlatform.GoToTime(targetTime);
            else
                movingPlatform.ResetMP();
        }
    }
}
