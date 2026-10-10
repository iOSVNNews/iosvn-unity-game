"""Skeleton shared by the art generator, the preview renderer and the Unity runtime.

World positions are in rig units (feet at y=0, about 1000 units tall), the figure
faces RIGHT in a three-quarter view.  "N" = near side (toward the viewer, drawn in
front), "F" = far side (behind the body).
"""

BONES = [  # name, parent
    ("root", None),
    ("hips", "root"),
    ("legF_up", "hips"), ("legF_lo", "legF_up"),
    ("legN_up", "hips"), ("legN_lo", "legN_up"),
    ("skirt", "hips"),
    ("torso", "hips"),
    ("back", "torso"),
    ("neck", "torso"),
    ("head", "neck"),
    ("armF_up", "torso"), ("armF_lo", "armF_up"), ("handF", "armF_lo"),
    ("armN_up", "torso"), ("armN_lo", "armN_up"), ("handN", "armN_lo"),
]

# Adult proportions (about 7 heads tall), broad shoulders for men, A-pose arms.
WORLD = {
    "m": {
        "root": (0, 0), "hips": (0, 640),
        "legF_up": (34, 644), "legF_lo": (38, 348),
        "legN_up": (-32, 640), "legN_lo": (-30, 345),
        "skirt": (0, 720), "torso": (0, 720), "back": (0, 900),
        "neck": (4, 975), "head": (8, 1012),
        "armF_up": (110, 956), "armF_lo": (183, 775), "handF": (249, 613),
        "armN_up": (-108, 958), "armN_lo": (-183, 773), "handN": (-250, 606),
    },
    "f": {
        "root": (0, 0), "hips": (0, 600),
        "legF_up": (28, 604), "legF_lo": (32, 325),
        "legN_up": (-27, 600), "legN_lo": (-25, 322),
        "skirt": (0, 670), "torso": (0, 670), "back": (0, 850),
        "neck": (3, 915), "head": (6, 950),
        "armF_up": (81, 898), "armF_lo": (150, 726), "handF": (212, 573),
        "armN_up": (-80, 900), "armN_lo": (-149, 728), "handN": (-211, 575),
    },
}

PARENT = dict(BONES)

# Display-only head proportion: the painted head and hair are drawn this much larger around the
# chin (the head bone), and the head sits this many units lower on a shorter neck.  The painted
# parts themselves are cut at the bind-pose size; only the exported atlas and previews apply it.
HEAD_SCALE = {"m": 1.05, "f": 1.05}
HEAD_DROP = {"m": 13.0, "f": 9.0}

# The bind pose is an A-pose: arms hang 22 degrees away from the body so painted sleeves
# never overlap the torso.  Angles are world degrees (counter-clockwise).
ARM_SPREAD = 22.0
WORLD_ANGLE = {"armN_up": -ARM_SPREAD, "armN_lo": -ARM_SPREAD, "handN": -ARM_SPREAD,
               "armF_up": ARM_SPREAD, "armF_lo": ARM_SPREAD, "handF": ARM_SPREAD}


def world_angle(name):
    return WORLD_ANGLE.get(name, 0.0)


def local(gender):
    """Bind position of each bone relative to its parent, in the parent's rotated frame."""
    import math
    w = WORLD[gender]
    out = {}
    for name, parent in BONES:
        x, y = w[name]
        if parent is None:
            out[name] = (x, y)
        else:
            px, py = w[parent]
            r = math.radians(-world_angle(parent))
            dx, dy = x - px, y - py
            out[name] = (dx * math.cos(r) - dy * math.sin(r), dx * math.sin(r) + dy * math.cos(r))
    return out


def local_angle(name):
    parent = PARENT[name]
    return world_angle(name) - (world_angle(parent) if parent else 0.0)


def length(gender, a, b):
    w = WORLD[gender]
    return ((w[a][0] - w[b][0]) ** 2 + (w[a][1] - w[b][1]) ** 2) ** .5


# Face anchors in head-bone space (sliders move features around these).
FACE = {
    "m": {"eye": (28, 86), "brow": (25, 107), "nose": (0, 54), "mouth": (0, 30), "mark": (0, 132)},
    "f": {"eye": (26, 82), "brow": (23, 102), "nose": (0, 51), "mouth": (0, 29), "mark": (0, 124)},
}

# Option counts (match the server's LOOK_STYLE_KEYS)
COUNTS = {"fa": 4, "ey": 8, "br": 5, "no": 4, "mo": 5, "bd": 5, "ha": 10, "to": 6, "hat": 6, "ma": 6}
