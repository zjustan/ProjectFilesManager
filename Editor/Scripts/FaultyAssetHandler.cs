using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

class FaultyAssetHandler : AssetPostprocessor
{

    private static readonly char[] SeperatorChars = new char[2] { Path.PathSeparator, Path.AltDirectorySeparatorChar };

    public static Action OnAssetChanged;

    public static Task SearchTask;

    public static List<FaultyFile> faultyFiles = null;

    private static bool NoFaultyfiles => faultyFiles == null || faultyFiles.Count == 0;

    private static int progressId;

    private static string datapath;



    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
    {

        StartNewSearch(deletedAssets, importedAssets.Concat(movedAssets), importedAssets.Length + movedAssets.Length);
    }

    public static void StartNewSearch(string[] RemoveAssets,IEnumerable<string> AssetsToSearch, int count)
    {

        //check if list exists
        if (NoFaultyfiles)
            faultyFiles = new List<FaultyFile>();


        //stop previous progress bar
        if (Progress.Exists(progressId))
            Progress.Remove(progressId);

        //create new progressabar
        progressId = Progress.Start("Scanning files for broken rules", null, Progress.Options.Managed | Progress.Options.Sticky);


        // remove all references from deleted files
        foreach (var item in RemoveAssets)
        {
            GUID guid = AssetDatabase.GUIDFromAssetPath(item);
            if(TryGetPath(item,out var file))
            {
                faultyFiles.Remove(file);
            }
            foreach(Rule rule in ProjectFileSettings.rules)
            {
                if(rule.IgnoredFiles != null)
                {
                    rule.IgnoredFiles = rule.IgnoredFiles.Where(e => e != guid.ToString()).ToArray();
                }
            }
        }

        //load rules if not yet loaded
        if (ProjectFileSettings.rules == null)
        {
            Progress.Report(progressId, 0, "Loading rules");
            ProjectFileSettings.LoadSettings();
        }

        //set datapath for multithreading
        datapath = Application.dataPath;

        //transform data
        var AssetsObject = AssetsToSearch.Select(e => (e, AssetDatabase.AssetPathToGUID(e))).ToArray();
        
        //create task
        SearchTask = new Task(() => PerformSearch(AssetsObject, count));
        SearchTask.Start();
        SearchTask.GetAwaiter().OnCompleted(() =>
        {

            //if error occoured,show in inspector
            if (SearchTask.IsFaulted)
            {
                Progress.Report(progressId, 1, SearchTask.Exception.ToString());
                Debug.LogException(SearchTask.Exception);
                Progress.Finish(progressId, Progress.Status.Failed);
            }

            //update window if needed
            if (OnAssetChanged != null)
                OnAssetChanged();
        });
    }


    public static void PerformSearch((string assetpath, string guid)[] AssetsToSearch, int totalCount)
    {

        float Index = 0;

        // loop through each asset
        foreach (var AssetToSearch in AssetsToSearch)
        {
            Rule rule;

            //check if asset has already been found in a previous search
            if (TryGetGUID(new GUID( AssetToSearch.guid), out var ExistingReference))
            {
                //remove if asset does not break rules anymore
                if (!TryFindBrokenRule(AssetToSearch.assetpath, ExistingReference.FileInfo.Extension, AssetToSearch.guid, out rule))
                {
                    faultyFiles.Remove(ExistingReference);
                }
            }
            //get path relative from the drive
            string CompletePath = datapath + AssetToSearch.assetpath.Remove(0, "Assets".Length);
            FileInfo fileInfo = new FileInfo(CompletePath);

            //check if rule is broken, and add to list if it has broken a rule
            if (TryFindBrokenRule(AssetToSearch.assetpath, fileInfo.Extension, AssetToSearch.guid, out rule))
            {
                var faultyFile = new FaultyFile
                {
                    AbsolutePath = CompletePath,
                    AssetPath = AssetToSearch.assetpath,
                    BrokenRule = rule,
                    FileInfo = fileInfo,
                    assetGUID = new GUID(AssetToSearch.guid),
                    UnityObject = null,
                };

                faultyFiles.Add(faultyFile);
            }


            Progress.Report(progressId, Index / totalCount);
            Index++;
        }
        //show in editor that this task has been completed
        Progress.SetDescription(progressId,$"{totalCount} files have been checked, {faultyFiles.Count} files have a problem");
        Progress.Finish(progressId);
    }

