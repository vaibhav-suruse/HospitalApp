using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using WebApplicationSampleTest2.Repository;
using static WebApplicationSampleTest2.Repository.BedService;

namespace WebApplicationSampleTest2
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews().AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);

            services.AddControllersWithViews().AddNewtonsoftJson();

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromSeconds(12000);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<Ipatient, PatientService>();
            services.AddScoped<ISymptom, SymptomRepository>();
            services.AddScoped<IMedicine, MedicineRepository>();
            services.AddScoped<IHospital, HospitalRepository>();
            services.AddScoped<IBillingMaster, BillingMasterRepository>();
            services.AddScoped<IUser, UserRepository>();
            services.AddScoped<IDoctor, DoctorRepository>();
            services.AddScoped<IOPDAppointment, OPDAppointmentRepository>();
            services.AddScoped<IOPD, OPDRepository>();
            services.AddScoped<IWard, WardService>();
            services.AddScoped<IRoom, RoomService>();
            services.AddScoped<IBed, BedRepository>();
            services.AddScoped<IIPDAdmission, IPDAdmissionRepository>();
            services.AddScoped<IReferenceDoctor, ReferenceDoctorRepository>();
            services.AddScoped<IIPDBedAllocation, IPDBedAllocationRepository>();
            services.AddScoped<INurse, NurseRepository>();
            services.AddScoped<IIPDNurseVitals, IPDNurseVitalsRepository>();
            services.AddScoped<IDoctorRound, DoctorRoundRepository>();
            services.AddScoped<ILabInvestigation, LabInvestigationRepository>();
            services.AddScoped<IDischarge, DischargeRepository>();
            services.AddScoped<IIPDBilling, IPDBillingRepository>();
            services.AddScoped<IOperationMaster, OperationMasterRepository>();
            services.AddScoped<IIPDOperation, IPDOperationRepository>();
            services.AddScoped<INursingChargesMaster, NursingChargesMasterRepository>();
            services.AddScoped<IIPDNursingCharge, IPDNursingChargeRepository>();
            services.AddScoped<IOPDBilling, OPDBillingRepository>();
            services.AddScoped<IPatientPortal, PatientPortalRepository>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IInventory, InventoryRepository>();
            services.AddScoped<ISupplier, SupplierRepository>();
            services.AddScoped<ICategory, CategoryRepository>();
            services.AddScoped<INotification, NotificationRepository>();
            services.AddScoped<ICounter, CounterRepository>();
            services.AddScoped<IPharmacyQueue, PharmacyQueueRepository>();


        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseDeveloperExceptionPage();
                //app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                // app.UseHsts();
            }
            //app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseSession();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                   // pattern: "{controller=Login}/{action=PatientLogin}/{id?}");
                   pattern: "{controller=User}/{action=Login}/{id?}");
        });
        }
    }
}
