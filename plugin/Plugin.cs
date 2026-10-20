using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace URAH_Hover
{
    /// <summary>
    /// Makes the province under the mouse in the world view easier to see: an outline like the political
    /// view's selection (HoverBorderPatch). No ModLink: it changes no game data, so it works with any map
    /// and mod list, vanilla included. [General] Enabled switches it on and off at runtime (the
    /// URAH_PluginManager convention): the patches come off and the outline drawn right now is cleared.
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

        private ConfigEntry<bool> enabledSetting, outline;
        private Harmony harmony;
        private bool active;

        private void Awake()
        {
            Log = Logger;
            enabledSetting = Config.Bind("General", "Enabled", true,
                "Switch the plugin on and off (takes effect right away)");
            outline = Config.Bind("Hover", "Outline", true,
                "Outline the province under the mouse in the world view");
            harmony = new Harmony(Guid);
            enabledSetting.SettingChanged += (sender, e) => Apply();
            outline.SettingChanged += (sender, e) => Apply();
            Apply();
            Log.LogInfo($"{Name} {Version} loaded, {(enabledSetting.Value ? "enabled" : "disabled")}, outline {(outline.Value ? "on" : "off")}");
        }

        /// <summary>Patches on while Enabled and Outline are both on, off (outline cleared) otherwise.</summary>
        private void Apply()
        {
            bool want = enabledSetting.Value && outline.Value;
            if (want == active)
            {
                return;
            }
            active = want;
            if (want)
            {
                foreach (Type patch in typeof(HoverBorderPatch).GetNestedTypes(AccessTools.all))
                {
                    Patch(harmony, patch);
                }
            }
            else
            {
                harmony.UnpatchSelf();
                HoverBorderPatch.Clear();
            }
            Log.LogInfo($"Outline {(want ? "patched" : "unpatched")}");
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
