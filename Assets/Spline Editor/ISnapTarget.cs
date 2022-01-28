using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Spline_Editor
{
    public interface ISnapTarget
    {
        public bool GetSnapPoint(Vector3 origin, out OrientedPoint snapPoint, float snapRange = .5f);
    }
}
