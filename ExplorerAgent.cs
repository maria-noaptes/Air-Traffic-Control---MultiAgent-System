using ActressMas;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Contexts;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace Reactive
{
    public class ExplorerAgent : Agent
    {
        private List<double> position; // [x, y, altitude];  (sqrt(pow(x, 2) + pow(y, 2)) = horizontal distance, 
        private double speed;
        private List<double> speedAxis;
        private double distanceToAirport;
        private bool flyOver = false;
        private DateTime programmedLandingTime;
        private bool startAgain = true; // after new plan
        private int iteration = 0;

        public override void Setup()
        {
            int index = Int32.Parse(Name.Replace("airplane", ""));
            programmedLandingTime = Utils.programmedLandingTimes[index];
            position = Utils.positions[index];

            UpdateDistanceAndSpeed(Utils.optimalSpeed);

            InformGlobalAgents("position");
        }

       public override void Act(Message message)
        {
            Console.WriteLine("\t[{1} -> {0}]: {2}", this.Name, message.Sender, message.Content);

            string action;
            List<string> parameters;
            Utils.ParseMessage(message.Content, out action, out parameters);
         
            switch (action)
            {
                case "move":
                    if (startAgain)
                    {
                        if (AtTheAirport())
                        {
                            Send("planet", "landing");
                            Send("coordinator", "landing");
                            this.Stop();
                        }
                        else
                        {
                            MoveTowardsAirport();
                            InformGlobalAgents("change");
                        }
                    }
                    break;
                    
                case "speed":
                    UpdateDistanceAndSpeed(Double.Parse(parameters[0]));
                    startAgain = true;
                    break;
                case "fly-over":
                    FlyOver();
                    break;
                default:
                    break;
            }
        }

        void InformGlobalAgents(string context)
        {
            string positionString = String.Join(" ", position);
            Send("planet", Utils.Str(context, positionString));
            Send("coordinator", Utils.Str(context, positionString, speed));
        }

        void FlyOver()
        {
            flyOver = true;
        }
        void UpdateDistanceAndSpeed(double speed)
        {
            this.speed = speed;  // units/iteration
            List<double> airportPosition = new List<double>() { 0, 0, 0 };
            distanceToAirport = Utils.distanceAirplaneAirport(position, airportPosition);
            speedAxis = position.Select(x => Math.Abs((x * this.speed) / distanceToAirport)).ToList<double>();
        }
        void MoveTowardsAirport()
        {
            Thread.Sleep(10);
            for (int i = 0; i < position.Count; i++)
            {
                if (position[i] > 0)
                    position[i] -= speedAxis[i];

                else if (position[i] < 0)
                    position[i] += speedAxis[i];
            }
        }

        bool AtTheAirport()
        {
            bool atAirport = true;
            for (int index = 0; index < position.Count; index++)
                atAirport &= Math.Abs(position[index] - 0) <= Math.Abs(speedAxis[index]);

            return atAirport;
        }
    }
}