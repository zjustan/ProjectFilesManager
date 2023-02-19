using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public class FileCard
{
    private static VisualTreeAsset treeAsset { get; set; }
    public VisualElement RootvisualElement { get; private set; }

    private VisualElement element;
    public string FilePath { get; private set; }
    public string AssetPath { get; private set; }
    public string FileName { get; private set; }

    public Object AssetObject { get; private set; }

    public FileInfo AssetFileInfo { get; private set; }
    public Rule BrokenRule { get; private set; }

    public Action OnFixClick
    {
        set
        {
            element.Q<Button>(FixButton).clicked += value;
        }

    }

    public Action OnMoreClick
    {
        set
        {
            element.Q<Button>(MoreButton).clicked += value;
        }

    }
    public Action OnCardClick { get; set; }

    private const string titleName = "Title";
    private const string pathName = "Path";
    private const string MainName = "MainBox";
    private const string FixButton = "Fix";
    private const string MoreButton = "More";
    private const string Message = "Message";
    private const string WarningIcon = "WarningIcon";
    private const string ErrorIcon = "ErrorIcon";
    private const string SuccesIcon = "SuccesIcon";

    static FileCard()
    {
        treeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.zjustan.projectfilesmanager/Editor/UI/MyFileCard.uxml");
    }
    public FileCard(VisualElement RootvisualElement, string FilePath, string AssetPath, Rule rule, string Filename)
    {
        this.RootvisualElement = RootvisualElement;
        this.FilePath = FilePath;
        this.AssetPath = AssetPath;
        this.FileName = Filename;
        this.BrokenRule = rule;

        AssetObject = AssetDatabase.LoadMainAssetAtPath(AssetPath);
        AssetFileInfo = new FileInfo(FilePath);

        element = treeAsset.Instantiate();

        element.Q<GroupBox>(MainName)
        .AddManipulator(
        new Clickable(evt =>
        {
            EditorGUIUtility.PingObject(AssetObject);
            Selection.activeObject = AssetObject;
        })
    );

        element.Q<Label>(titleName).text = Filename;
        element.Q<Label>(pathName).text = AssetPath;

        SetStatusIcon(WarningIcon);

        RootvisualElement.Add(element);

    }

    private void SetStatusIcon(string Icon)
    {
        element.HideChild(ErrorIcon);
        element.HideChild(SuccesIcon);
        element.HideChild(WarningIcon);

        element.ShowChild(Icon);
    }

    internal void ShowSucces(string Text)
    {
        SetStatusIcon(SuccesIcon);
        SetTextbox(Text);
    }

    internal void ShowWarning(string Text)
    {
        SetStatusIcon(WarningIcon);
        SetTextbox(Text);
    }

    internal void ShowError(string Text)
    {
        SetStatusIcon(ErrorIcon);
        SetTextbox(Text);
    }
    private void SetTextbox(string Text)
    {
        var msgbox = element.Q<Label>(Message);
        msgbox.text = Text;
    }
}
