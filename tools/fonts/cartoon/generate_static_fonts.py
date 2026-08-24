#!/usr/bin/env python3
"""Generate the exact static TTF instances consumed by Unity/TMP."""

from pathlib import Path

from fontTools.ttLib import TTFont
from fontTools.varLib.instancer import instantiateVariableFont


ROOT = Path(__file__).resolve().parents[3]
SOURCE = Path(__file__).resolve().parent / "sources"
OUTPUT = ROOT / "Assets" / "newdesign" / "Resources" / "Themes" / "Cartoon" / "Fonts"


FONTS = {
    "Montserrat[wght].ttf": {
        "Montserrat-Bold.ttf": {"wght": 700},
        "Montserrat-ExtraBold.ttf": {"wght": 800},
    },
    "PlusJakartaSans[wght].ttf": {
        "PlusJakartaSans-Regular.ttf": {"wght": 400},
        "PlusJakartaSans-Medium.ttf": {"wght": 500},
        "PlusJakartaSans-SemiBold.ttf": {"wght": 600},
    },
    "NotoSans[wdth,wght].ttf": {
        "NotoSans-Regular.ttf": {"wdth": 100, "wght": 400},
        "NotoSans-Medium.ttf": {"wdth": 100, "wght": 500},
        "NotoSans-SemiBold.ttf": {"wdth": 100, "wght": 600},
        "NotoSans-Bold.ttf": {"wdth": 100, "wght": 700},
        "NotoSans-ExtraBold.ttf": {"wdth": 100, "wght": 800},
    },
}


def main() -> None:
    OUTPUT.mkdir(parents=True, exist_ok=True)
    for source_name, instances in FONTS.items():
        source_path = SOURCE / source_name
        if not source_path.is_file():
            raise FileNotFoundError(source_path)
        for output_name, axes in instances.items():
            font = TTFont(source_path, recalcBBoxes=False, recalcTimestamp=False)
            instantiateVariableFont(font, axes, inplace=True, optimize=True)
            font["head"].modified = 0
            output_path = OUTPUT / output_name
            font.save(output_path, reorderTables=False)
            print(output_path.relative_to(ROOT))


if __name__ == "__main__":
    main()
