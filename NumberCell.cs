namespace Model.Core;

public partial class GameLogic
{
    public GameField Field { get; private set; } = null!;
    public bool IsGameOver { get; set; }
    public bool IsGameWon { get; set; }
    private bool _isFirstClick = true;
    private bool _minesGenerated = false;
    public bool MinesGenerated => _minesGenerated;

    public GameLogic(int rows, int cols, double minePercentage = 0.3)
    {
        Field = new GameField(rows, cols, minePercentage);
        IsGameOver = false;
        IsGameWon = false;
    }

    public GameLogic(GameField field)
    {
        Field = field;
        IsGameOver = false;
        IsGameWon = false;
        _isFirstClick = !field.MinesGenerated;
        _minesGenerated = field.MinesGenerated;
    }

    public bool RevealCell(int row, int col)
    {
        if (IsGameOver || IsGameWon)
            return false;

        var cell = Field.GetCell(row, col);

        if (cell.IsRevealed || cell.IsFlagged)
            return true;

        // Генерация мин при первом клике
        bool wasFirstClick = _isFirstClick;
        if (_isFirstClick)
        {
            Field.GenerateMines(row, col);
            _isFirstClick = false;
            _minesGenerated = true;
            cell = Field.GetCell(row, col); // Обновляем ссылку после генерации
        }

        cell.Reveal();

        if (cell is MineCell)
        {
            IsGameOver = true;
            RevealAllMines();
            return false;
        }

        // Если это был первый клик, открываем соседние клетки
        if (wasFirstClick)
        {
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0) continue;

                    int newRow = row + i;
                    int newCol = col + j;

                    if (IsValidCell(newRow, newCol))
                    {
                        var neighborCell = Field.GetCell(newRow, newCol);
                        if (!neighborCell.IsRevealed && !neighborCell.IsFlagged)
                        {
                            neighborCell.Reveal();
                            if (neighborCell is EmptyCell)
                            {
                                RevealAdjacentCells(newRow, newCol);
                            }
                        }
                    }
                }
            }
        }
        else if (cell is EmptyCell)
        {
            // Для обычных кликов на пустые клетки используем ту же логику, что и при первом клике
            RevealEmptyAreaAround(row, col);
        }

        CheckWinCondition();
        
        // Проверка таймера
        if (IsTimeUp)
        {
            IsGameOver = true;
            IsGameWon = false;
        }
        
        return true;
    }

    private void RevealEmptyAreaAround(int row, int col)
    {
        // Открываем все соседние клетки
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if (i == 0 && j == 0) continue;

                int newRow = row + i;
                int newCol = col + j;

                if (IsValidCell(newRow, newCol))
                {
                    var neighborCell = Field.GetCell(newRow, newCol);
                    if (!neighborCell.IsRevealed && !neighborCell.IsFlagged)
                    {
                        neighborCell.Reveal();
                        
                        // Если соседняя клетка пустая, рекурсивно открываем её соседей
                        if (neighborCell is EmptyCell)
                        {
                            RevealAdjacentCells(newRow, newCol);
                        }
                        // Если соседняя клетка с цифрой, проверяем её соседей на пустые клетки
                        else if (neighborCell is NumberCell)
                        {
                            CheckAndRevealEmptyAround(newRow, newCol);
                        }
                    }
                }
            }
        }
    }

    private void CheckAndRevealEmptyAround(int row, int col)
    {
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if (i == 0 && j == 0) continue;

                int newRow = row + i;
                int newCol = col + j;

                if (IsValidCell(newRow, newCol))
                {
                    var cell = Field.GetCell(newRow, newCol);
                    if (!cell.IsRevealed && !cell.IsFlagged && cell is EmptyCell)
                    {
                        cell.Reveal();
                        RevealAdjacentCells(newRow, newCol);
                    }
                }
            }
        }
    }

    public void ToggleFlag(int row, int col)
    {
        if (IsGameOver || IsGameWon)
            return;

        var cell = Field.GetCell(row, col);
        cell.ToggleFlag();
        
        CheckWinCondition();
    }

    private void RevealAdjacentCells(int row, int col)
    {
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if (i == 0 && j == 0) continue;

                int newRow = row + i;
                int newCol = col + j;

                if (IsValidCell(newRow, newCol))
                {
                    var cell = Field.GetCell(newRow, newCol);
                    if (!cell.IsRevealed && !cell.IsFlagged)
                    {
                        cell.Reveal();
                        if (cell is EmptyCell)
                        {
                            RevealAdjacentCells(newRow, newCol);
                        }
                        else if (cell is NumberCell)
                        {
                            // Открываем границу из клеток с цифрами
                            // Но не рекурсивно продолжаем
                        }
                    }
                }
            }
        }
    }

    public void RevealAllMines()
    {
        for (int i = 0; i < Field.Rows; i++)
        {
            for (int j = 0; j < Field.Cols; j++)
            {
                var cell = Field.GetCell(i, j);
                cell.Reveal();
            }
        }
    }

    public void RemoveAllFlags()
    {
        for (int i = 0; i < Field.Rows; i++)
        {
            for (int j = 0; j < Field.Cols; j++)
            {
                var cell = Field.GetCell(i, j);
                // Удаляем флаги только с клеток без мин
                if (cell.IsFlagged && !(cell is MineCell))
                {
                    cell.ToggleFlag();
                    // Убеждаемся, что клетка открыта
                    cell.Reveal();
                }
            }
        }
    }

    private bool IsValidCell(int row, int col)
    {
        return row >= 0 && row < Field.Rows && col >= 0 && col < Field.Cols;
    }

    partial void CheckWinCondition();
}
