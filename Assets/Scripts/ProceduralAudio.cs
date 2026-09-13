using UnityEngine;

namespace TubityWAI
{
    public static class ProceduralAudio
    {
        private static AudioClip coinClip;
        private static AudioClip crashClip;
        private static AudioClip breakClip;
        private static AudioClip acceptClip;
        private static AudioClip jumpClip;
        private static AudioClip doubleJumpClip;
        private static AudioClip speedUpClip;
        private static AudioClip speedDownClip;
        private static AudioClip magnetOnClip;
        private static AudioClip magnetOffClip;
        private static AudioClip menuForwardClip;
        private static AudioClip menuBackClip;
        private static AudioClip menuCloseClip;
        private static AudioClip menuSelectClip;

        private static AudioClip[] coinVariantClips;

        // Real recorded clips live in Assets/Resources/SFX and take priority; the
        // synthesized tones below only fire if a clip is missing from that folder.
        private static AudioClip LoadSfx(string name)
        {
            return Resources.Load<AudioClip>("SFX/" + name);
        }

        public static AudioClip GetCoinSound()
        {
            if (coinVariantClips == null)
            {
                var variants = new System.Collections.Generic.List<AudioClip>();
                AudioClip main = LoadSfx("Coin");
                AudioClip alt = LoadSfx("CoinAlt");
                if (main != null) variants.Add(main);
                if (alt != null) variants.Add(alt);
                coinVariantClips = variants.ToArray();
            }
            if (coinVariantClips.Length > 0)
            {
                return coinVariantClips[Random.Range(0, coinVariantClips.Length)];
            }
            // High-tech, pleasant blip/chirp
            if (coinClip == null) coinClip = CreateCyberBlip(0.15f, 1800f, 600f, 0.35f);
            return coinClip;
        }

        public static AudioClip GetAcceptSound()
        {
            AudioClip real = LoadSfx("Accept");
            if (real != null) return real;
            // Synthwave minor 7th chord (A4, E5, G5)
            if (acceptClip == null) acceptClip = CreateCyberChord(0.4f, new float[] { 440.00f, 659.25f, 783.99f }, 0.35f);
            return acceptClip;
        }

        public static AudioClip GetJumpSound()
        {
            AudioClip real = LoadSfx("Jump");
            if (real != null) return real;
            // Bass-heavy zap/whoosh
            if (jumpClip == null) jumpClip = CreateZap(0.2f, 300f, 80f, 0.3f);
            return jumpClip;
        }

        public static AudioClip GetDoubleJumpSound()
        {
            AudioClip real = LoadSfx("DoubleJump");
            if (real != null) return real;
            // Higher zap
            if (doubleJumpClip == null) doubleJumpClip = CreateZap(0.25f, 450f, 120f, 0.3f);
            return doubleJumpClip;
        }

        public static AudioClip GetSpeedUpSound()
        {
            AudioClip real = LoadSfx("SpeedUp");
            if (real != null) return real;
            if (speedUpClip == null) speedUpClip = CreateFMSweep(0.6f, 150f, 600f, 0.3f);
            return speedUpClip;
        }

        public static AudioClip GetSpeedDownSound()
        {
            AudioClip real = LoadSfx("SpeedDown");
            if (real != null) return real;
            if (speedDownClip == null) speedDownClip = CreateFMSweep(0.6f, 600f, 150f, 0.3f);
            return speedDownClip;
        }

        public static AudioClip GetMagnetOnSound()
        {
            AudioClip real = LoadSfx("MagnetOn");
            if (real != null) return real;
            if (magnetOnClip == null) magnetOnClip = CreatePulsingTone(0.6f, 300f, 0.3f, true);
            return magnetOnClip;
        }

        public static AudioClip GetMagnetOffSound()
        {
            AudioClip real = LoadSfx("MagnetOff");
            if (real != null) return real;
            if (magnetOffClip == null) magnetOffClip = CreatePulsingTone(0.6f, 300f, 0.3f, false);
            return magnetOffClip;
        }

