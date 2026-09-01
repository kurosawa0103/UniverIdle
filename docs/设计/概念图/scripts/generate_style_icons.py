#!/usr/bin/env python3
"""为风格对比页生成各画风 SVG 图标（sketch 沿用现有 PNG）。"""
from __future__ import annotations
import os

ROOT = os.path.join(os.path.dirname(__file__), "..", "assets", "styles")

PALETTES = {
    "woodcut": {"bg": "#342e28", "ink": "#c8b8a0", "a": "#b86038", "b": "#6a9080", "c": "#e8dcc8"},
    "watercolor": {"bg": "#4a5054", "ink": "#3a4044", "a": "#78b0a0", "b": "#c87858", "c": "#d8e4e0"},
    "flat": {"bg": "#4e5054", "ink": "#2a2c2e", "a": "#68a89a", "b": "#d07850", "c": "#ececea"},
    "pixel": {"bg": "#545454", "ink": "#303030", "a": "#68b0a0", "b": "#d07048", "c": "#d8d8d8"},
    "codex": {"bg": "#4a4034", "ink": "#2a2218", "a": "#d0a848", "b": "#c07840", "c": "#d8c8a8"},
    "glow": {"bg": "#2a3038", "ink": "#1a2028", "a": "#68c8b8", "b": "#e8c060", "c": "#88e0d0"},
}


def wrap(style: str, body: str, w: int = 128, h: int = 128, extra: str = "") -> str:
    p = PALETTES[style]
    glow = ""
    if style == "glow":
        glow = (
            '<defs><filter id="g" x="-50%" y="-50%" width="200%" height="200%">'
            '<feGaussianBlur stdDeviation="3" result="b"/><feMerge>'
            '<feMergeNode in="b"/><feMergeNode in="SourceGraphic"/></feMerge></filter></defs>'
        )
    if style == "watercolor":
        extra += f'<rect width="{w}" height="{h}" fill="{p["c"]}" opacity=".35"/>'
    return (
        f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {w} {h}" width="{w}" height="{h}">'
        f'{glow}{extra}{body}</svg>'
    )


def woodcut_icon(style: str, paths: str) -> str:
    p = PALETTES[style]
    return wrap(style, paths.format(**p), extra=f'<rect width="128" height="128" fill="{p["bg"]}"/>')


def flat_icon(style: str, body: str) -> str:
    p = PALETTES[style]
    return wrap(style, body.format(**p), extra=f'<rect width="128" height="128" rx="12" fill="{p["bg"]}"/>')


def pixel_cell(x: int, y: int, c: str, s: int = 8) -> str:
    return f'<rect x="{x*s}" y="{y*s}" width="{s}" height="{s}" fill="{c}"/>'


def build_pixel_grid(style: str, cells: list[tuple[int, int, str]]) -> str:
    p = PALETTES[style]
    parts = [f'<rect width="128" height="128" fill="{p["bg"]}"/>']
    for x, y, key in cells:
        parts.append(pixel_cell(x, y, p[key]))
    return wrap(style, "".join(parts))


PIXEL_SKILLS = {
    "hunting": [(5,3,"a"),(6,3,"a"),(4,4,"a"),(7,4,"a"),(3,5,"b"),(8,5,"b"),(4,6,"b"),(7,6,"b"),(5,7,"b"),(6,7,"b")],
    "fishing": [(10,2,"b"),(10,3,"b"),(10,4,"b"),(10,5,"b"),(9,6,"b"),(8,7,"b"),(7,8,"b"),(6,9,"b"),(5,10,"b"),(4,11,"a")],
    "foraging": [(4,5,"a"),(5,5,"a"),(6,5,"a"),(7,5,"a"),(8,5,"a"),(4,6,"b"),(8,6,"b"),(4,7,"b"),(8,7,"b"),(5,8,"b"),(6,8,"b"),(7,8,"b")],
    "mining": [(3,6,"a"),(4,5,"a"),(5,4,"b"),(6,5,"b"),(7,6,"a"),(8,7,"b"),(9,8,"b"),(10,9,"a"),(11,10,"a")],
    "alchemy": [(6,3,"c"),(7,3,"c"),(5,4,"c"),(8,4,"c"),(5,5,"a"),(6,5,"a"),(7,5,"a"),(8,5,"a"),(6,6,"a"),(7,6,"a"),(5,7,"b"),(8,7,"b"),(6,8,"b"),(7,8,"b")],
    "smithing": [(3,8,"ink"),(4,8,"ink"),(5,8,"ink"),(6,8,"ink"),(7,8,"ink"),(8,8,"ink"),(9,8,"ink"),(10,8,"ink"),(11,8,"ink"),(12,8,"ink"),(5,5,"b"),(6,5,"b"),(7,5,"b"),(4,6,"a"),(8,6,"a")],
    "combat": [(6,2,"c"),(6,3,"c"),(6,4,"c"),(6,5,"c"),(6,6,"c"),(6,7,"c"),(6,8,"c"),(6,9,"c"),(6,10,"c"),(6,11,"c"),(4,4,"a"),(8,6,"a")],
}


