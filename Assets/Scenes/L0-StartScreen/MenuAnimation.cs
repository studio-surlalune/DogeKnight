using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuAnimation : MonoBehaviour
{
    enum State
    {
        Empty,
        Title,
        Game,
        Count,
    }

    private State state;

    private readonly float kAnimSpeed = 1.5f;
    private float timeAnim = 0f;

    private Transform[] menus = new Transform[(int)State.Count];
    private TMP_Text pressStart;

    private TMP_Text newGame;
    private TMP_Text options;
    private TMP_Text credits;
    private TMP_Text back;

    // Start is called before the first frame update
    void Start()
    {
        state = State.Empty;
        timeAnim = 0f;

        menus[(int)State.Empty] = transform.Find("MenuEmpty");
        menus[(int)State.Title] = transform.Find("MenuTitle");
        menus[(int)State.Game] = transform.Find("MenuGame");

        pressStart = menus[(int)State.Title].Find("Press Start")?.GetComponent<TMP_Text>();

        newGame = menus[(int)State.Game].Find("New Game")?.GetComponent<TMP_Text>();
        options = menus[(int)State.Game].Find("Options")?.GetComponent<TMP_Text>();
        credits = menus[(int)State.Game].Find("Credits")?.GetComponent<TMP_Text>();
        back = menus[(int)State.Game].Find("Back")?.GetComponent<TMP_Text>();

        foreach (Transform t in menus)
            t?.gameObject.SetActive(false);

        DoTransitionState(State.Title);
    }

    // Update is called once per frame
    void Update()
    {
        ProcessEvents();

        timeAnim += Time.deltaTime;

        switch(state)
        {
            case State.Title: UpdateTitle(); break;
            case State.Game: UpdateGame(); break;
        }
    }

    private void ProcessEvents()
    {

    }

    private void UpdateTitle()
    {
        float alpha = Mathf.Repeat(timeAnim * kAnimSpeed, 2.0f);
        alpha = alpha > 1f ? 2f - alpha : alpha;
        alpha = Mathf.Pow(alpha, 0.5f);
        pressStart.color = new Color(1f, 1f, 1f, alpha);

        if (Input.GetMouseButtonDown(0))
        {
            DoTransitionState(State.Game);
        }
    }

    private void UpdateGame()
    {
        if (UIEventHandler.IsHovered(newGame))
            newGame.fontSize = 60;
        else
            newGame.fontSize = 50;

        if (UIEventHandler.IsHovered(options))
            options.fontSize = 60;
        else
            options.fontSize = 50;

        if (UIEventHandler.IsHovered(credits))
            credits.fontSize = 60;
        else
            credits.fontSize = 50;

        if (UIEventHandler.IsHovered(back))
            back.fontSize = 60;
        else
            back.fontSize = 50;

        if (UIEventHandler.IsClicked(back))
        {
           DoTransitionState(State.Title);
        }
    }

    private void DoTransitionState(State s)
    {
        menus[(int)state].gameObject.SetActive(false);
        menus[(int)s].gameObject.SetActive(true);
        state = s;
    }
}