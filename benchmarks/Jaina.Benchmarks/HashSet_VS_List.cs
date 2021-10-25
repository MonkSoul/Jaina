using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Jaina.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net50)]
    public class HashSet_VS_List
    {
        private readonly Random _random = new();
        private readonly HashSet<Wrapper> setter = new();
        private readonly List<Wrapper> list = new();

        public HashSet_VS_List()
        {
            for (int i = 0; i < 10000; i++)
            {
                setter.Add(new Wrapper($"EventId:{i}"));
            }

            for (int i = 0; i < 10000; i++)
            {
                list.Add(new Wrapper($"EventId:{i}"));
            }
        }

        [Params(1000, 10000, 50000)]
        public int N;

        [Benchmark]
        public Wrapper HashSetQuery()
        {
            int num = _random.Next(1, N);
            return setter.FirstOrDefault(u => u.ShouldRun($"EventId:{num}"));
        }

        [Benchmark]
        public Wrapper ListQuery()
        {
            int num = _random.Next(1, N);
            return list.FirstOrDefault(u => u.ShouldRun($"EventId:{num}"));
        }
    }
}