using Rage;

namespace AgencyCalloutsPlus.Extensions
{
    /// <summary>
    /// Credits to Albo1125 for most of these methods
    /// </summary>
    public static class EntityExtensions
    {
        public static float CalculateHeadingTowardsEntity(this Entity ent, Entity target)
        {
            Vector3 directionToTargetEnt = (target.Position - ent.Position);
            directionToTargetEnt.Normalize();
            return MathHelper.ConvertDirectionToHeading(directionToTargetEnt);
        }
    }
}
