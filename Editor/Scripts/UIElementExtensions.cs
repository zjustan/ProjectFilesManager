using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public static class UIElementExtensions 
{
    public static void Hide(this VisualElement element)
    {
        element.style.display = DisplayStyle.None;
    }

    public static void Show(this VisualElement element)
    {
        element.style.display = DisplayStyle.Flex;
    }

    public static void HideChild(this VisualElement element, string Name, string ClassName = null)
    {
        element.Q<VisualElement>().Hide();
    }

    public static void ShowChild(this VisualElement element, string Name, string ClassName = null)
    {
        element.Q<VisualElement>().Show();
    }
}
