using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SplineEditor
{
    //[CustomEditor(typeof(SceneHelper))]
    internal class LineHelper// : Editor
    {
        private const string MenuName = "Tools/Line Helper";
        private const string SettingName = "Line Helper";

        public static bool IsEnabled
        {
            get { return EditorPrefs.GetBool(SettingName, true); }
            set { EditorPrefs.SetBool(SettingName, value); }
        }

        [MenuItem(MenuName)]
        public static void ToggleHelperMenuItem()
        {
            IsEnabled = !IsEnabled;
            ToggleHelper();
        }

        public static void ToggleHelper()
        {
            if (IsEnabled) SceneView.duringSceneGui += OnSceneGUI;
            else SceneView.duringSceneGui -= OnSceneGUI;
        }

        [MenuItem(MenuName, true)]
        public static bool ToggleHelperValidate()
        {
            Menu.SetChecked(MenuName, IsEnabled);
            return true;
        }

        static LineHelper()
        {
            EditorApplication.delayCall += () => ToggleHelper();
        }

        public static void OnSceneGUI(SceneView sceneView)
        {
            Event e = Event.current;
            if (e.button == 0 && (e.type == EventType.MouseDown || e.type == EventType.MouseUp))
            {
                Ray r = HandleUtility.GUIPointToWorldRay(e.mousePosition);

                RaycastHit2D hitInfo = Physics2D.GetRayIntersection(r, 100f);
                if (hitInfo.collider != null)
                {
                    Debug.Log("b");
                    EditorApplication.delayCall += () => { Selection.activeGameObject = hitInfo.collider.gameObject; };
                    e.Use();
                }
            }
        }
    }
}