    public static bool TryFindBrokenRule(string AssetPath, string Extension, string GUID, out Rule rule)
    {
        rule = null;
        foreach (var possibleRule in ProjectFileSettings.rules)
        {
            if (CheckIfRuleIsBroken(possibleRule, AssetPath, Extension, GUID))
            {
                rule = possibleRule;
                return true;
            }
        }
        return false;
    }
    public static bool CheckIfRuleIsBroken(Rule rule, string AssetPath, string Extension, string GUID)
    {
        if (!rule.ComparePaths(AssetPath, GUID))
            return false;

        return !(rule.Complies(Extension));
    }
    public static void PerformFix(FaultyFile faultyFile)
    {
        //try to find a fixing rule
        Rule FixingRule = null;
        if (!TryFindFixingRule(faultyFile, out FixingRule))
        {
            return;
        }


        //create path if does not exist
        if (!AssetDatabase.IsValidFolder(faultyFile.FixingRule.Path))
        {

            string[] PathParts = FixingRule.Path.Split(SeperatorChars, StringSplitOptions.RemoveEmptyEntries);
            string[] ParentPath = PathParts[0..^1];
            AssetDatabase.CreateFolder(string.Join('\\', ParentPath), PathParts[^1]);
        }

        //move asset
        string NewAssetPath = Path.Combine(FixingRule.Path, faultyFile.FileInfo.Name);
        faultyFile.Message = AssetDatabase.MoveAsset(faultyFile.AssetPath, NewAssetPath);


        //if succesful, mark it as fixed and remove from faulty files
        if (string.IsNullOrEmpty(faultyFile.Message))
        {
            faultyFile.AssetPath = NewAssetPath;
            faultyFile.FixingRule = FixingRule;
            faultyFile.IsFixed = true;
            faultyFiles.Remove(faultyFile);
        }
    }


    private static bool TryFindFixingRule(FaultyFile info, out Rule rule)
    {

        rule = null;
        foreach (var possibleRule in ProjectFileSettings.rules)
        {
            if (CheckIfRuleCanFix(possibleRule, info))
            {
                rule = possibleRule;
                return true;
            }
        }
        return false;
    }

    private static bool CheckIfRuleCanFix(Rule rule, FaultyFile info)
    {
        return rule.Complies(info.FileInfo.Extension);
    }




    internal static bool ContainsGUID(GUID guid)
    {
        if (NoFaultyfiles)
            return false;
        return faultyFiles.Any(e => e.assetGUID == guid);
    }

    internal static bool TryGetGUID(GUID guid, out FaultyFile file)
    {
        file = null;
        if (NoFaultyfiles)
            return false;

        file = faultyFiles.FirstOrDefault(e => e.assetGUID == guid);
        return file != null;
    }

    internal static bool ContainsPath(string path)
    {
        if (NoFaultyfiles)
            return false;
        return faultyFiles.Any(e => e.AssetPath == path);
    }

    internal static bool TryGetPath(string path, out FaultyFile file)
    {
        file = null;
        if (NoFaultyfiles)
            return false;

        file = faultyFiles.FirstOrDefault(e => e.AssetPath == path);
        return file != null;
    }
}

public class FaultyFile
{
    public string AbsolutePath;
    public string AssetPath;
    public string Message;
    public FileInfo FileInfo;
    public Object UnityObject;
    public Rule BrokenRule;
    public Rule FixingRule;
    public GUID assetGUID;

    public bool IsFixed;
}
