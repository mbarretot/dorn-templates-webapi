var builder = DistributedApplication.CreateBuilder(args);

#if (UseSqlite)
builder.AddProject<Projects.CleanArchWebApi_WebApi>("webapi");
#elif (UseSqlServer)
var sql = builder.AddSqlServer("sql").AddDatabase("CleanArchWebApi");
builder.AddProject<Projects.CleanArchWebApi_WebApi>("webapi").WithReference(sql);
#elif (UsePostgres)
var postgres = builder.AddPostgres("postgres").AddDatabase("CleanArchWebApi");
builder.AddProject<Projects.CleanArchWebApi_WebApi>("webapi").WithReference(postgres);
#endif

builder.Build().Run();
