using ActressMas;
using Message = ActressMas.Message;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using System.Linq;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Data.SqlTypes;
using System.Drawing.Drawing2D;
using System.Xml.Linq;

namespace Reactive
{
    public class CoordinatorAgent : Agent
    {

        public Dictionary<string, string> AirplanesPositions { get; set; }
        public Dictionary<string, double> AirplanesSpeed { get; set; }
        public Dictionary<string, double> AirplanesNewSpeeds { get; set; }
        public bool planComputed = false;

        // calculates how many iterations (time) planes need before landing at their current speed
        // establishes an order of landing according to programmed times
        // makes planes change speed and/or fly around
        // minimum iterations between different landings is 5  
        public CoordinatorAgent()
        {
            AirplanesPositions = new Dictionary<string, string>();
            AirplanesSpeed = new Dictionary<string, double>();
            AirplanesNewSpeeds = new Dictionary<string, double>();
        }

        public override void Setup()
        {
            Console.WriteLine("Starting " + Name);
        }

        public override void Act(Message message)
        {
            Console.WriteLine("\t[{1} -> {0}]: {2}", this.Name, message.Sender, message.Content);

            string action; string parameters;
            Utils.ParseMessage(message.Content, out action, out parameters);

            switch (action)
            {
                case "position":
                    HandlePosition(message.Sender, parameters);
                    break;

                case "change":
                    HandleChange(message.Sender, parameters);
                    break;

                default:
                    break;
            }
        }

        private void computePlan()
        {
            Dictionary<string, double> distances = new Dictionary<string, double>();
            List<string> orderAirplanes = new List<string>();

            for (int index = 0; index < AirplanesPositions.Count; index++)
            {
                string airplane = "airplane" + index;
                List<double> airplanePosition = AirplanesPositions[airplane].Split(' ').Select(e => Double.Parse(e)).ToList<double>();
                distances.Add(airplane, Utils.distanceAirplaneAirport(airplanePosition, new List<double>() { 0, 0, 0 }));

                orderAirplanes.Add(airplane);
            }
            Console.WriteLine("distances");
            foreach (KeyValuePair<string, double> kpv in distances)
            {
                Console.WriteLine(kpv.Key + ": dist:" + kpv.Value + ", speed: " + AirplanesSpeed[kpv.Key]);
            }
            Console.WriteLine();

            orderAirplanes = orderAirplanes.OrderBy(name => distances[name]).ToList();

            Console.WriteLine("order");
            foreach (string name in orderAirplanes)
            {
                Console.Write(name + ", ");
            }
            Console.WriteLine();

            
            // continue adjusting speed until all airplanes are at a mimimum distance from one another
            bool neededAdjustmentInPreviousWhile = true;
            Console.WriteLine("orderAirplanes.Count: " + orderAirplanes.Count);
            foreach (KeyValuePair<string, double> kpv in AirplanesSpeed)
            {
                AirplanesNewSpeeds.Add(kpv.Key, kpv.Value);
            }
            while (neededAdjustmentInPreviousWhile)
            {
                foreach (KeyValuePair<string, double> kpv in distances)
                {
                    Console.WriteLine(kpv.Key + ": dist:" + kpv.Value + ", speed: " + AirplanesSpeed[kpv.Key]);
                }
                Console.WriteLine();
                Thread.Sleep(1000);
                neededAdjustmentInPreviousWhile = false;
                for (int index = 1; index < distances.Count; index++)
                {
                    string airplane = orderAirplanes[index];
                    string airplaneCloserToAirport = orderAirplanes[index-1];

                    double speed = AirplanesSpeed[airplane];
                    double speedAirplaneBefore = AirplanesSpeed[airplaneCloserToAirport];
                    Console.WriteLine("speed " + speed + ", speedAirplaneBefore " + speedAirplaneBefore);

                    double iterationsBetweenThese = Math.Abs(distances[airplane] / speed -
                        distances[airplaneCloserToAirport] / speedAirplaneBefore);
                    // double iterationsBetweenThese = Math.Abs(distBetweenTheseAirplanes) / speed;
                    Console.WriteLine("iterationsBetweenThese, Utils.minimumTimeBetweenLandings: " + iterationsBetweenThese + ", " + Utils.minimumTimeBetweenLandings);
                 
                    if (iterationsBetweenThese < Utils.minimumTimeBetweenLandings)
                    {
                        Console.WriteLine("pair (" + airplane + ", " + airplaneCloserToAirport + ")");
                        neededAdjustmentInPreviousWhile = true;
                        // adjust speed of second plane to meet the minimum distance required
                        Console.WriteLine("Utils.minimumTimeBetweenLandings, speed, iterationsBetweenThese: "
                            + Utils.minimumTimeBetweenLandings + ", " + speed + ", " + iterationsBetweenThese);
                        double xSpeed = Math.Abs((speed * iterationsBetweenThese)/Utils.minimumTimeBetweenLandings);

                        Console.WriteLine(airplane + ": speed, xSpeed " + speed + " " + xSpeed);

                        AirplanesSpeed[airplane] = xSpeed;
                        // Send(airplane, Utils.Str("speed", (int)xSpeed));
                        AirplanesNewSpeeds[airplane] = xSpeed;
                    }
                }
            }
            foreach (KeyValuePair<string, double> kpv in AirplanesNewSpeeds)
            {
                Console.WriteLine("kpv.Value, (int)kpv.Value: " + kpv.Value + ",  " + Math.Ceiling(kpv.Value));
                Send(kpv.Key, Utils.Str("speed", Math.Ceiling(kpv.Value)));
            }
        }

        private void HandlePosition(string sender, string data)
        {
            int speed = Int32.Parse(data.Split(' ')[3]);
            AirplanesPositions.Add(sender, Utils.RemoveFromEnd(data, " " + speed));
            AirplanesSpeed.Add(sender, speed);
        }
        private void HandleChange(string sender, string data)
        {
            int speed = Int32.Parse(data.Split(' ')[3]);
            AirplanesPositions[sender] = Utils.RemoveFromEnd(data, " " + speed);
            AirplanesSpeed[sender] = speed;
            Console.WriteLine("AirplanesPositions.Count, Utils.NoExplorers: " + AirplanesPositions.Count, Utils.NoExplorers);

            bool allAirplanesInfo = AirplanesPositions.Count == Utils.NoExplorers;
            if (!planComputed && allAirplanesInfo)
            {
                planComputed = true;
                Console.WriteLine("plan compute starts");
                computePlan();
            }
        }

    }
}