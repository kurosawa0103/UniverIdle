using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;

namespace Fungus
{
    [CommandInfo("Flow",
                 "Load Scene",
                 "Loads a new Unity scene and displays an optional loading image.")]
    [AddComponentMenu("")]
    [ExecuteInEditMode]
    public class LoadScene : Command
    {
        [Tooltip("Name of the scene to load. The scene must also be added to the build settings.")]
        [SerializeField] protected StringData _sceneName = new StringData("");

        [Tooltip("Image to display while loading the scene")]
        [SerializeField] protected Texture2D loadingImage;

        [Header("Advanced Options")]
        [Tooltip("��ѡ��� PlayerPrefs ��ȡ�������ƣ����� Scene Name")]
        [SerializeField] protected bool useLastSceneFromPrefs = false;
        [SerializeField] protected bool needSaveSceneName = true;

        #region Public members

        public override void OnEnter()
        {
            
            string targetScene;
            string sceneName = SceneManager.GetActiveScene().name;
            if(needSaveSceneName)
            {
                 PlayerPrefs.SetString("LastScene", sceneName);
                 PlayerPrefs.Save();
                 Debug.Log($"已保存当前地图场景名：{sceneName}");
                 // 仅当当前场景为地图（Map- 前缀）时，记录最近地图
                if (_sceneName.Value.StartsWith("Map-"))
                {
                  PlayerPrefs.SetString("LoadMapScene", _sceneName.Value);
                }
            }
           
            if (useLastSceneFromPrefs)
            {
                // �� PlayerPrefs ��ȡ�ϴα���ĳ�������
                    targetScene = PlayerPrefs.GetString("LoadMapScene", _sceneName.Value);
            }
            else
            {
                targetScene = _sceneName.Value;
            }

            if (string.IsNullOrEmpty(targetScene))
            {
                Debug.LogWarning("LoadScene: Scene name is empty!");
                Continue();
                return;
            }

            SceneLoader.LoadScene(targetScene, loadingImage);
        }

        public override string GetSummary()
        {
            if (useLastSceneFromPrefs)
            {
            return $"LoadScene (From PlayerPrefs: {PlayerPrefs.GetString("LoadMapScene", _sceneName.Value)})";
            }

            if (_sceneName.Value.Length == 0)
            {
                return "Error: No scene name selected";
            }

            return _sceneName.Value;
        }

        public override Color GetButtonColor()
        {
            return new Color32(235, 191, 217, 255);
        }

        public override bool HasReference(Variable variable)
        {
            return _sceneName.stringRef == variable || base.HasReference(variable);
        }

        #endregion

        #region Backwards compatibility

        [HideInInspector] [FormerlySerializedAs("sceneName")] public string sceneNameOLD = "";

        protected virtual void OnEnable()
        {
            if (sceneNameOLD != "")
            {
                _sceneName.Value = sceneNameOLD;
                sceneNameOLD = "";
            }
        }

        #endregion
    }
}
