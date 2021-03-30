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
        void SendRoundResults(string result, int damageTaken);
    }

    [ServiceContract(CallbackContract = typeof(ICallback))]
    public interface I007Game
    {
        [OperationContract]
        string Join(string name);

        [OperationContract(IsOneWay = true)]
        void Leave(string name);

        [OperationContract(IsOneWay = true)]
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
                // Retrieve client's callback proxy and add user
                ICallback cb = OperationContext.Current.GetCallbackChannel<ICallback>();
                callbacks.Add(name, cb);
                return null;
            }

        }

        /// <summary>
        /// Removes player with given name from the game
        /// </summary>
        /// <param name="name">Use alias</param>
        public void Leave(string name)
        {
            if (callbacks.ContainsKey(name))
            {
                callbacks.Remove(name);
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
            playerRounds.Add(name, new PlayerRound(action, target));
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
            foreach (var cb in callbacks)
            {
                PlayerRound round = playerRounds[cb.Key];
                cb.Value.SendRoundResults(round.GetResult(), round.HealthLost);
            }
        }
    }
}
