using UnityEngine;

public class MouseClick : MonoBehaviour
{
    public int gridIndex;

    private void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var board = GameObject.Find("TicTacToe_Board");
            var boardScript = board.GetComponent<TicTacToe>();

            if (boardScript.AvailableSlot(gridIndex)) boardScript.PlayTurn(gridIndex, TicTacToe.whosTurn);
        }
    }
}