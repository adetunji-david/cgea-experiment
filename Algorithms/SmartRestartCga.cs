using CgeaExperiment.Problems;

namespace CgeaExperiment.Algorithms
{
    internal sealed class SmartRestartCga<T> : ISolver<T> where T : struct, IEquatable<T>, IComparable<T>
    {
        private sealed class Solution : IEquatable<Solution>
        {
            public byte[] BitString { get; }
            public T Fitness { get; set; }
            public int EvaluationTime { get; set; }

            public Solution(int dimension)
            {
                BitString = new byte[dimension];
            }

            public bool Equals(Solution? other)
            {
                for (var i = 0; i < BitString.Length; i++)
                {
                    if (BitString[i] != other!.BitString[i]) return false;
                }

                return true;
            }
        }

        private readonly Random _rng;
        private readonly double _budgetFactor;
        private readonly double _updateFactor;
        private readonly IProblem<T> _problem;
        private readonly double[] _marginals;
        private int _hypotheticalPopulationSize, _evaluationCount;

        public byte[] BestBitString { get; }
        public T? BestFitness { get; private set; }

        public SmartRestartCga(IProblem<T> problem, Random rng, double budgetFactor, double updateFactor)
        {
            _rng = rng;
            _problem = problem;
            _budgetFactor = budgetFactor;
            _updateFactor = updateFactor;
            _marginals = new double[problem.Dimension];
            BestBitString = new byte[problem.Dimension];
        }

        public IReadOnlyList<HitEvent<T>> Run(int budget)
        {
            _evaluationCount = 0;
            var hitEvents = new List<HitEvent<T>>();
            _hypotheticalPopulationSize = 2;
            while (_evaluationCount < budget)
            {
                var nextRunBudget = (int)(_budgetFactor * Math.Pow(_hypotheticalPopulationSize, 2));
                var maxEvaluationCount = Math.Min(budget, _evaluationCount + nextRunBudget);
                Array.Fill(_marginals, 0.5);
                var isUpperBoundReached = Core(maxEvaluationCount, hitEvents);
                if (isUpperBoundReached)
                    break;
                _hypotheticalPopulationSize = (int)(_hypotheticalPopulationSize * _updateFactor);
            }

            return hitEvents;
        }

        private bool Core(int maxEvaluationCount, List<HitEvent<T>> hitEvents)
        {
            var solution1 = new Solution(_problem.Dimension);
            var solution2 = new Solution(_problem.Dimension);

            while (_evaluationCount < maxEvaluationCount)
            {
                SampleModel(solution1);
                SampleModel(solution2);
                var fitterSolution = solution1;
                var other = solution2;
                if (solution1.Equals(solution2))
                {
                    solution1.Fitness = _problem.Fitness(solution1.BitString);
                    solution2.Fitness = solution1.Fitness;
                    solution1.EvaluationTime = solution2.EvaluationTime = ++_evaluationCount;
                }
                else
                {
                    solution1.Fitness = _problem.Fitness(solution1.BitString);
                    solution1.EvaluationTime = ++_evaluationCount;
                    solution2.Fitness = _problem.Fitness(solution2.BitString);
                    solution2.EvaluationTime = ++_evaluationCount;
                    if (solution2.Fitness.CompareTo(solution1.Fitness) > 0)
                    {
                        fitterSolution = solution2;
                        other = solution1;
                    }
                }

                if (BestFitness is null || fitterSolution.Fitness.CompareTo(BestFitness.Value) > 0)
                {
                    BestFitness = fitterSolution.Fitness;
                    hitEvents.Add(new HitEvent<T>(fitterSolution.Fitness, fitterSolution.EvaluationTime));
                    Array.Copy(fitterSolution.BitString, BestBitString, BestBitString.Length);
                    if (_problem.FitnessUpperBound.Equals(BestFitness))
                        return true;
                }

                UpdateModel(fitterSolution, other, _hypotheticalPopulationSize);
            }

            return false;
        }

        private void SampleModel(Solution solution)
        {
            var marginals = _marginals;
            var bitstring = solution.BitString;
            for (var j = 0; j < marginals.Length; j++)
            {
                var p = marginals[j];
                bitstring[j] = (byte)(_rng.NextDouble() <= p ? 1 : 0);
            }
        }

        private void UpdateModel(Solution fitterSolution, Solution other, int hypotheticalPopulationSize)
        {
            var delta = 1.0 / hypotheticalPopulationSize;
            var minP = 1.0 / _problem.Dimension;
            var marginals = _marginals;
            var x = fitterSolution.BitString;
            var y = other.BitString;
            for (var i = 0; i < marginals.Length; i++)
            {
                if (x[i] == y[i])
                    continue;
                var sign = 2 * x[i] - 1;
                marginals[i] += sign * delta;
                marginals[i] = Math.Clamp(marginals[i], minP, 1.0 - minP);
            }
        }
    }
}