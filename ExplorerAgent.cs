using ActressMas;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Contexts;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading;
using static Reactive.Utils;
using Point = Reactive.Utils.Point;

namespace Reactive
{
    public class ExplorerAgent : Agent
    {
        private Point position; // [x, y, altitude]
        private double speed;
        private List<double> speedAxis;
        private double distanceToAirport;
        // private bool flyOver = false;
        // private DateTime programmedLandingTime;
        private bool startAgain = true; // after new plan
        //private int iteration = 0;
        private Random rnd = new Random();


        public override void Setup()
        {
            if (this.Name == "airplane0") {
                Thread myThread = new Thread(new ThreadStart(enterRadarZone));
                myThread.Start();
                //enterRadarZone();
            }
        }

        public override void Act(Message message)
        {
            Console.WriteLine("\t[{1} -> {0}]: {2}", this.Name, message.Sender, message.Content);

            string action;
            List<string> parameters;
            Utils.ParseMessage(message.Content, out action, out parameters);
         
            switch (action)
            {
                case "start":
                    Thread myThread = new Thread(new ThreadStart(enterRadarZone));
                    myThread.Start();

                    // enterRadarZone();
                    break;
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
                /*case "fly-over":
                    FlyOver();
                    break;*/
                default:
                    break;
            }
        }

        public void enterRadarZone()
        {
            int delay = 0;
            if (this.Name != "airplane0")
            {
                delay = (int)Utils.generateStartPoissonDist(1.0 / 3000);
            }
            Thread.Sleep(delay);
            position = Point.generateRandomAirplanePosition();

            UpdateDistanceAndSpeed(Utils.optimalSpeed);

            InformGlobalAgents("position");
        }
        void InformGlobalAgents(string context)
        {
            string positionString = position.ToString(false);
            Send("planet", Utils.Str(context, positionString, speed));
            Send("coordinator", Utils.Str(context, positionString, speed));
        }
        /*
        void FlyOver()
        {
            flyOver = true;
        }*/
        void UpdateDistanceAndSpeed(double speed)
        {
            this.speed = speed;  // units/iteration
            distanceToAirport = Point.distanceAirplaneAirport(position, new Point(0, 0, 0));
            speedAxis = position.ToList().Select(x => Math.Abs((x * this.speed) / distanceToAirport)).ToList<double>();
        }
        void MoveTowardsAirport()
        {
            Thread.Sleep(10);
            position.moveTowardsAirport(speedAxis);
        }

        bool AtTheAirport()
        {
            bool atAirport = true;
            for (int index = 0; index < position.ToList().Count; index++)
                atAirport &= Math.Abs(position.ToList()[index] - 0) <= Math.Abs(speedAxis[index]);

            return atAirport;
        }
    }
}