global using CleanArchWebApi.Application.Common.Persistence;
global using Dorn.Messaging.Contracts;
global using Dorn.SharedKernel;
#if (IncludeSample)
global using CleanArchWebApi.Domain.Entities;
#endif
#if (UseEfCore)
global using CleanArchWebApi.Infrastructure.Persistence;
#if (IncludeSample)
global using CleanArchWebApi.Infrastructure.Repositories.EfCore;
#endif
global using Microsoft.EntityFrameworkCore;
#endif
#if (UseDapper)
global using CleanArchWebApi.Infrastructure.Repositories.Dapper;
#endif
