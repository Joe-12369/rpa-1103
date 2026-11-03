using Microsoft.Extensions.DependencyInjection;
using System;

namespace II.RPA.Plugin.GUI.Commons
{
    public sealed class GuiModuleBuilder
    {
        public GuiModuleBuilder(IServiceCollection services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
        }

        public IServiceCollection Services { get; }

        public GuiModuleBuilder AddOperator<TOperator, TSpecialized>()
            where TOperator : class, IGuiOperator, TSpecialized
            where TSpecialized : class
        {
            Services.AddSingleton<TOperator>();
            Services.AddSingleton<IGuiOperator>(provider => provider.GetRequiredService<TOperator>());
            Services.AddSingleton<TSpecialized>(provider => provider.GetRequiredService<TOperator>());
            return this;
        }

        public GuiModuleBuilder AddOperator<TOperator>() where TOperator : class, IGuiOperator
        {
            Services.AddSingleton<TOperator>();
            Services.AddSingleton<IGuiOperator>(provider => provider.GetRequiredService<TOperator>());
            return this;
        }

        public GuiModuleBuilder AddNodeFactory<TFactory>() where TFactory : class, IGuiNodeFactory
        {
            Services.AddSingleton<IGuiNodeFactory, TFactory>();
            return this;
        }

        public GuiModuleBuilder AddNodeFactory<TContract, TFactory>()
            where TContract : class
            where TFactory : class, TContract
        {
            Services.AddSingleton<TContract, TFactory>();
            return this;
        }
    }
}