# ── 128×128 图标定义 ──────────────────────────────────────────

def icon_logo(s: str) -> str:
    if s == "woodcut":
        return woodcut_icon(s, '<path d="M64 18 L78 52 H50 Z" fill="{a}" stroke="{ink}" stroke-width="3"/><circle cx="64" cy="58" r="10" fill="{b}" stroke="{ink}" stroke-width="2"/><path d="M40 95 Q64 70 88 95" fill="none" stroke="{ink}" stroke-width="3"/>')
    if s == "flat":
        return flat_icon(s, '<circle cx="64" cy="42" r="18" fill="{b}"/><polygon points="64,22 74,48 54,48" fill="{a}"/><path d="M32 96 Q64 72 96 96" fill="none" stroke="{ink}" stroke-width="4" stroke-linecap="round"/>')
    if s == "pixel":
        return build_pixel_grid(s, [(6,2,"a"),(7,2,"a"),(5,3,"a"),(6,3,"b"),(7,3,"b"),(8,3,"a"),(6,4,"b"),(7,4,"b"),(4,10,"ink"),(5,10,"ink"),(6,10,"ink"),(7,10,"ink"),(8,10,"ink"),(9,10,"ink"),(10,10,"ink"),(5,11,"ink"),(6,11,"ink"),(7,11,"ink"),(8,11,"ink"),(9,11,"ink")])
    if s == "codex":
        return wrap(s, '<rect width="128" height="128" fill="{bg}"/><circle cx="64" cy="64" r="46" fill="none" stroke="{a}" stroke-width="3"/><path d="M64 28 L72 56 H56 Z" fill="{b}" stroke="{a}" stroke-width="2"/><text x="64" y="98" text-anchor="middle" font-size="14" fill="{a}" font-family="serif">坠</text>'.format(**PALETTES[s]))
    if s == "glow":
        return wrap(s, '<circle cx="64" cy="48" r="16" fill="{a}" filter="url(#g)"/><polygon points="64,24 76,52 52,52" fill="{b}" filter="url(#g)"/><path d="M28 100 Q64 76 100 100" stroke="{c}" stroke-width="4" fill="none" filter="url(#g)"/>'.format(**PALETTES[s]))
    # watercolor
    p = PALETTES[s]
    return wrap(s, f'<ellipse cx="64" cy="50" rx="22" ry="18" fill="{p["b"]}" opacity=".7"/><polygon points="64,26 76,54 52,54" fill="{p["a"]}" opacity=".75"/><path d="M30 98 Q64 74 98 98" stroke="{p["ink"]}" stroke-width="2" fill="none" opacity=".5"/>', extra=f'<rect width="128" height="128" fill="{p["c"]}"/>')


