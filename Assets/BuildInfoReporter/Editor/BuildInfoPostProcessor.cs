using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Sirenix.OdinInspector;

public class BuildInfoPostProcessor : IPostprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPostprocessBuild(BuildReport report)
    {
        var data = new BuildReportData
        {
            projectName = Application.productName,
            buildTarget = report.summary.platform.ToString(),
            buildTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            totalSize = (report.summary.totalSize / (1024f * 1024f)).ToString("F2") + " MB",
            outputFiles = new List<OutputFileInfo>(),
            assets = new List<AssetInfo>(),
            scenes = new List<string>()
        };

        // 获取启用的场景
        data.scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToList();

        // 输出文件
        foreach (var step in report.steps)
        {
            foreach (var msg in step.messages)
            {
                if (msg.type == LogType.Log && msg.content.Contains("output"))
                {
                    var outputPath = ExtractFilePathFromMessage(msg.content);
                    if (!string.IsNullOrEmpty(outputPath))
                    {
                        var fileInfo = new FileInfo(outputPath);
                        if (fileInfo.Exists)
                        {
                            data.outputFiles.Add(new OutputFileInfo
                            {
                                path = fileInfo.FullName,
                                size = (fileInfo.Length / (1024f * 1024f)).ToString("F2") + " MB"
                            });
                        }
                    }
                }
            }
        }

        // 资源列表
        foreach (var packedAsset in report.packedAssets)
        {
            foreach (var content in packedAsset.contents)
            {
                if (!string.IsNullOrEmpty(content.sourceAssetPath))
                {
                    var fileInfo = new FileInfo(content.sourceAssetPath);
                    long sizeInBytes = fileInfo.Exists ? fileInfo.Length : 0;

                    data.assets.Add(new AssetInfo
                    {
                        path = content.sourceAssetPath,
                        type = content.type.ToString(),
                        size = (sizeInBytes / 1024f).ToString("F1") + " KB"
                    });
                }
            }
        }

        // 如果 asset 列表为空，从 AssetDatabase 获取所有资源路径
        if (data.assets.Count == 0)
        {
            var allAssetGuids = AssetDatabase.FindAssets("", new[] { "Assets" });
            foreach (var guid in allAssetGuids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var fileInfo = new FileInfo(assetPath);
                long sizeInBytes = fileInfo.Exists ? fileInfo.Length : 0;

                data.assets.Add(new AssetInfo
                {
                    path = assetPath,
                    type = "Asset",
                    size = (sizeInBytes / 1024f).ToString("F1") + " KB"
                });
            }
        }

        // 缓存数据（写入临时文件）
        var json = JsonUtility.ToJson(data, true);
        var path = "BuildReports/LastBuildReport.json";
        Directory.CreateDirectory("BuildReports");
        File.WriteAllText(path, json);

        Debug.Log("Build report saved to: " + path);
    }

    private string ExtractFilePathFromMessage(string message)
    {
        var startIndex = message.IndexOf("output") + 7;
        var endIndex = message.LastIndexOf(" ");
        if (startIndex >= 0 && endIndex > startIndex)
        {
            return message.Substring(startIndex, endIndex - startIndex).Trim();
        }
        return string.Empty;
    }
}
