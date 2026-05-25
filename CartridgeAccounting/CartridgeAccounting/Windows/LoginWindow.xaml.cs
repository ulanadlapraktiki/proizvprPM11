using CartridgeAccounting.Data;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;

namespace CartridgeAccounting.Windows
{
    public partial class LoginWindow : Window
    {
        public static string CurrentUserRole { get; private set; } = "";
        public static string CurrentUserName { get; private set; } = "";

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Password.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Введите логин и пароль.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string query = "SELECT Role FROM Users WHERE Username = @Username AND PasswordHash = @Password";
                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@Username", username),
                    new SqlParameter("@Password", password)
                };
                object result = DatabaseHelper.ExecuteScalar(query, parameters);

                if (result != null)
                {
                    CurrentUserRole = result.ToString();
                    CurrentUserName = username;

                    Window targetWindow = null;
                    switch (CurrentUserRole)
                    {
                        case "Admin":
                            targetWindow = new MainWindow();
                            break;
                        case "Issuer":
                            targetWindow = new IssuedCartridgesWindow();
                            break;
                        case "Receiver":
                            targetWindow = new ReturnedCartridgesWindow();
                            break;
                        case "Preparer":
                            targetWindow = new ToRefillCartridgesWindow();
                            break;
                        case "Deliverer":
                            targetWindow = new FromRefillCartridgesWindow();
                            break;
                        default:
                            MessageBox.Show("Неизвестная роль. Обратитесь к администратору.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                    }
                    targetWindow.Show();
                    // Закрываем окно входа. Приложение не завершится, так как targetWindow активен.
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Неверный логин или пароль.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}