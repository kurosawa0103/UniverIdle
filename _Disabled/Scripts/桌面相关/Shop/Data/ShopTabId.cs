using Sirenix.OdinInspector;

namespace DesktopPet.Shop
{
    /// <summary>商店页签。Food / Misc 枚举预留；当前商店与仓库 UI 仅用 Decor。</summary>
    public enum ShopTabId
    {
        [LabelText("装饰")]
        Decor = 0,

        [LabelText("食物（预留）")]
        Food = 1,

        [LabelText("杂项（预留）")]
        Misc = 2
    }
}
