using System;
using UnityEngine;

[RequireComponent(typeof(Movement))]
public class Marble : MonoBehaviour
{
    public static Marble instance;
    public void Awake() => instance = this;

    private Movement movement;
    [HideInInspector] public Transform startPoint;

    void Start()
    {
        //disable cursor visibility
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        movement = GetComponent<Movement>();

        startPoint = GameManager.instance.startPad.transform.Find("Spawn").transform;

        transform.position = startPoint.position;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            GravityModifier.onResetGravity?.Invoke();

            movement.marbleVelocity = Vector3.zero;
            movement.marbleAngularVelocity = Vector3.zero;
            transform.position = startPoint.position;

            CameraController.instance.ResetCam();
        }
    }
}