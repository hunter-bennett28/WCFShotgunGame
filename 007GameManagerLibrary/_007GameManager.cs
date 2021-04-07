using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;

namespace _007GameLibrary
{
    public enum PlayerActions { Shoot, Reload, Block };

    public interface ICallback
    {
        [OperationContract(IsOneWay = true)]
        void StartGame();

        [OperationContract(IsOneWay = true)]
        void SendAllPlayers(string[] players);

        [OperationContract(IsOneWay = true)]
        void SendRoundResults(List<string> result, int damageTaken);

        [OperationContract(IsOneWay = true)]
        void ResetRound();
    }

    [ServiceContract(CallbackContract = typeof(ICallback))]
    public interface I007Game
    {
        [OperationContract]
        string Join(string name);

        [OperationContract(IsOneWay = true)]
        void Leave(string name, bool died);

        [OperationContract]
        bool StartGame();

        [OperationContract(IsOneWay = true)]
        void TakeAction(string name, PlayerActions action, string target = null);

    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class _007GameManager : I007Game
    {
        private Dictionary<string, ICallback> callbacks = new Dictionary<string, ICallback>();
        private Dictionary<string, PlayerRound> playerRounds = new Dictionary<string, PlayerRound>();
        private bool gameInProgress = false;

        /// <summary>
        /// Joins the game with the given alias if available and game not in progress
        /// </summary>
        /// <param name="name">User alias</param>
        /// <returns>A string containing an error message or null if succeeded</returns>
        public string Join(string name)
        {
            // Check if name already in use, or if game currently in progress
            if (gameInProgress)
                return "Game currently in progress.";
            else if (callbacks.ContainsKey(name))
                return "Name already in use.";
            else
            {
                Console.WriteLine($"{name} joining");

                // Retrieve client's callback proxy and add user
                ICallback cb = OperationContext.Current.GetCallbackChannel<ICallback>();
                callbacks.Add(name, cb);

                //Notify all clients of update
                Console.WriteLine("Notifying players");
                NotifyPlayers();

                return null;
            }
        }

        /// <summary>
        /// Removes player with given name from the game
        /// </summary>
        /// <param name="name">Use alias</param>
        public void Leave(string name, bool died)
        {
            Console.WriteLine($"User left");
            if (callbacks.ContainsKey(name))
            {
                Console.WriteLine($"{name} is leaving the callbacks");
                callbacks.Remove(name);
                NotifyPlayers();

                //Reset the round as someone might have selected them as a target
                playerRounds.Clear();

                if (!died && callbacks.Count > 1)
                    foreach (var cb in callbacks.Values)
                        cb.ResetRound();
                else if (callbacks.Count == 1)
                    callbacks.First().Value.SendRoundResults(new List<string>() { "You are the last player standing!" }, 0);

            }
        }

        /// <summary>
        /// Handles a user pressing the start game button
        /// </summary>
        /// <returns></returns>
        public bool StartGame()
        {
            if (callbacks.Count < 2 || gameInProgress)
                return false;

            foreach (ICallback cb in callbacks.Values)
                cb.StartGame();
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">The player's name</param>
        /// <param name="action">The action they have chosen</param>
        /// <param name="target">Optional target name, only used when they take the Shoot action</param>
        public void TakeAction(string name, PlayerActions action, string target = null)
        {
            playerRounds.Add(name, new PlayerRound(name, action, target));
            ProcessRound();
        }

        /// <summary>
        /// Processes all shots taken and then returns round results to each client
        /// </summary>
        public void ProcessRound()
        {
            // If there are still players who are deciding, do not complete round
            if (playerRounds.Count != callbacks.Count)
                return;

            Console.WriteLine("\nProcessing round");

            // Process all shots taken by players
            foreach (var playerRound in playerRounds.Values)
                if (playerRound.Action == PlayerActions.Shoot)
                    playerRound.ShotHit = playerRounds[playerRound.Target].ReceiveShot();

            //Get each Players results and concat it into a string[]


            // Report results to all players
            List<string> results = new List<string>();
            foreach (var cb in callbacks)
            {
                PlayerRound round = playerRounds[cb.Key];
                results.Add(round.GetResult());
            }

            // Report results to all players
            foreach (var cb in callbacks)
            {
                PlayerRound round = playerRounds[cb.Key];
                cb.Value.SendRoundResults(results, round.HealthLost);
            }

            //Update the players on each client as some might have left/died during the turn
            foreach (var cb in callbacks)
            {
                cb.Value.SendAllPlayers(callbacks.Keys.ToArray());
            }

            playerRounds.Clear();
            Console.WriteLine($"Players remaining: {callbacks.Count}");

            //Notify the last user
            if (callbacks.Count == 1)
                callbacks.Values.First().SendRoundResults(new List<string>() { "You are the last player standing!" }, 0);
        }

        // ------------- Helper Methods -------------

        /// <summary>
        /// Notify all players of an updated player list
        /// </summary>
        public void NotifyPlayers()
        {
            //Get the string array
            string[] players = callbacks.Keys.ToArray();
            foreach (string player in players)
                Console.WriteLine(player);

            Console.WriteLine($"Notifying updated players:");
            //Array.ForEach(players, Console.Write);
            Console.WriteLine($"\nCallbacks to make: {callbacks.Count}\nPlayers in lobby: {players.Length}");
            foreach (ICallback callback in callbacks.Values)
            {
                callback.SendAllPlayers(players);
            }
        }
    }
}
