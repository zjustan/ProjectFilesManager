using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Alias
{
    public readonly static Alias[] DefaultAliases = new Alias[]
    {
        new Alias("audio",".mp3",".ogg",".wav",".aiff",".aif",".mod",".it",".s3m",".xm"),
        new Alias("image",".png",".jpg",".bmp",".tif",".tiff",".jpeg",".psd"),
        new Alias("model",".fbx",".dea",".dxf",".obj"),
        new Alias("script",".cs",".asmdef"),
        AliasTooltip("any","All files","any")
    };

    public Alias(string name, params string[] refer)
    {
        Name = name;
        Refers = refer;
    }


    public static Alias AliasTooltip(string name, string Tooltip, params string[] refer)
    {
        var alias = new Alias(name, refer);
        alias.ToolTip = Tooltip;
        return alias;
    }

    public string Name;
    public string[] Refers;
    public string ToolTip;
}


