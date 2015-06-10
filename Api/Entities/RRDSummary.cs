namespace KoenZomers.pfSense.Api.Entities
{
    /// <summary>
    /// Data from the RRD Summary
    /// </summary>
    public class RrdSummary
    {
        /// <summary>
        /// Data use this month
        /// </summary>
        public DataUse DataUseThisMonth { get; set; }

        /// <summary>
        /// Data use last month
        /// </summary>
        public DataUse DataUseLastMonth { get; set; }
    }
}
