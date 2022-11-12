using CgeaExperiment.Problems;
using CgeaExperiment.Rng;
using System.Text;
using CgeaExperiment.Algorithms;

namespace CgeaExperiment
{
    internal class Program
    {
        private const int Budget = 10_000_000;
        private const int TrialCount = 100;
        private const int Dimension = 100;
        private const int CgEaPopSize = 10;
        private const double CgEaResetProbability = 0.1;
        private const double CgaUpdateFactor = 2.0;
        private const double CgaBudgetFactor = 8.0;
        private const uint RngSeed = 167776193;


        private static void Main(string[] args)
        {
            var dirBasePath = Path.Join(Environment.CurrentDirectory, "Output");
            var namedProblems = new Dictionary<string, IProblem<int>>
            {
                { "IsingRing", new IsingRing(Dimension) },
                { "IsingTorus", new IsingTorus(Dimension) },
                { "Mivs", new MaximumIndependentVertexSet(Dimension) }
            };

            var startTime = DateTime.Now;
            foreach (var (problemName, problem) in namedProblems)
            {
                foreach (var algorithmName in new[] { "cgEA", "cGA" })
                {
                    var dirPath = Path.Join(dirBasePath, problemName, algorithmName);
                    Directory.CreateDirectory(dirPath);
                    var rng = Pcg64.Create(RngSeed);
                    Console.WriteLine($"{algorithmName} on {problemName}");
                    Console.WriteLine($"Started at {DateTime.Now}");
                    for (var trial = 1; trial <= TrialCount; trial++)
                    {
                        ISolver<int> solver = algorithmName switch
                        {
                            "cGA" => new SmartRestartCga<int>(problem, rng,
                                CgaBudgetFactor, CgaUpdateFactor),
                            "cgEA" => new PartialRestartCgea<int>(
                                problem, rng, CgEaPopSize, CgEaResetProbability),
                            _ => throw new ArgumentOutOfRangeException(nameof(algorithmName))
                        };
                        var result = solver.Run(Budget);
                        var runtimes = string.Join(",", result.Select(r => $"\"{r.HittingTime}\""));
                        var targets = string.Join(",", result.Select(r => $"\"{r.Fitness}\""));
                        var nl = Environment.NewLine;
                        var json = $"{{{nl}  \"hitting_times\": [{runtimes}],{nl}  \"targets\": [{targets}]{nl}}}";
                        var filePath = Path.Join(dirPath, $"trial-{trial}.json");
                        File.WriteAllText(filePath, json, Encoding.ASCII);
                    }

                    Console.WriteLine($"Finished at {DateTime.Now}");
                }
            }

            var endTime = DateTime.Now;
            Console.WriteLine($"Time taken for experiment: {endTime - startTime}");
            Console.WriteLine($"Output at {dirBasePath}");
            Console.ReadLine();
        }
    }
}