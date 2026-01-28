using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HUDRotator : MonoBehaviour
{
    float speed = 120f;

    private void Start()
    {
        if (name.StartsWith("HC"))
            speed = 60f;

    }

    void FixedUpdate()
    {
        transform.rotation = Quaternion.AngleAxis(Time.fixedDeltaTime * speed, transform.rotation * Vector3.up) * transform.rotation;
    }
}
