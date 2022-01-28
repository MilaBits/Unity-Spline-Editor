using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Spline_Editor
{
    internal static class GismosM
    {
        public static void DrawWireCircle(Vector3 pos, Quaternion rot, float radius, int detail = 32)
        {
            Vector3[] points3D = new Vector3[detail];
            for (int i = 0; i < detail; i++)
            {
                float t = i / (float)detail;
                float angleRad = t * MathM.TAU;

                Vector2 point2D = MathM.GetVectorByAngle(angleRad) * radius;

                points3D[i] = pos + rot * point2D;

            }

            for (int i = 0; i < detail - 1; i++)
            {
                Gizmos.DrawLine(points3D[i], points3D[i + 1]);
            }
            Gizmos.DrawLine(points3D[detail - 1], points3D[0]);
        }

        public static void DrawWireArc(Vector3 pos, Quaternion rot, float radius, float fill, int detail = 32)
        {
            Vector3[] points3D = new Vector3[detail+1];
            for (int i = 0; i < detail; i++)
            {
                float t = i * fill / detail;
                float angleRad = t * MathM.TAU;

                Vector2 point2D = MathM.GetVectorByAngle(angleRad) * radius;

                points3D[i] = pos + rot * point2D;

            }
            points3D[detail] = pos + rot * MathM.GetVectorByAngle(MathM.TAU * fill) * radius;

            for (int i = 0; i < detail; i++)
            {
                Gizmos.DrawLine(points3D[i], points3D[i + 1]);
            }
            //Gizmos.DrawLine(points3D[detail - 1], points3D[0]);
        }
    }
}
