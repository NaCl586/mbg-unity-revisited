using UnityEngine;
using System.Collections.Generic;

public enum SmoothingType
{
    Linear,
    Accelerate,
    Spline
}

public enum MovementMode
{
    Constant,
    Triggered
}

[System.Serializable]
public class SequenceNumber
{
    public GameObject marker;
    [HideInInspector] public Vector3 markerPos;
    public float secondsToNext;
}

[RequireComponent(typeof(Rigidbody))]
public class MovingPlatform : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // Inspector
    // ─────────────────────────────────────────────
    public SmoothingType smoothing = SmoothingType.Linear;
    public MovementMode movementMode = MovementMode.Constant;
    public SequenceNumber[] sequenceNumbers;
    public float initialPosition = 0f;
    public float resolution = 0.1f; // for spline

    // ─────────────────────────────────────────────
    // Internal state
    // ─────────────────────────────────────────────
    private Rigidbody rb;

    private Vector3[] positions;

    // OPT: cached timing data
    private float[] segmentStartTimes;
    private float[] segmentInvDurations;

    private float time;
    private float targetTime;
    private float totalTime;
    private float maxReachableTime;

    private int index;
    private int initialIndex;

    private float currentSegmentEnd;
    private float initialSegmentEnd;

    private Vector3 previousPosition;
    [HideInInspector] public Vector3 platformVelocity;

    private Vector3 basePos;
    private Quaternion initialRotation;

    private SequenceNumber[] splineSequence;

    // ─────────────────────────────────────────────
    // Initialization
    // ─────────────────────────────────────────────
    public void InitMovingPlatform()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;

        basePos = transform.position;
        initialRotation = transform.rotation;

        CacheMarkerPositions();
        GeneratePositions();

        int count;

        if (smoothing == SmoothingType.Spline)
        {
            count = splineSequence.Length;

            segmentStartTimes = new float[count];
            segmentInvDurations = new float[count];

            totalTime = 0f;
            maxReachableTime = 0f;

            for (int i = 0; i < count; i++)
            {
                segmentStartTimes[i] = totalTime;

                float dt = splineSequence[i].secondsToNext;
                segmentInvDurations[i] = dt > 0f ? 1f / dt : 0f;

                totalTime += dt;

                if (i < count - 1)
                    maxReachableTime += dt;
            }

            time = Mathf.Clamp(initialPosition, 0f, maxReachableTime);
            targetTime = time;

            index = FindSegmentIndex(time);
            previousPosition = basePos;
            return;
        }

        count =
            smoothing == SmoothingType.Spline
            ? splineSequence.Length
            : sequenceNumbers.Length;

        // Ensure last marker has no segment
        sequenceNumbers[count - 1].secondsToNext = 0f;

        segmentStartTimes = new float[count];
        segmentInvDurations = new float[count];

        totalTime = 0f;
        maxReachableTime = 0f;

        // Build timing cache
        for (int i = 0; i < count; i++)
        {
            segmentStartTimes[i] = totalTime;

            float dt = sequenceNumbers[i].secondsToNext;
            segmentInvDurations[i] = dt > 0f ? 1f / dt : 0f;

            totalTime += dt;

            if (i < count - 1)
                maxReachableTime += dt;
        }

        time = Mathf.Clamp(initialPosition, 0f, maxReachableTime);
        targetTime = time;

        index = FindSegmentIndex(time);
        initialIndex = index;

        currentSegmentEnd =
            segmentStartTimes[index] + (smoothing == SmoothingType.Spline
                                        ? splineSequence[index].secondsToNext
                                        : sequenceNumbers[index].secondsToNext);

        initialSegmentEnd = currentSegmentEnd;

        previousPosition = basePos;
    }

    public void ResetMP()
    {
        transform.rotation = initialRotation;

        index = initialIndex;
        time = initialPosition;
        targetTime = time;
        currentSegmentEnd = initialSegmentEnd;

        rb.MovePosition(basePos);
        previousPosition = basePos;
        platformVelocity = Vector3.zero;
    }

    // ─────────────────────────────────────────────
    // Trigger API
    // ─────────────────────────────────────────────
    public void GoToTime(float t)
    {
        if (movementMode == MovementMode.Triggered)
            targetTime = Mathf.Clamp(t, 0f, maxReachableTime);
        else
            targetTime = t;
    }

    // ─────────────────────────────────────────────
    // Fixed Update
    // ─────────────────────────────────────────────
    void FixedUpdate()
    {
        float t;
        Vector3 newPosition;

        if (smoothing == SmoothingType.Spline)
        {
            // Early-out for triggered
            if (movementMode == MovementMode.Triggered &&
                Mathf.Approximately(time, targetTime))
            {
                platformVelocity = Vector3.zero;
                return;
            }

            // Advance time
            if (movementMode == MovementMode.Triggered)
                time = Mathf.MoveTowards(time, targetTime, Time.fixedDeltaTime);
            else
                time += Time.fixedDeltaTime;

            // Loop
            if (movementMode == MovementMode.Constant && time > totalTime)
                time = 0f;

            index = FindSegmentIndex(time);

            if (index >= positions.Length - 1)
                return;

            t = (time - segmentStartTimes[index]) * segmentInvDurations[index];

            newPosition =
                basePos + Vector3.LerpUnclamped(
                    positions[index],
                    positions[index + 1],
                    t
                );

            platformVelocity =
                (newPosition - previousPosition) / Time.fixedDeltaTime;

            previousPosition = newPosition;
            rb.MovePosition(newPosition);
            return;
        }


        // Early-out when triggered platform reached target
        if (movementMode == MovementMode.Triggered &&
            Mathf.Approximately(time, targetTime))
        {
            platformVelocity = Vector3.zero;
            return;
        }

        // Update time
        if (movementMode == MovementMode.Triggered)
        {
            time = Mathf.MoveTowards(time, targetTime, Time.fixedDeltaTime);
        }
        else
        {
            time += Time.fixedDeltaTime;
        }

        // Looping for constant platforms
        if (movementMode == MovementMode.Constant && time > totalTime)
        {
            time = 0f;
            index = 0;
            currentSegmentEnd =
                segmentStartTimes[0] + sequenceNumbers[0].secondsToNext;
        }

        // OPT: single-step segment advance
        if (index < positions.Length - 2 &&
            time > currentSegmentEnd)
        {
            index++;
            currentSegmentEnd += (smoothing == SmoothingType.Spline
                        ? splineSequence[index].secondsToNext
                        : sequenceNumbers[index].secondsToNext);
        }

        // Safety: never interpolate past last valid segment
        if (index >= positions.Length - 1)
        {
            platformVelocity = Vector3.zero;
            return;
        }

        // Interpolation
        t = (time - segmentStartTimes[index]) * segmentInvDurations[index];

        if (smoothing == SmoothingType.Accelerate)
        {
            t = 0.5f - 0.5f * Mathf.Cos(t * Mathf.PI);
        }

        newPosition =
            basePos + Vector3.LerpUnclamped(
                positions[index],
                positions[index + 1],
                t
            );

        // Velocity tracking
        platformVelocity =
            (newPosition - previousPosition) * (1f / Time.fixedDeltaTime);

        previousPosition = newPosition;

        rb.MovePosition(newPosition);
    }

    // ─────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────
    int FindSegmentIndex(float t)
    {
        for (int i = 0; i < segmentStartTimes.Length - 1; i++)
        {
            if (t < segmentStartTimes[i + 1])
                return i;
        }
        return segmentStartTimes.Length - 2;
    }

    void CacheMarkerPositions()
    {
        foreach (var sn in sequenceNumbers)
            sn.markerPos = sn.marker.transform.position;
    }

    void GeneratePositions()
    {
        if (smoothing != SmoothingType.Spline)
        {
            positions = new Vector3[sequenceNumbers.Length];
            Vector3 firstMarkerPos = sequenceNumbers[0].markerPos;

            for (int i = 0; i < positions.Length; i++)
                positions[i] = sequenceNumbers[i].markerPos - firstMarkerPos;

            return;
        }

        List<SequenceNumber> seq = new List<SequenceNumber>();
        Vector3 first = sequenceNumbers[0].markerPos;

        for (int i = 0; i < sequenceNumbers.Length - 1; i++)
        {
            GetCatmullRomSplineVectors(i, out var segment);

            float stepTime =
                sequenceNumbers[i].secondsToNext * resolution;

            foreach (var p in segment)
            {
                seq.Add(new SequenceNumber
                {
                    markerPos = p + first,
                    secondsToNext = stepTime
                });
            }
        }

        // Final point
        seq.Add(new SequenceNumber
        {
            markerPos = sequenceNumbers[^1].markerPos,
            secondsToNext = 0f
        });

        splineSequence = seq.ToArray();

        positions = new Vector3[splineSequence.Length];
        for (int i = 0; i < positions.Length; i++)
            positions[i] = splineSequence[i].markerPos - first;
    }



    // ─────────────────────────────────────────────
    // Spline helpers (unchanged)
    // ─────────────────────────────────────────────
    void GetCatmullRomSplineVectors(int pos, out List<Vector3> segment)
    {
        segment = new List<Vector3>();
        Vector3 firstMarkerPos = sequenceNumbers[0].markerPos;

        Vector3 p0 =
            sequenceNumbers[ClampListPos(pos - 1)].markerPos - firstMarkerPos;
        Vector3 p1 =
            sequenceNumbers[pos].markerPos - firstMarkerPos;
        Vector3 p2 =
            sequenceNumbers[ClampListPos(pos + 1)].markerPos - firstMarkerPos;
        Vector3 p3 =
            sequenceNumbers[ClampListPos(pos + 2)].markerPos - firstMarkerPos;

        Vector3 lastPos = p1;
        int loops = Mathf.FloorToInt(1f / resolution);

        for (int i = 1; i <= loops; i++)
        {
            float t = i * resolution;
            Vector3 newPos = GetCatmullRomPosition(t, p0, p1, p2, p3);
            segment.Add(lastPos);
            lastPos = newPos;
        }
    }

    int ClampListPos(int pos)
    {
        if (pos < 0) return sequenceNumbers.Length - 1;
        if (pos >= sequenceNumbers.Length) return 0;
        return pos;
    }

    Vector3 GetCatmullRomPosition(
        float t,
        Vector3 p0,
        Vector3 p1,
        Vector3 p2,
        Vector3 p3)
    {
        Vector3 a = 2f * p1;
        Vector3 b = p2 - p0;
        Vector3 c = 2f * p0 - 5f * p1 + 4f * p2 - p3;
        Vector3 d = -p0 + 3f * p1 - 3f * p2 + p3;
        return 0.5f * (a + b * t + c * t * t + d * t * t * t);
    }
}
