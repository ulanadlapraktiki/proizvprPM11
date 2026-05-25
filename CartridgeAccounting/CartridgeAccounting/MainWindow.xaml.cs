using CartridgeAccounting.Windows;
using System.Windows;

namespace CartridgeAccounting
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }


            public void SetStorekeeperMode()
            {
                // Скрываем кнопки, недоступные кладовщику
                BtnAddCartridge.Visibility = Visibility.Collapsed;
                BtnReferences.Visibility = Visibility.Collapsed;
                // При желании можно также убрать кнопки отчётов или другие
            }
        

        private void BtnIssued_Click(object sender, RoutedEventArgs e)
        {
            var window = new IssuedCartridgesWindow();
            window.ShowDialog();
        }

        private void BtnReturned_Click(object sender, RoutedEventArgs e)
        {
            var window = new ReturnedCartridgesWindow();
            window.ShowDialog();
        }

        private void BtnToRefill_Click(object sender, RoutedEventArgs e)
        {
            var window = new ToRefillCartridgesWindow();
            window.ShowDialog();
        }
        private void BtnFromRefill_Click(object sender, RoutedEventArgs e)
        {
            var window = new FromRefillCartridgesWindow();
            window.ShowDialog();
        }
        private void BtnAddCartridge_Click(object sender, RoutedEventArgs e)
        {
            var window = new AddCartridgeWindow();
            window.ShowDialog();
        }
        private void BtnReferences_Click(object sender, RoutedEventArgs e)
        {
            var window = new ReferencesWindow();
            window.ShowDialog();
        }
    }
}