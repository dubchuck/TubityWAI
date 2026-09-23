using UnityEngine;

namespace TubityWAI
{
    /// <summary>
    /// Moves one tunnel segment during the level-to-level hand-off (see GameManager).
    ///
    /// Arrive: the segment starts `offset` away from where the generator put it (far down the
    /// tube, out in the fog) and eases into place, so the next level assembles out of the
    /// distance around the still-moving sphere.
    ///
    /// Depart: the previous level's segment swells outward about the sphere's axis until it is
    /// past the edges of the screen, then the segment is left for its owner to destroy.
    ///
    /// Removes itself when done. If anything else moves the segment meanwhile (the generator
    /// recycling it), it stops at once rather than fight over the transform. An arriving segment
    /// that has not built itself yet is left alone until it has, since it lays its contents out
    /// from wherever it stands when it does.
    /// </summary>
    public class SegmentFlyIn : MonoBehaviour
    {
        private enum Mode { Arrive, Depart }

        private Mode mode;
        private Vector3 rest;
        private Vector3 offset;
        private Vector2 pivot;
        private float endScale;
        private float delay;
        private float duration;
        private float elapsed;
        private Vector3 lastWritten;
        private TunnelSegment segment;
        private bool started;
        private bool unscaled;

        public static void Arrive(TunnelSegment segment, Vector3 offset, float delay, float duration)
        {
            SegmentFlyIn fly = segment.gameObject.AddComponent<SegmentFlyIn>();
            fly.mode = Mode.Arrive;
            fly.segment = segment;
            fly.offset = offset;
            fly.delay = delay;
            fly.duration = Mathf.Max(0.05f, duration);
            fly.TryStart();
        }

        /// <summary>Takes the rest position and jumps out to the start of the flight, once the
        /// segment has populated. An unpopulated segment has no mesh, so waiting shows nothing.</summary>
        private void TryStart()
        {
            if (started || (segment != null && !segment.IsPopulated)) return;
            started = true;
            rest = transform.position;
            Write(rest + offset);
        }

        /// <remarks>`unscaledTime` lets it play while the game is paused (after a crash).</remarks>
        public static void Depart(GameObject segment, Vector2 pivotXY, float endScale, float delay, float duration,
                                  bool unscaledTime = false)
        {
            SegmentFlyIn fly = segment.AddComponent<SegmentFlyIn>();
            fly.unscaled = unscaledTime;
            fly.mode = Mode.Depart;
            fly.rest = segment.transform.position;
            fly.pivot = pivotXY;
            fly.endScale = endScale;
            fly.delay = delay;
            fly.duration = Mathf.Max(0.05f, duration);
            fly.lastWritten = fly.rest;
            fly.started = true;
        }

        private void Update()
        {
            if (!started)
            {
                TryStart();
                if (!started) return;
            }
            if (transform.position != lastWritten) { Destroy(this); return; }

            elapsed += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01((elapsed - delay) / duration);

            if (mode == Mode.Arrive)
            {
                float ease = 1f - (1f - t) * (1f - t) * (1f - t);   // out-cubic: quick, then settles
                Write(rest + offset * (1f - ease));
            }
            else
            {
                float ease = t * t;                                  // in-quad: lets go, then goes
                float s = Mathf.Lerp(1f, endScale, ease);
                // Scale about the sphere's axis rather than the segment's origin, so the tube
                // opens evenly around the player instead of lurching to one side.
                Vector3 p = rest;
                p.x = pivot.x + (rest.x - pivot.x) * s;
                p.y = pivot.y + (rest.y - pivot.y) * s;
                transform.localScale = new Vector3(s, s, 1f);
                Write(p);
            }

            if (t >= 1f) Destroy(this);
        }

        private void Write(Vector3 position)
        {
            transform.position = position;
            lastWritten = position;
        }
    }
}
