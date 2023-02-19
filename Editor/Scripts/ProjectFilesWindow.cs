using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.TerrainUtils;
using UnityEngine.UIElements;

public class ProjectFilesWindow : EditorWindow
{
    private const string ProjectFileWindowIndex = "ProjectFileWindowIndex";
    public readonly char[] SeperatorChars = new char[2] { Path.PathSeparator, Path.AltDirectorySeparatorChar };
    private static ProjectFilesWindow window;

    VisualElement FileTreeUI;
    VisualElement SettingsUI;
    VisualElement UIParent;


    List<Rule> rules;
    Dictionary<VisualElement, Action> FunctionHandler;

    public bool IsDirty;
    
    [MenuItem("Window/Zjustan/Project files manager")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        window = (ProjectFilesWindow)EditorWindow.GetWindow<ProjectFilesWindow>(false, "Project file manager");
        window.Show();

    }
    private void CreateGUI()
    {

        LoadSettings();

        var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.zjustan.projectfilesmanager/Editor/UI/ProjectFilesUI.uxml");


        rootVisualElement.Add(visualTreeAsset.Instantiate());

        //load sub UI
        FileTreeUI = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.zjustan.projectfilesmanager/Editor/UI/MyFilesUI.uxml").Instantiate();
        SettingsUI = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.zjustan.projectfilesmanager/Editor/UI/SettingsUI.uxml").Instantiate();

        //Add UI
        UIParent = rootVisualElement.Q<VisualElement>("UIParent");

        UIParent.Add(FileTreeUI);
        UIParent.Add(SettingsUI);

        //assign buttons
        rootVisualElement.Q<Button>("FileButton").clicked += () => Select(0);
        rootVisualElement.Q<Button>("SettingsButton").clicked += () => Select(1);

        FunctionHandler = new Dictionary<VisualElement, Action>
        {
            { FileTreeUI,FileTree},
            { SettingsUI,Settings},
        };

        AssetChangesDectector.OnAssetChanged += FileTree;


        if (EditorPrefs.HasKey(ProjectFileWindowIndex))
            Select(EditorPrefs.GetInt(ProjectFileWindowIndex));
        else
            Select(0);


    }

    public void Select(int index)
    {
        VisualElement UIToActivate = null;

        switch (index)
        {
            case 0:
                UIToActivate = FileTreeUI;
                break;
            case 1:
                UIToActivate = SettingsUI;
                break;
            default:
                return;
        }


        foreach (VisualElement element in UIParent.Children())
        {
            element.visible = false;
            element.SetEnabled(false);

        }

        UIToActivate.visible = true;
        UIToActivate.SetEnabled(true);
        UIToActivate.SendToBack();

        FunctionHandler[UIToActivate]();

        EditorPrefs.SetInt(ProjectFileWindowIndex, index);
    }

    public void FileTree()
    {
        var scrollview = FileTreeUI.Q<ScrollView>("View");
        scrollview.Clear();
        var cards = Search(Application.dataPath, scrollview);
        if (cards.Count == 0)
            scrollview.Add(new Label("There are no issues with any files :)"));
        var fixAllButton = FileTreeUI.Q<Button>("FixAllAction");
        fixAllButton.style.display = (cards.Count > 1) ? DisplayStyle.Flex : DisplayStyle.None;
        fixAllButton.clicked += () => cards.ForEach(e => FixAsset(e, false));

    }

    public List<FileCard> Search(string SourcePath, ScrollView scrollView)
    {
        List<FileCard> cards = new List<FileCard>();
        foreach (string file in Directory.EnumerateFiles(SourcePath, "*.*", SearchOption.AllDirectories).Where(x => !x.EndsWith(".meta")))
        {
            FileInfo fileInfo = new FileInfo(file);

            string AssetString = file.Remove(0, SourcePath.Length - "Assets".Length);

            Rule rule;
            if (!TryFindBrokenRule(AssetString, fileInfo.Extension, out rule))
                continue;


            string viewString = AssetString;
            if (viewString.StartsWith('\\'))
                viewString = viewString.Substring(1);


            FileCard card = new FileCard(scrollView, file, AssetString, rule, viewString.Split('\\').Last());
            card.OnFixClick = () => FixAsset(card, true);
            card.OnMoreClick = () => MoreOptions(card);
            card.ShowWarning("broken " + rule.Name + " Rule");

            cards.Add(card);
        }

        return cards;

    }

    private void MoreOptions(FileCard card)
    {
        GenericMenu OptionsMenu = new GenericMenu();
        OptionsMenu.AddItem(new GUIContent($"ignore {card.AssetFileInfo.Extension}", $"adds {card.AssetFileInfo.Extension} to the allowed extensions"), false, () => AddExtensionToRule(card));
        OptionsMenu.AddItem(new GUIContent($"ignore this asset", $"adds the asset GUID to a list this rule should ignore"), false, () => AddGUIDToRule(card));

        OptionsMenu.ShowAsContext();
    }

    private void AddExtensionToRule(FileCard card)
    {
        string Extension = card.AssetFileInfo.Extension;
        if (!card.BrokenRule.Extensions.EndsWith(','))
            Extension = $",{Extension}";
        card.BrokenRule.Extensions += Extension;

        SaveSettings();
    }

