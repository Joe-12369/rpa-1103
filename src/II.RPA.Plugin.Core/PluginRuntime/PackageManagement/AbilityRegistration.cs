using II.RPA.Plugin.Abstractions;
using System;
using System.Threading;

namespace II.RPA.Plugin.Core.PluginBase
{
    public sealed class AbilityRegistration
    {
        private int _busy;

        internal AbilityRegistration(Type abilityType, AbilityLifetime lifetime, AbilityConcurrencyMode concurrencyMode, AbilityActivationMode activationMode)
        {
            AbilityType = abilityType ?? throw new ArgumentNullException(nameof(abilityType));
            Lifetime = lifetime;
            ConcurrencyMode = concurrencyMode;
            ActivationMode = activationMode;
        }

        public Type AbilityType { get; }
        public AbilityLifetime Lifetime { get; }
        public AbilityConcurrencyMode ConcurrencyMode { get; }
        public AbilityActivationMode ActivationMode { get; }

        internal bool TryEnter() => ConcurrencyMode == AbilityConcurrencyMode.Parallel || Interlocked.CompareExchange(ref _busy, 1, 0) == 0;
        internal void Exit() { if (ConcurrencyMode == AbilityConcurrencyMode.RejectWhenBusy) Volatile.Write(ref _busy, 0); }
    }
}
