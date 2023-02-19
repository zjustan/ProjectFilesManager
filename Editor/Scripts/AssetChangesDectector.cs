using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

class AssetChangesDectector : AssetPostprocessor
{
    public static Action OnAssetChanged;
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
    {
        if (OnAssetChanged != null)
            OnAssetChanged();
    }
}
