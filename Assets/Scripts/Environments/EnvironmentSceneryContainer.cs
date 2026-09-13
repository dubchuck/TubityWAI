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
    }
}
