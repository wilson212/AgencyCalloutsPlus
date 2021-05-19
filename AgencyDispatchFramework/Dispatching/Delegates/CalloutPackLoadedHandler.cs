using System.Reflection;

namespace AgencyDispatchFramework.Dispatching
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="path">The full string path to the callout pack</param>
    /// <param name="assembly">The assembly containing the Callout scripts</param>
    /// <param name="itemCount">The total number of callout scenarios added</param>
    public delegate void CalloutPackLoadedHandler(string path, Assembly assembly, int itemCount);
}
