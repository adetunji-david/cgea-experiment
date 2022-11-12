using System.Diagnostics;
using System.Runtime.CompilerServices;
using CgeaExperiment.Problems;
using CgeaExperiment.Utils;

namespace CgeaExperiment.Algorithms
{
    internal sealed class PartialRestartCgea<T> : ISolver<T> where T : struct, IComparable<T>, IEquatable<T>
    {
        private sealed class Solution
        {
            private const int HashSeed = unchecked((int)2166136261);
            private const int P = 16777619;
            private int _hashCode;
            public Solution? Next;
            public Solution? EvaluatedCopy;

            public bool IsEvaluated => EvaluatedCopy == this;

            public byte[] BitString { get; }
            public T Fitness { get; set; }
            public double Utility { get; set; }
            public int EvaluationTime { get; set; }


            public Solution(int dimension)
            {
                BitString = new byte[dimension];
            }

            public void ReInitialize()
            {
                Utility = 0;
                Next = null;
                EvaluatedCopy = null;
                EvaluationTime = 0;
            }

            public bool Equals(Solution other)
            {
                for (var i = 0; i < BitString.Length; i++)
                {
                    if (BitString[i] != other.BitString[i]) return false;
                }

                return true;
            }

            public override int GetHashCode()
            {
                // ReSharper disable once NonReadonlyMemberInGetHashCode
                return _hashCode;
            }

            public void ComputeHashCode()
            {
                unchecked
                {
                    _hashCode = HashSeed;
                    foreach (var t in BitString)
                    {
                        _hashCode = _hashCode * P + t;
                    }
                }
            }
        }

        private sealed class SolutionComparer : Comparer<Solution>
        {
            public override int Compare(Solution? x, Solution? y)
            {
                if (x is null) return 1;
                if (y is null) return -1;
                if (ReferenceEquals(x, y)) return 0;
                return -x.Fitness.CompareTo(y.Fitness);
            }
        }

        private readonly Random _rng;
        private readonly IProblem<T> _problem;
        private readonly double _resetProbability;
        private readonly Comparer<Solution> _comparer;
        private readonly Solution[] _buckets, _solutions;
        private readonly int _bucketCount, _populationSize;
        private readonly double[] _marginals, _gradients, _startingDistribution;
        private int _winningIndex, _evaluationCount;

        public byte[] BestBitString { get; }
        public T? BestFitness { get; private set; }

        public PartialRestartCgea(IProblem<T> problem, Random rng, int populationSize, double resetProbability)
        {
            _rng = rng;
            _resetProbability = resetProbability;
            _problem = problem;
            _populationSize = populationSize;
            _solutions = new Solution[2 * _populationSize];
            for (var i = 0; i < _solutions.Length; i++)
                _solutions[i] = new Solution(_problem.Dimension);
            _bucketCount = HashHelpers.GetPrime(_solutions.Length + 1);
            _buckets = new Solution[_bucketCount];
            _comparer = new SolutionComparer();
            _marginals = new double[problem.Dimension];
            _gradients = new double[problem.Dimension];
            _startingDistribution = new double[problem.Dimension];
            BestFitness = null;
            BestBitString = new byte[problem.Dimension];
        }

        public IReadOnlyList<HitEvent<T>> Run(int budget)
        {
            _evaluationCount = 0;
            var hitEvents = new List<HitEvent<T>>();
            Array.Fill(_startingDistribution, 0.5);
            while (_evaluationCount < budget)
            {
                Array.Copy(_startingDistribution, _marginals, _problem.Dimension);
                var isUpperBoundReached = Core(budget, hitEvents);
                if (isUpperBoundReached)
                    break;
                UpdateStartingDistribution(BestBitString);
            }

            return hitEvents;
        }

