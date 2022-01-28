using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Assets.SplineEditor
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(PolygonCollider2D))]
    [RequireComponent(typeof(SnapTarget))]
    [ExecuteInEditMode]
    internal class LineArc : MonoBehaviour, ISnapTarget
    {
        [SerializeField]               public float radius = .5f;
        [SerializeField] [Range(0, 1)] public float fill = .5f;
        [SerializeField]               LineSettings lineSettings;
        [SerializeField]               bool debug = true;

        float radiusOuter => radius + lineSettings.lineThickness;
        int vertexCount => lineSettings.lineSegments * 2;

        Mesh mesh;
        Mesh colliderMesh;

        private void OnDrawGizmos()
        {
            if (debug)
            {
                Gizmos.color = Color.red;
                GismosM.DrawWireArc(transform.position, transform.rotation, radius, fill, lineSettings.lineSegments);

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
                    GismosM.DrawWireArc(transform.position, transform.rotation, lineOffset, fill, lineSettings.lineSegments);
                    Gizmos.color = Color.magenta;
                    GismosM.DrawWireArc(transform.position, transform.rotation, lineOuter, fill, lineSettings.lineSegments);
                    GismosM.DrawWireArc(transform.position, transform.rotation, lineInner, fill, lineSettings.lineSegments);
                }
            }
        }

        private void Awake()
        {
            GetComponent<SnapTarget>().target = this;
            GenerateMesh();
        }
        private void OnValidate() => GenerateMesh();
        private void CreateNewMesh()
        {
            mesh = new Mesh();
            mesh.name = "Line Mesh";
            GetComponent<MeshFilter>().sharedMesh = mesh;

            //colliderMesh = new Mesh();
            //colliderMesh.name = "Collider";
            //GetComponent<MeshCollider>().sharedMesh = colliderMesh;
        }

        void GenerateMesh()
        {
            if (mesh != null || colliderMesh != null)
            {
                mesh.Clear();
                //colliderMesh.Clear();
            }
            else CreateNewMesh();

            List<Vector2> colliderVertices = new List<Vector2>();
            List<int> colliderTriangles = new List<int>();

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();

            List<List<Vector3>> lines = new List<List<Vector3>>();
            for (int i = 0; i < lineSettings.lineCount; i++)
            {
                lines.Add(new List<Vector3>());

                float lineOffset = 0;
                if (lineSettings.lineCount == 1)
                {
                    switch (lineSettings.SingleLinePosition)
                    {
                        case SingleLinePosition.Center:
                            lineOffset = 0;
                            break;
                        case SingleLinePosition.Left:
                            lineOffset = lineSettings.lineSpacing / 2;
                            break;
                        case SingleLinePosition.Right:
                            lineOffset = -lineSettings.lineSpacing / 2;
                            break;
                    }
                }
                else
                {
                    if (i % 2 == 0) lineOffset += lineSettings.lineSpacing / 2;
                    else lineOffset -= lineSettings.lineSpacing / 2;
                }

                for (int j = 0; j < lineSettings.lineSegments; j++)
                {
                    float t = j * fill / lineSettings.lineSegments;
                    AddLineSegment(lines[i], normals, uvs, t, radius + lineOffset, lineSettings.lineThickness);
                    if (i == 0) AddLineSegment(colliderVertices, t, radius, (lineSettings.lineThickness * lineSettings.lineCount) + lineOffset);
                }
                AddLineSegment(lines[i], normals, uvs, fill, radius + lineOffset, lineSettings.lineThickness);
                if (i == 0) AddLineSegment(colliderVertices, fill, radius, (lineSettings.lineThickness * lineSettings.lineCount) + lineOffset);

                vertices.AddRange(lines[i]);
            }

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
            GetComponent<PolygonCollider2D>().SetPath(0,firstHalf.Concat(secondHalf).ToArray());
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
