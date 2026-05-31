namespace Model.Core;

public class EmptyCell : Cell
{
    public override bool IsMine => false;

    public EmptyCell(int row, int col) : base(row, col)
    {
    }
}
