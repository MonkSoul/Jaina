Jaina 基准测试

### `HashSet<Wrapper>` 和 `List<Wrapper>` 查询测试

```cs
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
        private readonly Random _random = new Random();
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
```

测试结果：

```
// * Summary *

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.22000
11th Gen Intel Core i7-1185G7 3.00GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK=6.0.100-rc.2.21505.57
  [Host]   : .NET 5.0.10 (5.0.1021.41214), X64 RyuJIT
  .NET 5.0 : .NET 5.0.10 (5.0.1021.41214), X64 RyuJIT

Job=.NET 5.0  Runtime=.NET 5.0

|       Method |     N |      Mean |    Error |    StdDev |
|------------- |------ |----------:|---------:|----------:|
| HashSetQuery |  1000 |  30.14 us | 0.308 us |  0.288 us |
|    ListQuery |  1000 |  30.01 us | 0.497 us |  0.465 us |
| HashSetQuery | 10000 | 307.76 us | 6.145 us | 10.924 us |
|    ListQuery | 10000 | 298.18 us | 3.869 us |  3.619 us |
| HashSetQuery | 50000 | 534.64 us | 4.363 us |  4.081 us |
|    ListQuery | 50000 | 543.27 us | 5.531 us |  5.174 us |

// * Legends *
  N      : Value of the 'N' parameter
  Mean   : Arithmetic mean of all measurements
  Error  : Half of 99.9% confidence interval
  StdDev : Standard deviation of all measurements
  1 us   : 1 Microsecond (0.000001 sec)

// ***** BenchmarkRunner: End *****
// ** Remained 0 benchmark(s) to run **
Run time: 00:01:50 (110.37 sec), executed benchmarks: 6

Global total time: 00:01:53 (113.6 sec), executed benchmarks: 6
// * Artifacts cleanup *
```