using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DesktopPet.Background;
using DesktopPet.Decor;
using DesktopPet.Inventory;
using DesktopPet.Luby;
using DesktopPet.Shop;
using UnityEngine;

namespace DesktopPet.Save
{
    [Serializable]
    public class DesktopPetInventoryEntry
    {
        public string itemId;
        public int count;
    }

    [Serializable]
    public class DesktopPetPlacedEntry
    {
        public string instanceId;
        public string itemId;
        public float x;
        public float y;
        public string parentInstanceId;
    }

    [Serializable]
    public class DesktopPetLubyEntry
    {
        public string instanceId;
        public string templateId;
        public string personalityId;
        public string traitId;
        public string traitId2;
        public string petName;
        public string appearanceKey;
        public float x;
        public float y;
        public float scale = 1f;
        /// <summary>UTC 秒；&gt; 当前时间 = 探险离桌中。</summary>
        public double adventureEndsAtUtc;
        public string adventureEventId;
        public string adventureRegionId;
        public string adventureBackgroundId;
        public string lastAdventureEventId;
        public string lastAdventureRegionId;
        public string lastAdventureTitle;
        public int lastAdventureGold;
        public double lastAdventureEndedAtUtc;
        public string adventureDayKey;
        public int adventureTripsToday;
        public float adventureExitX;
        /// <summary>近况日记条目。</summary>
        public List<LubyJournalEntry> journalEntries = new List<LubyJournalEntry>();
        /// <summary>喜好计数。</summary>
        public List<LubyJournalLike> journalLikes = new List<LubyJournalLike>();
    }

    /// <summary>单个背景场景的装饰摆放列表（用于 JsonUtility 序列化字典）。</summary>
    [Serializable]
    public class DesktopPetScenePlaced
    {
        public string backgroundId;
        public List<DesktopPetPlacedEntry> placed = new List<DesktopPetPlacedEntry>();
        /// <summary>该背景场景的装饰桌上容量；0 = 读背景 Definition 初始值（不提前写进档）。</summary>
        public int decorDeskCapacity;
        /// <summary>该背景场景的 Luby 桌上容量；0 = 读背景 Definition 初始值（不提前写进档）。</summary>
        public int lubyDeskCapacity;
    }

    [Serializable]
    public class DesktopPetSaveData
    {
        public int version = 2;
        public int currency;
        public List<DesktopPetInventoryEntry> inventory = new List<DesktopPetInventoryEntry>();

        /// <summary>多背景场景装饰摆放列表；key = backgroundId。</summary>
        public List<DesktopPetScenePlaced> scenes = new List<DesktopPetScenePlaced>();

        public List<DesktopPetLubyEntry> lubies = new List<DesktopPetLubyEntry>();
        /// <summary>未出场的 Luby（整只实例）。</summary>
        public List<DesktopPetLubyEntry> lubyWarehouse = new List<DesktopPetLubyEntry>();
        /// <summary>已解锁外表图鉴（appearanceKey = 外形 Prefab 名）。</summary>
        public List<string> unlockedAppearances = new List<string>();
        /// <summary>已解锁背景 ID 列表（商店购买后写入）。</summary>
        public List<string> unlockedBackgrounds = new List<string>();
        /// <summary>当前激活背景 ID；空则启动时回退到 catalog 中 defaultUnlocked 背景。</summary>
        public string currentBackgroundId;

