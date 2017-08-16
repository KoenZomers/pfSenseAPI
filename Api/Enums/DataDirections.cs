namespace KoenZomers.pfSense.Api.Enums
{
    /// <summary>
    /// Defines the directions data can flow into
    /// </summary>
    public enum DataDirections : short
    {
        /// <summary>
        /// Data flowing towards pfSense
        /// </summary>
        Inbound = 0,

        /// <summary>
        /// Data flowing out of pfSense
        /// </summary>
        Outbound = 1
    }
}
