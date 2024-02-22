using System.IO;
using System.IO.Compression;

namespace NUHS.VeinMapping.VeinProcess
{
    /// <summary>
    /// Compression types supported by the server
    /// </summary>
    public enum ServerImageCompressionType { None, GZip }

    public static class ServerImageCompressionTypeFactory
    {
        /// <summary>
        /// Factory method to get ServerImageCompressionType from string
        /// </summary>
        /// <param name="type"></param>
        /// <returns>ServerImageCompressionType</returns>
        public static ServerImageCompressionType CreateFromString(string type)
        {
            switch (type)
            {
                case "gzip":
                    return ServerImageCompressionType.GZip;
                case "none":
                default:
                    return ServerImageCompressionType.None;
            }
        }
    }

    public static class ServerImageCompressorFactory
    {
        /// <summary>
        /// Factory method to instantiate the implementation based on type
        /// </summary>
        /// <param name="type"></param>
        /// <returns>Instance of IServerImageCompressor</returns>
        public static IServerImageCompressor CreateFromType(ServerImageCompressionType type)
        {
            switch (type)
            {
                case (ServerImageCompressionType.GZip):
                    return new GZipServerImageCompressor();
                case (ServerImageCompressionType.None):
                default:
                    return new NoneServerImageCompressor();
            }
        }
    }

    /// <summary>
    /// Interface that defines an image compressor
    /// </summary>
    public interface IServerImageCompressor
    {
        /// <summary>
        /// Return the string of the type that matches the server implementation
        /// </summary>
        /// <returns></returns>
        string TypeString();
        
        /// <summary>
        /// Compress the given byte array
        /// </summary>
        /// <param name="b">Byte array</param>
        /// <returns>Compressed byte array</returns>
        byte[] Compress(byte[] b);

        /// <summary>
        /// Decompress the given byte array
        /// </summary>
        /// <param name="b">Compressed byte array</param>
        /// <returns>Byte array</returns>
        byte[] Decompress(byte[] b);
    }

    /// <summary>
    /// Implementation with no image compression
    /// </summary>
    public class NoneServerImageCompressor : IServerImageCompressor
    {
        public string TypeString() => "none";
        public byte[] Compress(byte[] b) => b;
        public byte[] Decompress(byte[] b) => b;
    }

    /// <summary>
    /// Implementation with GZip compression
    /// </summary>
    public class GZipServerImageCompressor : IServerImageCompressor
    {
        public string TypeString() => "gzip";

        public byte[] Compress(byte[] b)
        {
            using (var outputStream = new MemoryStream())
            {
                using (var compressor = new GZipStream(outputStream, CompressionMode.Compress))
                {
                    compressor.Write(b, 0, b.Length);                    
                }

                return outputStream.ToArray();
            }
        }

        public byte[] Decompress(byte[] b)
        {
            using (var outputStream = new MemoryStream())
            {
                using (var compressor = new GZipStream(outputStream, CompressionMode.Decompress))
                {
                    compressor.Write(b, 0, b.Length);
                }

                return outputStream.ToArray();
            }
        }
    }
}
