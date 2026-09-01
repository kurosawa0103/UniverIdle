#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using DesktopPet;
using DesktopPet.Shop;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using static DesktopPet.Editor.DesktopPetEditorUi;

namespace DesktopPet.Decor.Editor
{
    /// <summary>
    /// 创建装饰物体：选精灵 → 自动生成 Prefab + ShopItemDefinition，可选写入 Catalog。
    /// </summary>
    public sealed class DecorItemCreatorWindow : OdinEditorWindow
    {
        private const string DefaultPrefabFolder = "Assets/Resources/Prefabs/Prefabs_Decor";
        private const string DefaultItemFolder = "Assets/Resources/GameData/ShopItemData";
        private const string DefaultCatalogPath = "Assets/Resources/GameData/ShopItemData/DefaultShopCatalog.asset";

        [MenuItem("桌宠/创建装饰物体")]
        private static void Open()
        {
            var window = GetWindow<DecorItemCreatorWindow>();
            window.titleContent = new GUIContent("创建装饰物体");
            window.minSize = new Vector2(420f, 560f);
            window.Show();
        }

        [Title("创建装饰物体", "选图填参数 → 生成 Prefab + 商品 Asset")]
        [InfoBox("生成内容：\n1) 摆放预制体（SpriteRenderer + Trigger BoxCollider2D，底轴心）\n2) ShopItemDefinition 商品 SO\n可选：写入商店目录（商店打开时会按 Catalog 自动刷新列表，无需再复制场景行）。", InfoMessageType.None)]

        [FoldoutGroup("外观", expanded: true)]
        [LabelText("精灵图")]
        [Required("请指定 Sprite（可先把贴图设为 Sprite）")]
        [PreviewField(80)]
        [AssetsOnly]
        public Sprite sourceSprite;

        [FoldoutGroup("外观")]
        [LabelText("目标世界高度")]
        [Tooltip("按精灵高度缩放到约这么高（世界单位）。0 = 不额外缩放。")]
        [MinValue(0f)]
        [SuffixLabel("世界单位", true)]
        public float targetWorldHeight = 3.5f;

        [FoldoutGroup("外观")]
        [LabelText("强制底轴心")]
        [Tooltip("生成前把贴图 Sprite Alignment 设为 Bottom Center（推荐贴地）。")]
        public bool forceBottomPivot = true;

        [FoldoutGroup("商品信息", expanded: true)]
        [LabelText("商品 ID")]
        [Tooltip("存档/仓库用稳定 ID，如 decor_tree。留空则按显示名自动生成。")]
        public string itemId;

        [FoldoutGroup("商品信息")]
        [LabelText("显示名")]
        [Required]
        public string displayName = "新装饰";

        [FoldoutGroup("商品信息")]
        [LabelText("描述")]
        [TextArea(2, 4)]
        public string description;

        [FoldoutGroup("商品信息")]
        [LabelText("价格")]
        [MinValue(0)]
        public int price = 20;

        [FoldoutGroup("商品信息")]
        [LabelText("持有上限")]
        [Tooltip("0 = 不限。")]
        [MinValue(0)]
        public int maxOwnCount;

        [FoldoutGroup("摆放规则", expanded: true)]
        [LabelText("预制体加顶面 PlaceSurface")]
        [Tooltip("架子底座/木桩等：在 Prefab 顶边加 DecorPlaceSurface，别人可摆上来。")]
        public bool addPlaceSurface;

        [FoldoutGroup("摆放规则")]
        [LabelText("允许自己放到可摆放面上")]
        public bool canStackOnOthers;

        [FoldoutGroup("摆放规则")]
        [LabelText("家具用途")]
        [Tooltip("None / Bed / Chair / Floor；睡觉认 Bed。")]
        public DecorFurnitureKind furnitureKind = DecorFurnitureKind.None;

