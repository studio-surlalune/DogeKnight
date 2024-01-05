using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DevHud : MonoBehaviour
{
    #if UNITY_EDITOR || DEVELOPMENT_BUILD

    private readonly Color kWhite = new Color(1.0f, 1.0f, 1.0f, 0.66f);
    private readonly Color kLightGrey = new Color(0.66f, 0.66f, 0.66f, 0.66f);
    private readonly Color kDarkGrey = new Color(0.33f, 0.33f, 0.33f, 0.66f);
    private readonly Color kLightGreyTransparent = new Color(0.66f, 0.66f, 0.66f, 0.3f);

    private static DevHud s_Instance;

    private Canvas canvas;

    private Image timingBox;
    private Text gpuTimeText;
    private Text cpuTimeText;
    private Text frameTimeText;

    private Image scalingBox;
    private Image refResolutionBox;
    private Image scaledResolutionBox;
    private Text refResolutionText;
    private Text scaledResolutionText;
    private Text scaledPercentText;

    private Image logBox;
    private Text logText;

    private FrameTiming[] timings = new FrameTiming[1];

    void Start()
    {
        s_Instance = this;

        // Create Canvas
        GameObject canvasObject = new GameObject("DeveloperHud Canvas");

        // Trick to put this gameObject under the scene where the script component is located.
        // Thsi is necessary because we may load additive scenes, in which case Start() will be called
        // before the scene is made active.
        canvasObject.transform.parent = this.transform;
        canvasObject.transform.parent = null;

        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        int displayWidth = Screen.width;
        int displayHeight = Screen.height;
        Vector2 anchorTimingBox = new Vector2(0, 0);
        Vector2 anchorTimingDims = new Vector2(170, 5 + 5 + 15 + 15 + 15 + 5 + 5);
        Vector2 anchorScalingBox = new Vector2(anchorTimingBox.x + anchorTimingDims.x, 0);
        Vector2 anchorScalingDims = new Vector2(180, anchorTimingDims.y);
        Vector2 anchorLogBox = new Vector2(0, anchorTimingDims.y);
        Vector2 anchorLogDims = new Vector2(displayWidth - anchorLogBox.x, 5 + 5 + 15 + 15 + 15 + 5 + 5);

        timingBox = CreateBox(canvas, "timingBox", anchorTimingBox.x + 5, anchorTimingBox.y - 5, anchorTimingDims.x - 5 - 5, anchorTimingDims.y - 5 - 5, false);
        timingBox.color = kLightGreyTransparent;
        gpuTimeText = CreateText(canvas, "gpuTime", anchorTimingBox.x + 10, anchorTimingBox.y - 10);
        cpuTimeText = CreateText(canvas, "cpuTime", anchorTimingBox.x + 10, anchorTimingBox.y - 10 - 15);
        frameTimeText = CreateText(canvas, "frameTime", anchorTimingBox.x + 10, anchorTimingBox.y - 10 - 15 - 15);

        scalingBox = CreateBox(canvas, "scalingBox", anchorScalingBox.x + 5, anchorScalingBox.y - 5, Mathf.Max(anchorScalingDims.x, displayWidth/16) - 5 - 5, Mathf.Max(anchorScalingDims.y, displayHeight/16) - 5 - 5, false);
        scalingBox.color = kLightGreyTransparent;
        refResolutionBox = CreateBox(canvas, "resolutionBox", anchorScalingBox.x + 10, anchorScalingBox.y - 10, displayWidth/16, displayHeight/16);
        refResolutionBox.color = kLightGrey;
        scaledResolutionBox = CreateBox(canvas, "scaledResolutionBox", anchorScalingBox.x + 10, anchorScalingBox.y - 10, displayWidth/16.0f, displayHeight/16.0f, false);
        refResolutionText = CreateText(canvas, "refRes", anchorScalingBox.x + 10 + displayWidth/16.0f + 5, anchorScalingBox.y - 10);
        scaledResolutionText = CreateText(canvas, "scaledRes", anchorScalingBox.x + 10 + displayWidth / 16.0f + 5, anchorScalingBox.y - 10 - 15);
        scaledPercentText = CreateText(canvas, "scaledPercent", anchorScalingBox.x + 10, anchorScalingBox.y - 10);
        scaledPercentText.color = kDarkGrey;

        logBox = CreateBox(canvas, "logBox", anchorLogBox.x + 5, anchorLogBox.y - 5, anchorLogDims.x - 5 - 5, anchorLogDims.y - 5 - 5, false);
        logBox.color = kLightGreyTransparent;
        logText = CreateText(canvas, "log", anchorLogBox.x + 10, anchorLogBox.y - 10);
        logText.rectTransform.sizeDelta = new Vector2(anchorLogDims.x - 10 - 10, anchorLogDims.y - 10 - 10);
    }

    void Update()
    {
        int displayWidth = Screen.width;
        int displayHeight = Screen.height;
        refResolutionText.text = $"res:{displayWidth}x{displayHeight}";

        if (FrameTimingManager.GetLatestTimings(1, timings) != 1)
        {
            gpuTimeText.text = $"GPU: <?>";
            cpuTimeText.text = $"CPU: <?>";
            frameTimeText.text = $"Frame: <?>";
            scaledResolutionText.text = "dyn:<?>x<?>";
            return;
        }

        double gpuTime = timings[0].gpuFrameTime;
        double cpuTime = timings[0].cpuMainThreadFrameTime;
        double frameTime = timings[0].cpuFrameTime;
        float scaledWidth = (float)timings[0].widthScale;
        float scaledHeight = (float)timings[0].heightScale;
        int sw = (int)(displayWidth*scaledWidth);
        int sh = (int)(displayHeight*scaledHeight);

        gpuTimeText.text = $"GPU: {gpuTime.ToString("F2")}ms";
        cpuTimeText.text = $"CPU: {cpuTime.ToString("F2")}ms (?)"; // CPU time cannot always be trusted
        frameTimeText.text = $"Frame: {frameTime.ToString("F2")}ms";
        scaledResolutionBox.rectTransform.sizeDelta = new Vector2(sw/16.0f, sh/16.0f);
        scaledResolutionText.text = $"dyn:{sw}x{sh}";
        scaledPercentText.text = $"{ 100 * (sw*sh) / (displayWidth*displayHeight) }%";
    }

    public static void Log(string msg)
    {
        if (!s_Instance)
            return;
        
        s_Instance.logText.text = msg;
    }

    private Text CreateText(Canvas canvas, string name, float x, float y)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(canvas.transform);
        Text text = textObject.AddComponent<Text>();
        text.color = kWhite;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 12;
        text.rectTransform.anchorMin = new Vector2(0, 1); // Top left anchor
        text.rectTransform.anchorMax = new Vector2(0, 1); // Top left anchor
        text.rectTransform.pivot = new Vector2(0, 1); // Top left pivot
        text.rectTransform.anchoredPosition3D = new Vector3(x, y, 1f); // z=1 so that it does not interfere with other UI components events
        text.rectTransform.sizeDelta = new Vector2(300, 100);
        // Add outline to Text
        Outline textOutline = textObject.AddComponent<Outline>();
        textOutline.effectColor = kDarkGrey; // outline color
        textOutline.effectDistance = new Vector2(1, -1); // outline offset
        return text;
    }

    private Image CreateBox(Canvas canvas, string name, float x, float y, float w, float h, bool outline = true)
    {
        GameObject barObject = new GameObject("name");
        barObject.transform.SetParent(canvas.transform);
        Image box = barObject.AddComponent<Image>();
        box.color = kWhite;
        box.rectTransform.anchorMin = new Vector2(0, 1); // Top left anchor
        box.rectTransform.anchorMax = new Vector2(0, 1); // Top left anchor
        box.rectTransform.pivot = new Vector2(0, 1); // Top left pivot
        box.rectTransform.anchoredPosition3D = new Vector3(x, y, 1f); // z=1 so that it does not interfere with other UI components events
        box.rectTransform.sizeDelta = new Vector2(w, h);
        if (outline)
        {
            // Add outline
            Outline boxOutline = barObject.AddComponent<Outline>();
            boxOutline.effectColor = kDarkGrey; // outline color
            boxOutline.effectDistance = new Vector2(1, -1); // outline offset
        }
        return box;
    }

    #endif // UNITY_EDITOR || DEVELOPMENT_BUILD
}
