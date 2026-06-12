using PRM.Api.Extensions;
using PRM.Api.BackgroundServices;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddPrmSwagger();
builder.Services.AddPrmInfrastructure(builder.Configuration);
builder.Services.AddPrmBusinessServices(builder.Configuration);
builder.Services.AddPrmAuthentication(builder.Configuration);
builder.Services.AddHostedService<PrmBackgroundSchedulerService>();
var app = builder.Build();
await app.Services.SeedDatabaseAsync();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