        [FoldoutGroup("摆放规则")]
        [LabelText("摆放高度")]
        [Tooltip("层高校验用；≤0 自动量脚印。")]
        [MinValue(0f)]
        [SuffixLabel("世界单位，0=自动", true)]
        public float placeHeight;

        [FoldoutGroup("输出路径")]
        [FolderPath(RequireExistingPath = false)]
        [LabelText("预制体目录")]
        public string prefabFolder = DefaultPrefabFolder;

        [FoldoutGroup("输出路径")]
        [FolderPath(RequireExistingPath = false)]
        [LabelText("商品 Asset 目录")]
        public string itemFolder = DefaultItemFolder;

        [FoldoutGroup("输出路径")]
        [LabelText("文件名前缀")]
        [Tooltip("例如 Decor_Tree → Decor_Tree.prefab / Decor_Tree.asset")]
        public string fileName = "Decor_NewItem";

        [FoldoutGroup("生成选项", expanded: true)]
        [LabelText("写入商店目录")]
        public bool addToCatalog = true;

        [FoldoutGroup("生成选项")]
        [ShowIf(nameof(addToCatalog))]
        [LabelText("商店目录")]
        [AssetsOnly]
        public ShopCatalog catalog;

        [FoldoutGroup("上次结果")]
        [ShowInInspector, ReadOnly, LabelText("生成的预制体")]
        private GameObject _lastPrefab;

        [FoldoutGroup("上次结果")]
        [ShowInInspector, ReadOnly, LabelText("生成的商品")]
        private ShopItemDefinition _lastItem;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (catalog == null)
                catalog = AssetDatabase.LoadAssetAtPath<ShopCatalog>(DefaultCatalogPath);
        }

        [Button("生成预制体 + 商品 Asset", ButtonSizes.Large)]
        [GUIColor(0.35f, 0.75f, 0.45f)]
        private void Generate()
        {
            if (sourceSprite == null)
            {
                EditorUtility.DisplayDialog("创建装饰物体", "请先指定精灵图。", "好");
                return;
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                EditorUtility.DisplayDialog("创建装饰物体", "请填写显示名。", "好");
                return;
            }

            EnsureFolder(prefabFolder);
            EnsureFolder(itemFolder);

            if (addToCatalog)
            {
                if (catalog == null)
                    catalog = AssetDatabase.LoadAssetAtPath<ShopCatalog>(DefaultCatalogPath);
                if (catalog == null)
                {
                    EditorUtility.DisplayDialog(
                        "创建装饰物体",
                        "找不到 DefaultShopCatalog：\n" + DefaultCatalogPath +
                        "\n请从版本库恢复或手建 Catalog，勿自动生成。",
                        "好");
                    return;
                }
            }

            string resolvedId = string.IsNullOrWhiteSpace(itemId)
                ? MakeItemId(displayName)
                : itemId.Trim();
            string resolvedFile = string.IsNullOrWhiteSpace(fileName)
                ? "Decor_" + ToPascal(resolvedId)
                : SanitizeFileName(fileName.Trim());

            if (forceBottomPivot)
            {
                EnsureBottomPivot(sourceSprite);
                string spritePath = AssetDatabase.GetAssetPath(sourceSprite);
                sourceSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath) ?? sourceSprite;
            }

            Sprite sprite = sourceSprite;
            GameObject prefab = CreatePrefab(sprite, resolvedFile, resolvedId);
            ShopItemDefinition item = CreateOrUpdateItem(sprite, prefab, resolvedFile, resolvedId);

            if (addToCatalog)
                AddToCatalog(catalog, item);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _lastPrefab = prefab;
            _lastItem = item;
            Selection.activeObject = item;
            EditorGUIUtility.PingObject(item);

