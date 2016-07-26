using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPages.Samples.Web.Pages
{
    public class Index : Page
    {
        private static readonly object _lock = new object();
        private static readonly IList<Customer> _customers = new List<Customer>();

        public IEnumerable<Customer> GetCustomers()
        {
            lock (_lock)
            {
                return _customers.ToArray(); 
            }
        }

        public void AddCustomer(Customer customer)
        {
            lock (_lock)
            {
                customer.Id = _customers.Count;
                _customers.Add(customer);
            }
        }
    }
}
