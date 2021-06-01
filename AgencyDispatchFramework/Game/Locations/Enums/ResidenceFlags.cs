namespace AgencyDispatchFramework.Game.Locations
{
    public enum ResidenceFlags
    {
        /// <summary>
        /// Describes a house that is a single story
        /// </summary>
        SingleStoryHouse,

        /// <summary>
        /// Describes a house that is a two stories
        /// </summary>
        TwoStoryHouse,

        /// <summary>
        /// Describes a residence that is an apartment
        /// </summary>
        Apartment,

        /// <summary>
        /// Describes a residence that is a suite
        /// </summary>
        Suite,

        /// <summary>
        /// Describes a residence that is a condo
        /// </summary>
        Condo,

        /// <summary>
        /// Describes a residence that is a mobile home or trailer
        /// </summary>
        Trailer,

        /// <summary>
        /// Describes a residence that is a mansion
        /// </summary>
        Mansion,

        /// <summary>
        /// Describes a residence with no backdoor and spawn points
        /// </summary>
        NoBackDoor,

        /// <summary>
        /// Indicates that the home has a backyard
        /// </summary>
        HasBackyard,

        /// <summary>
        /// Indicates that the home has a side yard
        /// </summary>
        HasSideYard,

        /// <summary>
        /// Indicates that the home has a front yard
        /// </summary>
        HasFrontYard,

        /// <summary>
        /// Indicates that the home has a pool
        /// </summary>
        HasPool,

        /// <summary>
        /// Indicates that the home has a garage
        /// </summary>
        HasGarage,

        /// <summary>
        /// Indicates that the home has a car port
        /// </summary>
        HasCarPort,

        /// <summary>
        /// Indicates that the residence is in a Cul De Sac
        /// </summary>
        InCulsDSac,

        /// <summary>
        /// Describes a residence that has neighboring homes
        /// </summary>
        HasNeighbors,

        /// <summary>
        /// Describes a residence that has a surounding gate
        /// </summary>
        IsGated,

        /// <summary>
        /// Indicates there is a For Sale sign in front of the home
        /// </summary>
        ForSale,

        /// <summary>
        /// Indicates the home is a potential gang house
        /// </summary>
        GangHouse
    }
}
