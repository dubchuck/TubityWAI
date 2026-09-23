using System.Collections.Generic;
using UnityEngine;

namespace TubityWAI
{
    /// <summary>
    /// A journey past named celestial bodies: the world-space landmarks plus the environment blend
    /// whose stops carry the sky, fog and sun state at each point along it. LevelConfig takes one of
    /// these and hands the bodies to CelestialBodies and the blend to the usual environment plumbing.
    /// </summary>
    public class CelestialRoute
    {
        public readonly List<CelestialBody> bodies = new List<CelestialBody>();
        public EnvironmentBlend blend;

        /// <summary>Distance at which the last body sits, so a level can put its finish gate there.</summary>
        public float EndDistance;
    }

    /// <summary>
    /// The Uranus-to-Earth traverse: outward planets first, then the asteroid belt, then the inner
    /// system, finishing alongside the Moon with Earth filling the view ahead.
    ///
    /// Three things carry the sense of scale, and all of them are tuning rather than new machinery:
    ///
    /// 1. Lateral offset sets apparent size. A body stays on screen roughly while it is further ahead
    ///    than it is off to the side, so it peaks at about atan(radius / 1.41*|offset|) and then
    ///    sweeps out of frame. Pushing a body sideways makes it both more distant and smaller at its
    ///    best moment, which is how Mars stays a pinprick while Saturn fills half the screen.
    ///
    /// 2. Fog is the reveal. The route's fogEnd is ~1800 units against a tunnel only ~120 units long,
    ///    so the tube itself is never fogged and the entire fog ramp is spent on the landmarks. Earth
    ///    emerges from black around z=2950 and takes 1700 units to resolve; that long, slow fade is
    ///    what makes the gaps between planets read as distance rather than as empty flying.
    ///
    /// 3. The sun grows. Its angular size is set per stop from the real heliocentric distance of each
    ///    waypoint, compressed: true scale would make the sun at Uranus a sub-pixel dot, and the brief
    ///    is that it stays present throughout. The values below run about 6x in apparent radius from
    ///    Uranus to Earth against a true ratio of 19x, which reads as steady growth without ever
    ///    losing it against the star field.
    ///
    /// The whole route is one SolarSystem theme apart from the belt, which is its own theme so the
    /// blend's per-prop dissolve can thin its rocks in and out at the edges.
    /// </summary>
    public static class SolarSystemRoute
    {
        // Waypoint distances along the tube. Bodies sit abeam these; the palette stops share them so
        // the sun is the right size exactly when the matching planet is alongside.
        private const float Uranus = 620f;
        private const float Saturn = 1500f;
        private const float Jupiter = 2400f;
        private const float BeltIn = 2700f;
        private const float BeltOut = 3300f;
        private const float Mars = 3800f;
        private const float MoonPass = 4620f;
        private const float EarthAt = 4750f;

        /// <summary>Where the finish gate goes: alongside the Moon, with Earth still ahead and huge.</summary>
        public const float FinishDistance = 4400f;

        public static CelestialRoute Build()
        {
            CelestialRoute route = new CelestialRoute();
            route.EndDistance = EarthAt;
            AddBodies(route.bodies);
            route.blend = BuildBlend();
            return route;
        }

