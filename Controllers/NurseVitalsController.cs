using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;

namespace WebApplicationSampleTest2.Controllers
{
    public class NurseVitalsController : Controller
    {
        private readonly IIPDNurseVitals _vitalsRepo;
        private readonly INurse _nurseRepo;
        private readonly IIPDNursingCharge _nursingChargeRepo;
        private readonly INursingChargesMaster _nursingMasterRepo;


        public NurseVitalsController(IIPDNurseVitals vitalsRepo, INurse nurseRepo,
    IIPDNursingCharge nursingChargeRepo,INursingChargesMaster nursingMasterRepo)
        {
            _vitalsRepo = vitalsRepo;
            _nurseRepo = nurseRepo;
            _nursingChargeRepo = nursingChargeRepo;
            _nursingMasterRepo = nursingMasterRepo;
        }

        // ===============================
        // Private Method - Load Nurses
        // ===============================
        private void LoadNurses()
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            var nurses = _nurseRepo.GetAll(hospitalId, subHospitalId) ?? new List<NurseModel>();

            ViewBag.Nurses = new SelectList(
                nurses.Select(n => new
                {
                    Id = n.NurseId,
                    Name = n.FirstName + " " + n.LastName
                }),
                "Id",
                "Name"
            );
        }
        // ===============================
        // INDEX - List Vitals By IPD
        // ===============================
        public IActionResult Index( int hospitalId, int? subHospitalId)
        {
            var vitalsList = _vitalsRepo.GetVitalsByHospital( hospitalId, subHospitalId);

           
            ViewBag.HospitalId = hospitalId;
            ViewBag.SubHospitalId = subHospitalId;

            return View(vitalsList);
        }

       
        [HttpGet]
        public IActionResult Create(int ipdId)
        {
            int hid = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subid = HttpContext.Session.GetInt32("SubHospitalId");
            LoadNurses();

            var model = new IPDNurseVitals
            {

                IPDId = ipdId,
                RecordedDateTime = DateTime.Now
            };

            ViewBag.NursingMaster = _nursingMasterRepo.GetAll(hid, subid);

            return View(model);
        }

        // ===============================
        // CREATE (POST)
        // ===============================
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public IActionResult Create(IPDNurseVitals model, List<IPDNursingCharge> NursingProcedures)
        //{
        //    int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
        //    int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

        //    try
        //    {
        //        if (hospitalId <= 0)
        //            throw new Exception("Invalid hospital session.");

        //        if (!ModelState.IsValid)
        //        {
        //            LoadNurses();
        //            int hid = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
        //            int? subid = HttpContext.Session.GetInt32("SubHospitalId");
        //            ViewBag.NursingMaster = _nursingMasterRepo.GetAll(hid, subid); // ← ADD
        //            return View(model);

        //        }

        //        model.ParentHospitalId = hospitalId;
        //        model.SubHospitalId = subHospitalId;
        //        model.CreatedDate = DateTime.Now;
        //        model.IsActive = true;

        //        _vitalsRepo.CreateVitals(model);

        //        TempData["Success"] = "Vitals added successfully.";
        //        if (NursingProcedures != null && NursingProcedures.Count > 0)
        //        {
        //            int hid =
        //                HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
        //            int? subid =
        //                HttpContext.Session.GetInt32("SubHospitalId");

        //            foreach (var c in NursingProcedures)
        //            {
        //                c.IPDId = model.IPDId;
        //                c.ParentHospitalId = hid;
        //                c.SubHospitalId = subid;
        //                c.ChargeDate = model.RecordedDateTime.Date;
        //                c.NurseId = model.NurseId;
        //            }
        //            _nursingChargeRepo.SaveCharges(NursingProcedures);
        //        }
        //        return RedirectToAction("Details", "IPDAdmission", new { id = model.IPDId });
        //    }
        //    // Same in catch block:
        //    catch (Exception ex)
        //    {
        //        LoadNurses();
        //        int hid = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
        //        int? subid = HttpContext.Session.GetInt32("SubHospitalId");
        //        ViewBag.NursingMaster = _nursingMasterRepo.GetAll(hid, subid); // ← ADD
        //        TempData["Error"] = ex.Message;
        //        return View(model);
        //    }
        //}




        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public IActionResult Create(IPDNurseVitals model)
        //{
        //    int hospitalId =
        //        HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
        //    int? subHospitalId =
        //        HttpContext.Session.GetInt32("SubHospitalId");

        //    try
        //    {
        //        if (hospitalId <= 0)
        //            throw new Exception("Invalid hospital session.");

        //        if (!ModelState.IsValid)
        //        {
        //            LoadNurses();
        //            LoadNursingMaster();
        //            return View(model);
        //        }

        //        model.ParentHospitalId = hospitalId;
        //        model.SubHospitalId = subHospitalId;
        //        model.CreatedDate = DateTime.Now;
        //        model.IsActive = true;

        //        // ── Step 1: Save vitals ──────────────────────────────────────
        //        _vitalsRepo.CreateVitals(model);

