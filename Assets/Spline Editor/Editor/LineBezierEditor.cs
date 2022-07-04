using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Assets.SplineEditor
{
    [CustomEditor(typeof(LineBezier))]
    public class LineBezierEditor : Editor
    {
        float size = 1f;
        float snapDistance = .1f;

        float minTangentLength = .1f;
        float maxTangentLength = 100f;

        bool showChildTransforms = false;

        float addRotation = 0;

        LineBezier lineBezier;

        bool snapped;

        void OnSceneGUI()
        {
            if (Event.current.type == EventType.MouseUp && Event.current.button == 0) snapped = false;

            lineBezier = (LineBezier)target;

            Gizmos.color = Color.gray;
            Transform transform = lineBezier.transform;

            LineCapHandle(lineBezier.controlPoints[0], lineBezier.controlPoints[1]);
            LineCapHandle(lineBezier.controlPoints[3], lineBezier.controlPoints[2]);

            if (lineBezier.lineSettings.lineSegments >1)
            {
                TangentHandle(lineBezier.controlPoints[0], lineBezier.controlPoints[1]);
                TangentHandle(lineBezier.controlPoints[3], lineBezier.controlPoints[2]);
            }
        }

        private void TangentHandle(Transform cap, Transform tangent)
        {
            Handles.DrawDottedLine(cap.position, tangent.position, 5);
            Handles.CircleHandleCap(0, tangent.position, Quaternion.identity, HandleUtility.GetHandleSize(tangent.position) / 4, EventType.Repaint);
            Vector3 snapPoint = Vector3.zero;
            bool doSnap = false;
            switch (Event.current.modifiers)
            {
                case EventModifiers.Shift:
                    if (doSnap = SnapToEdge(cap, out Transform snapTarget))
                    {
                        float distance = Mathf.Clamp(Vector3.Distance(cap.position, tangent.position), minTangentLength, maxTangentLength);
                        List<Vector3> snaps = new List<Vector3> {
                        snapTarget.TransformPoint(snapTarget.up * distance), snapTarget.TransformPoint(-snapTarget.up * distance),
                        snapTarget.TransformPoint(snapTarget.right * distance), snapTarget.TransformPoint(-snapTarget.right * distance)
                    };
                        snapPoint = snaps.OrderBy(x => Vector3.Distance(tangent.position, x)).FirstOrDefault();
                        Handles.color = Color.gray;
                        Handles.CircleHandleCap(0, snapPoint, Quaternion.identity, snapDistance, EventType.Repaint);
                        Debug.DrawLine(snapPoint, tangent.position, doSnap ? Color.green : Color.red, Time.deltaTime);
                    }
                    break;
            }

            if (snapped) return;
            EditorGUI.BeginChangeCheck();
            Vector3 point = Handles.PositionHandle(tangent.position, Quaternion.identity);
            point.z = 0;
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(tangent, "Change Look At Target Position");
                Undo.RecordObject(cap, "Change Look At Target Position");
                cap.localScale = Vector3.one * Vector3.Distance(cap.position, tangent.position);
                switch (Event.current.modifiers)
                {
                    case EventModifiers.Shift:
                        if (doSnap)
                        {
                            Debug.Log("snap");
                            tangent.position = snapPoint;
                        }
                        else
                        {
                            tangent.position = point;
                        }
                        break;
                    default:
                        tangent.position = point;
                        break;
                }
                FixEndCap(cap);
                lineBezier.GenerateMesh();
            }
        }

        private void FixEndCap(Transform cap) => cap.right = cap.position - cap.GetChild(0).position;

        private void FixStraightEndCaps() 
        {
            Transform p0 = lineBezier.controlPoints[0];
            Transform p1 = lineBezier.controlPoints[3];

            p0.right = p0.position - p1.position;
            p1.right = p1.position - p0.position;
        }

        private void LineCapHandle(Transform  cap, Transform tangent)
        {
            if (Tools.current == Tool.Move)
            {
                switch (Event.current.modifiers)
                {
                    case EventModifiers.Shift:
                        break;
                    case EventModifiers.Alt:
                        Handles.color = Color.gray;
                        Handles.CircleHandleCap(0, cap.position, Quaternion.identity, snapDistance, Event.current.type);
                        break;
                    case EventModifiers.Alt | EventModifiers.Shift:
                        Handles.color = Color.yellow;
                        Handles.CircleHandleCap(0, cap.position, Quaternion.identity, snapDistance, Event.current.type);
                        break;
                }

                EditorGUI.BeginChangeCheck();
                Vector3 point = Handles.DoPositionHandle(cap.position, cap.rotation);
                point.z = 0;
                if (EditorGUI.EndChangeCheck())
                {
                    if (lineBezier.lineSettings.lineSegments < 2)
                    {
                        Undo.RecordObject(lineBezier.controlPoints[2], "Straighten Tangent");
                        Undo.RecordObject(lineBezier.controlPoints[3], "Straighten Tangent");
                        Straighten();
                    }
                    Undo.RecordObject(cap, "Moved Bezier Point");

                    switch (Event.current.modifiers)
                    {
                        case EventModifiers.Alt:
                            if (SnapToEdge(cap, out Transform snapTarget))
                            {
                                cap.position = snapTarget.position;
                            }
                            else
                            {
                                cap.position = point;
                            }
                            break;
                        case EventModifiers.Alt | EventModifiers.Shift:
                            if (SnapToEdge(cap, out Transform rotateSnapTarget))
                            {
                                cap.position = rotateSnapTarget.position;
                                float magnitude = Vector3.Distance(cap.position, tangent.position);
                                tangent.position = cap.position + Quaternion.Euler(0,0, addRotation) * rotateSnapTarget.right * magnitude;
                            }
                            else
                            {
                                cap.position = point;
                            }
                            break;
                        default:
                            cap.position = point;
                            break;
                    }
                    lineBezier.GenerateMesh();
                }
            }
        }

        private bool SnapToEdge(Transform current, out Transform target)
        {
            //Vector3 mousePosition = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition).origin;
            //mousePosition.z = 0;

            List<Transform> points = GameObject.FindGameObjectsWithTag("LinePoint").Select(x => x.transform).ToList();
            for (int i = 0; i < current.parent.childCount; i++) points.Remove(current.parent.GetChild(i));
            Transform point = points.OrderBy(x => Vector3.Distance(current.position, x.position)).FirstOrDefault();
            float distance = Vector3.Distance(current.position, new Vector3(point.position.x, point.position.y, 0));
            if (point != null && distance <= snapDistance)
            {
                target = point;
                return true;
            }
            target = null;
            return false;
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

            showPoints = EditorGUILayout.Foldout(showPoints, "Handle Details");
            if (showPoints)
            {
                DrawControlPointGUI(0, "Start");
                DrawControlPointGUI(3, "End");
            }

            if (GUILayout.Button("Straighten")) Straighten();
        }

        private void Straighten()
        {
            float third = 1f / 3;
            lineBezier.controlPoints[1].position = Vector3.Lerp(lineBezier.controlPoints[0].position, lineBezier.controlPoints[3].position, third);
            lineBezier.controlPoints[2].position = Vector3.Lerp(lineBezier.controlPoints[0].position, lineBezier.controlPoints[3].position, third*2);
            FixStraightEndCaps();
            lineBezier.GenerateMesh();
        }

        bool showPoints = false;
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
