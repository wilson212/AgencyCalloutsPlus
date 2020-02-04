namespace AgencyCalloutsPlus.API
{
    public enum HomeSpawn
    {
        /// <summary>
        /// A ped standing at the front door, facing away from the door,
        /// talking to <see cref="PolicePed1"/> and <see cref="PolicePed2"/>
        /// </summary>
        FrontDoorPed,

        /// <summary>
        /// Talking to <see cref="FrontDoorPed"/>
        /// </summary>
        PolicePed1,

        /// <summary>
        /// Talking to <see cref="FrontDoorPed"/>
        /// </summary>
        PolicePed2,

        /// <summary>
        /// A ped standing in the front yard. Ped will have clearance
        /// all around to spawn a Ped Group
        /// </summary>
        FrontYardPed,

        /// <summary>
        /// A ped standing at the backdoor facing away from the door
        /// </summary>
        BackDoorPed,

        /// <summary>
        /// A ped standing in the back yard. Ped will have clearance
        /// all around to spawn a Ped Group
        /// </summary>
        BackYardPed,

        /// <summary>
        /// A ped standing in the side yard. Ped will have clearance
        /// all around to spawn a Ped Group
        /// </summary>
        SideYardPed,

        /// <summary>
        /// A ped standing on the sidewalk in front of the home,
        /// talking to <see cref="PolicePed3"/>
        /// </summary>
        SidewalkPed,

        /// <summary>
        /// Talking to <see cref="SidewalkPed"/>
        /// </summary>
        PolicePed3,

        /// <summary>
        /// A place for a ped to hide
        /// </summary>
        HidingSpot1,

        /// <summary>
        /// A place for a ped to hide
        /// </summary>
        HidingSpot2,

        PoliceParking1,
        PoliceParking2,
        PoliceParking3,

        ResidentParking1,
        ResidentParking2,
    }
}
