using UnityEngine;

public class MouseClick : MonoBehaviour
{
    public App App;
    public int GridIndex;

    public void Start()
    {
        App = gameObject.transform.parent.GetComponent<GameScene>().App;
    }


    private void OnMouseOver()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        var board = gameObject.transform.parent;
        var boardScript = board.GetComponent<TicTacToe>();

        if (boardScript.AvailableSlot(GridIndex)) boardScript.PlayTurn(GridIndex, App.WhosTurn);
    }
}
