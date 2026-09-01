#if UNITY_EDITOR
using System.Collections.Generic;
using System.Diagnostics;
using DesktopPet.Background;
using DesktopPet.Decor;
using DesktopPet.Environment;
using DesktopPet.Luby;
using DesktopPet.Save;
using DesktopPet.Settings;
using DesktopPet.Shop;
using UnityEditor;
using UnityEngine;

namespace DesktopPet.Gm.Editor
{
    /// <summary>
    /// 桌宠 Editor GM：对齐运行时 DesktopPetGmController，并加环境快捷调试。
    /// </summary>
    public sealed class DesktopPetGmWindow : EditorWindow
    {
        private int _customMoney = 500;
        private string _status = "";
        private Vector2 _scroll;

        private int _grantTemplateIndex;
        private int _grantAppearanceIndex;
        private int _grantPersonalityIndex;
        private int _grantTraitIndex;
        private int _grantTrait2Index;

        private static readonly string[] PhaseLabels = { "白天", "黄昏", "夜晚" };

        [MenuItem("桌宠/GM 编辑器 %#g")]
        public static void Open()
        {
            var window = GetWindow<DesktopPetGmWindow>();
            window.titleContent = new GUIContent("桌宠 GM");
            window.minSize = new Vector2(320, 420);
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        private void OnPlayModeChanged(PlayModeStateChange _)
        {
            Repaint();
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.LabelField("桌宠 GM", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Play Mode：必须有 DesktopPetGmController（应用 MainCanvas 后会接线）。\n" +
                "Edit Mode 只能查看存档路径。运行时 Demo 右上角 GM 面板仍可用。",
                MessageType.Info);

            DrawStatus();
            EditorGUILayout.Space(8);

            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
            {
                DrawTimeScale();
                EditorGUILayout.Space(6);
                DrawEconomy();
                EditorGUILayout.Space(6);
                DrawDecor();
                EditorGUILayout.Space(6);
                DrawLuby();
                EditorGUILayout.Space(6);
                DrawSave();
                EditorGUILayout.Space(6);
                DrawEnvironment();
            }

            if (!EditorApplication.isPlaying)
                EditorGUILayout.HelpBox("进入 Play Mode 后才能改金币 / 装饰 / Luby / 环境。", MessageType.Warning);

            EditorGUILayout.Space(8);
            DrawEditModeTools();

            if (!string.IsNullOrEmpty(_status))
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.HelpBox(_status, MessageType.None);
            }

            EditorGUILayout.EndScrollView();

            if (EditorApplication.isPlaying)
                Repaint();
        }

        private void DrawStatus()
        {
            EditorGUILayout.LabelField("状态", EditorStyles.boldLabel);

            bool playing = EditorApplication.isPlaying;
            int currency = 0;
            int placed = 0;
            int inv = 0;
            int lubyCount = 0;
            bool hasSave = DesktopPetSaveMgr.HasSaveFile();

            if (playing)
            {
                ShopManager shop = DesktopPetServices.Shop;
                DecorWorld world = DesktopPetServices.DecorWorld;
                if (shop != null && shop.Wallet != null)
                    currency = shop.Wallet.Currency;
                if (world != null)
                    placed = world.Placed.Count;
                if (shop != null && shop.Inventory != null)
                    inv = shop.Inventory.Entries.Count;
                LubyWorld lubyWorld = DesktopPetServices.LubyWorld;
                if (lubyWorld != null)
                    lubyCount = lubyWorld.Count;
            }

            EditorGUILayout.LabelField("存档文件", hasSave ? "有 desktoppet.json" : "无");
            EditorGUILayout.LabelField("金币", playing ? currency.ToString() : "—");
            EditorGUILayout.LabelField("桌上装饰", playing ? placed.ToString() : "—");
            EditorGUILayout.LabelField("仓库条目", playing ? inv.ToString() : "—");
            EditorGUILayout.LabelField("Luby 数量", playing ? lubyCount.ToString() : "—");
        }

