using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace PassProtect
{
    class Program
    {
        static void Main(string[] args)
        {
            ProgramStart();
        }
        static void ProgramStart()
        {
            //ProgramStart();
            string[] exceptions = { "RECYCLER" , "System Volume Information" , "$RECYCLE.BIN" , "$AVG" , "Config.msi" , "Windows" , "OneDriveTemp", "ProgramData" , "$WINRE_BACKUP_PARTITION.MARKER" };
            Console.Clear();
            Console.WriteLine("Welcome to the folder locker tool!");
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
                    Locking(exceptions);
                }
                if (unos == 2)
                {
                    Unlocking(exceptions);
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
        static void Locking(string[] exceptions)
        {
            try
            {
                // clear start menu
                Console.Clear();
                ShowAvailableDrives();
                // enter the partition letter
                Console.WriteLine("Please, enter the drive letter you wish to lock");
                string partition_letter = Convert.ToString(Console.ReadLine());
                // list that will contain all folder names
                //List<string> folder_names = new List<string>();
                // get all files from partition
                DirectoryInfo d = new DirectoryInfo(partition_letter + ":\\");
                DirectoryInfo[] Folders = d.GetDirectories();
                if (Folders.Length == 0)
                {
                    Console.WriteLine("No folders found on drive " + partition_letter + ":\\");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    Locking(exceptions);
                }
                else
                {
                    foreach (DirectoryInfo folder in Folders)
                    {
                        // before, it was adding files to the list here, and locking them in next commented foreach
                        if (CheckName(folder.Name, exceptions))
                        {
                            DirectoryInfo d1 = new DirectoryInfo(partition_letter + ":\\" + folder.Name);
                            d1.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
                        }
                    }
                    // locks all folders
                    //foreach (string folder_name in folder_names)
                    //{
                        
                    //}
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
                Locking(exceptions);
            }
        }

        static void Unlocking(string[] exceptions)
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
                    Unlocking(exceptions);
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

                    string set_password = "goodluck123";

                    IfPasswordIsWrong: //2
                    Console.WriteLine("");
                    Console.Write("Enter the password: ");
                    string input_password = Convert.ToString(Console.ReadLine()); // password input
                    if (input_password == set_password) // if password is correct
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
                Unlocking(exceptions);
            }
        }
        static void ShowAvailableDrives()
        {
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
                    Console.WriteLine("  Drive type: {0}", d.DriveType);
                    Console.WriteLine("---------------------");
                }
            }
        }
        static bool Unlock_All(List<string> folder_names, string slovo, string[] exceptions)
        {
            try
            {
                foreach (string folder_name in folder_names)
                {
                    if (CheckName(folder_name,exceptions))
                    {
                        DirectoryInfo d1 = new DirectoryInfo(slovo + ":\\" + folder_name);
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
