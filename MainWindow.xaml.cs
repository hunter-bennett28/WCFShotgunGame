using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using _007GameLibrary;

namespace _007Game
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Reentrant, UseSynchronizationContext = false)]
    public partial class MainWindow : Window, ICallback
    {
        private string user = null;
        private string[] players = null; //Stored for shoot action
        private int health, ammo;
        private int currentAction = -1;
        private I007Game gameManager = null;
        public MainWindow()
        {
            InitializeComponent();

            //Establish a connection to the server
            DuplexChannelFactory<I007Game> channel = new DuplexChannelFactory<I007Game>(this, "GameEndpoint"); //We pass this as it is an ICallback object
            gameManager = channel.CreateChannel();
        }

        /// <summary>
        /// Process the shoot action and send it to the game manager
        /// </summary>
        /// <param name="sender">The object sending the button press</param>
        /// <param name="e">The event args</param>
        private void onShootClick(object sender, RoutedEventArgs e)
        {
            //TODO: Register action as a shoot
            //          - Pick who is being shot from the players array

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
        private void onReloadClick(object sender, RoutedEventArgs e)
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
        private void onBlockClick(object sender, RoutedEventArgs e)
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
        private void onStartClick(object sender, RoutedEventArgs e)
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
        }

        /// <summary>
        /// Process the client request to join a game (if available)
        /// </summary>
        /// <param name="sender">The object sending the button press</param>
        /// <param name="e">The event args</param>
        private void onJoinClick(object sender, RoutedEventArgs e)
        {
            try
            {
                user = userName.Text;
                string result = gameManager.Join(user);

                //Display the error message returned
                if (result != null)
                {
                    if (result == "Name already in use.")
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

        // ------------- Callback Methods -------------
        private delegate void StartGameDelegate();
        /// <summary>
        /// The callback that starts the game for all players
        /// </summary>
        public void StartGame()
        {
            //Only one thread may manage the GUI
            if (System.Threading.Thread.CurrentThread != Dispatcher.Thread)
            {
                //Hand the task to the dispatcher
                Dispatcher.BeginInvoke(new StartGameDelegate(StartGame));
                return;
            }

            //Set up the game screen
            health = 3;
            ammo = 3;

            userNameLabel.Content = user;
            RefreshUI();

            //Hide the start window and show the game window
            HomeScreen.Visibility = Visibility.Hidden;
            GameScreen.Visibility = Visibility.Visible;
        }

        private delegate void SendAllPlayersDelegate(string[] players);
        /// <summary>
        /// Recieve all players from the game manager
        /// </summary>
        /// <param name="players">The players currently in the game</param>
        public void SendAllPlayers(string[] players)
        {
            //Only one thread may manage the GUI
            if (System.Threading.Thread.CurrentThread != Dispatcher.Thread)
            {
                //Hand the task to the dispatcher
                Dispatcher.BeginInvoke(new SendAllPlayersDelegate(SendAllPlayers), new object[] { players }); //Cannot pass raw players as it excepts an object[]
                return;
            }

            //Update the potential targets
            targetComboBox.ItemsSource = players.Where(player => player != user);

            listPlayers.Items.Clear();
            //Add each player to the list
            Array.ForEach(players, (player) => listPlayers.Items.Add(player));
            this.players = players;
        }

        private delegate void SendRoundResultsDelegate(List<string> result, int damageTaken);

        /// <summary>
        /// Recieve round results from the game manager
        /// </summary>
        /// <param name="result">The string result to display</param>
        /// <param name="damageTaken">The damage taken</param>
        public void SendRoundResults(List<string> result, int damageTaken)
        {
            //Only one thread may manage the GUI
            if (System.Threading.Thread.CurrentThread != Dispatcher.Thread)
            {
                //Hand the task to the dispatcher
                Dispatcher.BeginInvoke(new SendRoundResultsDelegate(SendRoundResults), new object[] { result, damageTaken }); //Cannot pass raw players as it excepts an object[]
                return;
            }

            //Update health and the current action
            currentAction = -1;
            health = health - damageTaken < 0 ? 0 : health - damageTaken;
            WaitingBox.Visibility = Visibility.Hidden;
            RefreshUI();

            //Check if the player won (in the event of a tie, it will just show that both died)
            if (health > 0 && result[0] == "You are the last player standing!")
            {
                Task.Run(() =>
                {
                    MessageBox.Show(
                        result[0],
                        $"{user} Round Results",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                        );
                });
                ResetGameState();
            }
            else
            {
                //Temporary displaying the result
                ResultsText.Content = result.Aggregate((r1, r2) => r1 + ' ' + r2); //Store all round results
                EnableActionButtons();

                if (health <= 0)
                {
                    Task.Run(() =>
                    {
                        MessageBox.Show(
                          "You have died!",
                           $"{user} Round Results",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);
                    });
                    ResetGameState(true);
                }
            }
        }

        public delegate void ResetRoundDelegate();

        /// <summary>
        /// Reset the round if a player left mid-round
        /// </summary>
        public void ResetRound()
        {
            //Only one thread may manage the GUI
            if (System.Threading.Thread.CurrentThread != Dispatcher.Thread)
            {
                //Hand the task to the dispatcher
                Dispatcher.BeginInvoke(new ResetRoundDelegate(ResetRound));
                return;
            }

            EnableActionButtons();
            ResultsText.Content = "A player has left! This round has reset";

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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //On close, we want to leave the lobby/game
            if (user != null)
                gameManager.Leave(user, false);
        }


    }
}
