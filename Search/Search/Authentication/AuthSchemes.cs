﻿
using AspNetCore.Authentication.ApiKey;

using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Catalog.Search.Authentication;

public static class AuthSchemes
{
    public const string Default =
        JwtBearerDefaults.AuthenticationScheme + "," +
        ApiKeyDefaults.AuthenticationScheme;
}