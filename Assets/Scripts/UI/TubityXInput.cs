using UnityEngine;

namespace TubityWAI
{
    /// <summary>Which physical controls the player is holding right now.</summary>
    public enum InputScheme
    {
        Touch,      // iOS / Android: screen zones
        Keyboard,   // desktop: arrows / WASD / space
        Remote      // tvOS: Siri Remote touch surface
    }

    /// <summary>
    /// One place that decides which control scheme the help copy should talk
    /// about, plus the shared hint strings the How To Play screen and the
    /// in-tunnel tutorial both use, so the two never disagree.
    ///
    /// All strings stay inside the TubityX display face's glyph set
    /// (see TubityXFontMetrics): capitals, digits, basic punctuation and the
    /// arrow / mark glyphs. No quotes, no semicolons, no dashes other than '-'.
    /// </summary>
    public static class TubityXInput
    {
        /// <summary>
        /// Editor / test hook: force a scheme regardless of platform so every
        /// variant of the help can be previewed on one machine.
        /// </summary>
        public static InputScheme? Override;

        public static InputScheme Current
        {
            get
            {
                if (Override.HasValue) return Override.Value;
#if UNITY_EDITOR
                // Follow the active build target so a designer previewing the
                // tvOS or iOS build in the editor sees that platform's help.
                switch (UnityEditor.EditorUserBuildSettings.activeBuildTarget)
                {
                    case UnityEditor.BuildTarget.tvOS: return InputScheme.Remote;
                    case UnityEditor.BuildTarget.iOS:
                    case UnityEditor.BuildTarget.Android: return InputScheme.Touch;
                    default: return InputScheme.Keyboard;
                }
#elif UNITY_TVOS
                return InputScheme.Remote;
#elif UNITY_IOS || UNITY_ANDROID
                return InputScheme.Touch;
#else
                return Application.isMobilePlatform ? InputScheme.Touch : InputScheme.Keyboard;
#endif
            }
        }

        /// <summary>Badge text on the How To Play screen.</summary>
        public static string SchemeTitle(InputScheme s)
        {
            switch (s)
            {
                case InputScheme.Remote: return "SIRI REMOTE";
                case InputScheme.Keyboard: return "KEYBOARD CONTROLS";
                default: return "TOUCH CONTROLS";
            }
        }

        // ------------------------------------------------------------------
        // Short imperative hints, shared with the in-tunnel tutorial overlays.
        // ------------------------------------------------------------------

        public static string SteerRightHint(InputScheme s)
        {
            switch (s)
            {
                case InputScheme.Remote: return "THUMB ON THE RIGHT EDGE ▶▶";
                case InputScheme.Keyboard: return "HOLD ▶ OR D";
                default: return "TAP / HOLD HERE ▶▶";
            }
        }

        public static string SteerLeftHint(InputScheme s)
        {
            switch (s)
            {
                case InputScheme.Remote: return "◀◀ THUMB ON THE LEFT EDGE";
                case InputScheme.Keyboard: return "HOLD ◀ OR A";
                default: return "◀◀ TAP / HOLD HERE";
            }
        }

        /// <summary>Either direction: used when the player just needs to get out of the way.</summary>
        public static string SteerAnyHint(InputScheme s)
        {
            switch (s)
            {
                case InputScheme.Remote: return "THUMB ON THE LEFT OR RIGHT EDGE";
                case InputScheme.Keyboard: return "HOLD ◀ ▶ OR A / D";
                default: return "HOLD THE LEFT OR RIGHT SIDE";
            }
        }

        public static string JumpHint(InputScheme s)
        {
            switch (s)
            {
                case InputScheme.Remote: return "CLICK THE TOUCH SURFACE";
                case InputScheme.Keyboard: return "PRESS SPACE, ▲ OR W";
                default: return "TAP THE BOTTOM OF THE SCREEN";
            }
        }

        public static string DoubleJumpHint(InputScheme s)
        {
            switch (s)
            {
                case InputScheme.Remote: return "CLICK TWICE TO CROSS OVER";
                case InputScheme.Keyboard: return "PRESS SPACE TWICE TO CROSS OVER";
                default: return "DOUBLE TAP THE BOTTOM TO CROSS OVER";
            }
        }
    }
}
