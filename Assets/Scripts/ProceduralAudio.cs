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

        public static AudioClip GetCoinSound()
        {
            if (coinClip != null) return coinClip;
            // High-tech, pleasant blip/chirp
            coinClip = CreateCyberBlip(0.15f, 1800f, 600f, 0.35f);
            return coinClip;
        }

        public static AudioClip GetAcceptSound()
        {
            if (acceptClip != null) return acceptClip;
            // Synthwave minor 7th chord (A4, E5, G5)
            acceptClip = CreateCyberChord(0.4f, new float[] { 440.00f, 659.25f, 783.99f }, 0.35f);
            return acceptClip;
        }

        public static AudioClip GetJumpSound()
        {
            if (jumpClip != null) return jumpClip;
            // Bass-heavy zap/whoosh
            jumpClip = CreateZap(0.2f, 300f, 80f, 0.3f);
            return jumpClip;
        }

        public static AudioClip GetDoubleJumpSound()
        {
            if (doubleJumpClip != null) return doubleJumpClip;
            // Higher zap
            doubleJumpClip = CreateZap(0.25f, 450f, 120f, 0.3f);
            return doubleJumpClip;
        }

        public static AudioClip GetSpeedUpSound()
        {
            if (speedUpClip != null) return speedUpClip;
            speedUpClip = CreateFMSweep(0.6f, 150f, 600f, 0.3f);
            return speedUpClip;
        }

        public static AudioClip GetSpeedDownSound()
        {
            if (speedDownClip != null) return speedDownClip;
            speedDownClip = CreateFMSweep(0.6f, 600f, 150f, 0.3f);
            return speedDownClip;
        }

        public static AudioClip GetMagnetOnSound()
        {
            if (magnetOnClip != null) return magnetOnClip;
            magnetOnClip = CreatePulsingTone(0.6f, 300f, 0.3f, true);
            return magnetOnClip;
        }

        public static AudioClip GetMagnetOffSound()
        {
            if (magnetOffClip != null) return magnetOffClip;
            magnetOffClip = CreatePulsingTone(0.6f, 300f, 0.3f, false);
            return magnetOffClip;
        }

        public static AudioClip GetCrashSound()
        {
            if (crashClip != null) return crashClip;
            crashClip = CreateHeavyNoise(0.8f, 0.5f, 3f);
            return crashClip;
        }

        public static AudioClip GetBreakSound()
        {
            if (breakClip != null) return breakClip;
            breakClip = CreateHeavyNoise(0.3f, 0.4f, 8f);
            return breakClip;
        }

        // --- SYNTHESIS METHODS ---

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
