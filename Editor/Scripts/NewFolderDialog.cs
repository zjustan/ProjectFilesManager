using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;

public class NewFolderDialog : EditorWindow
{
    private Action<string> OnDialogCompleted;
    private string Path;

    public static void ShowNewFolder(string path, Action<string> OnComplete)
    {
        NewFolderDialog wnd = GetWindow<NewFolderDialog>(true);

        wnd.minSize = new Vector2(300, 100);
        wnd.maxSize = new Vector2(300, 100);
        wnd.Show();
        wnd.Path = path;
        wnd.OnDialogCompleted = OnComplete;
        wnd.titleContent = new GUIContent("Create new folder");
    }

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        Focus();

        // Import UXML
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.zjustan.projectfilesmanager/Editor/UI/NewFolderDialog.uxml");
        VisualElement VisualElement = visualTree.Instantiate();
        var Textfield = VisualElement.Q<TextField>("TextField");
        Textfield.isDelayed = true;
        Textfield.Focus();
        Textfield.RegisterValueChangedCallback<string>(e => {
            OnDialogCompleted(e.newValue);
            Close();
        });


        root.Add(VisualElement);
       
    }
}