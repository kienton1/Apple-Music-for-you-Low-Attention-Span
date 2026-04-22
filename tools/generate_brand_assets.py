from __future__ import annotations

from pathlib import Path
from typing import Iterable

from PIL import Image, ImageDraw, ImageFilter, ImageFont


PROJECT_ROOT = Path(__file__).resolve().parents[1]
ASSET_DIR = PROJECT_ROOT / "AutoAppleMusic.App" / "Assets"

ICON_SIZE = 1024
LOGO_SIZE = 768
WORDMARK_SIZE = (1600, 512)

BG_TOP = (8, 20, 29, 255)
BG_BOTTOM = (18, 48, 66, 255)
BG_PANEL = (16, 34, 47, 255)
CREAM = (248, 240, 226, 255)
CREAM_SOFT = (248, 240, 226, 184)
CORAL = (255, 150, 96, 255)
CORAL_GLOW = (255, 132, 80, 180)
AQUA = (107, 228, 220, 255)
AQUA_HALO = (116, 236, 226, 190)
TEXT_SECONDARY = (181, 202, 208, 255)
WHITE = (255, 255, 255, 255)


def lerp_color(start: tuple[int, int, int, int], end: tuple[int, int, int, int], t: float) -> tuple[int, int, int, int]:
    return tuple(int(start[index] + (end[index] - start[index]) * t) for index in range(4))


def vertical_gradient(size: tuple[int, int], top: tuple[int, int, int, int], bottom: tuple[int, int, int, int]) -> Image.Image:
    width, height = size
    gradient = Image.new("RGBA", size)
    draw = ImageDraw.Draw(gradient)
    for y in range(height):
        t = y / max(height - 1, 1)
        draw.line((0, y, width, y), fill=lerp_color(top, bottom, t))
    return gradient


def rounded_mask(size: int, radius: int) -> Image.Image:
    mask = Image.new("L", (size, size), 0)
    draw = ImageDraw.Draw(mask)
    draw.rounded_rectangle((0, 0, size - 1, size - 1), radius=radius, fill=255)
    return mask


