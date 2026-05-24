// Repository/IIPDBilling.cs
using System.Collections.Generic;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public interface IIPDBilling
    {
        IPDBillVM GetBillSummary(int ipdId);
        int SaveBill(IPDBill bill, List<IPDBillItem> items);
        IPDBill GetBillByIPDId(int ipdId);
        void AddPayment(IPDPayment payment);
        List<IPDPayment> GetPaymentsByBillId(int billId);
        void UpdateBillTotals(int billId);
        List<IPDBillItem> GetBillItems(int billId);
        BillingSummaryVM GetPatientBillingSummary(int ipdId);
        IPDBill GetBillByBillId(int billId);
    }
}