        /// <summary>查找或新建指定背景的场景记录。</summary>
        public DesktopPetScenePlaced GetOrCreateScene(string backgroundId)
        {
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i] != null && scenes[i].backgroundId == backgroundId)
                    return scenes[i];
            }

            var s = new DesktopPetScenePlaced { backgroundId = backgroundId };
            scenes.Add(s);
            return s;
        }

        /// <summary>获取指定背景的装饰列表（不存在返回 null）。</summary>
        public List<DesktopPetPlacedEntry> GetScenePlaced(string backgroundId)
        {
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i] != null && scenes[i].backgroundId == backgroundId)
                    return scenes[i].placed;
            }

            return null;
        }
    }

    /// <summary>
    /// 桌宠专用存档：金币 / 仓库 / 已摆装饰 / Luby → persistentDataPath/desktoppet.json。
    /// </summary>
    public static class DesktopPetSaveMgr
    {
        private const int CurrentVersion = 2;
        private const string SaveFileName = "desktoppet.json";
        private const string LegacySaveFileName = "desktoppet.dat";

        public static DesktopPetSaveData Current { get; private set; } = new DesktopPetSaveData();

        private static string GetSavePath()
        {
            return Path.Combine(Application.persistentDataPath, SaveFileName);
        }

        private static string GetLegacySavePath()
        {
            return Path.Combine(Application.persistentDataPath, LegacySaveFileName);
        }

        public static bool HasSaveFile()
        {
            return File.Exists(GetSavePath()) || File.Exists(GetLegacySavePath());
        }

        public static void Save()
        {
            NormalizeCurrent();

            try
            {
                string json = JsonUtility.ToJson(Current, true);
                File.WriteAllText(GetSavePath(), json, Encoding.UTF8);
            }
            catch (Exception e)
            {
                Debug.LogError($"[DesktopPetSave] 保存失败: {e.Message}");
            }
        }

        public static DesktopPetSaveData Load()
        {
            string path = GetSavePath();
            if (File.Exists(path))
            {
                if (TryReadSaveFile(path, out DesktopPetSaveData data))
                    return data;
            }

            string legacyPath = GetLegacySavePath();
            if (File.Exists(legacyPath))
            {
                if (TryReadSaveFile(legacyPath, out DesktopPetSaveData data))
                {
                    Current = data;
                    Save();
                    try
                    {
                        File.Delete(legacyPath);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[DesktopPetSave] 已迁移到 json，但删旧档失败: {e.Message}");
                    }

                    return Current;
                }
            }

            Current = new DesktopPetSaveData();
            NormalizeCurrent();
            return Current;
        }

        private static bool TryReadSaveFile(string path, out DesktopPetSaveData data)
        {
            data = null;
            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                data = JsonUtility.FromJson<DesktopPetSaveData>(json) ?? new DesktopPetSaveData();
                Current = data;
                NormalizeCurrent();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DesktopPetSave] 读取失败 ({path}): {e.Message}");
                return false;
            }
        }

        public static void DeleteSaveFile()
        {
            string path = GetSavePath();
            if (File.Exists(path))
                File.Delete(path);

            string legacyPath = GetLegacySavePath();
            if (File.Exists(legacyPath))
                File.Delete(legacyPath);

            Current = new DesktopPetSaveData();
            NormalizeCurrent();
        }

        /// <summary>重置背景解锁、各场景容量与当前背景到默认（新档态，不写盘）。</summary>
        public static void ResetBackgroundProgress()
        {
            NormalizeCurrent();
            Current.unlockedBackgrounds.Clear();
            Current.scenes.Clear();
            Current.currentBackgroundId = null;

            BackgroundCatalog catalog = BackgroundCatalog.LoadDefault();
            string defaultId = BackgroundCatalog.ResolveActiveId(null, catalog);
            Current.currentBackgroundId = defaultId;

            BackgroundSystem bgSys = BackgroundSystem.Instance;
            if (bgSys != null)
                bgSys.ApplyBackground(defaultId);

            BackgroundDefinition def = catalog?.FindById(defaultId);
            BackgroundSceneCapacity.ApplyToRuntime(defaultId, def);

            DesktopPetServices.SceneUi?.OnPageShown();
        }

        private static void NormalizeCurrent()
        {
            if (Current == null)
                Current = new DesktopPetSaveData();
            if (Current.inventory == null)
                Current.inventory = new List<DesktopPetInventoryEntry>();
            if (Current.scenes == null)
                Current.scenes = new List<DesktopPetScenePlaced>();
            if (Current.lubies == null)
                Current.lubies = new List<DesktopPetLubyEntry>();
            if (Current.lubyWarehouse == null)
                Current.lubyWarehouse = new List<DesktopPetLubyEntry>();
            if (Current.unlockedAppearances == null)
                Current.unlockedAppearances = new List<string>();
            if (Current.unlockedBackgrounds == null)
                Current.unlockedBackgrounds = new List<string>();

            if (Current.version <= 0)
                Current.version = CurrentVersion;
        }

        /// <summary>将运行时装饰写入指定背景 scene 槽（仅内存，不写盘）。</summary>
        public static void CaptureDecorToScene(string backgroundId, DecorWorld world)
        {
            if (world == null)
                return;

            NormalizeCurrent();
            DesktopPetScenePlaced scene = Current.GetOrCreateScene(backgroundId);
            scene.placed = new List<DesktopPetPlacedEntry>();
            IReadOnlyList<PlacedDecor> list = world.Placed;
            for (int i = 0; i < list.Count; i++)
            {
                PlacedDecor d = list[i];
                if (d == null)
                    continue;

                Vector3 p = d.transform.position;
                scene.placed.Add(new DesktopPetPlacedEntry
                {
                    instanceId = d.InstanceId,
                    itemId = d.ItemId,
                    x = p.x,
                    y = p.y,
                    parentInstanceId = d.ParentInstanceId
                });
            }
        }

        private static void CaptureFromRuntime(
            ShopManager shop,
            ItemInventory inventory,
            DecorWorld world,
            LubyWorld lubyWorld = null)
        {
            NormalizeCurrent();

            Current.currency = shop != null && shop.Wallet != null ? shop.Wallet.Currency : 0;

            Current.inventory = new List<DesktopPetInventoryEntry>();
            if (inventory != null)
            {
                IReadOnlyList<ItemInventory.Entry> entries = inventory.Entries;
                for (int i = 0; i < entries.Count; i++)
                {
                    ItemInventory.Entry e = entries[i];
                    Current.inventory.Add(new DesktopPetInventoryEntry
                    {
                        itemId = e.itemId,
                        count = e.count
                    });
                }
            }

            BackgroundCatalog catalog = BackgroundSystem.Instance?.Catalog ?? BackgroundCatalog.LoadDefault();
            // Start 前 Instance 可能已在，但 CurrentBackgroundId 仍是默认 transparent；未 Apply 时走 ResolveActiveId。
            string bgId = BackgroundSystem.Instance != null && BackgroundSystem.Instance.HasAppliedBackground
                ? BackgroundSystem.Instance.CurrentBackgroundId
                : BackgroundCatalog.ResolveActiveId(Current.currentBackgroundId, catalog);
            Current.currentBackgroundId = bgId;

            if (world != null)
            {
                CaptureDecorToScene(bgId, world);
                BackgroundSceneCapacity.CaptureFromRuntime(bgId, world, lubyWorld);
            }
            else if (lubyWorld != null)
            {
                BackgroundSceneCapacity.CaptureFromRuntime(bgId, null, lubyWorld);
            }

            if (lubyWorld != null)
            {
                Current.lubies = lubyWorld.CaptureForSave();
                Current.lubyWarehouse = lubyWorld.CaptureWarehouseForSave();
            }
            else
            {
                Current.lubies = new List<DesktopPetLubyEntry>();
                Current.lubyWarehouse = new List<DesktopPetLubyEntry>();
            }

            LubyAppearanceCodex codex = DesktopPetServices.AppearanceCodex;
            if (codex != null)
                Current.unlockedAppearances = codex.CaptureForSave();
            else if (Current.unlockedAppearances == null)
                Current.unlockedAppearances = new List<string>();
        }

        public static void ApplyToRuntime(
            ShopManager shop,
            ItemInventory inventory,
            DecorWorld world,
            LubyWorld lubyWorld = null)
        {
            if (Current == null)
                Load();
            NormalizeCurrent();

            if (shop != null && shop.Wallet != null)
                shop.Wallet.SetCurrency(Current.currency);

            if (inventory != null)
            {
                var list = new List<ItemInventory.Entry>();
                for (int i = 0; i < Current.inventory.Count; i++)
                {
                    DesktopPetInventoryEntry e = Current.inventory[i];
                    list.Add(new ItemInventory.Entry { itemId = e.itemId, count = e.count });
                }

                inventory.ReplaceAll(list);
            }

            BackgroundCatalog catalog = BackgroundCatalog.LoadDefault();
            string bgId = BackgroundCatalog.ResolveActiveId(Current.currentBackgroundId, catalog);
            if (string.IsNullOrEmpty(Current.currentBackgroundId))
                Current.currentBackgroundId = bgId;
            BackgroundDefinition def = catalog?.FindById(bgId);

            if (world != null)
            {
                world.SetDeskCapacity(BackgroundSceneCapacity.GetDecorCapacity(bgId, def));
                List<DesktopPetPlacedEntry> scenePlaced = Current.GetScenePlaced(bgId)
                    ?? new List<DesktopPetPlacedEntry>();
                world.RebuildFromSave(scenePlaced, shop != null ? shop.Catalog : null);
            }

            if (lubyWorld != null)
            {
                lubyWorld.SetDeskCapacity(BackgroundSceneCapacity.GetLubyCapacity(bgId, def));
                lubyWorld.RebuildWarehouseFromSave(Current.lubyWarehouse);
                lubyWorld.RebuildFromSave(Current.lubies);
            }

            LubyAppearanceCodex codex = DesktopPetServices.AppearanceCodex;
            if (codex != null)
            {
                codex.ReplaceFromSave(Current.unlockedAppearances);
                if (codex.BackfillFromOwned(lubyWorld))
                {
                    Current.unlockedAppearances = codex.CaptureForSave();
                    Save();
                }
            }
        }

        public static void SaveRuntime(
            ShopManager shop,
            ItemInventory inventory,
            DecorWorld world,
            LubyWorld lubyWorld = null)
        {
            CaptureFromRuntime(shop, inventory, world, lubyWorld);
            Save();
        }

        /// <summary>业务写档统一入口：优先 DecorPlacement.Persist（带本地引用），否则用 Services。</summary>
        public static void PersistActive()
        {
            DecorPlacementSystem placement = DesktopPetServices.Placement;
            if (placement != null)
            {
                placement.Persist();
                return;
            }

            SaveRuntime(
                DesktopPetServices.Shop,
                DesktopPetServices.Inventory,
                DesktopPetServices.DecorWorld,
                DesktopPetServices.LubyWorld);
        }

        /// <summary>
        /// 切换激活背景：先把当前桌面写入旧 scene 槽，再 Apply 底图、重建新 scene 装饰与容量，最后写盘。
        /// 购买/解锁由调用方处理。
        /// </summary>
        public static bool SwitchActiveBackground(string backgroundId)
        {
            BackgroundSystem bg = BackgroundSystem.Instance;
            DecorWorld decorWorld = DesktopPetServices.DecorWorld;
            if (bg == null || decorWorld == null)
            {
                Debug.LogError("[DesktopPetSave] 切换背景失败：缺少 BackgroundSystem 或 DecorWorld。");
                return false;
            }

            if (string.IsNullOrEmpty(backgroundId) || backgroundId == bg.CurrentBackgroundId)
                return true;

            string oldBgId = bg.CurrentBackgroundId;
            CaptureDecorToScene(oldBgId, decorWorld);
            BackgroundSceneCapacity.CaptureFromRuntime(
                oldBgId,
                decorWorld,
                DesktopPetServices.LubyWorld);

            bg.ApplyBackground(backgroundId);

            BackgroundDefinition targetDef = bg.Catalog?.FindById(backgroundId);
            List<DesktopPetPlacedEntry> scenePlaced =
                Current.GetScenePlaced(backgroundId) ?? new List<DesktopPetPlacedEntry>();
            ShopCatalog catalog = DesktopPetServices.Shop?.Catalog;
            decorWorld.RebuildFromSave(scenePlaced, catalog);
            BackgroundSceneCapacity.ApplyToRuntime(backgroundId, targetDef);

            PersistActive();
            return true;
        }
    }
}
