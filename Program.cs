using ActressMas;
using System;
using System.Threading;

namespace Reactive
{
    public class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            EnvironmentMas env = new EnvironmentMas(0, 1);

            var planetAgent = new PlanetAgent();
            env.Add(planetAgent, "planet");

            var coordinatorAgent = new CoordinatorAgent();
            env.Add(coordinatorAgent, "coordinator");

            for (int i = 0; i < Utils.NoExplorers; i++)
            {
                var explorerAgent = new ExplorerAgent();
                env.Add(explorerAgent, "airplane" + i);
            }

            Thread.Sleep(100);

            env.Start();


        }
    }
}