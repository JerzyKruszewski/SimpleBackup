using Microsoft.Extensions.Configuration;
using SimpleBackup.Core;
using SimpleBackup.Npgsql.Core;
using System;
using System.Collections.Generic;

namespace SimpleBackup.Npgsql.Example
{
    internal class Program
    {
        private static void Main()
        {
            try
            {
                IConfiguration configuration = new ConfigurationBuilder().AddJsonFile("./config.json").Build();
                IDatabaseHandler database = new DatabaseHandler(configuration);
                foreach (KeyValuePair<string, string> item in database.GetBackupQueries())
                {
                    Console.WriteLine($"{item.Key}\n{item.Value}\n");
                    Console.ReadKey();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.ReadKey();
                throw;
            }
        }
    }
}
