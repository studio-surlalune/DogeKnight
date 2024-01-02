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
    private Text gpuTimeText;
    private Text cpuTimeText;
    private Text frameTimeText;
    private Canvas canvas;

    private Image refResolutionBox;
    private Image scaledResolutionBox;
    private Text refResolutionText;
    private Text scaledResolutionText;
    private Text scaledPercentText;

    private FrameTiming[] timings = new FrameTiming[1];

    void Start()
    {
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

        gpuTimeText = CreateText(canvas, "gpuTime", 10, -10);
        cpuTimeText = CreateText(canvas, "cpuTime", 10, -30);
        frameTimeText = CreateText(canvas, "frameTime", 10, -50);

        refResolutionBox = CreateBox(canvas, "resolutionBox", 150, -10);
        int displayWidth = Screen.width;
        int displayHeight = Screen.height;
        refResolutionBox.rectTransform.sizeDelta = new Vector2(displayWidth/16, displayHeight/16);
        refResolutionBox.color = kLightGrey;
        scaledResolutionBox = CreateBox(canvas, "scaledResolutionBox", 150, -10, false);
        scaledResolutionBox.rectTransform.sizeDelta = new Vector2(displayWidth/16.0f, displayHeight/16.0f);
        refResolutionText = CreateText(canvas, "refRes", 150 + displayWidth/16.0f + 10, -10);
        refResolutionText.color = kLightGrey;
        scaledResolutionText = CreateText(canvas, "scaledRes", 150 + displayWidth/16.0f + 10, -30);
        scaledPercentText = CreateText(canvas, "scaledPercent", 150 + 5, -10 - 5);
        scaledPercentText.color = kDarkGrey;
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
        text.rectTransform.anchoredPosition = new Vector2(x, y);
        text.rectTransform.sizeDelta = new Vector2(300, 100);
        // Add outline to Text
        Outline textOutline = textObject.AddComponent<Outline>();
        textOutline.effectColor = kDarkGrey; // outline color
        textOutline.effectDistance = new Vector2(1, -1); // outline offset
        return text;
    }

    private Image CreateBox(Canvas canvas, string name, float x, float y, bool outline = true)
    {
        GameObject barObject = new GameObject("name");
        barObject.transform.SetParent(canvas.transform);
        Image box = barObject.AddComponent<Image>();
        box.color = kWhite;
        box.rectTransform.anchorMin = new Vector2(0, 1); // Top left anchor
        box.rectTransform.anchorMax = new Vector2(0, 1); // Top left anchor
        box.rectTransform.pivot = new Vector2(0, 1); // Top left pivot
        box.rectTransform.anchoredPosition = new Vector2(x, y);
        box.rectTransform.sizeDelta = new Vector2(100, 20);
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
