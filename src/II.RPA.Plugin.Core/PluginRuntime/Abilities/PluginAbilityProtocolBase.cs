using II.RPA.Plugin.Abstractions;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Core.PluginBase
{
    public abstract class PluginAbilityProtocolBase<TParam> : IPluginAbility
        where TParam : PluginParameterBase
    {
        protected string ComponentLabel { get; private set; }

        protected abstract eComponentType ComponentType { get; }

        public virtual async Task<string> RunAsync(PluginExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            TParam parameter = null;
            JMessage resultMessage = new JMessage();

            try
            {
                try
                {
                    parameter = DeserializeParameter(context.JsonParam);
                    if (parameter == null)
                    {
                        throw new JsonSerializationException("The plugin parameter cannot be null.");
                    }
                }
                catch (Exception exception)
                {
                    Exception(exception, eErrorMessageType.Error_ParamsError, ref resultMessage);
                    return HandlingResultMessage(parameter, resultMessage);
                }

                ComponentLabel = parameter.ComponentLabel;

                try
                {
                    ValidateParameter(parameter);
                    NormalizeParameter(parameter);
                    var preparationResult = await PrepareExecutionAsync(parameter, context).ConfigureAwait(false);
                    resultMessage = preparationResult ?? await ExecuteCoreAsync(parameter, context).ConfigureAwait(false);
                }
                catch (RPAException exception)
                {
                    Exception(exception, eErrorMessageType.Error_UNKOWN, ref resultMessage);
                }
                catch (Exception exception)
                {
                    Exception(exception, eErrorMessageType.Error_UNKOWN, ref resultMessage);
                }

                return HandlingResultMessage(parameter, resultMessage);
            }
            finally
            {
                ComponentLabel = null;
            }
        }

        protected virtual TParam DeserializeParameter(string jsonParam)
        {
            return JsonConvert.DeserializeObject<TParam>(jsonParam);
        }

        protected abstract void ValidateParameter(TParam parameter);

        protected virtual void NormalizeParameter(TParam parameter)
        {
        }

        protected virtual Task<JMessage> PrepareExecutionAsync(TParam parameter, PluginExecutionContext context)
        {
            return Task.FromResult<JMessage>(null);
        }

        protected abstract Task<JMessage> ExecuteCoreAsync(TParam parameter, PluginExecutionContext context);

        protected virtual string HandlingResultMessage(TParam parameter, JMessage resultMessage)
        {
            return HandlingErrorMessage(resultMessage);
        }

        protected virtual string HandlingErrorMessage(JMessage resultMessage)
        {
            if (resultMessage.Code != 0 && resultMessage.UnifyCode == "0")
            {
                resultMessage.UnifyCode = resultMessage.Code.ToString();
            }

            return resultMessage.ToJson();
        }

        protected void Exception(RPAException exception, ref JMessage message)
        {
            Exception(exception, exception.ErrorMessageType, ref message);
        }

        protected void Exception(eErrorMessageType errorMessageType, ref JMessage message)
        {
            Exception(null, errorMessageType, ref message);
        }

        protected void Exception(Exception exception, eErrorMessageType errorMessageType, ref JMessage message)
        {
            Exception(exception, eGuiType.None, errorMessageType, ref message);
        }

        protected virtual void Exception(Exception exception, eGuiType guiType, eErrorMessageType errorMessageType, ref JMessage message)
        {
            Exception(exception, guiType, errorMessageType, false, ref message);
        }

        protected virtual void Exception(Exception exception, eGuiType guiType, eErrorMessageType errorMessageType, bool simpleMessage, ref JMessage message)
        {
            RPAException rpaException;
            if (exception is RPAException existingException)
            {
                rpaException = existingException;
                rpaException.ComponentType = ComponentType;
            }
            else
            {
                rpaException = new RPAException(errorMessageType, ComponentType, exception);
            }

            if (!string.IsNullOrEmpty(ComponentLabel))
            {
                rpaException.ComponentName = ComponentLabel;
            }

            message.Code = rpaException.ErrorCode;
            message.UnifyCode = rpaException.UnifyErrorCode;
            message.Message = simpleMessage ? rpaException.ErrorCodeSuffixMessage : rpaException.ErrorMessage;

            JObject extension = string.IsNullOrEmpty(message.Extend)
                ? new JObject()
                : JObject.Parse(message.Extend);
            extension["ExtErrorCode"] = message.UnifyCode;
            message.Extend = extension.ToString();
        }
    }
}
