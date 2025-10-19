using System.Reflection;
using UnityEngine;
using HarmonyLib;
using MelonLoader;
using MelonLoader.Utils;
using Newtonsoft.Json;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Economy;  
using Il2CppScheduleOne.Money;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Quests;





[assembly: MelonInfo(typeof(ScheduleITeleportMenu.Main), "Teleport Menu", "2.1.0", "MrTibbz")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace ScheduleITeleportMenu
{
    public class Main : MelonMod
    {
        private string waitingForKey = null;
        private bool isMenuOpen = false;
        private Rect windowRect = new Rect(200, 100, 400, 500);
        private Vector2 scrollPosition;
        private bool isResizing = false;
        private Vector2 resizeStartMousePosition;
        private Vector2 resizeStartWindowSize;
        private bool isDragging = false;
        private Vector2 dragOffset;
        float x = 15f;
        private MenuState currentMenu = MenuState.Main;
        private string currentCategory = "";
        private string newLocationName = "";
        private string combinedSavePath;
        public static Main Instance;
        private SerializableVector3 safetyTeleportPosition = new SerializableVector3(Vector3.zero);
        private List<TeleportLocation> savedLocations = new List<TeleportLocation>();
        private List<TeleportLocation> favoriteLocations = new List<TeleportLocation>();
        private List<TeleportLocation> allLocations = new List<TeleportLocation>();
        private ModSettings settings = new ModSettings();


        

        private void TeleportToLocation(TeleportLocation location)
        {
            if (Player.Local != null)
            {
                Player.Local.transform.position = location.Position.ToVector3();
                MelonLogger.Msg($"Teleported to {location.Name} ({location.Position.x}, {location.Position.y}, {location.Position.z})");
            }
            if (settings.chargeForTeleport)
            {
                var moneyManager = NetworkSingleton<MoneyManager>.Instance;
                int cost = settings.teleportCost;

                if (moneyManager != null)
                {
                    if (moneyManager.cashBalance >= cost) // check current balance
                    {

                        moneyManager.ChangeCashBalance (-cost); // deduct cost
                        MelonLogger.Msg($"[Teleport Menu] Charged ${cost} for teleport. Remaining: ${moneyManager.cashBalance}");
                    }
                    else
                    {
                        MelonLogger.Msg($"[Teleport Menu] Not enough money! Requires ${cost}.");
                        return; // cancel teleport
                    }
                }
            }

        }
        #region Serializable
        [Serializable]
        public class TeleportLocation
        {
            public string Name;
            public SerializableVector3 Position;
            public bool IsFavorite = false;
            public string Category;
        }
        [Serializable]
        public class SerializableVector3
        {
            public float x;
            public float y;
            public float z;
            public SerializableVector3(Vector3 v)
            {
                x = v.x;
                y = v.y;
                z = v.z;
            }
            public Vector3 ToVector3()
            {
                return new Vector3(x, y, z);
            }
        }
        [Serializable]
        private class CombinedData
        {
            public List<TeleportLocation> SavedLocations = new List<TeleportLocation>();
            public List<TeleportLocation> FavoriteLocations = new List<TeleportLocation>();
            public SerializableVector3 SafetyTeleportPosition = new SerializableVector3(Vector3.zero);
            public ModSettings Settings = new ModSettings();
        }
        #endregion

        public override void OnInitializeMelon()
        {
            combinedSavePath = Path.Combine(MelonEnvironment.UserDataDirectory, "TeleportMenu/AllData.json");
            LoadAllData();
            InitializeAllLocations();
            Instance = this;
            var harmony = new HarmonyLib.Harmony("TeleportMenuPatches");
            harmony.PatchAll();


            foreach (var saved in savedLocations)
            {
                if (!allLocations.Any(l => l.Name == saved.Name && l.Category == saved.Category))
                {
                    allLocations.Add(saved);
                }
            }
            if (!allLocations.Any(l => l.Category == "Custom Teleports"))
            {
                allLocations.Add(new TeleportLocation
                {
                    Name = "", // Empty placeholder
                    Position = new SerializableVector3(Vector3.zero),
                    Category = "Custom Teleports"
                });
            }
            foreach (var fav in favoriteLocations)
            {
                var loc = allLocations.FirstOrDefault(l => l.Name == fav.Name && l.Category == fav.Category);
                if (loc != null) loc.IsFavorite = true;
            }
            MelonLogger.Msg("Initialization complete.");
        }

        public override void OnUpdate()
        {
            if (Input.GetKeyDown(settings.menuKey))
            {
                isMenuOpen = !isMenuOpen;

                if (isMenuOpen)
                {
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                    // Disable core player scripts
                    if (PlayerMovement.Instance != null) PlayerMovement.Instance.enabled = false;
                    if (PlayerCamera.Instance != null) PlayerCamera.Instance.enabled = false;

                }
                else
                {
                    Cursor.visible = false;
                    Cursor.lockState = CursorLockMode.Locked;
                    // Re-enable player scripts
                    if (PlayerMovement.Instance != null) PlayerMovement.Instance.enabled = true;
                    if (PlayerCamera.Instance != null) PlayerCamera.Instance.enabled = true;
                }
            }
            if (isResizing && Input.GetMouseButton(0))
            {
                Vector2 delta = (Vector2)Input.mousePosition - resizeStartMousePosition;
                windowRect.width = Mathf.Max(200, resizeStartWindowSize.x + delta.x);
                windowRect.height = Mathf.Max(200, resizeStartWindowSize.y - delta.y);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                isResizing = false;
            }
            if (isDragging && Input.GetMouseButton(0))
            {
                windowRect.position = (Vector2)Input.mousePosition - dragOffset;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                isDragging = false;
            }
            HandleTyping();
            if (Input.GetKeyDown(settings.safetyTeleportKey))
            {
                TeleportToLocation(new TeleportLocation
                {
                    Name = "Safety Teleport",
                    Position = safetyTeleportPosition,
                    Category = "Custom Teleports"
                });
            }
        }

        #region UI
        public override void OnGUI()
        {
            // Handle right-click for Back navigation
            if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
            {
                if (currentMenu == MenuState.Favorites ||
                    currentMenu == MenuState.Settings ||
                    currentMenu == MenuState.SubMenu)
                {
                    currentMenu = MenuState.Main;
                    scrollPosition = Vector2.zero; // reset scrollbar
                    Event.current.Use(); // consume so it doesn’t trigger button clicks
                }
                else if (currentMenu == MenuState.Main)
                {
                    // close menu if on main
                    //isMenuOpen = false;
                   // Event.current.Use();
                }
            }

            // === Handle key rebinding ===
            if (waitingForKey != null && Event.current.type == EventType.KeyDown)
            {
                KeyCode newKey = Event.current.keyCode;
                if (newKey != KeyCode.None)
                {
                    if (waitingForKey == "menuKey")
                    {
                        settings.menuKey = newKey;
                        MelonLogger.Msg($"[Teleport Menu] Menu key set to {newKey}");
                    }
                    else if (waitingForKey == "safetyTeleportKey")
                    {
                        settings.safetyTeleportKey = newKey;
                        MelonLogger.Msg($"[Teleport Menu] Safety teleport key set to {newKey}");
                    }

                    waitingForKey = null;
                    SaveAllData();
                    Event.current.Use();
                }
            }


            if (isMenuOpen)
            {
                // Block keyboard + mouse input from reaching the game
                if (Event.current.isKey || Event.current.isMouse)
                {
                    Event.current.Use();
                }
            }
            if (isMenuOpen)
            {
                GUI.backgroundColor = new Color(settings.backgroundR / 255f, settings.backgroundG / 255f, settings.backgroundB / 255f, settings.backgroundA / 255f);
                windowRect = GUI.Window(0, windowRect, (GUI.WindowFunction)DrawWindow, "");
                if (Event.current.type == EventType.MouseDown && new Rect(windowRect.x, windowRect.y, windowRect.width, 20).Contains(Event.current.mousePosition))
                {
                    isDragging = true;
                    dragOffset = (Vector2)Input.mousePosition - windowRect.position;
                    Event.current.Use();
                }
            }
        }

        private void HandleTyping()
        {
            if (!isMenuOpen) return;

            foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(key))
                {
                    if (key == KeyCode.Backspace)
                    {
                        if (newLocationName.Length > 0)
                            newLocationName = newLocationName.Substring(0, newLocationName.Length - 1);
                    }
                    else if (key == KeyCode.Return || key == KeyCode.KeypadEnter)
                    {

                    }
                    else
                    {
                        string keyStr = KeyCodeToString(key);
                        if (!string.IsNullOrEmpty(keyStr))
                            newLocationName += keyStr;
                    }
                }
            }
        }
        private string KeyCodeToString(KeyCode key)
        {
            if (key >= KeyCode.A && key <= KeyCode.Z)
            {
                string letter = key.ToString();
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    return letter.ToUpper();
                return letter.ToLower();
            }
            if (key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9)
                return ((int)(key - KeyCode.Alpha0)).ToString();
            if (key == KeyCode.Space)
                return " ";
            if (key == KeyCode.Minus)
                return "-";
            if (key == KeyCode.Period)
                return ".";
            return null;
        }

        private GUIStyle GetLabelStyle()
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = settings.fontSize,
                fontStyle = settings.bold ? FontStyle.Bold : FontStyle.Normal,
                normal = { textColor = new Color(settings.textColorR / 255f, settings.textColorG / 255f, settings.textColorB / 255f)},
            };
        }

        private GUIStyle GetButtonStyle()
        {
            GUIStyle style = new GUIStyle(GUI.skin.button)
            {
                fontSize = settings.fontSize,
                fontStyle = settings.bold ? FontStyle.Bold : FontStyle.Normal,
                normal = { textColor = new Color(settings.textColorR / 255f, settings.textColorG / 255f, settings.textColorB / 255f)},
                hover = { textColor = new Color(settings.buttonHoverR / 255f, settings.buttonHoverG / 255f, settings.buttonHoverB / 255f)},
            };
            return style;
        }

        private GUIStyle GetCenteredLabelStyle()
        {
            GUIStyle style = new GUIStyle(GetLabelStyle());
            style.alignment = TextAnchor.MiddleCenter;
            return style;
        }

        private void DrawWindow(int windowID)
        {
            GUI.backgroundColor = new Color(settings.accentBarR / 255f, settings.accentBarG / 255f, settings.accentBarB / 255f, settings.accentBarA / 255f);
            GUI.Box(new Rect(0, 0, windowRect.width, 20), "Teleport Menu", new GUIStyle()
            {
                fontSize = settings.titleFontSize,
                fontStyle = settings.bold ? FontStyle.Bold : FontStyle.Normal,
                normal = new GUIStyleState() { textColor = new Color(settings.titleTextColorR / 255f, settings.titleTextColorG / 255f, settings.titleTextColorB / 255f)},
                alignment = TextAnchor.MiddleCenter
            });
            float totalContentHeight = 1300;
            scrollPosition = GUI.BeginScrollView(new Rect(0, 20, windowRect.width, windowRect.height - 20), scrollPosition, new Rect(0, 0, windowRect.width - 20, totalContentHeight));

            GUIStyle buttonStyle = GetButtonStyle();
            GUIStyle labelStyle = GetLabelStyle();

            switch (currentMenu)
            {
                case MenuState.Main:
                    DrawMainMenu(buttonStyle);
                    break;
                case MenuState.SubMenu:
                    DrawLocationSubMenu(buttonStyle);
                    break;
                case MenuState.Favorites:
                    DrawFavoritesMenu(buttonStyle);
                    break;
                case MenuState.Settings:
                    DrawSettingsMenu(buttonStyle, labelStyle);
                    break;
            }
            GUI.EndScrollView();
            GUI.DragWindow(new Rect(0, 0, windowRect.width - 20, 20));
        }
        #endregion

        #region Main Menu
        private void DrawMainMenu(GUIStyle buttonStyle)
        {
            float y = 20f;
            if (GUI.Button(new Rect(x, y, windowRect.width - 40, 40), "Favorites", buttonStyle))
            {
                currentMenu = MenuState.Favorites;
                scrollPosition = Vector2.zero; // Reset scroll bar when entering submenu
            }
            y += 45f;
            foreach (var category in allLocations.Select(l => l.Category).Distinct().Where(c => c != "Custom Teleports"))
            {
                if (GUI.Button(new Rect(x, y, windowRect.width - 40, 40), category, buttonStyle))
                {
                    currentCategory = category;
                    currentMenu = MenuState.SubMenu;
                    scrollPosition = Vector2.zero; // Reset scroll bar when entering submenu
                }
                y += 45f;
            }
            if (GUI.Button(new Rect(x, y, windowRect.width - 40, 40), "Custom Teleports", buttonStyle))
            {
                currentCategory = "Custom Teleports";
                currentMenu = MenuState.SubMenu;
                scrollPosition = Vector2.zero; // Reset scroll bar when entering submenu
            }
            y += 45f;
            if (GUI.Button(new Rect(x, y, windowRect.width - 40, 40), "Settings", buttonStyle))
            {
                currentMenu = MenuState.Settings;
                scrollPosition = Vector2.zero; // Reset scroll bar when entering submenu
            }
        }
        private enum MenuState
        {
            Main,
            SubMenu,
            Favorites,
            Settings
        }
        #endregion

        #region Favorites Menu
        private void DrawFavoritesMenu(GUIStyle buttonStyle)
        {
            float y = 20f;
            float teleportButtonWidth = windowRect.width - 87;
            // Get ordered categories
            List<string> categoryOrder = GetMenuCategoryOrder();

            // Group favorites by category in menu order
            var orderedFavorites = favoriteLocations
                .OrderBy(f => categoryOrder.IndexOf(f.Category))
                //.ThenBy(f => f.Name) // optional: alphabetical within category
                .ToList();
            string currentCategoryHeader = "";
            foreach (var loc in orderedFavorites)
            {
                // Draw category header when it changes
                if (loc.Category != currentCategoryHeader)
                {
                    currentCategoryHeader = loc.Category;
                    GUI.Label(new Rect(x, y, windowRect.width - 40, 25), $"{currentCategoryHeader}", GetCenteredLabelStyle());
                    y += 30f;
                }
                // Draw teleport button
                if (GUI.Button(new Rect(x, y, teleportButtonWidth, 40), loc.Name, buttonStyle))
                    TeleportToLocation(loc);
                // Delete from favorites button
                if (GUI.Button(new Rect(x + teleportButtonWidth + 5, y, 40, 40), "X", buttonStyle))
                {
                    loc.IsFavorite = false;
                    favoriteLocations.RemoveAll(f => f.Name == loc.Name && f.Category == loc.Category);
                    SaveAllData();
                    break;
                }
                y += 45f;
            }
            if (GUI.Button(new Rect(x, y, windowRect.width - 40, 40), "Back", buttonStyle))
            {
                currentMenu = MenuState.Main;
                scrollPosition = Vector2.zero; // Reset scroll bar
            }
        }
        #endregion

        #region Dynamic Submenu
        private void DrawLocationSubMenu(GUIStyle buttonStyle)
        {
            float y = 20f;
            float favButtonWidth = 40;
            float deleteButtonWidth = 40;
            var locationsInCategory = allLocations
                .Where(l => l.Category == currentCategory && (!string.IsNullOrEmpty(l.Name) || currentCategory != "Custom Teleports"))
                .ToList();
            if (currentCategory == "Custom Teleports")
            {
                GUI.Label(new Rect(20, y, windowRect.width - 40, 30), "Enter Name: " + newLocationName, GetLabelStyle());
                y += 35f;
                if (GUI.Button(new Rect(x, y, windowRect.width - 40, 40), "Save Current Location", buttonStyle))
                {
                    SaveCurrentLocation();
                }
                y += 45f;

                // Safety Teleport button
                if (GUI.Button(new Rect(x, y, windowRect.width - 40, 40), "Set Safety Teleport", buttonStyle))
                {
                    safetyTeleportPosition = new SerializableVector3(Player.Local.transform.position);
                    SaveAllData();
                    MelonLogger.Msg($"Safety Teleport location saved at ({safetyTeleportPosition.x}, {safetyTeleportPosition.y}, {safetyTeleportPosition.z})");
                }
                y += 45f;
            }
            foreach (var loc in locationsInCategory)
            {
                float teleportButtonWidth = windowRect.width - 87; // Default width
                if (currentCategory == "Custom Teleports")
                    teleportButtonWidth = windowRect.width - 131; // Saved Locations have fav + delete
                string buttonText = $"{loc.Name}";
                if (string.IsNullOrEmpty(loc.Name)) buttonText = "Unnamed Location";
                if (GUI.Button(new Rect(x, y, teleportButtonWidth, 40), buttonText, buttonStyle))
                    TeleportToLocation(loc);
                string favLabel = loc.IsFavorite ? "★" : "☆";
                GUIStyle favStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = settings.fontSize + 2,
                    normal = { textColor = loc.IsFavorite ? Color.yellow : Color.white },
                    hover = { textColor = new Color(settings.buttonHoverR, settings.buttonHoverG, settings.buttonHoverB) },
                    alignment = TextAnchor.MiddleCenter
                };
                if (GUI.Button(new Rect(x + teleportButtonWidth + 5, y, favButtonWidth, 40), favLabel, favStyle))
                {
                    loc.IsFavorite = !loc.IsFavorite;
                    if (loc.IsFavorite)
                    {
                        if (!favoriteLocations.Any(f => f.Name == loc.Name && f.Category == loc.Category))
                            favoriteLocations.Add(loc);
                    }
                    else
                    {
                        favoriteLocations.RemoveAll(f => f.Name == loc.Name && f.Category == loc.Category);
                    }
                    SaveAllData();
                }
                // Delete button only for Saved Locations
                if (currentCategory == "Custom Teleports")
                {
                    if (GUI.Button(new Rect(x + teleportButtonWidth + 5 + favButtonWidth + 5, y, deleteButtonWidth, 40), "X", buttonStyle))
                    {
                        savedLocations.RemoveAll(l => l.Name == loc.Name && l.Category == loc.Category);
                        allLocations.RemoveAll(l => l.Name == loc.Name && l.Category == loc.Category);
                        favoriteLocations.RemoveAll(f => f.Name == loc.Name && f.Category == loc.Category);
                        SaveAllData();
                        break; // Prevent enumeration issues
                    }
                }
                y += 45f;
            }
            if (GUI.Button(new Rect(x, y, windowRect.width - 40, 40), "Back", buttonStyle))
            {
                currentMenu = MenuState.Main;
                scrollPosition = Vector2.zero; // Reset scroll bar
            }
        }
        private void InitializeAllLocations()
        {
            allLocations = new List<TeleportLocation>();
            // -------------------- Properties --------------------
            allLocations.Add(new TeleportLocation { Name = "RV", Position = new SerializableVector3(new Vector3(14f, 0.9f, -77f)), Category = "Properties" });
            allLocations.Add(new TeleportLocation { Name = "Motel", Position = new SerializableVector3(new Vector3(-66f, 1.6f, 83f)), Category = "Properties" });
            allLocations.Add(new TeleportLocation { Name = "Sweatshop", Position = new SerializableVector3(new Vector3(-64f, 0.4f, 142f)), Category = "Properties" });
            allLocations.Add(new TeleportLocation { Name = "Storage Unit", Position = new SerializableVector3(new Vector3(-5.1f, 1f, 103f)), Category = "Properties" });
            allLocations.Add(new TeleportLocation { Name = "Bungalow", Position = new SerializableVector3(new Vector3(-168f, -2.7f, 114f)), Category = "Properties" });
            allLocations.Add(new TeleportLocation { Name = "Barn", Position = new SerializableVector3(new Vector3(181f, 1f, -14f)), Category = "Properties" });
            allLocations.Add(new TeleportLocation { Name = "Docks Warehouse", Position = new SerializableVector3(new Vector3(-81f, -1.5f, -59f)), Category = "Properties" });
            allLocations.Add(new TeleportLocation { Name = "Manor (Inside Gate)", Position = new SerializableVector3(new Vector3(163f, 11f, -71f)), Category = "Properties" });
            allLocations.Add(new TeleportLocation { Name = "Manor (Outside Gate)", Position = new SerializableVector3(new Vector3(166f, 11f, -79f)), Category = "Properties" });
            // -------------------- Businesses --------------------
            allLocations.Add(new TeleportLocation { Name = "Laundromat", Position = new SerializableVector3(new Vector3(-22.5f, 1f, 25f)), Category = "Businesses" });
            allLocations.Add(new TeleportLocation { Name = "Post Office", Position = new SerializableVector3(new Vector3(47f, 1f, 5f)), Category = "Businesses" });
            allLocations.Add(new TeleportLocation { Name = "Car Wash", Position = new SerializableVector3(new Vector3(-8.5f, 1f, -19f)), Category = "Businesses" });
            allLocations.Add(new TeleportLocation { Name = "Taco Ticklers", Position = new SerializableVector3(new Vector3(-30f, 1f, 62f)), Category = "Businesses" });
            // -------------------- Stores --------------------
            allLocations.Add(new TeleportLocation { Name = "Dan's Hardware", Position = new SerializableVector3(new Vector3(-21f, -3f, 137f)), Category = "Stores" });
            allLocations.Add(new TeleportLocation { Name = "Handy Hank's", Position = new SerializableVector3(new Vector3(104f, 1f, 25f)), Category = "Stores" });
            allLocations.Add(new TeleportLocation { Name = "Gas-Mart", Position = new SerializableVector3(new Vector3(-113f, -2.9f, 68f)), Category = "Stores" });
            allLocations.Add(new TeleportLocation { Name = "Gas-Mart/Auto Shop", Position = new SerializableVector3(new Vector3(16f, 1f, -16.5f)), Category = "Stores" });
            allLocations.Add(new TeleportLocation { Name = "Pawnshop", Position = new SerializableVector3(new Vector3(-61.3f, 1f, 53f)), Category = "Stores" });
            allLocations.Add(new TeleportLocation { Name = "Shred Shack", Position = new SerializableVector3(new Vector3(-39f, -2.9f, 121f)), Category = "Stores" });
            allLocations.Add(new TeleportLocation { Name = "Ray's Real Estate", Position = new SerializableVector3(new Vector3(81.5f, 1f, -7f)), Category = "Stores" });
            allLocations.Add(new TeleportLocation { Name = "Blueball's Boutique", Position = new SerializableVector3(new Vector3(71f, 1f, -8f)), Category = "Stores" });
            allLocations.Add(new TeleportLocation { Name = "Thrifty Threads", Position = new SerializableVector3(new Vector3(-22.5f, 1f, 12f)), Category = "Stores" });
            allLocations.Add(new TeleportLocation { Name = "Top Tattoo", Position = new SerializableVector3(new Vector3(-130f, -2.9f, 67.4f)), Category = "Stores" });
            // -------------------- Dealers --------------------
            allLocations.Add(new TeleportLocation { Name = "Benji Coleman", Position = new SerializableVector3(new Vector3(-67f, 1.6f, 88f)), Category = "Dealers" });
            allLocations.Add(new TeleportLocation { Name = "Molly Presley", Position = new SerializableVector3(new Vector3(-166f, -2.8f, 93f)), Category = "Dealers" });
            allLocations.Add(new TeleportLocation { Name = "Brad Crosby", Position = new SerializableVector3(new Vector3(2.6f, 1f, 83f)), Category = "Dealers" });
            allLocations.Add(new TeleportLocation { Name = "Jane Lucero", Position = new SerializableVector3(new Vector3(-27.4f, 0.9f, -82f)), Category = "Dealers" });
            allLocations.Add(new TeleportLocation { Name = "Wei Long", Position = new SerializableVector3(new Vector3(65f, 5f, -67f)), Category = "Dealers" });
            allLocations.Add(new TeleportLocation { Name = "Leo Rivers", Position = new SerializableVector3(new Vector3(149f, 1.7f, 65f)), Category = "Dealers" });
            // -------------------- World --------------------
            allLocations.Add(new TeleportLocation { Name = "Warehouse (Outside)", Position = new SerializableVector3(new Vector3(-42f, -1.5f, 43f)), Category = "World" });
            allLocations.Add(new TeleportLocation { Name = "Warehouse (Inside)", Position = new SerializableVector3(new Vector3(-42f, -1f, 38f)), Category = "World" });
            allLocations.Add(new TeleportLocation { Name = "Casino", Position = new SerializableVector3(new Vector3(22.8f, 2f, 89f)), Category = "World" });
            allLocations.Add(new TeleportLocation { Name = "Construction site", Position = new SerializableVector3(new Vector3(-130f, -3f, 97f)), Category = "World" });
            allLocations.Add(new TeleportLocation { Name = "Water Front", Position = new SerializableVector3(new Vector3(51.5f, 1f, 95f)), Category = "World" });
            allLocations.Add(new TeleportLocation { Name = "Westville", Position = new SerializableVector3(new Vector3(-137.2f, -3f, 44f)), Category = "World" });
            // -------------------- Dead Drops --------------------
            allLocations.Add(new TeleportLocation { Name = "North Arcade Wall", Position = new SerializableVector3(new Vector3(-48f, -3f, 148f)), Category = "Dead Drops" });
            allLocations.Add(new TeleportLocation { Name = "Behind Thompson Construction", Position = new SerializableVector3(new Vector3(-36f, 1f, 113f)), Category = "Dead Drops" });
            allLocations.Add(new TeleportLocation { Name = "Taco Ticklers Exterior Wall", Position = new SerializableVector3(new Vector3(-24f, 1f, 82f)), Category = "Dead Drops" });
            allLocations.Add(new TeleportLocation { Name = "Skate Park", Position = new SerializableVector3(new Vector3(-41f, 1f, 87f)), Category = "Dead Drops" });
            allLocations.Add(new TeleportLocation { Name = "Behind Motel Office", Position = new SerializableVector3(new Vector3(-66f, 1f, 77f)), Category = "Dead Drops" });
            allLocations.Add(new TeleportLocation { Name = "Under West Bridge", Position = new SerializableVector3(new Vector3(-89f, -3f, 67f)), Category = "Dead Drops" });
            allLocations.Add(new TeleportLocation { Name = "Behind Gas-Mart", Position = new SerializableVector3(new Vector3(-113f, -3f, 55f)), Category = "Dead Drops" });
            allLocations.Add(new TeleportLocation { Name = "Alleyway Behind Top Tattoo", Position = new SerializableVector3(new Vector3(-139f, -3f, 73f)), Category = "Dead Drops" });
            allLocations.Add(new TeleportLocation { Name = "Brown Apartment Block", Position = new SerializableVector3(new Vector3(-171f, -3f, 93f)), Category = "Dead Drops" });
            allLocations.Add(new TeleportLocation { Name = "Pawn Shop West Wall", Position = new SerializableVector3(new Vector3(-67f, 1f, 52f)), Category = "Dead Drops" });
            allLocations.Add(new TeleportLocation { Name = "Alleyway Behind The Laundromat", Position = new SerializableVector3(new Vector3(-34f, -2f, 25f)), Category = "Dead Drops" });
            allLocations.Add(new TeleportLocation { Name = "Alleyway Behind Slop Shop", Position = new SerializableVector3(new Vector3(-3f, 1f, 32f)), Category = "Dead Drops" });
            allLocations.Add(new TeleportLocation { Name = "Alleyway Behind Grocery Store", Position = new SerializableVector3(new Vector3(6f, 1f, 72f)), Category = "Dead Drops" });
            allLocations.Add(new TeleportLocation { Name = "Behind The Supermarket", Position = new SerializableVector3(new Vector3(25f, 1f, 69f)), Category = "Dead Drops" });
            allLocations.Add(new TeleportLocation { Name = "Behind Crimson Canary", Position = new SerializableVector3(new Vector3(46f, 1f, 66f)), Category = "Dead Drops" });
            allLocations.Add(new TeleportLocation { Name = "Behind Medical Practice", Position = new SerializableVector3(new Vector3(98f, 1f, 72f)), Category = "Dead Drops" });
            allLocations.Add(new TeleportLocation { Name = "Behind Fire Station", Position = new SerializableVector3(new Vector3(114f, 1f, 38f)), Category = "Dead Drops" });
            allLocations.Add(new TeleportLocation { Name = "Behind Bank", Position = new SerializableVector3(new Vector3(76f, 1f, 36f)), Category = "Dead Drops" });
            allLocations.Add(new TeleportLocation { Name = "Fountain", Position = new SerializableVector3(new Vector3(48f, 1f, 32f)), Category = "Dead Drops" });
            allLocations.Add(new TeleportLocation { Name = "Central Canal", Position = new SerializableVector3(new Vector3(26f, -1f, 13f)), Category = "Dead Drops" });
            allLocations.Add(new TeleportLocation { Name = "Behind Auto Shops", Position = new SerializableVector3(new Vector3(2f, 1f, -4f)), Category = "Dead Drops" });
            allLocations.Add(new TeleportLocation { Name = "Grey Docks Building", Position = new SerializableVector3(new Vector3(-47f, -1.5f, -72f)), Category = "Dead Drops" });
            allLocations.Add(new TeleportLocation { Name = "Behind Randys Bait Tackle", Position = new SerializableVector3(new Vector3(-98f, -2f, -37f)), Category = "Dead Drops" });
            allLocations.Add(new TeleportLocation { Name = "Gazebo", Position = new SerializableVector3(new Vector3(93f, 5f, -129f)), Category = "Dead Drops" });
            // -------------------- Supplier Stashes  --------------------
            allLocations.Add(new TeleportLocation { Name = "Albert Hoover's Stash", Position = new SerializableVector3(new Vector3(-18f, -3f, 147f)), Category = "Supplier Stashes" });
            allLocations.Add(new TeleportLocation { Name = "Shirley Watt's Stash", Position = new SerializableVector3(new Vector3(-66f, -1.5f, 32f)), Category = "Supplier Stashes" });
            allLocations.Add(new TeleportLocation { Name = "Salvador Moreno's Stash", Position = new SerializableVector3(new Vector3(148f, 1f, 35f)), Category = "Supplier Stashes" });
            // -------------------- Saved Locations --------------------
            foreach (var saved in savedLocations)
            {
                saved.Category = "Custom Teleports";
                allLocations.Add(saved);
            }
        }
        #endregion

        #region Active Deals
        public void AddActiveDealTeleport(TeleportLocation loc)
        {
            allLocations.RemoveAll(l => l.Name == loc.Name && l.Category == "Active Deals");
            allLocations.Add(loc);
            MelonLogger.Msg($"[Teleport Menu] Added Active Deal teleport: {loc.Name}");
        }


        public void RemoveActiveDealTeleport(string dealName)
        {
            allLocations.RemoveAll(l => l.Name == dealName && l.Category == "Active Deals");
            MelonLogger.Msg($"[Teleport Menu] Removed Active Deal teleport: {dealName}");
        }



        static bool IsDealerThePlayer(object dealer)
        {
            if (dealer == null)
                return true; // some implementations pass null when the player accepted

            var t = dealer.GetType();

            // common property/field names used in different projects
            string[] candidateNames = { "IsLocalPlayer", "isLocalPlayer", "IsPlayer", "isPlayer" };

            foreach (var name in candidateNames)
            {
                var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (p != null && p.PropertyType == typeof(bool))
                    return (bool)p.GetValue(dealer);

                var f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (f != null && f.FieldType == typeof(bool))
                    return (bool)f.GetValue(dealer);
            }

            // fallback: check for an Owner/Player property and compare to Player.Local (if available)
            var ownerProp = t.GetProperty("Owner") ?? t.GetProperty("Player") ?? t.GetProperty("owner");
            if (ownerProp != null)
            {
                var owner = ownerProp.GetValue(dealer);
                // Player.Local – compare if possible
                if (owner != null && Player.Local != null)
                    return owner == Player.Local;
            }

            // final conservative default
            return false;
        }

        [HarmonyPatch(typeof(Customer), "ContractAccepted")]
        class Patch_ContractAccepted
        {
            static void Postfix(Customer __instance, Contract __result, EDealWindow window, bool trackContract, Dealer dealer)
            {
                // Prefer trackContract but fallback to reflection check
                if (!trackContract && !IsDealerThePlayer(dealer)) return;

                if (__result == null || __result.DeliveryLocation == null) return;

                Vector3 pos = __result.DeliveryLocation.transform.position;
                pos.y += 1f;

                string dealName = __result.Title ?? __instance?.DefaultDeliveryLocation?.name ?? "Deal";

                if (Main.Instance.allLocations.Any(l => l.Name == dealName && l.Category == "Active Deals"))
                    return;

                Main.Instance.AddActiveDealTeleport(new Main.TeleportLocation
                {
                    Name = dealName,
                    Position = new Main.SerializableVector3(pos),
                    Category = "Active Deals"
                });
            }
        }

        [HarmonyPatch(typeof(Customer), "ProcessHandover")]
        class Patch_ProcessHandover
        {
            static void Postfix(
                Contract contract)
            {
                if (contract == null) return;

                string dealName = contract.Title;

                if (Main.Instance.allLocations.Any(l => l.Name == $"{dealName}" && l.Category == "Active Deals"))
                {
                    Main.Instance.RemoveActiveDealTeleport($"{dealName}");

                }
            }
        }
        #endregion

        #region CustomTeleports
        private void SaveCurrentLocation()
        {
            if (string.IsNullOrWhiteSpace(newLocationName))
                newLocationName = GenerateLocationName();
            var newLoc = new TeleportLocation
            {
                Name = newLocationName,
                Position = new SerializableVector3(Player.Local.transform.position),
                Category = "Custom Teleports"
            };
            savedLocations.Add(newLoc);
            SaveAllData();
            if (!allLocations.Any(l => l.Name == newLoc.Name && l.Category == newLoc.Category))
                allLocations.Add(newLoc);
            MelonLogger.Msg($"Saved location: {newLocationName}");
            newLocationName = "";
            // Force submenu refresh
            currentCategory = "Custom Teleports";
            currentMenu = MenuState.SubMenu;
        }

        private string GenerateLocationName()
        {
            int counter = 1;
            string baseName = "Location_";
            string name = baseName + counter;
            // Ensure the name is unique among saved locations
            while (savedLocations.Any(l => l.Name == name))
            {
                counter++;
                name = baseName + counter;
            }
            return name;
        }

        private List<string> GetMenuCategoryOrder()
        {

            // Start with all main menu categories in order
            List<string> menuCategories = allLocations
                .Select(l => l.Category)
                .Distinct()
                .ToList();
            if (!menuCategories.Contains("Custom Teleports"))
                menuCategories.Add("Custom Teleports"); // or insert where you want it

            return menuCategories;
        }
        #endregion

        #region Settings
        private void DrawSettingsMenu(GUIStyle buttonStyle, GUIStyle labelStyle)
        {
            
            float y = 20f;
            // === Keybind Settings ===
            GUI.Label(new Rect(20, y, 400, 25), $"Menu Toggle Key: {settings.menuKey}", labelStyle);
            if (GUI.Button(new Rect(275, y, 100, 25), waitingForKey == "menuKey" ? "Press Key..." : "Change", buttonStyle))
            {
                waitingForKey = "menuKey";
            }
            y += 35;
            GUI.Label(new Rect(20, y, 400, 25), $"Safety Teleport Key: {settings.safetyTeleportKey}", labelStyle);
            if (GUI.Button(new Rect(275, y, 100, 25), waitingForKey == "safetyTeleportKey" ? "Press Key..." : "Change", buttonStyle))
            {
                waitingForKey = "safetyTeleportKey";
            }
            y += 35;
            if (GUI.Button(new Rect(x, y, windowRect.width - 40, 35), "Reset Keys", buttonStyle))
            {
                settings.menuKey = KeyCode.F2;
                settings.safetyTeleportKey = KeyCode.F3;
                SaveAllData();
                MelonLogger.Msg("Keys reset.");
            }
            y += 45;

            // ---------------- Title Settings ----------------
            GUI.Label(new Rect(x, y, windowRect.width - 40, 25), "Title Settings", GetCenteredLabelStyle());
            y += 35f;
            // Title Color Sliders
            float temp = settings.titleTextColorR;
            temp = GUI.HorizontalSlider(new Rect(20, y, 200, 20), temp, 0f, 255f);
            settings.titleTextColorR = Mathf.RoundToInt(temp);
            GUI.Label(new Rect(230, y - 4, 200, 25), $"Red: {settings.titleTextColorR}", labelStyle);
            y += 25f;
            temp = settings.titleTextColorG;
            temp = GUI.HorizontalSlider(new Rect(20, y, 200, 20), temp, 0f, 255f);
            settings.titleTextColorG = Mathf.RoundToInt(temp);
            GUI.Label(new Rect(230, y - 4, 200, 25), $"Green: {settings.titleTextColorG}", labelStyle);
            y += 25f;
            temp = settings.titleTextColorB;
            temp = GUI.HorizontalSlider(new Rect(20, y, 200, 20), temp, 0f, 255f);
            settings.titleTextColorB = Mathf.RoundToInt(temp);
            GUI.Label(new Rect(230, y - 4, 200, 25), $"Blue: {settings.titleTextColorB}", labelStyle);
            y += 25f;
            // Font Size Slider
            settings.titleFontSize = (int)GUI.HorizontalSlider(new Rect(20, y, 200, 20), settings.titleFontSize, 10f, 30f);
            GUI.Label(new Rect(230, y - 4, 200, 25), $"Font Size: {settings.titleFontSize}", labelStyle);
            y += 35f;

            // ---------------- Text Settings ----------------
            GUI.Label(new Rect(x, y, windowRect.width - 40, 25), "Text Settings", GetCenteredLabelStyle());
            y += 35f;
            temp = settings.textColorR;
            temp = GUI.HorizontalSlider(new Rect(20, y, 200, 20), temp, 0f, 255f);
            settings.textColorR = Mathf.RoundToInt(temp);
            GUI.Label(new Rect(230, y - 4, 200, 25), $"Red: {settings.textColorR}", labelStyle);
            y += 25f;
            temp = settings.textColorG;
            temp = GUI.HorizontalSlider(new Rect(20, y, 200, 20), temp, 0f, 255f);
            settings.textColorG = Mathf.RoundToInt(temp);
            GUI.Label(new Rect(230, y - 4, 200, 25), $"Green: {settings.textColorG}", labelStyle);
            y += 25f;
            temp = settings.textColorB;
            temp = GUI.HorizontalSlider(new Rect(20, y, 200, 20), temp, 0f, 255f);
            settings.textColorB = Mathf.RoundToInt(temp);
            GUI.Label(new Rect(230, y - 4, 200, 25), $"Blue: {settings.textColorB}", labelStyle);
            y += 25f;
            // Font Size
            settings.fontSize = (int)GUI.HorizontalSlider(new Rect(20, y, 200, 20), settings.fontSize, 10f, 30f);
            GUI.Label(new Rect(230, y - 4, 200, 25), $"Font Size: {settings.fontSize}", labelStyle);
            y += 25f;
            // Bold Toggle
            settings.bold = GUI.Toggle(new Rect(20, y, 20, 20), settings.bold, "");
            GUI.Label(new Rect(40, y - 2, 200, 25), "Bold", labelStyle);
            y += 35f;

            // ---------------- Hover Color ----------------
            GUI.Label(new Rect(x, y, windowRect.width - 40, 25), "Hover Color", GetCenteredLabelStyle());
            y += 35f;
            temp = settings.buttonHoverR;
            temp = GUI.HorizontalSlider(new Rect(20, y, 200, 20), temp, 0f, 255f);
            settings.buttonHoverR = Mathf.RoundToInt(temp);
            GUI.Label(new Rect(230, y - 4, 200, 25), $"Red: {settings.buttonHoverR}", labelStyle);
            y += 25f;
            temp = settings.buttonHoverG;
            temp = GUI.HorizontalSlider(new Rect(20, y, 200, 20), temp, 0f, 255f);
            settings.buttonHoverG = Mathf.RoundToInt(temp);
            GUI.Label(new Rect(230, y - 4, 200, 25), $"Green: {settings.buttonHoverG}", labelStyle);
            y += 25f;
            temp = settings.buttonHoverB;
            temp = GUI.HorizontalSlider(new Rect(20, y, 200, 20), temp, 0f, 255f);
            settings.buttonHoverB = Mathf.RoundToInt(temp);
            GUI.Label(new Rect(230, y - 4, 200, 25), $"Blue: {settings.buttonHoverB}", labelStyle);
            y += 35f;

            // ---------------- Accent Bar ----------------
            GUI.Label(new Rect(x, y, windowRect.width - 40, 25), "Accent Color & Alpha", GetCenteredLabelStyle());
            y += 35f;
            temp = settings.accentBarR;
            temp = GUI.HorizontalSlider(new Rect(20, y, 200, 20), temp, 0f, 255f);
            settings.accentBarR = Mathf.RoundToInt(temp);
            GUI.Label(new Rect(230, y - 4, 200, 25), $"Red: {settings.accentBarR}", labelStyle);
            y += 25f;
            temp = settings.accentBarG;
            temp = GUI.HorizontalSlider(new Rect(20, y, 200, 20), temp, 0f, 255f);
            settings.accentBarG = Mathf.RoundToInt(temp);
            GUI.Label(new Rect(230, y - 4, 200, 25), $"Green: {settings.accentBarG}", labelStyle);
            y += 25f;
            temp = settings.accentBarB;
            temp = GUI.HorizontalSlider(new Rect(20, y, 200, 20), temp, 0f, 255f);
            settings.accentBarB = Mathf.RoundToInt(temp);
            GUI.Label(new Rect(230, y - 4, 200, 25), $"Blue: {settings.accentBarB}", labelStyle);
            y += 25f;
            temp = settings.accentBarA;
            temp = GUI.HorizontalSlider(new Rect(20, y, 200, 20), temp, 0f, 255f);
            settings.accentBarA = Mathf.RoundToInt(temp);
            GUI.Label(new Rect(230, y - 4, 200, 25), $"Alpha: {settings.accentBarA}", labelStyle);
            y += 35f;


            // ---------------- Background ----------------
            GUI.Label(new Rect(x, y, windowRect.width - 40, 25), "Background Settings", GetCenteredLabelStyle());
            y += 35f;
            temp = settings.backgroundR;
            temp = GUI.HorizontalSlider(new Rect(20, y, 200, 20), temp, 0f, 255f);
            settings.backgroundR = Mathf.RoundToInt(temp);
            GUI.Label(new Rect(230, y - 4, 200, 25), $"Red: {settings.backgroundR}", labelStyle);
            y += 25f;
            temp = settings.backgroundG;
            temp = GUI.HorizontalSlider(new Rect(20, y, 200, 20), temp, 0f, 255f);
            settings.backgroundG = Mathf.RoundToInt(temp);
            GUI.Label(new Rect(230, y - 4, 200, 25), $"Green: {settings.backgroundG}", labelStyle);
            y += 25f;
            temp = settings.backgroundB;
            temp = GUI.HorizontalSlider(new Rect(20, y, 200, 20), temp, 0f, 255f);
            settings.backgroundB = Mathf.RoundToInt(temp);
            GUI.Label(new Rect(230, y - 4, 200, 25), $"Blue: {settings.backgroundB}", labelStyle);
            y += 25f;
            temp = settings.backgroundA;
            temp = GUI.HorizontalSlider(new Rect(20, y, 200, 20), temp, 0f, 255f);
            settings.backgroundA = Mathf.RoundToInt(temp);
            GUI.Label(new Rect(230, y - 4, 200, 25), $"Alpha: {settings.backgroundA}", labelStyle);
            y += 35f;

            // ---------------- Teleport Cost ----------------
            GUI.Label(new Rect(x, y, windowRect.width - 40, 25), "Teleport Cost Settings", GetCenteredLabelStyle());
            y += 35f;
            // Toggle: charge money for teleport
            settings.chargeForTeleport = GUI.Toggle(new Rect(20, y, 200, 25), settings.chargeForTeleport, "");
            GUI.Label(new Rect(40, y - 2, 200, 25), "Enable Teleport Cost", labelStyle);
            y += 30;
            // Label + slider
            GUI.Label(new Rect(230, y - 4, 200, 25), $"Cost: ${settings.teleportCost}", labelStyle);
            settings.teleportCost = (int)
            GUI.HorizontalSlider(new Rect(20, y, 200, 25), settings.teleportCost, 0, 500);
            y += 45f;

            // Save Settings Button
            if (GUI.Button(new Rect(x, y, windowRect.width - 40, 40), "Save Settings", buttonStyle))
            {
                SaveAllData();
            }
            y += 45f;
            // Load Settings Button
            if (GUI.Button(new Rect(x, y, windowRect.width - 40, 40), "Load Settings", buttonStyle))
            {
                LoadAllData();
            }
            y += 45f;
            // Reset Settings Button
            if (GUI.Button(new Rect(x, y, windowRect.width - 40, 40), "Reset Settings", buttonStyle))
            {
                ResetSettings();
            }
            y += 45f;
            // Export Teleports Button
            if (GUI.Button(new Rect(x, y, windowRect.width - 40, 40), "Export Teleports", buttonStyle))
            {
                string exportPath = Path.Combine(MelonEnvironment.UserDataDirectory, "TeleportMenu/ExportedTeleports.json");
                ExportTeleports(exportPath);
            }
            y += 45f;
            // Import Teleports Button
            if (GUI.Button(new Rect(x, y, windowRect.width - 40, 40), "Import Teleports", buttonStyle))
            {
                string importPath = Path.Combine(MelonEnvironment.UserDataDirectory, "TeleportMenu/ExportedTeleports.json");
                ImportTeleports(importPath);
            }
            y += 45f;
            // Back Button
            if (GUI.Button(new Rect(x, y, windowRect.width - 40, 40), "Back", buttonStyle))
            {
                currentMenu = MenuState.Main;
                scrollPosition = Vector2.zero; // Reset scroll bar
            }
        }
        private void ResetSettings()
        {
            settings = new ModSettings();
            MelonLogger.Msg("Settings reset to default.");
        }
        public class ModSettings
        {
            // Menu Keys
            public KeyCode menuKey = KeyCode.F2;
            public KeyCode safetyTeleportKey = KeyCode.F3;

            // All colors are 255 RGB
            // Title Settings
            public int titleFontSize = 18;
            public int titleTextColorR = 255;
            public int titleTextColorG = 255;
            public int titleTextColorB = 255;

            // Text Color
            public int textColorR = 255;
            public int textColorG = 255;
            public int textColorB = 255;
            public int fontSize = 14;
            public bool bold = false;

            // Button Hover Color
            public int buttonHoverR = 128;
            public int buttonHoverG = 161;
            public int buttonHoverB = 191;

            // Accent Bar
            public int accentBarR = 110;
            public int accentBarG = 110;
            public int accentBarB = 110;
            public int accentBarA = 255;

            // Background Color
            public int backgroundR = 77;
            public int backgroundG = 77;
            public int backgroundB = 77;
            public int backgroundA = 255;

            // Teleport Cost
            public bool chargeForTeleport = false;
            public int teleportCost = 30;
        }
        #endregion

        #region Import and Export custom teleports
        private void ExportTeleports(string exportPath)
        {
            try
            {
                string folder = Path.GetDirectoryName(exportPath);
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                // Only export custom teleports
                string json = JsonConvert.SerializeObject(savedLocations, Formatting.Indented);
                File.WriteAllText(exportPath, json);

                MelonLogger.Msg($"[Teleport Menu] Exported {savedLocations.Count} custom teleports to {exportPath}");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[Teleport Menu] Failed to export teleports: {ex}");
            }
        }
        private void ImportTeleports(string importPath)
        {
            try
            {
                if (!File.Exists(importPath))
                {
                    MelonLogger.Error($"[Teleport Menu] Import file not found: {importPath}");
                    return;
                }

                string json = File.ReadAllText(importPath);
                var imported = JsonConvert.DeserializeObject<List<TeleportLocation>>(json) ?? new List<TeleportLocation>();

                foreach (var loc in imported)
                {
                    loc.Category = "Custom Teleports";

                    // Add to savedLocations if not already there
                    if (!savedLocations.Any(l => l.Name == loc.Name && l.Category == "Custom Teleports"))
                        savedLocations.Add(loc);

                    // Add to allLocations so it shows instantly in the UI
                    if (!allLocations.Any(l => l.Name == loc.Name && l.Category == "Custom Teleports"))
                        allLocations.Add(loc);

                    // Restore favorite status if imported as favorite
                    if (loc.IsFavorite && !favoriteLocations.Any(f => f.Name == loc.Name && f.Category == loc.Category))
                        favoriteLocations.Add(loc);
                }
                SaveAllData();
                MelonLogger.Msg($"[Teleport Menu] Imported {imported.Count} custom teleports from {importPath}");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[Teleport Menu] Failed to import teleports: {ex}");
            }
        }
        #endregion

        #region Save and Load
        private void SaveAllData()
        {
            try
            {
                string folder = Path.GetDirectoryName(combinedSavePath);
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                CombinedData data = new CombinedData
                {
                    SavedLocations = savedLocations,
                    FavoriteLocations = favoriteLocations,
                    SafetyTeleportPosition = safetyTeleportPosition,
                    Settings = settings
                };

                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(combinedSavePath, json);
                MelonLogger.Msg("All data saved.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[Teleport Menu] Failed to data: {ex}");
            }
        }
        private void LoadAllData()
        {
            try
            {
                if (File.Exists(combinedSavePath))
                {
                    string json = File.ReadAllText(combinedSavePath);
                    CombinedData data = JsonConvert.DeserializeObject<CombinedData>(json);

                    if (data != null)
                    {
                        savedLocations = data.SavedLocations ?? new List<TeleportLocation>();
                        favoriteLocations = data.FavoriteLocations ?? new List<TeleportLocation>();
                        safetyTeleportPosition = data.SafetyTeleportPosition ?? new SerializableVector3(Vector3.zero);
                        settings = data.Settings ?? new ModSettings();
                    }

                    MelonLogger.Msg("All data loaded.");
                }
                else
                {
                    savedLocations = new List<TeleportLocation>();
                    favoriteLocations = new List<TeleportLocation>();
                    safetyTeleportPosition = new SerializableVector3(Vector3.zero);
                    settings = new ModSettings();
                    MelonLogger.Msg(" No data found, starting fresh.");
                }
            }
            catch (Exception ex)
            {
                savedLocations = new List<TeleportLocation>();
                favoriteLocations = new List<TeleportLocation>();
                safetyTeleportPosition = new SerializableVector3(Vector3.zero);
                settings = new ModSettings();
                MelonLogger.Error($"[Teleport Menu] Failed to load data: {ex}");
            }
        }
        #endregion

    }
}