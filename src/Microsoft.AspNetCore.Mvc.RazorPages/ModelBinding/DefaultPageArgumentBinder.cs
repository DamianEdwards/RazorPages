using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNetCore.Mvc.RazorPages.ModelBinding
{
    public class DefaultPageArgumentBinder : IPageArgumentBinder
    {
        private readonly IModelMetadataProvider _modelMetadataProvider;
        private readonly IModelBinderFactory _modelBinderFactory;
        private readonly IObjectModelValidator _validator;

        public DefaultPageArgumentBinder(
            IModelMetadataProvider modelMetadataProvider,
            IModelBinderFactory modelBinderFactory,
            IObjectModelValidator validator)
        {
            _modelMetadataProvider = modelMetadataProvider;
            _modelBinderFactory = modelBinderFactory;
            _validator = validator;
        }

        public async Task<object> BindAsync(PageContext pageContext, Type type, string name)
        {
            var factories = pageContext.ValueProviderFactories;
            var valueProviderFactoryContext = new ValueProviderFactoryContext(pageContext);
            for (var i = 0; i < factories.Count; i++)
            {
                var factory = factories[i];
                await factory.CreateValueProviderAsync(valueProviderFactoryContext);
            }

            var valueProvider = new CompositeValueProvider(valueProviderFactoryContext.ValueProviders);

            var metadata = _modelMetadataProvider.GetMetadataForType(type);
            var binder = _modelBinderFactory.CreateBinder(new ModelBinderFactoryContext()
            {
                BindingInfo = null,
                Metadata = metadata,
                CacheToken = null,
            });

            var modelBindingContext = DefaultModelBindingContext.CreateBindingContext(
                pageContext,
                valueProvider,
                metadata,
                null,
                name);

            if (modelBindingContext.ValueProvider.ContainsPrefix(name))
            {
                // We have a match for the parameter name, use that as that prefix.
                modelBindingContext.ModelName = name;
            }
            else
            {
                // No match, fallback to empty string as the prefix.
                modelBindingContext.ModelName = string.Empty;
            }

            await binder.BindModelAsync(modelBindingContext);

            var modelBindingResult = modelBindingContext.Result;
            if (modelBindingResult.IsModelSet)
            {
                _validator.Validate(
                    pageContext,
                    modelBindingContext.ValidationState,
                    modelBindingContext.ModelName,
                    modelBindingResult.Model);

                return modelBindingResult.Model;
            }

            return null;
        }
    }
}
