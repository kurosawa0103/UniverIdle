using Sirenix.OdinInspector;

namespace DesktopPet.Decor
{
    /// <summary>
    /// 装饰家具用途（商品 SO 标一种；睡找床、坐找椅、趴找地毯等）。
    /// </summary>
    public enum DecorFurnitureKind
    {
        [LabelText("无")]
        None = 0,

        [LabelText("床")]
        Bed = 1,

        [LabelText("椅子")]
        Chair = 2,

        [LabelText("地板")]
        Floor = 3,
    }
}
