using ActressMas;
using Message = ActressMas.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using static Reactive.Utils;
namespace Reactive
{
    public class CoordinatorAgent : Agent
    {
        public Dictionary<string, string> AirplanesPositions { get; set; }
        public Dictionary<string, double> AirplanesSpeed { get; set; }

        public bool planComputed = false;
        public int activeExplorers = 0;
        public int iterations = 0;

        public CoordinatorAgent()
        {
            AirplanesPositions = new Dictionary<string, string> { };  // { "airport", "0 0 0" }
            AirplanesSpeed = new Dictionary<string, double> { };   // { "airport", 100000000 }
        }

        public override void Setup()
        {
            Console.WriteLine("Starting " + Name);
        }
        private void HandleLanding(string sender)
        {
            AirplanesPositions.Remove(sender);
            AirplanesSpeed.Remove(sender);

            activeExplorers--;
            planComputed = false;
        }

        private void HandlePosition(string sender, string data)
        {
            double speed = Double.Parse(data.Split(' ')[3]);
            AirplanesPositions.Add(sender, Utils.RemoveFromEnd(data, " " + speed));
            AirplanesSpeed.Add(sender, speed);
            activeExplorers++;
        }
        private void HandleChange(string sender, string data)
        {
            double speed = Double.Parse(data.Split(' ')[3]);
            AirplanesPositions[sender] = Utils.RemoveFromEnd(data, " " + speed);
            AirplanesSpeed[sender] = speed;

            bool allAirplanesInfo = AirplanesPositions.Count == activeExplorers;  // explorers + airport
            if (allAirplanesInfo) 
            {
                iterations += 1;
                computeNewSpeeds();
            }
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
                case "landing":
                    HandleLanding(message.Sender);
                    break;
                default:
                    break;
            }
        }

        private bool checkOnSameAxis(Point newPoint, Point otherPoint)
        {
            Point airportPoint = new Point(0, 0, 0);
            newPoint = newPoint.coordsToInt();
            otherPoint = otherPoint.coordsToInt();
            int epsilon = 2; // NM
            double proportionA = (int)((newPoint.a - otherPoint.a) / (airportPoint.a - otherPoint.a));
            double proportionB = (int)((newPoint.b - otherPoint.b) / (airportPoint.b - otherPoint.b));
            double proportionC = (int)((newPoint.c - otherPoint.c) / (airportPoint.c - otherPoint.c));
            
            bool inAxis = (Math.Abs(proportionA - proportionB) < epsilon && Math.Abs(proportionB - proportionC) < epsilon);
            return inAxis;
        }

        private List<Dictionary<string, Point>> findPointsOnSameAxis(Dictionary<string, string> AirplanesPositions, Dictionary<string, double> distances)
        {
            Point airportPoint = new Point(0, 0, 0);

            List<Dictionary<string, Point>> airplaneLines = new List<Dictionary<string, Point>>();

            foreach (var airplane in AirplanesPositions.ToList())
            {
                var newPoint = new Point(airplane.Value);
                bool added = false;
                foreach (var dict in airplaneLines.ToList())
                {
                    if (dict.Count() >= 2 && checkOnSameAxis(newPoint, dict.Skip(1).Take(1).First().Value))
                    {
                        dict[airplane.Key] = newPoint;
                        added = true;
                        break;
                    }
                }
                if (!added)
                {
                    Dictionary<string, Point> newDict = new Dictionary<string, Point>
                    {
                        { "airport", airportPoint}, {airplane.Key, newPoint }
                    };
                    airplaneLines.Add(newDict);
                }
            }

            foreach (var dict in airplaneLines.ToList())
            {
                dict.OrderBy(a => distances[a.Key]);
            }

            return airplaneLines;
        }

        private double maxSpeedToKeepSeparation( double distanceTillNextWaypoint, int currentTime, double timeArrivalPredecessor, double separation=0)
        {
            if (separation == 0) separation = Utils.separationRequired;  // default parameter

            double minimumTimeRelativeToOptimalSpeed = currentTime + (distanceTillNextWaypoint - separation) / acceptedSpeedInterval[1]; 
            double minimumTimeArrivalAtNextWaypoint = Math.Max(minimumTimeRelativeToOptimalSpeed, timeArrivalPredecessor);
            double speedMax = (distanceTillNextWaypoint - separation) / (minimumTimeArrivalAtNextWaypoint - currentTime);
            return speedMax;
        }

