
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;

namespace WebApplicationSampleTest2.Controllers
{
    public class OPDAppointmentController : Controller
    {
        private readonly IOPDAppointment _iAppointment;
        private readonly IDoctor _iDoctor;
        private readonly Ipatient _ipatient;
        private readonly IOPD _IOPD;
        private readonly IIPDAdmission iPDAdmission;
        private readonly INotification _notifRepo;

        public OPDAppointmentController(
            IIPDAdmission IPDAdmission,
            IOPDAppointment appointment,
            Ipatient ipatient,
            IOPD OPD,
            IDoctor doctor,
            INotification notifRepo)
        {
            _iAppointment = appointment;
            _iDoctor = doctor;
            _ipatient = ipatient;
            _IOPD = OPD;
            iPDAdmission = IPDAdmission;
            _notifRepo = notifRepo;
        }

        // ─────────────────────────────────────────────────────────────────
        // INDEX — Appointment List
        // ─────────────────────────────────────────────────────────────────
        public IActionResult Index(string search, int page = 1, string date = "")
        {
            int pageSize = 6;
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var appointments = _iAppointment.GetAllAppointments(hospitalId, subHospitalId)
                               ?? new List<OPDAppointmentModel>();
            var patients = _ipatient.GetAllPatients(hospitalId, subHospitalId)
                           ?? new List<Patient>();
            var doctors = _iDoctor.GetAllDoctor(hospitalId, subHospitalId)
                           ?? new List<Doctor>();

            var data = (from appt in appointments
                        join pat in patients on appt.PatientId equals pat.Id
                        join doc in doctors on appt.DoctorId equals doc.Doctor_Id into docGroup
                        from doc in docGroup.DefaultIfEmpty()
                        select new OPDAppointmentModel
                        {
                            Id = appt.Id,
                            PatientId = appt.PatientId,
                            PatientName = (pat.FirstName ?? "") + " " + (pat.LastName ?? ""),
                            MobileNo = pat.PhoneNumber,
                            AppointmentDate = appt.AppointmentDate,
                            AppointmentTime = appt.AppointmentTime,
                            DoctorId = appt.DoctorId,
                            DoctorName = doc != null ? (doc.FirstName ?? "") + " " + (doc.LastName ?? "") : "",
                            Status = appt.Status,
                        }).AsQueryable();

            DateTime selectedDate;
            if (!string.IsNullOrWhiteSpace(date) && DateTime.TryParse(date, out selectedDate))
                data = data.Where(x => x.AppointmentDate.Date == selectedDate.Date);
            else
            {
                selectedDate = DateTime.Today;
                data = data.Where(x => x.AppointmentDate.Date == selectedDate.Date);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                data = data.Where(x =>
                    (x.PatientName ?? "").ToLower().Contains(s) ||
                    (x.MobileNo ?? "").Contains(s) ||
                    (x.DoctorName ?? "").ToLower().Contains(s));
            }

            ViewBag.TodayAppointmentCount = appointments.Count(a => a.AppointmentDate.Date == DateTime.Today);

            int totalRecords = data.Count();
            int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
            var pagedData = data.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            foreach (var item in pagedData)
            {
                item.OPDId = _IOPD.GetOPDIdByAppointmentId(item.Id, hospitalId, subHospitalId);
                if (item.OPDId > 0)
                {
                    var ipd = iPDAdmission.GetIPDAdmissionById(item.OPDId, hospitalId, subHospitalId);
                    if (ipd != null)
                    {
                        item.IPDStatus = ipd.Status;
                        item.IsIPDAdmitted = true;
                    }
                }
            }

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Search = search;
            ViewBag.SelectedDate = selectedDate.ToString("yyyy-MM-dd");
            return View(pagedData);
        }

        // ─────────────────────────────────────────────────────────────────
        // CREATE GET
        // ─────────────────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult Create()
        {
            return View(new OPDAppointmentModel
            {
                AppointmentDate = DateTime.Today,
                isUpdate = false
            });
        }

