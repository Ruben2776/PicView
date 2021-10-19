using PicView.FileHandling;
using PicView.UILogic;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PicView.ChangeImage
{
    internal static class History
    {
        static List<string>? fileHistory;
        const short maxCount = 15;

        internal static void InstantiateQ()
        {
            fileHistory = new List<string>();

            var listToRead = new StreamReader(FileFunctions.GetWritingPath() + "\\Recent.txt");

            using (listToRead)
            {
                while (listToRead.Peek() >= 0)
                {
                    fileHistory.Add(listToRead.ReadLine());
                }
            }
        }

        /// <summary>
        /// Write all entries to the Recent.txt file
        /// </summary>
        internal static void WriteToFile()
        {
            if (fileHistory == null) { return; }

            // Create file called "Recent.txt" located on app folder
            var streamWriter = new StreamWriter(FileFunctions.GetWritingPath() + "\\Recent.txt");

            foreach (string item in fileHistory)
            {
                // Write list to stream
                streamWriter.WriteLine(item);
            }

            // Write stream to file
            streamWriter.Flush();
            // Close the stream and reclaim memory
            streamWriter.Close();
        }

        internal static async Task OpenLastFileAsync()
        {
            if (fileHistory is null)
            {
                InstantiateQ();
            }

            if (fileHistory.Count <= 0)
            {
                return;
            }

            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, () =>
            {
                UC.ToggleStartUpUC(true);
            });

            await LoadPic.LoadPiFromFileAsync(fileHistory.Last()).ConfigureAwait(false);
        }

        /// <summary>
        /// Function to add file to MRU
        /// </summary>
        /// <returns></returns>
        internal static void Add(string fileName)
        {
            if (fileHistory == null) { InstantiateQ(); }

            if (fileHistory.Exists(e => e.EndsWith(fileName)))
            {
                return;
            }

            if (fileHistory.Count >= maxCount)
            {
                fileHistory.Remove(fileHistory.Last());
            }

            fileHistory.Add(fileName);
        }

        internal static async Task NextAsync()
        {
            var index = fileHistory.IndexOf(Navigation.Pics[Navigation.FolderIndex]);
            index++;

            if (index >= maxCount)
            {
                return;
            }

            if (fileHistory[index] == Navigation.Pics[Navigation.FolderIndex])
            {
                return;
            }

            await LoadPic.LoadPiFromFileAsync(fileHistory[index]).ConfigureAwait(false);
        }

        internal static async Task PrevAsync()
        {
            var index = fileHistory.IndexOf(Navigation.Pics[Navigation.FolderIndex]);
            index--;

            if (index < 0) { return; }

            if (fileHistory[index] == Navigation.Pics[Navigation.FolderIndex])
            {
                return;
            }

            await LoadPic.LoadPiFromFileAsync(fileHistory[index]).ConfigureAwait(false);
        }
    }
}
