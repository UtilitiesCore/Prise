using System.Linq;
using System.Threading.Tasks;
using PluginContract;
using PluginServer.Models;
using Prise.Infrastructure;

namespace PluginServer.Services
{
    public interface ICalculationService
    {
        Task<CalculationResponseModel> Calculate(CalculationRequestModel requestModel);
        Task<CalculationResponseModel> CalculateInt(CalculationRequestModel requestModel);
        Task<CalculationResponseModel> CalculateComplex(CalculationRequestModel requestModel);
        Task<CalculationResponseModel> CalculateComplexOutput(CalculationRequestModel requestModel);
        Task<CalculationResponseModel> CalculateMultiple(CalculationRequestMultiModel requestModel);
    }

    public class EagerCalculationService : ICalculationService
    {
        private readonly ICalculationPlugin _plugin;

        public EagerCalculationService(ICalculationPlugin plugin)
        {
            this._plugin = plugin;
        }

        public Task<CalculationResponseModel> Calculate(CalculationRequestModel requestModel)
        {
            // The plugin is eagerly loaded (in-scope)
            return Task.FromResult(new CalculationResponseModel
            {
                Result = _plugin.Calculate(requestModel.A, requestModel.B)
            });
        }

        public Task<CalculationResponseModel> CalculateInt(CalculationRequestModel requestModel)
        {
            // Overloading works due to matching the Proxy on parameter count and types
            return Task.FromResult(new CalculationResponseModel
            {
                Result = _plugin.Calculate((int)requestModel.A, (int)requestModel.B)
            });
        }

        public Task<CalculationResponseModel> CalculateComplex(CalculationRequestModel requestModel)
        {
            // Complex objects are serialized across Application Domains
            var context = new CalculationContext
            {
                A = requestModel.A,
                B = requestModel.B
            };
            return Task.FromResult(new CalculationResponseModel
            {
                Result = _plugin.CalculateComplex(context)
            });
        }

        public Task<CalculationResponseModel> CalculateComplexOutput(CalculationRequestModel requestModel)
        {
            var context = new CalculationContext
            {
                A = requestModel.A,
                B = requestModel.B
            };
            // Complex results are dezerialized using Newtonsoft JSON (by default)
            return Task.FromResult(new CalculationResponseModel
            {
                Result = _plugin.CalculateComplexResult(context).Result
            });
        }

        public Task<CalculationResponseModel> CalculateMultiple(CalculationRequestMultiModel requestModel)
        {
            // Ever more complex objects are serialized correctly
            var calculationContext = new ComplexCalculationContext
            {
                Calculations = requestModel.Calculations.Select(c => new CalculationContext { A = c.A, B = c.B }).ToArray()
            };

            return Task.FromResult(new CalculationResponseModel
            {
                Result = _plugin.CalculateMutiple(calculationContext).Results.Sum(r => r.Result)
            });
        }
    }

    public class LazyCalculationService : ICalculationService
    {
        private readonly IPluginLoader<ICalculationPlugin> loader;

        public LazyCalculationService(IPluginLoader<ICalculationPlugin> loader)
        {
            this.loader = loader;
        }

        public async Task<CalculationResponseModel> Calculate(CalculationRequestModel requestModel)
        {
            var _plugin = await loader.Load();
            // The plugin is eagerly loaded (in-scope)
            return new CalculationResponseModel
            {
                Result = _plugin.Calculate(requestModel.A, requestModel.B)
            };
        }

        public async Task<CalculationResponseModel> CalculateInt(CalculationRequestModel requestModel)
        {
            var _plugin = await loader.Load();
            // Overloading works due to matching the Proxy on parameter count and types
            return new CalculationResponseModel
            {
                Result = _plugin.Calculate((int)requestModel.A, (int)requestModel.B)
            };
        }

        public async Task<CalculationResponseModel> CalculateComplex(CalculationRequestModel requestModel)
        {
            var _plugin = await loader.Load();
            // Complex objects are serialized across Application Domains
            var context = new CalculationContext
            {
                A = requestModel.A,
                B = requestModel.B
            };
            return new CalculationResponseModel
            {
                Result = _plugin.CalculateComplex(context)
            };
        }

        public async Task<CalculationResponseModel> CalculateComplexOutput(CalculationRequestModel requestModel)
        {
            var _plugin = await loader.Load();
            var context = new CalculationContext
            {
                A = requestModel.A,
                B = requestModel.B
            };
            // Complex results are dezerialized using Newtonsoft JSON (by default)
            return new CalculationResponseModel
            {
                Result = _plugin.CalculateComplexResult(context).Result
            };
        }

        public async Task<CalculationResponseModel> CalculateMultiple(CalculationRequestMultiModel requestModel)
        {
            var _plugin = await loader.Load();
            // Ever more complex objects are serialized correctly
            var calculationContext = new ComplexCalculationContext
            {
                Calculations = requestModel.Calculations.Select(c => new CalculationContext { A = c.A, B = c.B }).ToArray()
            };

            return new CalculationResponseModel
            {
                Result = _plugin.CalculateMutiple(calculationContext).Results.Sum(r => r.Result)
            };
        }
    }
}