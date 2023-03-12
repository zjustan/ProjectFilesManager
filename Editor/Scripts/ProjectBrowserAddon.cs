using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ProjectBrowserAddon
{

    static ProjectBrowserAddon()
    {

        EditorApplication.projectWindowItemOnGUI += ItemGui;
    }
    public static void ItemGui(string guid, Rect rect)
    {

        if (Application.isPlaying)
        {
            return;
        }

        var assetPath = AssetDatabase.GUIDToAssetPath(guid);
        if (AssetDatabase.IsValidFolder(assetPath))
        {
            return;
        }

        FaultyFile faultyFile = null;
        if (!FaultyAssetHandler.TryGetGUID(new GUID(guid), out faultyFile))
            return;

        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
        if (asset == null)
        {
            return;
        }


        if (IsMainListAsset(rect))
        {
            const int width = 20;
            rect.x -= width - 5;
            rect.width = width;
        }
        else
        {
            int width = (int)Mathf.Min(30, rect.width / 2);
            rect.x += rect.width - width;
            rect.width = width;
            int height = 0;
            if (rect.height == 16)
            {

                height = (int)Mathf.Min(30, rect.height);
            }
            else
            {
                height = width;
            }
            rect.height = height;
        }
        var content = EditorGUIUtility.IconContent("console.warnicon");

        content.tooltip = $"Asset breaking {faultyFile.BrokenRule.Name} rule";
        if( GUI.Button(rect, content))
        {
            FaultyAssetHandler.PerformFix(faultyFile);

            var obj = AssetDatabase.LoadMainAssetAtPath(faultyFile.AssetPath);
            EditorGUIUtility.PingObject(obj);
            Selection.activeObject = obj;
        }
    }

    private static bool IsMainListAsset(Rect rect)
    {
        // Don't draw details if project view shows large preview icons:
        if (rect.height > 20)
        {
            return false;
        }
        // Don't draw details if this asset is a sub asset:
        if (rect.x > 16)
        {
            return false;
        }
        return true;
    }
}
