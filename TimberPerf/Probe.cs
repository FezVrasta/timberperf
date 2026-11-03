using System;
using System.Diagnostics;

namespace TimberPerf
{
    /**
     * Where the optimized code spends its time, for a profiler outside (BeaverBuddies' HitchWatch) that sets Spent and
     * Garbaged. Unset, a probe point costs one test.
     */
    public static class Probe
    {
        public static Action<string, string, long> Spent;
        public static Action<string, string, long> Garbaged;
        public static bool FollowGarbage;

        public static bool Profiling => Spent != null;
        public static long Now => Stopwatch.GetTimestamp();
        public static long Heap => FollowGarbage ? UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() : 0;
    }
}
