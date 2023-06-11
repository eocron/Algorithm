namespace Eocron.Algorithms.Levenstain
{
    public interface ILevenstainMatrix
    {
        float this[int i, int j] { get; set; }
        int M { get; }
        int N { get; }
    }
}