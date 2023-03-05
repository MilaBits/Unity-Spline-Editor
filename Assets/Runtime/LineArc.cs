using Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SplineEditor
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(PolygonCollider2D))]
    [RequireComponent(typeof(SnapTarget))]
    [ExecuteInEditMode]
    internal class LineArc : ImmediateModeShapeDrawer, ISnapTarget
    {
        [SerializeField]               public float radius = .5f;
        [SerializeField] [Range(0, 1)] public float fill = .5f;
        [SerializeField]               LineSettings lineSettings;
        [SerializeField]               bool debug = true;

        float radiusOuter => radius + lineSettings.lineThickness;
        int vertexCount => lineSettings.lineSegments * 2;

        Mesh mesh;
        //Mesh colliderMesh;

        private void OnDrawGizmos()
        {
            if (debug)
            {
                Gizmos.color = Color.red;
                GizmosM.DrawWireArc(transform.position, transform.rotation, radius, fill, lineSettings.lineSegments);
                
                for (int i = 0; i < lineSettings.lineCount; i++)
                {
                    float lineOffset = radius;
                    if (lineSettings.lineCount > 1)
                    {
                        if (i % 2 == 0) lineOffset += lineSettings.lineSpacing / 2;
                        else lineOffset -= lineSettings.lineSpacing / 2;
                    }

                    float lineOuter = lineOffset + (lineSettings.lineThickness / 2);
                    float lineInner = lineOffset - (lineSettings.lineThickness / 2);

                    Gizmos.color = Color.green;
                    GizmosM.DrawWireArc(transform.position, transform.rotation, lineOffset, fill, lineSettings.lineSegments);
                    Gizmos.color = Color.magenta;
                    GizmosM.DrawWireArc(transform.position, transform.rotation, lineOuter, fill, lineSettings.lineSegments);
                    GizmosM.DrawWireArc(transform.position, transform.rotation, lineInner, fill, lineSettings.lineSegments);


                    Transform start = transform.GetChild(0).transform;
                    Transform end = transform.GetChild(1).transform;
                    Handles.DrawLine(start.position, start.TransformPoint(Vector3.right*.1f), 0);
                    Handles.DrawLine(end.position, end.TransformPoint(Vector3.right*.1f), 0);
                }
            }
        }

        private void Awake()
        {
            GetComponent<SnapTarget>().target = this;
            GenerateMesh();

            Undo.undoRedoPerformed += OnUndoRedo;
        }

        public override void DrawShapes(Camera cam)
        {
            using (Draw.Command(cam))
            {
                Draw.LineGeometry = LineGeometry.Flat2D;
                Draw.ThicknessSpace = ThicknessSpace.Meters;
                Draw.Thickness = lineSettings.lineThickness;

                Draw.Matrix = transform.localToWorldMatrix;

                switch (lineSettings.lineConfiguration)
                {
                    case LineConfiguration.Center:
                        Draw.Arc(radius, 0, (float)(Math.PI * 2) * fill, ArcEndCap.Round, DiscColors.Flat(lineSettings.lineColor));
                        break;
                    case LineConfiguration.Left:
                    case LineConfiguration.Right:
                        Draw.Arc(radius + GetOffset(lineSettings.lineConfiguration), 0, (float)(Math.PI * 2) * fill, ArcEndCap.Round, DiscColors.Flat(lineSettings.lineColor));
                        break;
                    case LineConfiguration.Double:
                        Draw.Arc(radius + GetOffset(LineConfiguration.Left), 0, (float)(Math.PI * 2) * fill, ArcEndCap.Round, DiscColors.Flat(lineSettings.lineColor));
                        Draw.Arc(radius + GetOffset(LineConfiguration.Right), 0, (float)(Math.PI * 2) * fill, ArcEndCap.Round, DiscColors.Flat(lineSettings.lineColor));
                        break;
                }
            }
        }

        private void OnUndoRedo()
        {
            mesh.Clear();
            GenerateMesh();
        }

        private void OnValidate() => GenerateMesh();
        private void CreateNewMesh()
        {
            mesh = new Mesh();
            mesh.name = "Line Mesh";
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        void GenerateMesh()
        {
            if (mesh != null)
            {
                mesh.Clear();
            }
            else CreateNewMesh();


            List<Vector2> colliderVertices = new List<Vector2>();
            List<int> colliderTriangles = new List<int>();

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();

            List<List<Vector3>> lines = new List<List<Vector3>>();

            float lineOffset = 0;
            lineOffset = GetOffset(lineSettings.lineConfiguration);

            for (int i = 0; i < lineSettings.lineCount; i++)
            {
                lines.Add(new List<Vector3>());

                if (lineSettings.lineConfiguration == LineConfiguration.Double)
                    lineOffset = i == 0 ? GetOffset(LineConfiguration.Left) : GetOffset(LineConfiguration.Right);

                for (int j = 0; j < lineSettings.lineSegments; j++)
                {
                    float t = j * fill / lineSettings.lineSegments;
                    AddLineSegment(lines[i], normals, uvs, t, radius + lineOffset, lineSettings.lineThickness);
                }
                AddLineSegment(lines[i], normals, uvs, fill, radius + lineOffset, lineSettings.lineThickness);

                vertices.AddRange(lines[i]);
            }

            lineOffset = GetOffset(lineSettings.lineConfiguration);
            float spacing = lineSettings.lineCount > 1 ? lineSettings.lineSpacing : 0;
            for (int i = 0; i < lineSettings.lineSegments; i++)
            {
                float t = i * fill / lineSettings.lineSegments;
                AddLineSegment(colliderVertices, t, radius + lineOffset, spacing + lineSettings.lineThickness);
            }
            AddLineSegment(colliderVertices, fill, radius + lineOffset, spacing + lineSettings.lineThickness);

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

            int half = colliderVertices.Count / 2;

            Vector2[] firstHalf = colliderVertices.Where((x, i) => i % 2 == 1).ToArray();
            Vector2[] secondHalf = colliderVertices.Where((x, i) => i % 2 == 0).ToArray();
            Array.Reverse(secondHalf);
            GetComponent<PolygonCollider2D>().SetPath(0, firstHalf.Concat(secondHalf).ToArray());

            transform.GetChild(0).transform.position = transform.TransformPoint(MathM.GetVectorByAngle(0 * MathM.TAU) * radius);
            transform.GetChild(0).transform.rotation = Quaternion.Euler(0, 0, -90 + transform.rotation.eulerAngles.z);
            transform.GetChild(1).transform.position = transform.TransformPoint(MathM.GetVectorByAngle(fill * MathM.TAU) * radius);
            transform.GetChild(1).transform.rotation = Quaternion.Euler(0, 0, 90 + transform.rotation.eulerAngles.z + (360 * fill));
        }

        private float GetOffset(LineConfiguration line)
        {
            switch (line)
            {
                case LineConfiguration.Double:
                case LineConfiguration.Center:
                    return 0f;
                case LineConfiguration.Left:
                    return lineSettings.lineSpacing / 2f;
                case LineConfiguration.Right:
                    return -lineSettings.lineSpacing / 2f;
                default:
                    return 0f;
            }
        }

        void AddLineSegment(List<Vector3> line, List<Vector3> normals, List<Vector2> uvs, float t, float radius, float thickness)
        {
            AddLineSegment(line, t, radius, thickness);
            normals.Add(Vector3.forward);
            normals.Add(Vector3.forward);
            uvs.Add(new Vector2(t/fill, 1));
            uvs.Add(new Vector2(t/fill, 0));
        }

        void AddLineSegment(List<Vector3> line, float t, float radius, float thickness)
        {
            float angleRad = t * MathM.TAU;
            Vector3 lineCenter = MathM.GetVectorByAngle(angleRad) * radius;
            Vector3 lineOuter = lineCenter + (lineCenter.normalized * (thickness / 2));
            Vector3 lineInner = lineCenter - (lineCenter.normalized * (thickness / 2));
            line.Add(lineOuter);
            line.Add(lineInner);
        }

        void AddLineSegment(List<Vector2> line, float t, float radius, float thickness)
        {
            float angleRad = t * MathM.TAU;
            Vector3 lineCenter = MathM.GetVectorByAngle(angleRad) * radius;
            Vector3 lineOuter = lineCenter + (lineCenter.normalized * (thickness / 2));
            Vector3 lineInner = lineCenter - (lineCenter.normalized * (thickness / 2));
            line.Add(lineOuter);
            line.Add(lineInner);
        }

        public bool GetSnapPoint(Vector3 origin, out OrientedPoint snapPoint, float snapRange = 0.5F)
        {
            float shortestDistance = float.PositiveInfinity;
            OrientedPoint closest = new OrientedPoint(Vector3.positiveInfinity, Quaternion.identity);
            for (int i = 0; i < lineSettings.lineSegments; i++)
            {
                float t = i * fill / lineSettings.lineSegments;
                float angleRad = t * MathM.TAU;
                Vector3 lineCenter = MathM.GetVectorByAngle(angleRad) * radius;

                float currentDistance = Vector3.Distance(transform.TransformPoint(lineCenter), origin);
                if (currentDistance < shortestDistance)
                {
                    closest = new OrientedPoint(transform.TransformPoint(lineCenter), Quaternion.Euler(lineCenter));
                    shortestDistance = currentDistance;
                }
            }

            if (shortestDistance <= snapRange)
            {
                snapPoint = closest;
                return true;
            }

            snapPoint = new OrientedPoint(origin, Quaternion.identity);
            return false;
        }
    }
}
