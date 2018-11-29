using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Services;
using System.Diagnostics;
using System.IO;
using System.Configuration;

namespace TradePollerService
{
    public class LocalPowerTrade : PowerTrade
    {
        public new DateTime Date { get; set; }
        public new PowerPeriod[] Periods { get; set; }
    }
    public interface IPollerService
    {
        IEnumerable<string> GetTrades(DateTime date);

        //Task<IEnumerable<LocalPowerTrade>> GetTradesAsync(DateTime date);

    }
    public class TradeRepository : IPollerService
    {
        IPowerService powerService;

        public TradeRepository(IPowerService powerServiceParam)
        {
            powerService = powerServiceParam;
        }
        public IEnumerable<string> GetTrades(DateTime date)
        {
            return new List<string>();
        }

    }

    public class TradePollerService
    {
        #region Requirements
        /* Requirements.
       1.Must be implemented as a Windows service using .Net 4.5 using either F# or C#.
       2.All trade positions must be aggregated per hour(local / wall clock time).Note that for a given
       day, the actual local start time of the day is 23:00(11 pm) on the previous day.Local time is
       in the GMT time zone.
       3.CSV output format must be two columns, Local Time(format 24 hour HH: MM e.g. 13:00)
       and Volume and the first row must be a header row.
       4.CSV filename must be PowerPosition_YYYYMMDD_HHMM.csv where YYYYMMDD is
       year / month / day e.g. 20141220 for 20 Dec 2014 and HHMM is 24hr time hour and minutes
       e.g. 1837.The date and time are the local time of extract.
       5.The location of the CSV file should be stored and read from the application configuration file.
       6.An extract must run at a scheduled time interval; every X minutes where the actual interval
       X is stored in the application configuration file.This extract does not have to run exactly on
       the minute and can be within +/ -1 minute of the configured interval.
       7.It is not acceptable to miss a scheduled extract.
       8.An extract must run when the service first starts and then run at the interval specified as
       above.
       9.It is acceptable for the service to only read the configuration when first starting and it does
       not have to dynamically update if the configuration file changes.It is sufficient to require a
       service restart when updating the configuration.
       10.The service must provide adequate logging for production support to diagnose any issues
       */
        #endregion
        const string ReportHeader = "Local Time\tVolume";
        static readonly Dictionary<int, string> periodTimeMap = new Dictionary<int, string>();
       
        public static void DownloadTrades(IPowerService powerService, ILogService logService)
        {
            DateTime localDateTime = DateTime.Now;
            Dictionary<int, double> data = GetHourlyVolume(powerService, logService, localDateTime);
            SaveTradeData(GetHourlyVolumeReport(data), logService, localDateTime);
        }
        private static void SetupMapping()
        {
            if (periodTimeMap.Count == 0)
            {
                periodTimeMap.Add(1, "23:00");
                periodTimeMap.Add(2, "00:00");
                for (int t = 3; t <= 24; t++)
                {
                    periodTimeMap.Add(t, string.Format("{0}:00", (t - 2).ToString("D2")));
                }
            }
        }
        public static List<string> GetHourlyVolumeReport(Dictionary<int, double> hourlyVolume)
        {
            List<string> dataList = new List<string>();
            SetupMapping();
            //header
            dataList.Add(ReportHeader);
            //aggregate volumes
            foreach (int period in hourlyVolume.Keys)
                dataList.Add(string.Format("{0},{1}", periodTimeMap[period], hourlyVolume[period].ToString(".0000")));
            return dataList;
        }
        public static Dictionary<int, double> GetHourlyVolume(IPowerService powerService, ILogService logService, DateTime localDateTime)
        {
            Dictionary<int, double> periodVolume = new Dictionary<int, double>();

            try
            {
                foreach (PowerTrade p in powerService.GetTrades(localDateTime))
                {
                    foreach (PowerPeriod pp in p.Periods)
                    {
                        if (periodVolume.ContainsKey(pp.Period))
                            periodVolume[pp.Period] = periodVolume[pp.Period] + pp.Volume;
                        else
                            periodVolume.Add(pp.Period, pp.Volume);
                    }
                }

            }
            catch (Services.PowerServiceException seExp)
            {
                logService.Log(string.Format("Power service issue: {0} \n occured while getting data for {1}.csv file ", seExp.ToString(), GetFormattedFileName(localDateTime)));
            }
            catch (Exception exp)
            {
                logService.Log(string.Format("Unknow error: {0} \n  occured while getting data for {1}.csv file ", exp.ToString(), GetFormattedFileName(localDateTime)));
            }
            if (periodVolume.Count == 0)
                logService.Log(string.Format("No trade data found for {0}.csv", GetFormattedFileName(localDateTime)));

            return periodVolume;
                
        }
        static void SaveTradeData(List<string> message, ILogService logService, DateTime localDateTime)
        {
            try
            {
                File.WriteAllLines(Path.Combine(ConfigurationManager.AppSettings["PowerPositionCSVfilelocation"], GetFormattedFileName(localDateTime)), message);
            }
            catch (Exception exp)
            {
                logService.Log(string.Format("Unknow error: {0} \n  occured while generating data for {1}.csv file ", exp.ToString(), GetFormattedFileName(localDateTime)));
            }
        }
        static string GetFormattedFileName(DateTime localDateTime)
        {
            //PowerPosition_YYYYMMDD_HHMM.csv       
            return string.Format("PowerPosition_{0}{1}{2}_{3}.csv", localDateTime.Year.ToString("D4"), localDateTime.Month.ToString("D2"), localDateTime.Day.ToString("D2"), localDateTime.ToString("HH:mm").Replace(":", ""));
        }
    }
}

