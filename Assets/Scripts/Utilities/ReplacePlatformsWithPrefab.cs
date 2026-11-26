#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class ReplacePlatformsWithPrefab : EditorWindow
{
    [SerializeField] private GameObject targetPrefab;
    [SerializeField] private Transform parentContainer;
    [SerializeField] private string namePrefix = "Plataforma_";

    [MenuItem("Tools/Reemplazar Plataformas por Prefab")]
    private static void OpenWindow()
    {
        GetWindow<ReplacePlatformsWithPrefab>("Reemplazar Plataformas");
    }

    private void OnGUI()
    {
        targetPrefab = (GameObject)EditorGUILayout.ObjectField("Prefab Objetivo", targetPrefab, typeof(GameObject), false);
        parentContainer = (Transform)EditorGUILayout.ObjectField("Contenedor (ej. Torre)", parentContainer, typeof(Transform), true);
        namePrefix = EditorGUILayout.TextField("Prefijo de Nombre", namePrefix);

        EditorGUILayout.Space();

        if (GUILayout.Button("Reemplazar selección actual"))
        {
            ReplaceSelected();
        }
    }

    private void ReplaceSelected()
    {
        if (targetPrefab == null)
        {
            Debug.LogWarning("Asigna primero un Prefab Objetivo.");
            return;
        }

        GameObject[] selected = Selection.gameObjects;
        if (selected == null || selected.Length == 0)
        {
            Debug.LogWarning("Selecciona primero las plataformas en la Hierarchy.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();

        for (int i = 0; i < selected.Length; i++)
        {
            GameObject oldObj = selected[i];
            if (oldObj == null) continue;

            Transform targetParent = parentContainer != null ? parentContainer : oldObj.transform.parent;
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(targetPrefab, targetParent);

            Undo.RegisterCreatedObjectUndo(instance, "Crear instancia de plataforma");

            instance.name = $"{namePrefix}{(i + 1):000}";
            instance.transform.SetPositionAndRotation(oldObj.transform.position, oldObj.transform.rotation);
            instance.transform.localScale = oldObj.transform.localScale;
            instance.transform.SetSiblingIndex(oldObj.transform.GetSiblingIndex());

            Undo.DestroyObjectImmediate(oldObj);
        }

        Undo.CollapseUndoOperations(group);
        Debug.Log($"Reemplazadas {selected.Length} plataformas por {targetPrefab.name}.");
    }
}
#endif