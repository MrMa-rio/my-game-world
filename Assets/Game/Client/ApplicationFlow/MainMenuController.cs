using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace MyGameWorld.Client.ApplicationFlow
{
    [RequireComponent(typeof(UIDocument))]
    [DisallowMultipleComponent]
    public sealed class MainMenuController : MonoBehaviour
    {
        private static readonly IReadOnlyList<DeveloperSceneEntry> DevelopmentEntries = new[]
        {
            new DeveloperSceneEntry(
                SceneId.ProceduralWorld,
                "Procedural World",
                "WORLD",
                "Inspect deterministic terrain, environment and procedural runtime.",
                true)
        };

        [SerializeField]
        private ApplicationSceneCatalog _sceneCatalog;

        private UIDocument _document;
        private SceneLoader _sceneLoader;

        public ApplicationFlowState State { get; private set; } = ApplicationFlowState.MainMenu;

#if UNITY_EDITOR
        public void Configure(ApplicationSceneCatalog sceneCatalog) => _sceneCatalog = sceneCatalog;
#endif

        private void OnEnable()
        {
            _document = GetComponent<UIDocument>();
            _sceneLoader = new SceneLoader(_sceneCatalog);
            ShowMainMenu();
            Debug.Log("[GameFlow] MainMenu");
        }

        public void ShowMainMenu()
        {
            State = ApplicationFlowState.MainMenu;
            VisualElement panel = CreatePanel("MY GAME WORLD", "PROCEDURAL MMORPG FOUNDATION");
            panel.Add(CreateButton("PLAY", null, false));
            panel.Add(CreateButton("DEVELOPMENT", ShowDeveloperMenu, true));
            panel.Add(CreateButton("SETTINGS", null, false));
            panel.Add(CreateButton("EXIT", Application.Quit, true));
            ReplaceRoot(panel);
        }

        public void ShowDeveloperMenu()
        {
            State = ApplicationFlowState.Development;
            VisualElement panel = CreatePanel("DEVELOPMENT", "TECHNICAL ENVIRONMENTS");
            string activeCategory = string.Empty;
            for (int index = 0; index < DevelopmentEntries.Count; index++)
            {
                DeveloperSceneEntry entry = DevelopmentEntries[index];
                if (entry.Category != activeCategory)
                {
                    activeCategory = entry.Category;
                    panel.Add(CreateCategory(activeCategory));
                }

                panel.Add(CreateButton(entry.Title, () => Launch(entry), entry.Enabled));
                panel.Add(CreateDescription(entry.Description));
            }

            AddComingSoon(panel, "CHARACTERS");
            AddComingSoon(panel, "AI");
            AddComingSoon(panel, "ANIMATION");
            AddComingSoon(panel, "NETWORK");
            panel.Add(CreateButton("BACK", ShowMainMenu, true));
            ReplaceRoot(panel);
        }

        public void LaunchProceduralWorld()
        {
            Launch(DevelopmentEntries[0]);
        }

        private void Launch(DeveloperSceneEntry entry)
        {
            if (!entry.Enabled)
            {
                return;
            }

            Debug.Log($"[DeveloperMenu] Launching {entry.SceneId}");
            State = ApplicationFlowState.Loading;
            if (_sceneLoader.Load(entry.SceneId) == null)
            {
                State = ApplicationFlowState.Development;
            }
        }

        private static void AddComingSoon(VisualElement panel, string category)
        {
            panel.Add(CreateCategory(category));
            panel.Add(CreateButton("Coming Soon", null, false));
        }

        private static VisualElement CreatePanel(string title, string subtitle)
        {
            VisualElement panel = new VisualElement { name = "menu-panel" };
            panel.style.width = 520f;
            panel.style.maxHeight = new Length(92f, LengthUnit.Percent);
            panel.style.paddingLeft = 44f;
            panel.style.paddingRight = 44f;
            panel.style.paddingTop = 34f;
            panel.style.paddingBottom = 34f;
            panel.style.backgroundColor = new Color(0.045f, 0.075f, 0.105f, 0.96f);
            panel.style.borderTopLeftRadius = 18f;
            panel.style.borderTopRightRadius = 18f;
            panel.style.borderBottomLeftRadius = 18f;
            panel.style.borderBottomRightRadius = 18f;

            Label titleLabel = new Label(title);
            titleLabel.style.fontSize = 34f;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.color = new Color(0.85f, 0.95f, 1f);
            titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            panel.Add(titleLabel);

            Label subtitleLabel = new Label(subtitle);
            subtitleLabel.style.fontSize = 11f;
            subtitleLabel.style.letterSpacing = 3f;
            subtitleLabel.style.color = new Color(0.36f, 0.72f, 0.78f);
            subtitleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            subtitleLabel.style.marginBottom = 24f;
            panel.Add(subtitleLabel);
            return panel;
        }

        private static Label CreateCategory(string text)
        {
            Label label = new Label(text);
            label.style.fontSize = 11f;
            label.style.letterSpacing = 2f;
            label.style.color = new Color(0.4f, 0.76f, 0.81f);
            label.style.marginTop = 12f;
            label.style.marginBottom = 4f;
            return label;
        }

        private static Label CreateDescription(string text)
        {
            Label label = new Label(text);
            label.style.fontSize = 11f;
            label.style.color = new Color(0.58f, 0.67f, 0.71f);
            label.style.marginBottom = 4f;
            return label;
        }

        private static Button CreateButton(string text, System.Action clicked, bool enabled)
        {
            Button button = clicked == null ? new Button() : new Button(clicked);
            button.text = text;
            button.SetEnabled(enabled);
            button.style.height = 44f;
            button.style.marginTop = 4f;
            button.style.marginBottom = 4f;
            button.style.fontSize = 14f;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.color = enabled ? new Color(0.9f, 0.96f, 1f) : new Color(0.42f, 0.47f, 0.5f);
            button.style.backgroundColor = enabled
                ? new Color(0.1f, 0.24f, 0.29f, 0.96f)
                : new Color(0.08f, 0.1f, 0.12f, 0.72f);
            return button;
        }

        private void ReplaceRoot(VisualElement panel)
        {
            VisualElement root = _document.rootVisualElement;
            root.Clear();
            root.style.flexGrow = 1f;
            root.style.alignItems = Align.Center;
            root.style.justifyContent = Justify.Center;
            root.style.backgroundColor = new Color(0.018f, 0.035f, 0.055f, 1f);
            root.Add(panel);
        }
    }
}
