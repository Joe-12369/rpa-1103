using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Provider.Utility
{
    public class StateChanger<TState, TAction> : InterlockedState<TState, TAction>
    {
        private readonly Func<TState, TAction, TState> change;

        public StateChanger(Func<TState, TAction, TState> change) : base() 
        {
             this.change = change;
        }

        protected override TState Change(TState current, TAction action)
        {
            return change(current, action);
        }
    }
}
