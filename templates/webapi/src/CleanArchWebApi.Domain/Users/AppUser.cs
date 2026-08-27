#if (UseCustomAuth)
using Microsoft.AspNetCore.Identity;

namespace CleanArchWebApi.Domain.Users;

public class AppUser : IdentityUser<Guid> { }
#endif
