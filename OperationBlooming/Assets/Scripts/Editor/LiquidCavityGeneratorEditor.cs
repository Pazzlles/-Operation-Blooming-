
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LiquidCavityGenerator))]
public class LiquidCavityGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(12);

        if (GUILayout.Button(
            "Generate Liquid Mesh",
            GUILayout.Height(36)))
        {
            GenerateAndSave();
        }
    }

    private void GenerateAndSave()
    {
        LiquidCavityGenerator generator =
            (LiquidCavityGenerator)target;

        string path = EditorUtility.SaveFilePanelInProject(
            "Save Liquid Mesh",
            "Liquid_Cavity",
            "asset",
            "Choose where to save the generated mesh."
        );

        if (string.IsNullOrEmpty(path))
            return;

        Mesh generated = null;

        try
        {
            generated = generator.GenerateMesh();

            AssetDatabase.CreateAsset(generated, path);
            AssetDatabase.SaveAssets();

            MeshFilter filter =
                generator.GetComponent<MeshFilter>();

            Undo.RecordObject(
                filter,
                "Assign Generated Liquid Mesh"
            );

            filter.sharedMesh = generated;

            PrefabUtility
                .RecordPrefabInstancePropertyModifications(filter);

            EditorUtility.SetDirty(filter);

            Debug.Log(
                "Liquid mesh generated: " + path,
                generator
            );

            EditorGUIUtility.PingObject(generated);
        }
        catch (System.Exception exception)
        {
            // Если меш не удалось сохранить,
            // уничтожаем временный экземпляр.
            if (generated != null &&
                !AssetDatabase.Contains(generated))
            {
                DestroyImmediate(generated);
            }

            Debug.LogError(
                "Liquid generation failed: " +
                exception.Message,
                generator
            );
        }
    }
}
#endif
