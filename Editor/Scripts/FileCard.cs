using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>
/// responsible for rendering a single file information card
/// </summary>
public class FileCard
{
    private static VisualTreeAsset treeAsset { get; set; }
    public VisualElement RootvisualElement { get; private set; }

    private VisualElement element;

    public readonly FaultyFile faultyFile;
    public string FilePath { get => faultyFile.AbsolutePath; }
    public string AssetPath { get => faultyFile.AssetPath; }
    public string FileName { get => faultyFile.FileInfo.Name; }

    public Object AssetObject { 
        get => faultyFile.UnityObject; 
        set => faultyFile.UnityObject = value; 
    }

    public FileInfo AssetFileInfo { get => faultyFile.FileInfo; }
    public Rule BrokenRule { get => faultyFile.BrokenRule; }

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
    public FileCard(VisualElement RootvisualElement, FaultyFile faultyFileRef)
    {
        faultyFile = faultyFileRef;
        this.RootvisualElement = RootvisualElement;

        AssetObject = AssetDatabase.LoadMainAssetAtPath(AssetPath);

        element = treeAsset.Instantiate();

        element.Q<GroupBox>(MainName)
        .AddManipulator(
        new Clickable(evt =>
        {
            EditorGUIUtility.PingObject(AssetObject);
            Selection.activeObject = AssetObject;
        })
    );

        element.Q<Label>(titleName).text = FileName;
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
