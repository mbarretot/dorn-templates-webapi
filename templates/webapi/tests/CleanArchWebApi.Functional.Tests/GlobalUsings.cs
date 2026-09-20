global using System.Net;
global using System.Net.Http.Json;
global using Microsoft.AspNetCore.Hosting;
global using Microsoft.AspNetCore.Mvc.Testing;
global using Microsoft.Data.Sqlite;
global using Xunit;
#if (IncludeSample)
global using CleanArchWebApi.Application.Todos.GetTodoItems;
#endif
#if (UseEfCore)
global using CleanArchWebApi.Infrastructure.Persistence;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.EntityFrameworkCore.Diagnostics;
global using Microsoft.EntityFrameworkCore.Infrastructure;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.DependencyInjection.Extensions;
#endif
