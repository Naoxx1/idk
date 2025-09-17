using BepInEx;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GorillaNetworking;
using System.IO;
using Newtonsoft.Json;

[BepInPlugin("GorillaTagLobbyHopper", "Gorilla Tag Private Lobby Hopper", "1.0.0")]
public class MAINGUI : BaseUnityPlugin, IConnectionCallbacks, IMatchmakingCallbacks
{
    Rect windowRect = new Rect(100, 100, 700, 500);
    int D = 0;
    Color color = Color.white;
    Color windowColor = new Color(0.1f, 0.1f, 0.1f, 1f);
    int currentTab = 0;
    Texture2D? solidTex;
    Texture2D? windowTex;
    GUIStyle? tabStyle;
    GUIStyle? tabPressedStyle;
    GUIStyle? windowStyle;
    bool ini = false;
    Vector2 scrollPos = Vector2.zero;
    
    // Lobby Hopper Variables
    private List<RoomInfo> availableRooms = new List<RoomInfo>();
    private bool isHopping = false;
    private bool autoJoin = false;
    private int minPlayers = 1;
    private int maxPlayers = 10;
    private string targetRegion = "US";
    private string statusMessage = "Ready to hop lobbies";
    private float lastHopTime = 0f;
    private float hopDelay = 2f;
    private Vector2 lobbyScrollPos = Vector2.zero;
    private string[] regions = { "US", "EU", "USW"};
    private int selectedRegionIndex = 0;
    
    // Custom Room List Variables
    private List<string> customRoomList = new List<string>();
    private string newRoomName = "";
    private bool useCustomList = false;
    private int currentRoomIndex = 0;
    private Vector2 customListScrollPos = Vector2.zero;
    
    // Private Rooms Hopper Variables
    private bool privateRoomsHopperEnabled = false;
    private bool isJoiningPrivateRoom = false;
    private List<string> privateRoomCodes = new List<string>();
    private string newPrivateRoomCode = "";
    private int currentPrivateRoomIndex = 0;
    private float lastPrivateJoinAttempt = 0f;
    private const float PRIVATE_JOIN_COOLDOWN = 5f;
    private Vector2 privateRoomsScrollPos = Vector2.zero;
    
    // Simple Private Room Toggle Variables
    private bool simplePrivateRoomToggle = false;
    private bool isSimpleJoining = false;
    private float lastSimpleJoinAttempt = 0f;
    private float lastSimpleLeaveTime = 0f;
    private const float SIMPLE_JOIN_COOLDOWN = 3f; // 3 seconds in room before leaving
    private const float SIMPLE_LEAVE_COOLDOWN = 5f; // 5 seconds after leaving before joining next room
    private Dictionary<string, float> failedRooms = new Dictionary<string, float>(); // Track failed rooms with timestamps
    private const float FAILED_ROOM_COOLDOWN = 30f; // 30 seconds before retrying a failed room

    void Awake()
    {
        // Load private room codes from config
        LoadPrivateRoomCodes();
        
        // Register Photon callbacks
        PhotonNetwork.AddCallbackTarget(this);
    }

