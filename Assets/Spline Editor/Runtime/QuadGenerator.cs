using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.SplineEditor
{
    public class QuadGenerator : MonoBehaviour
    {
        private void Awake()
        {
            Mesh mesh = new Mesh();
            mesh.name = "ProcQuad";

            List<Vector3> points = new List<Vector3>()
            {
                new Vector3(-1, 1),
                new Vector3( 1, 1),
                new Vector3(-1,-1),
                new Vector3( 1,-1),
            };

            int[] triangles = new int[]
            {
                1,0,2,
                3,1,2
            };

            List<Vector2> uvs = new List<Vector2>
            {
                new Vector2(1,1),
                new Vector2(0,1),
                new Vector2(1,0),
                new Vector2(0,0),
            };

            List<Vector3> normals = new List<Vector3>
            {
                new Vector3(0,0,3),
                new Vector3(0,0,3),
                new Vector3(0,0,3),
                new Vector3(0,0,3),
            };


            mesh.SetVertices(points);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.triangles = triangles;


            GetComponent<MeshFilter>().sharedMesh = mesh;
        }
    }
}