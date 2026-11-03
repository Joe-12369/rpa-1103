using II.RPA.Plugin.Core.Entities;
using Newtonsoft.Json;
using System;

namespace II.RPA.Plugin.GUI.Deeps.Actions
{
    public interface IActionHandler
    {
        string Execute(object input);
    }

    public class UiaMessageHandler : IActionHandler
    {
        private readonly Func<string, string> _func;
        public UiaMessageHandler(Func<string, string> func) => _func = func;
        public string Execute(object input) 
        {
            if (input is string json)
            {
                return _func(json);
            }
            throw new ArgumentException($"input should be string!");
        }
    }

    //public class PeerHandler : IActionHandler
    //{
    //    private readonly Func<Peer, string> _func;
    //    public PeerHandler(Func<Peer, string> func) => _func = func;
    //    public string Execute(object input) => _func((Peer)input);
    //}
}
