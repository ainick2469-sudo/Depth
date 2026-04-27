using System;
using System.Diagnostics;
using UnityEngine;

namespace FrontierDepths.Core
{
    public static class LoadTimingLogger
    {
        public static IDisposable Measure(string label)
        {
            return new TimingScope(label);
        }

        public static void Log(string label, long elapsedMilliseconds)
        {
            if (!ShouldLog() || string.IsNullOrWhiteSpace(label))
            {
                return;
            }

            UnityEngine.Debug.Log($"[LoadTiming] {label}: {elapsedMilliseconds}ms");
        }

        private static bool ShouldLog()
        {
            return Application.isEditor || UnityEngine.Debug.isDebugBuild;
        }

        private sealed class TimingScope : IDisposable
        {
            private readonly string label;
            private readonly Stopwatch stopwatch;

            public TimingScope(string label)
            {
                this.label = label ?? string.Empty;
                stopwatch = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                stopwatch.Stop();
                Log(label, stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
