using DesktopPet;
using DesktopPet.Audio;
using DesktopPet.Adventure;
using DesktopPet.Background;
using DesktopPet.Decor;
using DesktopPet.Hub;
using DesktopPet.Inventory;
using DesktopPet.Luby;
using DesktopPet.Settings;
using DesktopPet.Shop;
using UnityEngine;

/// <summary>
/// 桌宠运行时服务注册表：各组件 Awake 时 Register，运行时只读静态槽。
/// </summary>
[DefaultExecutionOrder(-100)]
public sealed class DesktopPetServices : MonoBehaviour
{
    public const float DefaultManualGroundY = -11.48f;

    private static DesktopPetServices _host;

    /// <summary>
    /// 所有“会占用 Luby 并支持按 Luby 统一 End”的活动集合。
    /// 用接口减少 Services 对具体玩法系统类的硬依赖。
    /// </summary>
    private static readonly System.Collections.Generic.List<ILubyActivity> _lubyActivities =
        new System.Collections.Generic.List<ILubyActivity>(4);

    public static ShopManager Shop { get; private set; }
    public static ItemInventory Inventory { get; private set; }
    public static DecorWorld DecorWorld { get; private set; }
    public static DecorPlacementSystem Placement { get; private set; }
    public static LubyWorld LubyWorld { get; private set; }
    public static LubyAcquisitionService LubyAcquisition { get; private set; }
    public static LubyPlacementSystem LubyPlacement { get; private set; }
    public static DesktopHubUIController HubUi { get; private set; }
    public static ShopUIController ShopUi { get; private set; }
    public static InventoryUIController InventoryUi { get; private set; }
    public static LubyUIController LubyUi { get; private set; }
    public static SettingsUIController SettingsUi { get; private set; }
    public static ScenePageUIController SceneUi { get; private set; }
    public static DesktopPetGround Ground { get; private set; }
    public static DesktopPet.Environment.EnvironmentManager Environment { get; private set; }
    public static TransparentGameWindow TransparentWindow { get; private set; }
    public static DesktopCameraZoom CameraZoom { get; private set; }
    public static DesktopPetBgmPlayer Bgm { get; private set; }
    public static LubyDecorInteractionSystem LubyDecorInteraction { get; private set; }
    public static LubyTheaterDirector LubyTheater { get; private set; }
    public static LubyAdventureSystem LubyAdventure { get; private set; }
    public static LubyAppearanceCodex AppearanceCodex { get; private set; }
    public static CodexUIController CodexUi { get; private set; }
    public static AdventureBoardUiController AdventureUi { get; private set; }

    private void Awake()
    {
        _host = this;
    }

    private void OnDestroy()
    {
        if (_host != this)
            return;
        _host = null;
        ClearAll();
    }

    private static void ClearAll()
    {
        Shop = null;
        Inventory = null;
        DecorWorld = null;
        Placement = null;
        LubyWorld = null;
        LubyAcquisition = null;
        LubyPlacement = null;
        HubUi = null;
        ShopUi = null;
        InventoryUi = null;
        LubyUi = null;
        SettingsUi = null;
        SceneUi = null;
        Ground = null;
        Environment = null;
        TransparentWindow = null;
        CameraZoom = null;
        Bgm = null;
        LubyDecorInteraction = null;
        LubyTheater = null;
        LubyAdventure = null;
        AppearanceCodex = null;
        CodexUi = null;
        AdventureUi = null;

        _lubyActivities.Clear();
    }

    public static void RegisterShop(ShopManager value)
    {
        if (value != null) Shop = value;
    }

    public static void UnregisterShop(ShopManager value)
    {
        if (Shop == value) Shop = null;
    }

    public static void RegisterInventory(ItemInventory value)
    {
        if (value != null) Inventory = value;
    }

    public static void UnregisterInventory(ItemInventory value)
    {
        if (Inventory == value) Inventory = null;
    }

    public static void RegisterDecorWorld(DecorWorld value)
    {
        if (value != null) DecorWorld = value;
    }

    public static void UnregisterDecorWorld(DecorWorld value)
    {
        if (DecorWorld == value) DecorWorld = null;
    }

