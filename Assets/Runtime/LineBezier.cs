using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using Shapes;

namespace SplineEditor
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(PolygonCollider2D))]
    [RequireComponent(typeof(SnapTarget))]
    [ExecuteInEditMode]
    public class LineBezier : ImmediateModeShapeDrawer, ISnapTarget
    {
        public LineSettings lineSettings = new LineSettings();
        [SerializeField]
        public Transform[] controlPoints = new Transform[4];
        bool debug = false;
        Mesh mesh;

        public Vector3 GetPosition(int i) => controlPoints[i].position;
        OrientedPoint GetBezierOrientedPoint(float t)
        {
            Vector3 p0 = transform.InverseTransformPoint(GetPosition(0));
            Vector3 p1 = transform.InverseTransformPoint(GetPosition(1));
            Vector3 p2 = transform.InverseTransformPoint(GetPosition(2));
            Vector3 p3 = transform.InverseTransformPoint(GetPosition(3));

            Vector3 a = Vector3.Lerp(p0, p1, t);
            Vector3 b = Vector3.Lerp(p1, p2, t);
            Vector3 c = Vector3.Lerp(p2, p3, t);

            Vector3 d = Vector3.Lerp(a, b, t);
            Vector3 e = Vector3.Lerp(b, c, t);

            Vector3 pos = Vector3.Lerp(d, e, t);
            Vector3 tangent = (e - d).normalized;
            return new OrientedPoint(pos, tangent);
        }
        Quaternion GetBezierOrientation(float t) => Quaternion.LookRotation(GetBezierTangent(t));
        Vector3 GetBezierTangent(float t)
        {
            Vector3 p0 = GetPosition(0);
            Vector3 p1 = GetPosition(1);
            Vector3 p2 = GetPosition(2);
            Vector3 p3 = GetPosition(3);

            Vector3 a = Vector3.Lerp(p0, p1, t);
            Vector3 b = Vector3.Lerp(p1, p2, t);
            Vector3 c = Vector3.Lerp(p2, p3, t);

            Vector3 d = Vector3.Lerp(a, b, t);
            Vector3 e = Vector3.Lerp(b, c, t);

            return (e - d).normalized;
        }
        public override void DrawShapes(Camera cam)
        {
            using (Draw.Command(cam))
            {
                Draw.LineGeometry = LineGeometry.Flat2D;
                Draw.ThicknessSpace = ThicknessSpace.Meters;
                Draw.Thickness = lineSettings.lineThickness;

                Draw.Matrix = transform.localToWorldMatrix;

                //List<Vector3> points = GetLinePoints(lineSettings.lineConfiguration);


                switch (lineSettings.lineConfiguration)
                {
                    case LineConfiguration.Center:
                        if (lineSettings.lineSegments == 1) Draw.Line(GetBezierOrientedPoint(0).position, GetBezierOrientedPoint(3).position, lineSettings.lineColor);
                        else
                        {
                            PolylinePath path = new PolylinePath();
                            path.AddPoints(GetLinePoints(lineSettings.lineConfiguration));
                            Draw.Polyline(path, false, lineSettings.lineColor);
                        }
                        break;
                    case LineConfiguration.Left:
                    case LineConfiguration.Right:
                        if (lineSettings.lineSegments == 1) Draw.Line(GetBezierOrientedPoint(0).position, GetBezierOrientedPoint(3).position, lineSettings.lineColor);
                        else
                        {
                            PolylinePath path = new PolylinePath();
                            path.AddPoints(GetLinePoints(lineSettings.lineConfiguration));
                            Draw.Polyline(path, false, lineSettings.lineColor);
                        }
                        break;
                    case LineConfiguration.Double:
                        if (lineSettings.lineSegments == 1)
                        {
                            Draw.Line(GetBezierOrientedPoint(0).LocalToWorld(GetLineOffset(LineConfiguration.Left)), GetBezierOrientedPoint(3).LocalToWorld(GetLineOffset(LineConfiguration.Left)), lineSettings.lineColor);
                            Draw.Line(GetBezierOrientedPoint(0).LocalToWorld(GetLineOffset(LineConfiguration.Right)), GetBezierOrientedPoint(3).LocalToWorld(GetLineOffset(LineConfiguration.Right)), lineSettings.lineColor);
                        }
                        else
                        {
                            PolylinePath left = new(), right = new();
                            left.AddPoints(GetLinePoints(LineConfiguration.Left));
                            right.AddPoints(GetLinePoints(LineConfiguration.Right));
                            Draw.Polyline(left, false, lineSettings.lineColor);
                            Draw.Polyline(right, false, lineSettings.lineColor);
                        }
                        break;
                }
            }
        }

        public List<Vector3> GetLinePoints(LineConfiguration configuration)
        {
            List<Vector3> points = new List<Vector3>();

            for (int i = 0; i <= lineSettings.lineSegments; i++)
            {
                float t = i / (float)lineSettings.lineSegments;
                OrientedPoint currentPoint = GetBezierOrientedPoint(t);
                points.Add(currentPoint.LocalToWorld(GetLineOffset(configuration)));
            }
            return points;
        }

        public bool GetSnapPoint(Vector3 origin, out OrientedPoint snapPoint, float snapRange)
        {
            float shortestDistance = float.PositiveInfinity;
            OrientedPoint closest = new OrientedPoint(Vector3.positiveInfinity, Quaternion.identity);
            for (int i = 0; i < lineSettings.lineSegments; i++)
            {
                float t = i / (float)lineSettings.lineSegments;
                OrientedPoint currentPoint = GetBezierOrientedPoint(t);

                float currentDistance = Vector3.Distance(transform.TransformPoint(currentPoint.position), origin);
                if (currentDistance < shortestDistance)
                {
                    closest = currentPoint;
                    shortestDistance = currentDistance;
                }
            }

            if (shortestDistance <= snapRange)
            {
                Vector3 direction = closest.LocalToWorld(Vector3.right);
                snapPoint = new OrientedPoint(closest.position, closest.position - direction);
                return true;
            }

            snapPoint = new OrientedPoint(origin, Quaternion.identity);
            return false;
        }

        public void OnDrawGizmos()
        {
            if (debug)
            {
                Handles.DrawBezier(GetPosition(0), GetPosition(3), GetPosition(1), GetPosition(2), Color.gray, EditorGUIUtility.whiteTexture, 5f);
            }
        }
        private void OnValidate() => GenerateMesh();
        private void Awake()
        {
            GetComponent<SnapTarget>().target = this;
            GenerateMesh();

            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnUndoRedo()
        {
            if (mesh) mesh.Clear();
            GenerateMesh();
        }

        private void CreateNewMesh()
        {
            mesh = new Mesh();
            mesh.name = "Line Mesh";
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        private Vector3 GetLineOffset(LineConfiguration line)
        {
            switch (line)
            {
                case LineConfiguration.Center:
                case LineConfiguration.Double:
                    return Vector3.zero;
                case LineConfiguration.Left:
                    return Vector3.left * (lineSettings.lineSpacing / 2);
                case LineConfiguration.Right:
                    return Vector3.right * (lineSettings.lineSpacing / 2);
                default:
                    return Vector3.zero;
            }
        }

        void AddLineSegment(List<Vector3> line, OrientedPoint currentPoint, Vector3 lineOffset, List<Vector3> normals, List<Vector2> uvs, float t, float thickness)
        {
            AddLineSegment(line, currentPoint, lineOffset, thickness);
            normals.Add(Vector3.forward);
            normals.Add(Vector3.forward);
            uvs.Add(new Vector2(t, 1));
            uvs.Add(new Vector2(t, 0));
        }

        void AddLineSegment(List<Vector3> line, OrientedPoint currentPoint, Vector3 lineOffset, float thickness)
        {
            Vector3 lineTop = lineOffset + (Vector3.right * (thickness / 2));
            Vector3 lineBottom = lineOffset - (Vector3.right * (thickness / 2));
            line.Add(currentPoint.LocalToWorld(lineTop));
            line.Add(currentPoint.LocalToWorld(lineBottom));
            if (debug) Debug.DrawLine(currentPoint.LocalToWorld(lineTop), currentPoint.LocalToWorld(lineBottom), Color.red, Time.deltaTime);
        }

        void AddLineSegment(List<Vector2> line, OrientedPoint currentPoint, Vector3 lineOffset, float thickness)
        {
            Vector3 lineTop = lineOffset + (Vector3.right * (thickness / 2));
            Vector3 lineBottom = lineOffset - (Vector3.right * (thickness / 2));
            line.Add(currentPoint.LocalToWorld(lineTop));
            line.Add(currentPoint.LocalToWorld(lineBottom));
            if (debug) Debug.DrawLine(currentPoint.LocalToWorld(lineTop), currentPoint.LocalToWorld(lineBottom), Color.red, Time.deltaTime);
        }

        public void GenerateMesh()
        {
            if (mesh) mesh.Clear();
            else CreateNewMesh();

            List<Vector2> colliderVertices = new List<Vector2>();

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();

            List<List<Vector3>> lines = new List<List<Vector3>>();

            Vector3 lineOffset = Vector3.zero;
            lineOffset = GetLineOffset(lineSettings.lineConfiguration);

            for (int i = 0; i < lineSettings.lineCount; i++)
            {
                lines.Add(new List<Vector3>());

                if (lineSettings.lineConfiguration == LineConfiguration.Double)
                    lineOffset = i == 0 ? GetLineOffset(LineConfiguration.Left) : GetLineOffset(LineConfiguration.Right);

                for (int j = 0; j < lineSettings.lineSegments + 1; j++)
                {
                    float t = j / (float)lineSettings.lineSegments;
                    OrientedPoint currentPoint = GetBezierOrientedPoint(t);
                    AddLineSegment(lines[i], currentPoint, lineOffset, lineSettings.lineThickness);

                }
                vertices.AddRange(lines[i]);
            }

            lineOffset = GetLineOffset(lineSettings.lineConfiguration);
            float thickness = lineSettings.lineConfiguration == LineConfiguration.Double ?
                lineSettings.lineSpacing + lineSettings.lineThickness :
                lineSettings.lineThickness;
            for (int i = 0; i < lineSettings.lineSegments; i++)
            {
                float t = i / (float)lineSettings.lineSegments;
                OrientedPoint currentPoint = GetBezierOrientedPoint(t);
                AddLineSegment(colliderVertices, currentPoint, lineOffset, thickness);
            }
            AddLineSegment(colliderVertices, GetBezierOrientedPoint(1), lineOffset, thickness);

            // Triangles
            List<int> triangles = new List<int>();
            for (int i = 0; i < lineSettings.lineSegments * lineSettings.lineCount + 1; i++)
            {
                if (i == lineSettings.lineSegments) continue;

                int indexRoot = i * 2;
                int indexInnerRoot = indexRoot + 1;
                int indexOuterNext = indexRoot + 2;
                int indexInnerNext = indexRoot + 3;

                triangles.Add(indexRoot);
                triangles.Add(indexInnerNext);
                triangles.Add(indexOuterNext);

                triangles.Add(indexRoot);
                triangles.Add(indexInnerRoot);
                triangles.Add(indexInnerNext);
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);

            Vector2[] firstHalf = colliderVertices.Where((x, i) => i % 2 == 1).ToArray();
            Vector2[] secondHalf = colliderVertices.Where((x, i) => i % 2 == 0).ToArray();
            Array.Reverse(secondHalf);
            GetComponent<PolygonCollider2D>().SetPath(0, firstHalf.Concat(secondHalf).ToArray());
        }
    }
}
