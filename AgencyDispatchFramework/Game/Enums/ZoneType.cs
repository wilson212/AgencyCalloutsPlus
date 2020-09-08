using System;

namespace AgencyDispatchFramework.Game
{
    [Flags]
    public enum ZoneType
    {
        Rural = 0,

        Residential = 1,

        Commercial = 2,

        Mixed, // Residential and Commercial mixed

        Industrial = 4,

        Office = 8,

        Highway = 16,

        Recreation = 32,

        Forest = 64,

        Water = 128,

        Government = 256
    }
}