def icon_skill(s: str, kind: str) -> str:
    defs = {
        "hunting": ("M30 100 L70 40 L75 45 L40 100 Z M78 38 L95 55 L88 62 L72 45 Z", "bow and arrow"),
        "fishing": ("M90 20 V100 M90 20 Q50 50 35 85", "fishing rod"),
        "foraging": ("M35 45 H93 V95 H35 Z M35 55 H93", "basket"),
        "mining": ("M25 55 L55 25 L75 45 L45 75 Z M70 30 L100 60 L85 75 L55 45 Z", "pickaxe"),
        "alchemy": ("M48 35 H80 V55 H48 Z M54 55 L44 95 H84 L74 55 Z", "flask"),
        "smithing": ("M30 70 H98 V82 H30 Z M40 50 H70 V70 H40 Z", "anvil"),
        "combat": ("M64 20 L74 100 M50 35 L78 65", "sword"),
    }
    d, _ = defs[kind]
    if s == "woodcut":
        return woodcut_icon(s, f'<path d="{d}" fill="{{a}}" stroke="{{ink}}" stroke-width="3"/>')
    if s == "flat":
        return flat_icon(s, f'<path d="{d}" fill="{{a}}" stroke="{{ink}}" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/>')
    if s == "pixel":
        return build_pixel_grid(s, PIXEL_SKILLS.get(kind, PIXEL_SKILLS["fishing"]))
    if s == "codex":
        return wrap(s, f'<rect width="128" height="128" fill="{{bg}}"/><rect x="16" y="16" width="96" height="96" fill="none" stroke="{{a}}" stroke-width="2"/><path d="{d}" fill="{{b}}" stroke="{{a}}" stroke-width="2"/>'.format(**PALETTES[s]))
    if s == "glow":
        return wrap(s, f'<path d="{d}" fill="{{a}}" stroke="{{c}}" stroke-width="2" filter="url(#g)"/>'.format(**PALETTES[s]))
    p = PALETTES[s]
    return wrap(s, f'<path d="{d}" fill="{p["a"]}" opacity=".65" stroke="{p["ink"]}" stroke-width="2"/>', extra=f'<rect width="128" height="128" fill="{p["c"]}"/>')


def icon_item(s: str, kind: str) -> str:
    if kind == "firefly_shrimp":
        body = '<ellipse cx="64" cy="68" rx="28" ry="14" fill="{a}"/><path d="M92 64 L110 58 L110 70 Z" fill="{b}"/><circle cx="48" cy="62" r="4" fill="{c}"/>'
    elif kind == "herb_sprout":
        body = '<path d="M64 95 V55 M64 55 Q48 45 40 30 M64 55 Q80 45 88 30" stroke="{b}" stroke-width="4" fill="none"/><ellipse cx="40" cy="28" rx="10" ry="6" fill="{a}"/><ellipse cx="88" cy="28" rx="10" ry="6" fill="{a}"/>'
    elif kind == "red_potion":
        body = '<rect x="46" y="42" width="36" height="14" fill="{c}" stroke="{ink}" stroke-width="2"/><path d="M50 56 H78 V92 H50 Z" fill="{a}" stroke="{ink}" stroke-width="2"/>'
    elif kind == "copper_ingot":
        body = '<path d="M28 58 L64 42 L100 58 L64 74 Z" fill="{a}" stroke="{ink}" stroke-width="2"/><path d="M28 58 V78 L64 94 V74 M100 58 V78 L64 94" fill="{b}" stroke="{ink}" stroke-width="2"/>'
    else:  # star_sand
        body = '<circle cx="50" cy="60" r="3" fill="{b}"/><circle cx="70" cy="52" r="4" fill="{b}"/><circle cx="82" cy="68" r="2" fill="{b}"/><ellipse cx="64" cy="78" rx="30" ry="12" fill="{a}" opacity=".8"/>'
    if s == "woodcut":
        return woodcut_icon(s, body)
    if s == "flat":
        return flat_icon(s, body)
    if s == "pixel":
        return build_pixel_grid(s, [(6,6,"a"),(7,6,"a"),(6,7,"b"),(7,7,"b"),(5,8,"a"),(6,8,"a"),(7,8,"a"),(8,8,"a")])
    if s == "codex":
        return wrap(s, ('<rect width="128" height="128" fill="{bg}"/>' + body).format(**PALETTES[s]))
    if s == "glow":
        return wrap(s, body.format(**PALETTES[s]), extra="")  # glow filter in wrap
    p = PALETTES[s]
    return wrap(s, body.format(**p), extra=f'<rect width="128" height="128" fill="{p["c"]}"/>')


