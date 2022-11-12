namespace CgeaExperiment.Problems
{
    internal sealed class IsingTorus : IProblem<int>
    {
        private readonly int _rowCount;
        public int Dimension { get; }

        public int? FitnessUpperBound { get; }

        public IsingTorus(int dimension)
        {
            _rowCount = (int)Math.Sqrt(dimension);
            if (_rowCount * _rowCount != dimension)
                throw new ArgumentException("dimension is not a perfect square", nameof(dimension));
            Dimension = dimension;
            FitnessUpperBound = 2 * dimension;
        }

        public int Fitness(byte[] bitString)
        {
            var fitness = 0;
            for (var i = 0; i < _rowCount; i++)
            {
                for (var j = 0; j < _rowCount; j++)
                {
                    var me = bitString[i * _rowCount + j];
                    var up = bitString[(_rowCount + i - 1) % _rowCount * _rowCount + j];
                    var down = bitString[(i + 1) % _rowCount * _rowCount + j];
                    var right = bitString[i * _rowCount + (j + 1) % _rowCount];
                    var left = bitString[i * _rowCount + (_rowCount + j - 1) % _rowCount];

                    fitness += me * up + (1 - me) * (1 - up);
                    fitness += me * down + (1 - me) * (1 - down);
                    fitness += me * right + (1 - me) * (1 - right);
                    fitness += me * left + (1 - me) * (1 - left);
                }
            }

            return fitness / 2;
        }
    }
}