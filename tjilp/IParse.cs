using System.Threading.Tasks;

namespace tjilp
{
    /// <summary>
    ///     Interface to the Parse servers.
    /// </summary>
    internal interface IParse
    {
        /// <summary>
        ///     Tracks an app open
        /// </summary>
        Task TrackAppOpened ();
        /// <summary>
        ///     Saves an object in a class
        /// </summary>
        /// <param name="className">The name of the class to save to.</param>
        /// <param name="obj">The object to save.</param>
        Task SendObject(string className, object obj);
        /// <summary>
        ///     Tracks a custom event
        /// </summary>
        /// <param name="eventName">The name of the event to track</param>
        /// <param name="dimensions">Additional info of the tracked event.</param>
        Task TrackCustomEvent(string eventName, object dimensions);
    }
}