namespace CgeaExperiment.Problems
{
    internal sealed class IsingRing : IProblem<int>
    {
        public int Dimension { get; }

        public int? FitnessUpperBound { get; }

        public IsingRing(int dimension)
        {
            Dimension = dimension;
            FitnessUpperBound = dimension;
        }

        public int Fitness(byte[] bitString)
        {
            var fitness = 0;
            var x = bitString[0];
            var y = bitString[^1];
            fitness += x * y + (1 - x) * (1 - y);
            for (var i = 0; i < bitString.Length - 1; i++)
            {
                x = bitString[i];
                y = bitString[i + 1];
                fitness += x * y + (1 - x) * (1 - y);
            }

            return fitness;
        }
    }
}