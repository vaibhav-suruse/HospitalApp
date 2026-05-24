// Controllers/IPDBillingController.cs
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
    public class IPDBillingController : Controller
    {
        private readonly IIPDBilling _billingRepo;
        private readonly IBillingMaster _billingMaster;
        private readonly IHospital _IHospital;
        private readonly ILogger<IPDBillingController> _logger;

        public IPDBillingController(
            IIPDBilling billingRepo,
            IBillingMaster billingMaster,
            IHospital hospital,
            ILogger<IPDBillingController> logger)
        {
            _billingRepo = billingRepo;
            _billingMaster = billingMaster;
            _IHospital = hospital;
            _logger = logger;
        }

        // ── GENERATE BILL GET ────────────────────────────────────────────
        [HttpGet]
        public IActionResult GenerateBill(int ipdId)
        {
            try
            {
                int hospitalId =
                    HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId =
                    HttpContext.Session.GetInt32("SubHospitalId");

                var vm = _billingRepo.GetBillSummary(ipdId);

                var existingBill = _billingRepo.GetBillByIPDId(ipdId);
                if (existingBill != null)
                {
                    vm.BillId = existingBill.BillId;
                    vm.PaidAmount = existingBill.PaidAmount;
                    vm.DueAmount = existingBill.DueAmount;
                    vm.PaymentStatus = existingBill.PaymentStatus;
                    vm.DiscountPercent = existingBill.DiscountPercent;
                    vm.DiscountAmount = existingBill.DiscountAmount;
                    vm.TotalAmount = existingBill.TotalAmount;
                    vm.SubTotal = existingBill.SubTotal;
                    vm.TotalNursingCharges = existingBill.NursingCharges;
                    vm.TotalOperationCharges = existingBill.OperationCharges;
                    vm.Payments = _billingRepo
                        .GetPaymentsByBillId(existingBill.BillId);
                }

                var masterItems = _billingMaster
                    .GetAllBillings(hospitalId, subHospitalId)
                    .Where(b => b.IsActive == 1)
                    .Select(b => new SelectListItem
                    {
                        Value = b.Id.ToString(),
                        Text = $"{b.Name} - ₹{b.Amount}"
                    }).ToList();

                ViewBag.BillingMasterItems = masterItems;
               
                ViewBag.BillNumber = existingBill?.BillNumber ?? "";
                vm.BillNumber = existingBill?.BillNumber ?? "";

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error in GenerateBill. IPDId={IPDId}", ipdId);
                TempData["Error"] = ex.Message;
                return RedirectToAction("Details", "IPDAdmission",
                    new { id = ipdId });
            }
        }

        // ── SAVE BILL POST ───────────────────────────────────────────────
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult SaveBill([FromBody] SaveBillRequest request)
        {
            try
            {
                int hospitalId =
                    HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId =
                    HttpContext.Session.GetInt32("SubHospitalId");
                int userId =
                    HttpContext.Session.GetInt32("UserId") ?? 0;

                var bill = new IPDBill
                {
                    IPDId = request.IPDId,
                    ParentHospitalId = hospitalId,
                    SubHospitalId = subHospitalId,
                    BillNumber =
                        "BILL-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                    BedCharges = request.BedCharges,
                    DoctorVisitCharges = request.DoctorCharges,
                    MedicineCharges = request.MedicineCharges,
                    InvestigationCharges = request.InvestigationCharges,
                    DischargeMedicineCharges = request.DischargeCharges,
                    NursingCharges = request.NursingCharges,
                    OperationCharges = request.OperationCharges,
                    OtherCharges = request.OtherCharges,
                    SubTotal = request.SubTotal,
                    DiscountPercent = request.DiscountPercent,
                    DiscountAmount = request.DiscountAmount,
                    TaxAmount = 0,
                    TotalAmount = request.TotalAmount,
                    PaidAmount = 0,
                    DueAmount = request.TotalAmount,
                    PaymentStatus = "Unpaid",
                    CreatedBy = userId
                };

                int billId = _billingRepo.SaveBill(
                    bill, request.Items ?? new List<IPDBillItem>());

                return Json(new { success = true, billId = billId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error in SaveBill. IPDId={IPDId}", request?.IPDId);
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ── ADD PAYMENT POST ─────────────────────────────────────────────
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult AddPayment([FromBody] IPDPayment payment)
        {
            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                payment.ReceivedBy = userId;
                _billingRepo.AddPayment(payment);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ── PRINT BILL GET ───────────────────────────────────────────────
        [HttpGet]
        public IActionResult PrintBill(int ipdId)
        {
            try
            {
                var vm = _billingRepo.GetBillSummary(ipdId);
                var bill = _billingRepo.GetBillByIPDId(ipdId);

                if (bill != null)
                {
                    vm.BillId = bill.BillId;
                    vm.TotalAmount = bill.TotalAmount;
                    vm.PaidAmount = bill.PaidAmount;
                    vm.DueAmount = bill.DueAmount;
                    vm.PaymentStatus = bill.PaymentStatus;
                    vm.DiscountAmount = bill.DiscountAmount;
                    vm.DiscountPercent = bill.DiscountPercent;
                    vm.SubTotal = bill.SubTotal;
                    vm.TotalBedCharges = bill.BedCharges;
                    vm.TotalDoctorCharges = bill.DoctorVisitCharges;
                    vm.TotalMedicineCharges = bill.MedicineCharges;
                    vm.TotalInvestigationCharges = bill.InvestigationCharges;
                    vm.TotalDischargeMedCharges =
                        bill.DischargeMedicineCharges;
                    vm.TotalNursingCharges = bill.NursingCharges;
                    vm.TotalOperationCharges = bill.OperationCharges;
                    vm.TotalOtherCharges = bill.OtherCharges;
                    vm.Payments =
                        _billingRepo.GetPaymentsByBillId(bill.BillId);
                    vm.OtherItems =
                        _billingRepo.GetBillItems(bill.BillId);
                }

                int hospitalId =
                    HttpContext.Session.GetInt32("MainHospitalId") ?? 0;
                int? subHospitalId =
                    HttpContext.Session.GetInt32("SubHospitalId");
                var hospital = _IHospital
                    .GetsubandMainHospitalById(hospitalId, subHospitalId);

                ViewBag.HospitalName = hospital?.Name;
                ViewBag.HospitalAddress = hospital?.Address;
                ViewBag.HospitalPhone = hospital?.PhoneNumber;
                ViewBag.HospitalEmail = hospital?.EmailId;
                ViewBag.HospitalLogo = hospital?.Logo;
                ViewBag.HospitalRegNo = hospital?.RegistrationNumber;
                ViewBag.BillNumber = bill?.BillNumber ?? "N/A";

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error in PrintBill. IPDId={IPDId}", ipdId);
                TempData["Error"] = ex.Message;
                return RedirectToAction("GenerateBill",
                    new { ipdId = ipdId });
            }
        }

        // ── PATIENT BILLING SUMMARY ──────────────────────────────────────
        [HttpGet]
        public IActionResult PatientBillingSummary(int ipdId)
        {
            try
            {
                var vm = _billingRepo.GetPatientBillingSummary(ipdId);
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error in PatientBillingSummary. IPDId={IPDId}", ipdId);
                TempData["Error"] = ex.Message;
                return RedirectToAction("GenerateBill", new { ipdId });
            }
        }

        // ── REQUEST MODEL ────────────────────────────────────────────────
        public class SaveBillRequest
        {
            public int IPDId { get; set; }
            public decimal BedCharges { get; set; }
            public decimal DoctorCharges { get; set; }
            public decimal MedicineCharges { get; set; }
            public decimal InvestigationCharges { get; set; }
            public decimal DischargeCharges { get; set; }
            public decimal NursingCharges { get; set; }
            public decimal OperationCharges { get; set; }
            public decimal OtherCharges { get; set; }
            public decimal SubTotal { get; set; }
            public decimal DiscountPercent { get; set; }
            public decimal DiscountAmount { get; set; }
            public decimal TotalAmount { get; set; }
            public List<IPDBillItem> Items { get; set; }
        }

        [HttpGet]
        public IActionResult GetBillPanel(int ipdId)
        {
            var vm = _billingRepo.GetBillSummary(ipdId);
            var existingBill = _billingRepo.GetBillByIPDId(ipdId);
            if (existingBill != null)
            {
                vm.BillId = existingBill.BillId;
                vm.BillNumber = existingBill.BillNumber;
                vm.PaidAmount = existingBill.PaidAmount;
                vm.DueAmount = existingBill.DueAmount;
                vm.PaymentStatus = existingBill.PaymentStatus;
                vm.DiscountPercent = existingBill.DiscountPercent;
                vm.DiscountAmount = existingBill.DiscountAmount;
                vm.TotalAmount = existingBill.TotalAmount;
                vm.SubTotal = existingBill.SubTotal;
                vm.TotalNursingCharges = existingBill.NursingCharges;
                vm.TotalOperationCharges = existingBill.OperationCharges;
                vm.Payments = _billingRepo.GetPaymentsByBillId(existingBill.BillId);
            }
            return PartialView("_BillSummaryPanel", vm);
        }

        [HttpGet]
        public IActionResult GetBillBanner(int ipdId)
        {
            var bill = _billingRepo.GetBillByIPDId(ipdId);
            if (bill == null) return Content("");
            var html = $@"<div class='alert alert-success py-2 mb-3'>
        <i class='fas fa-check-circle me-1'></i>
        <strong>Bill No: {bill.BillNumber}</strong>
        &nbsp;|&nbsp; Status: <strong>{bill.PaymentStatus}</strong>
        &nbsp;|&nbsp; Total: <strong>₹{bill.TotalAmount:0.00}</strong>
        &nbsp;|&nbsp; Paid: <strong>₹{bill.PaidAmount:0.00}</strong>
        &nbsp;|&nbsp; Due: <strong class='text-danger'>₹{bill.DueAmount:0.00}</strong>
    </div>";
            return Content(html, "text/html");
        }



    }
}