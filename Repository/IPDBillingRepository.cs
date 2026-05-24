// Repository/IPDBillingRepository.cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Relational;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Xml.Linq;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public class IPDBillingRepository : IIPDBilling
    {
        private readonly string _connectionString;
        private readonly ILogger<IPDBillingRepository> _logger;
        private readonly IIPDNursingCharge _nursingRepo;
        private readonly IIPDOperation _operationRepo;

        public IPDBillingRepository(
            IConfiguration configuration,
            ILogger<IPDBillingRepository> logger,
            IIPDNursingCharge nursingRepo,
            IIPDOperation operationRepo)
        {
            _connectionString = configuration
                .GetConnectionString("MySqlConnection");
            _logger = logger;
            _nursingRepo = nursingRepo;
            _operationRepo = operationRepo;
        }

        public IPDBillVM GetBillSummary(int ipdId)
        {
            var vm = new IPDBillVM
            {
                BedCharges = new List<BedChargeDetail>(),
                DoctorVisits = new List<DoctorVisitDetail>(),
                Medicines = new List<MedicineChargeDetail>(),
                Investigations = new List<InvestigationChargeDetail>(),
                DischargeMedicines = new List<MedicineChargeDetail>(),
                OtherItems = new List<IPDBillItem>(),
                Payments = new List<IPDPayment>(),
                NursingCharges = new List<IPDNursingCharge>(),
                Operations = new List<IPDOperationModel>()
            };

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand(
                    "sp_GetIPDBillSummary", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_IPDId", ipdId);
                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        // RS1 — Bed charges
                        while (reader.Read())
                        {
                            vm.BedCharges.Add(new BedChargeDetail
                            {
                                AllocationId =
                                    Convert.ToInt32(reader["AllocationId"]),
                                BedNumber =
                                    reader["BedNumber"].ToString(),
                                WardName =
                                    reader["WardName"].ToString(),
                                RoomNumber =
                                    reader["RoomNumber"].ToString(),
                                ChargesPerDay =
                                    Convert.ToDecimal(reader["ChargesPerDay"]),
                                StartDateTime =
                                    Convert.ToDateTime(reader["StartDateTime"]),
                                EndDateTime =
                                    Convert.ToDateTime(reader["EndDateTime"]),
                                Days =
                                    Convert.ToInt32(reader["Days"]),
                                BedCharge =
                                    Convert.ToDecimal(reader["BedCharge"])
                            });
                        }

                        // RS2 — Doctor visits
                        reader.NextResult();
                        while (reader.Read())
                        {
                            vm.DoctorVisits.Add(new DoctorVisitDetail
                            {
                                RoundId =
                                    Convert.ToInt32(reader["RoundId"]),
                                RoundDateTime =
                                    Convert.ToDateTime(reader["RoundDateTime"]),
                                RoundType =
                                    reader["RoundType"].ToString(),
                                DoctorName =
                                    reader["DoctorName"].ToString(),
                                VisitCharge = 0
                            });
                        }

                        // RS3 — Medicines
                        reader.NextResult();
                        while (reader.Read())
                        {
                            vm.Medicines.Add(new MedicineChargeDetail
                            {
                                Id =
                                    Convert.ToInt32(reader["Id"]),
                                MedicineName =
                                    reader["MedicineName"].ToString(),
                                Type =
                                    reader["Type"].ToString(),
                                Days =
                                    reader["Days"] == DBNull.Value
                                    ? (int?)null
                                    : Convert.ToInt32(reader["Days"]),
                                Dosage =
                                    reader["Dosage"]?.ToString(),
                                Status =
                                    reader["Status"].ToString(),
                                UnitPrice = 0,
                                TotalPrice = 0
                            });
                        }

                        // RS4 — Investigations
                        reader.NextResult();
                        while (reader.Read())
                        {
                            vm.Investigations.Add(
                                new InvestigationChargeDetail
                                {
                                    Id =
                                        Convert.ToInt32(reader["Id"]),
                                    InvestigationType =
                                        reader["InvestigationType"].ToString(),
                                    TestName =
                                        reader["TestName"].ToString(),
                                    Priority =
                                        reader["Priority"].ToString(),
                                    Status =
                                        reader["Status"].ToString(),
                                    Charge = 0
                                });
                        }

                        // RS5 — Discharge medicines
                        reader.NextResult();
                        while (reader.Read())
                        {
                            vm.DischargeMedicines.Add(
                                new MedicineChargeDetail
                                {
                                    Id =
                                        Convert.ToInt32(reader["Id"]),
                                    MedicineName =
                                        reader["MedicineName"].ToString(),
                                    Type =
                                        reader["Type"].ToString(),
                                    Days =
                                        reader["Days"] == DBNull.Value
                                        ? (int?)null
                                        : Convert.ToInt32(reader["Days"]),
                                    Dosage =
                                        reader["Dosage"]?.ToString(),
                                    UnitPrice = 0,
                                    TotalPrice = 0
                                });
                        }

                        // RS6 — Patient info
                        reader.NextResult();
                        if (reader.Read())
                        {
                            vm.IPDId =
                                Convert.ToInt32(reader["IPDId"]);
                            vm.AdmissionNumber =
                                reader["AdmissionNumber"].ToString();
                            vm.PatientName =
                                reader["PatientName"].ToString();
                            vm.Age =
                                reader["Age"] == DBNull.Value
                                ? (int?)null
                                : Convert.ToInt32(reader["Age"]);
                            vm.Gender =
                                reader["Gender"].ToString();
                            vm.PhoneNumber =
                                reader["PhoneNumber"].ToString();
                            vm.AdmissionDateTime =
                                Convert.ToDateTime(
                                    reader["AdmissionDateTime"]);
                            vm.ActualDischargeDateTime =
                                reader["ActualDischargeDateTime"]
                                    == DBNull.Value
                                ? (DateTime?)null
                                : Convert.ToDateTime(
                                    reader["ActualDischargeDateTime"]);
                            vm.TotalDays =
                                Convert.ToInt32(reader["TotalDays"]);
                        }
                    }
                }

                // Calculate bed total
                vm.TotalBedCharges =
                    vm.BedCharges.Sum(b => b.BedCharge);

                // ── Pull nursing charges ─────────────────────────────────
                vm.NursingCharges = _nursingRepo.GetByIPDId(ipdId);
                vm.TotalNursingCharges =
                    vm.NursingCharges.Sum(n => n.TotalCharge);

                // ── Pull operations with staff ───────────────────────────
                vm.Operations = _operationRepo.GetByIPDId(ipdId);
                foreach (var op in vm.Operations)
                {
                    op.Staff = _operationRepo
                        .GetStaffByOperationId(op.IPDOperationId);
                    op.TotalStaffCharge =
                        op.Staff?.Sum(s => s.Charge) ?? 0;
                }
                vm.TotalOperationCharges = vm.Operations.Sum(op =>
                    op.ActualCharge +
                    op.AnesthesiaCharge +
                    op.SurgeonCharge +
                    op.OTCharge +
                    op.TotalStaffCharge);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error in GetBillSummary. IPDId={IPDId}", ipdId);
                throw new Exception(
                    "Error fetching bill summary.", ex);
            }

            return vm;
        }

        public int SaveBill(IPDBill bill, List<IPDBillItem> items)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var tran = conn.BeginTransaction())
                    {
                        try
                        {
                            string billSql = @"
                                INSERT INTO ipd_bill
                                (IPDId, ParentHospitalId, SubHospitalId,
                                 BillNumber, BedCharges, DoctorVisitCharges,
                                 MedicineCharges, InvestigationCharges,
                                 DischargeMedicineCharges,
                                 NursingCharges, OperationCharges,
                                 OtherCharges, SubTotal,
                                 DiscountPercent, DiscountAmount,
                                 TaxAmount, TotalAmount,
                                 PaidAmount, DueAmount,
                                 PaymentStatus, Notes, CreatedBy)
                                VALUES
                                (@IPDId, @HospitalId, @SubHospitalId,
                                 @BillNumber, @BedCharges, @DoctorCharges,
                                 @MedCharges, @InvCharges,
                                 @DischargeCharges,
                                 @NursingCharges, @OperationCharges,
                                 @OtherCharges, @SubTotal,
                                 @DiscPct, @DiscAmt,
                                 @Tax, @Total,
                                 @Paid, @Due,
                                 @Status, @Notes, @CreatedBy);
                                SELECT LAST_INSERT_ID();";

                            int billId;
                            using (var cmd = new MySqlCommand(
                                billSql, conn, tran))
                            {
                                cmd.Parameters.AddWithValue(
                                    "@IPDId", bill.IPDId);
                                cmd.Parameters.AddWithValue(
                                    "@HospitalId", bill.ParentHospitalId);
                                cmd.Parameters.AddWithValue(
                                    "@SubHospitalId",
                                    bill.SubHospitalId ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue(
                                    "@BillNumber", bill.BillNumber);
                                cmd.Parameters.AddWithValue(
                                    "@BedCharges", bill.BedCharges);
                                cmd.Parameters.AddWithValue(
                                    "@DoctorCharges", bill.DoctorVisitCharges);
                                cmd.Parameters.AddWithValue(
                                    "@MedCharges", bill.MedicineCharges);
                                cmd.Parameters.AddWithValue(
                                    "@InvCharges", bill.InvestigationCharges);
                                cmd.Parameters.AddWithValue(
                                    "@DischargeCharges",
                                    bill.DischargeMedicineCharges);
                                cmd.Parameters.AddWithValue(
                                    "@NursingCharges", bill.NursingCharges);
                                cmd.Parameters.AddWithValue(
                                    "@OperationCharges", bill.OperationCharges);
                                cmd.Parameters.AddWithValue(
                                    "@OtherCharges", bill.OtherCharges);
                                cmd.Parameters.AddWithValue(
                                    "@SubTotal", bill.SubTotal);
                                cmd.Parameters.AddWithValue(
                                    "@DiscPct", bill.DiscountPercent);
                                cmd.Parameters.AddWithValue(
                                    "@DiscAmt", bill.DiscountAmount);
                                cmd.Parameters.AddWithValue(
                                    "@Tax", bill.TaxAmount);
                                cmd.Parameters.AddWithValue(
                                    "@Total", bill.TotalAmount);
                                cmd.Parameters.AddWithValue(
                                    "@Paid", bill.PaidAmount);
                                cmd.Parameters.AddWithValue(
                                    "@Due", bill.DueAmount);
                                cmd.Parameters.AddWithValue(
                                    "@Status", bill.PaymentStatus);
                                cmd.Parameters.AddWithValue(
                                    "@Notes",
                                    bill.Notes ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue(
                                    "@CreatedBy", bill.CreatedBy);

                                billId = Convert.ToInt32(
                                    cmd.ExecuteScalar());
                            }

                            // Insert bill items
                            foreach (var item in items)
                            {
                                string itemSql = @"
                                    INSERT INTO ipd_bill_item
                                    (BillId, IPDId, ItemType,
                                     BillingMasterId, ItemName,
                                     Quantity, UnitPrice,
                                     TotalPrice, Notes)
                                    VALUES
                                    (@BillId, @IPDId, @ItemType,
                                     @MasterId, @Name,
                                     @Qty, @Unit,
                                     @Total, @Notes)";

                                using (var cmd = new MySqlCommand(
                                    itemSql, conn, tran))
                                {
                                    cmd.Parameters.AddWithValue(
                                        "@BillId", billId);
                                    cmd.Parameters.AddWithValue(
                                        "@IPDId", bill.IPDId);
                                    cmd.Parameters.AddWithValue(
                                        "@ItemType", item.ItemType);
                                    cmd.Parameters.AddWithValue(
                                        "@MasterId",
                                        item.BillingMasterId
                                            ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue(
                                        "@Name", item.ItemName);
                                    cmd.Parameters.AddWithValue(
                                        "@Qty", item.Quantity);
                                    cmd.Parameters.AddWithValue(
                                        "@Unit", item.UnitPrice);
                                    cmd.Parameters.AddWithValue(
                                        "@Total", item.TotalPrice);
                                    cmd.Parameters.AddWithValue(
                                        "@Notes",
                                        item.Notes ?? (object)DBNull.Value);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            tran.Commit();
                            return billId;
                        }
                        catch
                        {
                            tran.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error in SaveBill. IPDId={IPDId}", bill.IPDId);
                throw new Exception("Error saving bill.", ex);
            }
        }

        public IPDBill GetBillByIPDId(int ipdId)
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            using var cmd = new MySqlCommand(
                "SELECT * FROM ipd_bill WHERE IPDId = @ipdId LIMIT 1", conn);
            cmd.Parameters.AddWithValue("@ipdId", ipdId);
            using var r = cmd.ExecuteReader();
            if (r.Read()) return MapBill(r);
            return null;
        }

        public void AddPayment(IPDPayment payment)
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var tran = conn.BeginTransaction())
                    {
                        try
                        {
                            // Step 1 — Insert payment
                            using (var cmd = new MySqlCommand(@"
                                INSERT INTO ipd_payment
                                (BillId, IPDId,
                                 ParentHospitalId, SubHospitalId,
                                 Amount, PaymentMode,
                                 TransactionRef, Notes, ReceivedBy)
                                VALUES
                                (@BillId, @IPDId,
                                 @HospId, @SubHospId,
                                 @Amt, @Mode,
                                 @Ref, @Notes, @By)",
                                conn, tran))
                            {
                                cmd.Parameters.AddWithValue(
                                    "@BillId", payment.BillId);
                                cmd.Parameters.AddWithValue(
                                    "@IPDId", payment.IPDId);
                                cmd.Parameters.AddWithValue(
                                    "@HospId", payment.ReceivedBy);
                                cmd.Parameters.AddWithValue(
                                    "@SubHospId", DBNull.Value);
                                cmd.Parameters.AddWithValue(
                                    "@Amt", payment.Amount);
                                cmd.Parameters.AddWithValue(
                                    "@Mode", payment.PaymentMode);
                                cmd.Parameters.AddWithValue(
                                    "@Ref",
                                    payment.TransactionRef
                                        ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue(
                                    "@Notes",
                                    payment.Notes ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue(
                                    "@By", payment.ReceivedBy);
                                cmd.ExecuteNonQuery();
                            }

                            // Step 2 — Update PaidAmount
                            using (var cmd = new MySqlCommand(@"
                                UPDATE ipd_bill
                                SET PaidAmount = PaidAmount + @Amt
                                WHERE BillId = @BillId",
                                conn, tran))
                            {
                                cmd.Parameters.AddWithValue(
                                    "@Amt", payment.Amount);
                                cmd.Parameters.AddWithValue(
                                    "@BillId", payment.BillId);
                                cmd.ExecuteNonQuery();
                            }

                            // Step 3 — Update DueAmount + Status
                            using (var cmd = new MySqlCommand(@"
                                UPDATE ipd_bill
                                SET
                                    DueAmount = TotalAmount - PaidAmount,
                                    PaymentStatus = CASE
                                        WHEN PaidAmount >= TotalAmount
                                            THEN 'Paid'
                                        WHEN PaidAmount > 0
                                            THEN 'Partial'
                                        ELSE 'Unpaid'
                                    END
                                WHERE BillId = @BillId",
                                conn, tran))
                            {
                                cmd.Parameters.AddWithValue(
                                    "@BillId", payment.BillId);
                                cmd.ExecuteNonQuery();
                            }

                            tran.Commit();
                        }
                        catch
                        {
                            tran.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error in AddPayment. BillId={BillId}",
                    payment.BillId);
                throw new Exception("Error adding payment.", ex);
            }
        }

        public List<IPDPayment> GetPaymentsByBillId(int billId)
        {
            var list = new List<IPDPayment>();
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand(@"
                    SELECT * FROM ipd_payment
                    WHERE BillId = @billId
                    ORDER BY PaymentDate DESC", conn))
                {
                    cmd.Parameters.AddWithValue("@billId", billId);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new IPDPayment
                            {
                                PaymentId =
                                    Convert.ToInt32(reader["PaymentId"]),
                                BillId =
                                    Convert.ToInt32(reader["BillId"]),
                                Amount =
                                    Convert.ToDecimal(reader["Amount"]),
                                PaymentMode =
                                    reader["PaymentMode"].ToString(),
                                TransactionRef =
                                    reader["TransactionRef"]?.ToString(),
                                Notes =
                                    reader["Notes"]?.ToString(),
                                PaymentDate =
                                    Convert.ToDateTime(reader["PaymentDate"])
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error in GetPaymentsByBillId. BillId={BillId}",
                    billId);
                throw new Exception("Error fetching payments.", ex);
            }
            return list;
        }

        public void UpdateBillTotals(int billId) { }

        public List<IPDBillItem> GetBillItems(int billId)
        {
            var list = new List<IPDBillItem>();
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand(@"
                    SELECT * FROM ipd_bill_item
                    WHERE BillId   = @billId
                      AND IsActive = 1", conn))
                {
                    cmd.Parameters.AddWithValue("@billId", billId);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new IPDBillItem
                            {
                                ItemId =
                                    Convert.ToInt32(reader["ItemId"]),
                                BillId =
                                    Convert.ToInt32(reader["BillId"]),
                                ItemType =
                                    reader["ItemType"].ToString(),
                                ItemName =
                                    reader["ItemName"].ToString(),
                                Quantity =
                                    Convert.ToInt32(reader["Quantity"]),
                                UnitPrice =
                                    Convert.ToDecimal(reader["UnitPrice"]),
                                TotalPrice =
                                    Convert.ToDecimal(reader["TotalPrice"]),
                                Notes =
                                    reader["Notes"]?.ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error in GetBillItems. BillId={BillId}", billId);
                throw new Exception("Error fetching bill items.", ex);
            }
            return list;
        }

        public List<IPDOperationModel> GetOperationsByIPDId(int ipdId)
        {
            return _operationRepo.GetByIPDId(ipdId);
        }




        public BillingSummaryVM GetPatientBillingSummary(int ipdId)
        {
            var vm = new BillingSummaryVM { IPDId = ipdId };
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    // ── Patient + Admission Info ──────────────────────────────
                    // ── Patient + Admission Info ──────────────────────────────
                    using (var cmd = new MySqlCommand(@"
    SELECT
        a.IPDId,
        a.AdmissionNumber,
        a.AdmissionDateTime,
        a.ActualDischargeDateTime,
        a.Status                AS AdmissionStatus,
        a.ReasonForAdmission,
       GREATEST(1, DATEDIFF(
    IFNULL(a.ActualDischargeDateTime, NOW()),
    a.AdmissionDateTime
) + 1)                  AS TotalDays,
        CONCAT(
            p.FirstName,' ',
            IFNULL(p.LastName,'')
        )                       AS PatientName,
        p.Age,
        p.Gender,
        p.PhoneNumber,
        p.Address,
        CONCAT(
            d.FirstName,' ',
            IFNULL(d.LastName,'')
        )                       AS DoctorName,
        w.WardName,
        b.BedNumber
    FROM ipdadmission a
    INNER JOIN tbl_patient p
            ON a.PatientId       = p.Id
    LEFT  JOIN doctor d
            ON a.PrimaryDoctorId = d.Doctor_Id
    LEFT  JOIN ipdbedallocation ba
            ON a.IPDId           = ba.IPDId
           AND ba.IsCurrent      = 1
    LEFT  JOIN bed b
            ON ba.BedId          = b.BedId
    LEFT  JOIN ward w
            ON b.WardId          = w.WardId
    WHERE a.IPDId = @ipdId
    LIMIT 1", conn))
                    {
                        cmd.Parameters.AddWithValue("@ipdId", ipdId);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                vm.AdmissionNumber =
                                    r["AdmissionNumber"].ToString();
                                vm.AdmissionDateTime =
                                    Convert.ToDateTime(r["AdmissionDateTime"]);
                                vm.DischargeDateTime =
                                    r["ActualDischargeDateTime"] == DBNull.Value
                                    ? (DateTime?)null
                                    : Convert.ToDateTime(
                                        r["ActualDischargeDateTime"]);
                                vm.AdmissionStatus =
                                    r["AdmissionStatus"].ToString();
                                vm.TotalDays =
                                    Convert.ToInt32(r["TotalDays"]);
                                vm.PatientName =
                                    r["PatientName"].ToString();
                                vm.Age =
                                    r["Age"] == DBNull.Value
                                    ? (int?)null
                                    : Convert.ToInt32(r["Age"]);
                                vm.Gender =
                                    r["Gender"]?.ToString();
                                vm.PhoneNumber =
                                    r["PhoneNumber"]?.ToString();
                                vm.Address =
                                    r["Address"]?.ToString();
                                vm.DoctorName =
                                    r["DoctorName"]?.ToString() ?? "-";
                                vm.WardName =
                                    r["WardName"]?.ToString() ?? "-";
                                vm.BedNumber =
                                    r["BedNumber"]?.ToString() ?? "-";
                            }
                        }
                    }


                    // ── Bill Info + Charge Breakdown ──────────────────────────
                    using (var cmd = new MySqlCommand(@"
                SELECT *
                FROM   ipd_bill
                WHERE  IPDId    = @ipdId
                  AND  IsActive = 1
                LIMIT 1", conn))
                    {
                        cmd.Parameters.AddWithValue("@ipdId", ipdId);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                vm.BillId =
                                    Convert.ToInt32(r["BillId"]);
                                vm.BillNumber =
                                    r["BillNumber"].ToString();
                                vm.BillDate =
                                    Convert.ToDateTime(r["BillDate"]);
                                vm.PaymentStatus =
                                    r["PaymentStatus"].ToString();
                                vm.BedCharges =
                                    Convert.ToDecimal(r["BedCharges"]);
                                vm.DoctorCharges =
                                    Convert.ToDecimal(r["DoctorVisitCharges"]);
                                vm.MedicineCharges =
                                    Convert.ToDecimal(r["MedicineCharges"]);
                                vm.InvestigationCharges =
                                    Convert.ToDecimal(r["InvestigationCharges"]);
                                vm.DischargeMedCharges =
                                    Convert.ToDecimal(
                                        r["DischargeMedicineCharges"]);
                                vm.NursingCharges =
                                    Convert.ToDecimal(r["NursingCharges"]);
                                vm.OperationCharges =
                                    Convert.ToDecimal(r["OperationCharges"]);
                                vm.OtherCharges =
                                    Convert.ToDecimal(r["OtherCharges"]);
                                vm.SubTotal =
                                    Convert.ToDecimal(r["SubTotal"]);
                                vm.DiscountAmount =
                                    Convert.ToDecimal(r["DiscountAmount"]);
                                vm.DiscountPercent =
                                    Convert.ToDecimal(r["DiscountPercent"]);
                                vm.TotalAmount =
                                    Convert.ToDecimal(r["TotalAmount"]);
                                vm.PaidAmount =
                                    Convert.ToDecimal(r["PaidAmount"]);
                                vm.DueAmount =
                                    Convert.ToDecimal(r["DueAmount"]);
                            }
                        }
                    }

                    // ── Payment History ───────────────────────────────────────
                    if (vm.BillId.HasValue)
                    {
                        using (var cmd = new MySqlCommand(@"
                    SELECT *
                    FROM   ipd_payment
                    WHERE  BillId = @billId
                    ORDER  BY PaymentDate DESC", conn))
                        {
                            cmd.Parameters.AddWithValue("@billId", vm.BillId.Value);
                            using (var r = cmd.ExecuteReader())
                            {
                                while (r.Read())
                                {
                                    vm.Payments.Add(new IPDPayment
                                    {
                                        PaymentId =
                                            Convert.ToInt32(r["PaymentId"]),
                                        BillId =
                                            Convert.ToInt32(r["BillId"]),
                                        Amount =
                                            Convert.ToDecimal(r["Amount"]),
                                        PaymentMode =
                                            r["PaymentMode"].ToString(),
                                        TransactionRef =
                                            r["TransactionRef"]?.ToString(),
                                        Notes =
                                            r["Notes"]?.ToString(),
                                        PaymentDate =
                                            Convert.ToDateTime(r["PaymentDate"])
                                    });
                                }
                            }
                        }
                    }
                }

                // ── Nursing Detail ────────────────────────────────────────────
                vm.NursingDetails = _nursingRepo.GetByIPDId(ipdId);

                // ── Operation Detail with staff ───────────────────────────────
                vm.OperationDetails = _operationRepo.GetByIPDId(ipdId);
                foreach (var op in vm.OperationDetails)
                {
                    op.Staff =
                        _operationRepo.GetStaffByOperationId(op.IPDOperationId);
                    op.TotalStaffCharge =
                        op.Staff?.Sum(s => s.Charge) ?? 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error in GetPatientBillingSummary. IPDId={IPDId}", ipdId);
                throw new Exception(
                    "Error fetching patient billing summary.", ex);
            }
            return vm;
        }




        public IPDBill GetBillByBillId(int billId)
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            using var cmd = new MySqlCommand(
                "SELECT * FROM ipd_bill WHERE BillId = @billId LIMIT 1", conn);
            cmd.Parameters.AddWithValue("@billId", billId);
            using var r = cmd.ExecuteReader();
            if (r.Read()) return MapBill(r);
            return null;
        }




        private IPDBill MapBill(MySqlDataReader r)
        {
            return new IPDBill
            {
                BillId = Convert.ToInt32(r["BillId"]),
                IPDId = Convert.ToInt32(r["IPDId"]),
                ParentHospitalId = Convert.ToInt32(r["ParentHospitalId"]),
                SubHospitalId = r["SubHospitalId"] == DBNull.Value
                                           ? (int?)null
                                           : Convert.ToInt32(r["SubHospitalId"]),
                BillNumber = r["BillNumber"].ToString(),
                BedCharges = Convert.ToDecimal(r["BedCharges"]),
                DoctorVisitCharges = Convert.ToDecimal(r["DoctorVisitCharges"]),
                MedicineCharges = Convert.ToDecimal(r["MedicineCharges"]),
                InvestigationCharges = Convert.ToDecimal(r["InvestigationCharges"]),
                DischargeMedicineCharges = Convert.ToDecimal(r["DischargeMedicineCharges"]),
                NursingCharges = Convert.ToDecimal(r["NursingCharges"]),
                OperationCharges = Convert.ToDecimal(r["OperationCharges"]),
                OtherCharges = Convert.ToDecimal(r["OtherCharges"]),
                SubTotal = Convert.ToDecimal(r["SubTotal"]),
                DiscountPercent = Convert.ToDecimal(r["DiscountPercent"]),
                DiscountAmount = Convert.ToDecimal(r["DiscountAmount"]),
                TaxAmount = Convert.ToDecimal(r["TaxAmount"]),
                TotalAmount = Convert.ToDecimal(r["TotalAmount"]),
                PaidAmount = Convert.ToDecimal(r["PaidAmount"]),
                DueAmount = Convert.ToDecimal(r["DueAmount"]),
                PaymentStatus = r["PaymentStatus"].ToString(),
            };
        }


    }
}












