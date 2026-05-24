using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Relational;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Security.Cryptography;
using System.Security.Policy;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public class PatientService : Ipatient
    {
        private readonly string _connectionString;

        public PatientService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MySqlConnection");
        }
        public Patient Login(string email, string password)
        {
            Patient patient = null;
            try
            {
                using (MySqlConnection con =
                       new MySqlConnection(_connectionString))
                {
                    // ← Change SP name here
                    using (MySqlCommand cmd =
                           new MySqlCommand(
                               "sp_patient_login_old", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue(
                            "@p_email", email);
                        cmd.Parameters.AddWithValue(
                            "@p_password", password);

                        con.Open();

                        using (MySqlDataReader dr =
                               cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                patient = new Patient
                                {
                                    Id = Convert.ToInt32(
                                                    dr["Id"]),
                                    FirstName = dr["FirstName"]
                                                    .ToString(),
                                    LastName = dr["LastName"]
                                                    .ToString(),
                                    Email = dr["Email"]
                                                    .ToString(),
                                    Hospital_Id = dr["Hospital_Id"]
                                                  == DBNull.Value
                                                  ? 0
                                                  : Convert.ToInt32(
                                                      dr["Hospital_Id"]),
                                    SubHospital_Id =
                                        dr["SubHospital_Id"]
                                        == DBNull.Value
                                        ? (int?)null
                                        : Convert.ToInt32(
                                            dr["SubHospital_Id"])
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(
                    "Error while inserting hospital", ex);
            }
            return patient;
        }

        public bool CheckEmail(string email)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand("sp_check_email", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@p_email", email);

                        con.Open();
                        using (MySqlDataReader dr = cmd.ExecuteReader())
                        {
                            return dr.HasRows;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while inserting hospital", ex);
            }
           
        }

        public bool UpdatePassword(string email, string newPassword)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand("sp_update_password", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@p_email", email);
                        cmd.Parameters.AddWithValue("@p_new_password", newPassword);

                        con.Open();
                        int rows = cmd.ExecuteNonQuery();
                        return rows > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while inserting hospital", ex);
            }
        }

        

        public bool SignupPatient(Patient patient, out string message)
        {
            message = string.Empty;

            try
            {
                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand("sp_patient_Signup", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@p_FirstName", patient.FirstName);
                        cmd.Parameters.AddWithValue("@p_LastName", patient.LastName);
                        cmd.Parameters.AddWithValue("@p_Gender", patient.Gender);
                        cmd.Parameters.AddWithValue("@p_Age", patient.Age);
                        cmd.Parameters.AddWithValue("@p_PhoneNumber", patient.PhoneNumber);
                        cmd.Parameters.AddWithValue("@p_Email", patient.Email);
                        cmd.Parameters.AddWithValue("@p_Password", patient.Password);
                        cmd.Parameters.AddWithValue("@p_Address", patient.Address);

                        con.Open();
                        cmd.ExecuteNonQuery();

                        message = "Patient registered successfully";
                        return true;
                    }
                }
            }
            catch (MySqlException ex)
            {
                // Email or Mobile duplicate error from SIGNAL
                message = ex.Message;
                return false;
            }
        }

        //public int AddPatient(Patient model, int hospitalId, int? subHospitalId)
        //{
        //    try
        //    {
        //        using var con = new MySqlConnection(_connectionString);
        //        using var cmd = new MySqlCommand("sp_create_patient", con);
        //        cmd.CommandType = CommandType.StoredProcedure;

        //        cmd.Parameters.AddWithValue("@p_FirstName", model.FirstName);
        //        cmd.Parameters.AddWithValue("@p_LastName", model.LastName);
        //        cmd.Parameters.AddWithValue("@p_Gender", model.Gender);
        //        cmd.Parameters.AddWithValue("@p_Age", model.Age);
        //        cmd.Parameters.AddWithValue("@p_PhoneNumber", model.PhoneNumber);
        //        cmd.Parameters.AddWithValue("@p_Address", model.Address);
        //        cmd.Parameters.AddWithValue("@p_Hospital_Id", hospitalId);
        //        cmd.Parameters.AddWithValue("@p_SubHospital_Id", subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);

        //        con.Open();
        //        return cmd.ExecuteNonQuery();
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception("Error while inserting hospital", ex);
        //    }

        //}




        public int AddPatient(
    Patient model, int hospitalId, int? subHospitalId)
        {
            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand(
                    "sp_create_patient", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue(
                    "@p_FirstName", model.FirstName);
                cmd.Parameters.AddWithValue(
                    "@p_LastName", model.LastName);
                cmd.Parameters.AddWithValue(
                    "@p_Gender", model.Gender ?? "");
                cmd.Parameters.AddWithValue(
                    "@p_Age", model.Age ?? "0");
                cmd.Parameters.AddWithValue(
                    "@p_PhoneNumber", model.PhoneNumber ?? "");
                cmd.Parameters.AddWithValue(
                    "@p_Address", model.Address ?? "");
                cmd.Parameters.AddWithValue(
                    "@p_Hospital_Id", hospitalId);
                cmd.Parameters.AddWithValue(
                    "@p_SubHospital_Id",
                    subHospitalId.HasValue
                    ? subHospitalId.Value
                    : (object)DBNull.Value);

                con.Open();
                cmd.ExecuteNonQuery();

                // Get last inserted ID
                using var cmdId = new MySqlCommand(
                    "SELECT LAST_INSERT_ID()", con);
                return Convert.ToInt32(cmdId.ExecuteScalar());
            }
            catch (Exception ex)
            {
                throw new Exception(
                    "Error while inserting patient", ex);
            }
        }




        public List<Patient> GetAllPatients(int hospitalId, int? subHospitalId)
        {

            List<Patient> list = new List<Patient>();
            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_get_all_patient", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@p_Hospital_Id", hospitalId);
                cmd.Parameters.AddWithValue("@p_SubHospital_Id", subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);

                con.Open();
                using var dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    list.Add(new Patient
                    {
                        Id = Convert.ToInt32(dr["Id"]),
                        FirstName = dr["FirstName"].ToString(),
                        LastName = dr["LastName"].ToString(),
                        Gender = dr["Gender"].ToString(),
                        Age = dr["Age"].ToString(),
                        PhoneNumber = dr["PhoneNumber"].ToString(),
                        Address = dr["Address"].ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while inserting hospital", ex);
            }
           
            return list;
        }

        public Patient GetPatientById(int patientId, int hospitalId, int? subHospitalId)
        {
            Patient patient = null;
            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_get_patient_by_id", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@p_PatientId", patientId);
                cmd.Parameters.AddWithValue("@p_Hospital_Id", hospitalId);
                cmd.Parameters.AddWithValue("@p_SubHospital_Id", subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);

                con.Open();
                using var dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    patient = new Patient
                    {
                        Id = Convert.ToInt32(dr["Id"]),
                        FirstName = dr["FirstName"].ToString(),
                        LastName = dr["LastName"].ToString(),
                        Gender = dr["Gender"].ToString(),
                        Age = dr["Age"].ToString(),
                        PhoneNumber = dr["PhoneNumber"].ToString(),
                        Address = dr["Address"].ToString()
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while inserting hospital", ex);
            }
          
            return patient;
        }

        public int UpdatePatient(Patient model, int hospitalId, int? subHospitalId)
        {
            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_update_patient", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@p_PatientId", model.Id);
                cmd.Parameters.AddWithValue("@p_FirstName", model.FirstName);
                cmd.Parameters.AddWithValue("@p_LastName", model.LastName);
                cmd.Parameters.AddWithValue("@p_Gender", model.Gender);
                cmd.Parameters.AddWithValue("@p_Age", model.Age);
                cmd.Parameters.AddWithValue("@p_PhoneNumber", model.PhoneNumber);
                cmd.Parameters.AddWithValue("@p_Address", model.Address);
                cmd.Parameters.AddWithValue("@p_Hospital_Id", hospitalId);
                cmd.Parameters.AddWithValue("@p_SubHospital_Id", subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);

                con.Open();
                return cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception("Error while inserting hospital", ex);
            }
           
        }

        public int DeletePatient(int patientId, int hospitalId, int? subHospitalId)
        {
            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_delete_patient", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@p_PatientId", patientId);
                cmd.Parameters.AddWithValue("@p_Hospital_Id", hospitalId);
                cmd.Parameters.AddWithValue("@p_SubHospital_Id", subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);

                con.Open();
                return cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception("Error while inserting hospital", ex);
            }

        }

        public List<Patient> SearchPatientByMobile(string search, int hospitalId, int? subHospitalId)
        {
            List<Patient> list = new List<Patient>();
            try
            {
                using (var con = new MySqlConnection(_connectionString))
                {
                    using (var cmd = new MySqlCommand("sp_search_patients", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("p_Search", search);
                        cmd.Parameters.AddWithValue("p_Hospital_Id", hospitalId);
                        cmd.Parameters.AddWithValue("@p_SubHospital_Id", subHospitalId.HasValue ? subHospitalId.Value : (object)DBNull.Value);

                        con.Open();
                        using (var dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                list.Add(new Patient
                                {
                                    Id = Convert.ToInt32(dr["Id"]),
                                    FirstName = dr["FirstName"].ToString(),
                                    LastName = dr["LastName"].ToString(),
                                    PhoneNumber = dr["PhoneNumber"].ToString(),
                                    Gender = dr["Gender"].ToString()

                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while inserting hospital", ex);
            }
            return list;
        }









        // ── ADD THESE METHODS to existing PatientService class ──

        // New login — checks tbl_patient_account
        public PatientAccount LoginAccount(string email, string password)
        {
            PatientAccount account = null;
            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_patient_login", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@p_email", email);
                cmd.Parameters.AddWithValue("@p_password", password);
                con.Open();
                using var dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    account = new PatientAccount
                    {
                        AccountId = Convert.ToInt32(dr["AccountId"]),
                        Email = dr["Email"].ToString(),
                        FirstName = dr["FirstName"].ToString(),
                        LastName = dr["LastName"].ToString(),
                        PhoneNumber = dr["PhoneNumber"]?.ToString()
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error during account login", ex);
            }
            return account;
        }

        // Get all profiles (patient records) for one account
        public List<PatientProfileVM> GetProfilesByAccountId(
    int accountId)
        {
            var list = new List<PatientProfileVM>();
            try
            {
                using var con = new MySqlConnection(
                    _connectionString);
                using var cmd = new MySqlCommand(
                    "sp_get_profiles_by_account", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue(
                    "@p_AccountId", accountId);
                con.Open();
                using var dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    list.Add(new PatientProfileVM
                    {
                        PatientId =
                            dr["PatientId"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(dr["PatientId"]),

                        FirstName =
                            dr["FirstName"] == DBNull.Value
                            ? ""
                            : dr["FirstName"].ToString(),

                        LastName =
                            dr["LastName"] == DBNull.Value
                            ? ""
                            : dr["LastName"].ToString(),

                        Gender =
                            dr["Gender"] == DBNull.Value
                            ? ""
                            : dr["Gender"].ToString(),

                        Age =
                            dr["Age"] == DBNull.Value
                            ? ""
                            : dr["Age"].ToString(),

                        Relation =
                            dr["Relation"] == DBNull.Value
                            ? "Self"
                            : dr["Relation"].ToString(),

                        Hospital_Id =
                            dr["Hospital_Id"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(
                                dr["Hospital_Id"]),

                        SubHospital_Id =
                            dr["SubHospital_Id"] == DBNull.Value
                            ? (int?)null
                            : Convert.ToInt32(
                                dr["SubHospital_Id"]),

                        HospitalName =
                            dr["HospitalName"] == DBNull.Value
                            ? ""
                            : dr["HospitalName"].ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception(
                    "Error fetching profiles", ex);
            }
            return list;
        }

        // New signup — saves to tbl_patient_account
        public int SignupAccount(PatientAccount account, out string message)
        {
            message = string.Empty;
            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand(
                    "sp_patient_signup_new", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@p_FirstName", account.FirstName);
                cmd.Parameters.AddWithValue("@p_LastName", account.LastName);
                cmd.Parameters.AddWithValue("@p_Email", account.Email);
                cmd.Parameters.AddWithValue("@p_Password", account.Password);
                cmd.Parameters.AddWithValue("@p_PhoneNumber", account.PhoneNumber ?? "");

                var pAccountId = new MySqlParameter("@p_AccountId", MySqlDbType.Int32)
                { Direction = System.Data.ParameterDirection.Output };
                var pMessage = new MySqlParameter("@p_Message", MySqlDbType.VarChar, 255)
                { Direction = System.Data.ParameterDirection.Output };

                cmd.Parameters.Add(pAccountId);
                cmd.Parameters.Add(pMessage);

                con.Open();
                cmd.ExecuteNonQuery();

                message = pMessage.Value?.ToString();
                return Convert.ToInt32(pAccountId.Value);
            }
            catch (Exception ex)
            {
                throw new Exception("Error during signup", ex);
            }
        }

        // Generate OTP and save to tbl_otp
        public void GenerateOTP(string email, string purpose, string otp)
        {
            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_generate_otp", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@p_Email", email);
                cmd.Parameters.AddWithValue("@p_Purpose", purpose);
                cmd.Parameters.AddWithValue("@p_OTP", otp);
                con.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception("Error generating OTP", ex);
            }
        }

        // Verify OTP from tbl_otp
        public bool VerifyOTP(string email, string otp, string purpose)
        {
            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_verify_otp", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@p_Email", email);
                cmd.Parameters.AddWithValue("@p_OTP", otp);
                cmd.Parameters.AddWithValue("@p_Purpose", purpose);

                var pResult = new MySqlParameter("@p_Result", MySqlDbType.Byte)
                { Direction = System.Data.ParameterDirection.Output };
                cmd.Parameters.Add(pResult);

                con.Open();
                cmd.ExecuteNonQuery();

                return Convert.ToBoolean(pResult.Value);
            }
            catch (Exception ex)
            {
                throw new Exception("Error verifying OTP", ex);
            }
        }

        // Reset password in tbl_patient_account
        public void ResetPasswordAccount(string email, string newPassword)
        {
            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_reset_password", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@p_Email", email);
                cmd.Parameters.AddWithValue("@p_NewPassword", newPassword);
                con.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception("Error resetting password", ex);
            }
        }

        // Check if email exists in tbl_patient_account
        public bool EmailExistsInAccount(string email)
        {
            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand(@"
            SELECT COUNT(*) FROM tbl_patient_account
            WHERE Email = @email", con);
                cmd.Parameters.AddWithValue("@email", email);
                con.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Error checking email", ex);
            }
        }




        public void AddFamilyMember(
            int accountId,
            string firstName,
            string lastName,
            string relation,
            int age,
            string gender,
            string email)
        {
            try
            {
                using var con = new MySqlConnection(
                    _connectionString);
                using var cmd = new MySqlCommand(@"
            INSERT INTO tbl_patient
                (FirstName, LastName, Gender, Age,
                 Email, AccountId, Relation)
            VALUES
                (@FirstName, @LastName, @Gender, @Age,
                 @Email, @AccountId, @Relation)", con);

                cmd.Parameters.AddWithValue(
                    "@FirstName", firstName);
                cmd.Parameters.AddWithValue(
                    "@LastName", lastName);
                cmd.Parameters.AddWithValue(
                    "@Gender", gender ?? "");
                cmd.Parameters.AddWithValue(
                    "@Age", age);
                cmd.Parameters.AddWithValue(
                    "@Email", email ?? "");
                cmd.Parameters.AddWithValue(
                    "@AccountId", accountId);
                cmd.Parameters.AddWithValue(
                    "@Relation", relation);

                con.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception(
                    "Error adding family member", ex);
            }
        }


        public bool IsSamePassword(string email, string newPassword)
        {
            try
            {
                using var con = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand(@"
            SELECT COUNT(*) FROM tbl_patient_account
            WHERE Email = @email AND Password = @password", con);

                cmd.Parameters.AddWithValue("@email", email);
                cmd.Parameters.AddWithValue("@password", newPassword);

                con.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Error checking password", ex);
            }
        }


    }

 }

