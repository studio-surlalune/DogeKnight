using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class UIEventHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private static float clickedTime = -1f;
    private static Graphic clickedItem;
    private static Graphic hoveredItem;
    private static bool mouseOrTouchEnabled;

    private Graphic item;

    public static bool IsMouseOrTouchActive()
    {
        if (Input.anyKeyDown)
            mouseOrTouchEnabled = false;
        return mouseOrTouchEnabled;
    }

    public static bool IsClicked(Graphic t)
    {
        return clickedItem == t && Mathf.Abs(clickedTime - Time.time) < 0.033f;
    }

    public static bool IsHovered(Graphic t)
    {
        return t == hoveredItem;
    }

    private void Awake()
    {
        item = GetComponent<TMP_Text>();
        if (item == null)
            item = GetComponent<Image>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hoveredItem = item;
        mouseOrTouchEnabled = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hoveredItem = null;
        mouseOrTouchEnabled = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        clickedItem = item;
        clickedTime = Time.time;
        mouseOrTouchEnabled = true;
    }

    void Update()
    {
        if (Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0)
            mouseOrTouchEnabled = true;

        if (Input.touches.Length > 0)
            mouseOrTouchEnabled = true;
    }
}
