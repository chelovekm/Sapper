namespace Model.Core;

public interface ICell
{
    int Row { get; }
    int Col { get; }
    bool IsRevealed { get; }
    bool IsFlagged { get; }
    bool IsMine { get; }
    int NeighborMines { get; }
    
    void Reveal();
    void ToggleFlag();
    void SetNeighborMines(int count);
}
