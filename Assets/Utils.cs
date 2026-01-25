using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utils
{
    public static string FormatTime(float time)
    {
        TimeSpan ts = TimeSpan.FromMilliseconds(time);
        if (time == -1)
            return "99:59.999";
        else
            return string.Format("{0:00}:{1:00}.{2:000}", ts.Minutes, ts.Seconds, ts.Milliseconds);
    }
}
