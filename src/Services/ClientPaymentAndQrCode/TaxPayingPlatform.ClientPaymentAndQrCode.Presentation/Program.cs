using TaxPayingPlatform.ClientPaymentAndQrCode.Application.Repositories;
using TaxPayingPlatform.ClientPaymentAndQrCode.Application.Services;
using TaxPayingPlatform.ClientPaymentAndQrCode.Presentation.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

//controllers for routing
builder.Services.AddControllers();

//Register service dependencies
builder.Services.AddSingleton<ITaxPaymentInstructionRepository, InMemoryTaxPaymentInstructionRepository>();
builder.Services.AddScoped<ITaxPaymentInstructionService, TaxPaymentInstructionService>();

var app = builder.Build();

app.UseHttpsRedirection();
app.MapControllers();

app.Run();