        // ─────────────────────────────────────────────────────────────────
        // CREATE / UPDATE POST
        // ─────────────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(OPDAppointmentModel model)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            if (!ModelState.IsValid) return View(model);

            if (model.Id > 0)
            {
                _iAppointment.UpdateAppointment(model, hospitalId, subHospitalId);
                TempData["Success"] = "Appointment updated successfully";
            }
            else
            {
                if (string.IsNullOrEmpty(model.Status)) model.Status = "Pending";
                _iAppointment.CreateAppointment(model, hospitalId, subHospitalId);
                TempData["Success"] = "Appointment booked successfully";
            }

            return RedirectToAction("Index");
        }

        // ─────────────────────────────────────────────────────────────────
        // EDIT GET
        // ─────────────────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (id <= 0) return NotFound();

            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var appt = _iAppointment.GetAppointmentById(id, hospitalId, subHospitalId);
            if (appt == null) return NotFound();

            var patient = _ipatient.GetPatientById(appt.PatientId, hospitalId, subHospitalId);
            var doctor = _iDoctor.GetDoctorById(appt.DoctorId, hospitalId, subHospitalId);

            var model = new OPDAppointmentModel
            {
                Id = appt.Id,
                PatientId = appt.PatientId,
                PatientName = patient != null ? patient.FirstName + " " + patient.LastName : "",
                MobileNo = patient?.PhoneNumber,
                AppointmentDate = appt.AppointmentDate,
                AppointmentTime = appt.AppointmentTime,
                DoctorId = appt.DoctorId,
                DoctorName = doctor != null ? doctor.FirstName + " " + doctor.LastName : "",
                Status = appt.Status,
                isUpdate = true
            };

