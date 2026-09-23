using Microsoft.EntityFrameworkCore;
using YMI_PMT_PayrollManagement_API.Data;
using YMI_PMT_PayrollManagement_API.Interfaces.Repository;
using YMI_PMT_PayrollManagement_API.Interfaces.Services;
using YMI_PMT_PayrollManagement_API.Repository;
using YMI_PMT_PayrollManagement_API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// EF Core DbContext with Retry on Failure (transient error resiliency)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null
        )
    ));

// TIER 3: Repository
// TIER 3: Repository
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserMasterRepository, UserMasterRepository>();
builder.Services.AddScoped<IMailConfigRepository, MailConfigRepository>();
builder.Services.AddScoped<IVendorMasterRepository, VendorMasterRepository>();
builder.Services.AddScoped<IEmployeeMasterRepository, EmployeeMasterRepository>();
builder.Services.AddScoped<ICalendarMasterRepository, CalendarMasterRepository>();
builder.Services.AddScoped<ISalaryStructureRepository, SalaryStructureRepository>();
builder.Services.AddScoped<ISalaryFormulaRepository, SalaryFormulaRepository>();   // ← only once
builder.Services.AddScoped<IPayrollEmployeeRepository, PayrollEmployeeRepository>();


// TIER 2: Service
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserMasterService, UserMasterService>();
builder.Services.AddScoped<IMailConfigService, MailConfigService>();
builder.Services.AddScoped<IVendorMasterService, VendorMasterService>();
builder.Services.AddScoped<IEmployeeMasterService, EmployeeMasterService>();
builder.Services.AddScoped<ICalendarMasterService, CalendarMasterService>();
builder.Services.AddScoped<ISalaryStructureService, SalaryStructureService>();
builder.Services.AddScoped<ISalaryFormulaService, SalaryFormulaService>();       // ← THIS WAS MISSING
builder.Services.AddScoped<IPayrollEmployeeService, PayrollEmployeeService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "YMI PMT Payroll Management API",
        Version = "v1.0",
        Description = "3-Tier Architecture - Auth Service (EF Core)"
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();
app.Run();