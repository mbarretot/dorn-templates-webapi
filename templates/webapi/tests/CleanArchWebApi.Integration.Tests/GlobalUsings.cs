global using CleanArchWebApi.Domain.Entities;
global using Dorn.Messaging.Contracts;
global using Microsoft.Data.Sqlite;
global using NSubstitute;
global using Xunit;
#if (UseEfCore)
global using CleanArchWebApi.Infrastructure.Persistence;
global using Microsoft.EntityFrameworkCore;
#endif
