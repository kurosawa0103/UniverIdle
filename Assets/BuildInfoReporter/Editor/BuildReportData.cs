using System;
using System.Collections.Generic;

[Serializable]
public class BuildReportData
{
    public string projectName;
    public string buildTarget;
    public string buildTime;
    public string totalSize;
    public List<string> scenes = new();
    public List<OutputFileInfo> outputFiles = new();
    public List<AssetInfo> assets = new();
}

[Serializable]
public class OutputFileInfo
{
    public string path;
    public string size;
}

[Serializable]
public class AssetInfo
{
    public string path;
    public string size;
    public string type;
}