        private double minSpeedToKeepSeparation(double distanceTillNextWaypoint, int currentTime, double timeArrivalFollower, double separation=0)
        {
            if (separation == 0) separation = Utils.separationRequired;  // default parameter

            double maximumTimeRelativeToOptimalSpeed = currentTime + (distanceTillNextWaypoint + separation) / acceptedSpeedInterval[0]; 
            double maximumTimeArrivalAtNextWaypoint = Math.Min(maximumTimeRelativeToOptimalSpeed, timeArrivalFollower);
            double speedMin = (distanceTillNextWaypoint + separation) / (maximumTimeArrivalAtNextWaypoint - currentTime);
            return speedMin;
        }

        private double timeOfArrival(double v, double d, double currentTime)
        {
            return currentTime + d / v;
        }

        private double chooseSpeed(double vmin, double vmax)
        {
            if (vmax < Utils.optimalSpeed) return vmax;
            if (vmin > Utils.optimalSpeed) return vmin;

            return Utils.optimalSpeed;
        }

        private Tuple<double, double> intersectIntervals(double min, double max, double oldMin, double oldMax)
        {
            if (oldMin > max || min > oldMax) { return null; }
            double newMin = Math.Max(min, oldMin);
            double newMax = Math.Min(max, oldMax);
            return Tuple.Create(newMin, newMax);
        }

        private void adjustSpeedOnAxis(List<string> orderAirplanes, Dictionary<string, double> distances, Dictionary<string, Tuple<double, double>> acceptedIntervals)
        {
            int index = 0;

            foreach (var airplane in orderAirplanes.ToList())
            {
                double vmax = 0;
                double vmin = 0;
                
                if (index > 0)
                {
                    string predecessor = orderAirplanes[index - 1];
                    double timeArrivalPredecessor = timeOfArrival(AirplanesSpeed[predecessor], distances[predecessor], iterations);
                    vmax = maxSpeedToKeepSeparation(distances[airplane], iterations, timeArrivalPredecessor);
                }
                if (index < orderAirplanes.Count - 1)
                {
                    string follower = orderAirplanes[index + 1];
                    double timeArrivalFollower = timeOfArrival(AirplanesSpeed[follower], distances[follower], iterations);
                    vmin = minSpeedToKeepSeparation(distances[airplane], iterations, timeArrivalFollower);
                }
                vmax = vmax == 0 ? (vmin > Utils.optimalSpeed ? vmin : Utils.optimalSpeed) : vmax;
                vmin = vmin == 0 ? (vmax < Utils.optimalSpeed ? vmax : Utils.optimalSpeed) : vmin;


                var interval = new Tuple<double, double>(vmin, vmax);
                acceptedIntervals.Add(airplane, interval);

                index++;
            }
        }

        private void adjustSpeedOnWaypointNeighbours(List<string> orderAirplanes, Dictionary<string, double> distances, Dictionary<string, Tuple<double, double>> acceptedIntervals, List<Dictionary<string, Point>> airplaneLines)
        {
            foreach (var airplane in orderAirplanes.ToList())
            {
                double vmax = 0, vmin = 0;

                string predecessor = getWaypointPredecessor(airplane, orderAirplanes, airplaneLines);
                if (predecessor != null)
                {
                    double separationW = getSeparation(airplane, predecessor);

                    double timeArrivalPredecessor = timeOfArrival(AirplanesSpeed[predecessor], distances[predecessor], iterations);
                    vmax = maxSpeedToKeepSeparation(distances[airplane], iterations, timeArrivalPredecessor, separationW);
                }

                string follower = getWaypointFollower(airplane, orderAirplanes, airplaneLines);
                if (follower != null)
                {
                    double separationW = getSeparation(airplane, follower);

                    double timeArrivalFollower = timeOfArrival(AirplanesSpeed[follower], distances[follower], iterations);
                    vmax = minSpeedToKeepSeparation(distances[airplane], iterations, timeArrivalFollower, separationW);
                }

                if (vmax == 0)
                {
                    vmax = (vmin > Utils.optimalSpeed) ? vmin : Utils.optimalSpeed;
                }
                if (vmin == 0)
                {
                    vmin = (vmax < Utils.optimalSpeed) ? vmax : Utils.optimalSpeed;
                }


                double oldMin = acceptedIntervals[airplane].Item1;
           
                double oldMax = acceptedIntervals[airplane].Item2;
                Tuple<double, double> intersection = intersectIntervals(vmin, vmax, oldMin, oldMax);
                if (intersection != null) acceptedIntervals[airplane] = intersection;

                // intersection with physical accepted interval
                oldMin = acceptedIntervals[airplane].Item1;
                oldMax = acceptedIntervals[airplane].Item2;
                intersection = intersectIntervals(oldMin, oldMax, acceptedSpeedInterval[0], acceptedSpeedInterval[1]); 
                if (intersection != null) acceptedIntervals[airplane] = intersection;

                double speed = chooseSpeed(oldMin, oldMax);

                Send(airplane, Utils.Str("speed", speed));
            }
        }

