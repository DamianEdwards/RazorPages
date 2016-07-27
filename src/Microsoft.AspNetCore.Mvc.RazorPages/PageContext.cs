
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages.Internal;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Microsoft.AspNetCore.Mvc.RazorPages
{
    public class PageContext : ViewContext
    {
        private IList<IValueProviderFactory> _valueProviderFactories;

        public PageContext()
        {
        }

        public PageContext(
            ActionContext actionContext,
            ViewDataDictionary viewData,
            ITempDataDictionary tempDataDictionary,
            HtmlHelperOptions htmlHelperOptions)
            : base(actionContext, NullView.Instance, viewData, tempDataDictionary, TextWriter.Null, htmlHelperOptions)
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
