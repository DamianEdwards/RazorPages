using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class CompiledPageActionDescriptor : PageActionDescriptor
    {
        public CompiledPageActionDescriptor(PageActionDescriptor other)
        {
            ActionConstraints = other.ActionConstraints;
            AttributeRouteInfo = other.AttributeRouteInfo;
            BoundProperties = other.BoundProperties;
            DisplayName = other.DisplayName;
            FilterDescriptors = other.FilterDescriptors;
            Parameters = other.Parameters;
            Properties = other.Properties;
            RelativePath = other.RelativePath;
            RouteValues = other.RouteValues;
            ViewEnginePath = other.ViewEnginePath;
        }

        public TypeInfo PageType { get; set; }
    }
}
