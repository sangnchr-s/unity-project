using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Окно «TMP Importer» вызывает ImportPackage по пути Packages/com.unity.textmeshpro —
/// при типичном UPM этой папки нет (пакет лежит в Library/PackageCache), импорт может не запускаться.
/// Этот пункт открывает диалог импорта с корректным путём к .unitypackage.
/// </summary>
public static class TmpEssentialsPackageImporter
{
    const string EssentialsFileName = "TMP Essential Resources.unitypackage";

    [MenuItem("Tools/TextMesh Pro/Import TMP Essentials (from Package Cache)")]
    public static void ImportEssentialsFromPackageCache()
    {
        string packageRoot = FindTextMeshProPackageRoot();
        if (string.IsNullOrEmpty(packageRoot))
        {
            EditorUtility.DisplayDialog(
                "TMP Essentials",
                "Не найден пакет com.unity.textmeshpro в Library/PackageCache.\n\n" +
                "Откройте Window → Package Manager и убедитесь, что TextMeshPro установлен.",
                "OK");
            return;
        }

        string unityPackage = Path.Combine(packageRoot, "Package Resources", EssentialsFileName);
        if (!File.Exists(unityPackage))
        {
            EditorUtility.DisplayDialog(
                "TMP Essentials",
                "Файл не найден:\n" + unityPackage,
                "OK");
            return;
        }

        AssetDatabase.ImportPackage(unityPackage, true);
    }

    static string FindTextMeshProPackageRoot()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string cache = Path.Combine(projectRoot, "Library", "PackageCache");
        if (!Directory.Exists(cache))
            return null;

        try
        {
            foreach (string dir in Directory.GetDirectories(cache))
            {
                string name = Path.GetFileName(dir);
                if (name.StartsWith("com.unity.textmeshpro@", System.StringComparison.Ordinal))
                    return dir;
            }
        }
        catch (IOException)
        {
        }

        return null;
    }
}
