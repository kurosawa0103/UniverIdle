// This code is part of the Fungus library (https://github.com/snozbot/fungus)
// It is released for free under the MIT open source license (https://github.com/snozbot/fungus/blob/master/LICENSE)

﻿using UnityEngine;
using UnityEditor;

namespace Fungus.EditorUtils
{
    public class FlowchartMenuItems
    {
        [MenuItem("Tools/Fungus/Create/Fungus Logo", false, 1000)]
        static void CreateFungusLogo()
        {
            SpawnPrefab("FungusLogo");
        }

        [MenuItem("Tools/Fungus/Utilities/Export Fungus Package")]
        static void ExportFungusPackageFull()
        {
            ExportFungusPackage( new string[] {"Assets/Fungus", "Assets/FungusExamples" });
        }

        [MenuItem("Tools/Fungus/Utilities/Export Fungus Package - Lite")]
        static void ExportFungusPackageLite()
        {
            ExportFungusPackage(new string[] { "Assets/Fungus" });
        }

        static void ExportFungusPackage(string[] folders)
        {
            string path = EditorUtility.SaveFilePanel("Export Fungus Package", "", "Fungus", "unitypackage");
            if (path.Length == 0)
            {
                return;
            }

            AssetDatabase.ExportPackage(folders, path, ExportPackageOptions.Recurse);
        }

        public static GameObject SpawnPrefab(string prefabName)
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/" + prefabName);
            if (prefab == null)
            {
                return null;
            }

            GameObject go = GameObject.Instantiate(prefab) as GameObject;
            go.name = prefab.name;

            SceneView view = SceneView.lastActiveSceneView;
            if (view != null)
            {
                Camera sceneCam = view.camera;
                Vector3 pos = sceneCam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 10f));
                pos.z = 0f;
                go.transform.position = pos;
            }

            Selection.activeGameObject = go;
            
            Undo.RegisterCreatedObjectUndo(go, "Create Object");

            return go;
        }
    }
}