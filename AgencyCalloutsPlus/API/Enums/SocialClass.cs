namespace AgencyCalloutsPlus.API
{
    public enum SocialClass
    {
        /// <summary>
        /// Those who occupy poorly-paid positions or rely on government transfers. 
        /// Some high school education.
        /// </summary>
        Lower,

        /// <summary>
        /// Clerical, pink- and blue-collar workers with often low job security; 
        /// common household incomes range from $16,000 to $30,000. High school education.
        /// </summary>
        Working,

        /// <summary>
        /// Semi-professionals and craftsmen with some work autonomy; household incomes 
        /// commonly range from $35,000 to $75,000. Typically, some college education.
        /// </summary>
        LowerMiddle,

        /// <summary>
        /// Highly-educated (often with graduate degrees) professionals & managers with 
        /// household incomes varying from the high 5-figure range to commonly above $100,000.
        /// </summary>
        UpperMiddle,

        /// <summary>
        /// Top-level executives, celebrities, heirs; income of $500,000+ common. 
        /// Ivy league education common.
        /// </summary>
        Upper
    }
}
