using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuAnimation : MonoBehaviour
{
    public enum MenuState
    {
        Empty,
        Title,
        Game,
        Count,
    }

    private enum MenuGame
    {
        NewGame,
        Options,
        Credits,
        Back,
        Count,
    }

    enum AnimAction
    {
        None,
        TransitionIn,
        Loop,
        TransitionOut,
    }

    struct Button
    {
        private const float kAnimSpeed = 5.0f;
        private const float kFontFactor = 1.25f;

        public TMP_Text text;
        public float fontSize;
        public float animTime;
        public AnimAction action;

        public void Update(float deltaTime)
        {
            if (action == AnimAction.TransitionIn && animTime + deltaTime * kAnimSpeed > 1f)
            {
                action = AnimAction.Loop;
                animTime = 1f;
            }
            else if (action == AnimAction.TransitionOut && animTime - deltaTime * kAnimSpeed < 0f)
            {
                action = AnimAction.None;
                animTime = 0f;
            }

            switch (action)
            {
                case AnimAction.None:
                {
                    break;
                }
                case AnimAction.TransitionIn:
                {
                    animTime += deltaTime * kAnimSpeed;
                    text.fontSize = Mathf.Lerp(fontSize, fontSize * kFontFactor, animTime);
                    break;
                }
                case AnimAction.Loop:
                {
                    text.fontSize = fontSize * kFontFactor;
                    break;
                }
                case AnimAction.TransitionOut:
                {
                    animTime -= deltaTime * kAnimSpeed;
                    text.fontSize = Mathf.Lerp(fontSize, fontSize * kFontFactor, animTime);
                    break;
                }
            }
        }
    }

    // Only 1 instance by design.
    public static MenuAnimation s_Instance;

    private MenuState state;

    private const float kPressStartAnimSpeed = 1.5f;
    private float pressStartAnimTime = 0f;

    private Transform[] menus = new Transform[(int)MenuState.Count];

    private Button pressStartBtn;

    private Button[] gameBtns = new Button[(int)MenuGame.Count];

    // Start is called before the first frame update
    void Start()
    {
        s_Instance = this;

        state = MenuState.Empty;
        pressStartAnimTime = 0f;

        // Find all the menus.
        menus[(int)MenuState.Empty] = transform.Find("MenuEmpty");
        menus[(int)MenuState.Title] = transform.Find("MenuTitle");
        menus[(int)MenuState.Game] = transform.Find("MenuGame");

        // Find all the buttons.
        pressStartBtn = FindButton(MenuState.Title, "Press Start");

        gameBtns[(int)MenuGame.NewGame] = FindButton(MenuState.Game, "New Game");
        gameBtns[(int)MenuGame.Options] = FindButton(MenuState.Game, "Options");
        gameBtns[(int)MenuGame.Credits] = FindButton(MenuState.Game, "Credits");
        gameBtns[(int)MenuGame.Back] = FindButton(MenuState.Game, "Back");

        // Initial menu states.
        foreach (Transform t in menus)
            t.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        switch(state)
        {
            case MenuState.Title: UpdateTitle(); break;
            case MenuState.Game: UpdateGame(); break;
        }
    }

    public void DoMenuTransition(MenuState s)
    {
        menus[(int)state].gameObject.SetActive(false);
        menus[(int)s].gameObject.SetActive(true);
        state = s;
    }

    private Button FindButton(MenuState menu, string name)
    {
        TMP_Text t = menus[(int)menu].Find(name)?.GetComponent<TMP_Text>();
        return new Button
        {
            text = t,
            fontSize = t ? t.fontSize : 50f,
            animTime = 0f,
            action = AnimAction.None
        };
    }

    private void UpdateTitle()
    {
        pressStartAnimTime += Time.deltaTime;

        float alpha = Mathf.Repeat(pressStartAnimTime * kPressStartAnimSpeed, 2.0f);
        alpha = alpha > 1f ? 2f - alpha : alpha;
        alpha = Mathf.Pow(alpha, 0.5f);
        pressStartBtn.text.color = new Color(1f, 1f, 1f, alpha);

        if (Input.GetMouseButtonDown(0))
            DoMenuTransition(MenuState.Game);
    }

    private void UpdateGame()
    {
        float deltaTime = Time.deltaTime;

        for(int i = 0; i < gameBtns.Length; ++i)
        {
            ref Button btn = ref gameBtns[i];

            if (UIEventHandler.IsHovered(btn.text))
            {
                if (btn.action == AnimAction.None || btn.action == AnimAction.TransitionOut)
                    btn.action = AnimAction.TransitionIn;
            }
            else
            {
                if (btn.action == AnimAction.TransitionIn || btn.action == AnimAction.Loop)
                    btn.action = AnimAction.TransitionOut;
            }
        }

        if (UIEventHandler.IsClicked(gameBtns[(int)MenuGame.Back].text))
           DoMenuTransition(MenuState.Title);

        // perform animations.        
        for(int i = 0; i < gameBtns.Length; ++i)
        {
            ref Button btn = ref gameBtns[i];
            btn.Update(deltaTime);
        }
    }
}