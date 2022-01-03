using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace Avira.VPN.Core.Win
{
    public class FileWrapper : IFile
    {
        private bool isDirectory;

        public string Name { get; set; }

        public string Path { get; set; }

        public FileWrapper(string path)
        {
            Path = path;
            Name = System.IO.Path.GetFileName(Path);
            Init();
        }

        public Task<bool> Create(bool pathIsDirectory)
        {
            return Task.Run(delegate
            {
                try
                {
                    isDirectory = pathIsDirectory;
                    if (isDirectory)
                    {
                        Directory.CreateDirectory(Path);
                    }
                    else
                    {
                        using (File.Create(Path))
                        {
                        }
                    }

                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            });
        }

        public Task<bool> Delete()
        {
            return Task.Run(delegate
            {
                try
                {
                    if (isDirectory)
                    {
                        Directory.Delete(Path, recursive: true);
                    }
                    else
                    {
                        File.Delete(Path);
                    }

                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            });
        }

        public Task<bool> Exists()
        {
            return Task.Run(delegate
            {
                try
                {
                    return isDirectory ? Directory.Exists(Path) : File.Exists(Path);
                }
                catch (Exception)
                {
                    return false;
                }
            });
        }

        public Task<Stream> Open(FileAccess fileAccess)
        {
            throw new NotImplementedException();
        }

        public string ReadAllText()
        {
            throw new NotImplementedException();
        }

        public void UnzipToDirectory(string outputDirectory)
        {
            ZipFile.ExtractToDirectory(Path, outputDirectory);
        }

        public void WriteAllText(string contents)
        {
            File.WriteAllText(Path, contents);
        }

        private void Init()
        {
            try
            {
                isDirectory = File.GetAttributes(Path).HasFlag(FileAttributes.Directory);
            }
            catch (Exception)
            {
            }
        }
    }
}