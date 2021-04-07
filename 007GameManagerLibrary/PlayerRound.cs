namespace _007GameLibrary
{
    /// <summary>
    /// Class containing data for actions taken by 007Game players in a round
    /// </summary>
    public class PlayerRound
    {
        public PlayerActions Action { get; set; }
        public string Target { get; set; }
        public string Result { get; set; }
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
        public string GetResult()
        {
            string result = "";
            switch (Action)
            {
                case PlayerActions.Shoot:
                    result += $"{PlayerName}'s shot " + (ShotHit
                    ? "hit"
                    : "was blocked by") + $"{Target}";
                    break;
                case PlayerActions.Block:
                    result += $"{PlayerName} blocked!";
                    break;
                case PlayerActions.Reload:
                    result += $"{PlayerName} reloaded!";
                    break;
            }
            //if (Action == PlayerActions.Shoot)
            //    result += ShotHit
            //        ? $"{PlayerName}'s shot hit {Target}! "
            //        : $"{Target} blocked {PlayerName}'s shot! ";
            //if (HealthLost > 1)
            //    result += $"{PlayerName} was shot{(HealthLost > 1 ? $" {HealthLost} times!" : "!")}";

            return result;
        }

    }
}
