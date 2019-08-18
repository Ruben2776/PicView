using System;
using System.Collections.Generic;
using System.IO;

namespace PicView
{
    /// <summary>
    /// Class to handle Most Recently Used files
    /// </summary>
    internal static class RecentFiles
    {

        /// <summary>
        /// File list for Most Recently Used files
        /// </summary>
        internal static Queue<string> MRUlist;

        /// <summary>
        /// How many max recent files
        /// </summary>
        const int MRUcount = 7;

        static bool zipped;

        internal static void Initialize()
        {
            MRUlist = new Queue<string>();
            zipped = false;

            LoadRecent();
        }

        internal static void LoadRecent()
        {
            MRUlist.Clear();
            try
            {
                // Read file stream
                var listToRead = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + "\\Recent.txt");
                string line;

                // Read each line until end of file
                while ((line = listToRead.ReadLine()) != null)
                    MRUlist.Enqueue(line);

                listToRead.Close();
            }
            catch (Exception) { }
        }


        /// <summary>
        /// Function to add file to MRU
        /// </summary>
        /// <returns></returns>
        internal static void Add(string fileName)
        {
            // Don't add zipped files
            if (zipped) 
                return;

            // Prevent duplication on recent list
            if (!(MRUlist.Contains(fileName)))
                MRUlist.Enqueue(fileName);

            // Keep list number not exceeding max value
            while (MRUlist.Count > MRUcount)
            {
                MRUlist.Dequeue();
            }
        }

        /// <summary>
        /// Returns all values string[]
        /// </summary>
        internal static string[] LoadValues()
        {
            if (MRUlist == null)
                return null;
            return MRUlist.ToArray();
        }

        /// <summary>
        /// Write all entries to the Recent.txt file
        /// </summary>
        internal static void WriteToFile()
        {
            // Create file called "Recent.txt" located on app folder
            var streamWriter = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\\Recent.txt");
            foreach (string item in MRUlist)
            {
                // Write list to stream
                streamWriter.WriteLine(item);
            }
            // Write stream to file
            streamWriter.Flush();
            // Close the stream and reclaim memory
            streamWriter.Close();
        }

        internal static void SetZipped(string zipfile, bool isZipped = true)
        {
            if (!string.IsNullOrWhiteSpace(zipfile))
                Add(zipfile);
            zipped = isZipped;
        }

    }
}
