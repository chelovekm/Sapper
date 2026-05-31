using System.Xml.Serialization;
using Model.Core;

namespace Model.Data;

public class GameXmlSerializer : ISerializer<GameField>
{
    public void Serialize(GameField data, string filePath)
    {
        var gameState = new GameState
        {
            Rows = data.Rows,
            Cols = data.Cols,
            TotalMines = data.TotalMines,
            MinePercentage = data.MinePercentage,
            ElapsedTime = data.ElapsedTime,
            MinesGenerated = data.MinesGenerated,
            Cells = new List<CellData>()
        };

        for (int i = 0; i < data.Rows; i++)
        {
            for (int j = 0; j < data.Cols; j++)
            {
                var cell = data.GetCell(i, j);
                gameState.Cells.Add(new CellData
                {
                    Row = i,
                    Col = j,
                    CellType = cell.GetType().Name,
                    IsRevealed = cell.IsRevealed,
                    IsFlagged = cell.IsFlagged,
                    NeighborMines = cell.NeighborMines
                });
            }
        }

        var serializer = new System.Xml.Serialization.XmlSerializer(typeof(GameState));
        using var writer = new StreamWriter(filePath);
        serializer.Serialize(writer, gameState);
    }

    public GameField? Deserialize(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        try
        {
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(GameState));
            using var reader = new StreamReader(filePath);
            var gameState = (GameState?)serializer.Deserialize(reader);

            if (gameState == null)
                return null;

            var field = new GameField(gameState.Rows, gameState.Cols, gameState.MinePercentage);
            field.ElapsedTime = gameState.ElapsedTime;
            field.MinesGenerated = gameState.MinesGenerated;

            for (int i = 0; i < gameState.Rows; i++)
            {
                for (int j = 0; j < gameState.Cols; j++)
                {
                    var cellData = gameState.Cells.FirstOrDefault(c => c.Row == i && c.Col == j);
                    if (cellData != null)
                    {
                        Cell cell = cellData.CellType switch
                        {
                            nameof(MineCell) => new MineCell(i, j),
                            nameof(EmptyCell) => new EmptyCell(i, j),
                            nameof(NumberCell) => new NumberCell(i, j),
                            nameof(FlagCell) => new FlagCell(i, j),
                            _ => new EmptyCell(i, j)
                        };

                        if (cellData.IsRevealed)
                            cell.Reveal();
                        if (cellData.IsFlagged)
                            cell.ToggleFlag();
                        cell.SetNeighborMines(cellData.NeighborMines);

                        // Заменяем клетку в поле через reflection
                        var gridField = field.GetType().GetField("_grid", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (gridField != null)
                        {
                            var grid = (Cell[,])gridField.GetValue(field)!;
                            grid[i, j] = cell;
                        }
                    }
                }
            }

            return field;
        }
        catch
        {
            return null;
        }
    }

    public bool IsValidFile(string filePath)
    {
        if (!File.Exists(filePath))
            return false;

        try
        {
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(GameState));
            using var reader = new StreamReader(filePath);
            var gameState = (GameState?)serializer.Deserialize(reader);
            return gameState != null && gameState.Cells != null && gameState.Cells.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    [System.Xml.Serialization.XmlRoot("GameState")]
    public class GameState
    {
        public int Rows { get; set; }
        public int Cols { get; set; }
        public int TotalMines { get; set; }
        public double MinePercentage { get; set; }
        public int ElapsedTime { get; set; }
        public bool MinesGenerated { get; set; }
        [System.Xml.Serialization.XmlElement("Cell")]
        public List<CellData> Cells { get; set; } = null!;
    }

    public class CellData
    {
        public int Row { get; set; }
        public int Col { get; set; }
        public string CellType { get; set; } = null!;
        public bool IsRevealed { get; set; }
        public bool IsFlagged { get; set; }
        public int NeighborMines { get; set; }
    }
}
