using System;
using System.Linq;
using System.Reflection;

namespace TimberPerf
{
    /**
     * BeaverBuddies and Archipelago each ship this DLL, and the game loads every DLL in every enabled mod's folder, so with
     * both on there are two copies. Only one may patch the game: the newest version, or the first loaded of the newest.
     * All mods' DLLs are loaded before any starts, so every copy sees the others when it asks.
     */
    public static class Copies
    {
        private static bool? _active;

        public static bool Active => _active ??= Elect();

        public static Version Version => typeof(Copies).Assembly.GetName().Version;

        private static bool Elect()
        {
            Assembly mine = typeof(Copies).Assembly;
            Assembly winner = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.GetName().Name == mine.GetName().Name)
                .Aggregate((best, next) => next.GetName().Version > best.GetName().Version ? next : best);
            return winner == mine;
        }
    }
}
