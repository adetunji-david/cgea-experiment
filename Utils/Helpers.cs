namespace CgeaExperiment.Utils
{
    internal static class Helpers
    {
        public static void Swap<T>(ref T a, ref T b)
        {
            (a, b) = (b, a);
        }
    }
}