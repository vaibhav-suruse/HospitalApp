// Models/IPDBillingVM.cs
using System;
using System.Collections.Generic;

namespace WebApplicationSampleTest2.Models
{
    // Main bill header
    public class IPDBill
    {
        public int BillId { get; set; }
        public int IPDId { get; set; }
        public int ParentHospitalId { get; set; }
        public int? SubHospitalId { get; set; }
        public string BillNumber { get; set; }
        public DateTime BillDate { get; set; }

        public decimal BedCharges { get; set; }
        public decimal DoctorVisitCharges { get; set; }
        public decimal MedicineCharges { get; set; }
        public decimal InvestigationCharges { get; set; }
        public decimal DischargeMedicineCharges { get; set; }
        public decimal OtherCharges { get; set; }

        public decimal SubTotal { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DueAmount { get; set; }
        public string PaymentStatus { get; set; }
        public string Notes { get; set; }
        public int CreatedBy { get; set; }
        // ADD these to IPDBill class
        public decimal NursingCharges { get; set; }
        public decimal OperationCharges { get; set; }
    }

    // Bill line item
    public class IPDBillItem
    {
        public int ItemId { get; set; }
        public int BillId { get; set; }
        public int IPDId { get; set; }
        public string ItemType { get; set; }
        public int? BillingMasterId { get; set; }
        public string ItemName { get; set; }
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string Notes { get; set; }
    }

    // Payment
    public class IPDPayment
    {
        public int PaymentId { get; set; }
        public int BillId { get; set; }
        public int IPDId { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMode { get; set; }
        public string TransactionRef { get; set; }
        public string Notes { get; set; }
        public int ReceivedBy { get; set; }
    }

    // Bed charge breakdown
    public class BedChargeDetail
    {
        public int AllocationId { get; set; }
        public string BedNumber { get; set; }
        public string WardName { get; set; }
        public string RoomNumber { get; set; }
        public decimal ChargesPerDay { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public int Days { get; set; }
        public decimal BedCharge { get; set; }
    }

    // Doctor visit
    public class DoctorVisitDetail
    {
        public int RoundId { get; set; }
        public DateTime RoundDateTime { get; set; }
        public string RoundType { get; set; }
        public string DoctorName { get; set; }
        public decimal VisitCharge { get; set; }
    }

    // Medicine charge
    public class MedicineChargeDetail
    {
        public int Id { get; set; }
        public string MedicineName { get; set; }
        public string Type { get; set; }
        public int? Days { get; set; }
        public string Dosage { get; set; }
        public string Status { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    // Investigation charge
    public class InvestigationChargeDetail
    {
        public int Id { get; set; }
        public string InvestigationType { get; set; }
        public string TestName { get; set; }
        public string Priority { get; set; }
        public string Status { get; set; }
        public decimal Charge { get; set; }
    }

    // Full bill VM for view
    public class IPDBillVM
    {
        // Patient info
        public int IPDId { get; set; }
        public string AdmissionNumber { get; set; }
        public string PatientName { get; set; }
        public int? Age { get; set; }
        public string Gender { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime AdmissionDateTime { get; set; }
        public DateTime? ActualDischargeDateTime { get; set; }
        public int TotalDays { get; set; }

        // Charge breakdowns
        public List<BedChargeDetail> BedCharges { get; set; }
        public List<DoctorVisitDetail> DoctorVisits { get; set; }
        public List<MedicineChargeDetail> Medicines { get; set; }
        public List<InvestigationChargeDetail> Investigations { get; set; }
        public List<MedicineChargeDetail> DischargeMedicines { get; set; }
        public List<IPDBillItem> OtherItems { get; set; }

        // Totals
        public decimal TotalBedCharges { get; set; }
        public decimal TotalDoctorCharges { get; set; }
        public decimal TotalMedicineCharges { get; set; }
        public decimal TotalInvestigationCharges { get; set; }
        public decimal TotalDischargeMedCharges { get; set; }
        public decimal TotalOtherCharges { get; set; }
        public decimal SubTotal { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DueAmount { get; set; }
        public string PaymentStatus { get; set; }

        // Existing bill id if already generated
        public int? BillId { get; set; }

        // Payment history
        public List<IPDPayment> Payments { get; set; }




        

        // ADD these to IPDBillVM class
        public List<IPDNursingCharge> NursingCharges { get; set; }
            = new List<IPDNursingCharge>();
        public List<IPDOperationModel> Operations { get; set; }
            = new List<IPDOperationModel>();
        public decimal TotalNursingCharges { get; set; }
        public decimal TotalOperationCharges { get; set; }
        public string BillNumber { get; set; }
    }
}