        //        // ── Step 2: Save nursing procedures ─────────────────────────
        //        if (NursingProcedures != null
        //            && NursingProcedures.Count > 0)
        //        {
        //            // ✅ FIX — set ALL required fields here
        //            // never rely on model binding for session values
        //            foreach (var c in NursingProcedures)
        //            {
        //                c.IPDId = model.IPDId;
        //                c.ParentHospitalId = hospitalId;   // ← from session
        //                c.SubHospitalId = subHospitalId; // ← from session
        //                c.ChargeDate = model.RecordedDateTime.Date;
        //                c.NurseId = model.NurseId;

        //                // ✅ FIX — recalculate total in case JS didn't post it
        //                if (c.TotalCharge <= 0)
        //                    c.TotalCharge = c.Quantity * c.UnitCharge;

        //                // ✅ FIX — default quantity
        //                if (c.Quantity <= 0) c.Quantity = 1;
        //            }

        //            _nursingChargeRepo.SaveCharges(NursingProcedures);
        //        }

        //        TempData["Success"] = "Vitals added successfully.";
        //        return RedirectToAction("Details", "IPDAdmission",
        //            new { id = model.IPDId });
        //    }
        //    catch (Exception ex)
        //    {
        //        LoadNurses();
        //        LoadNursingMaster();
        //        TempData["Error"] = ex.Message;
        //        return View(model);
        //    }
        //}


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(IPDNurseVitals model)
        {
            int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

            try
            {
                if (hospitalId <= 0)
                    throw new Exception("Invalid hospital session.");

                if (!ModelState.IsValid)
                {
                    LoadNurses();
                    LoadNursingMaster();
                    return View(model);
                }

                model.ParentHospitalId = hospitalId;
                model.SubHospitalId = subHospitalId;
                model.CreatedDate = DateTime.Now;
                model.IsActive = true;

                _vitalsRepo.CreateVitals(model);

                TempData["Success"] = "Vitals added successfully.";
                return RedirectToAction("Details", "IPDAdmission", new { id = model.IPDId });
            }
            catch (Exception ex)
            {
                LoadNurses();
                LoadNursingMaster();
                TempData["Error"] = ex.Message;
                return View(model);
            }
        }



        private void LoadNursingMaster()
        {
            int hid = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subid = HttpContext.Session.GetInt32("SubHospitalId");
            ViewBag.NursingMaster = _nursingMasterRepo.GetAll(hid, subid);
        }






        // ===============================
        // EDIT (GET)
        // ===============================
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var vitals = _vitalsRepo.GetVitalsById(id);

            if (vitals == null)
                return NotFound();

            LoadNurses();

            return View(vitals);
        }

        // ===============================
        // EDIT (POST)
        // ===============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(IPDNurseVitals model)
        {
            if (!ModelState.IsValid)
            {
                LoadNurses();
                return View(model);
            }

            model.UpdatedDate = DateTime.Now;

            _vitalsRepo.UpdateVitals(model);

            TempData["SuccessMessage"] = "Vitals updated successfully.";

            //return RedirectToAction("Index", new
            //{
            //    ipdId = model.IPDId,
            //    hospitalId = model.ParentHospitalId,
            //    subHospitalId = model.SubHospitalId
            //});
            return RedirectToAction("Details", "IPDAdmission", new { id = model.IPDId });
        }

        // ===============================
        // DELETE (SOFT DELETE)
        // ===============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int vitalsId, int ipdId, int hospitalId, int? subHospitalId)
        {
            _vitalsRepo.DeleteVitals(vitalsId);

            TempData["SuccessMessage"] = "Vitals deleted successfully.";

            return RedirectToAction("Index", new
            {
                ipdId = ipdId,
                hospitalId = hospitalId,
                subHospitalId = subHospitalId
            });
        }




        [HttpGet]
        public IActionResult AddNursingProcedure(int ipdId)
        {
            int hid = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subid = HttpContext.Session.GetInt32("SubHospitalId");
            LoadNurses();
            ViewBag.NursingMaster = _nursingMasterRepo.GetAll(hid, subid);
            ViewBag.IPDId = ipdId;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveNursingProcedures(int IPDId, List<IPDNursingCharge> NursingProcedures)
        {
            int hid = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
            int? subid = HttpContext.Session.GetInt32("SubHospitalId");
            try
            {
                if (NursingProcedures != null && NursingProcedures.Count > 0)
                {
                    foreach (var c in NursingProcedures)
                    {
                        c.IPDId = IPDId;
                        c.ParentHospitalId = hid;
                        c.SubHospitalId = subid;
                        c.ChargeDate = DateTime.Now.Date;
                        if (c.Quantity <= 0) c.Quantity = 1;
                        if (c.TotalCharge <= 0) c.TotalCharge = c.Quantity * c.UnitCharge;
                    }
                    _nursingChargeRepo.SaveCharges(NursingProcedures);
                }
                TempData["Success"] = "Nursing procedures saved successfully.";
                return RedirectToAction("Details", "IPDAdmission", new { id = IPDId });
            }
            catch (Exception ex)
            {
                LoadNurses();
                ViewBag.NursingMaster = _nursingMasterRepo.GetAll(hid, subid);
                ViewBag.IPDId = IPDId;
                TempData["Error"] = ex.Message;
                return View("AddNursingProcedure");
            }
        }





    }
}

