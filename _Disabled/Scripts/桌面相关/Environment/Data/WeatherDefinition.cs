using UnityEngine;

namespace DesktopPet.Environment
{
    [CreateAssetMenu(menuName = "桌宠/环境/天气定义", fileName = "WeatherDefinition")]
    public sealed class WeatherDefinition : ScriptableObject
    {
        public string weatherId = "sunny";
        public string displayName = "晴天";

        [Tooltip("该天气的粒子特效预制体；晴天等无特效时留空")]
        public GameObject effectPrefab;

        [Tooltip("发射器相对地面高度；≤0 则用 WeatherFxPresenter 默认")]
        public float spawnHeightAboveGround;
    }
}
