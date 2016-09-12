// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="CodeBlueDev">
//   All rights reserved.
// </copyright>
// <summary>
//   The program entry-point.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace CodeBlueDev.EventAggregator.Test.Console
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// The program entry-point.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The main method.
        /// </summary>
        /// <param name="args">
        /// The arguments used by the main method.
        /// </param>
        public static void Main(string[] args)
        {
            Console.WriteLine("Press <Enter> to exit.");

            Program firstProgram = new Program();
            Program secondProgram = new Program();
            Program thirdProgram = new Program();

            firstProgram.Subscribe<Ping>(
                async program => await WritePing(program));

            secondProgram.Subscribe<Pong>(
                async program => await WritePong(program));

            thirdProgram.Subscribe<Pong>(
                async Program =>
                {
                    Console.WriteLine("Again!");
                    await Task.Delay(500);
                });

            secondProgram.Publish(new Ping().AsTask());

            Thread.Sleep(5000);

            thirdProgram.Unsubscribe<Pong>();
            
            Console.ReadLine();

            firstProgram.Unsubscribe<Ping>();
            secondProgram.Unsubscribe<Pong>();
        }

        private static async Task WritePing(Task<Ping> publisher)
        {
            Console.WriteLine("Ping...");
            await Task.Delay(500);
            await publisher.Publish(new Pong().AsTask());
        }

        private static async Task WritePong(Task<Pong> program)
        {
            Console.WriteLine("Pong!");
            await Task.Delay(500);
            await program.Publish(new Ping().AsTask());
        }

        public class Ping
        {
            
        }

        public class Pong
        {
            
        }
    }
}
