namespace Model.Core;

public partial class GameLogic
{
    partial void CheckWinCondition()
    {
        int revealedNonMines = 0;
        int flaggedMines = 0;
        int totalNonMines = (Field.Rows * Field.Cols) - Field.TotalMines;

        for (int i = 0; i < Field.Rows; i++)
        {
            for (int j = 0; j < Field.Cols; j++)
            {
                var cell = Field.GetCell(i, j);
                
                if (cell is MineCell && cell.IsFlagged)
                {
                    flaggedMines++;
                }
                else if (!(cell is MineCell) && cell.IsRevealed)
                {
                    revealedNonMines++;
                }
            }
        }

        // Победа: все мины закрыты флагами, остальные ячейки активированы
        if (flaggedMines == Field.TotalMines && revealedNonMines == totalNonMines)
        {
            IsGameWon = true;
            IsGameOver = true;
        }
    }
}
