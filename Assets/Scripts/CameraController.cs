using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CameraController : MonoBehaviour
{
    bool positionLocked = true;
    float mouseX, mouseY;

    private Vector3 offset;
    private Transform marble;

    Vector3 lastGravityDir;

    public class OnCameraFinish : UnityEvent { };
    public static OnCameraFinish onCameraFinish = new OnCameraFinish();

    public static CameraController instance;

    public void Awake()
    {
        instance = this;
    }

    public void Start()
    {
        marble = Marble.instance.transform;
        offset = marble.position - transform.position;

        lastGravityDir = GravitySystem.GravityDir;

        onCameraFinish.AddListener(FinishCameraPan);
        GravityModifier.onGravityChanged.AddListener(OnGravityChanged);
    }

    public void OnGravityChanged(Vector3 oldDir, Vector3 newDir)
    {
        // Compute delta between last frame’s gravity and new interpolated gravity
        Quaternion delta = Quaternion.FromToRotation(lastGravityDir, newDir);
        offset = delta * offset;
        lastGravityDir = newDir;
    }


    void LateUpdate()
    {
        HandleLook();
    }

    Quaternion ShortestRotation(Vector3 from, Vector3 to)
    {
        // Step 1: normalize vectors
        Vector3 f = from.normalized;
        Vector3 t = to.normalized;

        // Step 2: compute rotation axis
        Vector3 axis = Vector3.Cross(f, t);
        float axisLength = axis.magnitude;

        // Step 3: compute angle
        float dot = Mathf.Clamp(Vector3.Dot(f, t), -1f, 1f); // clamp for numerical stability
        float angle = Mathf.Acos(dot); // in radians

        // Step 4: handle parallel/anti-parallel vectors
        if (axisLength < 0.0001f)
        {
            // Vectors are parallel or opposite
            if (dot > 0f)
            {
                // Parallel vectors -> no rotation
                return Quaternion.identity;
            }
            else
            {
                // Opposite vectors -> 180 degrees around any perpendicular axis
                axis = Vector3.Cross(f, Vector3.right);
                if (axis.magnitude < 0.0001f)
                    axis = Vector3.Cross(f, Vector3.up);
                axis.Normalize();
                return Quaternion.AngleAxis(180f, axis);
            }
        }

        axis.Normalize();

        // Step 5: convert axis-angle to quaternion
        float halfAngle = angle * 0.5f;
        float s = Mathf.Sin(halfAngle);

        return new Quaternion(
            axis.x * s,
            axis.y * s,
            axis.z * s,
            Mathf.Cos(halfAngle)
        );
    }


    void HandleLook()
    {
        if (!GameManager.gameFinish && Time.timeScale > 0.01f)
        {
            mouseX = Input.GetAxis("Mouse X");
            mouseY = Input.GetAxis("Mouse Y");
        }
        else if (GameManager.gameFinish)
        {
            float speed = Time.deltaTime * 10;
            mouseX = speed;
        }
        else
        {
            mouseX = mouseY = 0f;
        }

        // Use cached gravity direction instead of recomputing every frame
        Vector3 up = -lastGravityDir.normalized;
        Vector3 forward = (marble.position - transform.position).normalized;
        Vector3 right = Vector3.Cross(up, forward).normalized;

        // ─── Yaw/Roll ───
        offset = Quaternion.AngleAxis(mouseX * 5f, up) * offset;

        // ─── Pitch ───
        float pitchAngle = Vector3.Angle(offset, up);
        bool canPitch =
            (pitchAngle > 10f && mouseY > 0) ||
            (pitchAngle < 170f && mouseY < 0);

        if (canPitch)
        {
            offset = Quaternion.AngleAxis(-mouseY * 5f, right) * offset;
        }

        // Camera collision code (unchanged)
        Vector3 diff = -offset;
        Vector3 marblePos = marble.position;

        Vector3 targetPos = marblePos + diff;
        RaycastHit hitInfo;
        float epsilon = 0.001f;

        int iter = 0;

        while (Physics.Raycast(marblePos, targetPos - marblePos, out hitInfo, Vector3.Distance(targetPos, marblePos)))
        {
            if (!hitInfo.collider.isTrigger)
            {
                targetPos += Vector3.Project(hitInfo.point - targetPos, hitInfo.normal);
                targetPos += hitInfo.normal * epsilon;
                diff = targetPos - marblePos;
            }

            iter++;
            if (iter > 100)
                break;
        }

        Vector3[] directions = {
            Vector3.down, Vector3.up, Vector3.forward, Vector3.right, Vector3.left, Vector3.back,
            new Vector3(1,1,1), new Vector3(-1,1,1), new Vector3(1,-1,1), new Vector3(-1,-1,1),
            new Vector3(1,1,-1), new Vector3(-1,1,-1), new Vector3(1,-1,-1), new Vector3(-1,-1,-1)
        };

        float castDistance = 0.05f;

        for (int i = 0; i < 5; i++)
        {
            bool hitSomething = false;
            for (int j = 0; j < directions.Length; j++)
            {
                if (Physics.Raycast(marblePos + diff, directions[j], out hitInfo, castDistance - epsilon))
                {
                    if (!hitInfo.collider.isTrigger)
                    {
                        hitSomething = true;
                        Vector3 newPos = hitInfo.point + hitInfo.normal * castDistance;
                        diff = newPos - marblePos;
                    }
                }
            }
            if (!hitSomething)
                break;
        }

        if (positionLocked || GameManager.gameFinish)
            transform.position = marble.position + diff;

        transform.LookAt(marble.position, up);
    }

    public void LockCamera(bool camCheck)
    {
        positionLocked = camCheck;
    }

    public void ResetCam() 
    {
        Transform startPad = GameManager.instance.startPad.transform;
        offset = (startPad.GetChild(0).position - startPad.GetChild(1).position);
    }

    public void FinishCameraPan()
    {
        StartCoroutine(PanCamera());
    }

    IEnumerator PanCamera()
    {
        float speed = Time.deltaTime * 20;

        if (transform.rotation.eulerAngles.x < 25 || transform.rotation.eulerAngles.x > 265)
        {
            while (true)
            {
                mouseY = -speed;
                yield return new WaitForFixedUpdate();
                if (!(transform.rotation.eulerAngles.x < 25 || transform.rotation.eulerAngles.x > 265))
                    break;
            }
        }
        else if (transform.rotation.eulerAngles.x > 25 && transform.rotation.eulerAngles.x < 95)
        {
            while (true)
            {
                mouseY = +speed;
                yield return new WaitForFixedUpdate();
                if (!(transform.rotation.eulerAngles.x > 25 && transform.rotation.eulerAngles.x < 95))
                    break;
            }
        }

        mouseY = 0;
    }
}
