using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class FinalProjectGroundPlaneBuilder
{
    private const string ScenePath = "Assets/Scenes/FinalProjectScene.unity";
    private const string SessionKey = "FinalProjectGroundPlaneBuilder_Applied_NormalPlane1";
    private const string OldPlaneName = "Plane Pro builder";
    private const string NewPlaneName = "Grass Plane";

    static FinalProjectGroundPlaneBuilder()
    {
        EditorApplication.delayCall += ApplyOnceAfterCompile;
    }

    [MenuItem("Tools/Grass Tools/Replace Ground With Normal Plane")]
    public static void ApplyNow()
    {
        ReplaceGroundPlane(force: true);
    }

    private static void ApplyOnceAfterCompile()
    {
        if (SessionState.GetBool(SessionKey, false))
        {
            return;
        }

        SessionState.SetBool(SessionKey, true);
        ReplaceGroundPlane(force: false);
    }

    private static void ReplaceGroundPlane(bool force)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        bool sceneWasOpen = activeScene.path == ScenePath;
        Scene scene = sceneWasOpen
            ? activeScene
            : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameObject oldPlane = GameObject.Find(OldPlaneName);
        GameObject existingNewPlane = GameObject.Find(NewPlaneName);

        if (oldPlane == null)
        {
            if (existingNewPlane != null || !force)
            {
                return;
            }

            Debug.LogWarning("FinalProjectGroundPlaneBuilder could not find the ProBuilder ground plane.");
            return;
        }

        MeshRenderer oldRenderer = oldPlane.GetComponent<MeshRenderer>();
        Transform oldTransform = oldPlane.transform;

        GameObject newPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        newPlane.name = NewPlaneName;
        newPlane.layer = oldPlane.layer;
        newPlane.tag = oldPlane.tag;

        Transform newTransform = newPlane.transform;
        newTransform.SetPositionAndRotation(oldTransform.position, oldTransform.rotation);
        newTransform.localScale = oldTransform.localScale;

        MeshRenderer newRenderer = newPlane.GetComponent<MeshRenderer>();
        if (newRenderer != null && oldRenderer != null)
        {
            newRenderer.sharedMaterials = oldRenderer.sharedMaterials;
            newRenderer.shadowCastingMode = oldRenderer.shadowCastingMode;
            newRenderer.receiveShadows = oldRenderer.receiveShadows;
            newRenderer.lightProbeUsage = oldRenderer.lightProbeUsage;
            newRenderer.reflectionProbeUsage = oldRenderer.reflectionProbeUsage;
        }

        MeshCollider newCollider = newPlane.GetComponent<MeshCollider>();
        if (newCollider != null)
        {
            newCollider.convex = false;
        }

        SubdividedPlaneGenerator generator = newPlane.AddComponent<SubdividedPlaneGenerator>();
        SerializedObject serializedGenerator = new SerializedObject(generator);
        serializedGenerator.FindProperty("width").floatValue = 10f;
        serializedGenerator.FindProperty("length").floatValue = 10f;
        serializedGenerator.FindProperty("subdivisions").intValue = 150;
        serializedGenerator.ApplyModifiedPropertiesWithoutUndo();
        generator.Rebuild();

        Object.DestroyImmediate(oldPlane);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("FinalProjectScene ground plane replaced with a normal subdivided plane.");
    }
}
