"""Neon band sphere - the generator, shared with NeonArcSphere.cs.

Reading the concept art again: the arcs are not a criss-cross cage. They are
*latitude bands wound around a tilted axis*, lying on the surface of a dark
sphere - which is why the poles read as two nested-circle "eyes" and why the
bands on the far side are hidden. A second, sparser family around a different
axis supplies the diagonals that cross the main winding.

Because the bands sit on the sphere, visibility is exact and free: a point is
seen when it faces the camera. No depth buffer needed, and in Unity the opaque
core sphere does the same job.
"""
import numpy as np

PALETTE = [
    (0.00, 0.95, 1.00),   # cyan
    (0.10, 0.45, 1.00),   # azure
    (0.40, 0.15, 1.00),   # indigo
    (0.78, 0.10, 1.00),   # violet
    (1.00, 0.08, 0.72),   # magenta
]
GOLD = (1.00, 0.70, 0.18)
CORE = (0.020, 0.022, 0.055)     # the dark ball the bands are wound onto

CFG = dict(
    # main winding
    bands=23,
    axis_tilt=52.0,           # degrees from vertical
    axis_spin=38.0,          # degrees about the view axis
    pole_bias=0.55,           # <1 clusters bands toward the poles (the "eyes")
    # crossing family
    cross_bands=4,
    cross_tilt=64.0,
    cross_spin=72.0,
    # band shape
    width_min=0.010,          # tube radius as a fraction of sphere radius
    width_max=0.038,
    gap_jitter=0.55,          # how uneven the spacing is
    lift=1.012,               # bands sit just proud of the surface
    gold_every=5,
    segments=128,
    seed=20260909,
)


def _axis(tilt_deg, spin_deg):
    t, s = np.radians(tilt_deg), np.radians(spin_deg)
    return np.array([np.sin(t) * np.sin(s), np.cos(t), -np.sin(t) * np.cos(s)])


def _basis(n):
    a = np.array([0.0, 0.0, 1.0]) if abs(n[2]) < 0.9 else np.array([0.0, 1.0, 0.0])
    u = np.cross(n, a); u /= np.linalg.norm(u)
    return u, np.cross(n, u)


def _family(axis, thetas, widths, colors, cfg):
    """Latitude rings about `axis`, each lying on the unit sphere."""
    u, v = _basis(axis)
    out = []
    for theta, width, col in zip(thetas, widths, colors):
        ring_r = np.sin(theta) * cfg["lift"]
        centre = axis * np.cos(theta) * cfg["lift"]
        seg = max(24, int(cfg["segments"] * max(ring_r, 0.15)))
        t = np.linspace(0, 2 * np.pi, seg, endpoint=True)
        pts = centre + ring_r * (np.outer(np.cos(t), u) + np.outer(np.sin(t), v))
        cols = np.repeat(np.asarray(col)[None, :], seg, axis=0)
        out.append((pts, cols, width))
    return out


def band_set(cfg=None):
    """[(points[N,3], colors[N,3], tube_radius)] for every band."""
    c = dict(CFG); c.update(cfg or {})
    rng = np.random.default_rng(c["seed"])

    # uneven latitudes, biased toward the poles so the winding looks wound
    steps = rng.uniform(1.0 - c["gap_jitter"], 1.0 + c["gap_jitter"], c["bands"])
    acc = np.cumsum(steps)
    lat = (acc - acc[0]) / (acc[-1] - acc[0])          # 0..1
    lat = lat ** c["pole_bias"] if c["pole_bias"] != 1 else lat
    thetas = 0.10 + lat * (np.pi - 0.20)

    widths, colors = [], []
    for i in range(c["bands"]):
        widths.append(rng.uniform(c["width_min"], c["width_max"]))
        if c["gold_every"] > 0 and i % c["gold_every"] == c["gold_every"] - 1:
            colors.append(GOLD)
        else:
            f = (i / max(1, c["bands"] - 1)) * (len(PALETTE) - 1)
            k = int(np.floor(f)); frac = f - k
            k2 = min(k + 1, len(PALETTE) - 1)
            colors.append(tuple(np.array(PALETTE[k]) * (1 - frac)
                                + np.array(PALETTE[k2]) * frac))

    bands = _family(_axis(c["axis_tilt"], c["axis_spin"]), thetas, widths, colors, c)

    if c["cross_bands"] > 0:
        ct = np.linspace(0.7, np.pi - 0.7, c["cross_bands"])
        cw = rng.uniform(c["width_min"], c["width_max"] * 0.7, c["cross_bands"])
        cc = [PALETTE[0], PALETTE[4], PALETTE[1], GOLD][:c["cross_bands"]]
        bands += _family(_axis(c["cross_tilt"], c["cross_spin"]), ct, cw, cc, c)

    return bands


def visible(pts, cam, centre=None):
    """A point on the sphere is seen when its outward normal faces the camera.
    `centre` matters once instances are offset - the count selector's formation."""
    c = np.zeros(3) if centre is None else np.asarray(centre, float)
    rel = pts - c[None, :]
    n = rel / np.maximum(np.linalg.norm(rel, axis=1, keepdims=True), 1e-6)
    return np.einsum("ij,ij->i", n, cam[None, :] - pts) > 0.0
