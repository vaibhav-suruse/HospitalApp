using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using WebApplicationSampleTest2.Models;
using WebApplicationSampleTest2.Repository;

namespace WebApplicationSampleTest2.Controllers
{
    public class OPDBillingController : Controller
    {
        private readonly IOPDBilling _billingRepo;
        private readonly IBillingMaster _billingMaster;
        private readonly ILogger<OPDBillingController> _logger;

        public OPDBillingController(
            IOPDBilling billingRepo,
            IBillingMaster billingMaster,
            ILogger<OPDBillingController> logger)
        {
            _billingRepo = billingRepo;
            _billingMaster = billingMaster;
            _logger = logger;
        }

        // ── GENERATE BILL (GET) ──────────────────────────────────────────
        [HttpGet]
        public IActionResult GenerateBill(int appointmentId)
        {
            try
            {
                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");

                var vm = _billingRepo.GetBillSummary(appointmentId);

                // Check if bill already exists
                var existingBill = _billingRepo.GetBillByAppointmentId(appointmentId);
                if (existingBill != null)
                {
                    vm.BillId = existingBill.BillId;
                    vm.BillNumber = existingBill.BillNumber;
                    vm.ConsultationFee = existingBill.ConsultationFee;
                    vm.MedicineCharges = existingBill.MedicineCharges;
                    vm.ProcedureCharges = existingBill.ProcedureCharges;
                    vm.OtherCharges = existingBill.OtherCharges;
                    vm.SubTotal = existingBill.SubTotal;
                    vm.DiscountPercent = existingBill.DiscountPercent;
                    vm.DiscountAmount = existingBill.DiscountAmount;
                    vm.TotalAmount = existingBill.TotalAmount;
                    vm.PaidAmount = existingBill.PaidAmount;
                    vm.DueAmount = existingBill.DueAmount;
                    vm.PaymentStatus = existingBill.PaymentStatus;
                    vm.PaymentMode = existingBill.PaymentMode;
                    vm.OtherItems = _billingRepo.GetBillItems(existingBill.BillId);
                }

                // Billing master items for procedure dropdown
                var masterItems = _billingMaster
                    .GetAllBillings(hospitalId, subHospitalId)
                    .Where(b => b.IsActive == 1)
                    .Select(b => new SelectListItem
                    {
                        Value = b.Id.ToString(),
                        Text = $"{b.Name} - ₹{b.Amount}"
                    }).ToList();

                ViewBag.BillingMasterItems = masterItems;
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GenerateBill. AppointmentId={Id}", appointmentId);
                TempData["Error"] = ex.Message;
                return RedirectToAction("Index", "OPDAppointment");
            }
        }

        // ── SAVE BILL (POST) ─────────────────────────────────────────────
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult SaveBill([FromBody] SaveOPDBillRequest request)
        {
            try
            {
                int hospitalId = HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId = HttpContext.Session.GetInt32("SubHospitalId");
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;

                var bill = new OPDBill
                {
                    AppointmentId = request.AppointmentId,
                    OPDId = request.OPDId,
                    PatientId = request.PatientId,
                    HospitalId = hospitalId,
                    SubHospitalId = subHospitalId,
                    BillNumber = "OPDBILL-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                    ConsultationFee = request.ConsultationFee,
                    MedicineCharges = request.MedicineCharges,
                    ProcedureCharges = request.ProcedureCharges,
                    OtherCharges = request.OtherCharges,
                    SubTotal = request.SubTotal,
                    DiscountPercent = request.DiscountPercent,
                    DiscountAmount = request.DiscountAmount,
                    TotalAmount = request.TotalAmount,
                    PaidAmount = 0,
                    DueAmount = request.TotalAmount,
                    PaymentStatus = "Unpaid",
                    CreatedBy = userId
                };

                int billId = _billingRepo.SaveBill(bill, request.Items ?? new List<OPDBillItem>());
                return Json(new { success = true, billId = billId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SaveBill");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ── PAY BILL (POST) ──────────────────────────────────────────────
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult PayBill([FromBody] PayOPDBillRequest request)
        {
            try
            {
                _billingRepo.PayBill(
                    request.BillId,
                    request.Amount,
                    request.PaymentMode,
                    request.TransactionRef);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PayBill. BillId={Id}", request?.BillId);
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ── GET BILL PANEL (AJAX partial reload) ─────────────────────────
        [HttpGet]
        public IActionResult GetBillPanel(int appointmentId)
        {
            try
            {
                var vm = _billingRepo.GetBillSummary(appointmentId);
                var existingBill = _billingRepo.GetBillByAppointmentId(appointmentId);
                if (existingBill != null)
                {
                    vm.BillId = existingBill.BillId;
                    vm.BillNumber = existingBill.BillNumber;
                    vm.ConsultationFee = existingBill.ConsultationFee;
                    vm.MedicineCharges = existingBill.MedicineCharges;
                    vm.ProcedureCharges = existingBill.ProcedureCharges;
                    vm.OtherCharges = existingBill.OtherCharges;
                    vm.SubTotal = existingBill.SubTotal;
                    vm.DiscountPercent = existingBill.DiscountPercent;
                    vm.DiscountAmount = existingBill.DiscountAmount;
                    vm.TotalAmount = existingBill.TotalAmount;
                    vm.PaidAmount = existingBill.PaidAmount;
                    vm.DueAmount = existingBill.DueAmount;
                    vm.PaymentStatus = existingBill.PaymentStatus;
                    vm.PaymentMode = existingBill.PaymentMode;
                }
                return PartialView("_OPDBillPanel", vm);
            }
            catch (Exception ex)
            {
                return Content("<div class='alert alert-danger'>Error: " + ex.Message + "</div>");
            }
        }
    }
}
