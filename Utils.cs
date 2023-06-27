using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Runtime.Remoting.Channels;

using System.Linq;
using System.Runtime.CompilerServices;

namespace Reactive
{
    public class Utils
    {
        public static int NoExplorers = 5;

        private static Random rnd = new Random();
        public static Brush PickBrush()
        {
            Type brushesType = typeof(Brushes);

            PropertyInfo[] properties = brushesType.GetProperties();
            int random = rnd.Next(20, 35);
            Brush result = (Brush)properties[random].GetValue(null, null);

            return result;
        }

        public static int Delay = 20;
        public static Random RandNoGen = new Random();


        public static DateTime[] programmedLandingTimes = new DateTime[] { new DateTime(2015, 12, 31, 12, 00, 00),
            new DateTime(2015, 12, 31, 12, 04, 00), new DateTime(2015, 12, 31, 12, 10, 00), new DateTime(2015, 12, 31, 12, 18, 00),
            new DateTime(2015, 12, 31, 11, 55, 00), new DateTime(2015, 12, 31, 11, 58, 00), new DateTime(2015, 12, 31, 11, 58, 00)  };

        public static List<List<double>> positions = new List<List<double>> { new List<double> { 250, -120, 200 }, new List<double> { 120, -230, 300 },
            new List<double> { 210, 240, 210 }, new List<double> { -180, 250, 200 }, new List<double> { 200, 100, 100 } };

        public static int windowWidth = 1050;
        public static int windowHeight = 600;
        public static int cellSize = 20;
        public static int radarRay = 250;
        public static int airportCenterX = 10 + radarRay;
        public static int airportCenterY = 10 + radarRay;
        public static int minimumTimeBetweenLandings = 50; // iterations

        public static void ParseMessage(string content, out string action, out List<string> parameters)
        {
            string[] t = content.Split();

            action = t[0];

            parameters = new List<string>();
            for (int i = 1; i < t.Length; i++)
                parameters.Add(t[i]);
        }

        public static void ParseMessage(string content, out string action, out string parameters)
        {
            string[] t = content.Split();

            action = t[0];

            parameters = "";

            if (t.Length > 1)
            {
                for (int i = 1; i < t.Length - 1; i++)
                    parameters += t[i] + " ";
                parameters += t[t.Length - 1];
            }
        }

        public static string Str(object p1, object p2)
        {
            return string.Format("{0} {1}", p1, p2);
        }

        public static string Str(object p1, object p2, object p3)
        {
            return string.Format("{0} {1} {2}", p1, p2, p3);
        }

        public static double distanceAirplaneAirport(List<double> pos1, List<double> pos2)
        {
            return Math.Sqrt(Math.Pow(pos1[0] - pos2[0], 2) + Math.Pow(pos1[1] - pos2[1], 2) + Math.Pow(pos1[2] - pos2[2], 2));
        }

        public static string RemoveFromEnd(string s, string suffix)
        {
            if (s.EndsWith(suffix))
            {
                return s.Substring(0, s.Length - suffix.Length);
            }

            return s;
        }
    }
}

