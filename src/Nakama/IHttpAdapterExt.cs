
using System.Collections.Generic;

namespace Nakama
{
    /// <summary>
    /// Extension methods for the <see cref="IHttpAdapter"> interface.
    /// </summary>
    public static class IHttpAdapterExt
    {
        /// <summary>
        /// Performs an in-place copy of keys and values from Nakama's error response dictionary into
        /// the data dictionary of an <see cref="ApiResponseException"/>.
        /// <param name="adapter">The adapter receiving the error response.</param>
        /// <param name="decodedResponse"> The decoded error response from the server.</param>
        /// <param name="e"> The exception whose data dictionary is being written to.</param>
        /// </summary>
        public static void CopyErrorDictionary(this IHttpAdapter adapter, Dictionary<string, object> decodedResponse, ApiResponseException e)
        {
            var errDict = decodedResponse["error"] as Dictionary<string, object>;

            foreach (KeyValuePair<string, object> keyVal in errDict)
            {
                e.Data[keyVal.Key] = keyVal.Value;
            }
        }
    }
}
