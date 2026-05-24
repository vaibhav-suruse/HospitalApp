using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using WebApplicationSampleTest2.Models;

namespace WebApplicationSampleTest2.Repository
{
    public class OPDAppointmentRepository:IOPDAppointment
    {
        private readonly string _connectionString;

        public OPDAppointmentRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MySqlConnection");
        }
        // Create appointment
        public void CreateAppointment(OPDAppointmentModel appointment, int hospitalId, int? subHospitalId)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_OPDAppointment_Insert", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("p_PatientId", appointment.PatientId);
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                //cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", (object?)subHospitalId ?? DBNull.Value);

                cmd.Parameters.AddWithValue("p_DoctorId", appointment.DoctorId);
                cmd.Parameters.AddWithValue("p_AppointmentDate", appointment.AppointmentDate);
                cmd.Parameters.AddWithValue("p_AppointmentTime", appointment.AppointmentTime);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception("Error while inserting hospital", ex);
            }
           
        }

        // Update appointment
        public int UpdateAppointment(OPDAppointmentModel appointment, int hospitalId, int? subHospitalId)
        {

            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_OPDAppointment_Update", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("p_Id", appointment.Id);
                cmd.Parameters.AddWithValue("p_PatientId", appointment.PatientId);
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", (object?)subHospitalId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_DoctorId", appointment.DoctorId);
                cmd.Parameters.AddWithValue("p_AppointmentDate", appointment.AppointmentDate);
                cmd.Parameters.AddWithValue("p_AppointmentTime", appointment.AppointmentTime);
                cmd.Parameters.AddWithValue("p_IsActive", appointment.IsActive);

                conn.Open();
                return cmd.ExecuteNonQuery(); // returns number of rows affected
            }
            catch (Exception ex)
            {
                throw new Exception("Error while inserting hospital", ex);
            }
           
        }

        // Soft delete appointment
        public void DeleteAppointment(int appointmentId, int hospitalId, int? subHospitalId)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_OPDAppointment_Delete", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("p_Id", appointmentId);
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                //cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", (object?)subHospitalId ?? DBNull.Value);



                conn.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception("Error while inserting hospital", ex);
            }
           
        }

        // Get appointment by Id
        public OPDAppointmentModel? GetAppointmentById(int appointmentId, int hospitalId, int? subHospitalId)
        {
            OPDAppointmentModel? appointment = null;
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_OPDAppointment_GetById", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("p_Id", appointmentId);
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                //cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", (object?)subHospitalId ?? DBNull.Value);


                conn.Open();
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    appointment = new OPDAppointmentModel
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        PatientId = Convert.ToInt32(reader["PatientId"]),
                        DoctorId = Convert.ToInt32(reader["DoctorId"]),
                        HospitalId = Convert.ToInt32(reader["HospitalId"]),
                        //SubHospitalId = Convert.ToInt32(reader["SubHospitalId"]),
                        SubHospitalId = reader["SubHospitalId"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["SubHospitalId"]),
                        AppointmentDate = Convert.ToDateTime(reader["AppointmentDate"]),
                        AppointmentTime = TimeSpan.Parse(reader["AppointmentTime"].ToString()),
                        IsActive = Convert.ToBoolean(reader["IsActive"])
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while inserting hospital", ex);
            }
            return appointment;
        }

        // Get all appointments
        public List<OPDAppointmentModel> GetAllAppointments(int hospitalId, int? subHospitalId)
        {
            var list = new List<OPDAppointmentModel>();
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_OPDAppointment_GetAll", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                //cmd.Parameters.AddWithValue("p_SubHospitalId", subHospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", (object?)subHospitalId ?? DBNull.Value);


                conn.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new OPDAppointmentModel
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        PatientId = Convert.ToInt32(reader["PatientId"]),
                        DoctorId = Convert.ToInt32(reader["DoctorId"]),
                        HospitalId = Convert.ToInt32(reader["HospitalId"]),
                        //SubHospitalId = Convert.ToInt32(reader["SubHospitalId"]),
                        SubHospitalId = reader["SubHospitalId"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["SubHospitalId"]),

                        AppointmentDate = Convert.ToDateTime(reader["AppointmentDate"]),
                        AppointmentTime = TimeSpan.Parse(reader["AppointmentTime"].ToString()),
                        IsActive = Convert.ToBoolean(reader["IsActive"]),
                        Status = reader["Status"].ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while inserting hospital", ex);
            }
            return list;
        }

        public void UpdateStatus(int appointmentId, int hospitalId, int? subHospitalId, string status)
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                using var cmd = new MySqlCommand("sp_OPDAppointment_UpdateStatus", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                // 🔹 Add parameters
                cmd.Parameters.AddWithValue("p_AppointmentId", appointmentId);
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId", (object?)subHospitalId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_Status", status);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception("Error while inserting hospital", ex);
            }
            
        }

        public List<OPDMedicineVM> GetMedicinesByOPDId(int opdId)
        {
            List<OPDMedicineVM> list = new List<OPDMedicineVM>();
            try
            {
                using (MySqlConnection con = new MySqlConnection(_connectionString))
                {
                    using (MySqlCommand cmd = new MySqlCommand("sp_GetMedicinesByOPDId", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("p_OPDId", opdId);

                        con.Open();

                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                list.Add(new OPDMedicineVM
                                {
                                    MedicineId = Convert.ToInt32(reader["Medicine_Id"]),
                                    MedicineName = reader["MedicineName"].ToString(),
                                    Morning = Convert.ToInt32(reader["Morning"]),
                                    Afternoon = Convert.ToInt32(reader["Afternoon"]),
                                    Evening = Convert.ToInt32(reader["Evening"]),
                                    Days = Convert.ToInt32(reader["Days"])
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

        public List<OPD> GetPatientFullHistory(int patientId)
        {
            var opdList = new List<OPD>();
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                using (var cmd = new MySqlCommand("GetPatientFullHistory", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@PatientId", patientId);

                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        // 1️⃣ Read OPD visits
                        while (reader.Read())
                        {
                            opdList.Add(new OPD
                            {
                                Id = Convert.ToInt32(reader["OPDId"]),
                                AppointmentId = Convert.ToInt32(reader["AppointmentId"]),
                                AppointmentDate = Convert.ToDateTime(reader["AppointmentDate"]),
                                BP = reader["BP"].ToString(),
                                Pulse = reader["Pulse"].ToString(),
                                Investigation = reader["Investigation"].ToString(),
                                ReportDetail = reader["ReportDetail"].ToString(),
                                ReportFilePath = reader["ReportFilePath"].ToString(),
                                NextAppointmentDate = reader["NextAppointmentDate"] != DBNull.Value
                                                      ? Convert.ToDateTime(reader["NextAppointmentDate"])
                                                      : (DateTime?)null
                            });
                        }

                        // 2️⃣ Read Symptoms
                        if (reader.NextResult())
                        {
                            while (reader.Read())
                            {
                                var opd = opdList.Find(x => x.Id == Convert.ToInt32(reader["OPD_Id"]));
                                if (opd != null)
                                {
                                    opd.Symptom.Add(reader["SymptomName"].ToString());
                                    // If you want full symptom details, you can also store OPDSymptomVM in a separate list
                                }
                            }
                        }

                        // 3️⃣ Read Medicines
                        if (reader.NextResult())
                        {
                            while (reader.Read())
                            {
                                var opd = opdList.Find(x => x.Id == Convert.ToInt32(reader["OPD_Id"]));
                                if (opd != null)
                                {
                                    opd.Medicines.Add(new OPDMedicine
                                    {
                                        MedicineName = reader["MedicineName"].ToString(),
                                        MedicineId = 0, // optional: you can pass Medicine_Id from SP if needed
                                        Morning = reader["Morning"].ToString(),
                                        Afternoon = reader["Afternoon"].ToString(),
                                        Evening = reader["Evening"].ToString(),
                                        Days = Convert.ToInt32(reader["Days"])
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error while inserting hospital", ex);
            }

           

            return opdList;
        }

    }
}
