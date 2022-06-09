using BenchmarkDotNet.Running;
using System;

namespace Jaina.Benchmarks;

internal class Program
{
    private static void Main(string[] args)
    {
        var addSummary = BenchmarkRunner.Run<HashSet_VS_List>();

        Console.ReadLine();
    }
}