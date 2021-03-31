using System;
using System.Collections.Generic;
using System.Linq;
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

namespace _007Game
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void onShootClick(object sender, RoutedEventArgs e)
        {
            //TODO: Register action as a shoot
        }

        private void onReloadClick(object sender, RoutedEventArgs e)
        {
            //TODO: Register action as a reload
        }

        private void onBlockClick(object sender, RoutedEventArgs e)
        {
            //TODO: Register the action as a block and send to the manager
        }

        private void onStartClick(object sender, RoutedEventArgs e)
        {
            //TODO: Initiate the game (and notify all players), then display proper screen
        }

        private void onJoinClick(object sender, RoutedEventArgs e)
        {
            //TODO: Register with the game manager and wait until a player starts the game
            //      Check if there is a game available
        }
    }
}