        // ------------------------------------------------------------------
        // Bodies
        // ------------------------------------------------------------------
        private static void AddBodies(List<CelestialBody> bodies)
        {
            // Offsets keep every body clear of the sun's corner of the sky. The sun sits fixed at
            // roughly 26 degrees left and 19 up (EnvironmentPalettes.SunDirection), so the route
            // sweeps its planets out to the right and down instead, and saves the upper right for
            // Earth at the end.

            // Uranus: rolled onto its side, as it actually is, which puts its rings edge-on across
            // the approach and makes the tilt read without a caption.
            bodies.Add(new CelestialBody
            {
                name = "Uranus",
                distance = Uranus,
                offset = new Vector2(235f, 85f),
                radius = 70f,
                axialTilt = 98f,
                spinSpeed = 1.6f,
                selfGlow = 0.14f,
                smoothness = 0.30f,
                bandCount = 5,
                bands = new[]
                {
                    new Color(0.42f, 0.74f, 0.78f), new Color(0.58f, 0.86f, 0.88f),
                    new Color(0.28f, 0.58f, 0.66f), new Color(0.72f, 0.94f, 0.94f)
                },
                atmosphere = new Color(0.45f, 0.82f, 0.86f, 0.10f)
            }.WithRing(new CelestialRing
            {
                innerScale = 1.55f, outerScale = 1.95f,
                inner = new Color(0.40f, 0.62f, 0.68f), outer = new Color(0.22f, 0.36f, 0.42f),
                opacity = 0.35f, segments = 48
            }));

            // Saturn: the widest thing on the route. The rings reach 2.35x its radius, so at closest
            // approach they span most of the screen - the payoff shot of the outer system.
            bodies.Add(new CelestialBody
            {
                name = "Saturn",
                distance = Saturn,
                offset = new Vector2(-400f, -130f),
                radius = 110f,
                axialTilt = 27f,
                spinSpeed = 3.2f,
                selfGlow = 0.16f,
                bandCount = 9,
                bands = new[]
                {
                    new Color(0.72f, 0.60f, 0.38f), new Color(0.90f, 0.82f, 0.62f),
                    new Color(0.54f, 0.42f, 0.26f), new Color(0.98f, 0.93f, 0.78f)
                }
            }.WithRing(new CelestialRing
            {
                innerScale = 1.32f, outerScale = 2.35f,
                inner = new Color(0.92f, 0.85f, 0.68f), outer = new Color(0.55f, 0.47f, 0.36f),
                opacity = 0.80f, segments = 72
            })
             .WithMoon(new CelestialMoon
             {
                 name = "Prometheus", radius = 7f, orbitRadius = 136f,
                 orbitPhase = 40f, orbitSpeed = 5f, tint = new Color(0.78f, 0.74f, 0.68f)
             })
             .WithMoon(new CelestialMoon
             {
                 name = "Pandora", radius = 6.5f, orbitRadius = 276f,
                 orbitPhase = 215f, orbitSpeed = -3.4f, tint = new Color(0.72f, 0.69f, 0.64f)
             }));

            // Jupiter: the largest body, with Io and Europa out at a radius that reads as separation
            // rather than as lumps on the limb.
            bodies.Add(new CelestialBody
            {
                name = "Jupiter",
                distance = Jupiter,
                offset = new Vector2(300f, -90f),
                radius = 130f,
                axialTilt = 8f,
                spinSpeed = 4.5f,
                selfGlow = 0.18f,
                bandCount = 11,
                bands = new[]
                {
                    new Color(0.62f, 0.40f, 0.26f), new Color(0.92f, 0.82f, 0.68f),
                    new Color(0.44f, 0.24f, 0.16f), new Color(0.98f, 0.90f, 0.78f)
                }
            }
             .WithMoon(new CelestialMoon
             {
                 name = "Io", radius = 11f, orbitRadius = 215f,
                 orbitPhase = 25f, orbitSpeed = 4.2f, tint = new Color(0.94f, 0.86f, 0.44f), selfGlow = 0.18f
             })
             .WithMoon(new CelestialMoon
             {
                 name = "Europa", radius = 10f, orbitRadius = 320f,
                 orbitPhase = 190f, orbitSpeed = -2.8f, tint = new Color(0.86f, 0.88f, 0.92f), selfGlow = 0.16f
             }));

            // Mars: deliberately small and far off to the side. After Jupiter it should feel like a
            // step down in scale, because that is the actual shape of the inner system.
            bodies.Add(new CelestialBody
            {
                name = "Mars",
                distance = Mars,
                offset = new Vector2(195f, 120f),
                radius = 34f,
                axialTilt = 25f,
                spinSpeed = 3f,
                selfGlow = 0.15f,
                bandCount = 6,
                bands = new[]
                {
                    new Color(0.52f, 0.24f, 0.13f), new Color(0.80f, 0.44f, 0.26f),
                    new Color(0.34f, 0.15f, 0.09f), new Color(0.90f, 0.66f, 0.50f)
                },
                atmosphere = new Color(0.85f, 0.52f, 0.36f, 0.05f)
            });

            // The Moon: the closest pass on the route, and where the finish gate sits. No emission to
            // speak of and no atmosphere, so it stays a hard grey crescent against Earth.
            bodies.Add(new CelestialBody
            {
                name = "Moon",
                distance = MoonPass,
                offset = new Vector2(-95f, -95f),
                radius = 70f,
                axialTilt = 6f,
                spinSpeed = 0.8f,
                selfGlow = 0.07f,
                smoothness = 0.10f,
                bandCount = 4,
                bands = new[]
                {
                    new Color(0.30f, 0.29f, 0.28f), new Color(0.62f, 0.61f, 0.59f),
                    new Color(0.20f, 0.19f, 0.19f), new Color(0.76f, 0.75f, 0.73f)
                }
            });

            // Earth: sits past the finish line and well out to the upper right, so it is still ahead
            // of the player when the level ends. At the gate it spans roughly 43 degrees - the single
            // biggest thing on the route, and the only one you never fly past.
            bodies.Add(new CelestialBody
            {
                name = "Earth",
                distance = EarthAt,
                offset = new Vector2(150f, 20f),
                radius = 150f,
                axialTilt = 23.4f,
                spinSpeed = 1.4f,
                selfGlow = 0.22f,
                smoothness = 0.42f,
                bandCount = 8,
                bands = new[]
                {
                    new Color(0.06f, 0.20f, 0.48f), new Color(0.16f, 0.44f, 0.72f),
                    new Color(0.22f, 0.42f, 0.24f), new Color(0.92f, 0.95f, 0.98f)
                },
                atmosphere = new Color(0.34f, 0.62f, 1f, 0.16f)
            });
        }

