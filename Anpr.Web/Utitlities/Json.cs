using System;
using System.IO;
using System.Reflection;

namespace ANPR.Utitlities
{
    public static class Json
    {
        public static string Load(this string filename, string fileDirectoryPath = null)
        {
            string filePath = GetFilePath(filename, fileDirectoryPath);
            if (filePath != null)
            {
                using (StreamReader r = new StreamReader(filePath))
                {
                    string fileContent = r.ReadToEnd();
                    return fileContent;
                }
            }
            return string.Empty;
        }

        public static string GetFilePath(this string filename, string fileDirectoryPath = null)
        {
            string filePath = string.Empty;
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            string assemblyDirectory = Path.GetDirectoryName(path);
            if (assemblyDirectory != null)
            {
                filePath = string.IsNullOrWhiteSpace(fileDirectoryPath)
                    ? Path.Combine(assemblyDirectory, filename)
                    : Path.Combine(assemblyDirectory, fileDirectoryPath, filename);
            }
            return filePath;
        }


    }
}