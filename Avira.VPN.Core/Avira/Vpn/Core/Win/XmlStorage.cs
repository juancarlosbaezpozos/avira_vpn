using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Schema;
using Microsoft.Win32.SafeHandles;
using Serilog;

namespace Avira.VPN.Core.Win
{
    public class XmlStorage : IStorage
    {
        private static class NativeMethods
        {
            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool FlushFileBuffers(IntPtr handle);
        }

        private static readonly object FileLockObject = new object();

        private readonly string path;

        private readonly string backup;

        private readonly StorageSecurity storageSecurity = new StorageSecurity();

        private XmlDocument document;

        private DateTime documentLastWriteTime;

        internal Func<string, DateTime> FGetLastWriteTime;

        public StorageType StorageType { get; private set; }

        public XmlStorage(string directory, string fileName, StorageType storageType = StorageType.AllUserAccess)
        {
            this.path = Path.Combine(directory, fileName);
            string path = Path.GetFileNameWithoutExtension(fileName) + ".backup";
            backup = Path.Combine(directory, path);
            StorageType = storageType;
            EnsureFileExists(this.path);
        }

        public void Set(string key, string value)
        {
            lock (FileLockObject)
            {
                try
                {
                    XmlDocument xmlDocument = LoadXmlDocument();
                    if (xmlDocument != null)
                    {
                        XmlNode settingsNode = GetSettingsNode(xmlDocument, key);
                        if (IsSettingsNodeValid(settingsNode))
                        {
                            settingsNode.InnerText = value;
                        }
                        else
                        {
                            AddSettingsNode(xmlDocument, key, value);
                        }

                        SaveDocument(xmlDocument, 5, TimeSpan.FromMilliseconds(200.0));
                        StoreLastWriteTime();
                    }
                }
                catch (Exception exception)
                {
                    Log.Warning(exception, "Setting " + key + " failed!");
                }
            }
        }

        public string Get(string key)
        {
            lock (FileLockObject)
            {
                try
                {
                    XmlDocument xmlDocument = LoadXmlDocument();
                    if (xmlDocument == null)
                    {
                        return string.Empty;
                    }

                    XmlNode settingsNode = GetSettingsNode(xmlDocument, key);
                    if (!IsSettingsNodeValid(settingsNode))
                    {
                        return string.Empty;
                    }

                    return settingsNode.InnerText;
                }
                catch (Exception exception)
                {
                    Log.Debug(exception, "Getting " + key + " failed.");
                    return string.Empty;
                }
            }
        }

        private static XmlDocument TryLoadXmlDocument(string filePath, int numRetries, TimeSpan retryTimeout)
        {
            try
            {
                XmlDocument xmlDocument = new XmlDocument();
                for (int i = 1; i <= numRetries; i++)
                {
                    try
                    {
                        xmlDocument.Load(filePath);
                    }
                    catch (FileNotFoundException)
                    {
                        throw;
                    }
                    catch (DirectoryNotFoundException)
                    {
                        throw;
                    }
                    catch (IOException)
                    {
                        if (i == numRetries)
                        {
                            throw;
                        }

                        Thread.Sleep(retryTimeout);
                        continue;
                    }
                    catch (XmlException)
                    {
                        return null;
                    }

                    break;
                }

                return xmlDocument;
            }
            catch
            {
                return null;
            }
        }

        private static XmlNode GetSettingsNode(XmlDocument xmlDocument, string key)
        {
            return xmlDocument.SelectSingleNode($"appSettings/add[@key='{key}']");
        }

        private static bool IsSettingsNodeValid(XmlNode settingsNode)
        {
            if (settingsNode != null)
            {
                return settingsNode.Attributes != null;
            }

            return false;
        }

        private static XmlNode GetRootElement(XmlDocument xmlDocument)
        {
            XmlNode xmlNode = xmlDocument.SelectSingleNode("appSettings");
            if (xmlNode == null)
            {
                throw new Exception($"does not have root element!");
            }

            return xmlNode;
        }

        private static void AddSettingsNode(XmlDocument xmlDocument, string key, string value)
        {
            XmlNode rootElement = GetRootElement(xmlDocument);
            XmlElement xmlElement = xmlDocument.CreateElement("add");
            xmlElement.SetAttribute("key", key);
            xmlElement.InnerText = value;
            rootElement.AppendChild(xmlElement);
        }

