namespace WPS_worder_node_1.Modal.Enums
{
    public enum CheckFrequency
    {
        /// <summary>
        /// One Minute,
        /// </summary>
        OM,

        /// <summary>
        /// Two Minutes,
        /// </summary>
        TWOM,

        /// <summary>
        /// Three Minutes,
        /// </summary>
        THRM,

        /// <summary>
        /// Five Minutes
        /// </summary>
        FIVM,

        /// <summary>
        /// Ten Minutes,
        /// </summary>
        TENM,

        /// <summary>
        /// Fifteen Minutes,
        /// </summary>
        FIFM,

        /// <summary>
        /// Thirty Minutes,
        /// </summary>
        HAFH,

        /// <summary>
        /// One Hour,
        /// </summary>
        OH,

        /// <summary>
        /// two Hour,
        /// </summary>
        TH,

        /// <summary>
        /// Three Hour,
        /// </summary>
        THH,

        /// <summary>
        /// six Hour,
        /// </summary>
        SIXH,

        /// <summary>
        /// nine Hour,
        /// </summary>
        NINH,

        /// <summary>
        /// twvel Hour,
        /// </summary>
        TWH,

        /// <summary>
        /// Eighteen Hour,
        /// </summary>
        EGTH,


        /// <summary>
        /// twenty Four Hour,
        /// </summary>
        TWFH,
    }

    public static class CronInterval
    {
        public static Dictionary<CheckFrequency, string> getCronInterval = new Dictionary<CheckFrequency, string>()
    {
        {CheckFrequency.OM, "* * * * *"},
        {CheckFrequency.THRM, "*/3 * * * *" },
        {CheckFrequency.FIVM, "*/5 * * * *" },
        {CheckFrequency.TENM, "*/10 * * * *" },
        {CheckFrequency.FIFM, "*/15 * * * *" },
        {CheckFrequency.HAFH, "*/30 * * * *" },
        {CheckFrequency.OH, "0 * * * *"   },
        {CheckFrequency.TH, "0 */2 * * *" },
        {CheckFrequency.THH, "0 */3 * * *" },
        {CheckFrequency.SIXH, "0 */6 * * *" },
        {CheckFrequency.NINH, "0 */9 * * *" },
        {CheckFrequency.TWH, "0 */12 * * *" },
        {CheckFrequency.EGTH, "0 */18 * * *" },
        {CheckFrequency.TWFH, "0 */24 * * *" }

    };
}
}
