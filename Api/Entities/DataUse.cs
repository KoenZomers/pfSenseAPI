namespace KoenZomers.pfSense.Api.Entities
{
    /// <summary>
    /// Defines data usage
    /// </summary>
    public class DataUse
    {
        /// <summary>
        /// Total amount of data coming in to pfSense in MB
        /// </summary>
        public int? In { get; set; }

        /// <summary>
        /// Total amount of data going out of pfSense in MB
        /// </summary>
        public int? Out { get; set; }

        /// <summary>
        /// Total amount of data coming in to and going out of pfSense in MB
        /// </summary>
        public int? Total { get; set; }
    }
}
