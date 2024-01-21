using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Assertions;
using TMPro;

public class MenuSystem : MonoBehaviour
{
    // Transition in/out black screen (mostly when switching scenes/levels).
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
        GameOver,
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
        void Init(Transform transform);

        void SetActive(bool active);

        void Enter(MenuIndex previousIndex);
        void Exit(MenuIndex nextIndex);

        void Update(float deltaTime);
    }

    //_____________________________________________________________________________________________

    /// Initial menu state on start-up.
    class EmptyMenu: IMenu
    {
        Transform transform;

        public void Init(Transform transform)
        {
            this.transform = transform;
        }

        public void SetActive(bool active)
        {
            transform.gameObject.SetActive(false);
        }

        public void Enter(MenuIndex previousIndex)
        {
            transform.gameObject.SetActive(true);
        }

        public void Exit(MenuIndex nextIndex)
        {
            transform.gameObject.SetActive(false);
        }

        public void Update(float deltaTime)
        {}
    }

    //_____________________________________________________________________________________________

    /// Game title menu.
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

        public void Init(Transform transform)
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

        public void Enter(MenuIndex previousIndex)
        {
            transform.gameObject.SetActive(true);
            transitionStep = 0;
            animTime = 0f;
        }

        public void Exit(MenuIndex nextIndex)
        {
            transform.gameObject.SetActive(false);
            ref Button titleBtn = ref buttons[(int)Buttons.Title];
            ref Button pressStartBtn = ref buttons[(int)Buttons.PressStart];
            titleBtn.text.color = new Color(1f, 1f, 1f, 0f);
            pressStartBtn.text.color = new Color(1f, 1f, 1f, 0f);
        }

        public void Update(float deltaTime)
        {
            KeyIndex keyIndex = ProcessInputs();

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

            if (IsSubmitKey())
            {
                DoMenuTransition(MenuIndex.Game);
            }
        }
    }

    //_____________________________________________________________________________________________

    /// New game, continue game, ....
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

        public void Init(Transform transform)
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

        public void Enter(MenuIndex previousIndex)
        {
            transform.gameObject.SetActive(true);
            selectedIndex = (int)Buttons.NewGame;
        }

        public void Exit(MenuIndex nextIndex)
        {
            transform.gameObject.SetActive(false);
        }

        public void Update(float deltaTime)
        {
            KeyIndex keyIndex = ProcessInputs();

            if (keyIndex != KeyIndex.None)
            {
                Buttons nextMenu = keyMap[selectedIndex][(int)keyIndex];
                selectedIndex = nextMenu != Buttons.Count ? (int)nextMenu : selectedIndex;
            }

            selectedIndex = UpdateButtonAnimations(buttons, selectedIndex);

            if (IsSubmitted(buttons, (int)Buttons.NewGame, selectedIndex))
            {
                Game.NewGame();
                DoMenuTransition(MenuIndex.Empty);
                DoLevelTransition("L1-Field", MenuIndex.InGame);
            }
            else if (IsSubmitted(buttons, (int)Buttons.Back, selectedIndex) || IsCancelKey())
                DoMenuTransition(MenuIndex.Title);
        }
    }

    //_____________________________________________________________________________________________

    /// When in actual gameplay.
    class InGameMenu: IMenu
    {
        Transform transform;

        TMP_Text hpTitle;
        TMP_Text hpValue;
        TMP_Text hpMaxValue;
        TMP_Text mpTitle;
        TMP_Text mpValue;
        TMP_Text mpMaxValue;

        // Used for animation HP bar.
        int hpStored;
        float hpAnimTime;
        float hpFontSize;
        // Used for animation MP bar.
        int mpStored;
        float mpAnimTime;
        float mpFontSize;

        Image escapeIcon;

        public void Init(Transform transform)
        {
            this.transform = transform;
            hpTitle = transform.Find("HP")?.GetComponent<TMP_Text>();
            hpValue = transform.Find("HP Value")?.GetComponent<TMP_Text>();
            hpMaxValue = transform.Find("HP Max Value")?.GetComponent<TMP_Text>();
            mpTitle = transform.Find("MP")?.GetComponent<TMP_Text>();
            mpValue = transform.Find("MP Value")?.GetComponent<TMP_Text>();
            mpMaxValue = transform.Find("MP Max Value")?.GetComponent<TMP_Text>();
            escapeIcon = transform.Find("Escape")?.GetComponent<Image>();

            hpFontSize = hpValue.fontSize;
            mpFontSize = mpValue.fontSize;
        }

        public void SetActive(bool active)
        {
            transform.gameObject.SetActive(false);
        }

        public void Enter(MenuIndex previousIndex)
        {
            transform.gameObject.SetActive(true);
            
            Creature player = Game.FindPlayer();
            if (player != null)
            {
                // Reset HP and MP animation bars.
                hpStored = player.stats.hp;
                hpAnimTime = -1f;
                mpStored = player.stats.mp;
                mpAnimTime = -1f;
            }
        }

        public void Exit(MenuIndex nextIndex)
        {
            transform.gameObject.SetActive(false);
        }

        public void Update(float deltaTime)
        {
            Creature player = Game.FindPlayer();
            if (player != null)
            {
                hpValue.text = $"{player.stats.hp}";
                mpValue.text = $"{player.stats.mp}";
                hpMaxValue.text = $"{player.stats.hpMax}";
                mpMaxValue.text = $"{player.stats.mpMax}";

                if (hpStored != player.stats.hp)
                {
                    hpStored = player.stats.hp;
                    hpAnimTime = 0f;
                }

                if (mpStored != player.stats.mp)
                {
                    mpStored = player.stats.mp;
                    mpAnimTime = 0f;
                }

                const float kStatAnimTime = 0.5f;
                if (hpAnimTime >= 0f)
                {
                    hpAnimTime += deltaTime;
                    float s = Mathf.Pow(Mathf.Clamp01(hpAnimTime / kStatAnimTime), 0.5f);
                    hpValue.fontSize = Mathf.Lerp(hpFontSize * 2f, hpFontSize, s);

                    if (hpAnimTime > kStatAnimTime)
                        hpAnimTime = -1f;
                }

                if (mpAnimTime >= 0f)
                {
                    mpAnimTime += deltaTime;
                    float s = Mathf.Pow(Mathf.Clamp01(mpAnimTime / kStatAnimTime), 0.5f);
                    mpValue.fontSize = Mathf.Lerp(mpFontSize * 2f, mpFontSize, s);

                    if (mpAnimTime > kStatAnimTime)
                        mpAnimTime = -1f;
                }

            }
            else
            {
                hpValue.text = "0";
                mpValue.text = "0";
                hpMaxValue.text = "0";
                mpMaxValue.text = "0";
            }

            KeyIndex keyIndex = ProcessInputs();

            if (UIEventHandler.IsMouseOrTouchActive() && UIEventHandler.IsClicked(escapeIcon) // mouse or touch
             || IsCancelKey()) // gamepad, keyboard
            {
                DoMenuTransition(MenuIndex.Pause);
                Game.TransitionPause(true);
            }
        }
    }

    //_____________________________________________________________________________________________

    /// In the gameplay pause menu.
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


        public void Init(Transform transform)
        {
            this.transform = transform;

            buttons[(int)Buttons.Resume] = FindButton(transform, "Resume");
            buttons[(int)Buttons.Exit] = FindButton(transform, "Exit");

            keyMap = new Buttons[(int)Buttons.Count][]
            {
                new Buttons[] { Buttons.Exit, Buttons.Resume, Buttons.Count, Buttons.Count }, // Resume
                new Buttons[] { Buttons.Resume, Buttons.Exit, Buttons.Count, Buttons.Count }, // Exit
            };
        }

        public void SetActive(bool active)
        {
            transform.gameObject.SetActive(false);
        }

        public void Enter(MenuIndex previousIndex)
        {
            transform.gameObject.SetActive(true);
            selectedIndex = (int)Buttons.Resume;
        }

        public void Exit(MenuIndex nextIndex)
        {
            transform.gameObject.SetActive(false);
        }

        public void Update(float deltaTime)
        {
            KeyIndex keyIndex = ProcessInputs();

            if (keyIndex != KeyIndex.None)
            {
                Buttons nextMenu = keyMap[selectedIndex][(int)keyIndex];
                selectedIndex = nextMenu != Buttons.Count ? (int)nextMenu : selectedIndex;
            }

            selectedIndex = UpdateButtonAnimations(buttons, selectedIndex);

            if (IsSubmitted(buttons, (int)Buttons.Resume, selectedIndex) || IsCancelKey())
            {
                DoMenuTransition(MenuIndex.InGame);
                Game.TransitionPause(false);
            }
            else if (IsSubmitted(buttons, (int)Buttons.Exit, selectedIndex))
            {
                Game.EndGame();
                DoMenuTransition(MenuIndex.Empty);
                DoLevelTransition("L0-StartScreen", MenuIndex.Title);
                Game.TransitionPause(false);
            }
        }
    }

    //_____________________________________________________________________________________________

    /// In the gameplay pause menu.
    class GameOverMenu: IMenu
    {
        private enum Buttons
        {
            Retry,
            Exit,
            Count,
        }

        private Button[] buttons = new Button[(int)Buttons.Count];
        Transform transform;
        int selectedIndex;
        private Buttons[][] keyMap;


        public void Init(Transform transform)
        {
            this.transform = transform;

            buttons[(int)Buttons.Retry] = FindButton(transform, "Retry");
            buttons[(int)Buttons.Exit] = FindButton(transform, "Exit");

            keyMap = new Buttons[(int)Buttons.Count][]
            {
                new Buttons[] { Buttons.Exit, Buttons.Retry, Buttons.Count, Buttons.Count }, // Retry
                new Buttons[] { Buttons.Retry, Buttons.Exit, Buttons.Count, Buttons.Count }, // Exit
            };
        }

        public void SetActive(bool active)
        {
            transform.gameObject.SetActive(false);
        }

        public void Enter(MenuIndex previousIndex)
        {
            transform.gameObject.SetActive(true);
            selectedIndex = (int)Buttons.Retry;
        }

        public void Exit(MenuIndex nextIndex)
        {
            transform.gameObject.SetActive(false);
        }

        public void Update(float deltaTime)
        {
            KeyIndex keyIndex = ProcessInputs();

            if (keyIndex != KeyIndex.None)
            {
                Buttons nextMenu = keyMap[selectedIndex][(int)keyIndex];
                selectedIndex = nextMenu != Buttons.Count ? (int)nextMenu : selectedIndex;
            }

            selectedIndex = UpdateButtonAnimations(buttons, selectedIndex);

            if (IsSubmitted(buttons, (int)Buttons.Retry, selectedIndex))
            {
                // We don't have save point, so just restart the game for now.
                Game.TransitionPause(false);
                Game.EndGame();
                Game.NewGame();
                DoMenuTransition(MenuIndex.Empty);
                DoLevelTransition("L1-Field", MenuIndex.InGame);
            }
            else if (IsSubmitted(buttons, (int)Buttons.Exit, selectedIndex))
            {
                Game.TransitionPause(false);
                Game.EndGame();
                DoMenuTransition(MenuIndex.Empty);
                DoLevelTransition("L0-StartScreen", MenuIndex.Title);
            }
        }
    }

    //_____________________________________________________________________________________________

    // Only 1 instance by design.
    public static MenuSystem s_Instance;

    private static MenuIndex menuIndex;
    private static float keyDelay;
    private static bool submitKeyUp;
    private static bool cancelKeyUp;

    /// Fade screen to black when transitioning between levels.
    private static Image fadeScreen;
    private static FadeScreenAnim fadeScreenAnim;
    private static float fadeScreenAnimTime;

    private static IMenu[] menus = new IMenu[(int)MenuIndex.Count];

    //_____________________________________________________________________________________________

    void Awake()
    {
        Assert.IsTrue(s_Instance == null);
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
        menus[(int)MenuIndex.GameOver] = FindMenu(MenuIndex.GameOver, "MenuGameOver");

        // Initial menu states.
        foreach (IMenu m in menus)
            m.SetActive(false);
    }

    // Late update is called once per frame
    // Game state will not be known until LateUpdate has finished, so this script
    // also run at a lower priority to be sure it works with most valid game states.
    void LateUpdate()
    {
        // Update fade screen (if any).
        UpdateFadeScreen();

        menus[(int)menuIndex].Update(Time.unscaledDeltaTime);
    }

    public static void DoMenuTransition(MenuIndex nextIndex)
    {
        menus[(int)menuIndex].Exit(nextIndex);
        menus[(int)nextIndex].Enter(menuIndex);
        menuIndex = nextIndex;
    }

    public static void DoLevelTransition(string sceneName, MenuSystem.MenuIndex menuIndex)
    {
        s_Instance.StartCoroutine(SwitchLevelCoroutine(sceneName, menuIndex));
    }

    public static void BeginScreenFadeOut()
    {
        fadeScreenAnim = FadeScreenAnim.TransitionOut;
        fadeScreenAnimTime = 0f;
    }

    public static void BeginScreenFadeIn()
    {
        fadeScreenAnim = FadeScreenAnim.TransitionIn;
        fadeScreenAnimTime = 0f;
    }

    public static bool IsScreenFading()
    {
        return fadeScreenAnim != FadeScreenAnim.None;
    }

    public static void SetScreenFaded(bool faded)
    {
        fadeScreenAnim = FadeScreenAnim.None;
        fadeScreenAnimTime = 0f;
        fadeScreen.color = new Color(0f, 0f, 0f, faded ? 1f : 0f);
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
            case MenuIndex.GameOver: menu = new GameOverMenu(); break;
            default: Assert.IsTrue(false); return null;
        }

        menu.Init(t);
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

    private static KeyIndex ProcessInputs()
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

    private static bool IsSubmitKey()
    {
        return submitKeyUp;
    }

    private static bool IsCancelKey()
    {
        return cancelKeyUp;
    }

    private static bool IsSubmitted(Button[] btns, int index, int selIndex)
    {
        if (UIEventHandler.IsMouseOrTouchActive() && UIEventHandler.IsClicked(btns[index].text)) // Mouse or touch
            return true;
        if (!UIEventHandler.IsMouseOrTouchActive() && index == selIndex && IsSubmitKey()) // gamepad or keyboard
            return true;
        return false;
    }

    private static void UpdateFadeScreen()
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
    private static IEnumerator SwitchLevelCoroutine(string sceneName, MenuSystem.MenuIndex menuIndex)
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

            DoMenuTransition(menuIndex);
        }
    }
}