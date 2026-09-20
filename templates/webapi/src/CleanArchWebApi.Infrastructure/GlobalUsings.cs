global using CleanArchWebApi.Application.Common.Persistence;
#if (IncludeSample)
global using CleanArchWebApi.Domain.Entities;
#endif
global using Dorn.Messaging.Contracts;
global using Dorn.SharedKernel;
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
