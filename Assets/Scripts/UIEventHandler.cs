using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class UIEventHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private static float clickedTime = -1f;
    private static TMP_Text clickedText;

    private static TMP_Text hoveredText;

    private TMP_Text tmpText;

    public static bool IsClicked(TMP_Text t)
    {
        return clickedText == t && Mathf.Abs(clickedTime - Time.time) < 0.033f;
    }

    public static bool IsHovered(TMP_Text t)
    {
        return t == hoveredText;
    }

    private void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hoveredText = tmpText;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hoveredText = null;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        clickedText = tmpText;
        clickedTime = Time.time;
    }
}
