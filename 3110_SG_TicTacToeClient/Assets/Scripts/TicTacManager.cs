using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TicTacManager : MonoBehaviour
{
    #region Variables 

    [Header("UI Elements")]
    public GameObject squarePrefab;
    public Transform gridParent;
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI player2Text;

    [Header("Debugging purposes")]
    [SerializeField]
    List<TicTacToeSquare> ticTacToeSquares = new List<TicTacToeSquare>();

    [SerializeField]
    private string player1Icon = "X";

    [SerializeField]
    private string player2Icon = "O";

    #region Private Variables

    private NetworkClient networkClient;
    public string currentPlayerIcon;
    private bool isPlayerTurn = true;
    private bool isPlayer1Turn = true;

    #endregion

    #endregion

    void Start()
    {
        networkClient = FindObjectOfType<NetworkClient>();

        #region Set up board and Assign the players to 1 or 2

        InitializeBoard();

        bool isPlayer1 = networkClient.IsPlayer1;
        AssignPlayers(isPlayer1);

        #endregion

        Debug.Log(networkClient.IsPlayer1);

        #region On Game Start - Player 1 always goes first

        if (isPlayer1)
        {
            currentPlayerIcon = player1Icon; // Player 1 starts
            Debug.Log(isPlayer1 + " - isPlayer1");
        }
        else
        {
            currentPlayerIcon = player2Icon; // Player 2 waits
            Debug.Log(isPlayer1 + " - isPlayer2");
        }

        CheckGameState();
        #endregion
    }

    #region Methods
    void InitializeBoard()
    {
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                GameObject square = Instantiate(squarePrefab, gridParent);
                TicTacToeSquare squareScript = square.GetComponent<TicTacToeSquare>();
                squareScript.row = row;
                squareScript.column = col;

                squareScript.OnSquarePressed += HandleSquarePressed;

                ticTacToeSquares.Add(squareScript);
            }
        }
    }

    void HandleSquarePressed(TicTacToeSquare squarePressed)
    {
        if (!isPlayerTurn || squarePressed.isSquareTaken)
            return;

        if (isPlayerTurn)
        {
            squarePressed.ClaimSquare(currentPlayerIcon);

            // Send move to the server
            string msg = $"{ClientServerSignifiers.MakeMove},{squarePressed.row},{squarePressed.column}";
            networkClient.SendMessageToServer(msg);
            if (squarePressed)
            {
                isPlayerTurn = false;

            }
        }

        CheckGameState();
    }

    public void OnOpponentMove(int row, int col)
    {
        string opponentIcon = currentPlayerIcon == "X" ? "O" : "X";

        UpdateBoard(row, col, opponentIcon);

        isPlayerTurn = true;

        CheckGameState();
    }

    public void CheckGameState()
    {
        if (CheckWinCondition())
        {
            Debug.Log($"{currentPlayerIcon} wins!");
            EndGame();
        }
        else if (CheckDrawCondition())
        {
            Debug.Log("It's a draw!");
            EndGame();
        }
        else
        {
            Debug.Log("Checked game state, no wins yet...");
            SwitchTurn();
        }
    }

    public void AssignPlayers(bool isPlayer1)
    {
        if (isPlayer1)
        {
            playerNameText.text = "You are Player 1";
            player2Text.text = "Opponent is Player 2";
        }
        else
        {
            playerNameText.text = "You are Player 2";
            player2Text.text = "Opponent is Player 1";
        }
    }

    void SwitchTurn()
    {
        Debug.Log("Switching ...");

        // Switch the player turn
        if (isPlayer1Turn)
        {
            isPlayer1Turn = false;
        }
        else if (!isPlayer1Turn)
        {
            isPlayer1Turn = true;
        }
        currentPlayerIcon = isPlayer1Turn ? player1Icon : player2Icon;

        // Update UI to reflect whose turn it is
        if (isPlayer1Turn)
        {
            playerNameText.text = "Player 1's Turn";
        }
        else
        {
            playerNameText.text = "Player 2's Turn";

            Debug.Log("waiting for opponent to play");
        }

    }

    public void UpdateBoard(int row, int col, string icon)
    {
        TicTacToeSquare square = ticTacToeSquares.Find(s => s.row == row && s.column == col);

        if (square != null && !square.isSquareTaken)
        {
            square.ClaimSquare(icon); // Updates the UI and marks the square as taken
        }
        else
        {
            Debug.LogWarning($"Attempted to update a square that is already taken or doesn't exist: Row {row}, Col {col}");
        }
    }

    #region Win conditions
    bool CheckWinCondition()
    {
        // Check rows, columns, and diagonals
        for (int i = 0; i < 3; i++)
        {
            if (CheckLine(i, 0, 0, 1) || CheckLine(0, i, 1, 0)) return true;
        }
        return CheckLine(0, 0, 1, 1) || CheckLine(0, 2, 1, -1);
    }

    bool CheckLine(int startRow, int startCol, int rowDir, int colDir)
    {
        string firstIcon = ticTacToeSquares.Find(s => s.row == startRow && s.column == startCol)?.icon;
        if (string.IsNullOrEmpty(firstIcon)) return false;

        for (int i = 1; i < 3; i++)
        {
            TicTacToeSquare square = ticTacToeSquares.Find(s => s.row == startRow + i * rowDir && s.column == startCol + i * colDir);
            if (square == null || square.icon != firstIcon) return false;
        }
        return true;
    }

    bool CheckDrawCondition()
    {
        foreach (TicTacToeSquare square in ticTacToeSquares)
        {
            if (!square.isSquareTaken) return false;
        }
        return true;
    }

    void EndGame()
    {
        foreach (TicTacToeSquare square in ticTacToeSquares)
        {
            square.GetComponent<Button>().interactable = false;
        }
    }

    #endregion

    #endregion
}
