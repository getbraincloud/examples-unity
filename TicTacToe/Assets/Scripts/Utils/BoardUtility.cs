using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BoardUtility 
{
    public static int[] Grid = new int[9];
    private static int[,] WinningConditions =
    {
        //List of possible winning conditions
        {0, 1, 2},
        {3, 4, 5},
        {6, 7, 8},
        {0, 3, 6},
        {1, 4, 7},
        {2, 5, 8},
        {0, 4, 8},
        {2, 4, 6}
    };
    
    //Generates board to check for completion
    private static void ReadBoardFromState(string board)
    {
        //Clear logical grid
        for (var i = 0; i < Grid.Length; i++)
        {
            Grid[i] = 0;
        }
        //Populate grid based on 'board' parameter
        var j = 0;
        foreach (char boardSlot in board)
        {
            if (boardSlot != '#')
            {
                Grid[j] = boardSlot.ToString() == "X" ? 1 : 2;
            }
            ++j;
        }
    }
    
    // Checks if we have a winner yet.
    // Returns -1 = Game Tied, 0 = No winner yet, 1 = Player1 won, 2 = Player2 won
    public static int CheckForWinner()
    {
        var ourWinner = 0;
        var gameEnded = true;

        for (var i = 0; i < 8; i++)
        {
            int a = WinningConditions[i, 0], b = WinningConditions[i, 1], c = WinningConditions[i, 2];
            int b1 = Grid[a], b2 = Grid[b], b3 = Grid[c];

            if (b1 == 0 || b2 == 0 || b3 == 0)
            {
                gameEnded = false;
                continue;
            }

            if (b1 == b2 && b2 == b3)
            {
                ourWinner = b1;
                break;
            }
        }

        if (gameEnded && ourWinner == 0) ourWinner = -1;

        return ourWinner;
    }
    
    public static bool IsGameCompleted(string board)
    {
        ReadBoardFromState(board);
        bool gameCompleted = false;
        for (int i = 0; i < 8; i++)
        {
            int a = WinningConditions[i, 0], b = WinningConditions[i, 1], c = WinningConditions[i, 2];
            int b1 = Grid[a], b2 = Grid[b], b3 = Grid[c];
            //Checking if this row has been filled
            if (b1 == 0 || b2 == 0 || b3 == 0)
            {
                continue;
            }
            //We have a winner
            if (b1 == b2 && b2 == b3)
            {
                gameCompleted = true;
                break;
            }
            //Game is Tied
            if (i == 7)
            {
                gameCompleted = true;
                break;
            }
        }
        return gameCompleted;
    }
}