def scene_banner(s: str) -> str:
    w, h = 320, 180
    p = PALETTES[s]
    river = f'<path d="M0 120 Q80 100 160 115 T320 105 V180 H0 Z" fill="{p["b"]}" opacity=".85"/>'
    hills = f'<path d="M0 90 Q60 40 130 70 T260 50 T320 80 V120 H0 Z" fill="{p["a"]}" opacity=".6"/>'
    stars = "".join(f'<circle cx="{20+i*40}" cy="{22+(i%3)*8}" r="2" fill="{p["c"]}"/>' for i in range(7))
    if s == "glow":
        stars = "".join(f'<circle cx="{20+i*40}" cy="{22+(i%3)*8}" r="3" fill="{p["a"]}" filter="url(#g)"/>' for i in range(7))
    if s == "woodcut":
        hills = f'<path d="M0 90 L80 50 L160 80 L240 45 L320 85 V120 H0 Z" fill="{p["a"]}" stroke="{p["ink"]}" stroke-width="2"/>'
    return wrap(s, hills + river + stars, w=w, h=h, extra=f'<rect width="{w}" height="{h}" fill="{p["bg"]}"/>')


def scene_card(s: str, kind: str) -> str:
    w, h = 320, 180
    p = PALETTES[s]
    water = f'<rect y="100" width="320" height="80" fill="{p["b"]}" opacity=".5"/>'
    if kind == "shrimp":
        fig = f'<ellipse cx="160" cy="110" rx="40" ry="18" fill="{p["a"]}"/><line x1="200" y1="60" x2="160" y2="100" stroke="{p["ink"]}" stroke-width="3"/>'
    elif kind == "sand":
        fig = f'<ellipse cx="160" cy="120" rx="50" ry="20" fill="{p["c"]}"/><circle cx="140" cy="100" r="4" fill="{p["b"]}"/><circle cx="175" cy="95" r="3" fill="{p["b"]}"/>'
    else:
        fig = f'<ellipse cx="200" cy="115" rx="35" ry="15" fill="{p["a"]}"/><line x1="120" y1="40" x2="200" y2="105" stroke="{p["ink"]}" stroke-width="3"/>'
    return wrap(s, water + fig, w=w, h=h, extra=f'<rect width="{w}" height="{h}" fill="{p["bg"]}"/>')


def icon_detail(s: str) -> str:
    p = PALETTES[s]
    body = (
        f'<ellipse cx="128" cy="150" rx="70" ry="35" fill="{p["a"]}"/>'
        f'<path d="M198 140 L240 120 L240 155 Z" fill="{p["b"]}"/>'
        f'<circle cx="90" cy="130" r="8" fill="{p["c"]}" opacity=".9"/>'
        f'<circle cx="110" cy="120" r="5" fill="{p["c"]}" opacity=".7"/>'
    )
    if s == "glow":
        body = body.replace('circle', 'circle filter="url(#g)"')
    return wrap(s, body, w=256, h=256, extra=f'<rect width="256" height="256" fill="{p["bg"]}"/>')


FILES: dict[str, str] = {}


def reg(name: str, builder):
    FILES[name] = builder  # type: ignore


for k in ["hunting", "fishing", "foraging", "mining", "alchemy", "smithing", "combat"]:
    reg(f"skill_{k}", lambda s, k=k: icon_skill(s, k))

for k in ["firefly_shrimp", "herb_sprout", "red_potion", "copper_ingot", "star_sand"]:
    reg(f"item_{k}", lambda s, k=k: icon_item(s, k))

reg("logo", icon_logo)
reg("banner_yingxi", scene_banner)
reg("card_catch_shrimp", lambda s: scene_card(s, "shrimp"))
reg("card_pan_sand", lambda s: scene_card(s, "sand"))
reg("card_catch_trout", lambda s: scene_card(s, "trout"))
reg("detail_firefly_shrimp", icon_detail)


def main():
    for style in PALETTES:
        out_dir = os.path.join(ROOT, style)
        os.makedirs(out_dir, exist_ok=True)
        for name, builder in FILES.items():
            path = os.path.join(out_dir, f"{name}.svg")
            with open(path, "w", encoding="utf-8") as f:
                f.write(builder(style))
            print("wrote", path)
    print("done:", len(PALETTES) * len(FILES), "icons")


if __name__ == "__main__":
    main()
