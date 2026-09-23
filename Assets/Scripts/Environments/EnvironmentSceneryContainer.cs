using System.Collections.Generic;
using UnityEngine;

namespace TubityWAI
{
    /// <summary>
    /// Lives on a tunnel segment's scenery container so a recycled segment can
    /// find and re-place its props instead of rebuilding them.
    /// </summary>
    public class EnvironmentSceneryContainer : MonoBehaviour
    {
        public List<EnvironmentScenery.Prop> props = new List<EnvironmentScenery.Prop>();

        /// <summary>
        /// Themes this segment has already built props for. On a blended level a recycled segment can
        /// land under a different theme, so it accumulates both sets and hides the one that is fading out.
        /// </summary>
        public List<EnvironmentTheme> builtThemes = new List<EnvironmentTheme>();
    }
}
