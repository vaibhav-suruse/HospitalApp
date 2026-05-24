using System.Collections.Generic;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface IOPDBilling
    {
        OPDBillVM GetBillSummary(int appointmentId);
        OPDBill GetBillByAppointmentId(int appointmentId);
        int SaveBill(OPDBill bill, List<OPDBillItem> items);
        void PayBill(int billId, decimal amount, string paymentMode, string transactionRef);
        List<OPDBillItem> GetBillItems(int billId);
    }
}