        private bool Core(int budget, List<HitEvent<T>> hitEvents)
        {
            var exhaustiveEnumerationThreshold = Math.Log2(_populationSize);
            var parameterCount = _marginals.Count(p => p is not 0 and not 1);
            while (parameterCount-- > 0 && _evaluationCount < budget)
            {
                Array.Fill(_buckets, null);
                if (parameterCount <= exhaustiveEnumerationThreshold)
                {
                    var newPopulationSize = ExhaustiveEnumeration(parameterCount);
                    ComputeFitnesses(0, newPopulationSize);
                    _solutions.AsSpan(0, newPopulationSize).Sort(_comparer);
                    parameterCount = 0;
                }
                else
                {
                    GenerateSolutions();
                    ComputeFitnesses(0, _populationSize);
                    ComputeUtilities();
                    SelectMarginal();
                    LocalSearch();
                    UpdateSelectedMarginal();
                }

                var iterBest = _solutions[0];
                var comparison = BestFitness is null ? 1 : iterBest.Fitness.CompareTo(BestFitness.Value);
                switch (comparison)
                {
                    case 0:
                        Array.Copy(iterBest.BitString, BestBitString, _problem.Dimension);
                        break;
                    case > 0:
                    {
                        BestFitness = iterBest.Fitness;
                        hitEvents.Add(new HitEvent<T>(iterBest.Fitness, iterBest.EvaluationTime));
                        Array.Copy(iterBest.BitString, BestBitString, _problem.Dimension);
                        if (_problem.FitnessUpperBound.Equals(BestFitness))
                            return true;
                        break;
                    }
                }
            }

            return false;
        }

        private int ExhaustiveEnumeration(int parameterCount)
        {
            var newPopulationSize = (int)Math.Pow(2, parameterCount);
            Trace.Assert(newPopulationSize <= _populationSize);
            Parallel.For(0, newPopulationSize, i =>
            {
                var solution = _solutions[i];
                solution.ReInitialize();
                var bitstring = solution.BitString;
                for (var j = 0; j < _marginals.Length; j++)
                {
                    var p = _marginals[j];
                    if (p is 0 or 1)
                    {
                        bitstring[j] = (byte)p;
                    }
                    else
                    {
                        bitstring[j] = (byte)(i & 1);
                        i >>= 1;
                    }
                }

                solution.ComputeHashCode();
            });
            return newPopulationSize;
        }

        private void GenerateSolutions()
        {
            for (var i = 0; i < _populationSize; i++)
            {
                _solutions[i].ReInitialize();
                var bitstring = _solutions[i].BitString;
                for (var j = 0; j < _marginals.Length; j++)
                {
                    bitstring[j] = (byte)(_rng.NextDouble() <= _marginals[j] ? 1 : 0);
                }

                _solutions[i].ComputeHashCode();
            }
        }

        private void ComputeFitnesses(int startIndex, int length)
        {
            Parallel.For(startIndex, startIndex + length, i =>
            {
                var solution = _solutions[i];
                var hashCode = solution.GetHashCode();
                var bucketIndex = GetBucketIndex(hashCode);
                var next = Interlocked.CompareExchange(ref _buckets[bucketIndex], solution, null);
                while (next is not null)
                {
                    if (hashCode == next.GetHashCode() && solution.Equals(next))
                    {
                        solution.EvaluatedCopy = next;
                        return;
                    }

                    next = Interlocked.CompareExchange(ref next.Next, solution, null);
                }

                solution.Fitness = _problem.Fitness(solution.BitString);
                solution.EvaluationTime = Interlocked.Increment(ref _evaluationCount);
                solution.EvaluatedCopy = solution;
            });
            for (var i = startIndex + length - 1; i >= startIndex; i--)
            {
                var s = _solutions[i];
                if (s.IsEvaluated)
                    continue;
                while (!s.IsEvaluated)
                    s = s.EvaluatedCopy!;
                var f = s.Fitness;
                var t = s.EvaluationTime;
                s = _solutions[i];
                while (!s.IsEvaluated)
                {
                    s.Fitness = f;
                    s.EvaluationTime = t;
                    Helpers.Swap(ref s, ref s.EvaluatedCopy!);
                }
            }
        }

