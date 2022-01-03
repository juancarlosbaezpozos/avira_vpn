using System.IO;
using System.Threading.Tasks;

namespace Avira.VPN.Core
{
    public interface IFile
    {
        string Name { get; }

        string Path { get; }

        Task<bool> Exists();

        Task<Stream> Open(FileAccess fileAccess);

        Task<bool> Delete();

        Task<bool> Create(bool pathIsDirectory);

        string ReadAllText();

        void WriteAllText(string contents);

        void UnzipToDirectory(string outputDirectory);
    }
}