        public static AudioClip GetCrashSound()
        {
            AudioClip real = LoadSfx("Crash");
            if (real != null)
            {
                Debug.Log("[ProceduralAudio] Crash: using real clip '" + real.name + "' (length " + real.length + "s, channels " + real.channels + ").");
                return real;
            }
            Debug.LogWarning("[ProceduralAudio] Crash: Resources.Load(\"SFX/Crash\") returned null, using procedural fallback.");
            if (crashClip == null) crashClip = CreateHeavyNoise(0.8f, 0.5f, 3f);
            return crashClip;
        }

        public static AudioClip GetBreakSound()
        {
            AudioClip real = LoadSfx("Break");
            if (real != null) return real;
            if (breakClip == null) breakClip = CreateHeavyNoise(0.3f, 0.4f, 8f);
            return breakClip;
        }

        // --- MENU UI SOUNDS ---
        // Named by navigation intent rather than by button, so any button that advances
        // a layer, retreats one, dismisses an overlay entirely, or just changes a value
        // in place sounds consistent across the whole menu system.

        /// <summary>Advancing deeper into the menu flow (opening a submenu, confirming, launching a level).</summary>
        public static AudioClip GetMenuForwardSound()
        {
            AudioClip real = LoadSfx("MenuForward");
            if (real != null) return real;
            // Quick ascending sweep - reads as "moving into" a screen
            if (menuForwardClip == null) menuForwardClip = CreateFMSweep(0.12f, 500f, 1100f, 0.25f);
            return menuForwardClip;
        }

        /// <summary>Retreating to the previous layer (a screen's own Back button).</summary>
        public static AudioClip GetMenuBackSound()
        {
            AudioClip real = LoadSfx("MenuBack");
            if (real != null) return real;
            // Quick descending sweep - the mirror image of Forward
            if (menuBackClip == null) menuBackClip = CreateFMSweep(0.12f, 900f, 400f, 0.22f);
            return menuBackClip;
        }

        /// <summary>Dismissing an overlay entirely (closing a popup, not just stepping back a layer).</summary>
        public static AudioClip GetMenuCloseSound()
        {
            AudioClip real = LoadSfx("MenuClose");
            if (real != null) return real;
            // Low, damped thump - more final than Back
            if (menuCloseClip == null) menuCloseClip = CreateZap(0.16f, 260f, 90f, 0.3f);
            return menuCloseClip;
        }

        /// <summary>Choosing or toggling a value without changing layers (tabs, paging, list items).</summary>
        public static AudioClip GetMenuSelectSound()
        {
            AudioClip real = LoadSfx("MenuSelect");
            if (real != null) return real;
            // Light neutral tick
            if (menuSelectClip == null) menuSelectClip = CreateCyberBlip(0.08f, 1200f, 1000f, 0.2f);
            return menuSelectClip;
        }

        // --- SYNTHESIS METHODS (fallback only, used when a real clip is missing) ---

        private static AudioClip CreateCyberBlip(float duration, float startFreq, float endFreq, float maxVol)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[sampleCount];

            float phase = 0f;
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleCount;
                // Exponential pitch drop
                float freq = Mathf.Lerp(startFreq, endFreq, t * t * t); 
                phase += 2 * Mathf.PI * freq / sampleRate;
                
                // Mix sine and square
                float sine = Mathf.Sin(phase);
                float square = sine > 0 ? 0.3f : -0.3f;
                float sample = (sine + square) * 0.7f;

