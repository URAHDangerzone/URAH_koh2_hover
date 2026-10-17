# URAH_Hover

A BepInEx 5 plugin for **Knights of Honor II: Sovereign** that makes the province under the mouse in
the world view easier to see: it gets the outline the political view uses for the selected province,
and a softer brightening than the game's. Needs no mod, works with any map and in vanilla.

Settings in `BepInEx/config/urah.koh2.hover.cfg`: `Outline` and `SofterBrightening` switch the two
parts on and off.

## Install

BepInEx 5 in the game folder, then `URAH_Hover.dll` and `hover_shader.bundle` into
`BepInEx/plugins/URAH_Hover/`. Without the bundle only the outline works.

## Building

```sh
git clone --recursive <this repo>      # external/URAH_shared is a submodule
python tools/make_hover_shader.py "<game folder>" plugin/bin/hover_shader.bundle
cd plugin && dotnet build -c Release -p:Deploy=true
```

`make_hover_shader.py` copies the game's own hover shader with a smaller brightening factor into an
asset bundle (needs python with UnityPy, see the script). The plugin compiles against your own game
install with BepInEx 5 (`GameDir`, default: Steam on Linux).
