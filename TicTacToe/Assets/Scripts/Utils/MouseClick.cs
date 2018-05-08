using UnityEngine;

public class MouseClick : MonoBehaviour
{
    public App app;
    public int gridIndex;

    public void Start()
    {
        app = gameObject.transform.parent.GetComponent<GameScene>().app;
    }


    private void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var board = gameObject.transform.parent;
            var boardScript = board.GetComponent<TicTacToe>();

            if (boardScript.AvailableSlot(gridIndex)) boardScript.PlayTurn(gridIndex, app.whosTurn);
        }
    }
}