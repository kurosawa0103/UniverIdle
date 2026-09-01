#if UNITY_EDITOR
using UnityEditor;

namespace DesktopPet.Editor
{
    /// <summary>桌宠 Editor 共用：确保文件夹路径存在。</summary>
    public static class DesktopPetEditorUi
    {
        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string[] parts = path.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }
    }
}
#endif
