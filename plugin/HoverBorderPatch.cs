using HarmonyLib;

namespace URAH_Hover
{
    /// <summary>
    /// In the world view the province under the mouse only gets a slight brightening (VerticalTint
    /// shader, see HoverShaderPatch), easy to miss. This also outlines it with the highlight border the
    /// political view uses for the selected province (MapData.h_realm_borders, selection_highlight_color),
    /// coasts included. In the world view vanilla keeps all of those borders switched off.
    /// The renderer is set up for the political view: lines flat at height 0 (snap_to_terrain off, under
    /// the world view's terrain) and 10 wide. In the world view it snaps to the terrain and gets
    /// WorldViewWidth (realm borders there are 1.25, kingdom borders 1.5); the political view gets the
    /// scene's values back.
    /// </summary>
    internal static class HoverBorderPatch
    {
        private const float WorldViewWidth = 3f;

        private static int shownRealm;
        private static BordersBatching styled;
        private static float pvWidth;
        private static bool pvSnap;

        private static void Refresh(MapData map)
        {
            if (!(map is WorldMap) || map.h_realm_borders == null)
            {
                return;
            }
            bool worldView = !ViewMode.IsPoliticalView();
            SetStyle(map.h_realm_borders, worldView);
            int want = 0;
            if (worldView && UserSettings.WV_Province_Highlight)
            {
                Realm r = Realm.Get(map.highlighted_realm);
                if (r != null && !r.IsSeaRealm())
                {
                    want = r.id;
                }
            }
            if (want == shownRealm)
            {
                return;
            }
            SetBorders(map, Realm.Get(shownRealm), false);
            SetBorders(map, Realm.Get(want), true);
            shownRealm = want;
            map.h_realm_borders.MarkDirty(reload: false, refresh_buffers: true);
        }

        private static void SetStyle(BordersBatching h, bool worldView)
        {
            if (styled != h)
            {
                styled = h; // a new campaign brings a new renderer with the scene's values
                pvWidth = h.width;
                pvSnap = h.snap_to_terrain;
            }
            float width = worldView ? WorldViewWidth : pvWidth;
            bool snap = worldView || pvSnap;
            if (h.width == width && h.snap_to_terrain == snap)
            {
                return;
            }
            // the fields are copied to the material whenever the buffers refill; set it now as well
            h.width = width;
            h.snap_to_terrain = snap;
            h.line_info?.material?.SetFloat("_Width", width);
            h.line_info?.material?.SetInt("_SNAP_TO_TERRAIN", snap ? 1 : 0);
        }

        private static void SetBorders(MapData map, Realm r, bool on)
        {
            if (r == null)
            {
                return;
            }
            foreach (Realm.Neighbor n in r.Neighbors)
            {
                Realm other = Realm.Get(n.rid);
                if (other != null)
                {
                    map.h_realm_borders.SetActiveBorder(r, other, on, !on && other.IsSeaRealm());
                }
            }
        }

        /// <summary>These rewrite h_realm_borders by the vanilla rules, which leave them off in the world view.</summary>
        private static void Reset(MapData map)
        {
            shownRealm = 0;
            Refresh(map);
        }

        [HarmonyPatch(typeof(MapData), nameof(MapData.SetHighlighedRealm))]
        private static class SetHighlighedRealmPatch
        {
            private static void Postfix(MapData __instance) => Refresh(__instance);
        }

        [HarmonyPatch(typeof(MapData), nameof(MapData.ViewModeChanged))]
        private static class ViewModeChangedPatch
        {
            private static void Postfix(MapData __instance) => Reset(__instance);
        }

        [HarmonyPatch(typeof(MapData), nameof(MapData.UpdateRealmBorders), typeof(Realm), typeof(bool))]
        private static class UpdateRealmBordersPatch
        {
            private static void Postfix(MapData __instance, Realm r)
            {
                if (r != null && r.id == shownRealm)
                {
                    Reset(__instance);
                }
            }
        }

        [HarmonyPatch(typeof(MapData), nameof(MapData.UpdateSelectedBorders))]
        private static class UpdateSelectedBordersPatch
        {
            private static void Postfix(MapData __instance) => Reset(__instance);
        }

        [HarmonyPatch(typeof(MapData), nameof(MapData.ClearSelectedBorders))]
        private static class ClearSelectedBordersPatch
        {
            private static void Postfix(MapData __instance) => Reset(__instance);
        }
    }
}
