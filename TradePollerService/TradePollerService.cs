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
    public class TradePollerService
    {      
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

