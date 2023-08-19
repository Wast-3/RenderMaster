using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenderMaster.Engine
{
    public static class Logger
    {
        // Declare the field as static
        public static List<ILogger> Loggers { get; } = new List<ILogger>();

        public static void AddLogger(ILogger logger)
        {
            Loggers.Add(logger);
        }

        // You might also want methods to perform logging, initializing, and shutting down the loggers.
        public static void Log(string message)
        {
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
        void Initialize(); // Corrected spelling
        void Shutdown();
    }

    public class ConsoleLogger : ILogger
    {
        // Implementation of ILogger methods
        public void Log(string message)
        {
            Console.WriteLine(message);
        }

        public void Initialize() // Corrected the method name
        {
            // Empty implementation, does nothing
        }

        public void Shutdown()
        {
            // Empty implementation, does nothing
        }
    }

    public class EngineDirFileLogger : ILogger
    {
        string logDirectory = EngineConfig.LoggingDirectory;

        StreamWriter sw;

        EngineDirFileLogger()
        {
            //Should not need to be done, but we'll do anyways:
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            //Create a log file based off of today's date: year-month-day as this is the superior log filename format (sortable)
            string logFileName = DateTime.Now.ToString("yyyy-MM-dd") + ".rendermaster.log";

            //Create file. If file already exists, append to it
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
