using System;
using SmartFileMan.Contracts.Core;

namespace SmartFileMan.Contracts.Models
{
    public class BiddingResult
    {
        public string PluginName { get; set; }
        public string PluginType { get; set; }
        public RouteProposal? Proposal { get; set; }
        public TimeSpan Duration { get; set; }
        public string? ErrorMessage { get; set; }
        public bool IsWinner { get; set; }
    }
}
