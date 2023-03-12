
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

public class Rule
{
    public string Name;
    public string Extensions
    {
        get => _Extensions; set
        {
            _Extensions = value;
            ProcessExtensions();
        }
    }
    private string _Extensions;
    public string[] ProcessedExtensions;
    public string[] DetectedAliases;
    public string Path;
    public bool DenyExtensions;
    public bool ApplyToSubfolder = true;
    public string[] IgnoredFiles;

    public Rule()
    {
        ProcessExtensions();
    }
    public bool ComparePaths(string AssetPath, string AssetGUID)
    {
        //check if path is empty
        if (string.IsNullOrEmpty(Path))
            return false;

        //check if file needs to be ignored
        if (IgnoredFiles != null && IgnoredFiles.Contains(AssetGUID))
            return false;

        //create path strings that are safe to check
        var SafeAssetPath = AssetPath.Replace("\\", "/");
        var SafePath = Path.Replace("\\", "/");

        if (SafePath.StartsWith("/"))
        {
            SafePath = SafePath.Substring(1);
        }
        if (!SafePath.EndsWith("/"))
        {
            SafePath += '/';
        }

        if (SafeAssetPath.StartsWith("/"))
        {
            SafeAssetPath = SafeAssetPath.Substring(1);
        }
        //if applies to subfolder only check the beginning of the string
        if (ApplyToSubfolder)
            return SafeAssetPath.StartsWith(SafePath);
        else
        {
            string final = SafeAssetPath.Remove(SafeAssetPath.LastIndexOf('/') + 1);

            return final.Equals(SafePath);
        }

    }

    private void ProcessExtensions()
    {
        List<string> extensions = new List<string>();
        

        //remove unneeded characters and split
        if (!string.IsNullOrEmpty(Extensions))
            extensions.AddRange(Extensions.Replace(" ", string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries));

        List<string> UsedAliases = new List<string>();
        foreach (string extension in extensions.ToArray())
        {
            Alias alias = Alias.DefaultAliases.FirstOrDefault(x => x.Name.ToLower().Equals(extension.ToLower()));
            if (alias != null)
            {
                UsedAliases.Add(extension);
                extensions.Remove(extension);
                extensions.AddRange(alias.Refers);
            }
        }
        DetectedAliases = UsedAliases.ToArray();

        ProcessedExtensions = extensions.ToArray();
    }

    public bool Complies(string extension)
    {
        //check if extension complies with rules

        //if the extension contains any then it will always comply unless deny extension is enabled
        if (_Extensions.ToLower().Equals("any"))
            return !DenyExtensions;

        return (ProcessedExtensions.Contains(extension) != DenyExtensions);
    }


}