    private void AddGUIDToRule(FileCard card)
    {
        string GUID = AssetDatabase.AssetPathToGUID(card.AssetPath);
        if (card.BrokenRule.IgnoredFiles == null)
            card.BrokenRule.IgnoredFiles = new string[1] { GUID };
        else
            card.BrokenRule.IgnoredFiles = card.BrokenRule.IgnoredFiles.Append(GUID).ToArray();

        SaveSettings();

    }
    public bool TryFindBrokenRule(string AssetPath, string Extension, out Rule rule)
    {
        rule = null;
        foreach (var possibleRule in rules)
        {
            if (CheckIfRuleIsBroken(possibleRule, AssetPath, Extension))
            {
                rule = possibleRule;
                return true;
            }
        }
        return false;
    }

    public bool TryFindFixingRule(FileCard info, out Rule rule)
    {
        rule = null;
        foreach (var possibleRule in rules)
        {
            if (CheckIfRuleCanFix(possibleRule, info))
            {
                rule = possibleRule;
                return true;
            }
        }
        return false;
    }
    public bool CheckIfRuleCanFix(Rule rule, FileCard info)
    {
        return rule.Complies(info.AssetFileInfo.Extension);
    }

    public bool CheckIfRuleIsBroken(Rule rule, string AssetPath, string Extension)
    {
        if (!rule.ComparePaths(AssetPath))
            return false;

        return !(rule.Complies(Extension));
    }
    public void FixAsset(FileCard card, bool ShowAssetAfter = false)
    {

        Rule rule = null;
        if (!TryFindFixingRule(card, out rule))
        {
            card.ShowError("There is no rule to fix this, please resolve manualy");
            return;
        }

        if (!AssetDatabase.IsValidFolder(rule.Path))
        {
            string[] PathParts = rule.Path.Split(SeperatorChars, StringSplitOptions.RemoveEmptyEntries);
            string[] ParentPath = PathParts[0..^1];
            AssetDatabase.CreateFolder(string.Join('\\', ParentPath), PathParts[^1]);
        }

        string NewAssetPath = Path.Combine(rule.Path, card.AssetFileInfo.Name);
        string result = AssetDatabase.MoveAsset(card.AssetPath, NewAssetPath);

        if (string.IsNullOrEmpty(result))
        {
            card.ShowSucces("File has been moved to " + rule.Path);
            if (ShowAssetAfter)
            {
                var obj = AssetDatabase.LoadMainAssetAtPath(NewAssetPath);
                EditorGUIUtility.PingObject(obj);
                Selection.activeObject = obj;
            }
        }
        else
        {
            card.ShowError(result);

        }
    }

    public void Settings()
    {
        var ParentElement = SettingsUI.Q<ScrollView>("RuleCards");
        ParentElement.Clear();

        if(rules != null)
        foreach (Rule rule in rules)
        {
            new RuleCard(ParentElement, rule, rules);
        }
        var newButton = SettingsUI.Q<Button>("NewButton");
        newButton.clicked += () =>
        {
            var rule = new Rule();
            rules.Add(rule);
            Settings();
        };

        var saveButton = SettingsUI.Q<Button>("SaveButton");
        saveButton.clicked += SaveSettings;

        var ReloadButton = SettingsUI.Q<Button>("LoadButton");
        ReloadButton.clicked += () => {

            LoadSettings();
            Settings();
                
                };
    }

    public void SaveSettings()
    {
        var splittedPath = Application.dataPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var path = string.Join("/", splittedPath[0..^1]);

        path += "/ProjectSettings/ProjectFileRules.json";

        if (!File.Exists(path))
        {
            File.Create(path);
        }

        var formatter = new JsonSerializerSettings().Formatting = Formatting.Indented;
        string json = JsonConvert.SerializeObject(rules, formatter);
        File.WriteAllText(path, json);

        SetSettingsClean();


    }

    public void SetSettingsDirty()
    {
        if (IsDirty)
            return;

        IsDirty = true;
        titleContent.text = titleContent.text + '*';
    }

    private void SetSettingsClean()
    {
        if (!IsDirty)
            return;

        IsDirty = false;

        titleContent.text = titleContent.text.TrimEnd('*');

    }
    public void LoadSettings()
    {
        if(IsDirty)
        {
           bool result =  EditorUtility.DisplayDialog("Unsaved changes",
                                                      "You are about to reload the settings, but there are changes that are not saved, do you wish to continue",
                                                      "yes",
                                                      "no");
            if (!result)
                return;
        }
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
        SetSettingsClean();

    }


    private void OnDestroy()
    {
        if (IsDirty)
        {
            bool result = EditorUtility.DisplayDialog("Unsaved changes",
                                                     "You are about to close the window, but there are changes that are not saved, do you wish to continue",
                                                     "Save and close",
                                                     "Dont save and close"
                                                     );


            if (!result)
            {
                SaveSettings();
            }
        }
    }
}

public class Data
{
    public List<Rule> rules;
    public List<Alias> aliases;
}
