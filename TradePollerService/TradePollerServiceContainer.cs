using Services;
using System;
using System.Configuration;
using System.IO;
using System.ServiceProcess;

namespace TradePollerService
{
    public partial class TradePollerServiceContainer : ServiceBase
    {
        private static System.Timers.Timer timer;
        IPowerService powerService = ServiceFactory.GetPowerService();
        
        ILogService logService = ServiceFactory.GetLogService();
        public TradePollerServiceContainer()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            string cwd = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
            Directory.SetCurrentDirectory(cwd ?? AppDomain.CurrentDomain.BaseDirectory);
            logService.Log("Starting TradePollerService at" + DateTime.Now);
            timer = new System.Timers.Timer();
            timer.Interval = 1000 * 60 * int.Parse(ConfigurationManager.AppSettings["ScheduleIntervalMinutes"]);
            timer.Elapsed += new System.Timers.ElapsedEventHandler(OnTimer);
            TradePollerService.DownloadTrades(powerService, logService);
            timer.Enabled = true;
            timer.Start();
        }

        protected override void OnStop()
        {
            timer.Stop();
            logService.Log("TradePollerService stopped at " + DateTime.Now);            
        }
        protected  void OnTimer(object sender, System.Timers.ElapsedEventArgs args)
        {
            timer.Stop();
            TradePollerService.DownloadTrades(powerService, logService);
            timer.Start();
        }
    }

    #region Helper Classes
    /// <summary>
    /// Service factory to instantiate objects that are outside remit of this application and can change in future.
    /// </summary>
    static class ServiceFactory
    {
        public static IPowerService GetPowerService()
        {
            return new PowerService();
        }
        public static ILogService GetLogService()
        {
            return new FileLogger();
        }
    }

    public interface ILogService
    {
        void Log(string message);
    }
    public class FileLogger : ILogService
    {
        private string filePath = ConfigurationManager.AppSettings["Logfilelocation"];
        public void Log(string message)
        {
            using (StreamWriter streamWriter = new StreamWriter(filePath,true))
            {
                streamWriter.WriteLine(message);
                streamWriter.Close();
            }
        }
    }
    // for future logging services
    public class DBLogger : ILogService
    {
        public void Log(string message)
        {
            // save records in DB
        }
    }
    public class EventLogger : ILogService
    {
        public void Log(string message)
        {
            // save records in Events
        }
    }
    #endregion
}
