using System.IO;
using HarmonyLib;
using UnityEngine;

namespace URAH_Hover
{
    /// <summary>
    /// The world view brightens the province under the mouse by × 1.15 in the VerticalTint post shader,
    /// a literal with no material property. tools/make_hover_shader.py builds hover_shader.bundle next
    /// to the plugin with a copy of that shader using a smaller factor; this swaps it into every
    /// VerticalTint effect (CheckResources runs each frame and makes a new material for a new shader).
    /// Without the bundle, or if the GPU rejects the copy, vanilla's shader stays.
    /// </summary>
    [HarmonyPatch(typeof(VerticalTint), nameof(VerticalTint.CheckResources))]
    internal static class HoverShaderPatch
    {
        private const string BundleFile = "hover_shader.bundle";
        private const string AssetPath = "assets/urah/verticaltint.shader";

        private static Shader patched;
        private static bool loaded;

        private static void Prefix(VerticalTint __instance)
        {
            Shader current = __instance.tintShader;
            if (current == null || current == patched || current.name != "Hidden/VerticalTint")
            {
                return;
            }
            Shader s = Load();
            if (s != null)
            {
                __instance.tintShader = s;
                Plugin.Log.LogInfo($"Hover shader swapped on '{__instance.name}' (was instance {current.GetInstanceID()}, now {s.GetInstanceID()})");
            }
        }

        private static Shader Load()
        {
            if (loaded)
            {
                return patched;
            }
            loaded = true;
            string path = Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location), BundleFile);
            if (!File.Exists(path))
            {
                Plugin.Log.LogInfo($"No {BundleFile}; world view keeps vanilla's hover brightening");
                return null;
            }
            AssetBundle bundle = AssetBundle.LoadFromFile(path);
            Shader s = bundle != null ? bundle.LoadAsset<Shader>(AssetPath) : null;
            if (s == null || !s.isSupported)
            {
                Plugin.Log.LogWarning($"{BundleFile}: shader {(s == null ? "missing" : "not supported")}; vanilla hover kept");
                return null;
            }
            patched = s;
            Plugin.Log.LogInfo($"World view hover shader replaced from {BundleFile}");
            return patched;
        }
    }
}