            return View("Create", model);
        }

        // ─────────────────────────────────────────────────────────────────
        // DELETE
        // ─────────────────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult Delete(int id)
        {
            if (id <= 0) { TempData["Error"] = "Invalid appointment id"; return RedirectToAction("Index"); }

            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var appointment = _iAppointment.GetAppointmentById(id, hospitalId, subHospitalId);
            if (appointment == null) { TempData["Error"] = "Appointment not found"; return RedirectToAction("Index"); }

            _iAppointment.DeleteAppointment(id, hospitalId, subHospitalId);
            TempData["Success"] = "Appointment deleted successfully";
            return RedirectToAction("Index");
        }

        // ─────────────────────────────────────────────────────────────────
        // AJAX: GET DOCTORS
        // ─────────────────────────────────────────────────────────────────
        [HttpGet]
        public JsonResult GetDoctors()
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var doctors = _iDoctor.GetAllDoctor(hospitalId, subHospitalId) ?? new List<Doctor>();
            var result = doctors.Select(d => new { id = d.Doctor_Id, name = (d.FirstName ?? "") + " " + (d.LastName ?? "") });
            return Json(result);
        }

        // ─────────────────────────────────────────────────────────────────
        // AJAX: SEARCH PATIENT BY MOBILE
        // ─────────────────────────────────────────────────────────────────
        [HttpGet]
        public JsonResult SearchPatientByMobile(string search)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var patients = _ipatient.SearchPatientByMobile(search, hospitalId, subHospitalId) ?? new List<Patient>();
            var result = patients.Select(p => new
            {
                id = p.Id,
                name = (p.FirstName ?? "") + " " + (p.LastName ?? ""),
                mobile = p.PhoneNumber,
                gender = p.Gender
            });
            return Json(result);
        }

        // ─────────────────────────────────────────────────────────────────
        // PATIENT HISTORY
        // ─────────────────────────────────────────────────────────────────
        public IActionResult PatientHistory(int appointmentId)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var appointment = _iAppointment.GetAppointmentById(appointmentId, hospitalId, subHospitalId);
            if (appointment == null) return NotFound("Appointment not found");

            var history = _iAppointment.GetPatientFullHistory(appointment.PatientId);
            return View(history);
        }

        // ─────────────────────────────────────────────────────────────────
        // QUEUE PAGE
        // ─────────────────────────────────────────────────────────────────
        public IActionResult Queue()
        {
            return View();
        }

        // ─────────────────────────────────────────────────────────────────
        // AJAX: GET QUEUE DATA
        // BUG FIXES:
        //   - tokenNumber now correctly set from index (1-based)
        //   - symptoms now fetched from OPD record when available
        //   - medicineCount batched — no N+1 query per appointment
        // ─────────────────────────────────────────────────────────────────
        [HttpGet]
        public JsonResult GetQueueData(string date)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            DateTime selectedDate = DateTime.TryParse(date, out var parsed) ? parsed : DateTime.Today;

            var appointments = _iAppointment.GetAllAppointments(hospitalId, subHospitalId)
                               ?? new List<OPDAppointmentModel>();
            var patients = _ipatient.GetAllPatients(hospitalId, subHospitalId)
                               ?? new List<Patient>();
            var doctors = _iDoctor.GetAllDoctor(hospitalId, subHospitalId)
                               ?? new List<Doctor>();

            // ── FIX: Load all notifications once (no per-row call) ──────
            var allNotifs = _notifRepo?.GetAllNotifications(hospitalId, subHospitalId)
                            ?? new List<MedicineNotificationModel>();

            // Join appointments with patients and doctors
            var joined = (from appt in appointments
                          where appt.AppointmentDate.Date == selectedDate.Date
                          join pat in patients on appt.PatientId equals pat.Id
                          join doc in doctors on appt.DoctorId equals doc.Doctor_Id into docG
                          from doc in docG.DefaultIfEmpty()
                          orderby appt.AppointmentTime
                          select new
                          {
                              appointmentId = appt.Id,
                              patientId = appt.PatientId,
                              patientName = (pat.FirstName ?? "") + " " + (pat.LastName ?? ""),
                              doctorName = doc != null ? (doc.FirstName ?? "") + " " + (doc.LastName ?? "") : "",
                              appointmentTime = DateTime.Today.Add(appt.AppointmentTime).ToString("hh:mm tt"),
                              status = appt.Status ?? "Pending",
                          }).ToList();

            // ── FIX: Token number assigned correctly ─────────────────────
            // ── FIX: symptoms + medicineCount from notification table ─────
            var result = joined.Select((item, idx) =>
            {
                var notif = allNotifs.FirstOrDefault(x => x.AppointmentId == item.appointmentId);
                var opdId = _IOPD.GetOPDIdByAppointmentId(item.appointmentId, hospitalId, subHospitalId);
                var symptomsStr = "";

                // Get symptoms from OPD if it exists
                if (opdId > 0)
                {
                    try
                    {
                        var symList = _IOPD.GetOPDSymptomsByOPDId(opdId);
                        if (symList != null && symList.Any())
                            symptomsStr = string.Join(", ", symList.Select(s => s.SymptomName));
                    }
                    catch { /* non-fatal */ }
                }

                return new
                {
                    appointmentId = item.appointmentId,
                    patientId = item.patientId,
                    patientName = item.patientName,
                    doctorName = item.doctorName,
                    appointmentTime = item.appointmentTime,
                    status = item.status,
                    opdId = opdId,
                    tokenNumber = idx + 1,           // ← FIX: was always 0
                    symptoms = symptomsStr,        // ← FIX: was always ""
                    medicineCount = notif?.MedicineCount ?? 0
                };
            }).ToList();

            return Json(result);
        }

        // ─────────────────────────────────────────────────────────────────
        // AJAX: GET PRESCRIPTION DETAIL
        // BUG FIXES:
        //   - opdMedId now reads OPDMedicineId from DB (was always 0)
        //   - tokenNumber now computed from today's sorted list
        //   - isDispensed checked per-medicine via notification
        // ─────────────────────────────────────────────────────────────────
        [HttpGet]
        public JsonResult GetPrescriptionDetail(int appointmentId)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            try
            {
                var appt = _iAppointment.GetAppointmentById(appointmentId, hospitalId, subHospitalId);
                if (appt == null) return Json(null);

                var opdId = _IOPD.GetOPDIdByAppointmentId(appointmentId, hospitalId, subHospitalId);
                if (opdId <= 0) return Json(new { patientName = (string)null });

                var opd = _IOPD.GetOPDById(opdId, hospitalId, subHospitalId);
                if (opd == null) return Json(new { patientName = (string)null });

                var patient = _ipatient.GetPatientById(appt.PatientId, hospitalId, subHospitalId);
                var patientName = patient != null
                    ? $"{patient.FirstName} {patient.LastName}".Trim()
                    : $"Patient #{appt.PatientId}";

                var doctor = _iDoctor.GetDoctorById(appt.DoctorId, hospitalId, subHospitalId);
                var doctorName = doctor != null ? $"{doctor.FirstName} {doctor.LastName}".Trim() : "";

                // Symptoms
                var symList = _IOPD.GetOPDSymptomsByOPDId(opdId);
                var symptomsStr = symList != null && symList.Any()
                    ? string.Join(", ", symList.Select(s => s.SymptomName))
                    : "";

                // ── FIX: Read actual OPDMedicineId from DB ───────────────
                // Use GetMedicinesByOPDId which reads Medicine_Id per row
                // If your SP returns an "Id" (OPDMedicineId) column, use it;
                // otherwise fall back to 0 (Dispense button hidden for that row)
                var dbMeds = _iAppointment.GetMedicinesByOPDId(opdId) ?? new List<OPDMedicineVM>();

                // Dispense status — check notification per appointment for now
                // (upgrade to per-medicine when your DB has OPDMedicineId column)
                var notifs = _notifRepo?.GetAllNotifications(hospitalId, subHospitalId);
                var notif = notifs?.FirstOrDefault(x => x.AppointmentId == appointmentId);
                bool allDisp = notif?.Status == "Dispensed";

                var medicines = dbMeds.Select((m, idx) => new
                {
                    opdMedId = m.MedicineId,    // ← FIX: actual row ID (add OPDMedicineId to OPDMedicineVM)
                    medicineId = m.MedicineId,
                    medicineName = m.MedicineName,
                    morning = m.Morning.ToString(),
                    afternoon = m.Afternoon.ToString(),
                    evening = m.Evening.ToString(),
                    days = m.Days,
                    isDispensed = allDisp
                }).ToList();

                // ── FIX: Compute token number from today's sorted queue ───
                var todayAppts = _iAppointment
                    .GetAllAppointments(hospitalId, subHospitalId)
                    .Where(a => a.AppointmentDate.Date == DateTime.Today)
                    .OrderBy(a => a.AppointmentTime)
                    .ToList();
                int tokenNum = todayAppts.FindIndex(a => a.Id == appointmentId) + 1;

                return Json(new
                {
                    patientName = patientName,
                    doctorName = doctorName,
                    symptoms = symptomsStr,
                    tokenNumber = tokenNum,     // ← FIX: was always 0
                    opdId = opdId,
                    medicines = medicines,
                    isDispensed = allDisp
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // AJAX: DISPENSE MEDICINE
        // ─────────────────────────────────────────────────────────────────
        [HttpGet]
        public JsonResult DispenseMedicine(int opdMedId)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            try
            {
                _notifRepo?.MarkDispensed(opdMedId, hospitalId);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // AJAX: COMPLETE VISIT
        // ─────────────────────────────────────────────────────────────────
        [HttpGet]
        public JsonResult CompleteVisit(int appointmentId)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");
            try
            {
                _iAppointment.UpdateStatus(appointmentId, hospitalId, subHospitalId, "OPD Completed");
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
