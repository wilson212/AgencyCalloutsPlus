using AgencyDispatchFramework.Game.Locations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AgencyDispatchFramework.Extensions
{
    /// <summary>
    /// My IEnumerable Extensions, mostly to filter, Group, and Order SoldierWrappers
    /// </summary>
    /// <remarks>Credits to Mitsu Furuta</remarks>
    /// <seealso cref="https://blogs.msdn.microsoft.com/mitsu/2007/12/21/playing-with-linq-grouping-groupbymany/"/>
    public static class IEnumerableExtensions
    {
        public static IEnumerable<GroupResult> GroupByMany<TElement>(
            this IEnumerable<TElement> elements,
            params Func<TElement, object>[] groupSelectors)
        {

            if (groupSelectors.Length > 0)
            {
                var selector = groupSelectors.First();

                //reduce the list recursively until zero
                var nextSelectors = groupSelectors.Skip(1).ToArray();
                return 
                    elements.GroupBy(selector).Select(
                        g => new GroupResult
                        {
                            Key = g.Key,
                            Count = g.Count(),
                            Items = g,
                            SubGroups = g.GroupByMany(nextSelectors)
                        });
            }

            return null;
        }

        /// <summary>
        /// This method is used to filter soldiers using the given SoldierPoolFilter, operator and value
        /// </summary>
        /// <param name="list"></param>
        /// <param name="filter"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        internal static IEnumerable<T> Filter<T>(this IEnumerable<T> list, FlagFilterGroup filters) where T : WorldLocation
        {
            if (filters.Mode == SelectionOperator.All)
            {
                // Must meet every requirement. We do this by applying a filter for each
                // requirement, thus reducing the collection as we go, leaving only the
                // locations that meet every single requirement
                IEnumerable<T> items = list;
                foreach (var filter in filters.Requirements)
                {
                    items = items.Filter(filter);
                }

                return items;
            }
            else
            {
                // Create a hashset, and add all locations that meet any requirement
                HashSet<T> locations = new HashSet<T>();
                foreach (var filter in filters.Requirements)
                {
                    locations.UnionWith(list.Filter(filter));
                }

                return locations.ToArray();
            }
        }

        /// <summary>
        /// This method is used to filter soldiers using the given SoldierPoolFilter, operator and value
        /// </summary>
        /// <param name="list"></param>
        /// <param name="filter"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        internal static IEnumerable<T> Filter<T>(this IEnumerable<T> list, Requirement filter) where T : WorldLocation
        {
            switch (filter.Mode)
            {
                default:
                case SelectionOperator.All:
                    return (filter.Inverse) ? list.Where(x => !x.HasAllFlags(filter.Flags)) : list.Where(x => x.HasAllFlags(filter.Flags));
                case SelectionOperator.Any:
                    return (filter.Inverse) ? list.Where(x => !x.HasAnyFlag(filter.Flags)) : list.Where(x => x.HasAnyFlag(filter.Flags));
            }
        }
    }

    public class GroupResult
    {
        public object Key
        {
            get;
            set;
        }

        public int Count
        {
            get;
            set;
        }

        public IEnumerable Items
        {
            get;
            set;
        }

        public IEnumerable<GroupResult> SubGroups
        {
            get;
            set;
        }

        public override string ToString()
        {
            return string.Format("{0} ({1})", Key, Count);
        }
    }
}