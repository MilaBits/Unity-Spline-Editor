using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SplineEditor
{
    internal static class MathM
    {
        public const float TAU = 6.28318530718f;
        public static Vector2 GetVectorByAngle(float angRad) => new Vector2(Mathf.Cos(angRad), Mathf.Sin(angRad));

    }
}
