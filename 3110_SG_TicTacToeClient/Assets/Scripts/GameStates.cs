using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStates : MonoBehaviour
{
    public GameState currentState;

    public GameObject loginPanel;
    public GameObject gameRoomPanel;
    public GameObject gamePlayPanel;

    // Start is called before the first frame update
    void Start()
    {
        SetState(GameState.Login);
    }

    public void SetState(GameState newState)
    {
        currentState = newState;

        // Hide all panels first
        HideAllPanels();

        // Show the appropriate panel based on the current state
        switch (currentState)
        {
            case GameState.Login:
                loginPanel.SetActive(true);
                gameRoomPanel.SetActive(false);
                gamePlayPanel.SetActive(false);
                break;
            case GameState.Game:
                loginPanel.SetActive(false);
                gameRoomPanel.SetActive(true);
                gamePlayPanel.SetActive(false);
                break;
            case GameState.Play:
                loginPanel.SetActive(false);
                gameRoomPanel.SetActive(false);
                gamePlayPanel.SetActive(true);
                break;
        }
    }

    private void HideAllPanels()
    {
        loginPanel.SetActive(false);
        gameRoomPanel.SetActive(false);
    }

    public void OnServerMessageReceived(string message)
    {
        if (message == "LoginSuccess")
        {
            Debug.Log("Changing state...");
            SetState(GameState.Game);
        }
        else if (message == "LoginFailed")
        {
            SetState(GameState.Login);
        }
    }
}
