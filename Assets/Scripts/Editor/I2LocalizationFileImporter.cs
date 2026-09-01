using UnityEngine;
using UnityEditor;
using System.IO;
using I2.Loc;

public static class I2LocalizationImporter
{
    [MenuItem("工具/一键导入多语言 CSV")]
    public static void ImportCSVToI2Languages()
    {
        string relativeCsvPath = "Assets/Resources/Language/Language.csv";
        string fullPath = Path.Combine(Application.dataPath, "../" + relativeCsvPath);

        if (!File.Exists(fullPath))
        {
            Debug.LogError(" 找不到 CSV 文件：" + fullPath);
            return;
        }

        try
        {
            string csvContent = File.ReadAllText(fullPath);

            // 加载 I2Languages（LanguageSourceAsset）
            var sourceAsset = Resources.Load<LanguageSourceAsset>("I2Languages");
            if (sourceAsset == null)
            {
                Debug.LogError(" 无法找到 Resources/I2Languages.asset（类型应为 LanguageSourceAsset）");
                return;
            }

            if (sourceAsset.mSource == null)
            {
                Debug.LogError(" sourceAsset.mSource 为空，无法导入 CSV");
                return;
            }

            // 直接替换内容
            sourceAsset.mSource.Import_CSV("", csvContent, eSpreadsheetUpdateMode.Replace);
            EditorUtility.SetDirty(sourceAsset);
            AssetDatabase.SaveAssets();

            Debug.Log("成功导入 CSV 文件内容到 Resources/I2Languages！");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("导入出错: " + ex.Message);
        }
    }
}
