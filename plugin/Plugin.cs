using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace URAH_Hover
{
    /// <summary>
    /// Makes the province under the mouse in the world view easier to see: an outline like the political
    /// view's selection (HoverBorderPatch) and a softer brightening (HoverShaderPatch). Always on, no
    /// ModLink: it changes no game data, so it works with any map and mod list, vanilla included.
    ///
    /// Patching MapData and VerticalTint at load is safe: neither has a static constructor that reads
    /// defs, settings or the ModManager (unlike UserSettings, see URAH_Zoom).
    /// </summary>
    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "urah.koh2.hover";
        public const string Name = "URAH_Hover";
        public const string Version = "0.1.0";

        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            ConfigEntry<bool> border = Config.Bind("Hover", "Outline", true,
                "Outline the province under the mouse in the world view");
            ConfigEntry<bool> shader = Config.Bind("Hover", "SofterBrightening", true,
                "Brighten the province under the mouse less than the game does (needs hover_shader.bundle next to the plugin)");
            var harmony = new Harmony(Guid);
            if (border.Value)
            {
                foreach (Type patch in typeof(HoverBorderPatch).GetNestedTypes(AccessTools.all))
                {
                    Patch(harmony, patch);
                }
            }
            if (shader.Value)
            {
                Patch(harmony, typeof(HoverShaderPatch));
            }
            Log.LogInfo($"{Name} {Version} loaded, outline {(border.Value ? "on" : "off")}, " +
                        $"softer brightening {(shader.Value ? "on" : "off")}");
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
