using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlBinding : MonoBehaviour
{
    public static ControlBinding instance;
    public void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public KeyCode moveForward;
    public KeyCode moveBackward;
    public KeyCode moveLeft;
    public KeyCode moveRight;

    public KeyCode usePowerup;
    public KeyCode jump;

    public KeyCode rotateCameraUp;
    public KeyCode rotateCameraDown;
    public KeyCode rotateCameraLeft;
    public KeyCode rotateCameraRight;

    public KeyCode freelookKey;
    public float mouseSensitivity;
    public bool invertMouseYAxis;
    public bool alwaysFreeLook;

    public void Start()
    {
        moveForward = KeyCode.W;
        moveBackward = KeyCode.S;
        moveLeft = KeyCode.A;
        moveRight = KeyCode.D;

        usePowerup = KeyCode.Mouse0;
        jump = KeyCode.Space;

        rotateCameraDown = KeyCode.DownArrow;
        rotateCameraLeft = KeyCode.LeftArrow;
        rotateCameraRight = KeyCode.RightArrow;
        rotateCameraUp = KeyCode.UpArrow;

        freelookKey = KeyCode.Mouse1;
        mouseSensitivity = 1f;
        invertMouseYAxis = false;
        alwaysFreeLook = true;
    }

}
