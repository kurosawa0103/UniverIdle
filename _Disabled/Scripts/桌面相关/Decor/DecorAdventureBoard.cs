using Sirenix.OdinInspector;
using UnityEngine;
using DesktopPet.Adventure;

namespace DesktopPet.Decor
{
    /// <summary>
    /// 探险看板：短按打开独立探险界面。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DecorAdventureBoard : MonoBehaviour, IDecorShortClickHandler
    {
        public void OnShortClick()
        {
            AdventureBoardUiController ui = DesktopPetServices.AdventureUi;
            if (ui == null)
            {
                Debug.LogError(
                    "[DecorAdventureBoard] 未找到 AdventureBoardUiController。请在 MainCanvas 预挂探险面板后再运行。",
                    this);
                return;
            }

            ui.OpenFromBoard();
        }
    }
}
