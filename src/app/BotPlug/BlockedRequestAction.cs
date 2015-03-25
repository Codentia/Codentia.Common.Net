namespace Codentia.Common.Net.BotPlug
{
    /// <summary>
    /// Enumerated type for actions to be taken when a request is received from a blocked source
    /// </summary>
    public enum BlockedRequestAction
    {
        /// <summary>
        /// Terminate the request immediately
        /// </summary>
        Terminate,

        /// <summary>
        /// Redirect the request to a preconfigured Url
        /// </summary>
        Url
    }
}