# URAH_Hover

A BepInEx 5 plugin for **Knights of Honor II: Sovereign** that makes the province under the mouse in
the world view easier to see: it gets the outline the political view uses for the selected province.
Needs no mod, works with any map and in vanilla.

Setting in `BepInEx/config/urah.koh2.hover.cfg`: `Outline` switches it on and off.

## Install

BepInEx 5 in the game folder, then `URAH_Hover.dll` into `BepInEx/plugins/URAH_Hover/`.

## Releases

Built and signed releases are in [URAH_koh2_releases](https://github.com/URAHDangerzone/URAH_koh2_releases/releases): [`URAH_Hover-v<version>`](https://github.com/URAHDangerzone/URAH_koh2_releases/releases?q=URAH_Hover&expanded=true) is this plugin alone
(`BepInEx/plugins/URAH_Hover/`), [`URAH_Starter`](https://github.com/URAHDangerzone/URAH_koh2_releases/releases?q=URAH_Starter&expanded=true) bundles BepInEx 5 with URAH_Loader, URAH_Net and URAH_Hover.
Every release is signed with the URAH release key; [URAH_koh2_releases](https://github.com/URAHDangerzone/URAH_koh2_releases) explains how to check it.

## Building

```sh
git clone --recursive <this repo>      # external/URAH_shared is a submodule
cd plugin && dotnet build -c Release -p:Deploy=true
```

The plugin compiles against your own game install with BepInEx 5 (`GameDir`, default: Steam on Linux).
