global using CleanArchWebApi.Application.Common.Persistence;
global using CleanArchWebApi.Domain.Entities;
global using Dorn.Messaging.Contracts;
global using Dorn.SharedKernel;
#if (UseEfCore)
global using CleanArchWebApi.Infrastructure.Persistence;
global using CleanArchWebApi.Infrastructure.Repositories.EfCore;
global using Microsoft.EntityFrameworkCore;
#endif
#if (UseDapper)
global using CleanArchWebApi.Infrastructure.Repositories.Dapper;
#endif
