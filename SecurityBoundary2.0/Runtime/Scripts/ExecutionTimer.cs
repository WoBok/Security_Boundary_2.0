using System;
using System.Diagnostics;

public class ExecutionTimer
{
    private Stopwatch _sw = new Stopwatch();
    private const int Warmup = 10;
    private const int Iterations = 100;

    public void Profile(Action method)
    {
        // ‘§»» 
        for (int i = 0; i < Warmup; i++) method();

        // æ´»∑≤‚ ‘ 
        _sw.Reset();
        _sw.Start();
        for (int i = 0; i < Iterations; i++) method();
        _sw.Stop();

        UnityEngine.Debug.Log($"Average time: {_sw.Elapsed.TotalMilliseconds / Iterations:F4}ms");
    }
}
