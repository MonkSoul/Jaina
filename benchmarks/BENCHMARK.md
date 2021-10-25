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
| HashSetQuery |  1000 |  23.14 us | 0.208 us |  0.188 us |
|    ListQuery |  1000 |  30.01 us | 0.497 us |  0.465 us |
| HashSetQuery | 10000 | 107.76 us | 2.145 us |  1.924 us |
|    ListQuery | 10000 | 298.18 us | 3.869 us |  3.619 us |
| HashSetQuery | 50000 | 334.64 us | 3.363 us |  3.081 us |
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

----

总数 50000 （5万）： List 检索 5W次 耗时 23秒， HashSet 检索 5W次 耗时 0.01秒。
总数 5000  （5千）： List 检索 5K次 耗时 0.16秒， HashSet 检索 5K次 耗时 0.001秒。
总数 500   （5百）： List 检索 500次 耗时 0.004秒， HashSet 检索 500次 耗时 0.000秒。
总数 50           ： List 检索 50次  耗时 0.002秒， HashSet 检索 500次 耗时 0.000秒。

集合查找元素，

当总数超过 10 时， HashSet<T>  的检索性能 就会比 List<T> 快。
当总数超过 1000 时， List<T> 的检索性能 会 急速下降。
当总数超过 10000 时， List<T> 将会以 秒 为单位 的损失性能。

无论怎样的数据量， HashSet<T> 的检索速度 都要比 List<T> 快。