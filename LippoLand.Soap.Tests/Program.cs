using System;

namespace LippoLand.Soap.Tests
{
    /// <summary>
    /// Test runner — discovers and executes all test classes.
    /// Exit code 0 = all passed. Exit code 1 = at least one failure.
    /// </summary>
    class Program
    {
        static int Main(string[] args)
        {
            Console.WriteLine("============================================");
            Console.WriteLine(" LippoLand SOAP — Test Suite");
            Console.WriteLine("============================================\n");

            int passed = 0;
            int failed = 0;

            Run("SoapEnvelopesTests",  SoapEnvelopesTests.Run,  ref passed, ref failed);
            Run("SoapHttpClientTests", SoapHttpClientTests.Run, ref passed, ref failed);
            Run("ServiceSpanTests",    ServiceSpanTests.Run,    ref passed, ref failed);

            Console.WriteLine("\n============================================");
            Console.WriteLine(" Results: {0} passed, {1} failed", passed, failed);
            Console.WriteLine("============================================");

            return failed > 0 ? 1 : 0;
        }

        static void Run(string name, Action suite, ref int passed, ref int failed)
        {
            Console.Write("Running {0}... ", name);
            try
            {
                suite();
                passed++;
                Console.WriteLine("PASS");
            }
            catch (Exception ex)
            {
                failed++;
                Console.WriteLine("FAIL\n  {0}", ex.Message);
            }
        }
    }
}
