namespace CgeaExperiment.Algorithms
{
    internal struct HitEvent<T>
    {
        public T Fitness;
        public int HittingTime;

        public HitEvent(T fitness, int hittingTime)
        {
            Fitness = fitness;
            HittingTime = hittingTime;
        }
    }

    internal interface ISolver<T> where T : struct, IEquatable<T>, IComparable<T>
    {
        public byte[] BestBitString { get; }
        public T? BestFitness { get; }
        public IReadOnlyList<HitEvent<T>> Run(int budget);
    }
}