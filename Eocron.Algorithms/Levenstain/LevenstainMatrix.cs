using System.Text;

namespace Eocron.Algorithms.Levenstain
{
    internal sealed class LevenstainMatrix : ILevenstainMatrix
    {
        internal LevenstainMatrix(int m, int n)
        {
            N = n;
            M = m;
            _matrix = new float[n * m];
        }

        public override string ToString()
        {
            var b = new StringBuilder();
            for (var i = 0; i < M; i++)
            {
                for (var j = 0; j < N; j++) b.AppendFormat("{0} ", this[i, j]);
                b.AppendLine();
            }

            return b.ToString();
        }

        public float this[int i, int j]
        {
            get => _matrix[i * N + j];
            set => _matrix[i * N + j] = value;
        }

        public int M { get; }
        public int N { get; }
        private readonly float[] _matrix;
    }
}