using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Assets.Spline_Editor
{
    [CustomEditor(typeof(LineBezier))]
    public class LineBezierEditor : Editor
    {
        float size = 1f;

        bool showChildTransforms = false;

        LineBezier lineBezier;

        void OnSceneGUI()
        {
            lineBezier = (LineBezier)target;

            Gizmos.color = Color.gray;
            Transform transform = lineBezier.transform;

            Handles.DrawDottedLine(lineBezier.GetPosition(0), lineBezier.GetPosition(1), 5);
            Handles.DrawDottedLine(lineBezier.GetPosition(3), lineBezier.GetPosition(2), 5);


            LineCapHandle(0);
            LineCapHandle(3);

            TangentHandle(0,1);
            TangentHandle(3,2);
        }

        private void TangentHandle(int cap, int tangent)
        {
            EditorGUI.BeginChangeCheck();
            Vector3 newTargetPosition = Handles.PositionHandle(lineBezier.GetPosition(tangent), Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(lineBezier, "Change Look At Target Position");

                lineBezier.controlPoints[tangent].position = newTargetPosition;
                lineBezier.controlPoints[cap].LookAt(newTargetPosition.normalized, Vector3.back);
                lineBezier.controlPoints[cap].localScale = Vector3.one * Vector3.Distance(lineBezier.GetPosition(cap), lineBezier.GetPosition(tangent));
                lineBezier.GenerateMesh();
            }

        }

        private void LineCapHandle(int cap)
        {

            Vector3 point =       lineBezier.controlPoints[cap].position;
            Vector3 scale =       lineBezier.controlPoints[cap].localScale;
            Quaternion rotation = lineBezier.controlPoints[cap].rotation;

            Quaternion handleRotation = rotation;

            if (Tools.current == Tool.Move)
            {
                EditorGUI.BeginChangeCheck();
                point = Handles.DoPositionHandle(point, handleRotation);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(lineBezier.controlPoints[cap], "Moved Bezier Point");

                    switch (Event.current.modifiers)
                    {
                        case EventModifiers.Shift:
                            lineBezier.controlPoints[cap].position = Vector3Int.RoundToInt(point);
                            break;
                        case EventModifiers.Alt:
                            Vector3 mousePosition = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition).origin;
                            mousePosition.z = 0;
                            OrientedPoint target;
                            if (ClosestSnapPoint(mousePosition, .5f, out target))
                            {
                                lineBezier.controlPoints[cap].position = target.position;
                                //lineBezier.controlPoints[i].rotation = target.rotation;
                            }
                            else
                            {
                                lineBezier.controlPoints[cap].position = point;
                            }
                            break;
                        default:
                            lineBezier.controlPoints[cap].position = point;
                            break;
                    }
                    lineBezier.GenerateMesh();
                }
            }
            
            //if (Tools.current == Tool.Rotate)
            //{
            //    EditorGUI.BeginChangeCheck();
            //    rotation = Handles.DoRotationHandle(handleRotation, point);
            //    if (EditorGUI.EndChangeCheck())
            //    {
            //        Undo.RecordObject(lineBezier.controlPoints[cap], "Rotated Bezier Point");
            //        lineBezier.controlPoints[cap].rotation = rotation;
            //        lineBezier.GenerateMesh();
            //    }
            //}

            //if (Tools.current == Tool.Scale)
            //{
            //    EditorGUI.BeginChangeCheck();
            //    scale = Handles.DoScaleHandle(scale, point, handleRotation, scale.y);
            //    if (EditorGUI.EndChangeCheck())
            //    {
            //        Undo.RecordObject(lineBezier.controlPoints[cap], "Scaled Bezier Point");
            //        lineBezier.controlPoints[cap].localScale = scale;
            //        lineBezier.GenerateMesh();
            //    }
            //}
        }

        private bool ClosestSnapPoint(Vector3 origin, float snapRange, out OrientedPoint result)
        {

            var overlap = Physics.OverlapSphere(origin, snapRange).ToList();
            var snapTargets = SnapTarget.Instances.Where(x => overlap.Contains(((MonoBehaviour)x.target).GetComponent<Collider>()));
            if (snapTargets.Count() == 0)
            {
                result = new OrientedPoint(origin, Quaternion.identity);
                return false;
            }

            float shortestDistance = float.PositiveInfinity;
            OrientedPoint closest = new OrientedPoint(Vector3.positiveInfinity, Quaternion.identity);

            foreach (var snapTarget in snapTargets)
            {
                if (snapTarget.target.GetSnapPoint(origin, out OrientedPoint current, snapRange))
                {
                    float currentDistance = Vector3.Distance(current.position, origin);
                    if (currentDistance < shortestDistance)
                    {
                        shortestDistance = currentDistance;
                        closest = current;
                    }
                }
            }

            Debug.Log(shortestDistance);
            if (shortestDistance <= snapRange)
            {
                result = closest;
                return true;
            }

            result = new OrientedPoint(origin, Quaternion.identity);
            return false;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            lineBezier = (LineBezier)target;

            DrawControlPointGUI(0,"Start");
            DrawControlPointGUI(3,"End");

            //for (int i = 0;i < lineBezier.controlPoints.Length; i++)
            //{
            //    if (lineBezier.controlPoints[i] == null) continue;

            //    DrawControlPointGUI(i);

            //    //SerializedProperty propPos = controlPointSO.FindProperty("m_LocalPosition");
            //    //SerializedProperty propRot = controlPointSO.FindProperty("m_LocalRotation");

            //    //EditorGUILayout.LabelField(lineBezier.controlPoints[i].name);
            //    //EditorGUI.BeginChangeCheck();
            //    //EditorGUILayout.PropertyField(propPos, new GUIContent("Position"));
            //    //if (EditorGUI.EndChangeCheck())
            //    //{
            //    //    Undo.RecordObject(lineBezier.controlPoints[i], "Changed Bezier Point");
            //    //    lineBezier.GenerateMesh();
            //    //}

            //    //EditorGUI.BeginChangeCheck();
            //    //EditorGUILayout.PropertyField(propRot, new GUIContent("Rotation"));
            //    //if (EditorGUI.EndChangeCheck())
            //    //{
            //    //    Undo.RecordObject(lineBezier.controlPoints[i], "Changed Bezier Point");
            //    //    lineBezier.GenerateMesh();
            //    //}

            //    //controlPointSO.ApplyModifiedProperties();
            //}


        }

        private void DrawControlPointGUI(int i, string label)
        {
            Transform transform = lineBezier.controlPoints[i];

            Vector3 position;
            Vector3 rotation;
            Vector3 scale;

            EditorGUILayout.LabelField(label);
            EditorGUIUtility.labelWidth = 15f;

            EditorGUILayout.BeginHorizontal();
            position = DrawVector3("Position", transform.localPosition);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            rotation = DrawVector3("Rotation", transform.localEulerAngles);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            scale = DrawVector3("Scale", transform.localScale);
            EditorGUILayout.EndHorizontal();

            if (GUI.changed)
            {
                Undo.RecordObject(transform, "Bezier Point Changed");
                transform.localPosition = ValidateVector(position);
                transform.localEulerAngles = ValidateVector(rotation);
                transform.localScale = ValidateVector(scale);
                lineBezier.GenerateMesh();
            }
        }

        private static Vector3 DrawVector3(string name, Vector3 value)
        {
            var option = GUILayout.MinWidth(30f);
            EditorGUILayout.LabelField(name);
            value.x = EditorGUILayout.FloatField("X", value.x, option);
            value.y = EditorGUILayout.FloatField("Y", value.y, option);
            value.z = EditorGUILayout.FloatField("Z", value.z, option);
            return value;
        }

        private static bool IsResetVectorValid(Vector3 vector, Vector3 target)
        {
            return (vector.x != target.x || vector.y != target.y || vector.z != target.z);
        }

        private static Vector3 ValidateVector(Vector3 vector)
        {
            vector.x = float.IsNaN(vector.x) ? 0f : vector.x;
            vector.y = float.IsNaN(vector.y) ? 0f : vector.y;
            vector.z = float.IsNaN(vector.z) ? 0f : vector.z;
            return vector;
        }
    }
}
