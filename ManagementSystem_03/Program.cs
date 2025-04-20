using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<DbContext>(options =>
options.UseSqlServer(builder.Configuration.GetConnectionString("MyDatabase")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();


//app.MapControllerRoute(
//    name: "deleteTable",
//    pattern: "TablesController/DeletePost"); 

//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Tables}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "deleteTable",
    pattern: "TablesController/DeletePost",
    defaults: new { controller = "Tables", action = "DeletePost" },
    constraints: new { httpMethod = new HttpMethodRouteConstraint("POST") }
);

app.MapControllerRoute(
    name: "editTable",
    pattern: "Tables/Edit/{tableName}",
    defaults: new { controller = "Tables", action = "Edit" },
    constraints: new { httpMethod = new HttpMethodRouteConstraint("POST") }
);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Tables}/{action=Index}/{id?}"
);

app.Run();

