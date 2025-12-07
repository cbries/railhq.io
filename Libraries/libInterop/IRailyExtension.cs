using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace libInterop
{
    [Flags]
    public enum RailyExtensionType
    {
        Controller = 1,
        Feedback = 2
    }

    public interface IRailyExtension
    {
        IRailyHostCallbacks Host { get; }

        Version Version { get; }
        string Name { get; }
        string DisplayName { get; }
        string Copyright { get; }
        string Description { get; }
        RailyExtensionType Type { get; }

        JObject GetState();

        void Initialize(IRailyHostCallbacks host, string cfgContent);
        bool IsCfgChanged(string cfgContent);
        void EnableHandling();
        void InitViews();

        bool IsRunning();

        void TryOpen();

        /// <summary>
        /// Used to stop the plugin business.
        /// Should be used in emergency / critical situtations, e.g. when the network
        /// connection stoped accidentally and locomotives are still cruising.
        /// The extensions are responsible for clever approaches.
        /// </summary>
        void Stop();
        
        Task RunAsync();
        
        Task ShutdownAsync();

        /// <summary>
        /// Creates an instance of `IPayload` when the extensions allows
        /// to receive any command to transfer to hardware devices, e.g. ESU ECoS 50210.
        /// </summary>
        /// <code>
        /// var ecosExt = Services.Get("ecos");
        /// if (ecosExt != null)
        /// {
        ///    var payload = ecosExt.CreatePayload();
        ///    payload.AddCommands(cmds);
        ///    var jsonPayload = JsonConvert.SerializeObject(payload);
        ///    ecosExt.ProvideMessageToExtension(jsonPayload);
        /// }
        /// </code>
        /// <returns>Returns an instance of `IPayload` to send commands to the hardware.</returns>
        IPayload CreatePayload();

        /// <summary>
        /// This method is called by hosts and other calles
        /// to provide message in JSON-dialect to the
        /// individual extension.
        /// </summary>
        /// <see cref="CreatePayload"/>
        /// <param name="jsonMessage"></param>
        void ProvideMessageToExtension(string jsonMessage);
    }
}
