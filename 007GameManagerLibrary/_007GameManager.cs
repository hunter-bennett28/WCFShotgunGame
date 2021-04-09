/*
 * Library:         007GameLibrary.dll
 * Module:          _007GameManager.cs
 * Author:          Hunter Bennett, Connor Black
 * Date:            March 25, 2021
 * Description:     Game manager library for multiplater 007 game. Contains the
 *                  game manager itself as well as the Serive and Callback contract
 *                  interfaces it uses.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text.RegularExpressions;

namespace _007GameLibrary
{
    // All Action options for players in a round
    public enum PlayerActions { Shoot, Reload, Block };

    /// <summary>
    /// Callback contract for game client
    /// </summary>
    public interface ICallback
    {
        [OperationContract(IsOneWay = true)]
        void StartGame();

        [OperationContract(IsOneWay = true)]
        void SendAllPlayers(string[] players);

        [OperationContract(IsOneWay = true)]
        void SendRoundResults(PlayerRound round);
        //void SendRoundResults(List<string> result, int damageTaken);

        [OperationContract(IsOneWay = true)]
        void ResetRound();
    }

    /// <summary>
    /// Service contract interface for communicating with client
    /// </summary>
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

    /// <summary>
    /// Game manager class library for 007 Game. Handles receiving, sending, and processing player data,
    /// as well as players joining and leaving.
    /// </summary>
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
                Console.WriteLine($"{name} joined.");

                // Retrieve client's callback proxy and add user
                ICallback cb = OperationContext.Current.GetCallbackChannel<ICallback>();
                callbacks.Add(name, cb);

                //Notify all clients of update
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
            Console.WriteLine($"{name} {(died ? "died" : "left")}!");
            if (callbacks.ContainsKey(name))
            {
                callbacks.Remove(name);
                NotifyPlayers();

                //Reset the round as someone might have selected them as a target
                playerRounds.Clear();

                if (!died && callbacks.Count > 1)
                    foreach (var cb in callbacks.Values)
                        cb.ResetRound();
                else if (callbacks.Count == 1)
                {
                    PlayerRound round = new PlayerRound("", PlayerActions.Shoot)
                    {
                        Results = new List<string>() { "You are the last player standing!" },
                        HealthLost = 0
                    };
                    callbacks.First().Value.SendRoundResults(round);
                } 
                else if (callbacks.Count == 0)
                    gameInProgress = false;
                Console.WriteLine($"Players remaining: {callbacks.Count}");
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
            gameInProgress = true;
            return true;
        }

        /// <summary>
        /// Handles a player submitting their action for a round
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

            // Process all shots taken by players
            foreach (var playerRound in playerRounds.Values)
                if (playerRound.Action == PlayerActions.Shoot)
                    playerRound.ShotHit = playerRounds[playerRound.Target].ReceiveShot();

            // Report results to all players
            // List<string> results = new List<string>();
            foreach (var cb in callbacks)
            {
                Regex playerName = new Regex(cb.Key);
                PlayerRound round = playerRounds[cb.Key];
                round.Results.Add(round.GetResult(true));
                foreach(var callback in callbacks)
                {
                    if (callback.Key == round.PlayerName)
                        continue;
                    string playerResult = playerRounds[callback.Key].GetResult();
                    if (playerName.IsMatch(playerResult))
                        playerResult = playerName.Replace(playerResult, "you");
                    round.Results.Add(playerResult);
                }
                
            }

            // Report results to all players
            foreach (var cb in callbacks)
                cb.Value.SendRoundResults(playerRounds[cb.Key]);

            //Update the players on each client as some might have left/died during the turn
            foreach (var cb in callbacks)
                cb.Value.SendAllPlayers(callbacks.Keys.ToArray());

            playerRounds.Clear();

            //Notify the last user
            if (callbacks.Count == 1)
            {
                var cb = callbacks.First();
                PlayerRound round = playerRounds[cb.Key];
                round.Results.Clear();
                round.Results.Add("You are the last player standing!");
                callbacks.Values.First().SendRoundResults(round);
            }
                
        }

        // ------------- Helper Methods -------------

        /// <summary>
        /// Notify all players of an updated player list
        /// </summary>
        public void NotifyPlayers()
        {
            //Get the string array
            string[] players = callbacks.Keys.ToArray();

            foreach (ICallback callback in callbacks.Values)
                callback.SendAllPlayers(players);
        }
    }
}
