using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace PlanB
{
    public sealed class HUD : MonoBehaviour
    {
        static readonly Color Navy = new Color(.025f, .043f, .065f, .96f);
        static readonly Color Panel = new Color(.055f, .08f, .105f, .96f);
        static readonly Color Amber = new Color(1f, .68f, .27f);
        static readonly Color Pale = new Color(.85f, .88f, .85f);
        static readonly Color Muted = new Color(.48f, .57f, .60f);
        Font font;
        RectTransform canvasRect, hudRoot, menuRoot;
        Text healthText, ammoText, floorText, objectiveText, enemiesText, promptText, noticeText, inventoryText, bossText;
        Image healthFill, reloadFill, bossFill, damageFlash;
        RectTransform bossPanel;
        GameObject reticle;
        float flash, noticeUntil;
        string notice;

        void Awake()
        {
            try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch (System.ArgumentException) { font = Resources.GetBuiltinResource<Font>("Arial.ttf"); }
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            var go = new GameObject("PlanB UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
            var scale = go.GetComponent<CanvasScaler>(); scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scale.referenceResolution = new Vector2(1600, 900); scale.matchWidthOrHeight = .5f;
            canvasRect = go.GetComponent<RectTransform>();
            if (EventSystem.current == null)
            {
                var events = new GameObject("PlanB EventSystem", typeof(EventSystem));
                events.transform.SetParent(transform, false);
#if ENABLE_INPUT_SYSTEM
                events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
#else
                events.AddComponent<StandaloneInputModule>();
#endif
            }
            var vignette = FullPanel("Facility darkness", canvasRect, Color.white);
            vignette.GetComponent<Image>().sprite = Vignette();
            hudRoot = FullRect("HUD", canvasRect);
            BuildHUD();
            damageFlash = FullPanel("Damage flash", canvasRect, new Color(.8f, .02f, .04f, 0)).GetComponent<Image>();
            menuRoot = FullRect("Menu", canvasRect);
            reticle = new GameObject("Crosshair", typeof(RectTransform));
            reticle.transform.SetParent(canvasRect, false);
            var rr = reticle.GetComponent<RectTransform>(); rr.sizeDelta = new Vector2(24, 24);
            Box(rr, "Left", -12, -1, 7, 2, Amber);
            Box(rr, "Right", 5, -1, 7, 2, Amber);
            Box(rr, "Top", -1, -12, 2, 7, Amber);
            Box(rr, "Bottom", -1, 5, 2, 7, Amber);
        }

        void BuildHUD()
        {
            var health = Box(hudRoot, "Health card", 32, 28, 260, 105, Navy);
            Box(health, "Accent", 0, 0, 4, 105, Amber);
            Label(health, "VITAL SIGNS", 20, 12, 220, 23, 15, Muted);
            healthText = Label(health, "120 / 120", 20, 35, 220, 35, 27, Pale);
            Box(health, "Health track", 20, 79, 220, 8, new Color(.16f, .12f, .13f));
            healthFill = Box(health, "Health fill", 20, 79, 220, 8, new Color(.78f, .29f, .25f)).GetComponent<Image>();
            var mission = Box(hudRoot, "Mission card", 315, 28, 735, 105, Navy);
            floorText = Label(mission, "01 / RESIDENTIAL + MAINTENANCE", 22, 12, 690, 23, 16, Amber);
            objectiveText = Label(mission, "Find the fuse", 22, 40, 690, 43, 28, Pale);
            var ammo = Box(hudRoot, "Weapon card", 1074, 28, 494, 105, Navy);
            Label(ammo, "PISTOL / 9mm", 22, 12, 230, 23, 15, Muted);
            ammoText = Label(ammo, "12 / 24", 22, 37, 230, 43, 32, Amber);
            enemiesText = Label(ammo, "11 HOSTILES", 270, 37, 200, 38, 18, Pale);
            reloadFill = Box(ammo, "Reload", 22, 89, 0, 3, Amber).GetComponent<Image>();
            inventoryText = Label(hudRoot, "", 36, 145, 850, 32, 18, Pale);
            bossPanel = Box(hudRoot, "Boss bar", 475, 145, 650, 66, Navy);
            bossText = Label(bossPanel, "THE WARDEN", 18, 8, 615, 24, 16, new Color(1, .34f, .34f));
            Box(bossPanel, "Boss track", 18, 43, 614, 7, new Color(.18f, .08f, .1f));
            bossFill = Box(bossPanel, "Boss health", 18, 43, 614, 7, new Color(.85f, .19f, .24f)).GetComponent<Image>();
            var controls = Box(hudRoot, "Controls", 32, 835, 1536, 40, Navy);
            Label(controls, "WASD  MOVE     MOUSE  AIM / FIRE     R  RELOAD     E  INTERACT     SPACE / RMB  BASH     T  TRAP     ESC  PAUSE", 16, 8, 1500, 25, 15, Muted, TextAnchor.MiddleCenter);
            noticeText = Label(hudRoot, "", 300, 740, 1000, 37, 20, Amber, TextAnchor.MiddleCenter);
            var prompt = Box(hudRoot, "Interaction", 450, 785, 700, 39, Navy);
            promptText = Label(prompt, "", 12, 4, 676, 31, 21, Pale, TextAnchor.MiddleCenter);
        }

        public void ShowMenu(GamePhase phase)
        {
            foreach (Transform child in menuRoot) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            bool show = phase != GamePhase.Playing;
            menuRoot.gameObject.SetActive(show);
            hudRoot.gameObject.SetActive(phase == GamePhase.Playing || phase == GamePhase.Paused);
            Cursor.visible = phase != GamePhase.Playing;
            if (!show) return;
            FullPanel("Menu shade", menuRoot, new Color(.015f, .025f, .04f, phase == GamePhase.Title ? .78f : .91f));
            if (phase == GamePhase.Title) BuildTitle();
            else BuildStatus(phase);
        }

        void BuildTitle()
        {
            Box(menuRoot, "Top rule", 75, 72, 1450, 2, new Color(.25f, .31f, .33f));
            Label(menuRoot, "A FACILITY SURVIVAL STORY", 80, 96, 800, 29, 18, Amber);
            Label(menuRoot, "PLAN B", 70, 145, 900, 150, 112, Pale);
            Label(menuRoot, "THE LAST SHIFT", 80, 292, 800, 48, 33, Amber);
            Label(menuRoot, "Two floors below the surface.\nOne chance to bring the power back.", 84, 365, 760, 90, 29, Pale);
            Label(menuRoot, "01   FIND A FUSE + RESTORE POWER\n02   DESCEND + UNLOCK CONTAINMENT\n03   ELIMINATE THE WARDEN + ESCAPE", 84, 493, 760, 130, 21, Muted);
            Button(menuRoot, "ENTER THE FACILITY", 84, 660, 510, 67, () => GameManager.Instance.StartRun(), true);
            Button(menuRoot, "QUIT", 610, 660, 165, 67, () => GameManager.Instance.Quit(), false);
            Label(menuRoot, "WASD move  /  Mouse aim + shoot  /  E interact\nR reload  /  Space or right click bash  /  Esc pause", 84, 761, 780, 65, 18, Muted);
            var card = Box(menuRoot, "Survivor selection", 960, 125, 560, 674, Panel);
            Label(card, "CHOOSE YOUR SURVIVOR", 28, 25, 500, 35, 22, Pale);
            ClassCard(card, CharacterClass.Fighter, "FIGHTER", "120 HP  /  DOUBLE BASH DAMAGE", 95);
            ClassCard(card, CharacterClass.Engineer, "ENGINEER", "100 HP  /  FAST RELOAD + EXTRA AMMO", 270);
            ClassCard(card, CharacterClass.Scout, "SCOUT", "90 HP  /  25% FASTER MOVEMENT", 445);
            Label(card, "Melee always works when ammunition runs out.", 28, 628, 510, 27, 16, Muted);
            Label(menuRoot, "RESIDENTIAL  /  MAINTENANCE  /  RESEARCH  /  SECURITY", 80, 32, 1400, 28, 16, Muted);
        }

        void ClassCard(RectTransform parent, CharacterClass choice, string title, string detail, float y)
        {
            bool selected = GameManager.Instance.SelectedClass == choice;
            var card = Box(parent, title, 26, y, 508, 150, selected ? new Color(.19f, .16f, .105f) : Navy);
            Box(card, "Selection stripe", 0, 0, 4, 150, selected ? Amber : new Color(.2f, .26f, .29f));
            var portrait = Box(card, "Portrait", 24, 24, 80, 105, Color.white).GetComponent<Image>();
            portrait.sprite = SpriteLibrary.Get(choice.ToString().ToLowerInvariant(), 1, 0); portrait.preserveAspect = true;
            Label(card, title + (selected ? "   / SELECTED" : ""), 130, 20, 350, 35, 23, selected ? Amber : Pale);
            Label(card, detail, 130, 59, 355, 48, 15, Muted);
            Button(card, selected ? "READY" : "SELECT", 130, 107, 345, 28, () => { GameManager.Instance.SelectedClass = choice; ShowMenu(GamePhase.Title); }, selected);
        }

        void BuildStatus(GamePhase phase)
        {
            var card = Box(menuRoot, "Status card", 400, 180, 800, 550, Navy);
            Box(card, "Accent", 0, 0, 800, 4, Amber);
            string title = phase == GamePhase.Paused ? "SHIFT PAUSED" : phase == GamePhase.Won ? "YOU MADE IT OUT" : "SIGNAL LOST";
            Label(card, "PLAN B / THE LAST SHIFT", 40, 31, 720, 29, 17, Amber, TextAnchor.MiddleCenter);
            Label(card, title, 30, 89, 740, 75, 44, Pale, TextAnchor.MiddleCenter);
            var game = GameManager.Instance;
            string detail = phase == GamePhase.Paused ? "Catch your breath. The facility can wait."
                : phase == GamePhase.Won ? "Power restored. Containment breached. Survivor recovered."
                : "The facility claimed another shift. Try a different survivor.";
            Label(card, detail, 40, 182, 720, 75, 22, Muted, TextAnchor.MiddleCenter);
            if (phase != GamePhase.Paused)
                Label(card, string.Format("TIME  {0:00}:{1:00}     HOSTILES  {2}\nSALVAGE  {3} SCRAP  /  {4} BATTERIES  /  {5} WIRE", (int)game.RunTime / 60, (int)game.RunTime % 60, game.Kills, game.Scrap, game.Battery, game.Wire), 40, 264, 720, 65, 19, Amber, TextAnchor.MiddleCenter);
            Button(card, phase == GamePhase.Paused ? "RESUME SHIFT" : "TRY AGAIN", 120, 365, 560, 62,
                () => { if (phase == GamePhase.Paused) game.SetPaused(false); else game.StartRun(); }, true);
            Button(card, "CHANGE SURVIVOR", 120, 445, 560, 48, () => game.ShowTitle(), false);
        }

        void Update()
        {
            Refresh();
            flash = Mathf.MoveTowards(flash, 0, Time.unscaledDeltaTime * .85f);
            damageFlash.color = new Color(.85f, .025f, .04f, flash);
            noticeText.text = Time.unscaledTime < noticeUntil ? notice : "";
            bool playing = GameManager.Instance != null && GameManager.Instance.IsPlaying;
            reticle.SetActive(playing);
            if (playing)
            {
                Vector2 local;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, InputBridge.MousePosition, null, out local);
                reticle.GetComponent<RectTransform>().anchoredPosition = local;
            }
        }

        public void Refresh()
        {
            var game = GameManager.Instance;
            if (game == null || game.Player == null || healthText == null) return;
            var player = game.Player;
            healthText.text = player.Health + " / " + player.MaxHealth;
            healthFill.rectTransform.sizeDelta = new Vector2(220f * player.Health / player.MaxHealth, 8);
            var weapon = player.Weapon;
            ammoText.text = weapon.Reloading ? "RELOADING…" : weapon.Magazine.ToString("00") + " / " + weapon.Reserve.ToString("00");
            ammoText.fontSize = weapon.Reloading ? 24 : 32;
            reloadFill.rectTransform.sizeDelta = new Vector2(450 * weapon.ReloadProgress, 3);
            floorText.text = game.Floors.CurrentFloor == 1 ? "01 / RESIDENTIAL + MAINTENANCE" : "02 / RESEARCH + SECURITY";
            objectiveText.text = game.Objective;
            enemiesText.text = game.Floors.EnemiesRemaining.ToString("00") + " HOSTILES";
            promptText.text = player.Nearby != null ? "[ E ]  " + player.Nearby.Prompt.Replace("[E]", "").Trim() : "";
            promptText.transform.parent.gameObject.SetActive(player.Nearby != null);
            inventoryText.text = (game.HasFuse ? "[ FUSE ]   " : "") + (game.HasKey ? "[ SECURITY KEY ]   " : "") + "TRAPS " + game.TrapCount + " / 2" + (Time.time < game.AlarmUntil ? "     • SECURITY ALERT" : "");
            Boss boss = game.Floors.ActiveRoot != null ? game.Floors.ActiveRoot.GetComponentInChildren<Boss>() : null;
            bossPanel.gameObject.SetActive(boss != null && !boss.IsDead && game.DoorUnlocked);
            if (boss != null)
            {
                bossText.text = "THE WARDEN    /    " + boss.Health + " HP";
                bossFill.rectTransform.sizeDelta = new Vector2(614f * boss.Health / Mathf.Max(1, boss.MaxHealth), 7);
            }
        }
        public void Notify(string message, float duration = 3) { notice = message; noticeUntil = Time.unscaledTime + duration; }
        public void Flash() { flash = .34f; }

        RectTransform FullRect(string name, Transform parent)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent, false);
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; return r;
        }
        RectTransform FullPanel(string name, Transform parent, Color color)
        {
            var r = FullRect(name, parent); var image = r.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return r;
        }
        RectTransform Box(Transform parent, string name, float x, float y, float width, float height, Color color)
        {
            var r = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            r.SetParent(parent, false); r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(width, height);
            var image = r.GetComponent<Image>(); image.color = color; image.raycastTarget = false; return r;
        }
        Text Label(Transform parent, string value, float x, float y, float width, float height, int size, Color color, TextAnchor align = TextAnchor.UpperLeft)
        {
            var r = new GameObject(value, typeof(RectTransform), typeof(Text)).GetComponent<RectTransform>();
            r.SetParent(parent, false); r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(width, height);
            var text = r.GetComponent<Text>(); text.font = font; text.text = value; text.fontSize = size; text.color = color;
            text.alignment = align; text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate; text.raycastTarget = false;
            return text;
        }
        void Button(Transform parent, string title, float x, float y, float width, float height, UnityEngine.Events.UnityAction click, bool primary)
        {
            var r = Box(parent, title, x, y, width, height, primary ? Amber : new Color(.12f, .17f, .20f));
            var image = r.GetComponent<Image>(); image.raycastTarget = true;
            var button = r.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.highlightedColor = new Color(1, .85f, .56f); colors.pressedColor = new Color(.65f, .65f, .65f); button.colors = colors;
            button.onClick.AddListener(click);
            Label(r, title, 8, 0, width - 16, height, height < 35 ? 14 : 20, primary ? Navy : Pale, TextAnchor.MiddleCenter);
        }

        static Sprite Vignette()
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear; texture.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float distance = new Vector2((x - 63.5f) / 63.5f, (y - 63.5f) / 63.5f).magnitude;
                pixels[y * size + x] = new Color(.015f, .025f, .04f, Mathf.SmoothStep(0, .55f, Mathf.InverseLerp(.3f, 1.25f, distance)));
            }
            texture.SetPixels(pixels); texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f));
        }
    }
}
