using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplicationSampleTest2.Models
{
    public class Patient
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Gender { get; set; }

        public string Age { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public bool isUpdate { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string pulse { get; set; }

        public string BP { get; set; }

        public List<tablet> Medicineslist { get; set; }

        public string Status { get; set; }
        public string Investigation { get; set; }

        public Dictionary<string, string> BillingDetails { get; set; }

        public string TotalBill { get; set; }

        public string ReportDetail { get; set; }
        public List<string> symptoms { get; set; }

        public List<string> symptomsmain { get; set; }

        public int Hospital_Id { get; set; }
        public int? SubHospital_Id { get; set; }

        public int? AccountId { get; set; }
        public string Relation { get; set; }
    }
}
