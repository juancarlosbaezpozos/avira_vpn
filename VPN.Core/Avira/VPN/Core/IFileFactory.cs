namespace Avira.VPN.Core
{
    public interface IFileFactory
    {
        IFile CreateFile(string path);

        IFile CreateApplicationDataFile(string fileName);
    }
}