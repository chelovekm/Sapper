namespace Model.Core;

public class NumberCell : Cell
{
    public override bool IsMine => false;

    public NumberCell(int row, int col) : base(row, col)
    {
    }
}
