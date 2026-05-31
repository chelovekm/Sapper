namespace Model.Core;

public class FlagCell : Cell
{
    public override bool IsMine => false;

    public FlagCell(int row, int col) : base(row, col)
    {
    }
}
