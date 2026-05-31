namespace Model.Core;

public abstract class Cell : ICell
{
    public int Row { get; }
    public int Col { get; }
    public bool IsRevealed { get; protected set; }
    public bool IsFlagged { get; protected set; }
    public abstract bool IsMine { get; }
    public int NeighborMines { get; private set; }

    protected Cell(int row, int col)
    {
        Row = row;
        Col = col;
        IsRevealed = false;
        IsFlagged = false;
        NeighborMines = 0;
    }

    public virtual void Reveal()
    {
        if (!IsFlagged)
        {
            IsRevealed = true;
        }
    }

    public virtual void ToggleFlag()
    {
        if (!IsRevealed)
        {
            IsFlagged = !IsFlagged;
        }
    }

    public void SetNeighborMines(int count)
    {
        NeighborMines = count;
    }
}
