﻿using System;
using System.IO;
using System.Threading.Tasks;

namespace Pyke.Utility
{
    /// <summary>
    /// Manages the operations relating to the league client's lockfile.
    /// </summary>
    internal class LockFileHandler
    {
        private const string FileName = "lockfile";

        /// <summary>
        /// Creates a new instance of the <see cref="LockFileHandler"/> class.
        /// </summary>
        public LockFileHandler()
        {
        }

        /// <summary>
        /// Waits for the lockfile to be created and then parses it for the token, port, etc.
        /// </summary>w
        /// <param name="path">The lockfile's path.</param>
        /// <returns>The league client's port and the user's authentication token.</returns>
        public async Task<(int port, string token, int procId)> ParseLockFileAsync(string path)
        {
            try
            {
                var lockfilePath = await WaitForFileAsync(path).ConfigureAwait(false);
                using (var fileStream = new FileStream(lockfilePath, FileMode.Open, FileAccess.Read))
                {
                    using (var reader = new StreamReader(fileStream))
                    {
                        var contents = await reader.ReadToEndAsync();
                        var items = contents.Split(':');

                        var processId = int.Parse(items[1]);
                        var port = int.Parse(items[2]);
                        var token = items[3];

                        return (port, token, processId);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return (0, null, 0);
            }
        }

        /// <summary>
        /// Waits until the lockfile is created by the league client.
        /// </summary>
        /// <param name="path">The directory to search in.</param>
        /// <returns>The lockfile's path.</returns>
        private async Task<string> WaitForFileAsync(string path)
        {
            var filePath = Path.Combine(path, FileName);
            if (File.Exists(filePath))
                return filePath;

            var fileCreated = new TaskCompletionSource<bool>();
            var fileWatcher = new FileSystemWatcher(path);

            void OnFileCreated(object sender, FileSystemEventArgs e)
            {
                if (e.Name == FileName)
                {
                    filePath = e.FullPath;
                    fileWatcher.Dispose();
                    fileCreated.SetResult(true);
                }
            }

            fileWatcher.Created += OnFileCreated;
            fileWatcher.EnableRaisingEvents = true;

            await fileCreated.Task;
            return filePath;
        }
    }
}
