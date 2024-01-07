using ActressMas;
using Message = ActressMas.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using static Reactive.Utils;
using System.Data.OleDb;

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
            if (allAirplanesInfo)  // && !planComputed  
            {
                iterations += 1;
                computeNewSpeeds();
                // computePlan();
                // planComputed = true;
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

        /*private bool adjustSpeedForAirplane(string airplane, string airplaneCloserToAirport, Dictionary<string, double> distances,
           Dictionary<string, double> AirplanesSpeed, bool neededAdjustmentInPreviousWhile)
        {
            double speed = AirplanesSpeed[airplane];
            double speedAirplaneBefore = AirplanesSpeed[airplaneCloserToAirport];

            double iterationsBetweenThese = Math.Abs(distances[airplane] / speed -
                distances[airplaneCloserToAirport] / speedAirplaneBefore);

            if (Math.Abs(iterationsBetweenThese - Utils.minimumTimeBetweenLandings) > Utils.tolerance)
            {
                neededAdjustmentInPreviousWhile = true;
                // adjust speed of second plane to meet the minimum distance required
                double xSpeed = (speedAirplaneBefore * distances[airplane]) / (Utils.minimumTimeBetweenLandings * speedAirplaneBefore
                    + distances[airplaneCloserToAirport]);

                AirplanesSpeed[airplane] = xSpeed;
            }
            return neededAdjustmentInPreviousWhile;
        }*/


        private bool checkOnSameAxis(Point newPoint, Point otherPoint)
        {
            Point airportPoint = new Point(0, 0, 0);
            newPoint = newPoint.coordsToInt();
            otherPoint = otherPoint.coordsToInt();
            int epsilon = 5;
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
                String[] newPointCoords = airplane.Value.Split(' ');
                var newPoint = new Point(double.Parse(newPointCoords[0]), double.Parse(newPointCoords[1]), double.Parse(newPointCoords[2]));
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

        private double getMinRelativeToOptimalSpeed(double optimalSpeed)
        {
            return optimalSpeed * ((double)80 / (double)100);
        }
        private double getMaxRelativeToOptimalSpeed(double optimalSpeed)
        {
            return optimalSpeed * ((double)120 / (double)100);
        }
        private double maxSpeedToKeepSeparation(double optimalSpeed, double distanceTillNextWaypoint, int currentTime, double timeArrivalPredecessor)
        {
            double minimumTimeRelativeToOptimalSpeed = currentTime + (distanceTillNextWaypoint - Utils.separationRequired) / getMaxRelativeToOptimalSpeed(optimalSpeed);
            double minimumTimeArrivalAtNextWaypoint = Math.Max(minimumTimeRelativeToOptimalSpeed, timeArrivalPredecessor);
            double speedMax = (distanceTillNextWaypoint - Utils.separationRequired) / (minimumTimeArrivalAtNextWaypoint - currentTime);
            return speedMax;
        }

        private double minSpeedToKeepSeparation(double optimalSpeed, double distanceTillNextWaypoint, int currentTime, double timeArrivalFollower)
        {
            double maximumTimeRelativeToOptimalSpeed = currentTime + (distanceTillNextWaypoint + Utils.separationRequired) / getMinRelativeToOptimalSpeed(optimalSpeed);
            double maximumTimeArrivalAtNextWaypoint = Math.Min(maximumTimeRelativeToOptimalSpeed, timeArrivalFollower);
            double speedMin = (distanceTillNextWaypoint + Utils.separationRequired) / (maximumTimeArrivalAtNextWaypoint - currentTime);
            return speedMin;
        }

        private double timeOfArrival(double v, double d, double currentTime)
        {
            return currentTime + d / v;
        }

        private double chooseSpeed(double vmin, double vmax)
        {
            /*double lowerEnd = getMinRelativeToOptimalSpeed(optimalSpeed);
            double upperEnd = getMaxRelativeToOptimalSpeed(optimalSpeed);
            if (vmax < Utils.optimalSpeed && vmax > lowerEnd) return vmax;
            if (vmax < Utils.optimalSpeed && vmax < lowerEnd) return lowerEnd;
            if (vmin > Utils.optimalSpeed && vmin < upperEnd) return vmin;
            if (vmin > Utils.optimalSpeed && vmin > upperEnd) return upperEnd;*/

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
            //orderAirplanes.ForEach(p => Console.Write(p + " "));

            foreach (var airplane in orderAirplanes.ToList())
            {
                double vmax = 0;
                double vmin = 0;

                if (index > 0)
                {
                    string predecessor = orderAirplanes[index - 1];

                    double timeArrivalPredecessor = timeOfArrival(AirplanesSpeed[predecessor], distances[predecessor], iterations);
                    vmax = maxSpeedToKeepSeparation(Utils.optimalSpeed, distances[airplane], iterations, timeArrivalPredecessor);
                }
                if (index < orderAirplanes.Count - 1)
                {
                    string follower = orderAirplanes[index + 1];
                    double timeArrivalFollower = timeOfArrival(AirplanesSpeed[follower], distances[follower], iterations);
                    vmin = minSpeedToKeepSeparation(Utils.optimalSpeed, distances[airplane], iterations, timeArrivalFollower);
                }
                if (vmax == 0)
                {
                    if (vmin > Utils.optimalSpeed) { vmax = vmin; } else { vmax = Utils.optimalSpeed; }
                }
                if (vmin == 0)
                {
                    if (vmax < Utils.optimalSpeed) { vmin = vmax; } else { vmin = Utils.optimalSpeed; }
                }

                var interval = new Tuple<double, double>(vmin, vmax);
                acceptedIntervals.Add(airplane, interval);

                index++;
            }
        }

        private void adjustSpeedOnWaypointNeighbours(List<string> orderAirplanes, Dictionary<string, double> distances, Dictionary<string, Tuple<double, double>> acceptedIntervals, List<Dictionary<string, Point>> airplaneLines)
        {
            // orderAirplanes.ForEach(p => Console.Write(p + " "));

            foreach (var airplane in orderAirplanes.ToList())
            {
                double vmax = 0;
                double vmin = 0;

                double separation = 0;


                string predecessor = getWaypointPredecessor(airplane, orderAirplanes, airplaneLines);
                if (predecessor != null)
                {
                    var alfa = AirplanesSpeed[airplane] / AirplanesSpeed[predecessor];
                    String[] pointCoords = AirplanesPositions[airplane].Split(' ');

                    var currentPoint = new Point(double.Parse(pointCoords[0]), double.Parse(pointCoords[1]), double.Parse(pointCoords[2]));
                    pointCoords = AirplanesPositions[predecessor].Split(' ');
                    var predPoint = new Point(double.Parse(pointCoords[0]), double.Parse(pointCoords[1]), double.Parse(pointCoords[2]));

                    var omega = -Point.getAngle(currentPoint, predPoint);

                    separation = minSeparationWaypointNeighbours(alfa, omega);

                    double timeArrivalPredecessor = timeOfArrival(AirplanesSpeed[predecessor], distances[predecessor], iterations);
                    vmax = maxSpeedToKeepSeparation(Utils.optimalSpeed, distances[airplane], iterations, timeArrivalPredecessor);
                }

                string follower = getWaypointFollower(airplane, orderAirplanes, airplaneLines);
              
                if (follower != null)
                {
                    var alfa = AirplanesSpeed[airplane] / AirplanesSpeed[follower];
                    String[] pointCoords = AirplanesPositions[airplane].Split(' ');

                    var currentPoint = new Point(double.Parse(pointCoords[0]), double.Parse(pointCoords[1]), double.Parse(pointCoords[2]));
                    pointCoords = AirplanesPositions[follower].Split(' ');
                    var predPoint = new Point(double.Parse(pointCoords[0]), double.Parse(pointCoords[1]), double.Parse(pointCoords[2]));

                    var omega = Point.getAngle(currentPoint, predPoint);

                    separation = minSeparationWaypointNeighbours(alfa, omega);

                    double timeArrivalFollower = timeOfArrival(AirplanesSpeed[follower], distances[follower], iterations);
                    vmax = minSpeedToKeepSeparation(Utils.optimalSpeed, distances[airplane], iterations, timeArrivalFollower);
                }
                if (vmax == 0)
                {
                    if (vmin > Utils.optimalSpeed) { vmax = vmin; } else { vmax = Utils.optimalSpeed; }
                }
                if (vmin == 0)
                {
                    if (vmax < Utils.optimalSpeed) { vmin = vmax; } else { vmin = Utils.optimalSpeed; }
                }
                double oldMin = acceptedIntervals[airplane].Item1;
                double oldMax = acceptedIntervals[airplane].Item2;
                Tuple<double, double> intersection = intersectIntervals(vmin, vmax, oldMin, oldMax);
                if (intersection != null) acceptedIntervals[airplane] = intersection;

                // intersection with physical accepted interval
                oldMin = acceptedIntervals[airplane].Item1;
                oldMax = acceptedIntervals[airplane].Item2;
                intersection = intersectIntervals(oldMin, oldMax, getMinRelativeToOptimalSpeed(Utils.optimalSpeed), getMaxRelativeToOptimalSpeed(Utils.optimalSpeed));
                if (intersection != null) acceptedIntervals[airplane] = intersection;

                double speed = chooseSpeed(acceptedIntervals[airplane].Item1, acceptedIntervals[airplane].Item2);
                Send(airplane, Utils.Str("speed", speed));
            }
        }


        private string getWaypointPredecessor(string airplane, List<string> orderAirplanes, List<Dictionary<string, Point>> airplaneLines)
        {
            // predecessor = on different axis and distance predecessor < distance current
            int airplaneIndex = orderAirplanes.IndexOf(airplane);
            Dictionary<string, Point> lineAirplane = airplaneLines.Where((list) => list.ContainsKey(airplane)).ToList()[0];


            foreach (string airplanePredecessor in orderAirplanes.Take(airplaneIndex).Reverse().ToList())  // reverse cause we need to search in descending order of distances (the closest airplane to the current one)
            {
                String[] newPointCoords = AirplanesPositions[airplanePredecessor].Split(' ');
                var newPoint = new Point(double.Parse(newPointCoords[0]), double.Parse(newPointCoords[1]), double.Parse(newPointCoords[2]));
                if (checkOnSameAxis(newPoint, lineAirplane.Skip(1).Take(1).First().Value))
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
                String[] newPointCoords = AirplanesPositions[airplaneFollower].Split(' ');
                var newPoint = new Point(double.Parse(newPointCoords[0]), double.Parse(newPointCoords[1]), double.Parse(newPointCoords[2]));
                if (checkOnSameAxis(newPoint, lineAirplane.Skip(1).Take(1).First().Value))
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
                distances.Add(airplane, Utils.distanceAirplaneAirport(position, new List<double>() { 0, 0, 0 }));

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
