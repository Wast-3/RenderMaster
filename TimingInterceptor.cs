using Castle.DynamicProxy;

namespace RenderMaster
{
    public class TimingInterceptor : IInterceptor
    {
        public Dictionary<string, double> Timings { get; } = new Dictionary<string, double>();

        public void Intercept(IInvocation invocation)
        {
            bool shouldMeasure = invocation.Method.IsDefined(typeof(MeasureTimeAttribute), true) ||
                                 invocation.Method.DeclaringType.IsDefined(typeof(MeasureTimeAttribute), true);

            if (shouldMeasure)
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                invocation.Proceed();
                watch.Stop();

                string methodName = invocation.Method.Name;
                double elapsedMs = watch.Elapsed.TotalMilliseconds;
                if (Timings.ContainsKey(methodName))
                {
                    Timings[methodName] = elapsedMs; // Update if key exists
                }
                else
                {
                    Timings.Add(methodName, elapsedMs); // Add if key does not exist
                }
            }
            else
            {
                invocation.Proceed(); // Just proceed without measuring if the attribute is not present
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class MeasureTimeAttribute : Attribute
    {
        // This attribute is a marker; it doesn't need to contain any properties or methods
    }

}

