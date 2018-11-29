using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Services;
using TradePollerService;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

namespace TradeServicePollerTests
{
    [TestClass]
    public class UnitTests
    {
        [TestMethod]
        public void TestAggregatesForHourlyVolume()
        {
            var powerService = new Moq.Mock<IPowerService>();
            var logService = new Moq.Mock<ILogService>();
            DateTime localdate = DateTime.Now;

            var pt = new Moq.Mock<PowerTrade>();

            typeof(PowerTrade).GetProperty("Date")
                .SetValue(pt.Object, localdate, null);

            PowerPeriod[] periods = new[]
            {
                new PowerPeriod()
                {
                    Period = 1, Volume = 100
                },
                new PowerPeriod()
                {
                    Period = 1, Volume = 50
                },
                 new PowerPeriod()
                {
                    Period = 2, Volume = 50
                },
                  new PowerPeriod()
                {
                    Period = 2, Volume = 150
                }
            };
            typeof(PowerTrade).GetProperty("Periods")
                .SetValue(pt.Object, periods, null);
            powerService.Setup(r => r.GetTrades(localdate)).Returns(() => new List<PowerTrade> { pt.Object });
            Dictionary<int, double> report = TradePollerService.TradePollerService.GetHourlyVolume(powerService.Object, logService.Object, localdate);
            Assert.AreEqual(150, report[1]);
            Assert.AreEqual(200, report[2]);
        }

        [TestMethod]
        public void TestReport()
        {
            var powerService = new Moq.Mock<IPowerService>();
            var logService = new Moq.Mock<ILogService>();
            DateTime localdate = DateTime.Now;

            var pt = new Moq.Mock<PowerTrade>();

            typeof(PowerTrade).GetProperty("Date")
                .SetValue(pt.Object, localdate, null);

            PowerPeriod[] periods = new[]
            {
                new PowerPeriod()
                {
                    Period = 1, Volume = 100.0000
                },
                new PowerPeriod()
                {
                    Period = 1, Volume = 50.0000
                },
                 new PowerPeriod()
                {
                    Period = 2, Volume = 50.0000
                },
                  new PowerPeriod()
                {
                    Period = 2, Volume = 150.0000
                },
                    new PowerPeriod()
                {
                    Period = 3, Volume = 150.0000
                },
                  new PowerPeriod()
                {
                    Period = 3, Volume = 150.0000
                },
                   new PowerPeriod()
                {
                    Period = 24, Volume = 15.7000
                },
                  new PowerPeriod()
                {
                    Period = 24, Volume = 1.5000
                }
            };
            typeof(PowerTrade).GetProperty("Periods")
                .SetValue(pt.Object, periods, null);
            powerService.Setup(r => r.GetTrades(localdate)).Returns(() => new List<PowerTrade> { pt.Object });
            Dictionary<int, double> vol = TradePollerService.TradePollerService.GetHourlyVolume(powerService.Object, logService.Object, localdate);

            List<string> report = TradePollerService.TradePollerService.GetHourlyVolumeReport(vol);

            Assert.AreEqual("Local Time\tVolume", report[0]);
            
            Assert.AreEqual(report[1], "23:00,150.0000");
           
            Assert.AreEqual(report[2], "00:00,200.0000");            
            Assert.AreEqual(report[3], "01:00,300.0000");
            Assert.AreEqual(report[4],"22:00,17.2000");
        }
    }
}
