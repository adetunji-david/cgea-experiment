namespace CgeaExperiment.Problems
{
    internal interface IProblem<T> where T : struct, IEquatable<T>, IComparable<T>
    {
        public int Dimension { get; }
        public T? FitnessUpperBound { get; }
        public T Fitness(byte[] bitString);
    }
}