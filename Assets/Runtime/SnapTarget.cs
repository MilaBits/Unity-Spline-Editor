using System.Collections.Generic;
using UnityEngine;

namespace SplineEditor
{
    [ExecuteInEditMode]
    public class SnapTarget : MonoBehaviour
    {
        private static readonly HashSet<SnapTarget> instances = new HashSet<SnapTarget>();
        public static HashSet<SnapTarget> Instances => new HashSet<SnapTarget>(instances);

        public ISnapTarget target;
        private void Awake()
        {
            instances.Add(this);
        }
        private void OnDestroy()
        {
            instances.Remove(this);
        }
    }
}
