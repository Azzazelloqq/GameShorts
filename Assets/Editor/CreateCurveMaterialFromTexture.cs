// Assets/Editor/CreateCurveMaterialFromTexture.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public static class CreateCurveMaterialFromTexture
{
    private const string kShaderName   = "Shader Graphs/CurveBaseLitShader";
    private const string kTextureProp  = "_BaseTexture";

    // Пункт контекстного меню по Right Click на текстуре (и в Assets-меню)
    [MenuItem("Assets/Create/Curve Material from Texture", false, 2021)]
    private static void CreateMatFromSelectedTexture()
    {
        var shader = Shader.Find(kShaderName);
        if (shader == null)
        {
            EditorUtility.DisplayDialog("Shader not found",
                $"Не найден шейдер \"{kShaderName}\".\nПроверь имя/наличие шейдера.", "Ок");
            return;
        }

        // Можно выбирать несколько текстур сразу
        var objs = Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets);
        if (objs == null || objs.Length == 0)
        {
            EditorUtility.DisplayDialog("No texture selected",
                "Выбери Texture2D в Project и повтори.", "Ок");
            return;
        }

        int created = 0;
        foreach (var o in objs)
        {
            var tex = o as Texture2D;
            if (tex == null) continue;

            string texPath = AssetDatabase.GetAssetPath(tex);
            if (string.IsNullOrEmpty(texPath)) continue;

            string folder   = Path.GetDirectoryName(texPath)?.Replace("\\","/");
            string baseName = Path.GetFileNameWithoutExtension(texPath);
            string matName  = $"{baseName}_CurveMat.mat";
            string matPath  = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(folder, matName));

            // Создать материал
            var mat = new Material(shader);

            // Проставить текстуру
            if (mat.HasProperty(kTextureProp))
                mat.SetTexture(kTextureProp, tex);
            else
                mat.mainTexture = tex;

            // Сохранить рядом с текстурой
            AssetDatabase.CreateAsset(mat, matPath);
            created++;

            // Выделить и подсветить
            EditorGUIUtility.PingObject(mat);
            Selection.activeObject = mat;
        }

        if (created > 0)
        {
            AssetDatabase.SaveAssets();
        }
        else
        {
            EditorUtility.DisplayDialog("Nothing created",
                "Не удалось создать материалы для выделенных объектов.", "Ок");
        }
    }

    // Валидация пункта меню — показывать только когда выбрана(ы) Texture2D
    [MenuItem("Assets/Create/Curve Material from Texture", true)]
    private static bool ValidateCreateMatFromSelectedTexture()
    {
        var objs = Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets);
        return objs != null && objs.Length > 0;
    }
}
#endif
