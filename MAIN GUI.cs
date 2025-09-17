using BepInEx;
using Liv.NGFX;
using UnityEngine;

[BepInPlugin("NAME", "NAME", "1.0.0")] // CHANGE NAME, VERSION, AND DESCRIPTION HERE
public class MAINGUI : BaseUnityPlugin
{
    Rect windowRect = new Rect(100, 100, 600, 400);
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
    // bool showMessage = false; // SHOWING MESSAGE LABEL
    // float messageTimer = 0f; // MESSAGE LABEL TIMER
    Vector2 scrollPos = Vector2.zero;

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
        GUI.DragWindow();
    }

    void home() // TABS HERES HOW TO ADD TABS else if (currentTab == 3) NAME(2); AND SO ON
    {
        if (currentTab == 0) HH(0);
        else if (currentTab == 1) W(0);
        else if (currentTab == 2) set(1);
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
        GUI.Label(new Rect(headerRect.x + 10, headerRect.y + 5, 200, 20), "NAME", headerStyle); // MOD NAME HERE
        GUI.Label(new Rect(headerRect.x + windowRect.width - 220, headerRect.y + 5, 210, 20), System.DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss tt"), headerStyle);
        GUILayout.EndHorizontal();
        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical(GUILayout.Width(150));
        GUIStyle scrollStyle = GUI.skin.scrollView;
        scrollStyle.normal.background = MakeTex(1, 1, new Color(0.1f, 0.1f, 0.1f, 1f));
        scrollStyle.margin = new RectOffset(0, 0, 0, 0);
        scrollPos = GUILayout.BeginScrollView(scrollPos, false, true, GUILayout.Width(150), GUILayout.Height(300));
        if (GUILayout.Button("CREDS", currentTab == 0 ? tabPressedStyle : tabStyle, GUILayout.Height(35))) currentTab = 0;  // TAB BUTTONS HERE
        if (GUILayout.Button("YOUR THINGY", currentTab == 1 ? tabPressedStyle : tabStyle, GUILayout.Height(35))) currentTab = 1;
        if (GUILayout.Button("YOUR THINGY", currentTab == 2 ? tabPressedStyle : tabStyle, GUILayout.Height(35))) currentTab = 2;
        GUILayout.EndScrollView();
        GUILayout.EndVertical();
        GUILayout.BeginVertical();
        GUILayout.Space(5);
        home();
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
        GUI.DragWindow();
    }




    void W(int id)
    {
        GUILayout.Label("ADD YOUR CODE HERE THIS IS A PLACEHOLDER ", tabPressedStyle);
        GUI.DragWindow();
    }

    void set(int id)
    {
        GUILayout.Label("ADD YOUR CODE HERE THIS IS A PLACEHOLDER ", tabPressedStyle);
        GUI.DragWindow();
    }



    void HH(int id)  // CRED N SHIT
    {
        GUIStyle centerLabel = new GUIStyle(GUI.skin.label);
        centerLabel.alignment = TextAnchor.MiddleCenter;
        centerLabel.normal.textColor = Color.white;
        centerLabel.fontSize = 12;
        GUILayout.FlexibleSpace();
        GUILayout.Label("TEMP", centerLabel);
        GUILayout.Label("TEMP", centerLabel);
        GUILayout.Label("TEMP", centerLabel);
        GUILayout.Label("TEMP", centerLabel);
        GUILayout.FlexibleSpace();
    }

    Texture2D MakeTex(int width, int height, Color col)
    {
        Texture2D result = new Texture2D(width, height);
        for (int y = 0; y < height; y++) for (int x = 0; x < width; x++) result.SetPixel(x, y, col);
        result.Apply();
        return result;
    }
}