        private double getSeparation(string airplane, string target)
        {
            var alfa = AirplanesSpeed[airplane] / AirplanesSpeed[target];

            var currentPoint = new Point(AirplanesPositions[airplane]);
            var predPoint = new Point(AirplanesPositions[target]);

            var omega = Point.getAngle(currentPoint, predPoint);

            return minSeparationWaypointNeighbours(alfa, omega);
        }

        private string getWaypointPredecessor(string airplane, List<string> orderAirplanes, List<Dictionary<string, Point>> airplaneLines)
        {
            // predecessor = on different axis and distance predecessor < distance current
            int airplaneIndex = orderAirplanes.IndexOf(airplane);
            Dictionary<string, Point> lineAirplane = airplaneLines.Where((list) => list.ContainsKey(airplane)).ToList()[0];

            foreach (string airplanePredecessor in orderAirplanes.Take(airplaneIndex).Reverse().ToList())  // reverse cause we need to search in descending order of distances (the closest airplane to the current one)
            {
                var newPoint = new Point(AirplanesPositions[airplanePredecessor]);
                if (!checkOnSameAxis(newPoint, lineAirplane.Skip(1).Take(1).First().Value))
                    return airplanePredecessor;
            }
            return null;
        }

        private string getWaypointFollower(string airplane, List<string> orderAirplanes, List<Dictionary<string, Point>> airplaneLines)
        {
            // follower = on different axis and distance predecessor > distance current
            int airplaneIndex = orderAirplanes.IndexOf(airplane);
            Dictionary<string, Point> lineAirplane = airplaneLines.Where((list) => list.ContainsKey(airplane)).ToList()[0];

            foreach (string airplaneFollower in orderAirplanes.Skip(airplaneIndex + 1).ToList())
            {
                var newPoint = new Point(AirplanesPositions[airplaneFollower]);
                if (!checkOnSameAxis(newPoint, lineAirplane.Skip(1).Take(1).First().Value))
                    return airplaneFollower;
            }
            return null;
        }

        private void computeNewSpeeds()
        {
            Dictionary<string, double> distances = new Dictionary<string, double>();
            List<string> orderAirplanes = new List<string>();

            foreach (var airplanePosition in AirplanesPositions)
            {
                string airplane = airplanePosition.Key;
                List<double> position = AirplanesPositions[airplane].Split(' ').Select(e => Double.Parse(e)).ToList<double>();
                distances.Add(airplane, Utils.distance(position, new List<double>() { 0, 0, 0 }));

                orderAirplanes.Add(airplane);
            }

            List<Dictionary<string, Point>> airplaneLines = findPointsOnSameAxis(AirplanesPositions, distances);
            Dictionary<string, Tuple<double, double>> acceptedIntervals = new Dictionary<string, Tuple<double, double>>();
            // get accepted intervals for direct neighbours
            foreach (var airplaneLine in airplaneLines.ToList())
            {
                adjustSpeedOnAxis(new List<string>(airplaneLine.Keys.Skip(1)), distances, acceptedIntervals);
            }

            // get accepted interval for waypoints neighbours (in this case, all the planes are waypoint neighbours and they have the same waypoint to reach - the airport)
            // the closest waypoint neighbours for every plane are given by the next list which orders planes by distance till airport 

            // TO-DO: exclude direct neighbours from waypoint neighbours
            List<string> waypointOrderAirplanes = orderAirplanes.OrderBy(name => distances[name]).ToList();
            adjustSpeedOnWaypointNeighbours(waypointOrderAirplanes, distances, acceptedIntervals, airplaneLines);
        }

        private double minSeparationWaypointNeighbours(double alfa, double omega)
        {
            // omega is the angle between both flows 
            // alfa is from v = alfa*vn (where vn = the speed of the neighbour)
            double rad = Math.Sqrt(Math.Pow(alfa, 2) - 2 * alfa * Math.Cos(omega) + 1);
            return Utils.separationRequired * (rad / Math.Sin(omega));
        }
   
    }
}
