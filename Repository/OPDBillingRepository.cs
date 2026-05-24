using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public class OPDBillingRepository : IOPDBilling
    {
        private readonly string _connectionString;
        private readonly ILogger<OPDBillingRepository> _logger;

        public OPDBillingRepository(
            IConfiguration configuration,
            ILogger<OPDBillingRepository> logger)
        {
            _connectionString = configuration.GetConnectionString("MySqlConnection");
            _logger = logger;
        }

        public OPDBillVM GetBillSummary(int appointmentId)
        {
            var vm = new OPDBillVM { AppointmentId = appointmentId };
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_GetOPDBillSummary", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_AppointmentId", appointmentId);
                conn.Open();

                using var reader = cmd.ExecuteReader();

                // RS1 — Patient + Appointment info
                if (reader.Read())
                {
                    vm.AppointmentId = Convert.ToInt32(reader["AppointmentId"]);
                    vm.PatientId = Convert.ToInt32(reader["PatientId"]);
                    vm.PatientName = reader["PatientName"].ToString();
                    vm.Age = reader["Age"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["Age"]);
                    vm.Gender = reader["Gender"]?.ToString();
                    vm.PhoneNumber = reader["PhoneNumber"]?.ToString();
                    vm.DoctorName = reader["DoctorName"].ToString();
                    vm.Specialization = reader["Specialization"]?.ToString();
                    vm.AppointmentDate = Convert.ToDateTime(reader["AppointmentDate"]);
                    vm.AppointmentStatus = reader["Status"]?.ToString();
                    vm.OPDId = reader["OPDId"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["OPDId"]);
                }

                // RS2 — Medicines
                reader.NextResult();
                while (reader.Read())
                {
                    vm.Medicines.Add(new OPDBillMedicine
                    {
                        MedicineId = Convert.ToInt32(reader["MedicineId"]),
                        MedicineName = reader["MedicineName"].ToString(),
                        Type = reader["Type"]?.ToString(),
                        Morning = Convert.ToBoolean(reader["Morning"]),
                        Afternoon = Convert.ToBoolean(reader["Afternoon"]),
                        Evening = Convert.ToBoolean(reader["Evening"]),
                        Days = reader["Days"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["Days"]),
                        UnitPrice = 0,
                        TotalPrice = 0
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetBillSummary. AppointmentId={Id}", appointmentId);
                throw;
            }
            return vm;
        }

        public OPDBill GetBillByAppointmentId(int appointmentId)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_GetOPDBillByAppointment", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_AppointmentId", appointmentId);
                conn.Open();
                using var r = cmd.ExecuteReader();
                if (r.Read()) return MapBill(r);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetBillByAppointmentId. AppointmentId={Id}", appointmentId);
                throw;
            }
        }

        public int SaveBill(OPDBill bill, List<OPDBillItem> items)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();
                using var tran = conn.BeginTransaction();
                try
                {
                    // Save bill header
                    using var cmd = new MySqlCommand("sp_SaveOPDBill", conn, tran);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_AppointmentId", bill.AppointmentId);
                    cmd.Parameters.AddWithValue("p_OPDId", bill.OPDId.HasValue ? (object)bill.OPDId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("p_PatientId", bill.PatientId);
                    cmd.Parameters.AddWithValue("p_HospitalId", bill.HospitalId);
                    cmd.Parameters.AddWithValue("p_SubHospitalId", bill.SubHospitalId.HasValue ? (object)bill.SubHospitalId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("p_BillNumber", bill.BillNumber);
                    cmd.Parameters.AddWithValue("p_ConsultationFee", bill.ConsultationFee);
                    cmd.Parameters.AddWithValue("p_MedicineCharges", bill.MedicineCharges);
                    cmd.Parameters.AddWithValue("p_ProcedureCharges", bill.ProcedureCharges);
                    cmd.Parameters.AddWithValue("p_OtherCharges", bill.OtherCharges);
                    cmd.Parameters.AddWithValue("p_SubTotal", bill.SubTotal);
                    cmd.Parameters.AddWithValue("p_DiscountPercent", bill.DiscountPercent);
                    cmd.Parameters.AddWithValue("p_DiscountAmount", bill.DiscountAmount);
                    cmd.Parameters.AddWithValue("p_TotalAmount", bill.TotalAmount);
                    cmd.Parameters.AddWithValue("p_CreatedBy", bill.CreatedBy);

                    int billId = Convert.ToInt32(cmd.ExecuteScalar());

                    // Save items
                    foreach (var item in items)
                    {
                        using var icmd = new MySqlCommand("sp_SaveOPDBillItem", conn, tran);
                        icmd.CommandType = CommandType.StoredProcedure;
                        icmd.Parameters.AddWithValue("p_BillId", billId);
                        icmd.Parameters.AddWithValue("p_AppointmentId", bill.AppointmentId);
                        icmd.Parameters.AddWithValue("p_ItemType", item.ItemType);
                        icmd.Parameters.AddWithValue("p_BillingMasterId", item.BillingMasterId.HasValue ? (object)item.BillingMasterId.Value : DBNull.Value);
                        icmd.Parameters.AddWithValue("p_ItemName", item.ItemName);
                        icmd.Parameters.AddWithValue("p_Quantity", item.Quantity);
                        icmd.Parameters.AddWithValue("p_UnitPrice", item.UnitPrice);
                        icmd.Parameters.AddWithValue("p_TotalPrice", item.TotalPrice);
                        icmd.ExecuteNonQuery();
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SaveBill. AppointmentId={Id}", bill.AppointmentId);
                throw;
            }
        }

        public void PayBill(int billId, decimal amount, string paymentMode, string transactionRef)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_PayOPDBill", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_BillId", billId);
                cmd.Parameters.AddWithValue("p_Amount", amount);
                cmd.Parameters.AddWithValue("p_PaymentMode", paymentMode);
                cmd.Parameters.AddWithValue("p_TransactionRef", transactionRef ?? (object)DBNull.Value);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PayBill. BillId={Id}", billId);
                throw;
            }
        }

        public List<OPDBillItem> GetBillItems(int billId)
        {
            var list = new List<OPDBillItem>();
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand(
                    "SELECT * FROM opd_bill_item WHERE BillId = @billId AND IsActive = 1", conn);
                cmd.Parameters.AddWithValue("@billId", billId);
                conn.Open();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new OPDBillItem
                    {
                        ItemId = Convert.ToInt32(r["ItemId"]),
                        BillId = Convert.ToInt32(r["BillId"]),
                        ItemType = r["ItemType"].ToString(),
                        ItemName = r["ItemName"].ToString(),
                        Quantity = Convert.ToInt32(r["Quantity"]),
                        UnitPrice = Convert.ToDecimal(r["UnitPrice"]),
                        TotalPrice = Convert.ToDecimal(r["TotalPrice"]),
                        BillingMasterId = r["BillingMasterId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["BillingMasterId"])
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetBillItems. BillId={Id}", billId);
                throw;
            }
            return list;
        }

        private OPDBill MapBill(MySqlDataReader r)
        {
            return new OPDBill
            {
                BillId = Convert.ToInt32(r["BillId"]),
                AppointmentId = Convert.ToInt32(r["AppointmentId"]),
                OPDId = r["OPDId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["OPDId"]),
                PatientId = Convert.ToInt32(r["PatientId"]),
                HospitalId = Convert.ToInt32(r["HospitalId"]),
                SubHospitalId = r["SubHospitalId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["SubHospitalId"]),
                BillNumber = r["BillNumber"].ToString(),
                BillDate = Convert.ToDateTime(r["BillDate"]),
                ConsultationFee = Convert.ToDecimal(r["ConsultationFee"]),
                MedicineCharges = Convert.ToDecimal(r["MedicineCharges"]),
                ProcedureCharges = Convert.ToDecimal(r["ProcedureCharges"]),
                OtherCharges = Convert.ToDecimal(r["OtherCharges"]),
                SubTotal = Convert.ToDecimal(r["SubTotal"]),
                DiscountPercent = Convert.ToDecimal(r["DiscountPercent"]),
                DiscountAmount = Convert.ToDecimal(r["DiscountAmount"]),
                TotalAmount = Convert.ToDecimal(r["TotalAmount"]),
                PaidAmount = Convert.ToDecimal(r["PaidAmount"]),
                DueAmount = Convert.ToDecimal(r["DueAmount"]),
                PaymentStatus = r["PaymentStatus"].ToString(),
                PaymentMode = r["PaymentMode"]?.ToString(),
                TransactionRef = r["TransactionRef"]?.ToString()
            };
        }
    }
}