    public static void RegisterPlacement(DecorPlacementSystem value)
    {
        if (value != null) Placement = value;
    }

    public static void UnregisterPlacement(DecorPlacementSystem value)
    {
        if (Placement == value) Placement = null;
    }

    public static void RegisterLubyWorld(LubyWorld value)
    {
        if (value != null) LubyWorld = value;
    }

    public static void UnregisterLubyWorld(LubyWorld value)
    {
        if (LubyWorld == value) LubyWorld = null;
    }

    public static void RegisterLubyAcquisition(LubyAcquisitionService value)
    {
        if (value != null) LubyAcquisition = value;
    }

    public static void UnregisterLubyAcquisition(LubyAcquisitionService value)
    {
        if (LubyAcquisition == value) LubyAcquisition = null;
    }

    public static void RegisterLubyPlacement(LubyPlacementSystem value)
    {
        if (value != null) LubyPlacement = value;
    }

    public static void UnregisterLubyPlacement(LubyPlacementSystem value)
    {
        if (LubyPlacement == value) LubyPlacement = null;
    }

    public static void RegisterHubUi(DesktopHubUIController value)
    {
        if (value != null) HubUi = value;
    }

    public static void UnregisterHubUi(DesktopHubUIController value)
    {
        if (HubUi == value) HubUi = null;
    }

    public static void RegisterShopUi(ShopUIController value)
    {
        if (value != null) ShopUi = value;
    }

    public static void UnregisterShopUi(ShopUIController value)
    {
        if (ShopUi == value) ShopUi = null;
    }

    public static void RegisterInventoryUi(InventoryUIController value)
    {
        if (value != null) InventoryUi = value;
    }

    public static void UnregisterInventoryUi(InventoryUIController value)
    {
        if (InventoryUi == value) InventoryUi = null;
    }

    public static void RegisterLubyUi(LubyUIController value)
    {
        if (value != null) LubyUi = value;
    }

    public static void UnregisterLubyUi(LubyUIController value)
    {
        if (LubyUi == value) LubyUi = null;
    }

    public static void RegisterSettingsUi(SettingsUIController value)
    {
        if (value != null) SettingsUi = value;
    }

    public static void UnregisterSettingsUi(SettingsUIController value)
    {
        if (SettingsUi == value) SettingsUi = null;
    }

    public static void RegisterSceneUi(ScenePageUIController value)
    {
        if (value != null) SceneUi = value;
    }

    public static void UnregisterSceneUi(ScenePageUIController value)
    {
        if (SceneUi == value) SceneUi = null;
    }

    public static void RegisterGround(DesktopPetGround value)
    {
        if (value != null) Ground = value;
    }

    public static void UnregisterGround(DesktopPetGround value)
    {
        if (Ground == value) Ground = null;
    }

    public static void RegisterEnvironment(DesktopPet.Environment.EnvironmentManager value)
    {
        if (value != null) Environment = value;
    }

    public static void UnregisterEnvironment(DesktopPet.Environment.EnvironmentManager value)
    {
        if (Environment == value) Environment = null;
    }

    public static void RegisterTransparentWindow(TransparentGameWindow value)
    {
        if (value != null) TransparentWindow = value;
    }

    public static void UnregisterTransparentWindow(TransparentGameWindow value)
    {
        if (TransparentWindow == value) TransparentWindow = null;
    }

    public static void RegisterCameraZoom(DesktopCameraZoom value)
    {
        if (value != null) CameraZoom = value;
    }

    public static void UnregisterCameraZoom(DesktopCameraZoom value)
    {
        if (CameraZoom == value) CameraZoom = null;
    }

    public static void RegisterBgm(DesktopPetBgmPlayer value)
    {
        if (value != null) Bgm = value;
    }

    public static void UnregisterBgm(DesktopPetBgmPlayer value)
    {
        if (Bgm == value) Bgm = null;
    }

    public static void RegisterLubyDecorInteraction(LubyDecorInteractionSystem value)
    {
        if (value != null) LubyDecorInteraction = value;
    }

