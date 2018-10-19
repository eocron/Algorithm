using System.Text;

namespace Algorithm.Levenstain
{
    internal sealed class LevenstainMatrix : ILevenstainMatrix
    {
        public int N { get; }
        public int M { get; }
        private readonly float[] _matrix;
        internal LevenstainMatrix(int m, int n)
        {
            N = n;
            M = m;
            _matrix = new float[n*m];
        }

        public float this[int i, int j]
        {
            get => _matrix[i * N + j];
            set => _matrix[i * N + j] = value;
        }

        public override string ToString()
        {
            var b = new StringBuilder();
            for (int i = 0; i < M; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    b.AppendFormat("{0} ", this[i, j]);
                }
                b.AppendLine();
            }
            return b.ToString();
        }
    }
}
