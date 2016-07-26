
using System.ComponentModel.DataAnnotations;

namespace RazorPages.Samples.Web
{
    public class Customer
    {
        public int Id { get; set; }

        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; }
    }
}