    public static void UnregisterLubyDecorInteraction(LubyDecorInteractionSystem value)
    {
        if (LubyDecorInteraction == value) LubyDecorInteraction = null;
    }

    public static void RegisterLubyTheater(LubyTheaterDirector value)
    {
        if (value != null) LubyTheater = value;
    }

    public static void UnregisterLubyTheater(LubyTheaterDirector value)
    {
        if (LubyTheater == value) LubyTheater = null;
    }

    public static void RegisterLubyAdventure(LubyAdventureSystem value)
    {
        if (value != null) LubyAdventure = value;
    }

    public static void UnregisterLubyAdventure(LubyAdventureSystem value)
    {
        if (LubyAdventure == value) LubyAdventure = null;
    }

    public static void RegisterLubyActivity(ILubyActivity value)
    {
        if (value == null)
            return;
        if (!_lubyActivities.Contains(value))
            _lubyActivities.Add(value);
    }

    public static void UnregisterLubyActivity(ILubyActivity value)
    {
        if (value == null)
            return;
        _lubyActivities.Remove(value);
    }

    /// <summary>装饰交互 / 捡币 / 探险占用中（不含小剧场）。</summary>
    public static bool IsLubyExternallyBusy(LubyInstanceComponent luby) =>
        IsLubyBusyWithOtherActivities(luby, LubyTheater);

    /// <summary>桌上活动开跑门禁：任一活动占用（含小剧场）。</summary>
    public static bool IsLubyBlockedForWorldActivity(LubyInstanceComponent luby) =>
        IsLubyBusyWithOtherActivities(luby, null);

    /// <summary>
    /// 除 <paramref name="self"/> 外是否有活动占用该 Luby（含小剧场）。
    /// <paramref name="self"/> 为 null 时统计全部活动。
    /// Tick 里用来发现被其它系统抢走，勿把 self 算进去。
    /// </summary>
    public static bool IsLubyBusyWithOtherActivities(LubyInstanceComponent luby, ILubyActivity self)
    {
        if (luby == null)
            return false;
        for (int i = 0; i < _lubyActivities.Count; i++)
        {
            ILubyActivity activity = _lubyActivities[i];
            if (activity == null || activity == self)
                continue;
            if (activity.IsLubyBusy(luby))
                return true;
        }

        return false;
    }

    public static void RegisterAppearanceCodex(LubyAppearanceCodex value)
    {
        if (value != null) AppearanceCodex = value;
    }

    public static void UnregisterAppearanceCodex(LubyAppearanceCodex value)
    {
        if (AppearanceCodex == value) AppearanceCodex = null;
    }

    public static void RegisterCodexUi(CodexUIController value)
    {
        if (value != null) CodexUi = value;
    }

    public static void UnregisterCodexUi(CodexUIController value)
    {
        if (CodexUi == value) CodexUi = null;
    }

    public static void RegisterAdventureUi(AdventureBoardUiController value)
    {
        if (value != null) AdventureUi = value;
    }

    public static void UnregisterAdventureUi(AdventureBoardUiController value)
    {
        if (AdventureUi == value) AdventureUi = null;
    }

    /// <summary>关闭主面板（放置装饰等需要收起 UI）。</summary>
    public static void CloseHub() => HubUi?.Close();

    public static bool IsAnyPlacementHolding() =>
        (Placement?.IsHolding == true) || (LubyPlacement?.IsHolding == true);

    public static bool IsHubOpen() => HubUi != null && HubUi.IsOpen;

    /// <summary>装饰交互 / 捡币 / 小剧场等 Luby 占用一并结束。</summary>
    public static void EndAllLubyActivities(LubyInstanceComponent luby)
    {
        if (luby == null)
            return;

        for (int i = _lubyActivities.Count - 1; i >= 0; i--)
        {
            ILubyActivity activity = _lubyActivities[i];
            activity?.EndAllForLuby(luby);
        }
    }

    public static float ResolveGroundY(float fallbackManualY = DefaultManualGroundY)
    {
        if (Ground != null)
            return Ground.ResolveGroundY();

        return fallbackManualY;
    }
}
