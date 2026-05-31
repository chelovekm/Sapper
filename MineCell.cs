namespace Model.Core;

public class GameField
{
    public int Rows { get; }
    public int Cols { get; }
    public int TotalMines { get; }
    public double MinePercentage { get; }
    public int ElapsedTime { get; set; }
    public bool MinesGenerated { get; set; }

    private Cell[,] _grid = null!;
    private Random _random = null!;

    public Cell[,] Grid => _grid;

    public GameField(int rows, int cols, double minePercentage = 0.3)
    {
        if (minePercentage < 0.2 || minePercentage > 0.4)
        {
            throw new ArgumentException("Mine percentage must be between 20% and 40%");
        }

        Rows = rows;
        Cols = cols;
        MinePercentage = minePercentage;
        TotalMines = (int)(rows * cols * minePercentage);

        InitializeGrid();
    }

    public void GenerateMines(int safeRow, int safeCol)
    {
        PlaceMines(safeRow, safeCol);
        CalculateNeighborMines();
    }

    private void InitializeGrid()
    {
        _grid = new Cell[Rows, Cols];
        _random = new Random();

        for (int i = 0; i < Rows; i++)
        {
            for (int j = 0; j < Cols; j++)
            {
                _grid[i, j] = new EmptyCell(i, j);
            }
        }
    }

    private void PlaceMines(int safeRow, int safeCol)
    {
        int minesPlaced = 0;
        int attempts = 0;
        int maxAttempts = TotalMines * 100;

        while (minesPlaced < TotalMines && attempts < maxAttempts)
        {
            int row = _random.Next(Rows);
            int col = _random.Next(Cols);

            // Не размещать мину на безопасной клетке и вокруг неё
            if (IsNeighborOrSelf(row, col, safeRow, safeCol))
                continue;

            if (!(_grid[row, col] is MineCell))
            {
                // Проверяем, что у мины будет достаточно соседей
                if (HasEnoughNeighbors(row, col))
                {
                    _grid[row, col] = new MineCell(row, col);
                    minesPlaced++;
                }
                else
                {
                    // Если не хватает соседей, прекращаем генерацию
                    break;
                }
            }

            attempts++;
        }

        // После размещения всех мин, проверяем и исправляем мины с недостаточным окружением
        FixMinesWithInsufficientNeighbors(safeRow, safeCol);
    }

    private void FixMinesWithInsufficientNeighbors(int safeRow, int safeCol)
    {
        var minesToFix = new List<(int row, int col)>();

        // Находим все мины с недостаточным окружением
        for (int i = 0; i < Rows; i++)
        {
            for (int j = 0; j < Cols; j++)
            {
                if (_grid[i, j] is MineCell && !HasEnoughNeighbors(i, j))
                {
                    minesToFix.Add((i, j));
                }
            }
        }

        // Перемещаем эти мины в подходящие места или удаляем, если невозможно
        foreach (var (row, col) in minesToFix)
        {
            bool moved = false;
            int attempts = 0;
            int maxAttempts = Rows * Cols * 10;

            while (!moved && attempts < maxAttempts)
            {
                int newRow = _random.Next(Rows);
                int newCol = _random.Next(Cols);

                // Не размещать на безопасной клетке и вокруг неё
                if (IsNeighborOrSelf(newRow, newCol, safeRow, safeCol))
                {
                    attempts++;
                    continue;
                }

                // Не размещать на другой мине
                if (_grid[newRow, newCol] is MineCell)
                {
                    attempts++;
                    continue;
                }

                // Проверяем, что новое место имеет достаточно соседей
                if (HasEnoughNeighbors(newRow, newCol))
                {
                    // Удаляем старую мину
                    _grid[row, col] = new EmptyCell(row, col);
                    // Размещаем новую мину
                    _grid[newRow, newCol] = new MineCell(newRow, newCol);
                    moved = true;
                }

                attempts++;
            }

            // Если не удалось переместить мину, удаляем её
            if (!moved)
            {
                _grid[row, col] = new EmptyCell(row, col);
            }
        }
    }

    private bool IsNeighborOrSelf(int row, int col, int safeRow, int safeCol)
    {
        // Проверяем, является ли клетка самой безопасной или её соседом
        int rowDiff = Math.Abs(row - safeRow);
        int colDiff = Math.Abs(col - safeCol);
        
        return rowDiff <= 1 && colDiff <= 1;
    }

    private bool HasEnoughNeighbors(int row, int col)
    {
        int nonMineNeighborCount = 0;

        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if (i == 0 && j == 0) continue;

                int newRow = row + i;
                int newCol = col + j;

                if (IsValidCell(newRow, newCol) && !(_grid[newRow, newCol] is MineCell))
                {
                    nonMineNeighborCount++;
                }
            }
        }

        return nonMineNeighborCount >= 3;
    }

    private void CalculateNeighborMines()
    {
        for (int i = 0; i < Rows; i++)
        {
            for (int j = 0; j < Cols; j++)
            {
                if (!(_grid[i, j] is MineCell))
                {
                    int neighborMines = CountNeighborMines(i, j);
                    _grid[i, j].SetNeighborMines(neighborMines);

                    if (neighborMines > 0)
                    {
                        _grid[i, j] = new NumberCell(i, j);
                        _grid[i, j].SetNeighborMines(neighborMines);
                    }
                }
            }
        }
    }

    private int CountNeighborMines(int row, int col)
    {
        int count = 0;

        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if (i == 0 && j == 0) continue;

                int newRow = row + i;
                int newCol = col + j;

                if (IsValidCell(newRow, newCol) && _grid[newRow, newCol] is MineCell)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private bool IsValidCell(int row, int col)
    {
        return row >= 0 && row < Rows && col >= 0 && col < Cols;
    }

    public Cell GetCell(int row, int col)
    {
        if (!IsValidCell(row, col))
            throw new ArgumentOutOfRangeException("Invalid cell coordinates");

        return _grid[row, col];
    }
}
