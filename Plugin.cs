using MelonLoader;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;
using System.Collections;
using System.Text;

[assembly: MelonInfo(typeof(LobbyInfoMod), "Lobby Info", "1.1.7", "CowboyHatVR")]

public class LobbyInfoMod : MelonMod
{
    private GameObject vrHudRoot;
    private TextMesh vrLine1;
    private TextMesh vrLine2;
    private Camera cachedCamera;

    public override void OnInitializeMelon()
    {
        MelonLogger.Msg("Lobby Info loaded");
    }

    public override void OnUpdate()
    {
        EnsureVrHudExists();
        UpdateVrHud();
    }

    private void EnsureVrHudExists()
    {
        Camera cam = GetMainCamera();
        if (cam == null)
            return;

        if (vrHudRoot != null && cachedCamera == cam)
            return;

        if (vrHudRoot != null)
            Object.Destroy(vrHudRoot);

        cachedCamera = cam;

        vrHudRoot = new GameObject("LobbyInfo_VRHUD");
        Object.DontDestroyOnLoad(vrHudRoot);

        vrHudRoot.transform.SetParent(cam.transform, false);

        // positie in headset
        vrHudRoot.transform.localPosition = new Vector3(0f, -0.14f, 0.62f);
        vrHudRoot.transform.localRotation = Quaternion.identity;

        // flink groter
        vrHudRoot.transform.localScale = Vector3.one * 0.0080f;

        GameObject line1Obj = new GameObject("VRLine1");
        line1Obj.transform.SetParent(vrHudRoot.transform, false);
        line1Obj.transform.localPosition = new Vector3(0f, 0.34f, 0f);

        vrLine1 = line1Obj.AddComponent<TextMesh>();
        vrLine1.text = "";
        vrLine1.fontSize = 80;
        vrLine1.characterSize = 0.1f;
        vrLine1.anchor = TextAnchor.MiddleCenter;
        vrLine1.alignment = TextAlignment.Center;
        vrLine1.color = Color.white;

        GameObject line2Obj = new GameObject("VRLine2");
        line2Obj.transform.SetParent(vrHudRoot.transform, false);
        line2Obj.transform.localPosition = new Vector3(0f, -0.14f, 0f);

        vrLine2 = line2Obj.AddComponent<TextMesh>();
        vrLine2.text = "";
        vrLine2.fontSize = 92;
        vrLine2.characterSize = 0.1f;
        vrLine2.anchor = TextAnchor.MiddleCenter;
        vrLine2.alignment = TextAlignment.Center;
        vrLine2.color = Color.white;

        ApplyTextMaterial(vrLine1);
        ApplyTextMaterial(vrLine2);

        MelonLogger.Msg("VR HUD created");
    }

    private void ApplyTextMaterial(TextMesh textMesh)
    {
        if (textMesh == null)
            return;

        MeshRenderer renderer = textMesh.GetComponent<MeshRenderer>();
        if (renderer == null)
            return;

        Font arial = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (arial != null)
        {
            textMesh.font = arial;
            renderer.material = new Material(arial.material);
        }

        renderer.enabled = true;
        renderer.receiveShadows = false;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        if (renderer.material != null)
        {
            renderer.material.color = Color.white;

            Shader shader = Shader.Find("GUI/Text Shader");
            if (shader != null)
                renderer.material.shader = shader;
        }
    }

    private void UpdateVrHud()
    {
        if (vrHudRoot == null || vrLine1 == null || vrLine2 == null)
            return;

        if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null)
        {
            Room room = PhotonNetwork.CurrentRoom;
            int maxPlayers = room.MaxPlayers > 0 ? room.MaxPlayers : 10;

            vrLine1.text = "Lobby: " + room.Name;
            vrLine2.text = BuildStatusLine(room, maxPlayers);
        }
        else
        {
            vrLine1.text = "Not in lobby";
            vrLine2.text = "";
        }
    }

    private string BuildStatusLine(Room room, int maxPlayers)
    {
        string privacy = room.IsVisible ? "Public" : "Private";
        string allText = CollectAllRoomText(room).ToLowerInvariant();

        string gameMode = GetGameModeFromText(allText);
        bool isModded = IsModdedFromText(allText);

        StringBuilder sb = new StringBuilder();
        sb.Append(privacy);

        if (!string.IsNullOrEmpty(gameMode))
        {
            sb.Append(" ");
            sb.Append(gameMode);
        }

        if (isModded)
        {
            sb.Append(" Modded");
        }

        sb.Append(" | Players: ");
        sb.Append(room.PlayerCount);
        sb.Append("/");
        sb.Append(maxPlayers);

        return sb.ToString();
    }

    private string CollectAllRoomText(Room room)
    {
        StringBuilder sb = new StringBuilder();

        if (room == null)
            return "";

        if (!string.IsNullOrEmpty(room.Name))
            sb.Append(room.Name).Append(" ");

        sb.Append(room.IsVisible ? "public " : "private ");

        sb.Append(room.PlayerCount).Append(" ");
        sb.Append(room.MaxPlayers).Append(" ");

        PhotonHashtable props = room.CustomProperties;
        if (props != null)
        {
            foreach (DictionaryEntry entry in props)
            {
                if (entry.Key != null)
                    sb.Append(entry.Key.ToString()).Append(" ");

                if (entry.Value != null)
                    sb.Append(entry.Value.ToString()).Append(" ");
            }
        }

        return sb.ToString();
    }

    private string GetGameModeFromText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "Unknown";

        if (text.Contains("infection"))
            return "Infection";

        if (text.Contains("casual"))
            return "Casual";

        if (text.Contains("hunt"))
            return "Hunt";

        if (text.Contains("paintbrawl") || text.Contains("paint brawl"))
            return "Paintbrawl";

        if (text.Contains("guardian"))
            return "Guardian";

        if (text.Contains("freeze"))
            return "Freeze";

        if (text.Contains("ambush"))
            return "Ambush";

        if (text.Contains("ghost"))
            return "Ghost";

        if (text.Contains("competitive"))
            return "Competitive";

        if (text.Contains("minigame") || text.Contains("mini game"))
            return "Minigames";

        return "Unknown";
    }

    private bool IsModdedFromText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        return text.Contains("modded")
            || text.Contains("mod ")
            || text.Contains(" mod")
            || text.Contains("mods")
            || text.Contains("mods=");
    }

    private Camera GetMainCamera()
    {
        if (Camera.main != null)
            return Camera.main;

        Camera[] cams = Object.FindObjectsOfType<Camera>();
        for (int i = 0; i < cams.Length; i++)
        {
            if (cams[i] != null && cams[i].enabled)
                return cams[i];
        }

        return null;
    }
}
