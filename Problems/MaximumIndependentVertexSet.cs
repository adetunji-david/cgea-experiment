namespace CgeaExperiment.Problems
{
    internal sealed class MaximumIndependentVertexSet : IProblem<int>
    {
        public int Dimension { get; }

        public int? FitnessUpperBound { get; }

        public MaximumIndependentVertexSet(int dimension)
        {
            if (dimension % 2 == 1)
                throw new ArgumentException("dimension is not even", nameof(dimension));
            if (dimension < 4)
                throw new ArgumentException("dimension is smaller than 4", nameof(dimension));
            Dimension = dimension;
            var target = dimension / 2;
            if (target % 2 == 1) target++;
            FitnessUpperBound = target;
        }

        public int Fitness(byte[] bitString)
        {
            var fitness = 0;
            var n = bitString.Length;
            var hn = n / 2;
            for (var i = 0; i < bitString.Length; i++)
            {
                fitness += bitString[i];
                if (bitString[i] is 0) continue;
                for (var j = 0; j < bitString.Length; j++)
                {
                    if (bitString[j] is 0) continue;
                    if (j == i + 1 && i <= n - 2 && i != hn - 1)
                        fitness -= n;
                    else if (j == i + hn + 1 && i <= hn - 2)
                        fitness -= n;
                    else if (j == i + hn - 1 && i <= hn - 1 && i > 0)
                        fitness -= n;
                }
            }

            return fitness;
        }
    }
}