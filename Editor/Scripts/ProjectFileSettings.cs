using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


public static class ProjectFileSettings 
{
    public static List<Rule> rules;

    public static void SaveSettings()
    {
        var splittedPath = Application.dataPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var path = string.Join("/", splittedPath[0..^1]);

        path += "/ProjectSettings/ProjectFileRules.json";

        var formatter = new JsonSerializerSettings().Formatting = Formatting.Indented;
        string json = JsonConvert.SerializeObject(rules, formatter);
        File.WriteAllText(path, json);
    }

    public static void LoadSettings()
    {
        var splittedPath = Application.dataPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var path = string.Join("/", splittedPath[0..^1]);

        path += "/ProjectSettings/ProjectFileRules.json";

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            rules = JsonConvert.DeserializeObject<List<Rule>>(json);
        }
        else
        {
            rules = new List<Rule>();
        }
    }


}
