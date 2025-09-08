using UnityEngine;
using MelonLoader;
using MelonLoader.Utils;
using Il2CppScheduleOne.PlayerScripts;
using Newtonsoft.Json;

[assembly: MelonInfo(typeof(ScheduleITeleportMenu.Main), "Teleport Menu", "2.0.0", "MrTibbz")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace ScheduleITeleportMenu
{

    public class Main : MelonMod
    {
        private Rect windowRect = new Rect(200, 100, 400, 500);
        private bool isMenuOpen = false;
        private KeyCode toggleKey = KeyCode.F2;
        private string settingsPath;
        private string favoriteFilePath;
        private string saveFilePath;
        private Vector2 scrollPosition;
        private bool isResizing = false;
        private Vector2 resizeStartMousePosition;
        private Vector2 resizeStartWindowSize;
        private Rect resizeHandleRect => new Rect(windowRect.xMax - 20, windowRect.yMax - 20, 20, 20);
        private bool isDragging = false;
        private Vector2 dragOffset;
        float x = 15f;
        private Dictionary<string, bool> predefinedFavorites = new Dictionary<string, bool>();
        private MenuState currentMenu = MenuState.Main;
        private string currentCategory = "";


        private string newLocationName = "";
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

            public SerializableVector3() { }

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

        private ModSettings settings = new ModSettings();
        private List<TeleportLocation> savedLocations = new List<TeleportLocation>();

        public override void OnInitializeMelon()
        {
            settingsPath = Path.Combine(MelonEnvironment.UserDataDirectory, "TeleportMenu/UISettings.json");
            saveFilePath = Path.Combine(MelonEnvironment.UserDataDirectory, "TeleportMenu/CustomTeleports.json");
            favoriteFilePath = Path.Combine(MelonEnvironment.UserDataDirectory, "TeleportMenu/FavouriteTeleports.json");
            if (settings == null)
                settings = new ModSettings();
            LoadSettings();
            LoadLocations();
            LoadFavoriteLocations();
            InitializePredefinedFavorites();
            InitializeAllLocations();

            foreach (var saved in savedLocations)
            {
                if (!allLocations.Any(l => l.Name == saved.Name && l.Category == saved.Category))
                {
                    allLocations.Add(saved);
                }
            }
            if (!allLocations.Any(l => l.Category == "Saved"))
            {
                allLocations.Add(new TeleportLocation
                {
                    Name = "", // Empty placeholder
                    Position = new SerializableVector3(Vector3.zero),
                    Category = "Saved"
                });
            }
            foreach (var fav in favoriteLocations)
            {
                var loc = allLocations.FirstOrDefault(l => l.Name == fav.Name && l.Category == fav.Category);
                if (loc != null) loc.IsFavorite = true;
            }
            MelonLogger.Msg("[Teleport_Menu] Initialization complete.");
        }

        private void InitializePredefinedFavorites()
        {
            foreach (string locationName in GetAllPredefinedLocationNames())
            {
                if (!predefinedFavorites.ContainsKey(locationName))
                    predefinedFavorites.Add(locationName, false);
            }
        }
        private List<string> GetAllPredefinedLocationNames()
        {
            return new List<string>
            {
                // -------------------- Properties --------------------
                "RV",
                "Motel",
                "Sweatshop",
                "Storage Unit",
                "Bungalow",
                "Barn",
                "Docks Warehouse",
                "Manor (Inside Gate)",
                "Manor (Outside Gate)",
                // -------------------- Businesses --------------------
                "Laundromat",
                "Post Office",
                "Car Wash",
                "Taco Ticklers",
                // -------------------- Stores --------------------
                "Dan's Hardware",
                "Handy Hank's",
                "Gas-Mart",
                "Gas-Mart/Auto Shop",
                "Pawnshop",
                "Shred Shack",
                "Ray's Real Estate",
                "Blueball's Boutique",
                "Thrifty Threads",
                "Top Tattoo",
                // -------------------- Dealers --------------------
                "Benji Coleman",
                "Molly Presley",
                "Brad Crosby",
                "Jane Lucero",
                "Wei Long",
                "Leo Rivers",
                // -------------------- World --------------------
                "Warehouse (Outside)",
                "Warehouse (Inside)",
                "Casino",
                "Construction site",
                "Water Front",
                "Westville",
                // -------------------- Dead Drops --------------------
                "North Arcade Wall",
                "Behind Thompson Construction",
                "Taco Ticklers Exterior Wall",
                "Skate Park",
                "Behind Motel Office",
                "Under West Bridge",
                "Behind Gas-Mart",
                "Alleyway Behind Top Tattoo",
                "Brown Apartment Block",
                "Pawn Shop West Wall",
                "Alleyway Behind The Laundromat",
                "Alleyway Behind Slop Shop",
                "Alleyway Behind Grocery Store",
                "Behind The Supermarket",
                "Behind Crimson Canary",
                "Behind Medical Practice",
                "Behind Fire Station",
                "Behind Bank",
                "Fountain",
                "Central Canal",
                "Behind Auto Shops",
                "Grey Docks Building",
                "Behind Randys Bait Tackle",
                "Gazebo",
                // -------------------- Supplier Stashes --------------------
                "Albert Hoover's Stash",
                "Shirley Watt's Stash",
                "Salvador Moreno's Stash"
            };
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
                saved.Category = "Saved";
                allLocations.Add(saved);
            }
        }

        public override void OnUpdate()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                isMenuOpen = !isMenuOpen;

                if (isMenuOpen)
                {
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                    if (PlayerMovement.Instance != null) PlayerMovement.Instance.enabled = false;
                    if (PlayerCamera.Instance != null) PlayerCamera.Instance.enabled = false;
                    //PlayerMovement.Instance?.SetStamina(0);
                }
                else
                {
                    Cursor.visible = false;
                    Cursor.lockState = CursorLockMode.Locked;
                    if (PlayerMovement.Instance != null) PlayerMovement.Instance.enabled = true;
                    if (PlayerCamera.Instance != null) PlayerCamera.Instance.enabled = true;
                    //PlayerMovement.Instance?.SetStamina(PlayerMovement.StaminaReserveMax);
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
        }

        public override void OnGUI()
        {
            if (isMenuOpen)
            {
                GUI.backgroundColor = new Color(settings.backgroundR, settings.backgroundG, settings.backgroundB, settings.backgroundA);
                windowRect = GUI.Window(0, windowRect, (GUI.WindowFunction)DrawWindow, "");
                GUI.Box(resizeHandleRect, "");
                if (Event.current.type == EventType.MouseDown && resizeHandleRect.Contains(Event.current.mousePosition))
                {
                    isResizing = true;
                    resizeStartMousePosition = Input.mousePosition;
                    resizeStartWindowSize = new Vector2(windowRect.width, windowRect.height);
                    Event.current.Use();
                }
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
                normal = { textColor = new Color(settings.textColorR, settings.textColorG, settings.textColorB) },
                padding = new RectOffset(settings.buttonPadding, settings.buttonPadding, settings.buttonPadding, settings.buttonPadding)
            };
        }

        private GUIStyle GetButtonStyle()
        {
            GUIStyle style = new GUIStyle(GUI.skin.button)
            {
                fontSize = settings.fontSize,
                fontStyle = settings.bold ? FontStyle.Bold : FontStyle.Normal,
                normal = { textColor = new Color(settings.textColorR, settings.textColorG, settings.textColorB) },
                hover = { textColor = new Color(settings.buttonHoverR, settings.buttonHoverG, settings.buttonHoverB) },
                border = new RectOffset(settings.buttonCornerRadius, settings.buttonCornerRadius, settings.buttonCornerRadius, settings.buttonCornerRadius),
                padding = new RectOffset(settings.buttonPadding, settings.buttonPadding, settings.buttonPadding, settings.buttonPadding)
            };
            return style;
        }

        private void DrawWindow(int windowID)
        {
            GUI.backgroundColor = new Color(settings.accentBarR, settings.accentBarG, settings.accentBarB, settings.accentBarA);
            GUI.Box(new Rect(0, 0, windowRect.width, 20), "Teleport Menu", new GUIStyle()
            {
                fontSize = settings.titleFontSize,
                fontStyle = settings.bold ? FontStyle.Bold : FontStyle.Normal,
                normal = new GUIStyleState() { textColor = new Color(settings.titleTextColorR, settings.titleTextColorG, settings.titleTextColorB) },
                alignment = TextAnchor.MiddleCenter
            });
            float totalContentHeight = 1300;
            scrollPosition = GUI.BeginScrollView(new Rect(0, 20, windowRect.width, windowRect.height - 20), scrollPosition, new Rect(0, 0, windowRect.width - 20, totalContentHeight));

            GUIStyle buttonStyle = GetButtonStyle();

            switch (currentMenu)
            {
                case MenuState.Main:
                    DrawMainMenu(buttonStyle);
                    break;
                case MenuState.Favorites:
                    DrawFavoritesMenu(buttonStyle);
                    break;
                case MenuState.SubMenu:
                    DrawLocationSubMenu(buttonStyle);
                    break;
                case MenuState.Settings:
                    DrawSettingsMenu(buttonStyle);
                    break;
            }
            GUI.EndScrollView();
            GUI.DragWindow(new Rect(0, 0, windowRect.width - 20, 20));
        }

        private List<TeleportLocation> favoriteLocations = new List<TeleportLocation>();
        private List<TeleportLocation> allLocations = new List<TeleportLocation>();

        private class MenuCategory
        {
            public string Name;
            public Action<GUIStyle> DrawFunction;

            public MenuCategory(string name, Action<GUIStyle> drawFunction)
            {
                Name = name;
                DrawFunction = drawFunction;
            }
        }

        private enum MenuState
        {
            Main,
            Settings,
            TeleportSavedLocations,
            Favorites,
            SubMenu 
        }

        private void DrawMainMenu(GUIStyle buttonStyle)
        {
            float y = 20f;
            if (GUI.Button(new Rect(x, y, windowRect.width - 40, 40), "Favorites", buttonStyle))
                currentMenu = MenuState.Favorites;
            y += 45f;
            foreach (var category in allLocations.Select(l => l.Category).Distinct().Where(c => c != "Saved"))
            {
                if (GUI.Button(new Rect(x, y, windowRect.width - 40, 40), category, buttonStyle))
                {
                    currentCategory = category;
                    currentMenu = MenuState.SubMenu;
                }
                y += 45f;
            }
            if (GUI.Button(new Rect(x, y, windowRect.width - 40, 40), "Custom Teleports", buttonStyle))
            {
                currentCategory = "Saved";
                currentMenu = MenuState.SubMenu;
            }
            y += 45f;
            if (GUI.Button(new Rect(x, y, windowRect.width - 40, 40), "Settings", buttonStyle))
                currentMenu = MenuState.Settings;
        }

        private void DrawLocationSubMenu(GUIStyle buttonStyle)
        {
            float y = 20f;
            float favButtonWidth = 40;
            float deleteButtonWidth = 40;
            var locationsInCategory = allLocations
            .Where(l => l.Category == currentCategory && (!string.IsNullOrEmpty(l.Name) || currentCategory != "Saved"))
            .ToList();
            if (currentCategory == "Saved")
            {
                GUI.Label(new Rect(20, y, windowRect.width - 40, 30), "Enter Name: " + newLocationName, GetLabelStyle());
                y += 35f;
                if (GUI.Button(new Rect(x, y, windowRect.width - 40, 40), "Save Current Location", buttonStyle))
                {
                    if (!string.IsNullOrWhiteSpace(newLocationName))
                        SaveCurrentLocation();
                    else
                        MelonLogger.Msg("Please enter a name before saving!");
                }
                y += 45f;
            }
            foreach (var loc in locationsInCategory)
            {
                float teleportButtonWidth = windowRect.width - 87; // Default width
                if (currentCategory == "Saved")
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
                    SaveFavoriteLocations();
                }
                // Delete button only for Saved Locations
                if (currentCategory == "Saved")
                {
                    if (GUI.Button(new Rect(x + teleportButtonWidth + 5 + favButtonWidth + 5, y, deleteButtonWidth, 40), "X", buttonStyle))
                    {
                        savedLocations.RemoveAll(l => l.Name == loc.Name && l.Category == loc.Category);
                        allLocations.RemoveAll(l => l.Name == loc.Name && l.Category == loc.Category);
                        SaveLocations();
                        break; // Prevent enumeration issues
                    }
                }
                y += 45f;
            }
            if (GUI.Button(new Rect(x, y, windowRect.width - 40, 40), "Back", buttonStyle))
                currentMenu = MenuState.Main;
        }

        private void SaveCurrentLocation()
        {
            if (string.IsNullOrEmpty(newLocationName))
                newLocationName = "New Location";

            var newLoc = new TeleportLocation
            {
                Name = string.IsNullOrEmpty(newLocationName) ? "New Location" : newLocationName,
                Position = new SerializableVector3(Player.Local.transform.position),
                Category = "Saved"
            };
            savedLocations.Add(newLoc);
            SaveLocations();
            if (!allLocations.Any(l => l.Name == newLoc.Name && l.Category == newLoc.Category))
                allLocations.Add(newLoc);

            MelonLogger.Msg($"Saved location: {newLocationName}");
            newLocationName = "";
        }

        private void DrawFavoritesMenu(GUIStyle buttonStyle)
        {
            float y = 20f;
            float teleportButtonWidth = windowRect.width - 87;
            foreach (var loc in favoriteLocations)
            {
                if (GUI.Button(new Rect(x, y, teleportButtonWidth, 40), $"{loc.Name}", buttonStyle))
                    TeleportToLocation(loc);

                if (GUI.Button(new Rect(x + teleportButtonWidth + 5, y, 40, 40), "X", buttonStyle))
                {
                    loc.IsFavorite = false;
                    favoriteLocations.RemoveAll(f => f.Name == loc.Name && f.Category == loc.Category);
                    SaveFavoriteLocations();
                    break;
                }
                y += 45f;
            }
            if (GUI.Button(new Rect(x, y, windowRect.width - 40, 40), "Back", buttonStyle))
                currentMenu = MenuState.Main;
        }

        private void DrawSettingsMenu(GUIStyle buttonStyle)
        {
            GUIStyle labelStyle = GetLabelStyle();
            float y = 20f;
            GUI.Label(new Rect(windowRect.width - 250, y, 200, 500), "Title Settings", labelStyle);
            y += 35f;
            // Title Color Sliders
            settings.titleTextColorR = GUI.HorizontalSlider(new Rect(20, y, 200, 20), settings.titleTextColorR, 0f, 1f);
            GUI.Label(new Rect(230, y - 4, 200, 500), "Red", labelStyle);
            y += 25f;
            settings.titleTextColorG = GUI.HorizontalSlider(new Rect(20, y, 200, 20), settings.titleTextColorG, 0f, 1f);
            GUI.Label(new Rect(230, y - 4, 200, 500), "Green", labelStyle);
            y += 25f;
            settings.titleTextColorB = GUI.HorizontalSlider(new Rect(20, y, 200, 20), settings.titleTextColorB, 0f, 1f);
            GUI.Label(new Rect(230, y - 4, 200, 500), "Blue", labelStyle);
            y += 25f;
            // Font Size Slider
            settings.titleFontSize = (int)GUI.HorizontalSlider(new Rect(20, y, 200, 20), settings.titleFontSize, 10f, 30f);
            GUI.Label(new Rect(230, y - 4, 200, 500), "Font Size", labelStyle);
            y += 50f;

            GUI.Label(new Rect(windowRect.width - 250, y, 200, 500), "Text Settings", labelStyle);
            y += 35f;
            // Text Color Sliders
            settings.textColorR = GUI.HorizontalSlider(new Rect(20, y, 200, 20), settings.textColorR, 0f, 1f);
            GUI.Label(new Rect(230, y - 4, 200, 500), "Red", labelStyle);
            y += 25f;
            settings.textColorG = GUI.HorizontalSlider(new Rect(20, y, 200, 20), settings.textColorG, 0f, 1f);
            GUI.Label(new Rect(230, y - 4, 200, 500), "Green", labelStyle);
            y += 25f;
            settings.textColorB = GUI.HorizontalSlider(new Rect(20, y, 200, 20), settings.textColorB, 0f, 1f);
            GUI.Label(new Rect(230, y - 4, 200, 500), "Blue", labelStyle);
            y += 25f;
            // Font Size Slider
            settings.fontSize = (int)GUI.HorizontalSlider(new Rect(20, y, 200, 20), settings.fontSize, 10f, 30f);
            GUI.Label(new Rect(230, y - 4, 200, 500), "Font Size", labelStyle);
            y += 25f;
            // Bold Text Toggle
            settings.bold = GUI.Toggle(new Rect(20, y, 200, 20), settings.bold, "");
            GUI.Label(new Rect(40, y, 200, 500), "Bold", labelStyle);
            y += 50f;

            GUI.Label(new Rect(windowRect.width - 250, y, 200, 500), "Hover Color Settings", labelStyle);
            y += 35f;
            // Button Hover Color Sliders
            settings.buttonHoverR = GUI.HorizontalSlider(new Rect(20, y, 200, 20), settings.buttonHoverR, 0f, 1f);
            GUI.Label(new Rect(230, y - 4, 200, 20), "Red", labelStyle);
            y += 25f;
            settings.buttonHoverG = GUI.HorizontalSlider(new Rect(20, y, 200, 20), settings.buttonHoverG, 0f, 1f);
            GUI.Label(new Rect(230, y - 4, 200, 20), "Green", labelStyle);
            y += 25f;
            settings.buttonHoverB = GUI.HorizontalSlider(new Rect(20, y, 200, 20), settings.buttonHoverB, 0f, 1f);
            GUI.Label(new Rect(230, y - 4, 200, 20), "Blue", labelStyle);
            y += 50f;

            GUI.Label(new Rect(windowRect.width - 300, y, 200, 500), "Accent Color & Alpha Settings", labelStyle);
            y += 35f;
            // Aaccent Color & Alpha
            settings.accentBarR = GUI.HorizontalSlider(new Rect(20, y, 200, 20), settings.accentBarR, 0f, 1);
            GUI.Label(new Rect(230, y - 4, 200, 20), "Red", labelStyle);
            y += 25f;
            settings.accentBarG = GUI.HorizontalSlider(new Rect(20, y, 200, 20), settings.accentBarG, 0f, 1);
            GUI.Label(new Rect(230, y - 4, 200, 20), "Green", labelStyle);
            y += 25f;
            settings.accentBarB = GUI.HorizontalSlider(new Rect(20, y, 200, 20), settings.accentBarB, 0f, 1);
            GUI.Label(new Rect(230, y - 4, 200, 20), "Blue", labelStyle);
            y += 25f;
            settings.accentBarA = GUI.HorizontalSlider(new Rect(20, y, 200, 20), settings.accentBarA, 0f, 1);
            GUI.Label(new Rect(230, y - 4, 200, 20), "Alpha", labelStyle);
            y += 50f;

            GUI.Label(new Rect(windowRect.width - 250, y, 200, 500), "Background Settings", labelStyle);
            y += 35f;
            // Background Color Sliders
            settings.backgroundR = GUI.HorizontalSlider(new Rect(20, y, 200, 20), settings.backgroundR, 0f, 1f);
            GUI.Label(new Rect(230, y - 4, 200, 20), "Red", labelStyle);
            y += 25f;
            settings.backgroundG = GUI.HorizontalSlider(new Rect(20, y, 200, 20), settings.backgroundG, 0f, 1f);
            GUI.Label(new Rect(230, y - 4, 200, 20), "Green", labelStyle);
            y += 25f;
            settings.backgroundB = GUI.HorizontalSlider(new Rect(20, y, 200, 20), settings.backgroundB, 0f, 1f);
            GUI.Label(new Rect(230, y - 4, 200, 20), "Blue", labelStyle);
            y += 25f;
            settings.backgroundA = GUI.HorizontalSlider(new Rect(20, y, 200, 20), settings.backgroundA, 0f, 1f);
            GUI.Label(new Rect(230, y - 4, 200, 20), "Alpha", labelStyle);
            y += 50f;

            // Save Settings Button
            if (GUI.Button(new Rect(x, y, windowRect.width - 40, 40), "Save Settings", buttonStyle))
            {
                SaveSettings();
            }
            y += 45f;
            // Load Settings Button
            if (GUI.Button(new Rect(x, y, windowRect.width - 40, 40), "Load Settings", buttonStyle))
            {
                LoadSettings();
            }
            y += 45f;
            // Reset Settings Button
            if (GUI.Button(new Rect(x, y, windowRect.width - 40, 40), "Reset Settings", buttonStyle))
            {
                ResetSettings();
            }
            y += 45f;
            // Back Button
            if (GUI.Button(new Rect(x, y, windowRect.width - 40, 40), "Back", buttonStyle))
            {
                currentMenu = MenuState.Main;
            }
        }

        private void TeleportToLocation(TeleportLocation location)
        {
            if (Player.Local != null)
            {
                Player.Local.transform.position = location.Position.ToVector3();
                MelonLogger.Msg($"Teleported to {location.Name} ({location.Position.x}, {location.Position.y}, {location.Position.z})");
            }
        }

        private void SaveLocations()
        {
            try
            {
                string json = JsonConvert.SerializeObject(savedLocations, Formatting.Indented);
                File.WriteAllText(saveFilePath, json);
                MelonLogger.Msg($"[TeleportMenu] Saved {savedLocations.Count} locations to: {saveFilePath}");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[TeleportMenu] Failed to save locations: {ex}");
            }
        }
        private void LoadLocations()
        {
            try
            {
                if (File.Exists(saveFilePath))
                {
                    string json = File.ReadAllText(saveFilePath);
                    savedLocations = JsonConvert.DeserializeObject<List<TeleportLocation>>(json);
                    if (savedLocations == null)
                        savedLocations = new List<TeleportLocation>();
                    MelonLogger.Msg($"[Teleport_Menu] Loaded {savedLocations.Count} teleport locations.");
                }
                else
                {
                    savedLocations = new List<TeleportLocation>();
                    MelonLogger.Msg("[Teleport_Menu] No teleport save file found, starting empty.");
                }
            }
            catch (Exception ex)
            {
                savedLocations = new List<TeleportLocation>();
                MelonLogger.Error($"[Teleport_Menu] Failed to load locations: {ex}");
            }
        }

        private void SaveSettings()
        {
            try
            {
                // Make sure the folder exists
                string folder = Path.GetDirectoryName(settingsPath);
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);
                string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(settingsPath, json);
                MelonLogger.Msg("Settings saved.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Failed to save settings: {ex.Message}");
            }
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(settingsPath))
                {
                    string json = File.ReadAllText(settingsPath);
                    settings = JsonConvert.DeserializeObject<ModSettings>(json);
                }
                else
                {
                    settings = new ModSettings();
                    SaveSettings();
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Failed to load settings: {ex.Message}");
                settings = new ModSettings();
            }
        }

        private void ResetSettings()
        {
            settings = new ModSettings();
            MelonLogger.Msg("Settings reset to default.");
        }

        private void SaveFavoriteLocations()
        {
            string folder = Path.GetDirectoryName(favoriteFilePath);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            File.WriteAllText(favoriteFilePath, JsonConvert.SerializeObject(favoriteLocations, Formatting.Indented));
        }

        private void LoadFavoriteLocations()
        {
            if (File.Exists(favoriteFilePath))
            {
                favoriteLocations = JsonConvert.DeserializeObject<List<TeleportLocation>>(File.ReadAllText(favoriteFilePath)) ?? new List<TeleportLocation>();
            }
        }

        [Serializable]
        private class FavoriteData
        {
            public Dictionary<string, bool> PredefinedFavorites = new Dictionary<string, bool>();
            public List<string> SavedLocationNames = new List<string>();
        }

        public class ModSettings
        {
            public int fontSize = 14;
            public bool bold = false;
            public float textColorR = 1f;
            public float textColorG = 1f;
            public float textColorB = 1f;
            public float buttonHoverR = 0.5f;
            public float buttonHoverG = 0.63f;
            public float buttonHoverB = 0.75f;
            public int buttonShape = 5;
            public float backgroundR = 0.3f;
            public float backgroundG = 0.3f;
            public float backgroundB = 0.3f;
            public float backgroundA = 1f;
            public int buttonCornerRadius = 8;
            public int buttonPadding = 3;
            public int spacing = 5;
            public float windowWidth = 400f;
            public float windowHeight = 500f;
            public float accentBarR = 0.43f;  
            public float accentBarG = 0.43f;  
            public float accentBarB = 0.43f; 
            public float accentBarA = 1f;  
            public int titleFontSize = 18;  
            public float titleTextColorR = 1f; 
            public float titleTextColorG = 1f; 
            public float titleTextColorB = 1f; 
        }
    }
}



