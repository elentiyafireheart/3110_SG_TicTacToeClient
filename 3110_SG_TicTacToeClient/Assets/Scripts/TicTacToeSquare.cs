using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Networking.Transport;

public class TicTacToeSquare : MonoBehaviour
{
    #region Variables

    public int row, column, ID;

    public bool diagonal1, diagonal2, isSquareTaken;
    public string icon;
    private const int maxColumns = 3;

    #region Delegate moment

    public delegate void SquarePressedDelegate(TicTacToeSquare squarePressed);
    public event SquarePressedDelegate OnSquarePressed;

    #endregion

    #endregion

    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnPressed);

        #region Init variables on start

        ID = (row * maxColumns) + column;
        diagonal1 = row == column;
        diagonal2 = (row + column) == maxColumns - 1;

        #endregion 
    }

    #region Methods
    public void OnPressed()
    {
        if (OnSquarePressed != null)
        {
            if (!isSquareTaken)
            {
                OnSquarePressed(this);
                isSquareTaken = true;
            }
        }
    }

    public void ClaimSquare(string icon)
    {
        this.icon = icon;
        isSquareTaken = true;
        GetComponentInChildren<TextMeshProUGUI>().text = icon;
        GetComponent<Button>().interactable = false;
    }

    #endregion
}
