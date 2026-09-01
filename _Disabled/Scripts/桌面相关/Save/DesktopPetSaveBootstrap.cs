using DesktopPet.Decor;
using DesktopPet.Inventory;
using DesktopPet.Luby;
using DesktopPet.Shop;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DesktopPet.Save
{
    /// <summary>
    /// 启动时加载 desktoppet 存档并应用到运行时。从 DecorPlacementSystem 拆出，避免摆放系统兼管存档。
    /// </summary>
    [DefaultExecutionOrder(-20)]
    public sealed class DesktopPetSaveBootstrap : MonoBehaviour
    {
        public static bool Exists { get; private set; }

        [Title("存档引导")]
        [SerializeField] private ShopManager shop;
        [SerializeField] private ItemInventory inventory;
        [SerializeField] private DecorWorld world;
        [SerializeField] private LubyWorld lubyWorld;

        private void Awake()
        {
            Exists = true;
            ResolveRefs();
        }

        private void OnDestroy()
        {
            Exists = false;
        }

        private void Start()
        {
            LoadSaveIntoRuntime();
        }

        private void ResolveRefs()
        {
            if (shop == null)
                shop = DesktopPetServices.Shop;
            if (inventory == null)
                inventory = DesktopPetServices.Inventory;
            if (world == null)
                world = DesktopPetServices.DecorWorld;
            if (lubyWorld == null)
                lubyWorld = DesktopPetServices.LubyWorld;
        }

        private void LoadSaveIntoRuntime()
        {
            bool hasSave = DesktopPetSaveMgr.HasSaveFile();
            DesktopPetSaveMgr.Load();

            if (!hasSave)
            {
                ResolveRefs();
                if (shop != null && shop.Wallet != null)
                {
                    int start = shop.Catalog != null ? shop.Catalog.startingCurrency : 100;
                    shop.Wallet.SetCurrency(start);
                }

                DesktopPetSaveMgr.SaveRuntime(shop, inventory, world, lubyWorld);
                return;
            }

            DesktopPetSaveMgr.ApplyToRuntime(shop, inventory, world, lubyWorld);
            DesktopPetServices.HubUi?.RefreshChrome();
        }
    }
}
