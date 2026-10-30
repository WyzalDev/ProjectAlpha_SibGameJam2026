using _Game.Scripts.Core.Features.Enemies.Behaviours;
using UnityEditor;
using UnityEngine;

namespace _Game.Scripts.Editor
{
    [CustomEditor(typeof(RobotPatrolBehaviour))]
    public sealed class RobotPatrolBehaviourEditor : UnityEditor.Editor
    {
        private void OnSceneGUI()
        {
            serializedObject.Update();
            SerializedProperty points = serializedObject.FindProperty("_patrolPoints");
            if (points == null || points.arraySize == 0)
                return;

            Handles.color = new Color(0.2f, 1f, 0.35f, 1f);
            for (int i = 0; i < points.arraySize; i++)
            {
                Transform point = points.GetArrayElementAtIndex(i).objectReferenceValue as Transform;
                if (point == null)
                    continue;

                Handles.SphereHandleCap(
                    GUIUtility.GetControlID(FocusType.Passive),
                    point.position,
                    Quaternion.identity,
                    HandleUtility.GetHandleSize(point.position) * 0.16f,
                    EventType.Repaint);
                Handles.Label(point.position + Vector3.up * 0.25f, $"Patrol {i + 1}");
            }
        }
    }
}