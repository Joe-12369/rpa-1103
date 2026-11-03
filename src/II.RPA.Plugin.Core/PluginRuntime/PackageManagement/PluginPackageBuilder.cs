using II.RPA.Plugin.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace II.RPA.Plugin.Core.PluginBase
{
    public sealed class PluginPackageBuilder
    {
        private readonly string _defaultRoutePackageName;
        private readonly HashSet<string> _routePackageNames;
        private readonly Dictionary<Type, AbilityRegistration> _registrationsByType = new Dictionary<Type, AbilityRegistration>();
        private readonly Dictionary<PluginRoute, AbilityRegistration> _registrations = new Dictionary<PluginRoute, AbilityRegistration>();

        public PluginPackageBuilder() : this("Default", new[] { "Default" })
        {
        }

        public PluginPackageBuilder(string defaultRoutePackageName, IEnumerable<string> routePackageNames)
        {
            _defaultRoutePackageName = ValidateName(defaultRoutePackageName, nameof(defaultRoutePackageName));
            if (routePackageNames == null) throw new ArgumentNullException(nameof(routePackageNames));
            _routePackageNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var routePackageName in routePackageNames)
            {
                var name = ValidateName(routePackageName, nameof(routePackageNames));
                if (!_routePackageNames.Add(name)) throw new ArgumentException("A route package name cannot be declared more than once.", nameof(routePackageNames));
            }
            if (_routePackageNames.Count == 0) throw new ArgumentException("At least one route package name is required.", nameof(routePackageNames));
            Services = new ServiceCollection();
        }

        public IServiceCollection Services { get; }

        internal IReadOnlyDictionary<PluginRoute, AbilityRegistration> RouteRegistrations => _registrations;

        public PluginPackageBuilder AddAbility<TAbility>(string abilityName, AbilityLifetime lifetime, AbilityConcurrencyMode concurrencyMode, AbilityActivationMode activationMode = AbilityActivationMode.Lazy)
            where TAbility : class, IPluginAbility
        {
            return AddAbility<TAbility>(_defaultRoutePackageName, abilityName, lifetime, concurrencyMode, activationMode);
        }

        public PluginPackageBuilder AddAbility<TAbility>(string routePackageName, string abilityName, AbilityLifetime lifetime, AbilityConcurrencyMode concurrencyMode, AbilityActivationMode activationMode = AbilityActivationMode.Lazy)
            where TAbility : class, IPluginAbility
        {
            var route = CreateRoute(routePackageName, abilityName);
            if (_registrations.ContainsKey(route)) throw new ArgumentException("Ability route is already registered.", nameof(abilityName));
            ValidateRegistrationPolicy(lifetime, concurrencyMode, activationMode);

            if (_registrationsByType.TryGetValue(typeof(TAbility), out var existingRegistration))
            {
                EnsureSamePolicy(existingRegistration, lifetime, concurrencyMode, activationMode);
                _registrations.Add(route, existingRegistration);
                return this;
            }

            var registration = new AbilityRegistration(typeof(TAbility), lifetime, concurrencyMode, activationMode);
            _registrationsByType.Add(typeof(TAbility), registration);
            _registrations.Add(route, registration);
            if (lifetime == AbilityLifetime.RuntimeSingleton) Services.AddSingleton(typeof(TAbility));
            else Services.AddTransient(typeof(TAbility));
            return this;
        }

        private PluginRoute CreateRoute(string routePackageName, string abilityName)
        {
            var packageName = ValidateName(routePackageName, nameof(routePackageName));
            if (!_routePackageNames.Contains(packageName)) throw new ArgumentException("The route package name is not declared by this package.", nameof(routePackageName));
            return new PluginRoute(packageName, ValidateName(abilityName, nameof(abilityName)));
        }

        private static void ValidateRegistrationPolicy(AbilityLifetime lifetime, AbilityConcurrencyMode concurrencyMode, AbilityActivationMode activationMode)
        {
            if (lifetime == AbilityLifetime.RuntimeSingleton && concurrencyMode == AbilityConcurrencyMode.Parallel) throw new ArgumentException("A singleton ability cannot use Parallel concurrency.", nameof(concurrencyMode));
            if (activationMode == AbilityActivationMode.Eager && lifetime != AbilityLifetime.RuntimeSingleton) throw new ArgumentException("Eager activation requires RuntimeSingleton.", nameof(activationMode));
        }

        private static void EnsureSamePolicy(AbilityRegistration registration, AbilityLifetime lifetime, AbilityConcurrencyMode concurrencyMode, AbilityActivationMode activationMode)
        {
            if (registration.Lifetime != lifetime || registration.ConcurrencyMode != concurrencyMode || registration.ActivationMode != activationMode)
                throw new ArgumentException("All routes for the same ability type must use the same lifetime, concurrency, and activation policy.");
        }

        private static string ValidateName(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value) || value != value.Trim() || value.Contains('.')) throw new ArgumentException("The ability name is invalid.", parameterName);
            return value;
        }
    }
}