                float amplitude = Mathf.Exp(-t * 10f); // snappy
                data[i] = sample * amplitude * maxVol;
            }
            AudioClip clip = AudioClip.Create("CyberBlip", sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateCyberChord(float duration, float[] freqs, float maxVol)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[sampleCount];
            float[] phases = new float[freqs.Length];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleCount;
                float sample = 0f;

                for (int j = 0; j < freqs.Length; j++)
                {
                    phases[j] += 2 * Mathf.PI * freqs[j] / sampleRate;
                    // Sawtooth-like wave (warm synth) using sine + 2nd harmonic
                    float wave = Mathf.Sin(phases[j]) + 0.5f * Mathf.Sin(2f * phases[j]);
                    sample += wave;
                }
                
                sample /= freqs.Length; // Normalize

                // Smooth attack and decay
                float amplitude = 1f;
                if (t < 0.1f) amplitude = t / 0.1f;
                else amplitude = Mathf.Exp(-(t - 0.1f) * 4f);

                data[i] = sample * amplitude * maxVol;
            }

            AudioClip clip = AudioClip.Create("CyberChord", sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateZap(float duration, float startFreq, float endFreq, float maxVol)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[sampleCount];

            float phase = 0f;
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleCount;
                float freq = Mathf.Lerp(startFreq, endFreq, t * t);
                phase += 2 * Mathf.PI * freq / sampleRate;
                
                // Sawtooth for aggressive zap
                float sample = 2f * (phase / (2f * Mathf.PI) - Mathf.Floor(phase / (2f * Mathf.PI) + 0.5f));
                
                // Add some noise
                sample += Random.Range(-0.2f, 0.2f);

                float amplitude = Mathf.Exp(-t * 7f);
                data[i] = sample * amplitude * maxVol;
            }
            AudioClip clip = AudioClip.Create("Zap", sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateFMSweep(float duration, float startFreq, float endFreq, float maxVol)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[sampleCount];

            float phase = 0f;
            float modPhase = 0f;
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleCount;
                float freq = Mathf.Lerp(startFreq, endFreq, t);
                
                modPhase += 2 * Mathf.PI * (freq * 2.5f) / sampleRate; // Modulator
                float fmAmount = 400f * (1f - t); // Decreasing FM over time
                
                phase += 2 * Mathf.PI * freq / sampleRate + Mathf.Sin(modPhase) * (fmAmount / sampleRate);
                
                float sample = Mathf.Sin(phase);
                
                float amplitude = 1f;
                if (t < 0.1f) amplitude = t / 0.1f;
                else if (t > 0.8f) amplitude = 1f - ((t - 0.8f) / 0.2f);

                data[i] = sample * amplitude * maxVol;
            }
            AudioClip clip = AudioClip.Create("FMSweep", sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreatePulsingTone(float duration, float freq, float maxVol, bool pitchUp)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[sampleCount];

            float phase = 0f;
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleCount;
                
                float targetFreq = pitchUp ? Mathf.Lerp(freq, freq * 1.5f, t) : Mathf.Lerp(freq * 1.5f, freq, t);
                phase += 2 * Mathf.PI * targetFreq / sampleRate;
                
                // Sine + Square for "techno" feel
                float sine = Mathf.Sin(phase);
                float square = sine > 0 ? 0.3f : -0.3f;
                float sample = (sine + square) * 0.7f;
                
                // Fast LFO for pulsing effect (16 Hz)
                float lfo = 0.5f + 0.5f * Mathf.Sin(2 * Mathf.PI * 16f * t);

                float amplitude = pitchUp ? Mathf.Lerp(0f, 1f, t) : Mathf.Lerp(1f, 0f, t);

                data[i] = sample * lfo * amplitude * maxVol;
            }
            AudioClip clip = AudioClip.Create("PulsingTone", sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateHeavyNoise(float duration, float maxVol, float decay)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleCount;
                
                // "Bitcrushed" noise
                float sample = Random.Range(-1f, 1f);
                sample = Mathf.Round(sample * 4f) / 4f; // Distort/Bitcrush
                
                float amplitude = Mathf.Exp(-t * decay);
                data[i] = sample * amplitude * maxVol;
            }

            AudioClip clip = AudioClip.Create("HeavyNoise", sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
