using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Diagnostics;

public class SpriteExporter : EditorWindow
{
    [MenuItem("Assets/Export Slices as Files")]
    public static void ExportSlices()
    {
        Texture2D sourceTex = Selection.activeObject as Texture2D;
        if (sourceTex == null) { UnityEngine.Debug.LogError("Select a Texture file first!"); return; }

        string path = AssetDatabase.GetAssetPath(sourceTex);
        UnityEngine.Object[] sprites = AssetDatabase.LoadAllAssetsAtPath(path);

        foreach (UnityEngine.Object obj in sprites)
        {
            if (obj is Sprite sprite)
            {
                Texture2D newTex = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height);
                Color[] pixels = sourceTex.GetPixels((int)sprite.rect.x, (int)sprite.rect.y, (int)sprite.rect.width, (int)sprite.rect.height);
                newTex.SetPixels(pixels);
                newTex.Apply();

                byte[] bytes = newTex.EncodeToPNG();
                File.WriteAllBytes(Path.GetDirectoryName(path) + "/" + sprite.name + ".png", bytes);
            }
        }
        AssetDatabase.Refresh();
        UnityEngine.Debug.Log("Slices successfully exported as individual files!");

    }
}
