// Models/OPDBillingModels.cs
using System;
using System.Collections.Generic;

namespace WebApplicationSampleTest2.Models
{
    // ── Bill Header ──────────────────────────────────────────────────────
    public class OPDBill
    {
        public int BillId { get; set; }
        public int AppointmentId { get; set; }
        public int? OPDId { get; set; }
        public int PatientId { get; set; }
        public int HospitalId { get; set; }
        public int? SubHospitalId { get; set; }
        public string BillNumber { get; set; }
        public DateTime BillDate { get; set; }
        public decimal ConsultationFee { get; set; }
        public decimal MedicineCharges { get; set; }
        public decimal ProcedureCharges { get; set; }
        public decimal OtherCharges { get; set; }
        public decimal SubTotal { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DueAmount { get; set; }
        public string PaymentStatus { get; set; }
        public string PaymentMode { get; set; }
        public string TransactionRef { get; set; }
        public string Notes { get; set; }
        public int CreatedBy { get; set; }
    }

    // ── Bill Item ────────────────────────────────────────────────────────
    public class OPDBillItem
    {
        public int ItemId { get; set; }
        public int BillId { get; set; }
        public int AppointmentId { get; set; }
        public string ItemType { get; set; }
        public int? BillingMasterId { get; set; }
        public string ItemName { get; set; }
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string Notes { get; set; }
    }

    // ── Medicine line in billing ─────────────────────────────────────────
    public class OPDBillMedicine
    {
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public string Type { get; set; }
        public bool Morning { get; set; }
        public bool Afternoon { get; set; }
        public bool Evening { get; set; }
        public int? Days { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    // ── Full Bill ViewModel ──────────────────────────────────────────────
    public class OPDBillVM
    {
        // Appointment + Patient info
        public int AppointmentId { get; set; }
        public int? OPDId { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; }
        public int? Age { get; set; }
        public string Gender { get; set; }
        public string PhoneNumber { get; set; }
        public string DoctorName { get; set; }
        public string Specialization { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string AppointmentStatus { get; set; }

        // Medicines from OPD
        public List<OPDBillMedicine> Medicines { get; set; }
            = new List<OPDBillMedicine>();

        // Other/Procedure items added manually
        public List<OPDBillItem> OtherItems { get; set; }
            = new List<OPDBillItem>();

        // Existing bill (if already generated)
        public int? BillId { get; set; }
        public string BillNumber { get; set; }
        public decimal ConsultationFee { get; set; }
        public decimal MedicineCharges { get; set; }
        public decimal ProcedureCharges { get; set; }
        public decimal OtherCharges { get; set; }
        public decimal SubTotal { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DueAmount { get; set; }
        public string PaymentStatus { get; set; }
        public string PaymentMode { get; set; }
    }

    // ── Save Bill Request (from JS fetch) ────────────────────────────────
    public class SaveOPDBillRequest
    {
        public int AppointmentId { get; set; }
        public int? OPDId { get; set; }
        public int PatientId { get; set; }
        public decimal ConsultationFee { get; set; }
        public decimal MedicineCharges { get; set; }
        public decimal ProcedureCharges { get; set; }
        public decimal OtherCharges { get; set; }
        public decimal SubTotal { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public List<OPDBillItem> Items { get; set; }
    }

    // ── Pay Bill Request ─────────────────────────────────────────────────
    public class PayOPDBillRequest
    {
        public int BillId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMode { get; set; }
        public string TransactionRef { get; set; }
    }
}
