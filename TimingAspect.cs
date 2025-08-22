using AspectInjector.Broker;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RenderMaster
{
    public class FunctionExecutionData
    {
        public string MethodName { get; set; }
        public double ExecutionTime { get; set; }
        public double Timestamp { get; set; }
    }
    [Aspect(Scope.Global)]
    public class TimingAspect
    {
        public static readonly Dictionary<string, CircularBuffer<double>> Timings = new();

        [Advice(Kind.Around, Targets = Target.Method)]
        public object HandleMethod(
            [Argument(Source.Target)] Func<object[], object> target,
            [Argument(Source.Arguments)] object[] args,
            [Argument(Source.Metadata)] MethodBase methodInfo)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = target(args);
            stopwatch.Stop();

            var methodName = methodInfo.DeclaringType.FullName + "." + methodInfo.Name;

            if (!Timings.TryGetValue(methodName, out var buffer))
            {
                buffer = new CircularBuffer<double>(300);
                Timings[methodName] = buffer;
            }

            buffer.Add(stopwatch.Elapsed.TotalMilliseconds);

            return result;
        }
    }

    [Injection(typeof(TimingAspect))]
    public class MeasureExecutionTimeAttribute : Attribute
    {
    }

    public class CircularBuffer<T>
    {
        private readonly int _size;
        private readonly T[] _values;
        private int _start;
        private int _count;

        public CircularBuffer(int size)
        {
            _size = size;
            _values = new T[size];
        }

        public void Add(T value)
        {
            _values[(_start + _count) % _size] = value;
            if (_count == _size)
            {
                _start = (_start + 1) % _size;
            }
            else
            {
                _count++;
            }
        }

        public IEnumerable<T> Values => Enumerable.Range(0, _count).Select(i => _values[(_start + i) % _size]);
    }

}
