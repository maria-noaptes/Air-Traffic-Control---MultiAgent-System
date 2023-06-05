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
        public static int minimumTimeBetweenLandings = 15; // iterations

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

        public static string RemoveFromEnd(this string s, string suffix)
        {
            if (s.EndsWith(suffix))
            {
                return s.Substring(0, s.Length - suffix.Length);
            }

            return s;
        }

        /*
        public static void addEdge(Dictionary<string, List<string>> adj,
                                    string i, string j)
        {
            adj[i].Add(j);
            adj[j].Add(i);
        }

        public static void shortestDistance(Dictionary<string, List<string>> adj,
                                                string s, string dest, int v, out List<string> path)
        {
            path = new List<string>();

            Dictionary<string, string> pred = new Dictionary<string, string>(v);
            Dictionary<string, int> dist = new Dictionary<string, int>(v);
            if (BFS(adj, s, dest,
                    v, pred, dist) == false)
            {
                Console.WriteLine("Given source and destination are not connected");
                return;
            }

            string exit = dest;
            path.Add(exit);

            while (pred[exit] != null)
            {
                path.Add(pred[exit]);
                exit = pred[exit];
            }

            Console.WriteLine("Shortest path length is: " +
                                dist[dest]);

            Console.WriteLine("Path is ::");

            for (int i = path.Count - 1;
                    i >= 0; i--)
            {
                Console.Write(path[i] + ", ");
            }
        }

        private static bool BFS(Dictionary<string, List<string>> adj,
                                string src, string dest,
                                int v, Dictionary<string, string> pred,
                                Dictionary<string, int> dist)
        {
            List<string> queue = new List<string>();
            Dictionary<string, bool> visited = new Dictionary<string, bool>(v);

            foreach (string cell in adj.Keys)
            {
                visited[cell] = false;
                dist[cell] = int.MaxValue;
                pred[cell] = null;
            }

            visited[src] = true;
            dist[src] = 0;
            queue.Add(src);

            while (queue.Count != 0)
            {
                string u = queue[0];
                queue.RemoveAt(0);

                for (int i = 0;
                        i < adj[u].Count; i++)
                {
                    if (visited[adj[u][i]] == false)
                    {
                        visited[adj[u][i]] = true;
                        dist[adj[u][i]] = dist[u] + 1;
                        pred[adj[u][i]] = u;
                        queue.Add(adj[u][i]);

                        if (adj[u][i] == dest)
                            return true;
                    }
                }
            }
            return false;
        }*/

    }
}