        // ------------------------------------------------------------------
        // Sky / sun ramp
        // ------------------------------------------------------------------

        /// <summary>
        /// One palette stop. <paramref name="sunSize"/> is the shader's disc parameter, whose apparent
        /// radius goes as its square root, so the values climb steeply to read as linear growth.
        /// </summary>
        private static EnvironmentPalette Stop(EnvironmentTheme theme, string name,
                                               float sunSize, float halo, float lightIntensity,
                                               Color sunTint, Color ambient, float fogEnd)
        {
            EnvironmentPalette p = EnvironmentPalettes.Get(theme).Clone();
            p.displayName = name;
            p.sunSize = sunSize;
            p.sunHalo = halo;
            p.sunColor = sunTint;
            p.sunLightColor = Color.Lerp(Color.white, sunTint, 0.35f);
            p.sunLightIntensity = lightIntensity;
            p.fogEnd = fogEnd;

            // Ambient climbs with the sun: out past Saturn the shadow side of a rock is genuinely
            // black, while near Earth there is enough scattered light to read detail in it.
            p.ambientLight = ambient;
            p.ambientSky = ambient * 1.5f;
            p.ambientGround = Color.Lerp(ambient, p.accentColor, 0.12f);
            return p;
        }

        private static EnvironmentBlend BuildBlend()
        {
            // Heliocentric distances of each waypoint, and the sun's apparent size there. Real ratios
            // relative to Earth are 0.052 (Uranus), 0.105 (Saturn), 0.19 (Jupiter), 0.37 (belt),
            // 0.66 (Mars); these are compressed so the sun is still a visible disc at the start.
            Color coldSun = new Color(2.0f, 2.05f, 2.2f);      // small and blue-white this far out
            Color warmSun = new Color(2.9f, 2.72f, 2.40f);     // broad and yellow-white near Earth

            return EnvironmentBlend.From(Stop(EnvironmentTheme.SolarSystem, "Uranus Orbit",
                                              0.0022f, 0.60f, 0.28f, coldSun,
                                              new Color(0.030f, 0.034f, 0.052f), 2000f))

                // Saturn, 9.5 AU. The sun has roughly doubled in apparent width since the start.
                .To(Stop(EnvironmentTheme.SolarSystem, "Saturn Orbit",
                         0.0040f, 0.80f, 0.50f, coldSun,
                         new Color(0.038f, 0.042f, 0.060f), 1950f), Saturn)

                // Jupiter, 5.2 AU.
                .To(Stop(EnvironmentTheme.SolarSystem, "Jovian Space",
                         0.0070f, 0.95f, 0.68f, Color.Lerp(coldSun, warmSun, 0.25f),
                         new Color(0.048f, 0.050f, 0.066f), 1900f), Jupiter)

                // Into the belt. A separate theme, so its rocks dissolve in over the last 400 units
                // rather than appearing all at once.
                .To(Stop(EnvironmentTheme.AsteroidBelt, "Asteroid Belt",
                         0.012f, 1.10f, 0.86f, Color.Lerp(coldSun, warmSun, 0.45f),
                         new Color(0.072f, 0.070f, 0.076f), 1400f), BeltIn, blendLength: 550f)

                .To(Stop(EnvironmentTheme.AsteroidBelt, "Inner Belt",
                         0.016f, 1.20f, 0.92f, Color.Lerp(coldSun, warmSun, 0.55f),
                         new Color(0.078f, 0.075f, 0.078f), 1400f), BeltOut)

                // Out of the belt and past Mars, 1.52 AU.
                .To(Stop(EnvironmentTheme.SolarSystem, "Martian Space",
                         0.030f, 1.35f, 1.05f, Color.Lerp(coldSun, warmSun, 0.80f),
                         new Color(0.070f, 0.066f, 0.072f), 1700f), Mars, blendLength: 450f)

                // Earth, 1 AU: the sun is now a proper disc and lights everything properly.
                .To(Stop(EnvironmentTheme.SolarSystem, "Earth Orbit",
                         0.058f, 1.55f, 1.25f, warmSun,
                         new Color(0.090f, 0.092f, 0.104f), 1700f), MoonPass);
        }
    }
}
