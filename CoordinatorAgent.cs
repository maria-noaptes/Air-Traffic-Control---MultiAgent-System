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
        public Dictionary<string, int> AirplanesSpeed { get; set; }
        public bool planComputed = false;

        // calculates how many iterations (time) planes need before landing at their current speed
        // establishes an order of landing according to programmed times
        // makes planes change speed and/or fly around
        // minimum iterations between different landings is 5  
        public CoordinatorAgent()
        {
            AirplanesPositions = new Dictionary<string, string>();
            AirplanesSpeed = new Dictionary<string, int>();
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
            foreach (double dist in distances.Values)
            {
                Console.Write(" dist:" + dist + ", ");
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
            while (neededAdjustmentInPreviousWhile)
            {
                neededAdjustmentInPreviousWhile = false;
                for (int index = orderAirplanes.Count - 2; index <= 0; index++)
                {
                    string airplane = orderAirplanes[index + 1];
                    string airplaneCloserToAirport = orderAirplanes[index];

                    int speed = AirplanesSpeed[airplane];
                    int speedAirplaneBefore = AirplanesSpeed[airplaneCloserToAirport];


                    double iterationsBetweenThese = Math.Abs(distances[airplane] / AirplanesSpeed[airplane] -
                        distances[airplaneCloserToAirport] / AirplanesSpeed[airplaneCloserToAirport]);
                    // double iterationsBetweenThese = Math.Abs(distBetweenTheseAirplanes) / speed;
                    Console.WriteLine("iterationsBetweenThese " + iterationsBetweenThese);
                    if (iterationsBetweenThese < Utils.minimumTimeBetweenLandings)
                    {

                        neededAdjustmentInPreviousWhile = true;
                        // adjust speed of second plane to meet the minimum distance required
                        double xSpeed = (Utils.minimumTimeBetweenLandings * speed) / iterationsBetweenThese;

                        Console.WriteLine(airplane + ": speed, new speed " + speed + " " + xSpeed);

                        Send(airplane, Utils.Str("speed", (int)xSpeed));
                        AirplanesSpeed[airplane] = (int)xSpeed;
                    }

                }
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