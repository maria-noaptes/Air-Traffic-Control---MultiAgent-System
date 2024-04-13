using ActressMas;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using Point = Reactive.Utils.Point;
using MathNet.Numerics.Distributions;


namespace Reactive
{
    public class ExplorerAgent : Agent
    {
        private Point position; // [x, y, altitude]
        private double speed;
        private List<double> speedAxis;
        private double distanceToAirport;
        private bool startAgain = true; // after new plan
        String[] activateCollaboration;
        private string airplaneIndex;

        private bool repeat = Boolean.Parse(Utils.config.AppSettings.Settings["repeat"].Value);
        private static readonly int avgTimeNewAirplane = Int32.Parse(Utils.config.AppSettings.Settings["avgTimeToNewAirplane"].Value);
         
        private Poisson poisson = new Poisson(1.0 / avgTimeNewAirplane); 
        public override void Setup()
        {
            if (this.Name == "airplane0") {
                Thread myThread = new Thread(new ThreadStart(enterRadarZone));
                myThread.Start();
            }
            airplaneIndex = this.Name.Replace("airplane", "");
            activateCollaboration = Utils.config.AppSettings.Settings["activateCollaboration"].Value.Split(' ');
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
                    break;
                case "restart":
                    Console.WriteLine("restart");
                    Setup();
                    break;
                case "move":
                    if (startAgain)
                    {
                        if (AtTheAirport())
                        {
                            Send("planet", "landing");
                            Send("coordinator", "landing");
                            if (!repeat) this.Stop();
                        }
                        else
                        {
                            MoveTowardsAirport();
                            InformGlobalAgents("change");
                        }
                    }
                    break;
                    
                case "speed":
                    if (activateCollaboration.Contains(airplaneIndex))
                    {
                        UpdateDistanceAndSpeed(Double.Parse(parameters[0]));
                    }
                    startAgain = true;
                    break;
                default:
                    break;
            }
        }

        public void enterRadarZone()
        {
            int delay = 0;
            if (this.Name != "airplane0")
            {
                // delay = (int)Utils.generateStartPoissonDist(1.0 / 140*1000);  // once every 140s
                delay = poisson.Sample();
            }
            Console.WriteLine("delay " + delay);
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
        void UpdateDistanceAndSpeed(double speed)
        {
            this.speed = speed;  // units/iteration
            distanceToAirport = Point.distance(position, new Point(0, 0, 0));
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