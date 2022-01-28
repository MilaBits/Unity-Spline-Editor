using Assets.SplineEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets
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
