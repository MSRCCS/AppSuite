using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Windows.Storage;
using Windows.Storage.Streams;

namespace WindowsApp
{
    public static class GlobalVariable
    {
        public static bool tutorialRun;

        //checks for file to run tutorial or not
    /*    public static GlobalVariable()
        {
            Boolean runTutorial = checkFile();
            tutorialRun = runTutorial;

        }
        public static async Boolean checkFile()
        {
            
            //StorageFile File = await localFolder.GetFileAsync("File.txt");
            String fileTxt = await ReadFile("File.txt");
            if ( fileTxt == null)
            {
                StorageFile newFile = await CreateFile();

                return false;
            }
            else
            {
                return true;
            }
        }
        /*   static GlobalVariable()
          {
              if (..Assets/File.txt.readFile()[1] = 't')
              {

              }
              else
              {

              }
          }
        */
        public static async Task<StorageFile> CreateFile()
        {
            StorageFolder localFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;

            StorageFile File = await localFolder.CreateFileAsync("File.txt", CreationCollisionOption.ReplaceExisting);

            return File;
        }
        public static async Task<String> ReadFile(string filename)
        {
            string contents;
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile textFile = await localFolder.GetFileAsync(filename);

            using (IRandomAccessStream textStream = await textFile.OpenReadAsync())
            {
                using (DataReader textReader = new DataReader(textStream))
                {
                    uint textLength = (uint)textStream.Size;
                    await textReader.LoadAsync(textLength);
                    contents = textReader.ReadString(textLength);
                }
            }
            return contents;

        }
    }


}