        private static string GetIndentString(XmlDocument xmlDocument)
        {
            try
            {
                MemoryStream memoryStream = new MemoryStream();
                XmlWriterSettings settings = new XmlWriterSettings
                {
                    Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                    ConformanceLevel = ConformanceLevel.Document,
                    Indent = true
                };
                XmlWriter w = XmlWriter.Create(memoryStream, settings);
                xmlDocument.Save(w);
                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
            catch
            {
                return xmlDocument.OuterXml;
            }
        }

        private static bool IsWellFormedXml(string xml)
        {
            try
            {
                XmlReaderSettings settings = new XmlReaderSettings
                {
                    ValidationType = ValidationType.None
                };
                using XmlReader xmlReader = XmlReader.Create(new StringReader(xml), settings);
                while (xmlReader.Read())
                {
                }
            }
            catch (XmlException)
            {
                return false;
            }

            return true;
        }

        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands",
            Justification = "This call site is trusted.")]
        private static void FlushToDisc(StreamWriter streamWriter)
        {
            streamWriter.Flush();
            FileStream fileStream = streamWriter.BaseStream as FileStream;
            if (fileStream != null)
            {
                SafeFileHandle safeFileHandle = fileStream.SafeFileHandle;
                if (safeFileHandle != null)
                {
                    NativeMethods.FlushFileBuffers(safeFileHandle.DangerousGetHandle());
                }
            }
        }

        private XmlDocument LoadXmlDocument()
        {
            if (File.Exists(path) && !WriteTimeHasChanged() && document != null)
            {
                return document;
            }

            document = TryLoadXmlDocument(path, 5, TimeSpan.FromMilliseconds(200.0));
            if (document == null && OverwriteVpnSettingsWithTemporaryBackupFile())
            {
                document = TryLoadXmlDocument(path, 5, TimeSpan.FromMilliseconds(200.0));
            }

            if (document == null)
            {
                RebuildStorage();
                return null;
            }

            StoreLastWriteTime();
            return document;
        }

        internal DateTime GetLastWriteTime(string path)
        {
            if (FGetLastWriteTime == null)
            {
                return File.GetLastWriteTime(this.path);
            }

            return FGetLastWriteTime(this.path);
        }

        private void StoreLastWriteTime()
        {
            try
            {
                documentLastWriteTime = GetLastWriteTime(path);
            }
            catch (Exception exception)
            {
                Log.Warning(exception, "StoreLastWriteTime failed");
            }
        }

        private bool WriteTimeHasChanged()
        {
            try
            {
                return GetLastWriteTime(path) != documentLastWriteTime;
            }
            catch (Exception exception)
            {
                Log.Warning(exception, "WriteTimeHasChanged failed.");
                return true;
            }
        }

        private void RebuildStorage()
        {
            Catch.All(delegate
            {
                File.Delete(path);
                EnsureFileExists(path);
            });
        }

        private void EnsureFileExists(string path)
        {
            if (!File.Exists(this.path))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(this.path, "<?xml version=\"1.0\" encoding=\"utf-8\" ?><appSettings></appSettings>");
            }

            if (StorageType == StorageType.Secure)
            {
                storageSecurity.AdjustSecurityForSecureFileContainer(path);
            }
        }

        private bool OverwriteVpnSettingsWithTemporaryBackupFile()
        {
            try
            {
                new XmlDocument().Load(backup);
                File.Copy(backup, path, overwrite: true);
            }
            catch
            {
                return false;
            }

            return true;
        }

        private void SaveDocument(XmlDocument xmlDocument, int numRetries, TimeSpan retryTimeout)
        {
            for (int i = 1; i <= numRetries; i++)
            {
                try
                {
                    string indentString = GetIndentString(xmlDocument);
                    if (IsWellFormedXml(indentString))
                    {
                        EnsureFileExists(backup);
                        using (StreamWriter streamWriter = new StreamWriter(backup, append: false, Encoding.UTF8))
                        {
                            streamWriter.Write(indentString);
                            FlushToDisc(streamWriter);
                        }

                        File.Copy(backup, path, overwrite: true);
                        return;
                    }

                    throw new XmlSchemaValidationException(
                        "Not able to store VpnSettings, it is not conform with the VpnSettingsSchema file.");
                }
                catch (IOException ex)
                {
                    if (i == numRetries || ex is DirectoryNotFoundException)
                    {
                        throw;
                    }

                    Thread.Sleep(retryTimeout);
                }
            }
        }
    }
}