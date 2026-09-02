#!/usr/bin/env python3
"""将炼金术道具图抠底为 RGBA，并缩放到 512×512。"""
from __future__ import annotations

import os
import sys
from collections import deque

from PIL import Image

ASSETS = os.path.join(os.path.dirname(__file__), "..", "assets")
SIZE = 512
# 与角落背景色的最大色差（欧氏距离平方）
BG_TOLERANCE_SQ = 28 * 28


def color_dist_sq(a: tuple, b: tuple) -> int:
    return sum((int(a[i]) - int(b[i])) ** 2 for i in range(3))


def sample_bg_color(im: Image.Image) -> tuple[int, int, int]:
    w, h = im.size
    corners = [(0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1)]
    rs = gs = bs = 0
    for x, y in corners:
        r, g, b = im.getpixel((x, y))[:3]
        rs += r
        gs += g
        bs += b
    n = len(corners)
    return rs // n, gs // n, bs // n


def remove_background(im: Image.Image) -> Image.Image:
    rgb = im.convert("RGB")
    w, h = rgb.size
    bg = sample_bg_color(rgb)
    visited = [[False] * w for _ in range(h)]
    transparent = [[False] * w for _ in range(h)]
    q: deque[tuple[int, int]] = deque()

    def try_push(x: int, y: int) -> None:
        if x < 0 or y < 0 or x >= w or y >= h or visited[y][x]:
            return
        visited[y][x] = True
        if color_dist_sq(rgb.getpixel((x, y)), bg) <= BG_TOLERANCE_SQ:
            transparent[y][x] = True
            q.append((x, y))

    for x in range(w):
        try_push(x, 0)
        try_push(x, h - 1)
    for y in range(h):
        try_push(0, y)
        try_push(w - 1, y)

    while q:
        x, y = q.popleft()
        try_push(x + 1, y)
        try_push(x - 1, y)
        try_push(x, y + 1)
        try_push(x, y - 1)

    rgba = rgb.convert("RGBA")
    px = rgba.load()
    for y in range(h):
        for x in range(w):
            if transparent[y][x]:
                r, g, b, _a = px[x, y]
                px[x, y] = (r, g, b, 0)
    return rgba


def process(path: str) -> None:
    im = Image.open(path)
    out = remove_background(im)
    out = out.resize((SIZE, SIZE), Image.Resampling.LANCZOS)
    out.save(path, "PNG")
    mode = Image.open(path).mode
    print(f"ok {os.path.basename(path)} -> {mode} {SIZE}x{SIZE}")


def main() -> None:
    names = sys.argv[1:] or [
        "item_rag.png",
        "item_splinter.png",
        "item_empty_bottle.png",
        "item_copper_nail.png",
        "item_worn_coin.png",
    ]
    for name in names:
        path = os.path.join(ASSETS, name)
        if not os.path.isfile(path):
            print("skip missing", name)
            continue
        process(path)


if __name__ == "__main__":
    main()
