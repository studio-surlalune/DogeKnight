using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MenuSystem : MonoBehaviour
{
    private enum FadeScreenAnim
    {
        None,
        TransitionIn,
        TransitionOut,
    }

    public enum MenuIndex
    {
        Empty,
        Title,
        Game,
        InGame,
        Pause,
        Count,
    }

    enum KeyIndex
    {
        None = -1,
        Up,
        Down,
        Left,
        Right,
        Count,
    }

    enum ButtonAnim
    {
        None,
        TransitionIn,
        Loop,
        TransitionOut,
    }

     //_____________________________________________________________________________________________
   struct Button
    {
        private const float kAnimSpeed = 5.0f;
        private const float kFontFactor = 1.25f;

        public TMP_Text text;
        public float fontSize;
        public float animTime;
        public ButtonAnim action;

        public void UpdateAnimation(float deltaTime)
        {
            if (action == ButtonAnim.TransitionIn && animTime + deltaTime * kAnimSpeed > 1f)
            {
                action = ButtonAnim.Loop;
                animTime = 1f;
            }
            else if (action == ButtonAnim.TransitionOut && animTime - deltaTime * kAnimSpeed < 0f)
            {
                action = ButtonAnim.None;
                animTime = 0f;
            }

            switch (action)
            {
                case ButtonAnim.None:
                {
                    break;
                }
                case ButtonAnim.TransitionIn:
                {
                    animTime += deltaTime * kAnimSpeed;
                    text.fontSize = Mathf.Lerp(fontSize, fontSize * kFontFactor, animTime);
                    break;
                }
                case ButtonAnim.Loop:
                {
                    text.fontSize = fontSize * kFontFactor;
                    break;
                }
                case ButtonAnim.TransitionOut:
                {
                    animTime -= deltaTime * kAnimSpeed;
                    text.fontSize = Mathf.Lerp(fontSize, fontSize * kFontFactor, animTime);
                    break;
                }
            }
        }
    }

    //_____________________________________________________________________________________________
    interface IMenu
    {
        void Init(MenuSystem system, Transform transform);

        void SetActive(bool active);

        void Enter(MenuSystem system, MenuIndex previousIndex);
        void Exit(MenuSystem system, MenuIndex nextIndex);

        void Update(MenuSystem system, float deltaTime);
    }

    //_____________________________________________________________________________________________

    class EmptyMenu: IMenu
    {
        Transform transform;

        public void Init(MenuSystem system, Transform transform)
        {
            this.transform = transform;
        }

        public void SetActive(bool active)
        {
            transform.gameObject.SetActive(false);
        }

        public void Enter(MenuSystem system, MenuIndex previousIndex)
        {
            transform.gameObject.SetActive(true);
        }

        public void Exit(MenuSystem system, MenuIndex nextIndex)
        {
            transform.gameObject.SetActive(false);
        }

        public void Update(MenuSystem system, float deltaTime)
        {}
    }

    //_____________________________________________________________________________________________

    class TitleMenu: IMenu
    {
        private enum Buttons
        {
            Title,
            PressStart,
            Count,
        }

        private Button[] buttons = new Button[(int)Buttons.Count];
        Transform transform;
        int transitionStep;
        float animTime;

        public void Init(MenuSystem system, Transform transform)
        {
            this.transform = transform;
            transitionStep = 0;
            animTime = 0f;

            buttons[(int)Buttons.Title] = FindButton(transform, "Title");
            buttons[(int)Buttons.PressStart] = FindButton(transform, "Press Start");
        }

        public void SetActive(bool active)
        {
            transform.gameObject.SetActive(false);
        }

        public void Enter(MenuSystem system, MenuIndex previousIndex)
        {
            transform.gameObject.SetActive(true);
            transitionStep = 0;
            animTime = 0f;
        }

        public void Exit(MenuSystem system, MenuIndex nextIndex)
        {
            transform.gameObject.SetActive(false);
            ref Button titleBtn = ref buttons[(int)Buttons.Title];
            ref Button pressStartBtn = ref buttons[(int)Buttons.PressStart];
            titleBtn.text.color = new Color(1f, 1f, 1f, 0f);
            pressStartBtn.text.color = new Color(1f, 1f, 1f, 0f);
        }

        public void Update(MenuSystem system, float deltaTime)
        {
            KeyIndex keyIndex = system.ProcessInputs();

            const float kPressStartAnimSpeed = 1.5f;

            ref Button titleBtn = ref buttons[(int)Buttons.Title];
            ref Button pressStartBtn = ref buttons[(int)Buttons.PressStart];

            if (transitionStep == 0)
            {
                animTime += deltaTime;

                titleBtn.text.color = new Color(1f, 1f, 1f, 0f);
                pressStartBtn.text.color = new Color(1f, 1f, 1f, 0f);

                if (animTime >= 1.2f)
                {
                    transitionStep = 1;
                    animTime = 0f;
                }
            }
            else if (transitionStep == 1)
            {
                animTime += deltaTime;
                float targetTime = 1.5f;

                float alpha = Mathf.Min(animTime / targetTime, 1f);
                titleBtn.text.color = new Color(1f, 1f, 1f, alpha);
                pressStartBtn.text.color = new Color(1f, 1f, 1f, alpha);

                if (animTime >= targetTime)
                {
                    transitionStep = 2;
                    pressStartBtn.animTime = 1f; // make a smooth transition to the next animation
                }
            }
            else
            {
                // Custom animation for "Press Start" button.
                pressStartBtn.animTime += deltaTime;
                float alpha = Mathf.Repeat(pressStartBtn.animTime * kPressStartAnimSpeed, 2.0f);
                alpha = alpha > 1f ? 2f - alpha : alpha;
                alpha = Mathf.Pow(alpha, 0.5f);
                pressStartBtn.text.color = new Color(1f, 1f, 1f, alpha);
            }

            if (system.IsSubmitKey())
            {
                system.DoMenuTransition(MenuIndex.Game);
            }
        }
    }

    //_____________________________________________________________________________________________

    class GameMenu: IMenu
    {
        private enum Buttons
        {
            NewGame,
            Options,
            Credits,
            Back,
            Count,
        }

        private Button[] buttons = new Button[(int)Buttons.Count];
        Transform transform;
        int selectedIndex;
        private Buttons[][] keyMap;

        public void Init(MenuSystem system, Transform transform)
        {
            this.transform = transform;

            buttons[(int)Buttons.NewGame] = FindButton(transform, "New Game");
            buttons[(int)Buttons.Options] = FindButton(transform, "Options");
            buttons[(int)Buttons.Credits] = FindButton(transform, "Credits");
            buttons[(int)Buttons.Back] = FindButton(transform, "Back");

            // KeyIndex [ Up, Down, Left, Right ]
            keyMap = new Buttons[(int)Buttons.Count][]
            {
                new Buttons[] { Buttons.Back, Buttons.Options, Buttons.Count, Buttons.Count }, // New Game
                new Buttons[] { Buttons.NewGame, Buttons.Credits, Buttons.Count, Buttons.Count }, // Options
                new Buttons[] { Buttons.Options, Buttons.Back, Buttons.Count, Buttons.Count }, // Credits
                new Buttons[] { Buttons.Credits, Buttons.NewGame, Buttons.Count, Buttons.Count} // Back
            };
        }

        public void SetActive(bool active)
        {
            transform.gameObject.SetActive(false);
        }

        public void Enter(MenuSystem system, MenuIndex previousIndex)
        {
            transform.gameObject.SetActive(true);
            selectedIndex = (int)Buttons.NewGame;
        }

        public void Exit(MenuSystem system, MenuIndex nextIndex)
        {
            transform.gameObject.SetActive(false);
        }

        public void Update(MenuSystem system, float deltaTime)
        {
            KeyIndex keyIndex = system.ProcessInputs();

            if (keyIndex != KeyIndex.None)
            {
                Buttons nextMenu = keyMap[selectedIndex][(int)keyIndex];
                selectedIndex = nextMenu != Buttons.Count ? (int)nextMenu : selectedIndex;
            }

            selectedIndex = UpdateButtonAnimations(buttons, selectedIndex);

            if (system.IsSubmitted(buttons, (int)Buttons.NewGame, selectedIndex))
            {
                system.DoMenuTransition(MenuIndex.InGame);
                system.DoLevelTransition("L1-Field");
            }
            else if (system.IsSubmitted(buttons, (int)Buttons.Back, selectedIndex) || system.IsCancelKey())
                system.DoMenuTransition(MenuIndex.Title);
        }
    }

    //_____________________________________________________________________________________________

    class InGameMenu: IMenu
    {
        Transform transform;

        TMP_Text hpTitle;
        TMP_Text hpValue;
        TMP_Text mpTitle;
        TMP_Text mpValue;

        public void Init(MenuSystem system, Transform transform)
        {
            this.transform = transform;
            hpTitle = transform.Find("HP")?.GetComponent<TMP_Text>();
            hpValue = transform.Find("HP Value")?.GetComponent<TMP_Text>();
            mpTitle = transform.Find("MP")?.GetComponent<TMP_Text>();
            mpValue = transform.Find("MP Value")?.GetComponent<TMP_Text>();
        }

        public void SetActive(bool active)
        {
            transform.gameObject.SetActive(false);
        }

        public void Enter(MenuSystem system, MenuIndex previousIndex)
        {
            transform.gameObject.SetActive(true);
        }

        public void Exit(MenuSystem system, MenuIndex nextIndex)
        {
            transform.gameObject.SetActive(false);
        }

        public void Update(MenuSystem system, float deltaTime)
        {
            Creature mainChar = Game.dogeKnight;
            if (mainChar != null)
            {
                hpValue.text = $"{mainChar.hp} / {mainChar.hpMax}";
                mpValue.text = $"{mainChar.mp} / {mainChar.mpMax}";
            }
            else
            {
                hpValue.text = "0 / 0";
                mpValue.text = "0 / 0";
            }

            KeyIndex keyIndex = system.ProcessInputs();

            if (system.IsCancelKey())
            {
                system.DoMenuTransition(MenuIndex.Pause);
                Game.TransitionPause(true);
            }
        }
    }

    //_____________________________________________________________________________________________

    class PauseMenu: IMenu
    {
        private enum Buttons
        {
            Resume,
            Exit,
            Count,
        }

        private Button[] buttons = new Button[(int)Buttons.Count];
        Transform transform;
        int selectedIndex;
        private Buttons[][] keyMap;


        public void Init(MenuSystem system, Transform transform)
        {
            this.transform = transform;

            buttons[(int)Buttons.Resume] = FindButton(transform, "Resume");
            buttons[(int)Buttons.Exit] = FindButton(transform, "Exit");

            keyMap = new Buttons[(int)Buttons.Count][]
            {
                new Buttons[] { Buttons.Exit, Buttons.Exit, Buttons.Count, Buttons.Count }, // Resume
                new Buttons[] { Buttons.Resume, Buttons.Resume, Buttons.Count, Buttons.Count }, // Exit
            };
        }

        public void SetActive(bool active)
        {
            transform.gameObject.SetActive(false);
        }

        public void Enter(MenuSystem system, MenuIndex previousIndex)
        {
            transform.gameObject.SetActive(true);
            selectedIndex = (int)Buttons.Resume;
        }

        public void Exit(MenuSystem system, MenuIndex nextIndex)
        {
            transform.gameObject.SetActive(false);
        }

        public void Update(MenuSystem system, float deltaTime)
        {
            KeyIndex keyIndex = system.ProcessInputs();

            if (keyIndex != KeyIndex.None)
            {
                Buttons nextMenu = keyMap[selectedIndex][(int)keyIndex];
                selectedIndex = nextMenu != Buttons.Count ? (int)nextMenu : selectedIndex;
            }

            selectedIndex = UpdateButtonAnimations(buttons, selectedIndex);

            if (system.IsSubmitted(buttons, (int)Buttons.Resume, selectedIndex) || system.IsCancelKey())
            {
                system.DoMenuTransition(MenuIndex.InGame);
                Game.TransitionPause(false);
            }
            else if (system.IsSubmitted(buttons, (int)Buttons.Exit, selectedIndex))
            {
                system.DoMenuTransition(MenuIndex.Title);
                system.DoLevelTransition("L0-StartScreen");
                Game.TransitionPause(false);
            }
        }
    }

    //_____________________________________________________________________________________________

    // Only 1 instance by design.
    public static MenuSystem s_Instance;

    private MenuIndex menuIndex;
    private float keyDelay;
    private bool submitKeyUp;
    private bool cancelKeyUp;

    /// Fade screen to black when transitioning between levels.
    private Image fadeScreen;
    private FadeScreenAnim fadeScreenAnim;
    private float fadeScreenAnimTime;

    private IMenu[] menus = new IMenu[(int)MenuIndex.Count];

    //_____________________________________________________________________________________________

    void Awake()
    {
        s_Instance = this;

        menuIndex = MenuIndex.Empty;
        keyDelay = 0f;

        // Find the fade screen.
        fadeScreen = transform.Find("FadeScreen")?.GetComponent<Image>();
        fadeScreen.color = Color.clear;
        fadeScreen.gameObject.SetActive(false);
        fadeScreenAnim = FadeScreenAnim.None;
        fadeScreenAnimTime = 0f;
        
        // Find all the menus.
        menus[(int)MenuIndex.Empty] = FindMenu(MenuIndex.Empty, "MenuEmpty");
        menus[(int)MenuIndex.Title] = FindMenu(MenuIndex.Title, "MenuTitle");
        menus[(int)MenuIndex.Game] = FindMenu(MenuIndex.Game, "MenuGame");
        menus[(int)MenuIndex.InGame] = FindMenu(MenuIndex.InGame, "MenuInGame");
        menus[(int)MenuIndex.Pause] = FindMenu(MenuIndex.Pause, "MenuPause");

        // Initial menu states.
        foreach (IMenu m in menus)
            m.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        // Update fade screen (if any).
        UpdateFadeScreen();

        menus[(int)menuIndex].Update(this, Time.unscaledDeltaTime);
    }

    public void DoMenuTransition(MenuIndex nextIndex)
    {
        menus[(int)menuIndex].Exit(this, nextIndex);
        menus[(int)nextIndex].Enter(this, menuIndex);
        menuIndex = nextIndex;
    }

    public void DoLevelTransition(string sceneName)
    {
        StartCoroutine(SwitchLevelCoroutine(sceneName));
    }

    public void BeginScreenFadeOut()
    {
        fadeScreenAnim = FadeScreenAnim.TransitionOut;
        fadeScreenAnimTime = 0f;
    }

    public void BeginScreenFadeIn()
    {
        fadeScreenAnim = FadeScreenAnim.TransitionIn;
        fadeScreenAnimTime = 0f;
    }

    public bool IsScreenFading()
    {
        return fadeScreenAnim != FadeScreenAnim.None;
    }

    public void SetScreenFaded(bool faded)
    {
        fadeScreenAnim = FadeScreenAnim.None;
        fadeScreenAnimTime = 0f;
        fadeScreen.color = new Color(0f, 0f, 0f, faded ? 1f : 0f);
    }

    //_____________________________________________________________________________________________

    public void OnButtonClicked_InGame_Escape()
    {
        DoMenuTransition(MenuIndex.Pause);
        Game.TransitionPause(true);
    }

    //_____________________________________________________________________________________________

    private IMenu FindMenu(MenuIndex index, string name)
    {
        Transform t = transform.Find(name);
        if (t == null)
            return null;
        
        IMenu menu;
        switch(index)
        {
            case MenuIndex.Empty: menu = new EmptyMenu(); break;
            case MenuIndex.Title: menu = new TitleMenu(); break;
            case MenuIndex.Game: menu = new GameMenu(); break;
            case MenuIndex.InGame: menu = new InGameMenu(); break;
            case MenuIndex.Pause: menu = new PauseMenu(); break;
            default: return null;
        }

        menu.Init(this, t);
        return menu;
    }

    private static Button FindButton(Transform transform, string name)
    {
        TMP_Text t = transform.Find(name)?.GetComponent<TMP_Text>();
        return new Button
        {
            text = t,
            fontSize = t ? t.fontSize : 50f,
            animTime = 0f,
            action = ButtonAnim.None
        };
    }

    private KeyIndex ProcessInputs()
    {
        // Mainly processing gamepad, keyboard input and mouse click 0.

        const float kKeyDelay = 0.25f;

        keyDelay -= Time.unscaledDeltaTime;
        submitKeyUp = false;
        cancelKeyUp = false;

        if (keyDelay <= 0.0f)
        {
            if (Input.GetAxis("Vertical") > 0.05f)
            {
                keyDelay = kKeyDelay;
                return KeyIndex.Up;
            }
            else if (Input.GetAxis("Vertical") < -0.05f)
            {
                keyDelay = kKeyDelay;
                return KeyIndex.Down;
            }
            else if (Input.GetAxis("Horizontal") < -0.05f)
            {
                keyDelay = kKeyDelay;
                return KeyIndex.Left;
            }
            else if (Input.GetAxis("Horizontal") > 0.05f)
            {
                keyDelay = kKeyDelay;
                return KeyIndex.Right;
            }
            else if (Input.GetButtonUp("Submit") || Input.GetMouseButtonUp(0))
            {
                keyDelay = kKeyDelay;
                submitKeyUp = true;
            }
            else if (Input.GetButtonUp("Cancel") || Input.GetKeyUp(KeyCode.Escape))
            {
                keyDelay = kKeyDelay;
                cancelKeyUp = true;
            }
            else
            {
                keyDelay = 0f;
            }
        }

        return KeyIndex.None;
    }

    private bool IsSubmitKey()
    {
        return submitKeyUp;
    }

    private bool IsCancelKey()
    {
        return cancelKeyUp;
    }

    private bool IsSubmitted(Button[] btns, int index, int selIndex)
    {
        if (UIEventHandler.IsMouseOrTouchActive() && UIEventHandler.IsClicked(btns[index].text)) // Mouse or touch
            return true;
        if (!UIEventHandler.IsMouseOrTouchActive() && index == selIndex && IsSubmitKey()) // gamepad or keyboard
            return true;
        return false;
    }

    private void UpdateFadeScreen()
    {
        const float kFadeScreenAnimSpeed = 1.5f;
        float deltaTime = Time.unscaledDeltaTime;

        switch(fadeScreenAnim)
        {
            case FadeScreenAnim.None:
                break;
            case FadeScreenAnim.TransitionIn:
            {
                fadeScreenAnimTime += deltaTime * kFadeScreenAnimSpeed;
                float alpha = Mathf.Min(fadeScreenAnimTime, 1f);
                alpha = Mathf.Pow(1f - alpha, 1.5f);
                fadeScreen.color = new Color(0f, 0f, 0f, alpha);
                break;
            }
            case FadeScreenAnim.TransitionOut:
            {
                fadeScreenAnimTime += deltaTime * kFadeScreenAnimSpeed;
                float alpha = Mathf.Min(fadeScreenAnimTime, 1f);
                alpha = Mathf.Pow(alpha, 1.5f);
                fadeScreen.color = new Color(0f, 0f, 0f, alpha);
                break;
            }
        }

        bool needActive = (fadeScreen.color.a != 0f);
        if (fadeScreen.gameObject.activeSelf != needActive)
            fadeScreen.gameObject.SetActive(needActive);

        if (fadeScreenAnimTime >= 1f)
        {
            fadeScreenAnim = FadeScreenAnim.None;
            fadeScreenAnimTime = 0f;
        }
    }

    private static int UpdateButtonAnimations(Button[] buttons, int selIndex)
    {
        int hoveredIndex = -1;

        for (int i = 0; i < buttons.Length; ++i)
        {
            ref Button btn = ref buttons[i];

            if ((UIEventHandler.IsMouseOrTouchActive() && UIEventHandler.IsHovered(btn.text)) // mouse or touch
             || !UIEventHandler.IsMouseOrTouchActive() && i == selIndex) // gamepad or keyboard
            {
                if (btn.action == ButtonAnim.None || btn.action == ButtonAnim.TransitionOut)
                    btn.action = ButtonAnim.TransitionIn;

                hoveredIndex = i;
            }
            else
            {
                if (btn.action == ButtonAnim.TransitionIn || btn.action == ButtonAnim.Loop)
                    btn.action = ButtonAnim.TransitionOut;
            }
        }

        // perform animations.        
        float deltaTime = Time.unscaledDeltaTime;
        for (int i = 0; i < buttons.Length; ++i)
        {
            ref Button btn = ref buttons[i];
            btn.UpdateAnimation(deltaTime);
        }

        return hoveredIndex != -1 ? hoveredIndex : selIndex;
    }

    /// Unload current active level and load another one.
    private IEnumerator SwitchLevelCoroutine(string sceneName)
    {
        // Fade-in to black screen.
        BeginScreenFadeOut();

        while (IsScreenFading())
            yield return null; // continue execution after Update phase

        Scene activeScene = SceneManager.GetActiveScene();
        AsyncOperation asyncOp;

        if (activeScene != null && activeScene.name != "UI")
        {
            asyncOp = SceneManager.UnloadSceneAsync(activeScene);
            while (!asyncOp.isDone)
                yield return null; // continue execution after Update phase
        }

        asyncOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        while (!asyncOp.isDone)
            yield return null; // continue execution after Update phase

        Scene loadedScene = SceneManager.GetSceneByName(sceneName);
        if (loadedScene != null) // do assert
        {
            SceneManager.SetActiveScene(loadedScene);

            // Fade-out to black screen.
            BeginScreenFadeIn();
        }
    }
}