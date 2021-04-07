/*
 *  Program:        007Service.exe 
 *  Module:         Program.cs
 *  Author:         H. Bennett, C. Black
 *  Date:           March 30, 2021
 *  Description:    A host application for the 007GameManager WCF service
 */

using System;
using System.ServiceModel;
using _007GameLibrary;

namespace _007GameService
{
    class Program
    {
        static void Main(string[] args)
        {
            ServiceHost servHost = null;
            try
            {
                // Create the service host 
                servHost = new ServiceHost(typeof(_007GameManager));

                // Start the service
                servHost.Open();
                Console.WriteLine("Service started. Press any key to quit.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                // Key program going until user presses a key to exit
                Console.ReadKey();
                if (servHost != null)
                    servHost.Close();
            }
        }
    }
}
