namespace NUHS.VeinMapping.Config
{
    public class VeinMappingAppConfig
    {
        /// <summary>
        /// Available type are "mock", "threshold", "contrast" and "ml"
        /// </summary>
        public string VeinSegmentationType = "mock";

        /// <summary>
        /// The hex color to use for the point cloud. Alpha is ignored.
        /// </summary>
        public string PointColor = "#0050FF";

        /// <summary>
        /// IP address of server
        /// </summary>
        public string ServerAddress = "";

        /// <summary>
        /// gRPC communication port of server
        /// </summary>
        public string ServerPort = "52000";
        
        /// <summary>
        /// Convenience property to get full address string
        /// </summary>
        public string ServerFullAddress { get { return $"{ServerAddress}:{ServerPort}"; } }

        /// <summary>
        /// Timeout for server Ping connectivity test
        /// </summary>
        public int ServerTimeoutSeconds = 5;

        /// <summary>
        /// Vein segmentation algorithm, see server documentation
        /// </summary>
        public string ServerVeinMapperType = "0";

        /// <summary>
        /// Image compression algorithm, see server documentation
        /// </summary>
        public string ServerImageCompressionType = "gzip";
    }
}
