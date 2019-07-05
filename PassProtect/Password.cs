using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace PassProtect
{
    class Password
    {
        public RegistryKey Key { get; private set; }
        public Password() {
            Key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\FolderLocker", true);

            if(Key == null)
            {
                Key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\FolderLocker");
                ShowDialog();
            }
            else
            {
                if(Key.GetValue("password") == null)
                {
                    ShowDialog();
                }
            }
        }

        private string GetInput()
        {
            Console.Write("Password: ");
            string password = "";
            do
            {
                ConsoleKeyInfo cons_key = Console.ReadKey(true);
                // Backspace Should Not Work
                if (cons_key.Key != ConsoleKey.Backspace && cons_key.Key != ConsoleKey.Enter)
                {
                    password += cons_key.KeyChar;
                    Console.Write("*");
                }
                else
                {
                    if (cons_key.Key == ConsoleKey.Backspace && password.Length > 0)
                    {
                        password = password.Substring(0, (password.Length - 1));
                        Console.Write("\b \b");
                    }
                    else if (cons_key.Key == ConsoleKey.Enter)
                    {
                        break;
                    }
                }
            } while (true);

            return password;
        }

        private void ShowDialog()
        {
            Console.WriteLine("Please create a unique password for use to lock drives: ");

            string password = GetInput();

            // hash the password
            string hashed = Hash(password);

            // save password hash
            Key.SetValue("password", hashed);

            // set current date and time
            Key.SetValue("last_updated", DateTime.Now);

            Console.Clear();
        }

        public bool Validate()
        {
            string password = GetInput();

            bool valid = true;
            /* Extract the bytes */
            byte[] hashBytes = Convert.FromBase64String(Key.GetValue("password").ToString());
            /* Get the salt */
            byte[] salt = new byte[16];
            Array.Copy(hashBytes, 0, salt, 0, 16);
            /* Compute the hash on the password the user entered */
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000);
            byte[] hash = pbkdf2.GetBytes(20);
            /* Compare the results */
            for (int i = 0; i < 20 && valid; i++)
                if (hashBytes[i + 16] != hash[i])
                    valid = false;

            return valid;
        }

        private string Hash(string pass)
        {
            byte[] salt;
            new RNGCryptoServiceProvider().GetBytes(salt = new byte[16]);

            var pbkdf2 = new Rfc2898DeriveBytes(pass, salt, 10000);
            byte[] hash = pbkdf2.GetBytes(20);

            byte[] hashBytes = new byte[36];
            Array.Copy(salt, 0, hashBytes, 0, 16);
            Array.Copy(hash, 0, hashBytes, 16, 20);

            return Convert.ToBase64String(hashBytes);
        }

    }
}
