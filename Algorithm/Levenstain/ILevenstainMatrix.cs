namespace Algorithm.Levenstain
{
    public interface ILevenstainMatrix
    {
        int N { get; }
        int M { get; }

        float this[int i, int j] { get; set; }
    }
}