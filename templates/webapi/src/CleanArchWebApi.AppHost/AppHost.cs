var builder = DistributedApplication.CreateBuilder(args);

#if (UseSqlite)
builder.AddProject<Projects.CleanArchWebApi_WebApi>("webapi");
#elif (UseSqlServer)
#if (HasConnectionString)
// External database: no container; the value comes from this project's appsettings.json.
var sql = builder.AddConnectionString("CleanArchWebApi");
#else
var sql = builder.AddSqlServer("sql").AddDatabase("CleanArchWebApi");
#endif
builder.AddProject<Projects.CleanArchWebApi_WebApi>("webapi").WithReference(sql);
#elif (UsePostgres)
#if (HasConnectionString)
// External database: no container; the value comes from this project's appsettings.json.
var postgres = builder.AddConnectionString("CleanArchWebApi");
#else
var postgres = builder.AddPostgres("postgres").AddDatabase("CleanArchWebApi");
#endif
builder.AddProject<Projects.CleanArchWebApi_WebApi>("webapi").WithReference(postgres);
#endif

builder.Build().Run();
