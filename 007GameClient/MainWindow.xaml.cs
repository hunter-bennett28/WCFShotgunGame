/*
 * Program:         007Game.exe
 * Module:          MainWindow.xaml.cs
 * Author:          Hunter Bennett, Connor Black
 * Date:            March 25, 2021
 * Description:     A Windows WPF client that uses 007GameLibrary.dll via a WCF service.
 * 
 *                  Note that this requires running with administration priviledges
 *                  due to HTML connection type
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;
using _007GameLibrary;

namespace _007Game
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml 007 game client
    /// </summary>

    [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Reentrant, UseSynchronizationContext = false)]
    public partial class MainWindow : Window, ICallback
    {
        private string user;
        private string[] players;
        private int health, ammo;
        private int currentAction = -1;
        private I007Game gameManager;

        public MainWindow()
        {
            InitializeComponent();

            //Establish a connection to the server
            DuplexChannelFactory<I007Game> channel = new DuplexChannelFactory<I007Game>(this, "GameEndpoint"); //We pass this as it is an ICallback object
            gameManager = channel.CreateChannel();
        }

        // ------------- Button Handlers -------------

        /// <summary>
        /// Process the shoot action and send it to the game manager
        /// </summary>
        /// <param name="sender">The object sending the button press</param>
        /// <param name="e">The event args</param>
        private void OnShootClick(object sender, RoutedEventArgs e)
        {
            if (ammo > 0)
            {
                //Check if there is a target
                if (targetComboBox.Text != "")
                {
                    //Process the action and refrest/update the UI
                    gameManager.TakeAction(user, PlayerActions.Shoot, targetComboBox.Text);
                    --ammo;

                    currentAction = (int)PlayerActions.Shoot;
                    DisableActionButtons();
                    WaitingBox.Visibility = Visibility.Visible;
                    RefreshUI();
                }
                else
                    MessageBox.Show(
                        "Select a valid target!",
                        "Game Manager",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
            }
            else
                MessageBox.Show(
                  "You have no ammo!",
                  "007 Game Client",
                  MessageBoxButton.OK,
                  MessageBoxImage.Error);
        }

        /// <summary>
        /// Process the reload action and send it to the game manager
        /// </summary>
        /// <param name="sender">The object sending the button press</param>
        /// <param name="e">The event args</param>
        private void OnReloadClick(object sender, RoutedEventArgs e)
        {
            gameManager.TakeAction(user, PlayerActions.Reload);
            ++ammo;
            currentAction = (int)PlayerActions.Reload;
            DisableActionButtons();
            WaitingBox.Visibility = Visibility.Visible;
            RefreshUI();
        }

        /// <summary>
        /// Process the block action and send it to the game manager
        /// </summary>
        /// <param name="sender">The object sending the button press</param>
        /// <param name="e">The event args</param>
        private void OnBlockClick(object sender, RoutedEventArgs e)
        {
            gameManager.TakeAction(user, PlayerActions.Block);
            currentAction = (int)PlayerActions.Block;
            DisableActionButtons();
            WaitingBox.Visibility = Visibility.Visible;
            RefreshUI();
        }

        /// <summary>
        /// Tell the game manager to start, if it does not start then display an error
        /// </summary>
        /// <param name="sender">The object sending the button press</param>
        /// <param name="e">The event args</param>
        private void OnStartClick(object sender, RoutedEventArgs e)
        {
            if (!gameManager.StartGame())
            {
                Console.WriteLine("Game did not start");

                //Notify user game did not start properly
                MessageBox.Show(
                  "The game did not start due to insufficient players or a game is in progress",
                  "007 Game Client",
                  MessageBoxButton.OK,
                  MessageBoxImage.Warning);
            }
            else //Clear the round results box as there could be pre-game data in it
                ResultsText.Content = "";

        }

        /// <summary>
        /// Process the client request to join a game (if available)
        /// </summary>
        /// <param name="sender">The object sending the button press</param>
        /// <param name="e">The event args</param>
        private void OnJoinClick(object sender, RoutedEventArgs e)
        {
            try
            {
                user = userName.Text.Trim();

                //No username was passed
                if (user=="")
                {
                    Task.Run(() => MessageBox.Show(
                          "Please enter a name",
                          "007 Game Client",
                          MessageBoxButton.OK,
                          MessageBoxImage.Error));
                    return;
                }

                if (user.Length > 15)
                {
                    Task.Run(() => MessageBox.Show(
                        "Name too long (15 characters max)",
                        "007 Game Client",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error));
                    return;
                }

                string result = gameManager.Join(user);
                //Display the error message returned
                if (result != null)
                {
                    if (result == "Name already in use." || result == "Game currently in progress.")
                    {
                        //Notify user game did not start properly
                        Task.Run(() => MessageBox.Show(
                          result,
                          "007 Game Client",
                          MessageBoxButton.OK,
                          MessageBoxImage.Error));
                        return;
                    }

                    listPlayers.Items.Clear();
                    listPlayers.Items.Add(result); //Display the message in the player box
                }
                else
                {
                    buttonJoin.IsEnabled = false;
                    buttonStart.IsEnabled = true;
                    userName.IsEnabled = false;
                    PlayersViewBox.Visibility = Visibility.Visible;
                }
            }
            catch (Exception err)
            {
                //Notify user game did not start properly
                MessageBox.Show(
                  err.Message,
                  "007 Game Client",
                  MessageBoxButton.OK,
                  MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handler for OK button in game ending dialog. Closes dialog and resets content
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnGameEndOKClick(object sender, RoutedEventArgs e)
        {
            GameEndText.Content = "";
            GameEndScreen.Visibility = Visibility.Hidden;
        }

        // ------------- Callback Methods -------------

        /// <summary>
        /// The callback that starts the game for all players
        /// </summary>
        public void StartGame()
        {
            //Only one thread may manage the GUI
            if (System.Threading.Thread.CurrentThread != Dispatcher.Thread)
            {
                //Hand the task to the dispatcher
                Dispatcher.BeginInvoke(new Action(StartGame));
                return;
            }

            //Set up the game screen
            health = 3;
            ammo = 1;

            userNameLabel.Content = user;
            RefreshUI();

            //Hide the start window and show the game window
            HomeScreen.Visibility = Visibility.Hidden;
            GameScreen.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Recieve all players from the game manager
        /// </summary>
        /// <param name="players">The players currently in the game</param>
        public void SendAllPlayers(string[] allPlayers)
        {
            //Only one thread may manage the GUI
            if (System.Threading.Thread.CurrentThread != Dispatcher.Thread)
            {
                //Hand the task to the dispatcher
                Dispatcher.BeginInvoke(new Action<string[]>(SendAllPlayers), new object[] { allPlayers }); //Cannot pass raw players as it excepts an object[]
                return;
            }

            //Update the potential targets
            targetComboBox.ItemsSource = allPlayers.Where(player => player != user);
            targetComboBox.SelectedIndex = 0;

            //Find any players who died
            if (players != null)
                foreach (string player in players)
                    if (!allPlayers.Contains(player))
                        ResultsText.Content = $"{ResultsText.Content}\n{player} died!";

            listPlayers.Items.Clear();
            //Add each player to the list
            Array.ForEach(allPlayers, (player) => listPlayers.Items.Add(player));
            players = allPlayers;
        }

        /// <summary>
        /// Helper function that moves a result string to the start of the list
        /// </summary>
        /// <param name="list"></param>
        /// <param name="index"></param>
        public void SwapResultIndexWithStart(List<string> list, int index)
        {
            if (index < 1)
                return;

            string temp = list[0];
            list[0] = list[index];
            list[index] = temp;
        }

        /// <summary>
        /// Recieve round results from the game manager
        /// </summary>
        /// <param name="round">The object from server containing the players round result information</param>
        public void SendRoundResults(PlayerRound round)
        {
            //Only one thread may manage the GUI
            if (System.Threading.Thread.CurrentThread != Dispatcher.Thread)
            {
                //Hand the task to the dispatcher
                Dispatcher.BeginInvoke(new Action<PlayerRound>(SendRoundResults), new object[] { round }); //Cannot pass raw players as it excepts an object[]
                return;
            }

            //Update health and the current action
            currentAction = -1;
            health = health - round.HealthLost < 0 ? 0 : health - round.HealthLost;
            WaitingBox.Visibility = Visibility.Hidden;
            RefreshUI();

            //Check if the player won (in the event of a tie, it will just show that both died)
            if (health > 0 && round.Results[0] == "You are the last player standing!")
            {
                GameEndText.Content = round.Results[0];
                GameEndScreen.Visibility = Visibility.Visible;
                ResetGameState();
            }
            else
            {
                // Ensure results don't go out of bounds
                for (int i = 0; i < round.Results.Count; ++i)
                {
                    if (round.Results[i].Length > 29)
                    {
                        int lastSpace = round.Results[i].Substring(0, 29).LastIndexOf(' ');
                        round.Results[i] = $"{round.Results[i].Substring(0, lastSpace)}\n{round.Results[i].Substring(lastSpace + 1)}";
                    }
                }

                // Temporary displaying the result
                ResultsText.Content = round.Results.Aggregate((r1, r2) => r1 + '\n' + r2); //Store all round results
                EnableActionButtons();

                if (health <= 0)
                {
                    GameEndText.Content = "You have died!";
                    GameEndScreen.Visibility = Visibility.Visible;
                    ResetGameState(true);
                }
            }
        }

        /// <summary>
        /// Reset the round if a player left mid-round
        /// </summary>
        public void ResetRound()
        {
            //Only one thread may manage the GUI
            if (System.Threading.Thread.CurrentThread != Dispatcher.Thread)
            {
                //Hand the task to the dispatcher
                Dispatcher.BeginInvoke(new Action(ResetRound));
                return;
            }

            EnableActionButtons();
            ResultsText.Content = "A player has left!\nThis round has reset";

            //Reset the users health/ammo if there was a current action
            switch (currentAction)
            {
                case (int)PlayerActions.Shoot:
                    ammo++;
                    currentAction = -1;
                    break;
                case (int)PlayerActions.Reload:
                    ammo--;
                    currentAction = -1;
                    break;
                default:
                    break;
            }
        }

        // ---------- Helper Methods ----------

        /// <summary>
        /// Disable the 3 action buttons
        /// </summary>
        private void DisableActionButtons()
        {
            buttonBlock.IsEnabled = false;
            buttonShoot.IsEnabled = false;
            buttonReload.IsEnabled = false;
        }

        /// <summary>
        /// Enable the 3 action buttons
        /// </summary>
        private void EnableActionButtons()
        {
            buttonBlock.IsEnabled = true;
            buttonShoot.IsEnabled = true;
            buttonReload.IsEnabled = true;
        }

        /// <summary>
        /// Refresh the health and ammo labels
        /// </summary>
        private void RefreshUI()
        {
            targetComboBox.SelectedIndex = 0;
            healthLabel.Content = health;
            ammoLabel.Content = ammo;
        }

        /// <summary>
        /// Resets the client's game state back to the initial state
        /// </summary>
        /// <param name="died"></param>
        private void ResetGameState(bool died = false)
        {
            //Unsubscribe from the callbacks
            gameManager.Leave(user, died);

            //Reset the windows
            GameScreen.Visibility = Visibility.Hidden;
            HomeScreen.Visibility = Visibility.Visible;
            PlayersViewBox.Visibility = Visibility.Hidden;

            buttonJoin.IsEnabled = true;
            buttonStart.IsEnabled = false;
            userName.IsEnabled = true;

            players = null;
            listPlayers.Items.Clear();
            ResultsText.Content = "";
        }

        /// <summary>
        /// Window closing handler that leaves the game before exiting
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //On close, we want to leave the lobby/game
            if (user != null)
                gameManager.Leave(user, false);
        }


    }
}
