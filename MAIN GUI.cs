using BepInEx;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using GorillaNetworking;
using System.IO;
using Newtonsoft.Json;

[BepInPlugin("SimplePrivateRoomHopper", "Simple Private Room Hopper", "1.0.0")]
public class MAINGUI : BaseUnityPlugin, IConnectionCallbacks, IMatchmakingCallbacks
{
    Rect windowRect = new Rect(100, 100, 700, 500);
    Color color = Color.white;
    Color windowColor = new Color(0.1f, 0.1f, 0.1f, 1f);
    Texture2D? solidTex;
    Texture2D? windowTex;
    GUIStyle? tabPressedStyle;
    GUIStyle? windowStyle;
    bool ini = false;
    Vector2 privateRoomsScrollPos = Vector2.zero;
    
    // Simple Private Room Toggle Variables
    private bool simplePrivateRoomToggle = false;
    private bool isSimpleJoining = false;
    private float lastSimpleJoinAttempt = 0f;
    private float lastSimpleLeaveTime = 0f;
    private const float SIMPLE_JOIN_COOLDOWN = 5f; // 3 seconds in room before leaving
    private const float SIMPLE_LEAVE_COOLDOWN = 5f; // 5 seconds after leaving before joining next room
    private Dictionary<string, float> failedRooms = new Dictionary<string, float>(); // Track failed rooms with timestamps
    private const float FAILED_ROOM_COOLDOWN = 30f; // 30 seconds before retrying a failed room
    
    // Private Room Management
    private List<string> privateRoomCodes = new List<string>();
    private string newPrivateRoomCode = "";
    private int currentPrivateRoomIndex = 0;

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

    void OnGUI()
    {
        if (!ini)
        {
            solidTex = new Texture2D(1, 1);
            solidTex.SetPixel(0, 0, new Color(0.15f, 0.15f, 0.15f, 1f));
            solidTex.Apply();
            windowTex = MakeTex(1, 1, windowColor);
            tabPressedStyle = new GUIStyle(GUI.skin.button);
            tabPressedStyle.normal.background = solidTex;
            tabPressedStyle.normal.textColor = Color.white;
            tabPressedStyle.fontStyle = FontStyle.Bold;
            tabPressedStyle.padding = new RectOffset(8, 8, 4, 4);
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
        GUI.Label(new Rect(headerRect.x + 10, headerRect.y + 5, 200, 20), "Simple Private Room Hopper", headerStyle);
        GUI.Label(new Rect(headerRect.x + windowRect.width - 220, headerRect.y + 5, 210, 20), System.DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss tt"), headerStyle);
        GUILayout.EndHorizontal();
        GUILayout.Space(5);
        GUILayout.BeginVertical();
        GUILayout.Space(5);
        SimplePrivateRoomHopperGUI(0);
        GUILayout.EndVertical();
        GUI.DragWindow();
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

    // Private Room Management Methods
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
            else
            {
                // Load default room codes if no config file exists
                LoadDefaultRoomCodes();
                SavePrivateRoomCodes(); // Save the defaults to config file
            }
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"Failed to load private rooms config: {e.Message}");
            LoadDefaultRoomCodes();
        }
    }
    
    void LoadDefaultRoomCodes()
    {
        privateRoomCodes = new List<string>
        {
            "MBEACHY", "AA", "TWINK", "COFFEE", "DYL", "JB630", "CHAMOYVR", "XQMVR", "LT777", "XQMVVR",
            "RAEPLAYS", "LT", "RAE", "CHAMOY", "XQMVGT", "XQMV1", "XQMV", "JB360", "LUVOGT", "360",
            "@OLIVERSAG", "LUVO", "LEMONPP", "GAY", "67MANGO", "MANGO67", "OFFICAL67", "OFFICALVR", "OLIVERSAGE", "BANSHEE",
            "ESPRESSO", "BANSH33", "HELP", "DOPEVR8", "HIDE", "CGT", "H3LP", "LUCIO", "DOPEVR", "404",
            "KIRPI4", "SNAZZY", "FACELESS", "6", "MANDA67", "SREN", "RUN", "SREN18", "FAADUU", "MODS",
            "GLITCH", "ZION", "5", "DOPEVR0", "GOOP11", "4", "@PIXEL", "MOLLYGT1", "GOOPER12", "LTMERCH",
            "FOOJO", "TTT", "FOOJ", "TTTPIG", "NIFTY", "FELINE", "FELINE67", "ELLIOT", "LILYSINGS", "GOOP12",
            "CHUNKY", "GTAG", "BPP", "RYAN", "K9", "FOGGY", "JMANCURLY", "RYANVR", "69", "7",
            "ALECVR", "10", "LANAVR", "67", "ALEC", "ZBR", "MELT", "67777", "VEN1", "0",
            "PIXEL", "MBEACHY67", "RF5", "THATCYAN", "MOD", "GULLIBLE", "JMAN", "VIKING", "WATERMAN", "REDBOY",
            "CRYPTIK", "WAG3R", "3", "BUBBLES", "SOCKYSOCK", "8", "NORGE", "QWERTY", "9", "NACHO",
            "MYM", "FEM", "2", "MBEACHY1", "COFFEE", "BOYS", "1", "GIRLS"
        };
        currentPrivateRoomIndex = 0;
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
        isSimpleJoining = false;
    }
    
    public void OnRegionListReceived(RegionHandler regionHandler) { }
    public void OnCustomAuthenticationResponse(Dictionary<string, object> data) { }
    public void OnCustomAuthenticationFailed(string debugMessage) { }
    public void OnConnected() { }
    
    // IMatchmakingCallbacks implementation
    public void OnFriendListUpdate(List<FriendInfo> friendList) { }
    public void OnCreatedRoom() { }
    public void OnCreateRoomFailed(short returnCode, string message) { }
    public void OnJoinedLobby()
    {
        UnityEngine.Debug.Log("SimplePrivateRoomHopper: Joined Photon Lobby");
    }
    public void OnLeftLobby() { }
    public void OnRoomListUpdate(List<RoomInfo> roomList) { }
    public void OnLobbyStatisticsUpdate(List<TypedLobbyInfo> lobbyStatistics) { }
    public void OnJoinRandomFailed(short returnCode, string message) { }
    public void OnPreLeavingRoom() { }
    
    // Simple Private Room Hopper Methods
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