        private void DrawTimeScale()
        {
            EditorGUILayout.LabelField("时间加速", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Ctrl+Shift+T 切换 1→2→5→10。只影响 Time.timeScale（走动/动画/看板计时）。\n" +
                "探险离桌 UTC 倒计时不受影响。退出 Play 自动回 1。",
                MessageType.None);

            float scale = DesktopPetTimeScaleEditor.CurrentScale;
            EditorGUILayout.LabelField("当前", $"{scale:0.##}x");

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("x1"))
                DesktopPetTimeScaleEditor.Apply(1f);
            if (GUILayout.Button("x2"))
                DesktopPetTimeScaleEditor.Apply(2f);
            if (GUILayout.Button("x5"))
                DesktopPetTimeScaleEditor.Apply(5f);
            if (GUILayout.Button("x10"))
                DesktopPetTimeScaleEditor.Apply(10f);
            if (GUILayout.Button("切换"))
                DesktopPetTimeScaleEditor.Cycle();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawEconomy()
        {
            EditorGUILayout.LabelField("经济", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+100"))
                RunGm(gm => gm.AddMoney(100));
            if (GUILayout.Button("+1000"))
                RunGm(gm => gm.AddMoney(1000));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _customMoney = EditorGUILayout.IntField("自定义", _customMoney);
            if (GUILayout.Button("加钱", GUILayout.Width(60)))
            {
                int amount = _customMoney;
                RunGm(gm => gm.AddMoney(amount));
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawDecor()
        {
            EditorGUILayout.LabelField("装饰", EditorStyles.boldLabel);
            if (GUILayout.Button("发放全部装饰进仓库"))
                RunGm(gm => gm.GrantAllDecorItems());
            if (GUILayout.Button("清空桌上装饰"))
                RunGm(gm => gm.ClearPlacedDecors());
        }

        private void DrawLuby()
        {
            EditorGUILayout.LabelField("Luby", EditorStyles.boldLabel);
            DrawGrantSpecifiedLuby();
            EditorGUILayout.Space(4);
            if (GUILayout.Button("移除最近一只 Luby"))
                RunGm(gm => gm.RemoveLastLuby());
            if (GUILayout.Button("清空全部 Luby"))
                RunGm(gm => gm.ClearAllLubies());
        }

        private void DrawGrantSpecifiedLuby()
        {
            EditorGUILayout.LabelField("指定获得（配置）", EditorStyles.miniBoldLabel);

            LubyWorld world = DesktopPetServices.LubyWorld;
            LubyTemplateCatalog catalog = world != null ? world.Catalog : LubyTemplateCatalog.LoadDefault();
            if (catalog == null || catalog.templates == null || catalog.templates.Count == 0)
            {
                EditorGUILayout.HelpBox("无 Luby 目录 / 模板。", MessageType.None);
                return;
            }

            var templates = new List<LubyTemplateDefinition>();
            var templateLabels = new List<string>();
            DesktopPetGmGrantCatalog.CollectTemplates(catalog, templates, templateLabels);

            if (templates.Count == 0)
            {
                EditorGUILayout.HelpBox("目录里没有可用模板。", MessageType.None);
                return;
            }

            _grantTemplateIndex = Mathf.Clamp(_grantTemplateIndex, 0, templates.Count - 1);
            _grantTemplateIndex = EditorGUILayout.Popup("模板", _grantTemplateIndex, templateLabels.ToArray());
            LubyTemplateDefinition template = templates[_grantTemplateIndex];

            var appearances = new List<GameObject>();
            var appearanceLabels = new List<string>();
            DesktopPetGmGrantCatalog.CollectAppearances(template, appearances, appearanceLabels);
            if (appearances.Count > 0)
            {
                _grantAppearanceIndex = Mathf.Clamp(_grantAppearanceIndex, 0, appearances.Count - 1);
                _grantAppearanceIndex = EditorGUILayout.Popup("外形", _grantAppearanceIndex, appearanceLabels.ToArray());
            }
            else
            {
                EditorGUILayout.LabelField("外形", "（仅模板默认 Prefab）");
            }

            var personalities = new List<LubyPersonalityDefinition>();
            var personalityLabels = new List<string>();
            DesktopPetGmGrantCatalog.CollectPersonalities(
                catalog, template, personalities, personalityLabels, includeNone: true);
            _grantPersonalityIndex = Mathf.Clamp(_grantPersonalityIndex, 0, personalityLabels.Count - 1);
            _grantPersonalityIndex = EditorGUILayout.Popup("性格", _grantPersonalityIndex, personalityLabels.ToArray());

            var traits = new List<LubyTraitDefinition>();
            var traitLabels = new List<string>();
            DesktopPetGmGrantCatalog.CollectTraits(
                catalog, template, traits, traitLabels, includeNone: true);
            _grantTraitIndex = Mathf.Clamp(_grantTraitIndex, 0, traitLabels.Count - 1);
            _grantTrait2Index = Mathf.Clamp(_grantTrait2Index, 0, traitLabels.Count - 1);
            _grantTraitIndex = EditorGUILayout.Popup("特质", _grantTraitIndex, traitLabels.ToArray());
            _grantTrait2Index = EditorGUILayout.Popup("第二特质", _grantTrait2Index, traitLabels.ToArray());

            if (GUILayout.Button("指定获得（免费）"))
            {
                GameObject appearance = appearances.Count > 0 ? appearances[_grantAppearanceIndex] : null;
                string appearanceKey = appearance != null ? appearance.name : string.Empty;
                LubyPersonalityDefinition personality = personalities[_grantPersonalityIndex];
                LubyTraitDefinition trait = traits[_grantTraitIndex];
                LubyTraitDefinition trait2 = traits[_grantTrait2Index];
                DesktopPetGmController gm = ResolveGm();
                if (gm == null)
                {
                    SetStatus("缺少 DesktopPetGmController。请：应用主面板预制体");
                    return;
                }

                bool ok = gm.GrantSpecifiedLuby(template, appearance, appearanceKey, personality, trait, trait2);
                SetStatus(ok ? "指定获得成功" : "指定获得失败");
            }
        }

        private void DrawSave()
        {
            EditorGUILayout.LabelField("存档", EditorStyles.boldLabel);
            GUI.backgroundColor = new Color(1f, 0.55f, 0.55f);
            if (GUILayout.Button("重置存档"))
            {
                if (EditorUtility.DisplayDialog(
                        "重置桌宠存档",
                        "将删除 desktoppet.json（及旧 desktoppet.dat），并清空仓库/桌上装饰/Luby，金币回 Catalog 起始值。",
                        "重置",
                        "取消"))
                {
                    RunGm(gm => gm.ResetSave());
                }
            }
            GUI.backgroundColor = Color.white;
        }

        private void DrawEnvironment()
        {
            EditorGUILayout.LabelField("环境", EditorStyles.boldLabel);

            EnvironmentManager env = DesktopPetServices.Environment;
            if (env == null || env.DayNight == null)
            {
                EditorGUILayout.HelpBox("场景中无 EnvironmentManager（Services 未注册）。", MessageType.None);
                return;
            }

            EnvironmentApplicator applicator = ResolveEnvironmentApplicator();
            if (applicator == null)
            {
                EditorGUILayout.HelpBox(
                    "无 EnvironmentApplicator，只读显示（避免与设置存档不同步）。",
                    MessageType.Warning);
                EditorGUILayout.LabelField("昼夜", PhaseLabels[Mathf.Clamp((int)env.DayNight.CurrentPhase, 0, PhaseLabels.Length - 1)]);
                EditorGUILayout.LabelField("自动昼夜循环", env.DayNight.AutoCycleEnabled ? "开" : "关");
                if (env.Weather?.CurrentWeather != null)
                    EditorGUILayout.LabelField("当前天气", env.Weather.CurrentWeather.displayName);
                return;
            }

            DayNightPhase phase = env.DayNight.CurrentPhase;
            int phaseIndex = Mathf.Clamp((int)phase, 0, PhaseLabels.Length - 1);
            int newPhase = EditorGUILayout.Popup("昼夜", phaseIndex, PhaseLabels);
            if (newPhase != phaseIndex)
            {
                applicator.ApplyDayNightPhase((DayNightPhase)newPhase, fromManual: true);
                SetStatus($"昼夜 → {PhaseLabels[newPhase]}");
            }

            bool auto = env.DayNight.AutoCycleEnabled;
            bool newAuto = EditorGUILayout.Toggle("自动昼夜循环", auto);
            if (newAuto != auto)
            {
                applicator.ApplyDayNightAutoCycle(newAuto);
                SetStatus(newAuto ? "已开自动昼夜" : "已关自动昼夜");
            }

            if (env.Weather != null)
            {
                string weatherName = env.Weather.CurrentWeather != null
                    ? env.Weather.CurrentWeather.displayName
                    : "—";
                EditorGUILayout.LabelField("当前天气", weatherName);

                WeatherCatalog catalog = applicator.WeatherCatalog;
                BackgroundDefinition bgDef = BackgroundWeatherRules.ResolveActiveDefinition();
                List<WeatherDefinition> allowed = BackgroundWeatherRules.GetAllowedWeathers(bgDef, catalog);
                if (allowed.Count > 0)
                {
                    var labels = new List<string>(allowed.Count + 1);
                    for (int i = 0; i < allowed.Count; i++)
                    {
                        WeatherDefinition w = allowed[i];
                        labels.Add(w != null ? w.displayName : "天气");
                    }

                    if (allowed.Count > 1)
                        labels.Add("随机");

                    int pick = EditorGUILayout.Popup("切换天气", -1, labels.ToArray());
                    if (pick >= 0 && pick < labels.Count)
                    {
                        if (allowed.Count > 1 && pick == allowed.Count)
                        {
                            applicator.ApplyRandomWeather();
                            SetStatus("天气 → 随机");
                        }
                        else
                        {
                            WeatherDefinition weather = allowed[pick];
                            if (weather != null && applicator.ApplyConcreteWeather(weather))
                                SetStatus($"天气 → {weather.displayName}");
                        }
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("当前背景未配置可用天气。", MessageType.Info);
                }
            }
        }

        private void DrawEditModeTools()
        {
            EditorGUILayout.LabelField("工具", EditorStyles.boldLabel);
            if (GUILayout.Button("打开存档目录"))
            {
                string path = Application.persistentDataPath;
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }

            EditorGUILayout.SelectableLabel(
                Application.persistentDataPath,
                EditorStyles.miniLabel,
                GUILayout.Height(EditorGUIUtility.singleLineHeight));
        }

        private static DesktopPetGmController ResolveGm()
        {
            GameObject host = GameObject.Find("GmSystem");
            return host != null ? host.GetComponent<DesktopPetGmController>() : null;
        }

        private static EnvironmentApplicator ResolveEnvironmentApplicator()
        {
            GameObject host = GameObject.Find("SettingsSystem");
            return host != null ? host.GetComponent<EnvironmentApplicator>() : null;
        }

        private void RunGm(System.Action<DesktopPetGmController> action)
        {
            DesktopPetGmController gm = ResolveGm();
            if (gm == null)
            {
                SetStatus("缺少 DesktopPetGmController。请：应用主面板预制体（MainCanvas 含 GM UI）");
                return;
            }

            action(gm);
        }

        private void SetStatus(string msg)
        {
            _status = msg;
            UnityEngine.Debug.Log($"[桌宠GM] {msg}");
        }
    }
}
#endif
