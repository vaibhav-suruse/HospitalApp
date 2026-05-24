using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Linq;
using WebApplicationSampleTest2.Repository;
using WebApplicationSampleTest2.Models;


public class IPDDashboardController : Controller
{
    private readonly IBed _bedRepo;
    private readonly IWard _wardRepo;
    private readonly IRoom _roomRepo;
    private readonly IDoctor _doctorRepo;
    private readonly Ipatient _patientRepo;
    private readonly string _connectionString;
    

    public IPDDashboardController(
        IBed bedRepo, IWard wardRepo, IRoom roomRepo,
        IDoctor doctorRepo, Ipatient patientRepo,
        IConfiguration configuration)
    {
        _bedRepo = bedRepo;
        _wardRepo = wardRepo;
        _roomRepo = roomRepo;
        _doctorRepo = doctorRepo;
        _patientRepo = patientRepo;
        _connectionString = configuration.GetConnectionString("MySqlConnection");
    }

    public IActionResult Index()
    {
        try
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var allBeds = _bedRepo.GetAllBeds(hospitalId, subHospitalId);
            var wards = _wardRepo.GetAllWards(hospitalId, subHospitalId);
            var rooms = _roomRepo.GetAllRoom(hospitalId, subHospitalId);
            var doctors = _doctorRepo.GetAllDoctor(hospitalId, subHospitalId);
            var patients = _patientRepo.GetAllPatients(hospitalId, subHospitalId);

            var vm = new IPDDashboardVM
            {
                TotalRooms = rooms.Count,
                TotalBeds = allBeds.Count,
                OccupiedBeds = allBeds.Count(b => b.OperationalStatus == "Occupied" && b.IsActive),
                MaintenanceBeds = allBeds.Count(b => b.OperationalStatus == "Maintenance"),
                AvailableBeds = allBeds.Count(b => b.OperationalStatus == "Active" && b.IsActive),
                TotalWards = wards.Count,
                TotalDoctors = doctors.Count,
                TotalPatients = patients.Count
            };

            // Ward-wise
            foreach (var w in wards)
            {
                var bedsInWard = allBeds.Where(b => b.WardId == w.WardId).ToList();
                vm.WardWiseBeds.Add(new WardBedInfo
                {
                    WardName = w.WardName,
                    Total = bedsInWard.Count,
                    Occupied = bedsInWard.Count(b => b.OperationalStatus == "Occupied")
                });
            }

            // SP Data
            using (var conn = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand("sp_GetIPDDashboard", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_HospitalId", hospitalId);
                cmd.Parameters.AddWithValue("p_SubHospitalId",
                    subHospitalId.HasValue ? (object)subHospitalId.Value : DBNull.Value);

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    // RS1 - Today Admissions
                    if (reader.Read())
                        vm.TodayAdmissions = Convert.ToInt32(reader["TodayAdmissions"]);

                    // RS2 - Today Discharges
                    reader.NextResult();
                    if (reader.Read())
                        vm.TodayDischarges = Convert.ToInt32(reader["TodayDischarges"]);

                    // RS3 - Currently Admitted
                    reader.NextResult();
                    if (reader.Read())
                        vm.CurrentlyAdmitted = Convert.ToInt32(reader["CurrentlyAdmitted"]);

                    // RS4 - Weekly
                    reader.NextResult();
                    if (reader.Read())
                        vm.WeeklyAdmissions = Convert.ToInt32(reader["WeeklyAdmissions"]);

                    // RS5 - Monthly
                    reader.NextResult();
                    if (reader.Read())
                        vm.MonthlyAdmissions = Convert.ToInt32(reader["MonthlyAdmissions"]);

                    // RS6 - Yearly
                    reader.NextResult();
                    if (reader.Read())
                        vm.YearlyAdmissions = Convert.ToInt32(reader["YearlyAdmissions"]);

                    // RS7 - Pending Lab
                    reader.NextResult();
                    if (reader.Read())
                        vm.PendingLab = Convert.ToInt32(reader["PendingLab"]);

                    // RS8 - Critical
                    reader.NextResult();
                    if (reader.Read())
                        vm.CriticalPatients = Convert.ToInt32(reader["CriticalPatients"]);

                    // RS9 - Chart trend
                    reader.NextResult();
                    while (reader.Read())
                    {
                        vm.ChartDates.Add(
                            Convert.ToDateTime(reader["AdmissionDate"]).ToString("dd MMM"));
                        vm.ChartCounts.Add(Convert.ToInt32(reader["Count"]));
                    }
                }
            }

            return View(vm);
        }
        catch (Exception ex)
        {
            ViewBag.Error = ex.Message;
            return View(new IPDDashboardVM());
        }
    }
}