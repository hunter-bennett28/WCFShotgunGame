/*
 * Library:         007GameLibrary.dll
 * Module:          PlayerRound.cs
 * Author:          Hunter Bennett, Connor Black
 * Date:            March 25, 2021
 * Description:     Class contains round information for each player
 */

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace _007GameLibrary
{
    [DataContract]
    public class PlayerRound
    {
        public PlayerActions Action { get; set; }
        public string Target { get; set; }
        [DataMember]
        public List<string> Results { get; set; }
        [DataMember]
        public int HealthLost { get; set; }
        public bool ShotHit { get; set; }
        public string PlayerName { get; set; }

        public PlayerRound(string name, PlayerActions action, string target = null)
        {
            PlayerName = name;
            Action = action;
            Target = target;
            HealthLost = 0;
            ShotHit = false;
            Results = new List<string>();
        }

        /// <summary>
        /// Handles player being shot, incrementing health lost if they did not block it
        /// </summary>
        /// <returns></returns>
        public bool ReceiveShot()
        {
            if (Action == PlayerActions.Block)
                return false;
            HealthLost++;
            return true;
        }

        /// <summary>
        /// Returns a results string based on their action and damage taken
        /// </summary>
        /// <returns></returns>
        public string GetResult(bool isSelf = false)
        {
            string result = "";
            switch (Action)
            {
                case PlayerActions.Shoot:
                    result += $"{(isSelf ? "Your" : $"{PlayerName}'s" )} shot " + (ShotHit
                    ? "hit "
                    : "was blocked by ") + $"{Target}!";
                    break;
                case PlayerActions.Block:
                    result += $"{(isSelf ? "You" : PlayerName)} blocked.";
                    break;
                case PlayerActions.Reload:
                    result += $"{(isSelf ? "You" : PlayerName)} reloaded.";
                    break;
            }

            return result;
        }

    }
}
