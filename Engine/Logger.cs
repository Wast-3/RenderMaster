namespace RenderMaster.Engine
{
    public enum LogLevel
    {
        Info,
        Warning,
        Error,
        Debug
    }


    public static class Logger
    {

        public static List<ILogger> Loggers { get; } = new List<ILogger>();

        static Logger()
        {

            Loggers.Add(new ConsoleLogger());
            Loggers.Add(new EngineDirFileLogger());
        }

        public static void AddLogger(ILogger logger)
        {
            Loggers.Add(logger);
        }


        public static void Log(string message, LogLevel level)
        {

            message = $"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}] [{System.Threading.Thread.CurrentThread.ManagedThreadId}] [{level.ToString().ToUpper()}]: {message}";

            foreach (var logger in Loggers)
                logger.Log(message);
        }

        public static void Initialize()
        {
            foreach (var logger in Loggers)
                logger.Initialize();
        }

        public static void Shutdown()
        {
            foreach (var logger in Loggers)
                logger.Shutdown();
        }
    }

    public interface ILogger
    {
        void Log(string message);
        void Initialize();
        void Shutdown();
    }

    public class ConsoleLogger : ILogger
    {

        public void Log(string message)
        {
            Console.WriteLine(message);
        }

        public void Initialize()
        {

        }

        public void Shutdown()
        {

        }
    }

    public class EngineDirFileLogger : ILogger
    {
        string logDirectory = EngineConfig.LoggingDirectory;

        StreamWriter sw;

        public EngineDirFileLogger()
        {

            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }


            string logFileName = DateTime.Now.ToString("yyyy-MM-dd") + ".rendermaster.log";


            sw = File.AppendText(Path.Combine(logDirectory, logFileName));

            sw.WriteLine("RenderMaster Engine Log File");
            sw.WriteLine("Opened:  " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sw.WriteLine("begin logs:\n\n\n");
        }

        public void Log(string message)
        {
            sw.WriteLine(message);
        }

        public void Initialize()
        {

        }

        public void Shutdown()
        {
            sw.WriteLine("\n\n\nend logs:");
            sw.WriteLine("Closed:  " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sw.Close();
        }

        ~EngineDirFileLogger()
        {
            sw.Close();
        }
    }
}
