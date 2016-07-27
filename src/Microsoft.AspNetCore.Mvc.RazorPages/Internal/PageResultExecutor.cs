using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class PageResultExecutor : ViewExecutor
    {
        private readonly IRazorViewEngine _razorViewEngine;
        private readonly IRazorPageActivator _razorPageActivator;
        private readonly HtmlEncoder _htmlEncoder;

        public PageResultExecutor(
            IOptions<MvcViewOptions> viewOptions,
            IHttpResponseStreamWriterFactory writerFactory,
            ICompositeViewEngine viewEngine,
            ITempDataDictionaryFactory tempDataFactory,
            DiagnosticSource diagnosticSource,
            IModelMetadataProvider modelMetadataProvider,
            IRazorViewEngine razorViewEngine,
            IRazorPageActivator razorPageActivator,
            HtmlEncoder htmlEncoder)
            : base(viewOptions, writerFactory, viewEngine, tempDataFactory, diagnosticSource, modelMetadataProvider)
        {
            _razorViewEngine = razorViewEngine;
            _razorPageActivator = razorPageActivator;
            _htmlEncoder = htmlEncoder;
        }

        public Task ExecuteAsync(ActionContext actionContext, PageViewResult result)
        {
            if (result.Model != null)
            {
                result.Page.PageContext.ViewData.Model = result.Model;
            }

            var view = new RazorView(_razorViewEngine, _razorPageActivator, new IRazorPage[0], result.Page, _htmlEncoder);
            return ExecuteAsync(actionContext, view, result.Page.ViewData, result.Page.TempData, result.ContentType, result.StatusCode);
        } 
    }
}