        private void ComputeUtilities()
        {
            _solutions.AsSpan(0, _populationSize).Sort(_comparer);
            var i = 0;
            for (; i < _populationSize; i++)
            {
                _solutions[i].Utility = 1.0 / (i + 1);
            }

            i = 0;
            while (i < _populationSize - 1)
            {
                if (!_solutions[i].Fitness.Equals(_solutions[i + 1].Fitness))
                {
                    i++;
                }
                else
                {
                    var j = i + 1;
                    var f = _solutions[i].Fitness;
                    while (j < _populationSize && f.Equals(_solutions[j].Fitness))
                        j++;
                    var u = _solutions[j - 1].Utility;
                    for (var k = i; k < j; k++)
                        _solutions[k].Utility = u;

                    i = j;
                }
            }
        }

        private void SelectMarginal()
        {
            var marginals = _marginals;
            var gradients = _gradients;
            Array.Fill(gradients, 0.0);
            for (var i = 0; i < _populationSize; i++)
            {
                var solution = _solutions[i];
                var bitstring = solution.BitString;
                for (var j = 0; j < gradients.Length; j++)
                {
                    var p = marginals[j];
                    if (bitstring[j] == 1)
                        gradients[j] += (1 - p) * solution.Utility;
                    else
                        gradients[j] -= p * solution.Utility;
                }
            }

            _winningIndex = 0;
            var reservoirCounter = 0;
            var maxAbsGrad = double.NegativeInfinity;
            for (var i = 0; i < gradients.Length; i++)
            {
                var absGrad = Math.Abs(gradients[i]);
                if (absGrad > maxAbsGrad)
                {
                    reservoirCounter = 1;
                    maxAbsGrad = absGrad;
                    _winningIndex = i;
                }
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                else if (absGrad == maxAbsGrad)
                {
                    // reservoir sampling to uniformly pick one of the maximums
                    reservoirCounter++;
                    if (_rng.Next(reservoirCounter) == 0) _winningIndex = i;
                }
            }
        }

        private void LocalSearch()
        {
            for (var i = 0; i < _populationSize; i++)
            {
                var original = _solutions[i];
                var twin = _solutions[i + _populationSize];
                twin.ReInitialize();
                Array.Copy(original.BitString, twin.BitString, _problem.Dimension);
                twin.BitString[_winningIndex] = Flip(twin.BitString[_winningIndex]);
                twin.ComputeHashCode();
            }

            ComputeFitnesses(_populationSize, _populationSize);
            for (var i = 0; i < _populationSize; i++)
            {
                var original = _solutions[i];
                var twin = _solutions[i + _populationSize];
                var comparison = twin.Fitness.CompareTo(original.Fitness);
                if (comparison < 0)
                    continue;
                if (comparison == 0 && _rng.NextDouble() <= 0.5)
                    continue;
                Helpers.Swap(ref _solutions[i], ref _solutions[i + _populationSize]);
            }
        }

        private void UpdateSelectedMarginal()
        {
            _solutions.AsSpan(0, _populationSize).Sort(_comparer);
            var oneVotes = 0;
            var zeroVotes = 0;
            var mu = _populationSize / 2;
            for (var i = 0; i < mu; i++)
            {
                if (_solutions[i].BitString[_winningIndex] == 1)
                    oneVotes += 1;
                else
                    zeroVotes += 1;
            }

            if (oneVotes > zeroVotes) _marginals[_winningIndex] = 1;
            else if (oneVotes < zeroVotes) _marginals[_winningIndex] = 0;
            else _marginals[_winningIndex] = _rng.Next(2);
        }

        private void UpdateStartingDistribution(byte[] referenceBitString)
        {
            for (var i = 0; i < _startingDistribution.Length; i++)
            {
                if (_rng.NextDouble() <= _resetProbability)
                    _startingDistribution[i] = 0.5;
                else
                    _startingDistribution[i] = referenceBitString[i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetBucketIndex(int hashCode)
        {
            return (int)(unchecked((uint)hashCode) % (uint)_bucketCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte Flip(byte value)
        {
            return value == 1 ? (byte)0 : (byte)1;
        }
    }
}