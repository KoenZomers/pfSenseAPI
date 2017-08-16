namespace KoenZomers.pfSense.Api.Entities
{
    public class IfData
    {
        /// <summary>
        /// Name of the interface
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Amount of bytes having gone through
        /// </summary>
        public long Bytes { get; set; }

        /// <summary>
        /// Amount of packets having gone through
        /// </summary>
        public long Packets { get; set; }

        /// <summary>
        /// Direction the data flows in to
        /// </summary>
        public Enums.DataDirections  DataDirection { get; set; }
    }
}
