#if (UseCustomAuthWithDapper)
#error Auth=custom requires Orm=efcore. The custom user store (AppUser) and its migrations only exist under EF Core — regenerate with --Orm efcore, or --Auth none / --Auth azure-ad to keep Dapper.
#endif
#if (LocalizationIsInvalid)
#error IncludeLocalization=true requires DefaultLanguage and Languages to be culture codes such as en or pt-BR: Languages is a comma-separated list of at most 24 unique codes. Regenerate with valid values, or without --IncludeLocalization.
#endif
#if (UseCustomAuth && !IncludeSample)
#error Auth=custom requires IncludeSample=true. The seeded user's permissions and the EF Core migrations are built around the Todo sample — regenerate with --IncludeSample true, or --Auth none / --Auth azure-ad to leave the sample out.
#endif
