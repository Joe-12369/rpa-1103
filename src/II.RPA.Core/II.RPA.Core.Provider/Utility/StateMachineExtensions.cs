using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stateless;
using Stateless.Graph;

namespace II.RPA.Core.Provider.Utility
{
    public static class StateMachineExtensions
    {
        public static string ToGraph<TState, TTrigger>(this StateMachine<TState, TTrigger> stateMachine) => UmlDotGraph.Format(stateMachine.GetInfo());
    }
}
