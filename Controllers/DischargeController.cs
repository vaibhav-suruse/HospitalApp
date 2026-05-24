// DischargeController.cs
// Full file — replace your existing DischargeController.cs with this

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;

namespace WebApplicationSampleTest2.Controllers
{
    public class DischargeController : Controller
    {
        private readonly IDischarge _dischargeRepo;
        private readonly IDoctor _doctorRepo;
        private readonly IHospital _IHospital;
        private readonly IMedicine _medicineRepo;
        private readonly IDoctorRound _roundRepo;
        private readonly IIPDNurseVitals _vitalsRepo;

        public DischargeController(
            IDischarge dischargeRepo,
            IDoctor doctorRepo,
            IHospital iHospital,
            IMedicine medicineRepo,
            IDoctorRound roundRepo,
            IIPDNurseVitals vitalsRepo)
        {
            _dischargeRepo = dischargeRepo;
            _doctorRepo = doctorRepo;
            _IHospital = iHospital;
            _medicineRepo = medicineRepo;
            _roundRepo = roundRepo;
            _vitalsRepo = vitalsRepo;
        }

        [HttpGet]
        public IActionResult Discharge(int ipdId)
        {
            try
            {
                var model = _dischargeRepo.GetAdmissionForDischarge(ipdId);
                if (model == null) return NotFound();

                if (model.Status == "Discharged")
                {
                    TempData["Error"] = "Patient is already discharged.";
                    return RedirectToAction("Details", "IPDAdmission", new { id = ipdId });
                }

                model.DischargeMedicines = _dischargeRepo.GetDischargeMedicines(ipdId);
                LoadDropdowns(ipdId);
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Details", "IPDAdmission", new { id = ipdId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Discharge(DischargeModel model)
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;

            if (!ModelState.IsValid)
            {
                LoadDropdowns(model.IPDId);
                var existing = _dischargeRepo.GetAdmissionForDischarge(model.IPDId);
                model.PatientName = existing.PatientName;
                model.AdmissionNumber = existing.AdmissionNumber;
                model.AdmissionDateTime = existing.AdmissionDateTime;
                model.PrimaryDoctorName = existing.PrimaryDoctorName;
                model.BedNumber = existing.BedNumber;
                model.WardName = existing.WardName;
                model.TotalDaysStayed = existing.TotalDaysStayed;
                return View(model);
            }

            try
            {
                // Step 1: Discharge the patient
                _dischargeRepo.DischargePatient(model, userId);

                // Step 2: Save medicines submitted with the form
                if (model.DischargeMedicines != null && model.DischargeMedicines.Count > 0)
                {
                    var existingMeds = _dischargeRepo.GetDischargeMedicines(model.IPDId);
                    foreach (var existing in existingMeds)
                        _dischargeRepo.DeleteDischargeMedicine(existing.Id);

                    foreach (var med in model.DischargeMedicines)
                    {
                        if (med.MedicineId <= 0) continue;
                        med.IPDId = model.IPDId;
                        _dischargeRepo.SaveDischargeMedicine(med, userId);
                    }
                }

                TempData["Success"] = "Patient discharged successfully.";
                return RedirectToAction("Summary", new { ipdId = model.IPDId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                LoadDropdowns(model.IPDId);
                var existing = _dischargeRepo.GetAdmissionForDischarge(model.IPDId);
                model.PatientName = existing.PatientName;
                model.AdmissionNumber = existing.AdmissionNumber;
                model.AdmissionDateTime = existing.AdmissionDateTime;
                model.PrimaryDoctorName = existing.PrimaryDoctorName;
                model.BedNumber = existing.BedNumber;
                model.WardName = existing.WardName;
                model.TotalDaysStayed = existing.TotalDaysStayed;
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Summary(int ipdId)
        {
            try
            {
                var model = _dischargeRepo.GetDischargeSummary(ipdId);
                if (model == null) return NotFound();

                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                model.DischargeMedicines = _dischargeRepo.GetDischargeMedicines(ipdId);

                // Load rounds
                var allRounds = _roundRepo.GetAllRoundsPrescriptionPrint(ipdId);
                ViewBag.Rounds = allRounds?.Rounds ?? new List<IPDRoundPrintVM>();

                // ✅ Load vitals
                ViewBag.Vitals = _vitalsRepo.GetVitalsByIPDId(ipdId, hospitalId, subHospitalId)
                                 ?? new List<IPDNurseVitals>();

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Index", "IPDAdmission");
            }
        }

        [HttpGet]
        public IActionResult PrintSummary(int ipdId)
        {
            try
            {
                var model = _dischargeRepo.GetDischargeSummary(ipdId);
                if (model == null) return NotFound();

                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                var hospital = _IHospital.GetsubandMainHospitalById(hospitalId, subHospitalId);
                ViewBag.HospitalName = hospital?.Name;
                ViewBag.HospitalAddress = hospital?.Address;
                ViewBag.HospitalPhone = hospital?.PhoneNumber;
                ViewBag.HospitalEmail = hospital?.EmailId;
                ViewBag.HospitalLogo = hospital?.Logo;
                ViewBag.HospitalRegNo = hospital?.RegistrationNumber;

                ViewBag.DischargeMedicines = _dischargeRepo.GetDischargeMedicines(ipdId);

                // Load rounds
                var allRounds = _roundRepo.GetAllRoundsPrescriptionPrint(ipdId);
                ViewBag.Rounds = allRounds?.Rounds ?? new List<IPDRoundPrintVM>();

                // ✅ Load vitals
                ViewBag.Vitals = _vitalsRepo.GetVitalsByIPDId(ipdId, hospitalId, subHospitalId)
                                 ?? new List<IPDNurseVitals>();

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Summary", new { ipdId = ipdId });
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult AddMedicine([FromForm] DischargeMedicineModel model)
        {
            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                _dischargeRepo.SaveDischargeMedicine(model, userId);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult DeleteMedicine(int id, int ipdId)
        {
            try
            {
                _dischargeRepo.DeleteDischargeMedicine(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private void LoadDropdowns(int ipdId)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var doctors = _doctorRepo.GetAllDoctor(hospitalId, subHospitalId)
                .Select(d => new SelectListItem
                {
                    Value = d.Doctor_Id.ToString(),
                    Text = $"Dr. {d.FirstName} {d.LastName} - {d.Specialization}"
                }).ToList();

            var medicines = _medicineRepo.GetAllMedicine(hospitalId, subHospitalId)
                .Select(m => new SelectListItem
                {
                    Value = m.MedicineId.ToString(),
                    Text = $"{m.MedicineName} ({m.Type})"
                }).ToList();

            ViewBag.Doctors = doctors;
            ViewBag.Medicines = medicines;
            ViewBag.DischargeTypes = new SelectList(new[]
                { "Regular", "LAMA", "Death", "Referral", "Absconded" });
            ViewBag.DischargeConditions = new SelectList(new[]
                { "Good", "Fair", "Poor", "Expired" });
        }
    }
}