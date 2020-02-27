using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Client
{
    public partial class MainWindow : Window
    {
        ServiceRef.ServiceClient proxy = new ServiceRef.ServiceClient();
        MediaPlayer howl = new MediaPlayer();
        MediaPlayer menu = new MediaPlayer();
        MediaPlayer game = new MediaPlayer();
        bool menuPlay = true;
        bool gamePlay = true;
        int id = new Random().Next(100, 10000);

        public MainWindow()
        {
            InitializeComponent();
            
            howl.Open(new Uri(@"../../Sounds/Howl.wav", UriKind.Relative));
            menu.Open(new Uri(@"../../Sounds/Menu.mp3", UriKind.Relative));
            game.Open(new Uri(@"../../Sounds/Loop.mp3", UriKind.Relative));
            menu.MediaEnded += Menu_MediaEnded;
            game.MediaEnded += Game_MediaEnded;

            howl.Play();
            menu.Play();
        }

        private void Game_MediaEnded(object sender, EventArgs e)
        {
            if (gamePlay)
            {
                game.Position = TimeSpan.Zero;
                game.Play();
            }
        }
        private void Menu_MediaEnded(object sender, EventArgs e)
        {
            if (menuPlay)
            {
                menu.Position = TimeSpan.Zero;
                menu.Play();
            }
        }

        private void Border_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                proxy.AddClient(id);

                menuPlay = false;
                menu.Stop();
                howl.Stop();
                grStart.Visibility = Visibility.Collapsed;
                stWait.Visibility = Visibility.Visible;

                Thread thd = new Thread(new ThreadStart ( () => {
                    while (true)
                    {
                        if (proxy.CheckCl())
                        {
                            gamePlay = true;   

                            Dispatcher.Invoke(new Action(() => {
                                stWait.Visibility = Visibility.Collapsed;
                                game.Play();
                                grGame.Visibility = Visibility.Visible;
                            }), DispatcherPriority.Normal);

                            FillBoard();

                            while (true)
                            {
                                try
                                {
                                    Dispatcher.Invoke(new Action(() => imDice.IsEnabled = proxy.CheckMove(id)));
                                    Dispatcher.Invoke(new Action(() =>
                                    {
                                        ServiceRef.GameGrid tmp = proxy.OponentPos(id);
                                        Grid.SetColumn(imOponent, tmp.CrdCol);
                                        Grid.SetRow(imOponent, tmp.CrdRow);
                                    }), DispatcherPriority.Normal);
                                    Thread.Sleep(100);
                                }
                                catch
                                {
                                    gamePlay = false;
                                    game.Stop();
                                    proxy.Abort();
                                }
                            }
                        }
                        /*else
                        {
                            if (stWait.Visibility == Visibility.Collapsed)
                            {
                                try
                                {
                                    gamePlay = false;

                                    Dispatcher.Invoke(new Action(() =>
                                    {
                                        grGame.Visibility = Visibility.Collapsed;
                                        game.Stop();
                                        stWait.Visibility = Visibility.Visible;
                                    }), DispatcherPriority.Normal);
                                }
                                catch
                                {
                                    proxy.Abort();
                                    Application.Current.Shutdown();
                                }
                            }
                        }*/

                        Thread.Sleep(500);
                    }
                } ));

                thd.Start();             
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        void FillBoard()
        {
            ServiceRef.GameGrid[] lst = proxy.GetBoard();

            Dispatcher.Invoke(new Action(() =>
            {
                foreach (ServiceRef.GameGrid item in lst)
                {
                    Label tmp = new Label { Content = item.Num.ToString(), VerticalContentAlignment = VerticalAlignment.Center, HorizontalContentAlignment = HorizontalAlignment.Center, FontWeight = FontWeights.Bold, FontSize = 14 };
                    Grid.SetRow(tmp, item.CrdRow);
                    Grid.SetColumn(tmp, item.CrdCol);
                    if (item.MoveType == ServiceRef.GridType.BrownEnd || item.MoveType == ServiceRef.GridType.BrownStart)
                        tmp.Foreground = Brushes.HotPink;
                    else if (item.MoveType == ServiceRef.GridType.NavyStart || item.MoveType == ServiceRef.GridType.NavyEnd)
                        tmp.Foreground = Brushes.Navy;
                    else if (item.MoveType == ServiceRef.GridType.Red)
                        tmp.Foreground = Brushes.Green;
                    else if (item.MoveType == ServiceRef.GridType.StartEnd)
                        tmp.Foreground = Brushes.Lavender;

                    grGame.Children.Add(tmp);
                }
            }), DispatcherPriority.Normal);
        }

        private void Image_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Random rnd = new Random();
            Thread thd = new Thread(new ThreadStart(() =>
            {
                int num = 0;

                for (int i = 1; i < rnd.Next(5, 12); i++)
                {
                    num = rnd.Next(1, 7);
                    Dispatcher.Invoke(new Action(() => (sender as Image).Source = new BitmapImage(new Uri($"pack://application:,,,/Images/{num}.png")) ));
                    Thread.Sleep(350);
                }

                try
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        ServiceRef.GameGrid tmp = proxy.Move(id, num);

                        if (tmp.MoveType == ServiceRef.GridType.Red)
                            MessageBox.Show("You can move again!", "Bonus", MessageBoxButton.OK, MessageBoxImage.Information);
                        else if (tmp.Num == 8 || tmp.Num == 17 || tmp.Num == 20 || tmp.Num == 32 || tmp.Num == 40 || tmp.Num == 51)
                            MessageBox.Show("You can go further!", "Bonus", MessageBoxButton.OK, MessageBoxImage.Information);
                        else if (tmp.MoveType == ServiceRef.GridType.BrownStart)
                            MessageBox.Show("You should return!", "Trap", MessageBoxButton.OK, MessageBoxImage.Warning);
                        else if (tmp.MoveType == ServiceRef.GridType.StartEnd)
                        {
                            if (proxy.CheckIfWon(id))                                
                                MessageBox.Show("You won!", "Game is over.", MessageBoxButton.OK, MessageBoxImage.Information);
                            else
                                MessageBox.Show("Try again!", "Game is over.", MessageBoxButton.OK, MessageBoxImage.Hand);

                            gamePlay = false;
                            grGame.Visibility = Visibility.Collapsed;
                            game.Stop();
                            stWait.Visibility = Visibility.Visible;
                        }

                        Grid.SetColumn(imHero, tmp.CrdCol);
                        Grid.SetRow(imHero, tmp.CrdRow);
                    }));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }));
            thd.Start();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            proxy.DelClient(id);
            proxy.Abort();
        }
    }
}