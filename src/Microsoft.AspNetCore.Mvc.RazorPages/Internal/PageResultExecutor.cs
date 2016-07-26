using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class PageResultExecutor : ViewExecutor
    {
        public PageResultExecutor(
            IOptions<MvcViewOptions> viewOptions,
            IHttpResponseStreamWriterFactory writerFactory,
            ICompositeViewEngine viewEngine,
            ITempDataDictionaryFactory tempDataFactory,
            DiagnosticSource diagnosticSource,
            IModelMetadataProvider modelMetadataProvider)
            : base(viewOptions, writerFactory, viewEngine, tempDataFactory, diagnosticSource, modelMetadataProvider)
        {
        }

        public Task ExecuteAsync(ActionContext actionContext, PageViewResult result)
        {
            if (result.Model != null)
            {
                result.Page.PageContext.ViewData.Model = result.Model;
            }

            return ExecuteAsync(actionContext, result.Page, result.Page.ViewData, result.Page.TempData, result.ContentType, result.StatusCode);
        } 
    }
}