    void OnDestroy()
    {
        // Remove Photon callbacks
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    void OnGUI() // DONT MESS WITH THIS UNLESS YOU KNOW WHAT YOU ARE DOING
    {
        if (!ini)
        {
            solidTex = new Texture2D(1, 1);
            solidTex.SetPixel(0, 0, new Color(0.15f, 0.15f, 0.15f, 1f));
            solidTex.Apply();
            windowTex = MakeTex(1, 1, windowColor);
            tabStyle = new GUIStyle(GUI.skin.button);
            tabStyle.normal.background = solidTex;
            tabStyle.normal.textColor = Color.white;
            tabStyle.fontStyle = FontStyle.Bold;
            tabStyle.padding = new RectOffset(8, 8, 4, 4);
            tabPressedStyle = new GUIStyle(tabStyle);
            tabPressedStyle.normal.background = MakeTex(1, 1, new Color(0.2f, 0.6f, 1f, 1f));
            windowStyle = new GUIStyle(GUI.skin.window);
            windowStyle.normal.background = windowTex;
            windowStyle.hover.background = windowTex;
            windowStyle.active.background = windowTex;
            windowStyle.focused.background = windowTex;
            windowStyle.onNormal.background = windowTex;
            windowStyle.onHover.background = windowTex;
            windowStyle.onActive.background = windowTex;
            windowStyle.onFocused.background = windowTex;
            windowStyle.normal.textColor = Color.white;
            windowStyle.fontStyle = FontStyle.Bold;
            ini = true;
        }
        GUI.color = new Color(color.r, color.g, color.b, 1f);
        windowRect = GUILayout.Window(0, windowRect, ww, "", windowStyle);
    }

    void home() // Simple GUI - just private room hopper
    {
        SimplePrivateRoomHopperGUI(0);
    }

    void ww(int id)
    {
        GUILayout.BeginHorizontal(GUI.skin.box);
        GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.normal.textColor = Color.white;
        headerStyle.alignment = TextAnchor.MiddleLeft;
        GUILayout.Box("", GUILayout.Height(30), GUILayout.ExpandWidth(true));
        Rect headerRect = GUILayoutUtility.GetLastRect();
        GUI.DrawTexture(headerRect, MakeTex(1, 1, new Color(0.15f, 0.15f, 0.15f, 1f)));
        GUI.Label(new Rect(headerRect.x + 10, headerRect.y + 5, 200, 20), "Gorilla Tag Lobby Hopper", headerStyle); // MOD NAME HERE
        GUI.Label(new Rect(headerRect.x + windowRect.width - 220, headerRect.y + 5, 210, 20), System.DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss tt"), headerStyle);
        GUILayout.EndHorizontal();
        GUILayout.Space(5);
        GUILayout.BeginVertical();
        GUILayout.Space(5);
        home();
        GUILayout.EndVertical();
        GUI.DragWindow();
    }

    void LobbyHopper(int id)
    {
        GUILayout.Label("LOBBY HOPPER", tabPressedStyle);
        GUILayout.Space(10);
        
        // Status Display
        GUILayout.BeginHorizontal();
        GUILayout.Label("Status: ", GUILayout.Width(60));
        GUILayout.Label(statusMessage);
        GUILayout.EndHorizontal();
        
        // Info about mod status
        GUILayout.BeginHorizontal();
        GUILayout.Label("ℹ️ MOD IN SAFE MODE - No interference with normal gameplay", GUILayout.Width(350));
        GUILayout.EndHorizontal();
        
        GUILayout.Space(10);
        
        // Control Buttons
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(isHopping ? "STOP HOPPING" : "START HOPPING", GUILayout.Height(30)))
        {
            if (isHopping)
            {
                StopHopping();
            }
            else
            {
                StartHopping();
            }
        }
        
        if (GUILayout.Button("REFRESH LOBBIES", GUILayout.Height(30)))
        {
            RefreshLobbies();
        }
        GUILayout.EndHorizontal();
        
        GUILayout.Space(10);
        
        // Auto Join Toggle
        autoJoin = GUILayout.Toggle(autoJoin, "Auto Join Best Lobby");
        
        GUILayout.Space(5);
        
        // Custom Room List Toggle
        useCustomList = GUILayout.Toggle(useCustomList, "Use Custom Room List");
        
        GUILayout.Space(10);
        
        // Custom Room List Management
        if (useCustomList)
        {
            GUILayout.Label("Custom Room List:");
            
            // Add new room input
            GUILayout.BeginHorizontal();
            newRoomName = GUILayout.TextField(newRoomName, GUILayout.Width(200));
            if (GUILayout.Button("ADD", GUILayout.Width(60), GUILayout.Height(20)))
            {
                AddToCustomList(newRoomName);
                newRoomName = "";
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            // Custom room list display
            customListScrollPos = GUILayout.BeginScrollView(customListScrollPos, GUILayout.Height(150));
            for (int i = 0; i < customRoomList.Count; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{i + 1}. {customRoomList[i]}");
                if (GUILayout.Button("JOIN", GUILayout.Width(50), GUILayout.Height(20)))
                {
                    JoinRoom(customRoomList[i]);
                }
                if (GUILayout.Button("X", GUILayout.Width(25), GUILayout.Height(20)))
                {
                    RemoveFromCustomList(i);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            
            GUILayout.Space(5);
            
            // Custom list controls
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("CLEAR LIST", GUILayout.Height(25)))
            {
                customRoomList.Clear();
                currentRoomIndex = 0;
            }
            if (GUILayout.Button("HOP NEXT", GUILayout.Height(25)))
            {
                HopToNextCustomRoom();
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
        }
        
        // Available Lobbies List
        GUILayout.Label($"Available Lobbies ({availableRooms.Count}):");
        lobbyScrollPos = GUILayout.BeginScrollView(lobbyScrollPos, GUILayout.Height(200));
        
        foreach (var room in availableRooms)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{room.Name} - {room.PlayerCount}/{room.MaxPlayers} players");
            if (GUILayout.Button("JOIN", GUILayout.Width(60), GUILayout.Height(20)))
            {
                JoinRoom(room.Name);
            }
            if (GUILayout.Button("+", GUILayout.Width(25), GUILayout.Height(20)))
            {
                AddToCustomList(room.Name);
            }
            GUILayout.EndHorizontal();
        }
        
        GUILayout.EndScrollView();
    }

    void PrivateRoomsHopper(int id)
    {
        GUILayout.Label("PRIVATE ROOMS HOPPER", tabPressedStyle);
        GUILayout.Space(10);
        
        // Status Display
        GUILayout.BeginHorizontal();
        GUILayout.Label("Status: ", GUILayout.Width(60));
        GUILayout.Label(privateRoomsHopperEnabled ? "Private rooms hopper ACTIVE" : "Private rooms hopper DISABLED");
        GUILayout.EndHorizontal();
        
        GUILayout.Space(10);
        
        // Main Toggle Button
        if (GUILayout.Button(privateRoomsHopperEnabled ? "DISABLE PRIVATE ROOMS HOPPER" : "ENABLE PRIVATE ROOMS HOPPER", GUILayout.Height(40)))
        {
            TogglePrivateRoomsHopper();
        }
        
        GUILayout.Space(10);
        
        // Private Room Codes Management
        GUILayout.Label("Private Room Codes:");
        
        // Add new room code input
        GUILayout.BeginHorizontal();
        newPrivateRoomCode = GUILayout.TextField(newPrivateRoomCode, GUILayout.Width(200));
        if (GUILayout.Button("ADD", GUILayout.Width(60), GUILayout.Height(20)))
        {
            AddPrivateRoomCode(newPrivateRoomCode);
            newPrivateRoomCode = "";
        }
        GUILayout.EndHorizontal();
        
        GUILayout.Space(5);
        
        // Private room codes list
        privateRoomsScrollPos = GUILayout.BeginScrollView(privateRoomsScrollPos, GUILayout.Height(150));
        for (int i = 0; i < privateRoomCodes.Count; i++)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{i + 1}. {privateRoomCodes[i]}");
            if (GUILayout.Button("JOIN", GUILayout.Width(50), GUILayout.Height(20)))
            {
                JoinPrivateRoom(privateRoomCodes[i]);
            }
            if (GUILayout.Button("X", GUILayout.Width(25), GUILayout.Height(20)))
            {
                RemovePrivateRoomCode(i);
            }
            GUILayout.EndHorizontal();
        }
        GUILayout.EndScrollView();
        
        GUILayout.Space(5);
        
        // Private room controls
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("CLEAR ALL", GUILayout.Height(25)))
        {
            privateRoomCodes.Clear();
            currentPrivateRoomIndex = 0;
            SavePrivateRoomCodes();
        }
        if (GUILayout.Button("JOIN NEXT", GUILayout.Height(25)))
        {
            JoinNextPrivateRoom();
        }
        GUILayout.EndHorizontal();
        
        GUILayout.Space(10);
        
        // Current room info
        if (PhotonNetwork.InRoom)
        {
            GUILayout.Label($"Current Room: {PhotonNetwork.CurrentRoom.Name}");
            GUILayout.Label($"Players: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");
        }
        else
        {
            GUILayout.Label("Not in any room");
        }
        
        GUILayout.Space(10);
        
        // Auto-join info
        if (privateRoomsHopperEnabled)
        {
            GUILayout.Label("Auto-joining private rooms every 5 seconds");
            GUILayout.Label($"Next attempt in: {Mathf.Max(0, PRIVATE_JOIN_COOLDOWN - (Time.time - lastPrivateJoinAttempt)):F1}s");
        }
    }

    void Settings(int id)
    {
        GUILayout.Label("SETTINGS", tabPressedStyle);
        GUILayout.Space(10);
        
        // Player Count Settings
        GUILayout.Label("Player Count Filter:");
        GUILayout.BeginHorizontal();
        GUILayout.Label("Min: ", GUILayout.Width(40));
        minPlayers = int.Parse(GUILayout.TextField(minPlayers.ToString(), GUILayout.Width(50)));
        GUILayout.Label("Max: ", GUILayout.Width(40));
        maxPlayers = int.Parse(GUILayout.TextField(maxPlayers.ToString(), GUILayout.Width(50)));
        GUILayout.EndHorizontal();
        
        GUILayout.Space(10);
        
        // Region Selection
        GUILayout.Label("Target Region:");
        selectedRegionIndex = GUILayout.SelectionGrid(selectedRegionIndex, regions, regions.Length);
        targetRegion = regions[selectedRegionIndex];
        
        GUILayout.Space(10);
        
        // Hop Delay
        GUILayout.Label($"Hop Delay: {hopDelay:F1}s");
        hopDelay = GUILayout.HorizontalSlider(hopDelay, 0.5f, 10f);
        
        GUILayout.Space(10);
        
        // Custom Room List Status
        GUILayout.Space(10);
        GUILayout.Label("Custom Room List Status:");
        GUILayout.Label($"Rooms in list: {customRoomList.Count}");
        GUILayout.Label($"Use custom list: {useCustomList}");
        if (customRoomList.Count > 0)
        {
            GUILayout.Label($"Current index: {currentRoomIndex + 1}/{customRoomList.Count}");
            GUILayout.Label($"Next room: {(currentRoomIndex < customRoomList.Count ? customRoomList[currentRoomIndex] : "None")}");
        }
        
        GUILayout.Space(10);
        
        // Connection Status
        GUILayout.Label("Connection Status:");
        GUILayout.Label($"Connected: {PhotonNetwork.IsConnected}");
        GUILayout.Label($"In Room: {PhotonNetwork.InRoom}");
        if (PhotonNetwork.InRoom)
        {
            GUILayout.Label($"Current Room: {PhotonNetwork.CurrentRoom.Name}");
            GUILayout.Label($"Players: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");
        }
    }

    void SimplePrivateRoomHopperGUI(int id)
    {
        GUIStyle centerLabel = new GUIStyle(GUI.skin.label);
        centerLabel.alignment = TextAnchor.MiddleCenter;
        centerLabel.normal.textColor = Color.white;
        centerLabel.fontSize = 12;
        
        GUILayout.Label("Private Room Hopper", tabPressedStyle);
        GUILayout.Space(10);
        
        // Main Toggle Button
        GUIStyle toggleButtonStyle = new GUIStyle(GUI.skin.button);
        toggleButtonStyle.fontSize = 16;
        toggleButtonStyle.fontStyle = FontStyle.Bold;
        if (simplePrivateRoomToggle)
        {
            toggleButtonStyle.normal.background = MakeTex(1, 1, new Color(0.2f, 0.8f, 0.2f, 1f)); // Green when active
            toggleButtonStyle.normal.textColor = Color.white;
        }
        else
        {
            toggleButtonStyle.normal.background = MakeTex(1, 1, new Color(0.3f, 0.3f, 0.3f, 1f)); // Gray when inactive
            toggleButtonStyle.normal.textColor = Color.white;
        }
        
        if (GUILayout.Button(simplePrivateRoomToggle ? "PRIVATE ROOM HOPPER: ON" : "PRIVATE ROOM HOPPER: OFF", 
            toggleButtonStyle, GUILayout.Height(50)))
        {
            ToggleSimplePrivateRoomHopper();
        }
        
        GUILayout.Space(10);
        
        // Status display
        GUILayout.Label(simplePrivateRoomToggle ? "Status: 3s in room, 5s between rooms" : "Status: Private room hopper disabled", centerLabel);
        
        if (simplePrivateRoomToggle && privateRoomCodes.Count > 0)
        {
            GUILayout.Label($"Current room: {privateRoomCodes[currentPrivateRoomIndex]}", centerLabel);
            if (PhotonNetwork.InRoom)
            {
                float timeLeft = Mathf.Max(0, SIMPLE_JOIN_COOLDOWN - (Time.time - lastSimpleJoinAttempt));
                GUILayout.Label($"Leaving room in: {timeLeft:F1}s", centerLabel);
            }
            else if (lastSimpleLeaveTime > 0 && Time.time - lastSimpleLeaveTime < SIMPLE_LEAVE_COOLDOWN)
            {
                float timeLeft = Mathf.Max(0, SIMPLE_LEAVE_COOLDOWN - (Time.time - lastSimpleLeaveTime));
                GUILayout.Label($"Next room in: {timeLeft:F1}s", centerLabel);
            }
            else
            {
                GUILayout.Label("Trying to join room...", centerLabel);
            }
        }
        else if (simplePrivateRoomToggle && privateRoomCodes.Count == 0)
        {
            GUILayout.Label("⚠️ No private room codes configured!", centerLabel);
        }
        
        GUILayout.Space(15);
        
        // Room Management Section
        GUILayout.Label("Room Management:", tabPressedStyle);
        GUILayout.Space(5);
        
        // Add new room code input
        GUILayout.BeginHorizontal();
        newPrivateRoomCode = GUILayout.TextField(newPrivateRoomCode, GUILayout.Width(200));
        if (GUILayout.Button("ADD", GUILayout.Width(60), GUILayout.Height(20)))
        {
            AddPrivateRoomCode(newPrivateRoomCode);
            newPrivateRoomCode = "";
        }
        GUILayout.EndHorizontal();
        
        GUILayout.Space(5);
        
        // Room codes list
        if (privateRoomCodes.Count > 0)
        {
            GUILayout.Label($"Room Codes ({privateRoomCodes.Count}):");
            privateRoomsScrollPos = GUILayout.BeginScrollView(privateRoomsScrollPos, GUILayout.Height(120));
            for (int i = 0; i < privateRoomCodes.Count; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{i + 1}. {privateRoomCodes[i]}");
                if (GUILayout.Button("JOIN", GUILayout.Width(50), GUILayout.Height(20)))
                {
                    if (!isSimpleJoining && !PhotonNetwork.InRoom)
                    {
                        JoinSimplePrivateRoom(privateRoomCodes[i]);
                    }
                }
                if (GUILayout.Button("X", GUILayout.Width(25), GUILayout.Height(20)))
                {
                    RemovePrivateRoomCode(i);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            
            GUILayout.Space(5);
            
            // Room controls
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("CLEAR ALL", GUILayout.Height(25)))
            {
                privateRoomCodes.Clear();
                currentPrivateRoomIndex = 0;
                SavePrivateRoomCodes();
            }
            if (GUILayout.Button("JOIN NEXT", GUILayout.Height(25)))
            {
                if (!isSimpleJoining && !PhotonNetwork.InRoom)
                {
                    JoinNextSimplePrivateRoom();
                }
            }
            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.Label("No room codes added yet");
        }
        
        GUILayout.Space(10);
        
        // Current room info
        if (PhotonNetwork.InRoom)
        {
            GUILayout.Label($"Current Room: {PhotonNetwork.CurrentRoom.Name}");
            GUILayout.Label($"Players: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");
        }
        else
        {
            GUILayout.Label("Not in any room");
        }
    }

    // Lobby Hopping Methods
    void StartHopping()
    {
        statusMessage = "Lobby hopper disabled to prevent interference with normal gameplay";
        // Disabled to prevent connection issues
    }
    
    void StopHopping()
    {
        isHopping = false;
        statusMessage = "Hopping stopped";
    }
    
    IEnumerator HoppingCoroutine()
    {
        // Disabled to prevent connection issues
        yield break;
    }
    
    void RefreshLobbies()
    {
        statusMessage = "Lobby refresh disabled to prevent interference with normal gameplay";
        // Disabled to prevent connection issues
    }
    
    RoomInfo GetBestRoom()
    {
        var filteredRooms = availableRooms.Where(room => 
            room.PlayerCount >= minPlayers && 
            room.PlayerCount <= maxPlayers &&
            room.IsOpen &&
            !room.RemovedFromList
        ).ToList();
        
        if (filteredRooms.Count == 0) return null;
        
        // Prefer rooms with more players but not full
        return filteredRooms.OrderByDescending(room => room.PlayerCount)
                           .ThenBy(room => room.MaxPlayers - room.PlayerCount)
                           .FirstOrDefault();
    }
    
    void JoinRoom(string roomName)
    {
        statusMessage = $"Room joining disabled to prevent interference with normal gameplay";
        // Disabled to prevent connection issues
    }
    
    // Custom Room List Methods
    void AddToCustomList(string roomName)
    {
        if (!string.IsNullOrEmpty(roomName) && !customRoomList.Contains(roomName))
        {
            customRoomList.Add(roomName);
            statusMessage = $"Added {roomName} to custom list";
        }
    }
    
    void RemoveFromCustomList(int index)
    {
        if (index >= 0 && index < customRoomList.Count)
        {
            string removedRoom = customRoomList[index];
            customRoomList.RemoveAt(index);
            statusMessage = $"Removed {removedRoom} from custom list";
            
            // Adjust current index if needed
            if (currentRoomIndex >= customRoomList.Count)
            {
                currentRoomIndex = 0;
            }
        }
    }
    
    void HopToNextCustomRoom()
    {
        if (customRoomList.Count == 0)
        {
            statusMessage = "Custom room list is empty";
            return;
        }
        
        if (currentRoomIndex >= customRoomList.Count)
        {
            currentRoomIndex = 0;
        }
        
        string roomToJoin = customRoomList[currentRoomIndex];
        statusMessage = $"Hopping to {roomToJoin} ({currentRoomIndex + 1}/{customRoomList.Count})";
        JoinRoom(roomToJoin);
        
        // Move to next room in list
        currentRoomIndex = (currentRoomIndex + 1) % customRoomList.Count;
    }
    
    // Private Rooms Hopper Methods
    void TogglePrivateRoomsHopper()
    {
        privateRoomsHopperEnabled = !privateRoomsHopperEnabled;
        
        if (privateRoomsHopperEnabled)
        {
            StartCoroutine(PrivateRoomsHoppingCoroutine());
        }
    }
    
    IEnumerator PrivateRoomsHoppingCoroutine()
    {
        while (privateRoomsHopperEnabled)
        {
            if (!isJoiningPrivateRoom && !PhotonNetwork.InRoom && Time.time - lastPrivateJoinAttempt > PRIVATE_JOIN_COOLDOWN)
            {
                JoinNextPrivateRoom();
                lastPrivateJoinAttempt = Time.time;
            }
            
            yield return new WaitForSeconds(1f);
        }
    }
    
    void JoinPrivateRoom(string roomCode)
    {
        if (isJoiningPrivateRoom || PhotonNetwork.InRoom || string.IsNullOrEmpty(roomCode))
        {
            return;
        }
        
        isJoiningPrivateRoom = true;
        
        try
        {
            // Use the same logic as DataCollection-Project
            PhotonNetworkController.Instance.AttemptToJoinSpecificRoom(roomCode, JoinType.Solo);
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"Error joining private room {roomCode}: {e.Message}");
            isJoiningPrivateRoom = false;
        }
    }
    
    void JoinNextPrivateRoom()
    {
        if (privateRoomCodes.Count == 0)
        {
            return;
        }
        
        if (currentPrivateRoomIndex >= privateRoomCodes.Count)
        {
            currentPrivateRoomIndex = 0;
        }
        
        string roomToJoin = privateRoomCodes[currentPrivateRoomIndex];
        JoinPrivateRoom(roomToJoin);
        
        // Move to next room in list
        currentPrivateRoomIndex = (currentPrivateRoomIndex + 1) % privateRoomCodes.Count;
    }
    
    void AddPrivateRoomCode(string roomCode)
    {
        if (!string.IsNullOrEmpty(roomCode) && !privateRoomCodes.Contains(roomCode))
        {
            privateRoomCodes.Add(roomCode);
            SavePrivateRoomCodes();
        }
    }
    
    void RemovePrivateRoomCode(int index)
    {
        if (index >= 0 && index < privateRoomCodes.Count)
        {
            privateRoomCodes.RemoveAt(index);
            SavePrivateRoomCodes();
            
            // Adjust current index if needed
            if (currentPrivateRoomIndex >= privateRoomCodes.Count)
            {
                currentPrivateRoomIndex = 0;
            }
        }
    }
    
    void LoadPrivateRoomCodes()
    {
        try
        {
            string configPath = Path.Combine(Application.persistentDataPath, "private_rooms_config.json");
            if (File.Exists(configPath))
            {
                string json = File.ReadAllText(configPath);
                var config = JsonConvert.DeserializeObject<PrivateRoomsConfig>(json);
                if (config != null)
                {
                    privateRoomCodes = config.RoomCodes ?? new List<string>();
                    currentPrivateRoomIndex = config.CurrentIndex;
                }
            }
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"Failed to load private rooms config: {e.Message}");
            privateRoomCodes = new List<string>();
        }
    }
    
