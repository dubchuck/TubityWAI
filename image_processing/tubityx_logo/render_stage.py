"""Software preview of the attract-screen hero: neon arc sphere, glossy pad and
its stencil-clipped reflection. Faithful because the whole thing is unlit and
additive - there is no lighting model to approximate, only bloom."""
import numpy as np
from scipy.ndimage import gaussian_filter

W, H = 1024, 1024
FOV = 40.0
CAM = np.array([0.0, 1.55, -10.4])
LOOK = np.array([0.0, -0.05, 0.0])
PLANE_Y = -1.18
PAD_HALF = 1.78            # half diagonal of the square pad
PAD_ROT = np.pi / 4        # corner toward the camera


def _view():
    f = LOOK - CAM; f /= np.linalg.norm(f)
    r = np.cross(f, [0, 1, 0]); r /= np.linalg.norm(r)
    return np.stack([r, np.cross(r, f), -f])


VM = _view()
FOCAL = (H * 0.5) / np.tan(np.radians(FOV) * 0.5)


def set_camera(cam, look, fov, size=None):
    """Point the preview at the real attract-mode camera.

    Everything here is in sphere radii; Unity works in world units, so divide
    world distances by heroSphereRadius before passing them in."""
    global CAM, LOOK, FOV, VM, FOCAL, W, H
    CAM = np.asarray(cam, float)
    LOOK = np.asarray(look, float)
    FOV = float(fov)
    if size is not None:
        W, H = size
    VM = _view()
    FOCAL = (H * 0.5) / np.tan(np.radians(FOV) * 0.5)


def attract_camera(distance_world, radius=0.9, pad_y=-1.58, hover=0.18,
                   pitch_deg=11.0, fov=60.0, size=(1280, 720)):
    """The camera CameraController actually uses in attraction mode: at the
    origin, pitched down 11 degrees, 60 degree field of view."""
    centre_y = pad_y + radius * (1.0 + hover)          # world Y of the sphere
    cam = np.array([0.0, -centre_y, -distance_world]) / radius
    p = np.radians(pitch_deg)
    fwd = np.array([0.0, -np.sin(p), np.cos(p)])
    set_camera(cam, cam + fwd * (distance_world / radius), fov, size)


def project(p):
    q = (np.asarray(p, float) - CAM) @ VM.T
    z = -q[..., 2]
    safe = np.maximum(z, 1e-4)
    return q[..., 0] / safe * FOCAL + W * 0.5, -q[..., 1] / safe * FOCAL + H * 0.5, z


def draw_arc(layer, pts, cols, tube, gain=1.0, depth_fade=0.42):
    """One arc, max-combined along its own length so overlapping segments do
    not stack (they are one continuous tube, not many lights)."""
    layer[:] = 0.0
    x, y, z = project(pts)
    ok = z > 0.05
    px_r = np.where(ok, tube * FOCAL / np.maximum(z, 1e-4), 0.0)
    zmid = float(z.mean())
    for i in range(len(pts) - 1):
        if not (ok[i] and ok[i + 1]):
            continue
        r = max(px_r[i], px_r[i + 1])
        if r < 0.05:
            continue
        halo = r * 3.0 + 3.0
        x0, y0, x1, y1 = x[i], y[i], x[i + 1], y[i + 1]
        lo_x = int(max(0, min(x0, x1) - halo)); hi_x = int(min(W, max(x0, x1) + halo + 1))
        lo_y = int(max(0, min(y0, y1) - halo)); hi_y = int(min(H, max(y0, y1) + halo + 1))
        if lo_x >= hi_x or lo_y >= hi_y:
            continue
        gy, gx = np.mgrid[lo_y:hi_y, lo_x:hi_x]
        dx, dy = x1 - x0, y1 - y0
        L2 = dx * dx + dy * dy
        t = 0.0 if L2 < 1e-9 else np.clip(((gx - x0) * dx + (gy - y0) * dy) / L2, 0, 1)
        d = np.hypot(gx - (x0 + t * dx), gy - (y0 + t * dy))
        core = np.clip((r + 0.15 - d) / 1.0, 0, 1)
        halo_v = np.exp(-(d / (r * 2.0 + 1.5)) ** 2) * 0.20
        fade = 1.0 - depth_fade * np.clip((z[i] - zmid) / 1.1, -1, 1)
        c = (cols[i] + cols[i + 1]) * 0.5 * gain * max(fade, 0.18)
        np.maximum(layer[lo_y:hi_y, lo_x:hi_x],
                   (core + halo_v)[..., None] * c[None, None, :],
                   out=layer[lo_y:hi_y, lo_x:hi_x])


def pad_corners():
    c, s = np.cos(PAD_ROT), np.sin(PAD_ROT)
    k = PAD_HALF / np.sqrt(2)
    pts = []
    for sx, sz in ((-1, -1), (1, -1), (1, 1), (-1, 1)):
        x, z = sx * k, sz * k
        pts.append([x * c - z * s, PLANE_Y, x * s + z * c])
    return np.array(pts)


