using System.Collections.Generic;
using System.Diagnostics;

namespace TimberPerf
{
    /**
     * Time spent in a few hot spots, for the dev command that measures
     * performance (Archipelago's perf command, through TimberPerfBridge). Off, a hot spot costs one flag test.
     */
    public static class PerfCounters
    {
        public static bool Enabled;
        // Log every call slower than this many ms as it happens (0 for none), to find a hitch's cause
        public static double LogOver;
        private static readonly Dictionary<string, (double Ms, int Calls, double Worst)> Totals = new();

        private static readonly Dictionary<long, long> Mem = new();
        private static readonly Dictionary<string, long> Garbage = new();

        public static long Start()
        {
            if (!Enabled) return 0;
            long at = Stopwatch.GetTimestamp();
            Mem[at] = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();
            return at;
        }

        /** KB allocated in each counted spot since the last call. */
        public static string TakeGarbage()
        {
            var lines = new List<string>();
            foreach (var (name, bytes) in Garbage) lines.Add($"{name} {bytes / 1024} KB");
            Garbage.Clear();
            lines.Sort();
            return string.Join(", ", lines);
        }

        public static void Stop(string name, long started)
        {
            if (started == 0) return;
            if (Mem.TryGetValue(started, out long before))
            {
                Mem.Remove(started);
                long grown = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() - before;
                if (grown > 0) { Garbage.TryGetValue(name, out long sum); Garbage[name] = sum + grown; }
            }
            double ms = (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency;
            if (LogOver > 0 && ms > LogOver) UnityEngine.Debug.Log($"[TimberPerf] Slow {name}: {ms:0.0} ms at frame {UnityEngine.Time.frameCount}");
            Totals.TryGetValue(name, out var total);
            Totals[name] = (total.Ms + ms, total.Calls + 1, System.Math.Max(total.Worst, ms));
        }

        public static void Reset() => Totals.Clear();

        // Enabled and LogOver set from outside, by reflection
        public static void Switch(bool enabled, double logOver)
        {
            Enabled = enabled;
            LogOver = logOver;
        }

        /** "name total/calls (worst)" for each, per frame when frames is given. */
        public static string Report(int frames)
        {
            var lines = new List<string>();
            foreach (var (name, total) in Totals)
            {
                lines.Add($"{name}: {total.Ms / System.Math.Max(frames, 1):0.000} ms/frame ({total.Calls} calls, {total.Ms / System.Math.Max(total.Calls, 1):0.000} ms each, worst {total.Worst:0.000})");
            }
            lines.Sort();
            return string.Join("; ", lines);
        }
    }
}
