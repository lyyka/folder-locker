using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using Microsoft.Win32;
using System.Security.Cryptography;
using System.Threading;

namespace PassProtect
{
    class Program
    {
        static void Main(string[] args)
        {
            ProgramStart();
        }

        static string HashPassword(string password)
        {
            byte[] salt;
            new RNGCryptoServiceProvider().GetBytes(salt = new byte[16]);

            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000);
            byte[] hash = pbkdf2.GetBytes(20);

            byte[] hashBytes = new byte[36];
            Array.Copy(salt, 0, hashBytes, 0, 16);
            Array.Copy(hash, 0, hashBytes, 16, 20);

            return Convert.ToBase64String(hashBytes);
        }

        static bool ComparePassword(string hashed_pass, string password)
        {
            bool valid = true;
            /* Extract the bytes */
            byte[] hashBytes = Convert.FromBase64String(hashed_pass);
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

        static void ShowPasswordCreation(RegistryKey key)
        {
            Console.WriteLine("Please create a unique password for use to lock folders: ");
            string password = Console.ReadLine();
            string hashed = HashPassword(password);
            key.SetValue("password", hashed);
            Console.Clear();
        }

        static void ProgramStart()
        {
            //ProgramStart();
            string[] exceptions = { "RECYCLER" , "System Volume Information" , "$RECYCLE.BIN" , "$AVG" , "Config.msi" , "Windows" , "OneDriveTemp", "ProgramData" , "$WINRE_BACKUP_PARTITION.MARKER" };
            Console.Clear();
            Console.WriteLine("Welcome to the folder locker tool!");

            bool password_set = false;
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\FolderLocker", true);
            if(key == null)
            {
                key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\FolderLocker");
                ShowPasswordCreation(key);
            }
            else
            {
                password_set = key.GetValue("password") != null;
                if (!password_set)
                {
                    ShowPasswordCreation(key);
                }
            }

            Console.WriteLine("--Enter the number to start--");
            Console.WriteLine("1 -> Lock folders");
            Console.WriteLine("2 -> Unlock folders");
            Console.WriteLine("3 -> Close this");
            Console.WriteLine("");
            try
            {
                int unos = Convert.ToInt32(Console.ReadLine());
                if (unos == 1)
                {
                    Locking(exceptions, key);
                }
                if (unos == 2)
                {
                    Unlocking(exceptions, key);
                }
                if (unos == 3)
                {
                    Environment.Exit(0);
                }
                if (unos < 1 || unos > 3)
                {
                    ProgramStart();
                }
            }
            catch
            {
                ProgramStart();
            }

        }
        static void Locking(string[] exceptions, RegistryKey key)
        {
            try
            {
                // clear start menu
                Console.Clear();
                ShowAvailableDrives();
                Timer t = new Timer(TimerShowDrives, null, 0, 2000);
                // enter the partition letter
                Console.WriteLine("Please, enter the drive letter you wish to lock");
                string partition_letter = Convert.ToString(Console.ReadLine());
                // list that will contain all folder names
                //List<string> folder_names = new List<string>();
                // get all files from partition
                DirectoryInfo d = new DirectoryInfo(partition_letter + ":\\");
                DirectoryInfo[] folders_on_drive = d.GetDirectories();
                if (folders_on_drive.Length == 0)
                {
                    Console.WriteLine("No folders found on drive " + partition_letter + ":\\");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    Locking(exceptions, key);
                }
                else
                {
                    foreach (DirectoryInfo folder in folders_on_drive)
                    {
                        // before, it was adding files to the list here, and locking them in next commented foreach
                        if (CheckName(folder.Name, exceptions))
                        {
                            DirectoryInfo d1 = new DirectoryInfo(partition_letter + ":\\" + folder.Name);
                            d1.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
                        }
                    }
                    Console.WriteLine("");
                    Console.WriteLine("All folders are successfully locked!");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    ProgramStart();
                }
            }
            catch
            {
                Console.WriteLine("Something went wrong (maybe the drive you entered does not exist)! :(");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                Locking(exceptions, key);
            }
        }

        static void Unlocking(string[] exceptions, RegistryKey key)
        {
            try
            {
                // clear start menu
                Console.Clear();
                ShowAvailableDrives();
                // enter the partition letter
                Console.WriteLine("Please, enter the drive letter you wish to unlock");
                string partition_letter = Convert.ToString(Console.ReadLine());
                // folder getters
                DirectoryInfo d = new DirectoryInfo(partition_letter + ":\\");
                DirectoryInfo[] Folders = d.GetDirectories();
                if(Folders.Length == 0)
                {
                    Console.WriteLine("No folders found on drive " + partition_letter + ":\\");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    Unlocking(exceptions, key);
                }
                else
                {
                    // the list that holds folder names
                    List<string> folder_names = new List<string>();
                    // get folder names
                    foreach (DirectoryInfo folder in Folders)
                    {
                        if (CheckName(folder.Name, exceptions))
                        {
                            folder_names.Add(folder.Name);
                        }
                    }

                    IfPasswordIsWrong: //2
                    Console.WriteLine("");
                    Console.Write("Enter the password: ");
                    string input_password = Convert.ToString(Console.ReadLine()); // password input
                    if (ComparePassword(key.GetValue("password").ToString(), input_password)) // if password is correct
                    {
                        Console.WriteLine("");
                        Console.WriteLine("Folders:"); // shows all folders on the drive
                        // print out all folders with their index numbers
                        int index = 0;
                        foreach (string folder_name in folder_names)
                        {
                            Console.WriteLine(index + "." + "" + folder_name);
                            index++;
                        }
                        Console.WriteLine("");
                        // unlock all
                        if (Unlock_All(folder_names, partition_letter, exceptions))
                        {
                            Console.WriteLine("Folders successfully unlocked!");
                        }
                        else
                        {
                            Console.WriteLine("There was a problem unlocking all folders on a selected drive!");
                        }
                        IfFolderNumberWrong: //1
                        // user enters the folder index he wishes to open
                        Console.WriteLine("Input the number of a folder you wish to open (just hit ENTER if you wish to open none)");
                        string user_input = Console.ReadLine();
                        if(user_input.Trim() != "")
                        {
                            int folder_number_to_open = Convert.ToInt32(user_input);
                            if (folder_number_to_open <= folder_names.Count() - 1 && folder_number_to_open >= 0) // if folder number is valid, folder opens
                            {
                                Process.Start("explorer.exe", partition_letter + ":\\" + folder_names[folder_number_to_open]);
                                ProgramStart();
                            }
                            else if (folder_number_to_open < 0 || folder_number_to_open >= folder_names.Count()) // if folder number is not between 0 and max folder number
                            {
                                Console.WriteLine("That folder number does not exist");
                                goto IfFolderNumberWrong;
                            }
                        }
                        else
                        {
                            ProgramStart();
                        }
                    }
                    else // if password is incorrect
                    {
                        Console.WriteLine("Wrong password");
                        goto IfPasswordIsWrong;
                    }
                }
            }
            catch
            {
                Console.WriteLine("Something went wrong (maybe the drive you entered does not exist)! :(");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                Unlocking(exceptions, key);
            }
        }
        static void ShowAvailableDrives()
        {
            Console.Clear();

            DriveInfo[] allDrives = DriveInfo.GetDrives();

            if(allDrives.Length > 0)
            {
                Console.WriteLine("Here is the list of available drives:");
            }

            foreach (DriveInfo d in allDrives)
            {
                if (d.IsReady == true)
                {
                    Console.WriteLine("Drive {0}", d.Name);
                    Console.WriteLine("  Drive Type: {0}", d.DriveType);
                    Console.WriteLine("  Drive Size: {0} GB ({1} GB free)", d.TotalSize/(1024 * 1024 * 1024), d.AvailableFreeSpace/(1024*1024*1024));

                    DirectoryInfo drive_root = d.RootDirectory.Root;
                    Console.WriteLine("  Folders: {0}", drive_root.GetDirectories().Length);
                    Console.WriteLine("  Files: {0}", drive_root.GetFiles().Length);

                    Console.WriteLine("---------------------");
                }
            }
        }

        private static void TimerShowDrives(Object o)
        {
            ShowAvailableDrives();
            GC.Collect();
        }
        static bool Unlock_All(List<string> folder_names, string drive_letter, string[] exceptions)
        {
            try
            {
                foreach (string folder_name in folder_names)
                {
                    if (CheckName(folder_name,exceptions))
                    {
                        DirectoryInfo d1 = new DirectoryInfo(drive_letter + ":\\" + folder_name);
                        d1.Attributes = FileAttributes.Directory | FileAttributes.Normal;
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
        static bool CheckName(string folder_name,string[] exceptions)
        {
            // check if folder name is not empty
            if(folder_name.Trim() == "")
            {
                return false;
            }
            // check if it is not part of the exceptions
            foreach(string exception in exceptions)
            {
                if(folder_name.ToLower() == exception.ToLower())
                {
                    return false;
                }
            }
            return true;
        }
    }
}