def pad_mask():
    px, py, _ = project(pad_corners())
    poly = np.stack([px, py], axis=1)
    gy, gx = np.mgrid[0:H, 0:W]
    inside = np.ones((H, W), bool)
    edge = np.full((H, W), 1e9)
    for i in range(4):
        a, b = poly[i], poly[(i + 1) % 4]
        e = b - a
        n = np.array([e[1], -e[0]]); n /= np.linalg.norm(n)
        d = (gx - a[0]) * n[0] + (gy - a[1]) * n[1]
        inside &= d <= 0
        edge = np.minimum(edge, np.abs(d))
    return inside, edge


def visible_runs(pts, cols, centre=None, min_len=2):
    """Split a ring into the contiguous stretches facing the camera."""
    from arcsphere import visible
    vis = visible(pts, CAM, centre)
    runs, start = [], None
    for i, v in enumerate(vis):
        if v and start is None:
            start = i
        elif not v and start is not None:
            if i - start >= min_len:
                runs.append((pts[start:i], cols[start:i]))
            start = None
    if start is not None and len(vis) - start >= min_len:
        runs.append((pts[start:], cols[start:]))
    return runs


def pad_mask_offset(drop):
    c = pad_corners(); c[:, 1] -= drop
    px, py, _ = project(c)
    poly = np.stack([px, py], axis=1)
    gy, gx = np.mgrid[0:H, 0:W]
    inside = np.ones((H, W), bool)
    for i in range(4):
        a, b = poly[i], poly[(i + 1) % 4]
        e = b - a
        n = np.array([e[1], -e[0]]); n /= np.linalg.norm(n)
        inside &= ((gx - a[0]) * n[0] + (gy - a[1]) * n[1]) <= 0
    return inside


def render(cfg=None, reflect=True, show_pad=True, exposure=1.0, instances=None):
    """instances: [(centre_xyz, scale)] in sphere-radius units. Defaults to one
    sphere at the origin - the hero. The count selector passes its formation."""
    from arcsphere import band_set, visible, CORE
    base = band_set(cfg)
    if instances is None:
        instances = [((0.0, 0.0, 0.0), 1.0)]
    arcs = []
    for centre, sc in instances:
        c = np.asarray(centre, float)
        for pts, cols, tube in base:
            arcs.append((pts * sc + c, cols, tube * sc, c))
    buf = np.zeros((H, W, 3))
    layer = np.zeros((H, W, 3))

    gyb, gxb = np.mgrid[0:H, 0:W]
    rball = np.full((H, W), 9e9)
    for centre, sc in instances:
        cx, cy, cz = project(np.array([list(centre)], dtype=float))
        if cz[0] <= 0.05:
            continue
        px = sc * FOCAL / cz[0]
        rball = np.minimum(rball, np.hypot(gxb - cx[0], gyb - cy[0]) / max(px, 1e-4))

    if show_pad:
        inside, edge = pad_mask()
        gy = np.mgrid[0:H, 0:W][0]
        # polished dark metal: a touch brighter toward the far edge
        sheen = (0.030 - 0.024 * (gy / H))[..., None] * np.array([0.40, 0.52, 0.85])
        buf += inside[..., None] * sheen
        buf += (np.exp(-(edge / 1.6) ** 2) * inside)[..., None] * np.array([0.35, 0.95, 1.70])
        # slab thickness: the same quad dropped down, drawn as dark side faces
        thick = pad_mask_offset(0.11)
        side = thick & ~inside
        buf += side[..., None] * np.array([0.012, 0.014, 0.030])

    ball = (rball <= 1.0)
    shade = np.clip(1.0 - rball ** 2, 0, 1) ** 0.35
    buf = np.where(ball[..., None],
                   np.asarray(CORE)[None, None, :] * (0.45 + 0.55 * shade[..., None]),
                   buf)

    if reflect and show_pad:
        inside, _ = pad_mask()
        for pts, cols, tube, ctr in arcs:
            for seg_p, seg_c in visible_runs(pts, cols, ctr):
                m = seg_p.copy()
                m[:, 1] = 2 * PLANE_Y - m[:, 1]
                fade = np.clip(1.0 - (PLANE_Y - m[:, 1]) / 2.8, 0, 1) ** 1.3
                draw_arc(layer, m, seg_c * fade[:, None] * 0.58, tube * 1.05, gain=1.0)
                buf += layer * inside[..., None]

    for pts, cols, tube, ctr in arcs:
        for seg_p, seg_c in visible_runs(pts, cols, ctr):
            draw_arc(layer, seg_p, seg_c, tube, gain=1.0)
            buf += layer

    # faint rim on the dark ball so its silhouette reads against the tunnel
    rim = np.clip(1.0 - np.abs(rball - 0.985) / 0.05, 0, 1) * (rball < 1.02)
    buf += rim[..., None] * np.array([0.10, 0.22, 0.45])

    bright = np.maximum(buf - 0.80, 0)
    out = buf.copy()
    for sigma, amt in ((5, 0.34), (16, 0.26), (48, 0.18)):
        out += gaussian_filter(bright, sigma=(sigma, sigma, 0)) * amt
    return np.clip(1.0 - np.exp(-out * exposure * 0.85), 0, 1) ** (1 / 2.2)


if __name__ == "__main__":
    from PIL import Image
    Image.fromarray((render() * 255).astype(np.uint8)).save("stage.png")
    print("ok")
