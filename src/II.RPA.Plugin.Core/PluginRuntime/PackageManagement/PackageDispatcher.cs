using II.RPA.Plugin.Abstractions;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Core.PluginBase
{
    internal sealed class PackageDispatcher
    {
        private readonly IServiceProvider _services;
        private readonly IReadOnlyDictionary<PluginRoute, AbilityRegistration> _registrations;

        public PackageDispatcher(IServiceProvider services, IReadOnlyDictionary<PluginRoute, AbilityRegistration> registrations)
        {
            _services = services;
            _registrations = registrations;
        }

        public async Task<string> DispatchAsync(PluginExecutionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!_registrations.TryGetValue(context.Route, out var registration)) return Error(eErrorMessageType.Error_PluginGetAbility_NotFoundError);
            if (!registration.TryEnter()) return Error(eErrorMessageType.Error_CannotParallelExcuteError);

            try
            {
                var ability = (IPluginAbility)_services.GetRequiredService(registration.AbilityType);
                return await ability.RunAsync(context).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                return Error(exception is RPAException rpa ? rpa.ErrorMessageType : eErrorMessageType.Error_UNKOWN);
            }
            finally
            {
                registration.Exit();
            }
        }

        private static string Error(eErrorMessageType errorType)
        {
            var exception = new RPAException(errorType);
            return new JMessage
            {
                Code = exception.ErrorCode,
                UnifyCode = exception.UnifyErrorCode,
                Message = exception.ErrorMessage
            }.ToJson();
        }
    }
}
