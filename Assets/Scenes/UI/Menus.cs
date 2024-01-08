using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class Menus : MonoBehaviour
{
    private enum FadeScreenAnim
    {
        None,
        TransitionIn,
        TransitionOut,
    }

    public enum MenuState
    {
        Empty,
        Title,
        Game,
        InGame,
        Pause,
        Count,
    }

    private enum MenuTitle
    {
        Title,
        PressStart,
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

   private enum MenuPause
    {
        Resume,
        Exit,
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

    // Only 1 instance by design.
    public static Menus s_Instance;

    private MenuState state;
    private float keyDelay;
    private bool submitKeyUp;
    private bool cancelKeyUp;

    /// Fade screen to black when transitioning between levels.
    private Image fadeScreen;
    private FadeScreenAnim fadeScreenAnim;
    private float fadeScreenAnimTime;

    private Transform[] menus = new Transform[(int)MenuState.Count];

    private Button[] titleBtns = new Button[(int)MenuTitle.Count];
    private Button[] gameBtns = new Button[(int)MenuGame.Count];
    private Button[] pauseBtns = new Button[(int)MenuPause.Count];

    int gameSelectedIndex;
    private MenuGame[][] gameKeyMaps;

    int pauseSelectedIndex;
    private MenuPause[][] pauseKeyMaps;

    void Awake()
    {
        s_Instance = this;

        state = MenuState.Empty;
        keyDelay = 0f;

        // Find the fade screen.
        fadeScreen = transform.Find("FadeScreen")?.GetComponent<Image>();
        fadeScreen.color = Color.clear;
        fadeScreen.gameObject.SetActive(false);
        fadeScreenAnim = FadeScreenAnim.None;
        fadeScreenAnimTime = 0f;
        
        // Find all the menus.
        menus[(int)MenuState.Empty] = transform.Find("MenuEmpty");
        menus[(int)MenuState.Title] = transform.Find("MenuTitle");
        menus[(int)MenuState.Game] = transform.Find("MenuGame");
        menus[(int)MenuState.InGame] = transform.Find("MenuInGame");
        menus[(int)MenuState.Pause] = transform.Find("MenuPause");

        // Find all the buttons.
        titleBtns[(int)MenuTitle.Title] = FindButton(MenuState.Title, "Title");
        titleBtns[(int)MenuTitle.PressStart] = FindButton(MenuState.Title, "Press Start");
        //
        gameBtns[(int)MenuGame.NewGame] = FindButton(MenuState.Game, "New Game");
        gameBtns[(int)MenuGame.Options] = FindButton(MenuState.Game, "Options");
        gameBtns[(int)MenuGame.Credits] = FindButton(MenuState.Game, "Credits");
        gameBtns[(int)MenuGame.Back] = FindButton(MenuState.Game, "Back");
        //
        pauseBtns[(int)MenuPause.Resume] = FindButton(MenuState.Pause, "Resume");
        pauseBtns[(int)MenuPause.Exit] = FindButton(MenuState.Pause, "Exit");

        // Initial menu states.
        foreach (Transform t in menus)
            t.gameObject.SetActive(false);

        // KeyIndex [ Up, Down, Left, Right ]
        gameKeyMaps = new MenuGame[(int)MenuGame.Count][]
        {
            new MenuGame[] { MenuGame.Back, MenuGame.Options, MenuGame.Count, MenuGame.Count }, // New Game
            new MenuGame[] { MenuGame.NewGame, MenuGame.Credits, MenuGame.Count, MenuGame.Count }, // Options
            new MenuGame[] { MenuGame.Options, MenuGame.Back, MenuGame.Count, MenuGame.Count }, // Credits
            new MenuGame[] { MenuGame.Credits, MenuGame.NewGame, MenuGame.Count, MenuGame.Count} // Back
       };

        pauseKeyMaps = new MenuPause[(int)MenuPause.Count][]
        {
            new MenuPause[] { MenuPause.Exit, MenuPause.Exit, MenuPause.Count, MenuPause.Count }, // Resume
            new MenuPause[] { MenuPause.Resume, MenuPause.Resume, MenuPause.Count, MenuPause.Count }, // Exit
       };

    }

    // Update is called once per frame
    void Update()
    {
        // Update fade screen (if any).
        UpdateFadeScreen();

        switch(state)
        {
            case MenuState.Title: UpdateTitle(); break;
            case MenuState.Game: UpdateGame(); break;
            case MenuState.InGame: UpdateInGame(); break;
            case MenuState.Pause: UpdatePause(); break;
        }
    }

    public void DoMenuTransition(MenuState s)
    {
        menus[(int)state].gameObject.SetActive(false);
        menus[(int)s].gameObject.SetActive(true);
        state = s;
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

    private Button FindButton(MenuState menu, string name)
    {
        TMP_Text t = menus[(int)menu].Find(name)?.GetComponent<TMP_Text>();
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

        keyDelay -= Time.deltaTime;
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

        switch(fadeScreenAnim)
        {
            case FadeScreenAnim.None:
                break;
            case FadeScreenAnim.TransitionIn:
            {
                fadeScreenAnimTime += Time.deltaTime * kFadeScreenAnimSpeed;
                float alpha = Mathf.Min(fadeScreenAnimTime, 1f);
                alpha = Mathf.Pow(1f - alpha, 1.5f);
                fadeScreen.color = new Color(0f, 0f, 0f, alpha);
                break;
            }
            case FadeScreenAnim.TransitionOut:
            {
                fadeScreenAnimTime += Time.deltaTime * kFadeScreenAnimSpeed;
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

    private void UpdateTitle()
    {
        KeyIndex keyIndex = ProcessInputs();

        const float kPressStartAnimSpeed = 1.5f;

        ref Button pressStartBtn = ref titleBtns[(int)MenuTitle.PressStart];

        // Custom animation for "Press Start" button.
        pressStartBtn.animTime += Time.deltaTime;
        float alpha = Mathf.Repeat(pressStartBtn.animTime * kPressStartAnimSpeed, 2.0f);
        alpha = alpha > 1f ? 2f - alpha : alpha;
        alpha = Mathf.Pow(alpha, 0.5f);
        pressStartBtn.text.color = new Color(1f, 1f, 1f, alpha);

        if (IsSubmitKey())
        {
            DoMenuTransition(MenuState.Game);
            gameSelectedIndex = (int)MenuGame.NewGame;
        }
    }

    private void UpdateGame()
    {
        KeyIndex keyIndex = ProcessInputs();

        if (keyIndex != KeyIndex.None)
        {
            MenuGame nextMenu = gameKeyMaps[gameSelectedIndex][(int)keyIndex];
            gameSelectedIndex = nextMenu != MenuGame.Count ? (int)nextMenu : gameSelectedIndex;
        }

        gameSelectedIndex = UpdateButtonAnimations(gameBtns, gameSelectedIndex);

        if (IsSubmitted(gameBtns, (int)MenuGame.NewGame, gameSelectedIndex))
        {
            DoMenuTransition(MenuState.InGame);
            DoLevelTransition("L1-Field");
        }
        else if (IsSubmitted(gameBtns, (int)MenuGame.Back, gameSelectedIndex) || IsCancelKey())
            DoMenuTransition(MenuState.Title);
    }

    private void UpdateInGame()
    {
        KeyIndex keyIndex = ProcessInputs();

        if (IsCancelKey())
        {
            DoMenuTransition(MenuState.Pause);
            pauseSelectedIndex = (int)MenuPause.Resume;
        }
    }

    private void UpdatePause()
    {
        KeyIndex keyIndex = ProcessInputs();

        if (keyIndex != KeyIndex.None)
        {
            MenuPause nextMenu = pauseKeyMaps[pauseSelectedIndex][(int)keyIndex];
            pauseSelectedIndex = nextMenu != MenuPause.Count ? (int)nextMenu : pauseSelectedIndex;
        }

        pauseSelectedIndex = UpdateButtonAnimations(pauseBtns, pauseSelectedIndex);

        if (IsSubmitted(pauseBtns, (int)MenuPause.Resume, pauseSelectedIndex) || IsCancelKey())
        {
            DoMenuTransition(MenuState.InGame);
        }
        else if (IsSubmitted(pauseBtns, (int)MenuPause.Exit, pauseSelectedIndex))
        {
            DoMenuTransition(MenuState.Title);
            DoLevelTransition("L0-StartScreen");
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
        float deltaTime = Time.deltaTime;
        for (int i = 0; i < buttons.Length; ++i)
        {
            ref Button btn = ref buttons[i];
            btn.UpdateAnimation(deltaTime);
        }

        return hoveredIndex != -1 ? hoveredIndex : selIndex;
    }

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