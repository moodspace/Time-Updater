using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Time_Updater
{
    class Program
    {
        static void Main(string[] args)
        {
            string loc = "NY";
            using (System.IO.StreamReader sr = new System.IO.StreamReader(
                System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "loc.txt")))
            {
                bool findLine = false;
                while (!sr.EndOfStream || !findLine) 
                {
                    string tmp = sr.ReadLine().Trim();
                    if (!tmp.StartsWith("//"))
                    {
                        findLine = true;
                        loc = tmp.Substring(tmp.LastIndexOf("\t") + 1);
                    }
                }
            }

            Engines.Worldtimeserver wts = new Engines.Worldtimeserver();

            String timeStr;
            if (!loc.StartsWith("/")) //domestic
                timeStr = wts.GetResponse("US-" + loc);
            else //foreign
                timeStr = wts.GetResponse(loc.Substring(1));

            DateTime time = Convert.ToDateTime(timeStr);
            time = time.AddSeconds(DateTime.Now.Second - time.Second);
            time = time.ToUniversalTime();

            SetSysTime(time);

            Console.Write("Setting system time complete, @ ");
            Console.WriteLine(time.ToLocalTime());

            Thread.Sleep(2000);
            Environment.Exit(0);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEMTIME
        {
            public short wYear;
            public short wMonth;
            public short wDayOfWeek;
            public short wDay;
            public short wHour;
            public short wMinute;
            public short wSecond;
            public short wMilliseconds;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetSystemTime(ref SYSTEMTIME st);

        private static void SetSysTime(DateTime datetime)
        {
            SYSTEMTIME st = new SYSTEMTIME();
            st.wSecond = (short)datetime.Second;
            st.wMinute = (short)datetime.Minute;
            st.wHour = (short)datetime.Hour;

            st.wDay = (short)datetime.Day;
            st.wDayOfWeek = (short)datetime.DayOfWeek;
            st.wMonth = (short)datetime.Month;
            st.wYear = (short)datetime.Year; // must be short

            SetSystemTime(ref st);
        }
    }
}
