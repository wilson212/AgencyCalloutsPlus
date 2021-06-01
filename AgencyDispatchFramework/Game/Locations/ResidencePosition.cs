namespace AgencyDispatchFramework.Game.Locations
{
    public enum ResidencePosition
    {
        /// <summary>
        /// A ped standing at the front door, facing away from the door,
        /// talking to <see cref="FrontDoorPolicePed1"/> and <see cref="FrontDoorPolicePed2"/>
        /// </summary>
        FrontDoorPed,

        /// <summary>
        /// Talking to <see cref="FrontDoorPed"/>
        /// </summary>
        FrontDoorPolicePed1,

        /// <summary>
        /// Talking to <see cref="FrontDoorPed"/>
        /// </summary>
        FrontDoorPolicePed2,

        /// <summary>
        /// Talking to <see cref="FrontDoorPed"/>, but standing back
        /// </summary>
        FrontDoorPolicePed3,

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
        /// Talking to <see cref="BackDoorPed"/>
        /// </summary>
        BackYardPolicePed1,

        /// <summary>
        /// A ped standing in the side yard. Ped will have clearance
        /// all around to spawn a Ped Group
        /// </summary>
        SideYardPed,

        /// <summary>
        /// A ped standing on the sidewalk in front of the home,
        /// talking to <see cref="SideWalkPolicePed1"/>
        /// </summary>
        SidewalkPed,

        /// <summary>
        /// Talking to <see cref="SidewalkPed"/>
        /// </summary>
        SideWalkPolicePed1,

        /// <summary>
        /// Talking to <see cref="SidewalkPed"/>
        /// </summary>
        SideWalkPolicePed2,

        /// <summary>
        /// A place for a ped to hide
        /// </summary>
        HidingSpot1,

        /// <summary>
        /// A place for a ped to hide
        /// </summary>
        HidingSpot2,

        /// <summary>
        /// A place for police car to park
        /// </summary>
        PoliceParking1,

        /// <summary>
        /// A place for police car to park
        /// </summary>
        PoliceParking2,

        /// <summary>
        /// A place for police car to park
        /// </summary>
        PoliceParking3,

        /// <summary>
        /// A place for police car to park
        /// </summary>
        PoliceParking4,

        /// <summary>
        /// A place for a resident car to park
        /// </summary>
        ResidentParking1,

        /// <summary>
        /// A place for a resident car to park
        /// </summary>
        ResidentParking2,
    }
}
