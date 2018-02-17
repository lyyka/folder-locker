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
        Start_Menu:
            Console.Clear();
            Console.WriteLine("Welcome to the folder lock tool!");
            Console.WriteLine("");
            Console.WriteLine("1. Lock folders");
            Console.WriteLine("2. Unlock folders");
            Console.WriteLine("3. Close the program");
            Console.WriteLine("");
            try
            {
                int unos = Convert.ToInt32(Console.ReadLine());
                if (unos == 1)
                {
                    Locking();
                }
                if (unos == 2)
                {
                    Unlocking();
                }
                if (unos == 3)
                {
                    Environment.Exit(0);
                }
                if (unos < 1 || unos > 3)
                {
                    goto Start_Menu;
                }
            }
            catch
            {
                goto Start_Menu;
            }

        }
        static void Locking()
        {
        Start_Locking:
            try
            {
                // gets folders
                Console.Clear();
                Console.WriteLine("Please, input the letter of the partition you wish to lock");
                string partition_letter = Convert.ToString(Console.ReadLine());
                List<string> folder_names = new List<string>();
                DirectoryInfo d = new DirectoryInfo(partition_letter + ":\\");
                DirectoryInfo[] Files = d.GetDirectories();
                foreach (DirectoryInfo file in Files)
                {
                    folder_names.Add(file.Name);
                }
                if(folder_names.Count == 0)
                {
                    Console.WriteLine("No folders found! :(");
                    Console.ReadKey();
                    goto Start_Locking;
                }
                else
                {
                    // locks all folders
                    foreach(string folder_name in folder_names)
                    {
                        if (folder_name.Trim() != "")
                        {
                            DirectoryInfo d1 = new DirectoryInfo(partition_letter + ":\\" + folder_name);
                            d1.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
                        }
                    }
                    Console.WriteLine("");
                    Console.WriteLine("Folders are successfully locked!");
                    Console.ReadKey();
                    ProgramStart();
                }   
            }
            catch
            {
                Console.WriteLine("Something went wrong (maybe the partition you entered deos not exist)! :(");
                Console.ReadKey();
                goto Start_Locking;
            }
        }

        static void Unlocking()
        {
        Start_Unlocking:
            try
            {
                Console.Clear();
                // gets folders
                Console.WriteLine("Please, input the letter of the partition you wish to lock");
                string partition_letter = Convert.ToString(Console.ReadLine());
                List<string> folder_names = new List<string>();
                DirectoryInfo d = new DirectoryInfo(partition_letter + ":\\");
                DirectoryInfo[] Files = d.GetDirectories();
                foreach (DirectoryInfo file in Files)
                {
                    folder_names.Add(file.Name);
                }

                string set_password = "gudlak123";

            IfPasswordIsWrong: //2
                Console.WriteLine("");
                Console.WriteLine("Input password:");
                string input_password = Convert.ToString(Console.ReadLine()); // password input
                if (input_password == set_password) // if password is correct
                {
                    Console.WriteLine("");
                    Console.WriteLine("Folders:"); // shows all folders on the drive
                    int index = 0;
                    foreach(string folder_name in folder_names)
                    {
                        Console.WriteLine(index + "." + "" + folder_name);
                        index++;
                    }
                    IfFolderNumberWrong: //1
                    Console.WriteLine("");
                    if(Unlock_All(folder_names, partition_letter))
                    {
                        Console.WriteLine("Folders successfully unlocked!");
                    }
                    Console.WriteLine("Input the number of folder you wish to open (-1 if you don't wish to open any)");
                    int rednibroj = Convert.ToInt32(Console.ReadLine());
                    if (rednibroj <= folder_names.Count() - 1 && rednibroj >= -1) // ako je redni broj validan, folder se otvara
                    {
                        if(rednibroj >= 0)
                        {                 
                            Process.Start("explorer.exe", "E:\\" + folder_names[rednibroj]);
                        }
                        ProgramStart();
                    }
                    else // if folder number is not between -1 and max folder number
                    {
                        Console.WriteLine("That folder number does not exist");
                        goto IfFolderNumberWrong;
                    }
                }
                else // if password is incorrect
                {
                    Console.WriteLine("Wrong password");
                    goto IfPasswordIsWrong;
                }
            }
            catch
            {
                Console.WriteLine("This partition does not exist!");
                Console.ReadKey();
                goto Start_Unlocking;
            }
        }
        static bool Unlock_All(List<string> folder_names, string slovo)
        {
            try
            {
                foreach (string folder_name in folder_names)
                {
                    if (folder_name.Trim() != "" && folder_name != "System volume information" && folder_name != "RECYCLER")
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
    }
}
