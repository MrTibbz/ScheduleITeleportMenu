using UnityEngine;
using MelonLoader;
using MelonLoader.Utils;
using Il2CppScheduleOne.PlayerScripts;
using Newtonsoft.Json;


[assembly: MelonInfo(typeof(ScheduleITeleportMenu.Main), "Teleport Menu", "1.0.0", "MrTibbz")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace ScheduleITeleportMenu
{

    public class Main : MelonMod
    {
        private bool showMenu = false;
        private int selectedTab = 0;
        private Vector3 teleportLocation = new Vector3(0f, 0f, 0f);
        private MenuSettings settings = new MenuSettings();
        private string settingsFilePath;
        private Rect windowRect = new Rect(100f, 100f, 425f, 550f);
        private float fontSize;
        private float windowOpacity;
        private string teleportLocationFilePath = Path.Combine(MelonEnvironment.UserDataDirectory, "TeleportMenu/SavedTeleportLocation.json");


        public override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.F2))
            {
                showMenu = !showMenu;
            }
        }

        public override void OnGUI()
        {
            if (showMenu)
            {
                windowRect = GUI.Window(0, windowRect, (GUI.WindowFunction)WindowFunction, "");
            }
        }

        public override void OnInitializeMelon()
        {
            settingsFilePath = System.IO.Path.Combine(MelonEnvironment.UserDataDirectory, "TeleportMenu/TeleportMenuSettings.json");
            EnsureDirectoryExists();
            LoadSettings();
        }

        private void WindowFunction(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, windowRect.width, 30));
            DrawTitleBar();
            DrawBackground(new Rect(0, 30, windowRect.width, windowRect.height - 30));
            float tabWidth = (float)(windowRect.width / 4.5);
            float startX = (windowRect.width - 4 * tabWidth) / 2;

            DrawTabButton(new Rect(startX, 60, tabWidth, 40), "Properties", 0);
            DrawTabButton(new Rect(startX + tabWidth, 60, tabWidth, 40), "Businesses", 1);
            DrawTabButton(new Rect(startX + 2 * tabWidth, 60, tabWidth, 40), "Dealers", 2);
            DrawTabButton(new Rect(startX + 3 * tabWidth, 60, tabWidth, 40), "Stores", 3);
            float secondRowYOffset = 100f;
            DrawTabButton(new Rect(startX, secondRowYOffset, tabWidth, 40), "World", 4);
            DrawTabButton(new Rect(startX + tabWidth, secondRowYOffset, tabWidth, 40), "Misc", 5);
            DrawTabButton(new Rect(startX + 2 * tabWidth, secondRowYOffset, tabWidth, 40), "Settings", 6);
            float buttonYOffset = secondRowYOffset + 50f;
            float contentButtonWidth = 220f;
            float contentStartX = (windowRect.width - contentButtonWidth) / 2;

            float contentHeight = 0f;
            if (selectedTab == 0) contentHeight = ShowPropertiesTab(ref buttonYOffset, contentStartX, contentButtonWidth);
            if (selectedTab == 1) contentHeight = ShowBusinessesTab(ref buttonYOffset, contentStartX, contentButtonWidth);
            if (selectedTab == 2) contentHeight = ShowDealersTab(ref buttonYOffset, contentStartX, contentButtonWidth);
            if (selectedTab == 3) contentHeight = ShowStoresTab(ref buttonYOffset, contentStartX, contentButtonWidth);
            if (selectedTab == 4) contentHeight = ShowWorldTab(ref buttonYOffset, contentStartX, contentButtonWidth);
            if (selectedTab == 5) contentHeight = ShowMiscTab(ref buttonYOffset, contentStartX, contentButtonWidth);
            if (selectedTab == 6) contentHeight = ShowSettingsTab(ref buttonYOffset, contentStartX, contentButtonWidth);
        }

        private void DrawTitleBar()
        {
            GUIStyle titleBarStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = (int)fontSize,
                normal = { textColor = Color.white }
            };
            GUI.Box(new Rect(0, 0, windowRect.width, 30), "Teleport Menu", titleBarStyle);
        }

        private void DrawBackground(Rect rect)
        {
            GUIStyle backgroundStyle = new GUIStyle()
            {
                normal = { background = MakeTexture((int)settings.bgColorR, (int)settings.bgColorG, (int)settings.bgColorB, windowOpacity) },
                border = new RectOffset(10, 10, 10, 10)
            };
            GUI.Box(rect, "", backgroundStyle);
        }

        private Texture2D MakeTexture(int r, int g, int b, float opacity)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, new Color(r / 255f, g / 255f, b / 255f, opacity));
            tex.Apply();
            return tex;
        }

        private void DrawTabButton(Rect rect, string label, int tabIndex)
        {
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = (int)fontSize,
                normal = { textColor = Color.white },
                hover = { textColor = Color.cyan },
                alignment = TextAnchor.MiddleCenter,
            };

            if (selectedTab == tabIndex)
            {
                buttonStyle.normal.background = MakeTexture(0, 50, 150, 1);
            }

            if (GUI.Button(rect, label, buttonStyle))
            {
                selectedTab = tabIndex;
            }
        }

        private float ShowPropertiesTab(ref float yOffset, float startX, float buttonWidth)
        {
            float contentHeight = 0f;
            contentHeight += AddTeleportButton(ref yOffset, startX, buttonWidth, "RV", new Vector3(14f, 0.9f, -77f));
            contentHeight += AddTeleportButton(ref yOffset, startX, buttonWidth, "Motel", new Vector3(-66f, 1.6f, 83f));
            contentHeight += AddTeleportButton(ref yOffset, startX, buttonWidth, "Sweatshop", new Vector3(-64f, 0.4f, 142f));
            contentHeight += AddTeleportButton(ref yOffset, startX, buttonWidth, "Bungalow", new Vector3(-168f, -2.7f, 114f));
            contentHeight += AddTeleportButton(ref yOffset, startX, buttonWidth, "Barn", new Vector3(181f, 1f, -14f));
            contentHeight += AddTeleportButton(ref yOffset, startX, buttonWidth, "Docks Warehouse", new Vector3(-81f, -1.5f, -59f));
            contentHeight += AddTeleportButton(ref yOffset, startX, buttonWidth, "Manor", new Vector3(163f, 11f, -71f));
            return contentHeight;
        }

        private float ShowBusinessesTab(ref float yOffset, float startX, float buttonWidth)
        {
            float contentHeight = 0f;
            contentHeight += AddTeleportButton(ref yOffset, startX, buttonWidth, "Laundromat", new Vector3(-22.5f, 1f, 25f));
            contentHeight += AddTeleportButton(ref yOffset, startX, buttonWidth, "Post Office", new Vector3(47f, 1f, 5f));
            contentHeight += AddTeleportButton(ref yOffset, startX, buttonWidth, "Car Wash", new Vector3(-8.5f, 1f, -19f));
            contentHeight += AddTeleportButton(ref yOffset, startX, buttonWidth, "Taco Ticklers", new Vector3(-30f, 1f, 62f));
            return contentHeight;
        }

        private float ShowDealersTab(ref float yOffset, float startX, float buttonWidth)
        {
            float contentHeight = 0f;
            contentHeight += AddTeleportButton(ref yOffset, startX, buttonWidth, "Benji Coleman", new Vector3(-67f, 1.6f, 88f));
            contentHeight += AddTeleportButton(ref yOffset, startX, buttonWidth, "Molly Presley", new Vector3(-166f, -2.8f, 93f));
            contentHeight += AddTeleportButton(ref yOffset, startX, buttonWidth, "Brad Crosby", new Vector3(2.6f, 1f, 83f));
            contentHeight += AddTeleportButton(ref yOffset, startX, buttonWidth, "Jane Lucero", new Vector3(-27.4f, 0.9f, -82f));
            contentHeight += AddTeleportButton(ref yOffset, startX, buttonWidth, "Wei Long", new Vector3(65f, 5f, -67f));
            contentHeight += AddTeleportButton(ref yOffset, startX, buttonWidth, "Leo Rivers", new Vector3(149f, 1.7f, 65f));
            return contentHeight;
        }

        private float ShowStoresTab(ref float yOffset, float startX, float buttonWidth)
        {
            float contentHeight = 0f;
            contentHeight += AddTeleportButton(ref yOffset, startX, buttonWidth, "Dan's Hardware", new Vector3(-21f, -3f, 137f));
            contentHeight += AddTeleportButton(ref yOffset, startX, buttonWidth, "Handy Hank's", new Vector3(104f, 1f, 25f));
            contentHeight += AddTeleportButton(ref yOffset, startX, buttonWidth, "Gas-Mart", new Vector3(-113f, -2.9f, 68f));
            contentHeight += AddTeleportButton(ref yOffset, startX, buttonWidth, "Gas-Mart/Auto Shop", new Vector3(16f, 1f, -16.5f));
            contentHeight += AddTeleportButton(ref yOffset, startX, buttonWidth, "Shred Shack", new Vector3(-39f, -2.9f, 121f));
            contentHeight += AddTeleportButton(ref yOffset, startX, buttonWidth, "Ray's Real Estate", new Vector3(81.5f, 1f, -7f));
            contentHeight += AddTeleportButton(ref yOffset, startX, buttonWidth, "Blueball's Boutique", new Vector3(71f, 1f, -8f));
            contentHeight += AddTeleportButton(ref yOffset, startX, buttonWidth, "Thrifty Threads", new Vector3(-22.5f, 1f, 12f));
            return contentHeight;
        }

        private float ShowWorldTab(ref float yOffset, float startX, float buttonWidth)
        {
            float contentHeight = 0f;
            contentHeight += AddTeleportButton(ref yOffset, startX, buttonWidth, "Warehouse", new Vector3(-42f, -1.5f, 43f));
            contentHeight += AddTeleportButton(ref yOffset, startX, buttonWidth, "Warehouse Inside", new Vector3(-42f, -1f, 38f));
            contentHeight += AddTeleportButton(ref yOffset, startX, buttonWidth, "Construction site", new Vector3(-130f, -3f, 97f));
            return contentHeight;
        }

        private float ShowMiscTab(ref float yOffset, float startX, float buttonWidth)
        {
            float contentHeight = 0f;
            if (GUI.Button(new Rect(startX, yOffset, buttonWidth, 40), "Teleport to Location"))
            {
                TeleportToLocation();
            }
            yOffset += 50f;

            GUI.Label(new Rect(startX, yOffset, 220, 20), "X:");
            teleportLocation.x = GUI.HorizontalSlider(new Rect(startX + 30f, yOffset + 20f, 150, 20), teleportLocation.x, -200f, 200f);

            GUI.Label(new Rect(startX + 190f, yOffset + 20f, 50f, 20f), teleportLocation.x.ToString("F1"));
            yOffset += 50f;


            GUI.Label(new Rect(startX, yOffset, 220, 20), "Y:");
            teleportLocation.y = GUI.HorizontalSlider(new Rect(startX + 30f, yOffset + 20f, 150, 20), teleportLocation.y, 0f, 100f);

            GUI.Label(new Rect(startX + 190f, yOffset + 20f, 50f, 20f), teleportLocation.y.ToString("F1"));
            yOffset += 50f;


            GUI.Label(new Rect(startX, yOffset, 220, 20), "Z:");
            teleportLocation.z = GUI.HorizontalSlider(new Rect(startX + 30f, yOffset + 20f, 150, 20), teleportLocation.z, -200f, 200f);

            GUI.Label(new Rect(startX + 190f, yOffset + 20f, 50f, 20f), teleportLocation.z.ToString("F1"));
            yOffset += 50f;


            if (GUI.Button(new Rect(startX, yOffset, buttonWidth, 40), "Save Location"))
            {
                SaveTeleportLocation();
            }
            yOffset += 50f;


            if (GUI.Button(new Rect(startX, yOffset, buttonWidth, 40), "Load Location"))
            {
                LoadTeleportLocation();
            }
            yOffset += 50f;
            return contentHeight;
        }


        private float bgColorR = 50f;
        private float bgColorG = 50f;
        private float bgColorB = 50f;

        public class MenuSettings
        {
            public float windowX = 100f;
            public float windowY = 100f;
            public float windowWidth = 490;
            public float windowHeight = 630;

            public float bgColorR = 50f;
            public float bgColorG = 50f;
            public float bgColorB = 50f;

            public float windowOpacity = 1f;
            public float fontSize = 14f;
        }
        private float ShowSettingsTab(ref float yOffset, float startX, float buttonWidth)
        {
            float labelWidth = 150f;
            float sliderWidth = 250f;
            float buttonWidthFull = sliderWidth + labelWidth;


            float contentHeight = yOffset + 300f;


            GUI.Label(new Rect(30, yOffset, labelWidth, 20), "Window Width:");
            windowRect.width = GUI.HorizontalSlider(new Rect(30 + labelWidth, yOffset, sliderWidth, 20), windowRect.width, 400f, 1000f);
            yOffset += 35f;


            GUI.Label(new Rect(30, yOffset, labelWidth, 20), "Window Height:");
            windowRect.height = GUI.HorizontalSlider(new Rect(30 + labelWidth, yOffset, sliderWidth, 20), windowRect.height, 400f, 1000f);
            yOffset += 35f;


            GUI.Label(new Rect(30, yOffset, labelWidth, 20), "Window X Position:");
            windowRect.x = GUI.HorizontalSlider(new Rect(30 + labelWidth, yOffset, sliderWidth, 20), windowRect.x, 0f, Screen.width - windowRect.width);
            yOffset += 35f;


            GUI.Label(new Rect(30, yOffset, labelWidth, 20), "Window Y Position:");
            windowRect.y = GUI.HorizontalSlider(new Rect(30 + labelWidth, yOffset, sliderWidth, 20), windowRect.y, 0f, Screen.height - windowRect.height);
            yOffset += 55f;


            GUI.Label(new Rect(30, yOffset, labelWidth, 20), $"Background Red: {(int)settings.bgColorR}");
            settings.bgColorR = GUI.HorizontalSlider(new Rect(30 + labelWidth, yOffset, sliderWidth, 20), settings.bgColorR, 0f, 255f);
            yOffset += 35f;


            GUI.Label(new Rect(30, yOffset, labelWidth, 20), $"Background Green: {(int)settings.bgColorG}");
            settings.bgColorG = GUI.HorizontalSlider(new Rect(30 + labelWidth, yOffset, sliderWidth, 20), settings.bgColorG, 0f, 255f);
            yOffset += 35f;


            GUI.Label(new Rect(30, yOffset, labelWidth, 20), $"Background Blue: {(int)settings.bgColorB}");
            settings.bgColorB = GUI.HorizontalSlider(new Rect(30 + labelWidth, yOffset, sliderWidth, 20), settings.bgColorB, 0f, 255f);
            yOffset += 55f;


            GUI.Label(new Rect(30, yOffset, labelWidth, 20), $"Window Opacity:");
            windowOpacity = GUI.HorizontalSlider(new Rect(30 + labelWidth, yOffset, sliderWidth, 20), windowOpacity, 0f, 1f);
            yOffset += 35f;


            GUI.Label(new Rect(30, yOffset, labelWidth, 20), $"Font Size:");
            fontSize = GUI.HorizontalSlider(new Rect(30 + labelWidth, yOffset, sliderWidth, 20), fontSize, 10f, 30f);
            yOffset += 35f;


            if (GUI.Button(new Rect(startX, yOffset, buttonWidthFull / 2, 30f), "Save Settings"))
            {
                UpdateSettingsFromCurrent();
                SaveSettings();
            }
            yOffset += 40f;

            if (GUI.Button(new Rect(startX, yOffset, buttonWidthFull / 2, 30f), "Load Settings"))
            {
                LoadSettings();
            }
            yOffset += 40f;

            if (GUI.Button(new Rect(startX, yOffset, buttonWidthFull / 2, 30f), "Reset to Default"))
            {
                ResetSettings();
            }
            return yOffset;
        }



        private void ResetSettings()
        {

            windowRect = new Rect(100f, 100f, 490f, 630);
            windowOpacity = 1f;
            fontSize = 14f;


            settings.bgColorR = 50f;
            settings.bgColorG = 50f;
            settings.bgColorB = 50f;


            SaveSettings();
        }

        private void SaveSettings()
        {
            string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            System.IO.File.WriteAllText(settingsFilePath, json);
            MelonLogger.Msg($"Settings saved: {settings.bgColorR}, {settings.bgColorG}, {settings.bgColorB}");
        }

        private void LoadSettings()
        {
            if (System.IO.File.Exists(settingsFilePath))
            {
                string json = System.IO.File.ReadAllText(settingsFilePath);
                settings = JsonConvert.DeserializeObject<MenuSettings>(json);
                fontSize = settings.fontSize;
                windowOpacity = settings.windowOpacity;
                MelonLogger.Msg($"Settings loaded: {settings.bgColorR}, {settings.bgColorG}, {settings.bgColorB}");
            }
            else
            {

                fontSize = 14f;
                windowOpacity = 1f;
                MelonLogger.Msg("No settings file found, using defaults.");
            }


            windowRect.x = settings.windowX;
            windowRect.y = settings.windowY;
            windowRect.width = settings.windowWidth;
            windowRect.height = settings.windowHeight;

            settings.bgColorR = bgColorR;
            settings.bgColorG = bgColorG;
            settings.bgColorB = bgColorB;
        }

        private void UpdateSettingsFromCurrent()
        {
            settings.windowX = windowRect.x;
            settings.windowY = windowRect.y;
            settings.windowWidth = windowRect.width;
            settings.windowHeight = windowRect.height;

            bgColorR = settings.bgColorR;
            bgColorG = settings.bgColorG;
            bgColorB = settings.bgColorB;

            settings.fontSize = fontSize;
            settings.windowOpacity = windowOpacity;
        }


        private void EnsureDirectoryExists()
        {
            string directory = Path.GetDirectoryName(teleportLocationFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        [System.Serializable]
        public struct TeleportData
        {
            public float x;
            public float y;
            public float z;
            public string name;


            public TeleportData(Vector3 position, string name)
            {
                this.x = position.x;
                this.y = position.y;
                this.z = position.z;
                this.name = name;
            }


            public Vector3 ToVector3()
            {
                return new Vector3(x, y, z);
            }
        }

        public void SaveTeleportLocation()
        {
            Vector3 currentLocation = Player.Local.transform.position;
            TeleportData teleportData = new TeleportData(currentLocation, "TeleportLocation");
            string json = JsonConvert.SerializeObject(teleportData, Formatting.Indented);
            File.WriteAllText(teleportLocationFilePath, json);
            MelonLogger.Msg($"Teleport location saved: {currentLocation}");
        }

        public void LoadTeleportLocation()
        {
            if (File.Exists(teleportLocationFilePath))
            {
                var json = File.ReadAllText(teleportLocationFilePath);
                TeleportData teleportData = JsonConvert.DeserializeObject<TeleportData>(json);
                Vector3 teleportLocation = teleportData.ToVector3();
                Player.Local.transform.position = teleportLocation;
                MelonLogger.Msg($"Teleported to saved location: {teleportLocation}");
            }
            else
            {
                MelonLogger.Warning("No saved teleport location found!");
            }
        }

        private float AddTeleportButton(ref float yOffset, float startX, float buttonWidth, string label, Vector3 location)
        {
            if (GUI.Button(new Rect(startX, yOffset, buttonWidth, 40), label))
            {
                TeleportToPresetLocation(location);
            }
            yOffset += 50f;
            return 50f;
        }

        private void TeleportToPresetLocation(Vector3 targetLocation)
        {
            if (Player.Local != null)
            {
                Player.Local.transform.position = targetLocation;
                MelonLogger.Msg($"Teleported to {targetLocation}");
            }
        }
        private void TeleportToLocation()
        {
            if (Player.Local != null)
            {
                Player.Local.transform.position = teleportLocation;
                MelonLogger.Msg($"Teleported to {teleportLocation}");
            }
        }
    }
}
