
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.RazorPages
{
    public class PageContext : ActionContext
    {
        private IList<IValueProviderFactory> _valueProviderFactories;

        public PageContext(ActionContext actionContext)
            : base(actionContext)
        {
        }

        public new CompiledPageActionDescriptor ActionDescriptor
        {
            get { return (CompiledPageActionDescriptor)base.ActionDescriptor; }
            set { base.ActionDescriptor = value; }
        }

        public IList<IValueProviderFactory> ValueProviderFactories
        {
            get
            {
                if (_valueProviderFactories == null)
                {
                    _valueProviderFactories = new List<IValueProviderFactory>();
                }

                return _valueProviderFactories;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _valueProviderFactories = value;
            }
        }
    }
}