def blur_blob(size: tuple[int, int], box: tuple[int, int, int, int], fill: tuple[int, int, int, int], blur_radius: int) -> Image.Image:
    layer = Image.new("RGBA", size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    draw.ellipse(box, fill=fill)
    return layer.filter(ImageFilter.GaussianBlur(blur_radius))


def add_blob(canvas: Image.Image, box: tuple[int, int, int, int], fill: tuple[int, int, int, int], blur_radius: int, mask: Image.Image | None = None) -> None:
    blob = blur_blob(canvas.size, box, fill, blur_radius)
    if mask is not None:
        clipped = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
        clipped.paste(blob, (0, 0), mask)
        canvas.alpha_composite(clipped)
        return

    canvas.alpha_composite(blob)


def draw_mark(canvas: Image.Image, *, include_glow: bool) -> None:
    draw = ImageDraw.Draw(canvas)
    size = canvas.size[0]
    ring_box = (int(size * 0.18), int(size * 0.17), int(size * 0.82), int(size * 0.81))
    ring_width = max(14, size // 28)
    accent_width = max(8, size // 52)

    if include_glow:
        add_blob(canvas, (int(size * 0.17), int(size * 0.13), int(size * 0.83), int(size * 0.79)), (92, 208, 204, 66), size // 18)
        add_blob(canvas, (int(size * 0.22), int(size * 0.30), int(size * 0.66), int(size * 0.74)), (255, 142, 82, 52), size // 20)

    draw.arc(ring_box, start=28, end=342, fill=AQUA, width=ring_width)
    draw.arc(
        (ring_box[0] + ring_width // 2, ring_box[1] + ring_width // 2, ring_box[2] - ring_width // 2, ring_box[3] - ring_width // 2),
        start=224,
        end=298,
        fill=CREAM_SOFT,
        width=accent_width,
    )

    dot_radius = max(12, size // 32)
    dot_center = (int(size * 0.73), int(size * 0.22))
    draw.ellipse(
        (
            dot_center[0] - dot_radius,
            dot_center[1] - dot_radius,
            dot_center[0] + dot_radius,
            dot_center[1] + dot_radius,
        ),
        fill=CORAL,
    )

    shadow = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    shadow_draw = ImageDraw.Draw(shadow)

    bar_height = int(size * 0.34)
    bar_width = int(size * 0.075)
    top = int(size * 0.34)
    bar_one_left = int(size * 0.31)
    bar_gap = int(size * 0.045)
    rounding = bar_width // 2

    for left in (bar_one_left, bar_one_left + bar_width + bar_gap):
        shadow_draw.rounded_rectangle((left + 10, top + 14, left + bar_width + 10, top + bar_height + 14), radius=rounding, fill=(0, 0, 0, 110))

    triangle = [
        (int(size * 0.50) + 12, int(size * 0.34) + 14),
        (int(size * 0.50) + 12, int(size * 0.66) + 14),
        (int(size * 0.73) + 12, int(size * 0.50) + 14),
    ]
    shadow_draw.polygon(triangle, fill=(0, 0, 0, 110))
    canvas.alpha_composite(shadow.filter(ImageFilter.GaussianBlur(size // 60)))

    for left in (bar_one_left, bar_one_left + bar_width + bar_gap):
        draw.rounded_rectangle((left, top, left + bar_width, top + bar_height), radius=rounding, fill=CORAL)

    triangle = [
        (int(size * 0.50), int(size * 0.34)),
        (int(size * 0.50), int(size * 0.66)),
        (int(size * 0.73), int(size * 0.50)),
    ]
    draw.polygon(triangle, fill=CREAM)


def build_icon_image() -> Image.Image:
    base = Image.new("RGBA", (ICON_SIZE, ICON_SIZE), (0, 0, 0, 0))
    background = vertical_gradient((ICON_SIZE, ICON_SIZE), BG_TOP, BG_BOTTOM)
    mask = rounded_mask(ICON_SIZE, 248)
    base.paste(background, (0, 0), mask)

    add_blob(base, (80, 18, 580, 400), CORAL_GLOW, 88, mask)
    add_blob(base, (468, 524, 1010, 1010), AQUA_HALO, 96, mask)
    add_blob(base, (210, 200, 820, 840), (255, 255, 255, 28), 120, mask)

    sheen = Image.new("RGBA", (ICON_SIZE, ICON_SIZE), (0, 0, 0, 0))
    sheen_draw = ImageDraw.Draw(sheen)
    sheen_draw.rounded_rectangle((56, 56, ICON_SIZE - 56, int(ICON_SIZE * 0.45)), radius=210, fill=(255, 255, 255, 34))
    base.alpha_composite(sheen.filter(ImageFilter.GaussianBlur(18)))

    draw_mark(base, include_glow=True)

    frame = Image.new("RGBA", (ICON_SIZE, ICON_SIZE), (0, 0, 0, 0))
    frame_draw = ImageDraw.Draw(frame)
    frame_draw.rounded_rectangle((6, 6, ICON_SIZE - 7, ICON_SIZE - 7), radius=252, outline=(255, 255, 255, 66), width=5)
    base.alpha_composite(frame)
    return base


def build_logo_mark() -> Image.Image:
    logo = Image.new("RGBA", (LOGO_SIZE, LOGO_SIZE), (0, 0, 0, 0))
    draw_mark(logo, include_glow=True)
    return logo


def find_font(candidates: Iterable[str], size: int) -> ImageFont.FreeTypeFont:
    for candidate in candidates:
        font_path = Path(candidate)
        if font_path.exists():
            return ImageFont.truetype(str(font_path), size=size)
    return ImageFont.load_default()


def build_wordmark(mark: Image.Image) -> Image.Image:
    wordmark = Image.new("RGBA", WORDMARK_SIZE, (0, 0, 0, 0))
    resized_mark = mark.resize((280, 280), Image.Resampling.LANCZOS)
    wordmark.alpha_composite(resized_mark, (48, 116))

    title_font = find_font(
        (
            r"C:\Windows\Fonts\bahnschrift.ttf",
            r"C:\Windows\Fonts\segoeuib.ttf",
        ),
        156,
    )
    subtitle_font = find_font(
        (
            r"C:\Windows\Fonts\segoeui.ttf",
            r"C:\Windows\Fonts\bahnschrift.ttf",
        ),
        46,
    )

    draw = ImageDraw.Draw(wordmark)
    draw.text((370, 136), "Auto Apple Music", font=title_font, fill=CREAM)
    draw.text((378, 304), "Audio handoff, handled.", font=subtitle_font, fill=TEXT_SECONDARY)
    return wordmark


def save_outputs() -> None:
    ASSET_DIR.mkdir(parents=True, exist_ok=True)

    icon = build_icon_image()
    logo_mark = build_logo_mark()
    wordmark = build_wordmark(logo_mark)

    icon_png_path = ASSET_DIR / "app-icon.png"
    icon_ico_path = ASSET_DIR / "app-icon.ico"
    logo_path = ASSET_DIR / "logo-mark.png"
    wordmark_path = ASSET_DIR / "logo-wordmark.png"

    icon.save(icon_png_path)
    icon.save(icon_ico_path, sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])
    logo_mark.save(logo_path)
    wordmark.save(wordmark_path)

    print(f"Created {icon_png_path}")
    print(f"Created {icon_ico_path}")
    print(f"Created {logo_path}")
    print(f"Created {wordmark_path}")


if __name__ == "__main__":
    save_outputs()
