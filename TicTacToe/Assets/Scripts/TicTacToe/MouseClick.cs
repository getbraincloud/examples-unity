using UnityEngine;
using System.Collections;

public class MouseClick : MonoBehaviour {
	public int gridIndex;
	
	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
	}
	
	void OnMouseOver()
	{
		if (Input.GetMouseButtonDown(0))
		{
			GameObject board = GameObject.Find("TicTacToe_Board");
			TicTacToe boardScript = board.GetComponent<TicTacToe>();

			if(boardScript.AvailableSlot(gridIndex))
			{
				boardScript.PlayTurn(gridIndex, TicTacToe.whosTurn);
			}
		}
	}
}
