# Bootstrap source notices

Dependencies retain their own original license/notice files on restore. See the
[complete inventory and compliance decisions](dependencies.md); this short file
does not replace bundled native-library notices or source obligations.

The button in `apps/web/components/ui/button.tsx` follows the shadcn/ui
component pattern, adapted for this small page.

## shadcn/ui — MIT License

Copyright (c) 2023 shadcn

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
of the Software, and to permit persons to whom the Software is furnished to do
so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

Lucide icons are used under their packaged ISC and icon-specific notices;
retain `lucide-react`'s original license files in redistributed output.

## Spec 003 storage dependencies

EF Core, Relational, Sqlite, Sqlite.Core, Abstractions and Analyzers 10.0.13,
and Microsoft.Extensions.Caching.Memory/Abstractions 10.0.13, are MIT-licensed
Microsoft components. Retain their original copyright/license attribution and
the caching packages' `THIRD-PARTY-NOTICES.TXT` on redistribution.
The exact-source license is
[dotnet/dotnet, commit 4099f83977f4d29604a9aeb971fb0a7bc1d5f699](https://github.com/dotnet/dotnet/blob/4099f83977f4d29604a9aeb971fb0a7bc1d5f699/src/efcore/LICENSE.txt).

Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3 uses the permissive PostgreSQL
license. Retain its copyright, license and disclaimer from
[npgsql/efcore.pg, commit 5e912bf071068a44e5abbdab497b313ba99d3ee1](https://github.com/npgsql/efcore.pg/blob/5e912bf071068a44e5abbdab497b313ba99d3ee1/LICENSE).
No covered upstream source is modified. SQLite and its native bundle retain
the existing reviewed graph and notices; this does not approve stripping
native-library notices or imply all dependencies have the application's MIT
license. The complete inventory and compliance decisions remain authoritative.

## Spec 004 DI project edge

Microsoft.Extensions.DependencyInjection.Abstractions 10.0.13 is an already
reviewed MIT-licensed Microsoft component, now required directly by the
platform author contract. Retain its original copyright/license and packaged
`THIRD-PARTY-NOTICES.TXT`; no upstream source modification or additional
license family is introduced. Its exact source is
[dotnet/dotnet, commit 4099f83977f4d29604a9aeb971fb0a7bc1d5f699](https://github.com/dotnet/dotnet/blob/4099f83977f4d29604a9aeb971fb0a7bc1d5f699/src/runtime/LICENSE.TXT).
