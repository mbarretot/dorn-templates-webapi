#if (UseCustomAuthWithDapper)
#error Auth=custom requires Orm=efcore. The custom user store (AppUser) and its migrations only exist under EF Core — regenerate with --Orm efcore, or --Auth none / --Auth azure-ad to keep Dapper.
#endif
