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
    // Inspector
    public SmoothingType smoothing = SmoothingType.Linear;
    public MovementMode movementMode = MovementMode.Constant;
    public SequenceNumber[] sequenceNumbers;
    public int initialTargetPosition;   // >=0 = marker, -1 = forward, -2 = backward
    public float initialPosition = 0f;
    public float resolution = 0.1f; // spline density

    // Internal
    Rigidbody rb;
    Vector3 basePos;

    Vector3[] positions;

    float[] segmentStartTimes;
    float[] segmentInvDurations;

    float time;
    float targetTime;
    float totalTime;
    float maxReachableTime;

    int index;
    int initialIndex;

    float currentSegmentEnd;
    float initialSegmentEnd;

    Vector3 previousPosition;
    [HideInInspector] public Vector3 platformVelocity;

    SequenceNumber[] splineSequence;

    // ─────────────────────────────────────────────
    // Initialization
    // ─────────────────────────────────────────────
    public void InitMovingPlatform()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;

        basePos = transform.position;

        CacheMarkerPositions();
        GeneratePositions();

        var seq = (smoothing == SmoothingType.Spline)
            ? splineSequence
            : sequenceNumbers;

        int count = seq.Length;

        // Last marker has no outgoing segment
        seq[count - 1].secondsToNext = 0f;

        segmentStartTimes = new float[count];
        segmentInvDurations = new float[count];

        totalTime = 0f;
        maxReachableTime = 0f;

        for (int i = 0; i < count; i++)
        {
            segmentStartTimes[i] = totalTime;

            float dt = seq[i].secondsToNext;
            segmentInvDurations[i] = dt > 0f ? 1f / dt : 0f;

            totalTime += dt;
            if (i < count - 1)
                maxReachableTime += dt;
        }

        time = Mathf.Clamp(initialPosition, 0f, maxReachableTime);
        targetTime = ComputeTargetTime();

        index = FindSegmentIndex(time);
        initialIndex = index;

        currentSegmentEnd =
            segmentStartTimes[index] + seq[index].secondsToNext;

        initialSegmentEnd = currentSegmentEnd;

        Vector3 startPos = basePos + positions[index];
        rb.MovePosition(startPos);
        previousPosition = startPos;

        platformVelocity = Vector3.zero;
    }

    public void ResetMP()
    {
        time = Mathf.Clamp(initialPosition, 0f, maxReachableTime);
        targetTime = ComputeTargetTime();

        index = FindSegmentIndex(time);

        var seq = (smoothing == SmoothingType.Spline)
            ? splineSequence
            : sequenceNumbers;

        currentSegmentEnd =
            segmentStartTimes[index] + seq[index].secondsToNext;

        Vector3 pos = basePos + positions[index];
        rb.MovePosition(pos);
        previousPosition = pos;

        platformVelocity = Vector3.zero;
    }

    // ─────────────────────────────────────────────
    // Trigger API
    // ─────────────────────────────────────────────
    public void GoToTime(float t)
    {
        if (movementMode == MovementMode.Triggered)
            targetTime = Mathf.Clamp(t, 0f, maxReachableTime);
    }

    float ComputeTargetTime()
    {
        if (initialTargetPosition < 0)
            return -1f;

        var seq = (smoothing == SmoothingType.Spline)
            ? splineSequence
            : sequenceNumbers;

        int count = seq.Length;
        int target = Mathf.Clamp(initialTargetPosition, 0, count);

        float t = 0f;

        for (int i = 0; i < target && i < count - 1; i++)
            t += seq[i].secondsToNext;

        return t;
    }

    // ─────────────────────────────────────────────
    // Fixed Update
    // ─────────────────────────────────────────────
    void FixedUpdate()
    {
        var seq = (smoothing == SmoothingType.Spline)
            ? splineSequence
            : sequenceNumbers;

        // Triggered early-out
        if (movementMode == MovementMode.Triggered &&
            targetTime >= 0f &&
            Mathf.Approximately(time, targetTime))
        {
            platformVelocity = Vector3.zero;
            return;
        }

        // Advance time
        if (movementMode == MovementMode.Triggered)
        {
            if (targetTime >= 0f)
                time = Mathf.MoveTowards(time, targetTime, Time.fixedDeltaTime);
        }
        else
        {
            if (initialTargetPosition == -1)
                time += Time.fixedDeltaTime;
            else if (initialTargetPosition == -2)
                time -= Time.fixedDeltaTime;
        }

        // Loop
        if (movementMode == MovementMode.Constant)
        {
            if (time > totalTime)
                time = 0f;
            else if (time < 0f)
                time = totalTime;
        }

        // Forward segment advance
        while (index < seq.Length - 2 &&
               time > segmentStartTimes[index] + seq[index].secondsToNext)
        {
            index++;
        }

        // Backward segment advance
        while (index > 0 &&
               time < segmentStartTimes[index])
        {
            index--;
        }

        if (index >= seq.Length - 1)
        {
            platformVelocity = Vector3.zero;
            return;
        }

        float t =
            (time - segmentStartTimes[index]) *
            segmentInvDurations[index];

        if (smoothing == SmoothingType.Accelerate)
            t = 0.5f - 0.5f * Mathf.Cos(t * Mathf.PI);

        Vector3 newPosition =
            basePos + Vector3.LerpUnclamped(
                positions[index],
                positions[index + 1],
                t
            );

        platformVelocity =
            (newPosition - previousPosition) / Time.fixedDeltaTime;

        previousPosition = newPosition;
        rb.MovePosition(newPosition);
    }

    // ─────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────
    int FindSegmentIndex(float t)
    {
        for (int i = 0; i < segmentStartTimes.Length - 1; i++)
            if (t < segmentStartTimes[i + 1])
                return i;

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
            Vector3 first = sequenceNumbers[0].markerPos;

            for (int i = 0; i < positions.Length; i++)
                positions[i] = sequenceNumbers[i].markerPos - first;

            return;
        }

        List<SequenceNumber> seq = new List<SequenceNumber>();
        Vector3 firstMarker = sequenceNumbers[0].markerPos;

        for (int i = 0; i < sequenceNumbers.Length - 1; i++)
        {
            GetCatmullRomSplineVectors(i, out var segment);
            float step = sequenceNumbers[i].secondsToNext * resolution;

            foreach (var p in segment)
            {
                seq.Add(new SequenceNumber
                {
                    markerPos = p + firstMarker,
                    secondsToNext = step
                });
            }
        }

        seq.Add(new SequenceNumber
        {
            markerPos = sequenceNumbers[^1].markerPos,
            secondsToNext = 0f
        });

        splineSequence = seq.ToArray();

        positions = new Vector3[splineSequence.Length];
        for (int i = 0; i < positions.Length; i++)
            positions[i] = splineSequence[i].markerPos - firstMarker;
    }

    void GetCatmullRomSplineVectors(int pos, out List<Vector3> segment)
    {
        segment = new List<Vector3>();

        Vector3 first = sequenceNumbers[0].markerPos;

        Vector3 p0 = sequenceNumbers[ClampListPos(pos - 1)].markerPos - first;
        Vector3 p1 = sequenceNumbers[pos].markerPos - first;
        Vector3 p2 = sequenceNumbers[ClampListPos(pos + 1)].markerPos - first;
        Vector3 p3 = sequenceNumbers[ClampListPos(pos + 2)].markerPos - first;

        Vector3 last = p1;
        int loops = Mathf.FloorToInt(1f / resolution);

        for (int i = 1; i <= loops; i++)
        {
            float t = i * resolution;
            Vector3 next = GetCatmullRomPosition(t, p0, p1, p2, p3);
            segment.Add(last);
            last = next;
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
