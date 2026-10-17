# URAH_Hover

A BepInEx 5 plugin for **Knights of Honor II: Sovereign** that makes the province under the mouse in
the world view easier to see: it gets the outline the political view uses for the selected province.
Needs no mod, works with any map and in vanilla.

Setting in `BepInEx/config/urah.koh2.hover.cfg`: `Outline` switches it on and off.

## Install

BepInEx 5 in the game folder, then `URAH_Hover.dll` into `BepInEx/plugins/URAH_Hover/`.

## Building

```sh
git clone --recursive <this repo>      # external/URAH_shared is a submodule
cd plugin && dotnet build -c Release -p:Deploy=true
```

The plugin compiles against your own game install with BepInEx 5 (`GameDir`, default: Steam on Linux).
