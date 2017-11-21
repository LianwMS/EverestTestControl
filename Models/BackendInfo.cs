namespace EverestTest
{
    /// <summary>
    /// Everest backend info, including worker and DB version.
    /// </summary>
    internal class BackendInfo
    {
        /// <summary>
        /// Version of DTS bits on worker node, e.g: 14.0.900.5478.3272278
        /// </summary>
        public string WorkerVersion { get; set; }

        /// <summary>
        /// Version of SSISDB, e.g: 6_0
        /// </summary>
        public string DBVersion { get; set; }

        /// <summary>
        /// True if is on Prod. 
        /// </summary>
        public bool CurrentOnProd { get; set; }

        /// <summary>
        /// Some Description
        /// </summary>
        public string Description { get; set; }
    }
}
