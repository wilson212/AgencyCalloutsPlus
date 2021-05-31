using System;
using System.IO;
using System.Xml;

namespace AgencyDispatchFramework.Xml
{
    /// <summary>
    /// A base class that represents a disposable XML file that is specifically 
    /// forattmed for a purpose within this mod
    /// </summary>
    public abstract class XmlFileBase : IDisposable
    {
        /// <summary>
        /// Gets the string file path to the XML file
        /// </summary>
        public string FilePath { get; private set; }

        /// <summary>
        /// Gets the <see cref="XmlDocument"/> that is to be parsed
        /// </summary>
        internal XmlDocument Document { get; private set; }

        /// <summary>
        /// Indicates whether or not this instance is disposed
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Creates a new instance of <see cref="XmlFileBase"/>. This constuctor is responsible
        /// for loading the XML document
        /// </summary>
        /// <param name="filePath">The full file path the XML file</param>
        public XmlFileBase(string filePath)
        {
            // Store
            FilePath = filePath;

            // Load XML document
            Document = new XmlDocument();
            using (var file = new FileStream(filePath, FileMode.Open))
            {
                Document.Load(file);
            }
        }

        /// <summary>
        /// Disposes this instance and clears the internal document
        /// </summary>
        public virtual void Dispose()
        {
            if (!IsDisposed)
            {
                IsDisposed = true;
                Document = null;
            }
        }
    }
}
