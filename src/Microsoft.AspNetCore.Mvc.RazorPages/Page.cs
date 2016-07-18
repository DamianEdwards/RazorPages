using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.RazorPages.ModelBinding;
using Microsoft.AspNetCore.Razor.Runtime.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.RazorPages
{
    public abstract class Page
    {
        private IPageArgumentBinder _binder;

        public PageContext PageContext { get; set; }

        public HttpContext HttpContext => PageContext.HttpContext;

        protected async Task<T> BindAsync<T>(string name)
        {
            if (_binder == null)
            {
                _binder = PageContext.HttpContext.RequestServices.GetRequiredService<IPageArgumentBinder>();
            }

            return (T)(await _binder.BindAsync(PageContext, typeof(T), name) ?? default(T));
        }

        protected IActionResult Redirect(string url)
        {
            return new RedirectResult(url);
        }

        protected IActionResult View()
        {
            return new PageViewResult(this);
        }

        public virtual async Task ExecuteAsync()
        {
            await RenderAsync();
        }

        public virtual Task RenderAsync()
        {
            return TaskCache.CompletedTask;
        }

        protected TTagHelper CreateTagHelper<TTagHelper>() where TTagHelper : ITagHelper, new()
        {
            return new TTagHelper();
        }

        protected void StartTagHelperWritingScope(HtmlEncoder encoder)
        {

        }

        protected TagHelperContent EndTagHelperWritingScope()
        {
            return null;
        }

        protected void BeginWriteTagHelperAttribute()
        {

        }

        protected string EndWriteTagHelperAttribute()
        {
            return null;
        }

        protected virtual void Write(object value)
        {
            if (value == null)
            {
                return;
            }

            var htmlContent = value as IHtmlContent;
            if (htmlContent == null)
            {
                Write(Convert.ToString(value));
                return;
            }

            using (var writer = new StringWriter())
            {
                htmlContent.WriteTo(writer, HtmlEncoder.Default);
                Write(writer.ToString());
                return;
            }
        }

        protected void Write(string value)
        {
            HttpContext.Response.WriteAsync(value).GetAwaiter().GetResult();
        }

        protected virtual void WriteLiteral(object value)
        {

        }

        protected void WriteLiteral(string value)
        {
            HttpContext.Response.WriteAsync(value).GetAwaiter().GetResult();
        }

        protected virtual void BeginWriteAttribute(
            string name,
            string prefix,
            int prefixOffset,
            string suffix,
            int suffixOffset,
            int attributeValuesCount)
        {

        }

        protected void WriteAttributeValue(
            string prefix,
            int prefixOffset,
            object value,
            int valueOffset,
            int valueLength,
            bool isLiteral)
        {

        }

        protected virtual void EndWriteAttribute()
        {

        }

        protected void BeginAddHtmlAttributeValues(
            TagHelperExecutionContext executionContext,
            string attributeName,
            int attributeValuesCount,
            HtmlAttributeValueStyle attributeValueStyle)
        {
        }

        protected void AddHtmlAttributeValue(
            string prefix,
            int prefixOffset,
            object value,
            int valueOffset,
            int valueLength,
            bool isLiteral)
        {
        }

        protected void EndAddHtmlAttributeValues(TagHelperExecutionContext executionContext)
        {

        }

        protected void BeginContext(int position, int length, bool isLiteral)
        {
        }

        protected void EndContext()
        {
        }
    }
}
