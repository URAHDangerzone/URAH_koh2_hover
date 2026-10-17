using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace URAH_Hover
{
    /// <summary>
    /// Makes the province under the mouse in the world view easier to see: an outline like the political
    /// view's selection (HoverBorderPatch). Always on, no ModLink: it changes no game data, so it works
    /// with any map and mod list, vanilla included.
    ///
    /// Patching MapData at load is safe: it has no static constructor that reads defs, settings or the
    /// ModManager (unlike UserSettings, see URAH_Zoom).
    /// </summary>
    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "urah.koh2.hover";
        public const string Name = "URAH_Hover";
        public const string Version = URAH.BuildVersion.Value; // plugin/version.txt

        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            ConfigEntry<bool> border = Config.Bind("Hover", "Outline", true,
                "Outline the province under the mouse in the world view");
            if (border.Value)
            {
                var harmony = new Harmony(Guid);
                foreach (Type patch in typeof(HoverBorderPatch).GetNestedTypes(AccessTools.all))
                {
                    Patch(harmony, patch);
                }
            }
            Log.LogInfo($"{Name} {Version} loaded, outline {(border.Value ? "on" : "off")}");
        }

        /// <summary>Each patch on its own, so one that no longer fits the game does not take the others with it.</summary>
        private static void Patch(Harmony harmony, Type patch)
        {
            try
            {
                harmony.CreateClassProcessor(patch).Patch();
            }
            catch (Exception e)
            {
                Log.LogError($"Patch {patch.Name} failed: {e}");
            }
        }
    }
}