            EditorUtility.DisplayDialog(
                "创建装饰物体",
                $"已生成：\n• Prefab: {prefabFolder}/{resolvedFile}.prefab\n• Asset: {itemFolder}/{resolvedFile}.asset\n商品 ID: {resolvedId}",
                "好");
        }

        private GameObject CreatePrefab(Sprite sprite, string fileBase, string id)
        {
            string path = $"{prefabFolder}/{fileBase}.prefab";
            GameObject go = new GameObject("Decor_" + id);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 5;

            float scale = 1f;
            if (targetWorldHeight > 0.01f && sprite != null)
            {
                float worldH = sprite.bounds.size.y;
                if (worldH > 0.0001f)
                    scale = targetWorldHeight / worldH;
            }

            go.transform.localScale = new Vector3(scale, scale, 1f);

            BoxCollider2D box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            if (sprite != null)
            {
                box.size = sprite.bounds.size;
                box.offset = sprite.bounds.center;
            }

            DecorEditorDust.Attach(go);
            DesktopPetLayers.ApplyDecor(go);

            if (addPlaceSurface)
            {
                float width = Mathf.Max(0.2f, box.size.x);
                GameObject surfaceGo = new GameObject("PlaceSurface");
                surfaceGo.transform.SetParent(go.transform, false);
                surfaceGo.transform.localPosition = new Vector3(box.offset.x, box.offset.y + box.size.y * 0.5f, 0f);
                BoxCollider2D surfaceBox = surfaceGo.AddComponent<BoxCollider2D>();
                surfaceBox.isTrigger = true;
                surfaceBox.size = new Vector2(width, 0.08f);
                surfaceGo.AddComponent<DecorPlaceSurface>();
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private ShopItemDefinition CreateOrUpdateItem(
            Sprite icon,
            GameObject prefab,
            string fileBase,
            string id)
        {
            string path = $"{itemFolder}/{fileBase}.asset";
            ShopItemDefinition item = AssetDatabase.LoadAssetAtPath<ShopItemDefinition>(path);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ShopItemDefinition>();
                AssetDatabase.CreateAsset(item, path);
            }

            item.itemId = id;
            item.displayName = displayName.Trim();
            item.description = description ?? string.Empty;
            item.icon = icon;
            item.price = Mathf.Max(0, price);
            item.tab = ShopTabId.Decor;
            item.maxOwnCount = Mathf.Max(0, maxOwnCount);
            item.placementPrefab = prefab;
            item.canStackOnOthers = canStackOnOthers;
            item.furnitureKind = furnitureKind;
            item.placeHeight = Mathf.Max(0f, placeHeight);
            EditorUtility.SetDirty(item);
            return item;
        }

        private static void AddToCatalog(ShopCatalog catalog, ShopItemDefinition item)
        {
            if (catalog.items == null)
                catalog.items = new List<ShopItemDefinition>();

            for (int i = 0; i < catalog.items.Count; i++)
            {
                if (catalog.items[i] != null && catalog.items[i].itemId == item.itemId)
                {
                    catalog.items[i] = item;
                    EditorUtility.SetDirty(catalog);
                    return;
                }
            }

            catalog.items.Add(item);
            EditorUtility.SetDirty(catalog);
        }

        private static void EnsureBottomPivot(Sprite sprite)
        {
            if (sprite == null)
                return;

            string path = AssetDatabase.GetAssetPath(sprite);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            if (settings.spriteAlignment == (int)SpriteAlignment.BottomCenter)
                return;

            settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private static string MakeItemId(string name)
        {
            string id = "decor_" + Regex.Replace(name.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "_");
            id = Regex.Replace(id, @"_+", "_").Trim('_');
            if (string.IsNullOrEmpty(id) || id == "decor")
                id = "decor_item_" + System.DateTime.Now.ToString("HHmmss");
            return id;
        }

        private static string ToPascal(string id)
        {
            string[] parts = id.Split(new[] { '_' }, System.StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length == 0)
                    continue;
                parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1);
            }

            return string.Join(string.Empty, parts);
        }

        private static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name.Replace(' ', '_');
        }
    }
}
#endif
