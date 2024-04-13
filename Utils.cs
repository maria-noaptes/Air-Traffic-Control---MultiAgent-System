using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Configuration;
using System.Linq;
using System.IO;

namespace Reactive
{
    public class Utils
    {
        public static int NoExplorers;

        private static Random rnd = new Random();
        private static StreamWriter writer;
        public static Brush PickBrush(int airplaneIndex)
        {
            Type brushesType = typeof(Brushes);

            PropertyInfo[] properties = brushesType.GetProperties();

            Brush result = (Brush)properties[20 + airplaneIndex % 15].GetValue(null, null);

            return result;
        }

        public static int Delay = 20;
        public static Random RandNoGen = new Random();

        public static int windowWidth = 1050;
        public static int windowHeight = 600;
        public static int cellSize = 20;
        public static int radarRay = 250;
        public static int airportCenterX = radarRay;
        public static int airportCenterY = radarRay;
        public static double optimalSpeed = 467.0 * 10.0 / 3600.0; // kt (knot = 1NM/h)   467.0 / 3600.0;
        public static double[] acceptedSpeedInterval = { 447.0 * 10.0 / 3600.0, 497.0 * 10.0 / 3600.0 }; // kt   
        public static double separationRequired = 14.14; // NM (1 Nautical mile) 
        public static string configFilePath;
        public static Configuration config;

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

        public static double distance(List<double> pos1, List<double> pos2)
        {
            return Math.Sqrt(Math.Pow(pos1[0] - pos2[0], 2) + Math.Pow(pos1[1] - pos2[1], 2) + Math.Pow(pos1[2] - pos2[2], 2));
        }
        public static double distance(string pos1_s, string pos2_s)
        {
            List<double> pos1 = pos1_s.Split(' ').Select(e => Double.Parse(e)).ToList<double>();
            List<double> pos2 = pos2_s.Split(' ').Select(e => Double.Parse(e)).ToList<double>();
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


        /*public static double generateStartPoissonDist(double lambda)
        {
            Console.WriteLine("lambda "+ lambda);
            // var doub = rnd.NextDouble();
            return -Math.Log(1.0 - rnd.NextDouble()) / lambda;
        }
        */
        public static int generateRandomStart()
        {
            return (int)(rnd.NextDouble() * 20000);  // till 2 minutes
        }

        public static void appendToFile(string file, string text)
        {
            writer = new StreamWriter(file, true);
            writer.WriteLine(text);
            writer.Close();
        }
        public class Point
        {
            // field variable
            public double a, b, c;
            public static Point airportPoint = new Point(0, 0, 0);
            public Point(double a, double b, double c)
            {
                this.a = a;
                this.b = b;
                this.c = c;
            }
            public Point(string point)
            {
                var pointCoords = point.Split(' ');
                (this.a, this.b, this.c) = (double.Parse(pointCoords[0]), double.Parse(pointCoords[1]), double.Parse(pointCoords[2]));
            }
            public string ToString(bool comma = true)
            {
                if (comma)
                {
                    return this.a + ", " + this.b + ", " + this.c;
                }
                else
                {
                    return this.a + " " + this.b + " " + this.c;
                }
            }
            public List<double> ToList()
            {
                var list = new List<double>
                {
                    this.a,
                    this.b,
                    this.c
                };
                return list;
            }

            public void moveTowardsAirport(List<double> speedAxis)
            {
                this.a = move(this.a, speedAxis[0]);
                this.b = move(this.b, speedAxis[1]);
                this.c = move(this.c, speedAxis[2]);

            }
            private double move(double a, double speed)
            {
                if (a > 0)
                    a -= speed;

                else if (a < 0)
                    a += speed;
                return a;
            }

            public double withoutDecimals(double a)
            {
                return (double)(int)a;
            }
            public Point coordsToInt()
            {
                return new Point(withoutDecimals(this.a), withoutDecimals(this.b), withoutDecimals(this.c));
            }
            public static double distance(Point pos1, Point pos2)
            {
                return Math.Sqrt(Math.Pow(pos1.a - pos2.a, 2) + Math.Pow(pos1.b - pos2.b, 2) + Math.Pow(pos1.c - pos2.c, 2));
            }

            public static int Compare(Point a, Point b)
            {
                double d1 = Point.distance(a, Point.airportPoint);
                double d2 = Point.distance(b, Point.airportPoint);
                return d1.CompareTo(d2);
            }
            public static Point vector(Point a, Point b)  // a and b are vectors  (https://socratic.org/questions/how-do-vectors-represent-a-point-in-space)
            {
                // vectors can be thought of as offsets in 3 dimensions, thus we use the same Point class
                return new Point(b.a - a.a, b.b - a.b, b.c - a.c);
            }
            public static double dotProductVectors(Point a, Point b)
            {
                return (a.a * b.a + a.b * b.b + a.c * b.c);
            }
            public static double magnitudeVector(Point a)
            {
                return Math.Sqrt(a.a * a.a + a.b * a.b + a.c * a.c);
            }
            public static double getAngle(Point a, Point b) // the transformed dot product equation
            {
                return Math.Acos(dotProductVectors(a, b) / (magnitudeVector(a) * magnitudeVector(b)));
            }
            public static Point generateRandomAirplanePosition()  // airplanes should be on the circle radius
            {
                double angle = rnd.Next(0, 360);
                double x = radarRay * Math.Cos(angle);
                double y = radarRay * Math.Sin(angle);
                double z = rnd.Next(250, 300);
                return new Point(x, y, z);
            }
        }

        public static void LoadConfigBasedOnEnvironment(string environment)
        {
            if (string.IsNullOrEmpty(environment))
            {
                Console.WriteLine("Environment variable not set. Loading default App.config.");
                environment = "default";
            }
            if (environment == "default")
                configFilePath = "D://disertatie//approach nou//Reactive//Reactive//Reactive//App.config";
            else configFilePath = environment;


            ExeConfigurationFileMap configFileMap = new ExeConfigurationFileMap();
            configFileMap.ExeConfigFilename = configFilePath;

            config = ConfigurationManager.OpenMappedExeConfiguration(configFileMap, ConfigurationUserLevel.None);
          
            NoExplorers = Int32.Parse(config.AppSettings.Settings["noExplorers"].Value);
        }
    }
}
