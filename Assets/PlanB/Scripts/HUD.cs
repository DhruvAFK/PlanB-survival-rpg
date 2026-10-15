using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace PlanB
{
    /// <summary>A cached facility terminal. Only changed values rebuild the static HUD canvas.</summary>
    public sealed class HUD : MonoBehaviour
    {
        static readonly Color Navy = new Color(.025f, .046f, .065f, .95f);
        static readonly Color Panel = new Color(.055f, .088f, .108f, .97f);
        static readonly Color Amber = new Color(1f, .72f, .34f);
        static readonly Color Teal = new Color(.37f, .83f, .77f);
        static readonly Color Pale = new Color(.90f, .94f, .92f);
        static readonly Color Muted = new Color(.53f, .66f, .69f);
        static readonly Color Rule = new Color(.18f, .30f, .34f);
        static readonly Color Danger = new Color(.94f, .36f, .34f);
        static readonly Color DimIcon = new Color(.36f, .45f, .48f, .65f);
        Font font;
        Sprite rounded, vignette, solid;
        RectTransform canvasRect, hudRoot, menuRoot, motionRoot, titlePanel, statusPanel;
        RectTransform bossPanel, noticePanel, promptPanel, alarmPanel, mapPanel, mapRect, mapDotRect;
        Text healthText, ammoText, floorText, objectiveText, enemiesText, promptText, noticeText, inventoryText, bossText;
        Text survivorText, mapLabel, statusHeading, statusDetail, statusStats, statusPrimaryLabel;
        Image healthFill, reloadFill, bossFill, damageFlash, fuseIcon, keyIcon, menuShade, mapImage;
        // Preserve the menu references when Unity reloads scripts during a running session.
        [SerializeField, HideInInspector] Image[] classPanels = new Image[3];
        [SerializeField, HideInInspector] Image[] classStripes = new Image[3];
        [SerializeField, HideInInspector] Text[] classTitles = new Text[3];
        [SerializeField, HideInInspector] Text[] classStatus = new Text[3];
        GameObject reticle;
        RectTransform reticleRect;
        LevelBuilder shownMapLevel;
        GamePhase menuPhase;
        float flash, noticeUntil, refreshReady, shownReloadProgress = -1f;
        bool noticeVisible, shownReloading, haveMapPosition;
        Vector2 shownMapPosition, shownMousePosition = new Vector2(-9999, -9999);
        int shownHealth = -1, shownMaxHealth = -1, shownMagazine = -1, shownReserve = -1;
        int shownFloor = -1, shownEnemies = -1, shownInventory = -1, shownBossHealth = -1, shownBossMax = -1, shownClass = -1;
        string shownPrompt = "\0", shownRoom;

        void Awake()
        {
            try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch (System.ArgumentException) { font = Resources.GetBuiltinResource<Font>("Arial.ttf"); }
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            rounded = RoundedPanel();
            vignette = Vignette();
            solid = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f));
            var go = new GameObject("PlanB UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(transform, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100; canvas.pixelPerfect = true;
            var scale = go.GetComponent<CanvasScaler>();
            scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
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
            FullPanel("Facility edge shade", canvasRect, Color.white).GetComponent<Image>().sprite = vignette;
            hudRoot = FullRect("Static mission HUD", canvasRect);
            AddCanvas(hudRoot, 101);
            motionRoot = FullRect("Reticle and animated feedback", canvasRect);
            AddCanvas(motionRoot, 103);
            BuildHUD();
            damageFlash = FullPanel("Damage flash", motionRoot, new Color(.8f, .02f, .04f, 0)).GetComponent<Image>();
            damageFlash.enabled = false;
            reticleRect = Rect("Crosshair", motionRoot, 0, 0, 24, 24);
            reticleRect.anchorMin = reticleRect.anchorMax = reticleRect.pivot = new Vector2(.5f, .5f);
            reticleRect.sizeDelta = Vector2.zero;
            reticle = reticleRect.gameObject;
            Box(reticleRect, "Left", -12, -1, 7, 2, Amber, false);
            Box(reticleRect, "Right", 5, -1, 7, 2, Amber, false);
            Box(reticleRect, "Top", -1, -12, 2, 7, Amber, false);
            Box(reticleRect, "Bottom", -1, 5, 2, 7, Amber, false);
            menuRoot = FullRect("Facility terminal menus", canvasRect);
            AddCanvas(menuRoot, 110);
            menuRoot.gameObject.AddComponent<GraphicRaycaster>();
            menuShade = FullPanel("Menu shade", menuRoot, new Color(.012f, .028f, .042f, .45f)).GetComponent<Image>();
            BuildTitle();
            BuildStatus();
        }

        void BuildHUD()
        {
            var health = EdgeBox(hudRoot, "Vital signs", new Vector2(0, 1), 24, -24, 280, 108, Navy);
            Box(health, "Status accent", 0, 20, 3, 68, Teal, false);
            survivorText = Label(health, "FIGHTER // VITALS", 18, 13, 244, 24, 16, Muted);
            Icon(health, "medkit", 18, 40, 30, 32, Pale);
            healthText = Label(health, "120 / 120", 63, 34, 195, 43, 32, Pale);
            Box(health, "Health track", 18, 89, 244, 6, Rule, false);
            healthFill = Fill(health, "Health fill", 18, 89, 244, 6, Teal);

            var mission = EdgeBox(hudRoot, "Current mission", new Vector2(.5f, 1), 0, -24, 600, 86, Navy);
            Box(mission, "Mission marker", 18, 16, 6, 6, Amber);
            floorText = Label(mission, "SECTOR 01 // RESIDENTIAL", 34, 10, 548, 24, 16, Amber);
            objectiveText = Label(mission, "Find the fuse", 18, 36, 564, 40, 28, Pale);

            var ammo = EdgeBox(hudRoot, "Sidearm", new Vector2(1, 1), -24, -24, 280, 108, Navy);
            Label(ammo, "9MM // SIDEARM", 18, 13, 244, 24, 16, Muted);
            Icon(ammo, "ammo", 18, 39, 30, 35, Amber);
            ammoText = Label(ammo, "12 / 24", 63, 34, 199, 43, 32, Amber);
            enemiesText = Label(ammo, "HOSTILES  11", 18, 80, 244, 22, 16, Muted);
            Box(ammo, "Reload track", 18, 103, 244, 3, Rule, false);
            var ammoMotion = EdgeRect(motionRoot, "Reload animation", new Vector2(1, 1), -24, -24, 280, 108);
            reloadFill = Fill(ammoMotion, "Reload fill", 18, 103, 244, 3, Amber);
            reloadFill.enabled = false;

            var kit = EdgeRect(hudRoot, "Field kit", new Vector2(0, 1), 24, -142, 280, 36);
            var trap = Box(kit, "Trap counter", 0, 0, 100, 36, Navy);
            Icon(trap, "trap", 10, 7, 23, 23, Pale);
            inventoryText = Label(trap, "2 / 2", 41, 7, 54, 24, 16, Pale);
            var fuse = Box(kit, "Fuse status", 108, 0, 84, 36, Navy);
            fuseIcon = Icon(fuse, "fuse", 8, 7, 22, 22, DimIcon);
            Label(fuse, "FUSE", 35, 7, 43, 24, 14, Muted);
            var key = Box(kit, "Key status", 200, 0, 80, 36, Navy);
            keyIcon = Icon(key, "key", 8, 7, 22, 22, DimIcon);
            Label(key, "KEY", 35, 7, 39, 24, 14, Muted);

            alarmPanel = EdgeBox(hudRoot, "Security warning", new Vector2(.5f, 1), 0, -124, 340, 36, new Color(.23f, .07f, .075f, .96f));
            Label(alarmPanel, "SECURITY ALERT // TRACKED", 12, 7, 316, 25, 16, Danger, TextAnchor.MiddleCenter);
            alarmPanel.gameObject.SetActive(false);
            bossPanel = EdgeBox(hudRoot, "Warden telemetry", new Vector2(.5f, 1), 0, -174, 500, 58, Navy);
            bossText = Label(bossPanel, "THE WARDEN", 18, 10, 464, 24, 17, Danger);
            Box(bossPanel, "Boss track", 18, 42, 464, 6, new Color(.19f, .1f, .12f), false);
            bossFill = Fill(bossPanel, "Boss health", 18, 42, 464, 6, Danger);
            bossPanel.gameObject.SetActive(false);

            var controls = EdgeRect(hudRoot, "Control legend", new Vector2(0, 0), 24, 22, 1150, 25);
            Label(controls, "WASD  MOVE     MOUSE  FIRE     R  RELOAD     E  USE     SPACE / RMB  BASH     T  TRAP     ESC  PAUSE", 0, 0, 1150, 25, 14, Muted);
            noticePanel = EdgeBox(hudRoot, "Mission notification", new Vector2(.5f, 0), 0, 116, 800, 44, Navy);
            Box(noticePanel, "Notification accent", 0, 10, 3, 24, Amber, false);
            noticeText = Label(noticePanel, "", 18, 8, 764, 30, 20, Amber, TextAnchor.MiddleCenter);
            noticePanel.gameObject.SetActive(false);
            promptPanel = EdgeBox(hudRoot, "Interaction", new Vector2(.5f, 0), 0, 62, 600, 44, Navy);
            var keycap = Box(promptPanel, "Interact key", 9, 8, 30, 28, Rule);
            Label(keycap, "E", 0, 0, 30, 28, 19, Pale, TextAnchor.MiddleCenter);
            promptText = Label(promptPanel, "", 50, 7, 532, 31, 21, Pale, TextAnchor.MiddleCenter);
            promptPanel.gameObject.SetActive(false);

            mapPanel = EdgeBox(hudRoot, "Local floor plan", new Vector2(1, 0), -24, 62, 228, 170, Navy);
            mapLabel = Label(mapPanel, "SECTOR 01 // LOCAL MAP", 15, 10, 198, 22, 14, Teal);
            mapRect = Box(mapPanel, "Floor map", 15, 40, 198, 115.5f, new Color(.8f, .94f, .95f)).GetComponent<RectTransform>();
            mapImage = mapRect.GetComponent<Image>();
            mapImage.preserveAspect = true;
            var mapMotion = FullRect("Position marker canvas", mapRect);
            mapMotion.gameObject.AddComponent<Canvas>();
            mapDotRect = Box(mapMotion, "You are here", 0, 0, 8, 8, Amber);
            mapDotRect.anchorMin = mapDotRect.anchorMax = Vector2.zero;
            mapDotRect.pivot = new Vector2(.5f, .5f);
        }

        public void ShowMenu(GamePhase phase)
        {
            menuPhase = phase;
            bool visible = phase != GamePhase.Playing;
            bool hudVisible = phase == GamePhase.Playing || phase == GamePhase.Paused;
            menuRoot.gameObject.SetActive(visible);
            hudRoot.gameObject.SetActive(hudVisible);
            motionRoot.gameObject.SetActive(hudVisible);
            reticle.SetActive(phase == GamePhase.Playing);
            Cursor.visible = visible;
            if (!visible) { Refresh(); return; }
            bool title = phase == GamePhase.Title;
            titlePanel.gameObject.SetActive(title);
            statusPanel.gameObject.SetActive(!title);
            menuShade.color = new Color(.012f, .028f, .042f, title ? .42f : .72f);
            if (title) { UpdateClassSelection(); return; }
            var game = GameManager.Instance;
            statusHeading.text = phase == GamePhase.Paused ? "SHIFT PAUSED" : phase == GamePhase.Won ? "SURVIVOR RECOVERED" : "SIGNAL LOST";
            statusHeading.color = phase == GamePhase.Won ? Teal : phase == GamePhase.Lost ? Danger : Pale;
            statusDetail.text = phase == GamePhase.Paused ? "Catch your breath. The facility can wait."
                : phase == GamePhase.Won ? "Power restored. Containment breached.\nYou made it back to the surface."
                : "The facility claimed another shift.\nRegroup, choose your survivor, and try again.";
            statusStats.text = phase == GamePhase.Paused ? "ESC returns to the facility.\nYour progress is held here."
                : string.Format("SHIFT  {0:00}:{1:00}     /     HOSTILES  {2}\nSALVAGE  {3} SCRAP  /  {4} BATTERIES  /  {5} WIRE", (int)game.RunTime / 60, (int)game.RunTime % 60, game.Kills, game.Scrap, game.Battery, game.Wire);
            statusPrimaryLabel.text = phase == GamePhase.Paused ? "RESUME SHIFT" : "TRY AGAIN";
        }

        void BuildTitle()
        {
            titlePanel = CenterBox(menuRoot, "Survivor terminal", -260, 0, 760, 726, Navy);
            Box(titlePanel, "Terminal accent", 0, 24, 4, 82, Teal, false);
            Label(titlePanel, "FIELD TERMINAL // FACILITY SURVIVAL", 32, 24, 696, 25, 16, Teal);
            var title = Label(titlePanel, "PLAN B", 28, 65, 696, 88, 72, Pale); title.fontStyle = FontStyle.Bold;
            Label(titlePanel, "THE LAST SHIFT", 32, 152, 696, 32, 24, Amber);
            Label(titlePanel, "Two floors underground.\nRestore power. Breach containment. Get out.", 32, 207, 696, 66, 23, Pale);
            MissionStep(titlePanel, "01", "RESTORE\nTHE POWER", 32);
            MissionStep(titlePanel, "02", "BREACH\nCONTAINMENT", 270);
            MissionStep(titlePanel, "03", "DEFEAT THE WARDEN\nAND ESCAPE", 508);
            Box(titlePanel, "Section divider", 32, 375, 696, 1, Rule, false);
            Label(titlePanel, "CHOOSE YOUR SURVIVOR", 32, 394, 696, 25, 16, Muted);
            ClassCard(titlePanel, CharacterClass.Fighter, "FIGHTER", "120 HP\nDOUBLE BASH DAMAGE", 32);
            ClassCard(titlePanel, CharacterClass.Engineer, "ENGINEER", "100 HP\nFAST RELOAD + AMMO", 270);
            ClassCard(titlePanel, CharacterClass.Scout, "SCOUT", "90 HP\n25% FASTER MOVEMENT", 508);
            Button(titlePanel, "ENTER THE FACILITY", 32, 619, 500, 57, () => GameManager.Instance.StartRun(), true);
            Button(titlePanel, "QUIT", 548, 619, 180, 57, () => GameManager.Instance.Quit(), false);
            Label(titlePanel, "WASD move  /  Mouse aim + fire  /  E interact  /  ESC pause", 32, 692, 696, 23, 15, Muted);
        }

        void MissionStep(RectTransform parent, string number, string title, float x)
        {
            var step = Box(parent, "Mission " + number, x, 300, 220, 58, Panel);
            Label(step, number, 12, 14, 33, 31, 22, Amber);
            Label(step, title, 55, 10, 154, 42, 14, Muted);
        }

        void ClassCard(RectTransform parent, CharacterClass choice, string title, string detail, float x)
        {
            int index = (int)choice;
            var card = Box(parent, title, x, 435, 220, 161, Panel);
            classPanels[index] = card.GetComponent<Image>();
            ConfigureButton(classPanels[index], () => { GameManager.Instance.SelectedClass = choice; UpdateClassSelection(); });
            string sprite = choice == CharacterClass.Fighter ? "fighter" : choice == CharacterClass.Engineer ? "engineer" : "scout";
            Icon(card, sprite, 15, 15, 56, 76, Pale, 1);
            classTitles[index] = Label(card, title, 83, 21, 126, 29, 20, Pale);
            classStatus[index] = Label(card, "SELECT", 83, 59, 126, 22, 13, Muted);
            Label(card, detail, 15, 106, 195, 44, 14, Muted);
            classStripes[index] = Box(card, "Selection indicator", 14, 157, 192, 3, Rule, false).GetComponent<Image>();
        }

        void UpdateClassSelection()
        {
            int selected = (int)GameManager.Instance.SelectedClass;
            for (int i = 0; i < 3; i++)
            {
                bool active = i == selected;
                classPanels[i].color = active ? new Color(.09f, .20f, .22f) : Panel;
                classTitles[i].color = active ? Teal : Pale;
                classStatus[i].text = active ? "SELECTED" : "SELECT";
                classStatus[i].color = active ? Teal : Muted;
                classStripes[i].color = active ? Teal : Rule;
            }
        }

        void BuildStatus()
        {
            statusPanel = CenterBox(menuRoot, "Shift report", 0, 0, 660, 508, Navy);
            Box(statusPanel, "Report accent", 28, 0, 604, 3, Teal, false);
            Label(statusPanel, "PLAN B // THE LAST SHIFT", 32, 31, 596, 28, 16, Amber, TextAnchor.MiddleCenter);
            statusHeading = Label(statusPanel, "SHIFT PAUSED", 22, 86, 616, 61, 36, Pale, TextAnchor.MiddleCenter);
            statusDetail = Label(statusPanel, "", 42, 165, 576, 72, 22, Muted, TextAnchor.MiddleCenter);
            statusStats = Label(statusPanel, "", 32, 257, 596, 66, 18, Teal, TextAnchor.MiddleCenter);
            statusPrimaryLabel = Button(statusPanel, "RESUME SHIFT", 56, 352, 548, 56, HandlePrimary, true);
            Button(statusPanel, "CHANGE SURVIVOR", 56, 426, 548, 47, () => GameManager.Instance.ShowTitle(), false);
        }

        void HandlePrimary()
        {
            var game = GameManager.Instance;
            if (menuPhase == GamePhase.Paused) game.SetPaused(false);
            else game.StartRun();
        }

        void Update()
        {
            var game = GameManager.Instance;
            bool playing = game != null && game.IsPlaying;
            if (hudRoot.gameObject.activeSelf && Time.unscaledTime >= refreshReady) Refresh();
            if (noticeVisible && Time.unscaledTime >= noticeUntil)
            { noticeVisible = false; noticePanel.gameObject.SetActive(false); }
            if (flash > 0f)
            {
                flash = Mathf.MoveTowards(flash, 0, Time.unscaledDeltaTime * .85f);
                damageFlash.color = new Color(.85f, .025f, .04f, flash);
                if (flash <= 0f) damageFlash.enabled = false;
            }
            if (reticle.activeSelf != playing) reticle.SetActive(playing);
            Vector2 mouse = InputBridge.MousePosition;
            if (playing && mouse != shownMousePosition)
            {
                shownMousePosition = mouse;
                Vector2 local;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mouse, null, out local);
                reticleRect.anchoredPosition = local;
            }
            if (game != null && game.Player != null && game.Player.Weapon != null)
            {
                float progress = game.Player.Weapon.ReloadProgress;
                if (Mathf.Abs(progress - shownReloadProgress) > .0005f)
                {
                    shownReloadProgress = progress;
                    reloadFill.fillAmount = progress;
                    reloadFill.enabled = progress > 0;
                }
            }
        }

        public void Refresh()
        {
            var game = GameManager.Instance;
            if (game == null || game.Player == null || healthText == null) return;
            refreshReady = Time.unscaledTime + .1f;
            var player = game.Player;
            if (player.Health != shownHealth || player.MaxHealth != shownMaxHealth)
            {
                shownHealth = player.Health; shownMaxHealth = player.MaxHealth;
                healthText.text = shownHealth + " / " + shownMaxHealth;
                float health = shownHealth / (float)Mathf.Max(1, shownMaxHealth);
                healthFill.fillAmount = health;
                healthFill.color = health <= .3f ? Danger : health <= .55f ? Amber : Teal;
            }
            int role = (int)game.SelectedClass;
            if (role != shownClass)
            {
                shownClass = role;
                survivorText.text = role == 0 ? "FIGHTER // VITALS" : role == 1 ? "ENGINEER // VITALS" : "SCOUT // VITALS";
            }
            var weapon = player.Weapon;
            if (weapon != null && (weapon.Magazine != shownMagazine || weapon.Reserve != shownReserve || weapon.Reloading != shownReloading))
            {
                shownMagazine = weapon.Magazine; shownReserve = weapon.Reserve; shownReloading = weapon.Reloading;
                ammoText.text = shownReloading ? "RELOADING..." : shownMagazine.ToString("00") + " / " + shownReserve.ToString("00");
                ammoText.fontSize = shownReloading ? 23 : 32;
                ammoText.color = !shownReloading && shownMagazine == 0 ? Danger : Amber;
            }
            if (shownFloor != game.Floors.CurrentFloor)
            {
                shownFloor = game.Floors.CurrentFloor;
                floorText.text = shownFloor == 1 ? "SECTOR 01 // RESIDENTIAL + MAINTENANCE" : "SECTOR 02 // RESEARCH + SECURITY";
                mapLabel.text = shownFloor == 1 ? "SECTOR 01 // LOCAL MAP" : "SECTOR 02 // LOCAL MAP";
            }
            string objective = game.Objective;
            if (objectiveText.text != objective) objectiveText.text = objective;
            int count = game.Floors.EnemiesRemaining;
            if (count != shownEnemies) { shownEnemies = count; enemiesText.text = "HOSTILES  " + count.ToString("00"); }
            var nearby = player.Nearby;
            string prompt = nearby != null && nearby.isActiveAndEnabled && nearby.Available ? nearby.Prompt : null;
            if (shownPrompt != prompt)
            {
                shownPrompt = prompt;
                promptText.text = prompt != null ? prompt.Replace("[E]", "").Trim() : "";
                promptPanel.gameObject.SetActive(prompt != null);
            }
            int inventory = (game.HasFuse ? 1 : 0) | (game.HasKey ? 2 : 0) | (Time.time < game.AlarmUntil ? 4 : 0) | (game.TrapCount << 3);
            if (inventory != shownInventory)
            {
                shownInventory = inventory;
                inventoryText.text = game.TrapCount + " / 2";
                fuseIcon.color = game.HasFuse ? Amber : DimIcon;
                keyIcon.color = game.HasKey ? Amber : DimIcon;
                alarmPanel.gameObject.SetActive((inventory & 4) != 0);
            }
            Boss boss = game.Floors.ActiveBoss;
            bool showBoss = boss != null && !boss.IsDead && game.DoorUnlocked;
            if (bossPanel.gameObject.activeSelf != showBoss) bossPanel.gameObject.SetActive(showBoss);
            if (boss != null && (boss.Health != shownBossHealth || boss.MaxHealth != shownBossMax))
            {
                shownBossHealth = boss.Health; shownBossMax = boss.MaxHealth;
                bossText.text = "THE WARDEN  //  " + boss.Health + " HP";
                bossFill.fillAmount = boss.Health / (float)Mathf.Max(1, boss.MaxHealth);
            }
            LevelBuilder level = game.Floors.ActiveLevel;
            if (level != shownMapLevel || (mapImage.sprite == null && level != null && level.MapPreview != null))
            {
                shownMapLevel = level;
                mapImage.sprite = level != null ? level.MapPreview : null;
                mapPanel.gameObject.SetActive(mapImage.sprite != null);
                haveMapPosition = false;
            }
            Vector2 position = player.transform.position;
            string room = level != null ? level.RoomAt(position) : null;
            if (room != shownRoom)
            {
                shownRoom = room;
                mapLabel.text = room != null ? "LOCAL // " + room : "LOCAL MAP";
            }
            Vector2 normalized = new Vector2(Mathf.Clamp01((position.x + 24.5f) / 48f), Mathf.Clamp01((position.y + 13.5f) / 28f));
            if (!haveMapPosition || (normalized - shownMapPosition).sqrMagnitude > .000001f)
            {
                shownMapPosition = normalized; haveMapPosition = true;
                mapDotRect.anchoredPosition = new Vector2(normalized.x * 198f, normalized.y * 115.5f);
            }
        }

        public void Notify(string message, float duration = 3)
        {
            noticeUntil = Time.unscaledTime + duration;
            noticeVisible = !string.IsNullOrEmpty(message);
            noticeText.text = message;
            noticePanel.gameObject.SetActive(noticeVisible);
        }
        public void Flash() { flash = .30f; damageFlash.enabled = true; }

        void AddCanvas(RectTransform rect, int sorting)
        {
            var canvas = rect.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true; canvas.sortingOrder = sorting; canvas.pixelPerfect = true;
        }
        RectTransform FullRect(string name, Transform parent)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent, false);
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; return r;
        }
        RectTransform FullPanel(string name, Transform parent, Color color)
        {
            var r = FullRect(name, parent); var image = r.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return r;
        }
        RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent, false); r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(width, height); return r;
        }
        RectTransform Box(Transform parent, string name, float x, float y, float width, float height, Color color, bool smooth = true)
        {
            var r = Rect(name, parent, x, y, width, height);
            var image = r.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false;
            if (smooth) { image.sprite = rounded; image.type = Image.Type.Sliced; }
            return r;
        }
        RectTransform EdgeRect(Transform parent, string name, Vector2 anchor, float x, float y, float width, float height)
        {
            var r = Rect(name, parent, 0, 0, width, height);
            r.anchorMin = r.anchorMax = anchor; r.pivot = anchor;
            r.anchoredPosition = new Vector2(x, y); return r;
        }
        RectTransform EdgeBox(Transform parent, string name, Vector2 anchor, float x, float y, float width, float height, Color color)
        {
            var r = Box(parent, name, 0, 0, width, height, color);
            r.anchorMin = r.anchorMax = anchor; r.pivot = anchor;
            r.anchoredPosition = new Vector2(x, y); return r;
        }
        RectTransform CenterBox(Transform parent, string name, float x, float y, float width, float height, Color color)
        {
            return EdgeBox(parent, name, new Vector2(.5f, .5f), x, y, width, height, color);
        }
        Image Fill(Transform parent, string name, float x, float y, float width, float height, Color color)
        {
            var image = Box(parent, name, x, y, width, height, color, false).GetComponent<Image>();
            image.sprite = solid;
            image.type = Image.Type.Filled; image.fillMethod = Image.FillMethod.Horizontal; image.fillOrigin = 0; image.fillAmount = 1;
            return image;
        }
        Image Icon(Transform parent, string key, float x, float y, float width, float height, Color color, int frame = 0)
        {
            var image = Box(parent, key + " icon", x, y, width, height, color, false).GetComponent<Image>();
            image.sprite = SpriteLibrary.Get(key, frame, 0); image.preserveAspect = true; return image;
        }
        Text Label(Transform parent, string value, float x, float y, float width, float height, int size, Color color, TextAnchor align = TextAnchor.UpperLeft)
        {
            var r = Rect("Label", parent, x, y, width, height);
            var text = r.gameObject.AddComponent<Text>(); text.font = font; text.text = value; text.fontSize = size; text.color = color;
            text.alignment = align; text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false; text.supportRichText = false; return text;
        }
        Text Button(Transform parent, string title, float x, float y, float width, float height, UnityEngine.Events.UnityAction click, bool primary)
        {
            var r = Box(parent, title, x, y, width, height, primary ? Amber : Rule);
            ConfigureButton(r.GetComponent<Image>(), click);
            return Label(r, title, 10, 0, width - 20, height, 18, primary ? Navy : Pale, TextAnchor.MiddleCenter);
        }
        void ConfigureButton(Image image, UnityEngine.Events.UnityAction click)
        {
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            image.gameObject.AddComponent<UISound>();
            var colors = button.colors;
            colors.normalColor = Color.white; colors.highlightedColor = new Color(.86f, 1f, .96f);
            colors.selectedColor = new Color(.86f, 1f, .96f); colors.pressedColor = new Color(.70f, .79f, .80f); colors.fadeDuration = .06f;
            button.colors = colors;
            button.onClick.AddListener(() => AudioFactory.Play("ui_click", .45f));
            button.onClick.AddListener(click);
        }

        static Sprite RoundedPanel()
        {
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "PlanB UI rounded panel"; texture.filterMode = FilterMode.Bilinear; texture.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float qx = Mathf.Max(0, Mathf.Abs(x - 15.5f) - 7.5f), qy = Mathf.Max(0, Mathf.Abs(y - 15.5f) - 7.5f);
                float alpha = Mathf.Clamp01(8.5f - Mathf.Sqrt(qx * qx + qy * qy));
                pixels[y * size + x] = new Color(1, 1, 1, alpha);
            }
            texture.SetPixels(pixels); texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(8, 8, 8, 8));
        }
        static Sprite Vignette()
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "PlanB UI edge shade"; texture.filterMode = FilterMode.Bilinear; texture.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float distance = new Vector2((x - 63.5f) / 63.5f, (y - 63.5f) / 63.5f).magnitude;
                pixels[y * size + x] = new Color(.012f, .025f, .04f, Mathf.SmoothStep(0, .32f, Mathf.InverseLerp(.45f, 1.28f, distance)));
            }
            texture.SetPixels(pixels); texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f));
        }
        void OnDestroy()
        {
            if (rounded != null) { Destroy(rounded.texture); Destroy(rounded); }
            if (vignette != null) { Destroy(vignette.texture); Destroy(vignette); }
            if (solid != null) Destroy(solid);
        }
    }
}
