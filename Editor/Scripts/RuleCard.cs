using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class RuleCard
{
    private static VisualTreeAsset treeAsset { get; set; }

    private VisualElement RootVisualElement;

    private VisualElement Element;


    const string RuleField = "RuleNameField";
    const string ExtensionsField = "ExtensionsField";
    const string PathField = "PathField";
    const string PathEditorName = "PathEditor";
    const string RemoveButton = "RemoveButton";
    const string DenyToggle = "DenyToggle";
    const string SubfolderToggle = "SubfolderToggle";
    const string AddExtensionsButtons = "AddExtensions";
    const string AddAliasesButtons = "AddAliases";
    const string SuggestionBox = "SuggestionBox";



    Rule rule;

    private TextField extensionsField;
    private TextField pathField;

    private GroupBox PathEditor;


    static RuleCard()
    {
        treeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.zjustan.projectfilesmanager/Editor/UI/RuleCard.uxml");
    }

    public RuleCard(VisualElement root, Rule bindingRule, List<Rule> AllRules)
    {
        RootVisualElement = root;
        rule = bindingRule;

        Element = treeAsset.Instantiate();

        var ruleField = Element.Q<TextField>(RuleField);
        ruleField.value = rule.Name;
        ruleField.RegisterCallback<InputEvent>(e => rule.Name = e.newData);
        ruleField.RegisterCallback<InputEvent>(e => InitDirty());

        extensionsField = Element.Q<TextField>(ExtensionsField);
        extensionsField.value = rule.Extensions;
        extensionsField.RegisterCallback<InputEvent>(e => rule.Extensions = e.newData);
        extensionsField.RegisterCallback<InputEvent>(e => InitDirty());


        pathField = Element.Q<TextField>(PathField);
        pathField.value = rule.Path;
        pathField.RegisterCallback<InputEvent>(e => {
            rule.Path = e.newData;
            InitDirty();
            CreatePathEditor();
        });

        var denyToggle = Element.Q<Toggle>(DenyToggle);
        denyToggle.value = rule.DenyExtensions;
        denyToggle.RegisterValueChangedCallback(e =>
        {
            rule.DenyExtensions = e.newValue;
            SetExtensionslabel(!rule.DenyExtensions);
            InitDirty();
        });


        var subfolderToggle = Element.Q<Toggle>(SubfolderToggle);
        subfolderToggle.value = rule.ApplyToSubfolder;
        subfolderToggle.RegisterValueChangedCallback(e =>
        {
            rule.ApplyToSubfolder = e.newValue;
            InitDirty();
        });

        Element.Q<Button>(RemoveButton).clicked += () =>
        {
            AllRules.Remove(bindingRule);
            root.Remove(Element);
            InitDirty();
        };



        PathEditor = Element.Q<GroupBox>(PathEditorName);
        CreatePathEditor();

        CreateSuggestions();

        SetExtensionslabel(!rule.DenyExtensions);
        root.Add(Element);
    }

    private void InitDirty()
    {
       var window = ProjectFilesWindow.focusedWindow as ProjectFilesWindow;
        window.SetSettingsDirty();
    }

    private void CreateSuggestions()
    {
        var suggestionBox = Element.Q<VisualElement>(SuggestionBox);


        var extensionsParent = Element.Q<VisualElement>(AddExtensionsButtons);
        extensionsParent.Clear();

        var test = Application.dataPath.Replace("Assets", "");
        var CommonExtensions = Directory.EnumerateFiles(Path.Combine(test, rule.Path))
            .Select(e => new FileInfo(e).Extension)
            .Where(e => (e != ".meta"))
            .GroupBy(e => e)
            .OrderByDescending(e => e.Count())
            .Select(e => e.Key).ToArray();

        foreach (var extension in CommonExtensions.Where(e =>  !rule.ProcessedExtensions.Contains(e)))
        {
            var button = new Button(() =>
            {
                rule.Extensions += $", {extension}";
                extensionsField.value = rule.Extensions;
            });
            button.text = extension;
            extensionsParent.Add(button);
        }



        var ExtensionSuggestionTitle = suggestionBox[suggestionBox.IndexOf(extensionsParent) - 1];
        if (extensionsParent.childCount == 0)
        {
            ExtensionSuggestionTitle.Hide();
            extensionsParent.Hide();
        }
        else
        {
            ExtensionSuggestionTitle.Show();
            extensionsParent.Show();
        }



        var aliasParent = Element.Q<VisualElement>(AddAliasesButtons);
        aliasParent.Clear();

        if (!rule.DetectedAliases.Contains("any"))
            AddAliasRecommendations(aliasParent, CommonExtensions);

        var AliasSuggestionTitle = suggestionBox[suggestionBox.IndexOf(aliasParent) - 1];
        if (aliasParent.childCount == 0)
        {
            AliasSuggestionTitle.Hide();
            aliasParent.Hide();
        }
        else
        {
            AliasSuggestionTitle.Show();
            aliasParent.Show();
        }
    }

    private void AddAliasRecommendations(VisualElement aliasParent, string[] Extensions)
    {

        var ShowAliases = Alias.DefaultAliases.Where(x => !rule.DetectedAliases.Contains(x.Name)).ToArray();

        if (Extensions.Length > 0)
            ShowAliases = ShowAliases.Where(e => e.Refers.Any(Extensions.Contains)).ToArray();

        foreach (var alias in ShowAliases)
        {

            var button = new Button(() =>
            {
                if (string.IsNullOrEmpty(rule.Extensions))
                    rule.Extensions = alias.Name;
                else if (rule.Extensions.EndsWith(','))
                    rule.Extensions += $" {alias.Name}";
                else
                    rule.Extensions += $", {alias.Name}";
                extensionsField.value = rule.Extensions;
            });
            button.text = alias.Name;
            button.tooltip = $"adds an alias for the files: {(string.IsNullOrEmpty(alias.ToolTip) ? string.Join(", ", alias.Refers) : alias.ToolTip)}";
            aliasParent.Add(button);
        }
    }

    public void CreatePathEditor()
    {
        if (!AssetDatabase.IsValidFolder(rule.Path))
        {
            if (pathField.panel.focusController.focusedElement == pathField)
                return;
            rule.Path = "Assets/";
        }
        PathEditor.Clear();

        string[] PathPart = rule.Path.Split('/', '\\', System.StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < PathPart.Length; i++)
        {
            if (i == 0)
                PathEditor.Add(new Label(PathPart[i]));
            else
            {
                string path = Path.Combine(PathPart[0..i]);
                var button = new Button(() => CreateOptionsMenu(path));
                button.text = PathPart[i];
                PathEditor.Add(button);
            }

            PathEditor.Add(new Label("/"));


        }

        var AddButton = new Button(() => CreateOptionsMenu(rule.Path, false, false));
        AddButton.text = "+";
        PathEditor.Add(AddButton);
    }

    public void CreateOptionsMenu(string path, bool DeepSearch = false, bool allowSelectParent = true)
    {
        path = path.TrimEnd('/', '\\');
        var SubPaths = AssetDatabase.GetSubFolders(path);

        GenericMenu genericMenu = new GenericMenu();
        if (DeepSearch)
        {
            List<string> paths = new List<string>(SubPaths);
            List<string> AllPaths = new List<string>(SubPaths);
            for (int i = 0; i < 3; i++)
            {
                List<string> NewPaths = new List<string>();

                foreach (string SearchPath in paths)
                {

                    NewPaths.AddRange(AssetDatabase.GetSubFolders(SearchPath));
                }
                AllPaths.AddRange(NewPaths);
                paths = NewPaths;
            }

            SubPaths = AllPaths.ToArray();
        }

        foreach (var subPath in SubPaths.OrderByDescending(e => e.Length))
        {
            string viewpath = subPath.Remove(0, path.Length + 1);
            genericMenu.AddItem(new GUIContent(viewpath), false, () => SetPath(subPath));
        }
        genericMenu.AddSeparator(string.Empty);
        if (allowSelectParent)
            genericMenu.AddItem(new GUIContent("Select Parent"), false, () =>
            {
                string splittedPath = Path.Combine(path.Split('/', '\\', System.StringSplitOptions.RemoveEmptyEntries)[0..^1]);

                SetPath(path.Replace('\\','/'));
            });

        genericMenu.AddItem(new GUIContent("(+) New folder"), false, () =>
        {

            AssetDatabase.CreateFolder(path, "New folder");
            EditorUtility.FocusProjectWindow();
        }
        );
        genericMenu.ShowAsContext();
    }

    public void SetPath(string Path)
    {
        rule.Path = Path;
        CreatePathEditor();
        CreateSuggestions();

        var pathField = Element.Q<TextField>(PathField);
        pathField.value = rule.Path;
    }
    private void SetExtensionslabel(bool Allow)
    {
        extensionsField.label = ((Allow) ? "Allowed" : "Denied") + " Extensions";
    }
}
