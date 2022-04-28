using System;
using System.Collections;
using UnityEngine;

namespace TimeDelegates
{
    public delegate float GetDeltaTimeFunc();
    public delegate float UnscaledDeltaTimeFunc();
    public delegate System.DateTime SystemTimeFunc();
}

public interface ITimeProvider
{

    public System.DateTime GetSystemTime();
    public float GetDeltaTime();
    public float GetUnscaledDeltaTime();
}
