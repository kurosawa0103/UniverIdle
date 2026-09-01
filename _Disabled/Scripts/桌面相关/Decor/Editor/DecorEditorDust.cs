using UnityEngine;
using UnityEditor;

namespace DesktopPet.Decor.Editor
{
    /// <summary>装饰 Editor 共用：把烟尘预制体挂到 placementPrefab 上。</summary>
    internal static class DecorEditorDust
    {
        public const string DustPrefabPath = "Assets/Resources/Prefabs/effect/DecorPlaceDust.prefab";

        public static void Attach(GameObject decorRoot)
        {
            GameObject dustPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DustPrefabPath);
            if (dustPrefab == null || decorRoot == null)
                return;

            GameObject dust = (GameObject)PrefabUtility.InstantiatePrefab(dustPrefab, decorRoot.transform);
            dust.name = "DecorPlaceDust";
            dust.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            dust.transform.localRotation = Quaternion.identity;
            dust.transform.localScale = Vector3.one;

            PlacedDecor placed = decorRoot.GetComponent<PlacedDecor>() ?? decorRoot.AddComponent<PlacedDecor>();
            ParticleSystem ps = dust.GetComponent<ParticleSystem>();
            SerializedObject so = new SerializedObject(placed);
            SerializedProperty prop = so.FindProperty("placeDust");
            if (prop != null)
            {
                prop.objectReferenceValue = ps;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
