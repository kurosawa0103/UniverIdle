using DesktopPet.Audio;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.Decor
{
    /// <summary>收音机：短按开关曲目，覆盖场景 BGM；再关则淡回默认曲。</summary>
    [DisallowMultipleComponent]
    public sealed class DecorRadio : MonoBehaviour, IDecorShortClickHandler
    {
        [Title("收音机")]
        [LabelText("电台曲目")]
        [SerializeField]
        private AudioClip track;

        [ShowInInspector, ReadOnly, LabelText("开启中")]
        public bool IsOn { get; private set; }

        public void OnShortClick() => Toggle();

        private void Toggle() => SetOn(!IsOn);

        public void SetOn(bool on)
        {
            if (on == IsOn)
            {
                if (on)
                    EnsurePlaying();
                return;
            }

            IsOn = on;
            DesktopPetBgmPlayer bgm = DesktopPetServices.Bgm;
            if (bgm == null)
                return;

            if (on)
            {
                if (track == null)
                {
                    Debug.LogWarning("[DecorRadio] 未配置电台曲目。", this);
                    IsOn = false;
                    return;
                }

                bgm.PlayOverride(track, this);
            }
            else
            {
                bgm.ClearOverride(this);
            }
        }

        private void EnsurePlaying()
        {
            DesktopPetBgmPlayer bgm = DesktopPetServices.Bgm;
            if (bgm == null || track == null)
                return;
            bgm.PlayOverride(track, this);
        }

        private void OnDisable()
        {
            if (!IsOn)
                return;
            IsOn = false;
            DesktopPetServices.Bgm?.ClearOverride(this);
        }
    }
}
