"""Build a small asset bundle with the world view's post shader (Hidden/VerticalTint) whose hover
brightening is turned down. The shader multiplies the colour of the province under the mouse by
1.15 (one `mul r, r0.xyz, l(1.15, 1.15, 1.15, 0)` in the d3d11 pixel shader); this rewrites that
literal and fixes the DXBC checksum, which d3d11 checks. The plugin (HoverShaderPatch.cs) loads the
bundle and swaps the shader into the VerticalTint camera effects.

The host is the game's europe bundle, which carries a copy of the shader (byte-identical with the
one in resources.assets): everything but the shader and the AssetBundle object is dropped, and the
bundle and its file are renamed so Unity loads it next to the real europe bundle.

Usage: python tools/make_hover_shader.py <game_dir> <out_bundle> [--factor 1.07]
"""

import argparse
import hashlib
import math
import struct
from pathlib import Path

import lz4.block
import UnityPy

SHADER = "Hidden/VerticalTint"
VANILLA_FACTOR = 1.15
BUNDLE_NAME = "urah_hover_shader"
CAB_NAME = "CAB-" + hashlib.md5(BUNDLE_NAME.encode()).hexdigest()
CONTAINER_PATH = "assets/urah/verticaltint.shader"

_S = [7, 12, 17, 22] * 4 + [5, 9, 14, 20] * 4 + [4, 11, 16, 23] * 4 + [6, 10, 15, 21] * 4
_K = [int(abs(math.sin(i + 1)) * 2**32) & 0xFFFFFFFF for i in range(64)]


def _md5_block(state, chunk):
    a, b, c, d = state
    m = struct.unpack("<16I", chunk)
    for i in range(64):
        if i < 16:
            f, g = (b & c) | (~b & d), i
        elif i < 32:
            f, g = (d & b) | (~d & c), (5 * i + 1) % 16
        elif i < 48:
            f, g = b ^ c ^ d, (3 * i + 5) % 16
        else:
            f, g = c ^ (b | (~d & 0xFFFFFFFF)), (7 * i) % 16
        f = (f + a + _K[i] + m[g]) & 0xFFFFFFFF
        a, d, c = d, c, b
        b = (b + (((f << _S[i]) | (f >> (32 - _S[i]))) & 0xFFFFFFFF)) & 0xFFFFFFFF
    return [(x + y) & 0xFFFFFFFF for x, y in zip(state, (a, b, c, d))]


def dxbc_checksum(blob: bytes) -> bytes:
    """MD5 over everything after the 20-byte header, with d3dcompiler's own padding of the last block."""
    data = blob[20:]
    n = len(data)
    state = [0x67452301, 0xEFCDAB89, 0x98BADCFE, 0x10325476]
    full = n - n % 64
    for i in range(0, full, 64):
        state = _md5_block(state, data[i:i + 64])
    rest, bits = data[full:], n * 8
    tail = struct.pack("<I", (bits >> 2) | 1)
    if len(rest) >= 56:
        state = _md5_block(state, rest + b"\x80" + b"\x00" * (63 - len(rest)))
        state = _md5_block(state, struct.pack("<I", bits) + b"\x00" * 56 + tail)
    else:
        state = _md5_block(state, struct.pack("<I", bits) + rest + b"\x80" + b"\x00" * (55 - len(rest)) + tail)
    return struct.pack("<4I", *state)


def patch_programs(data: bytes, factor: float) -> bytes:
    """Rewrite the hover factor in every DXBC program that has it and re-sign those programs."""
    old = struct.pack("<f", VANILLA_FACTOR) * 3 + struct.pack("<f", 0.0)
    new = struct.pack("<f", factor) * 3 + struct.pack("<f", 0.0)
    out = bytearray(data)
    patched = 0
    i = 0
    while (i := data.find(b"DXBC", i)) >= 0:
        size = struct.unpack_from("<I", data, i + 24)[0]
        blob = bytes(out[i:i + size])
        if dxbc_checksum(blob) != blob[4:20]:
            raise SystemExit(f"DXBC at {i}: checksum does not match before patching")
        hits = blob.count(old)
        if hits:
            if hits != 1:
                raise SystemExit(f"DXBC at {i}: {hits} copies of the hover literal, expected 1")
            blob = blob.replace(old, new)
            blob = blob[:4] + dxbc_checksum(blob) + blob[20:]
            out[i:i + size] = blob
            patched += 1
        i += size
    if patched == 0:
        raise SystemExit("hover literal 1.15 not found - the game's shader changed")
    return bytes(out)


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("game_dir", type=Path)
    ap.add_argument("out", type=Path)
    ap.add_argument("--factor", type=float, default=1.07, help="hover brightening (vanilla 1.15)")
    args = ap.parse_args()

    env = UnityPy.load(str(args.game_dir / "AssetBundles" / "europe"))
    bundle = env.file
    (cab_name, sf), = [(k, v) for k, v in bundle.files.items() if hasattr(v, "objects")]
    ab_obj = shader_obj = None
    for obj in sf.objects.values():
        if obj.type.name == "AssetBundle":
            ab_obj = obj
        elif obj.type.name == "Shader" and obj.read().m_ParsedForm.m_Name == SHADER:
            shader_obj = obj
    if ab_obj is None or shader_obj is None:
        raise SystemExit(f"europe bundle has no AssetBundle object or no {SHADER}")

    shader = shader_obj.read_typetree()
    blob = bytes(shader["compressedBlob"])
    offsets, clens, dlens = shader["offsets"], shader["compressedLengths"], shader["decompressedLengths"]
    if len(offsets) != 1 or len(offsets[0]) != 1:
        raise SystemExit(f"{SHADER}: expected one platform with one chunk, got offsets {offsets}")
    raw = lz4.block.decompress(blob[offsets[0][0]:offsets[0][0] + clens[0][0]], uncompressed_size=dlens[0][0])
    packed = lz4.block.compress(patch_programs(raw, args.factor), mode="high_compression", store_size=False)
    shader["compressedBlob"] = list(packed)
    shader["offsets"], shader["compressedLengths"], shader["decompressedLengths"] = [[0]], [[len(packed)]], [[len(raw)]]
    shader_obj.save_typetree(shader)

    pptr = {"m_FileID": 0, "m_PathID": shader_obj.path_id}
    ab = ab_obj.read_typetree()
    ab.update(m_Name=BUNDLE_NAME, m_AssetBundleName=BUNDLE_NAME, m_Dependencies=[], m_PreloadTable=[pptr],
              m_Container=[(CONTAINER_PATH, {"preloadIndex": 0, "preloadSize": 1, "asset": pptr})])
    ab_obj.save_typetree(ab)

    for path_id in [p for p in sf.objects if p not in (ab_obj.path_id, shader_obj.path_id)]:
        del sf.objects[path_id]
    sf.externals = []
    sf.name = CAB_NAME
    bundle.files = {CAB_NAME: sf}

    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_bytes(bundle.save(packer="lz4"))
    print(f"{args.out}: {SHADER} with hover factor {args.factor} ({args.out.stat().st_size} bytes)")


if __name__ == "__main__":
    main()