    void SavePrivateRoomCodes()
    {
        try
        {
            string configPath = Path.Combine(Application.persistentDataPath, "private_rooms_config.json");
            var config = new PrivateRoomsConfig
            {
                RoomCodes = privateRoomCodes,
                CurrentIndex = currentPrivateRoomIndex
            };
            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(configPath, json);
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"Failed to save private rooms config: {e.Message}");
        }
    }
    
    // Photon callbacks for private room joining
    public void OnJoinedRoom()
    {
        isJoiningPrivateRoom = false;
        isSimpleJoining = false;
        
        // Start countdown when we successfully join a room
        if (simplePrivateRoomToggle)
        {
            lastSimpleJoinAttempt = Time.time;
            UnityEngine.Debug.Log($"Successfully joined private room: {PhotonNetwork.CurrentRoom.Name}, starting 3s countdown");
        }
        else
        {
            UnityEngine.Debug.Log($"Successfully joined private room: {PhotonNetwork.CurrentRoom.Name}");
        }
    }
    
    public void OnJoinRoomFailed(short returnCode, string message)
    {
        isJoiningPrivateRoom = false;
        isSimpleJoining = false;
        UnityEngine.Debug.LogError($"Failed to join private room. Code: {returnCode}, Message: {message}");
        
        // If we're in simple hopper mode and the room failed, move to next room
        if (simplePrivateRoomToggle && privateRoomCodes.Count > 0)
        {
            // Track this room as failed
            string failedRoom = privateRoomCodes[currentPrivateRoomIndex];
            failedRooms[failedRoom] = Time.time;
            UnityEngine.Debug.Log($"SimplePrivateRoomHopper: Room {failedRoom} failed, marking as failed and moving to next room");
            
            // Move to next room
            currentPrivateRoomIndex = (currentPrivateRoomIndex + 1) % privateRoomCodes.Count;
        }
    }
    
    public void OnLeftRoom()
    {
        UnityEngine.Debug.Log("SimplePrivateRoomHopper: Left the room");
        
        // If we're in simple hopper mode, start the 5-second countdown
        if (simplePrivateRoomToggle)
        {
            lastSimpleLeaveTime = Time.time;
            UnityEngine.Debug.Log("SimplePrivateRoomHopper: Starting 5-second countdown before next room");
        }
    }
    
    // IConnectionCallbacks implementation
    public void OnConnectedToMaster()
    {
        UnityEngine.Debug.Log("SimplePrivateRoomHopper: Connected to Photon Master Server");
    }
    
    public void OnDisconnected(DisconnectCause cause)
    {
        UnityEngine.Debug.Log($"SimplePrivateRoomHopper: Disconnected from Photon: {cause}");
        isJoiningPrivateRoom = false;
        isSimpleJoining = false;
    }
    
    public void OnRegionListReceived(RegionHandler regionHandler)
    {
        // Not needed for our use case
    }
    
    public void OnCustomAuthenticationResponse(Dictionary<string, object> data)
    {
        // Not needed for our use case
    }
    
    public void OnCustomAuthenticationFailed(string debugMessage)
    {
        // Not needed for our use case
    }
    
    public void OnConnected()
    {
        // Not needed for our use case
    }
    
    // IMatchmakingCallbacks implementation
    public void OnFriendListUpdate(List<FriendInfo> friendList)
    {
        // Not needed for our use case
    }
    
    public void OnCreatedRoom()
    {
        // Not needed for our use case
    }
    
    public void OnCreateRoomFailed(short returnCode, string message)
    {
        // Not needed for our use case
    }
    
    public void OnJoinedLobby()
    {
        UnityEngine.Debug.Log("SimplePrivateRoomHopper: Joined Photon Lobby");
    }
    
    public void OnLeftLobby()
    {
        // Not needed for our use case
    }
    
    public void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        // Not needed for our use case
    }
    
    public void OnLobbyStatisticsUpdate(List<TypedLobbyInfo> lobbyStatistics)
    {
        // Not needed for our use case
    }
    
    public void OnJoinRandomFailed(short returnCode, string message)
    {
        // Not needed for our use case
    }
    
    public void OnPreLeavingRoom()
    {
        // Not needed for our use case
    }
    
    // Simple Private Room Hopper Methods (using core logic from DataCollection-Project)
    void ToggleSimplePrivateRoomHopper()
    {
        simplePrivateRoomToggle = !simplePrivateRoomToggle;
        
        if (simplePrivateRoomToggle)
        {
            StartCoroutine(SimplePrivateRoomHoppingCoroutine());
        }
    }
    
    IEnumerator SimplePrivateRoomHoppingCoroutine()
    {
        while (simplePrivateRoomToggle)
        {
            // If we're in a room, wait for the countdown to finish before leaving
            if (PhotonNetwork.InRoom)
            {
                float timeInRoom = Time.time - lastSimpleJoinAttempt;
                if (timeInRoom > SIMPLE_JOIN_COOLDOWN)
                {
                    UnityEngine.Debug.Log($"SimplePrivateRoomHopper: Been in room for {timeInRoom:F1}s, leaving to join next room");
                    PhotonNetwork.LeaveRoom();
                }
            }
            // If we're not in a room and not joining, check if we can try to join
            else if (!isSimpleJoining)
            {
                // Check if we've waited long enough after leaving the last room
                if (lastSimpleLeaveTime > 0 && Time.time - lastSimpleLeaveTime > SIMPLE_LEAVE_COOLDOWN)
                {
                    JoinNextSimplePrivateRoom();
                }
                // If this is the initial start (no leave time set), try to join immediately
                else if (lastSimpleLeaveTime == 0)
                {
                    JoinNextSimplePrivateRoom();
                }
            }
            
            yield return new WaitForSeconds(0.1f); // Check more frequently for smoother countdown
        }
    }
    
    void JoinNextSimplePrivateRoom()
    {
        if (privateRoomCodes.Count == 0)
        {
            UnityEngine.Debug.LogWarning("SimplePrivateRoomHopper: No private room codes configured!");
            return;
        }
        
        if (isSimpleJoining || PhotonNetwork.InRoom)
        {
            return;
        }
        
        // Find next available room (skip recently failed ones)
        int attempts = 0;
        string roomToJoin = null;
        
        while (attempts < privateRoomCodes.Count)
        {
            if (currentPrivateRoomIndex >= privateRoomCodes.Count)
            {
                currentPrivateRoomIndex = 0;
            }
            
            string candidateRoom = privateRoomCodes[currentPrivateRoomIndex];
            
            // Check if this room recently failed
            if (failedRooms.ContainsKey(candidateRoom))
            {
                float timeSinceFailure = Time.time - failedRooms[candidateRoom];
                if (timeSinceFailure < FAILED_ROOM_COOLDOWN)
                {
                    UnityEngine.Debug.Log($"SimplePrivateRoomHopper: Skipping recently failed room {candidateRoom} (failed {timeSinceFailure:F1}s ago)");
                    currentPrivateRoomIndex = (currentPrivateRoomIndex + 1) % privateRoomCodes.Count;
                    attempts++;
                    continue;
                }
                else
                {
                    // Remove from failed list since enough time has passed
                    failedRooms.Remove(candidateRoom);
                }
            }
            
            roomToJoin = candidateRoom;
            break;
        }
        
        if (roomToJoin != null)
        {
            JoinSimplePrivateRoom(roomToJoin);
            // Move to next room in list
            currentPrivateRoomIndex = (currentPrivateRoomIndex + 1) % privateRoomCodes.Count;
        }
        else
        {
            UnityEngine.Debug.LogWarning("SimplePrivateRoomHopper: All rooms are currently on cooldown!");
        }
    }
    
    void JoinSimplePrivateRoom(string roomCode)
    {
        if (isSimpleJoining || PhotonNetwork.InRoom || string.IsNullOrEmpty(roomCode))
        {
            return;
        }
        
        isSimpleJoining = true;
        
        try
        {
            // Use the same logic as DataCollection-Project PrivateRoomManager
            PhotonNetworkController.Instance.AttemptToJoinSpecificRoom(roomCode, JoinType.Solo);
            UnityEngine.Debug.Log($"SimplePrivateRoomHopper: Attempting to join private room: {roomCode}");
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"SimplePrivateRoomHopper: Error joining private room {roomCode}: {e.Message}");
            isSimpleJoining = false;
        }
    }

    Texture2D MakeTex(int width, int height, Color col)
    {
        Texture2D result = new Texture2D(width, height);
        for (int y = 0; y < height; y++) for (int x = 0; x < width; x++) result.SetPixel(x, y, col);
        result.Apply();
        return result;
    }
}

[System.Serializable]
public class PrivateRoomsConfig
{
    public List<string> RoomCodes { get; set; } = new List<string>();
    public int CurrentIndex { get; set; } = 0;
}
